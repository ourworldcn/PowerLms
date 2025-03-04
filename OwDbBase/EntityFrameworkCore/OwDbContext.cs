/*
 * OwDbContext.cs
 * 版权所有 (c) 2023 PowerLms. 保留所有权利。
 * 此文件包含 OwDbContext 类的定义，该类用于管理数据库上下文并自动生成存储过程。
 * 作者: OW
 * 创建日期: 2023-10-10
 * 修改日期: 2023-10-10
 */

using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Buffers;
using System.Collections.Generic;

namespace OW.EntityFrameworkCore
{

    /// <summary>
    /// 表示 OwDbContext 类，用于管理数据库上下文并自动生成存储过程。
    /// </summary>
    public class OwDbContext : DbContext
    {
        #region 字段

        private readonly ILogger<OwDbContext> _Logger;
        private readonly IServiceProvider _ServiceProvider;

        private const string _CreateGetRootIdProcedure = @"
                                CREATE PROCEDURE GetRootId
                                    @TableName NVARCHAR(128),
                                    @IdField NVARCHAR(128),
                                    @ParentIdField NVARCHAR(128)
                                AS
                                BEGIN
                                    SET NOCOUNT ON;

                                    DECLARE @RootId UNIQUEIDENTIFIER;
                                    DECLARE @Sql NVARCHAR(MAX);

                                    -- 动态SQL语句
                                    SET @Sql = N'
                                        SELECT @RootId = ' + QUOTENAME(@IdField) + '
                                        FROM ' + QUOTENAME(@TableName) + '
                                        WHERE ' + QUOTENAME(@ParentIdField) + ' IS NULL
                                    ';

                                    -- 获取根组织机构的Id
                                    EXEC sp_executesql @Sql, N'@RootId UNIQUEIDENTIFIER OUTPUT', @RootId OUTPUT;

                                    -- 返回根组织机构的Id
                                    SELECT @RootId AS RootId;
                                END
                            ";

        #endregion 字段

        #region 构造函数

        /// <summary>
        /// 初始化 OwDbContext 类的新实例。
        /// </summary>
        /// <param name="options">用于此上下文的选项。</param>
        /// <param name="logger">日志记录器实例。</param>
        public OwDbContext(DbContextOptions<OwDbContext> options, IServiceProvider serviceProvider, ILogger<OwDbContext> logger) : base(options)
        {
            _ServiceProvider = serviceProvider;
            _Logger = logger;

            // 订阅事件
            SavingChanges += OnSavingChanges;
        }

        #endregion 构造函数

        #region 属性
        /// <summary>
        /// 本对象生成时注入的服务提供者。
        /// </summary>
        public IServiceProvider Service => _ServiceProvider;
        #endregion 属性

        #region 方法

        /// <summary>
        /// 初始化数据库，检查并创建存储过程。
        /// </summary>
        /// <param name="context">数据库上下文实例。</param>
        public static void InitializeDatabase(OwDbContext context)
        {
            context.Database.ExecuteSqlRaw(@"
                                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'GetRootId')
                                    BEGIN
                                        EXEC sp_executesql N'" + _CreateGetRootIdProcedure + @"'
                                    END
                                ");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            InitializeDatabase(this);
        }

        /// <summary>
        /// 调用存储过程以获取根 ID。
        /// </summary>
        /// <param name="tableName">表名。</param>
        /// <param name="idField">ID 字段名。</param>
        /// <param name="parentIdField">父 ID 字段名。</param>
        /// <returns>根 ID。</returns>
        public Guid GetRootId(string tableName, string idField, string parentIdField)
        {
            try
            {
                _Logger.LogDebug("调用存储过程 GetRootId，参数：TableName={TableName}, IdField={IdField}, ParentIdField={ParentIdField}", tableName, idField, parentIdField);

                var tableNameParameter = new SqlParameter("@TableName", tableName);
                var idFieldParameter = new SqlParameter("@IdField", idField);
                var parentIdFieldParameter = new SqlParameter("@ParentIdField", parentIdField);

                var result = Set<RootIdResult>().FromSqlRaw(
                    "EXEC GetRootId @TableName, @IdField, @ParentIdField",
                    tableNameParameter,
                    idFieldParameter,
                    parentIdFieldParameter
                ).AsEnumerable().FirstOrDefault();

                return result?.RootId ?? Guid.Empty;
            }
            catch (SqlException ex)
            {
                _Logger.LogError(ex, "执行存储过程 GetRootId 时发生 SQL 异常。");
                throw;
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "执行存储过程 GetRootId 时发生异常。");
                throw;
            }
        }

        /// <summary>
        /// 在保存更改之前触发的事件处理程序。
        /// </summary>
        /// <param name="sender">事件的发送者，通常是 DbContext 实例。</param>
        /// <param name="e">事件参数。</param>
        private void OnSavingChanges(object sender, SavingChangesEventArgs e)
        {
            var context = (OwDbContext)sender;

            // 用于跟踪已处理的实体条目
            var processedEntities = new HashSet<EntityEntry>();

            // 定义一个标志，用于控制循环
            bool hasNewChanges;

            do
            {
                hasNewChanges = false;

                using (var pooledArray = context.ChangeTracker.Entries()
                    .Where(e => (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted) && !processedEntities.Contains(e))
                    .TryToPooledArray())
                    if (pooledArray.Count > 0)
                    {
                        // 按实体类型分组
                        var groupedEntities = pooledArray.Array
                            .Take(pooledArray.Count)
                            .GroupBy(e => e.Entity.GetType());

                        foreach (var group in groupedEntities)
                        {
                            var entityType = group.Key;
                            var savingInterfaceType = typeof(IDbContextSaving<>).MakeGenericType(entityType);

                            var savingServices = _ServiceProvider.GetServices(savingInterfaceType);
                            foreach (var service in savingServices)
                            {
                                var method = savingInterfaceType.GetMethod(nameof(IDbContextSaving<object>.Saving));
                                method?.Invoke(service, new object[] { group, _ServiceProvider, new Dictionary<object, object>() });

                                // 将已处理的实体条目添加到集合中
                                foreach (var entityEntry in group)
                                {
                                    processedEntities.Add(entityEntry);
                                }
                                hasNewChanges = true; // 仅在处理了新的实体时设置为 true
                            }
                        }
                    }
            } while (hasNewChanges);

            // 触发 IAfterDbContextSaving 接口的方法
            var afterGroupedEntities = processedEntities.GroupBy(e => e.Entity.GetType());

            foreach (var group in afterGroupedEntities)
            {
                var entityType = group.Key;
                var afterSavingInterfaceType = typeof(IAfterDbContextSaving<>).MakeGenericType(entityType);

                var afterSavingServices = _ServiceProvider.GetServices(afterSavingInterfaceType);
                foreach (var service in afterSavingServices)
                {
                    var method = afterSavingInterfaceType.GetMethod(nameof(IAfterDbContextSaving<object>.AfterSaving));
                    method?.Invoke(service, new object[] { context, _ServiceProvider, new Dictionary<object, object>() });
                }
            }
        }
        #endregion 方法

        #region 嵌套类型

        /// <summary>
        /// 表示存储过程返回的根 ID 结果。
        /// </summary>
        private class RootIdResult
        {
            /// <summary>
            /// 获取或设置根 ID。
            /// </summary>
            public Guid RootId { get; set; }
        }

        #endregion 嵌套类型
    }
}

