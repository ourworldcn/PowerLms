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
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace OW.EntityFrameworkCore
{

    /// <summary>
    /// 表示 OwDbContext 类，用于管理数据库上下文并自动生成存储过程。
    /// </summary>
    public abstract class OwDbContext : DbContext
    {
        #region 字段

        private readonly ILogger _Logger;
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
        /// <param name="serviceProvider">服务容器。</param>
        protected OwDbContext(DbContextOptions options, ILogger logger, IServiceProvider serviceProvider) : base(options)
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
        public IServiceProvider ServiceProvider => _ServiceProvider;
        #endregion 属性

        #region 方法

        private static bool _databaseInitialized = false;
        private static readonly object _initLock = new object();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            // 仅在第一个实例时初始化数据库
            if (!_databaseInitialized)
            {
                lock (_initLock)
                {
                    if (!_databaseInitialized)
                    {
                        try
                        {
                            //InitializeDatabase(this);
                            _databaseInitialized = true;
                        }
                        catch (Exception ex)
                        {
                            _Logger.LogError(ex, "数据库初始化失败");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 初始化数据库，检查并创建存储过程。
        /// </summary>
        /// <param name="context">数据库上下文实例。</param>
        public static void InitializeDatabase(OwDbContext context)
        {
            context.Database.ExecuteSqlRaw(@"IF NOT EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'GetRootId')
                                                BEGIN
                                                    EXEC sp_executesql N'" + _CreateGetRootIdProcedure + @"'
                                                END");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
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
            var processedEntities = new HashSet<object>();
            var types = new HashSet<Type>();
            // 定义一个标志，用于控制循环
            bool hasNewChanges;

            do
            {
                hasNewChanges = false;
                if (!ChangeTracker.AutoDetectChangesEnabled)    //若没有开启自动检测更改，则手动检测
                    ChangeTracker.DetectChanges();
                using (var pooledArray = context.ChangeTracker.Entries()
                    .Where(c => (c.State == EntityState.Added || c.State == EntityState.Modified || c.State == EntityState.Deleted) && !processedEntities.Contains(c.Entity))
                    .TryToPooledArray())
                    if (pooledArray.Count > 0)
                    {
                        // 按实体类型分组
                        //var groupedEntities = pooledArray.Array
                        //    .Take(pooledArray.Count)
                        //    .GroupBy(c => c.Metadata.ClrType).ToArray();  //避免获取到代理类的类型

                        var groupedEntities = pooledArray.Array
                            .Take(pooledArray.Count)
                            .ToLookup(c => c.Metadata.ClrType);  //避免获取到代理类的类型

                        foreach (var group in groupedEntities)
                        {
                            var entityType = group.Key;
                            var savingInterfaceType = typeof(IDbContextSaving<>).MakeGenericType(entityType);

                            var savingServices = _ServiceProvider.GetServices(savingInterfaceType);
                            types.Add(entityType); // 添加到类型集合中
                            foreach (var service in savingServices)
                            {
                                var method = savingInterfaceType.GetMethod(nameof(IDbContextSaving<object>.Saving));
                                method?.Invoke(service, new object[] { group, _ServiceProvider, new Dictionary<object, object>() });

                                // 将已处理的实体条目添加到集合中
                                foreach (var entityEntry in group)
                                {
                                    processedEntities.Add(entityEntry.Entity);
                                }
                                hasNewChanges = true; // 仅在处理了新的实体时设置为 true
                            }
                        }
                    }
            } while (hasNewChanges);

            // 触发 IAfterDbContextSaving 接口的方法

            foreach (var entityType in types)
            {
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

    /// <summary>
    /// 为 <see cref="DbSet{TEntity}"/> 提供扩展方法。
    /// </summary>
    public static class OwDbSetExtensions
    {
        /// <summary>
        /// 使用指定的条件从数据库加载实体，并合并本地跟踪器中符合条件的实体。
        /// </summary>
        /// <typeparam name="TEntity">实体类型。</typeparam>
        /// <param name="dbSet">数据库集合。</param>
        /// <param name="predicate">用于筛选实体的表达式。</param>
        /// <returns>符合条件的实体列表，包括数据库和本地跟踪器中的实体。</returns>
        public static IEnumerable<TEntity> WhereWithLocal<TEntity>(this DbSet<TEntity> dbSet, Expression<Func<TEntity, bool>> predicate)
            where TEntity : class
        {
            // 从数据库加载符合条件的实体
            dbSet.Where(predicate).Load();

            // 编译表达式，用于在本地集合上筛选
            var compiledPredicate = predicate.Compile();

            // 从本地集合中获取符合条件的实体（排除已删除的实体）
            var db = dbSet.GetDbContext();
            return dbSet.Local
                .Where(compiledPredicate)
                .Where(e => db.Entry(e).State != EntityState.Deleted);
        }

        /// <summary>
        /// 安全地使用指定的条件从数据库加载实体，并合并本地跟踪器中符合条件的实体，避免"枚举子在枚举期间基础集合被改变"异常。
        /// </summary>
        /// <typeparam name="TEntity">实体类型。</typeparam>
        /// <param name="dbSet">数据库集合。</param>
        /// <param name="predicate">用于筛选实体的表达式。</param>
        /// <returns>符合条件的实体列表，包括数据库和本地跟踪器中的实体。</returns>
        public static List<TEntity> WhereWithLocalSafe<TEntity>(this DbSet<TEntity> dbSet, Expression<Func<TEntity, bool>> predicate)
            where TEntity : class
        {
            // 直接调用 WhereWithLocal 方法并将结果转换为列表
            return dbSet.WhereWithLocal(predicate).ToList();
        }

        /// <summary>
        /// 从本地跟踪器和数据库中查找满足条件的第一个实体。如果找不到，则返回默认值。
        /// </summary>
        /// <typeparam name="TEntity">实体类型。</typeparam>
        /// <param name="dbSet">数据库集合。</param>
        /// <param name="predicate">用于筛选实体的表达式。</param>
        /// <returns>满足条件的第一个实体，若没有则返回默认值。</returns>
        public static TEntity FirstOrDefaultWithLocal<TEntity>(this DbSet<TEntity> dbSet, Expression<Func<TEntity, bool>> predicate)
            where TEntity : class
        {
            var db = dbSet.GetDbContext();
            var compiledPredicate = predicate.Compile();

            // 先检查本地跟踪器中新添加的实体
            return db.ChangeTracker.Entries<TEntity>()
                .Where(e => e.State != EntityState.Deleted)
                .Select(e => e.Entity)
                .FirstOrDefault(compiledPredicate) ?? dbSet.FirstOrDefault(predicate);
        }

        /// <summary>
        /// 获取 DbSet 关联的 DbContext 实例。
        /// </summary>
        public static DbContext GetDbContext<TEntity>(this DbSet<TEntity> dbSet) where TEntity : class
        {
            var infrastructure = dbSet as IInfrastructure<IServiceProvider>;
            var serviceProvider = infrastructure.Instance;
            var currentDbContext = serviceProvider.GetService(typeof(ICurrentDbContext)) as ICurrentDbContext;
            return currentDbContext.Context;
        }
    }
}

