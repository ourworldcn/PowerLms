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

namespace OW.EntityFrameworkCore
{

    /// <summary>
    /// 表示 OwDbContext 类，用于管理数据库上下文并自动生成存储过程。
    /// </summary>
    public class OwDbContext : DbContext
    {
        #region 字段

        private static bool _IsInitialized = false;
        private static readonly object _Lock = new object();
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
            Initialize();
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

                var result = this.Set<RootIdResult>().FromSqlRaw(
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
        /// 初始化方法，只在第一个 DbContext 实例创建时调用。
        /// </summary>
        private void Initialize()
        {
            if (!_IsInitialized)
            {
                lock (_Lock)
                {
                    if (!_IsInitialized)
                    {
                        _Logger.LogDebug("初始化数据库。");
                        InitializeDatabase(this);
                        _IsInitialized = true;
                    }
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

