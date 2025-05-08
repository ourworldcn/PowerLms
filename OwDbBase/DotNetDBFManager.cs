/*
 * 文件名：DotNetDBFManager.cs
 * 作者：OW
 * 创建日期：2025年5月8日
 * 修改日期：2025年5月8日
 * 描述：该文件包含 DotNetDBFManager 类的实现，用于操作 DBF 文件。
 */

using System;
using System.Data;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetDBF;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.ObjectPool;

namespace OW.Data
{
    /// <summary>
    /// DotNetDBFManager 类，提供高效的 DBF 文件操作功能。
    /// </summary>
    public class DotNetDBFManager
    {
        #region 字段和属性
        private readonly ILogger<DotNetDBFManager> _logger;
        private readonly ObjectPool<DataTable> _dataTablePool; // 使用对象池来减少内存分配
        private static readonly System.Text.Encoding _defaultEncoding = System.Text.Encoding.GetEncoding("GB2312"); // 默认编码
        private const int _maxBatchSize = 5000; // 批处理大小，避免过大的内存消耗
        #endregion

        #region 构造函数
        /// <summary>
        /// 构造函数，初始化日志记录器和对象池。
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public DotNetDBFManager(ILogger<DotNetDBFManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); // 验证参数非空
            _dataTablePool = new DefaultObjectPool<DataTable>(new DataTablePoolPolicy()); // 初始化对象池
        }

        /// <summary>
        /// 构造函数，使用空日志记录器。
        /// </summary>
        public DotNetDBFManager() : this(NullLogger<DotNetDBFManager>.Instance) { } // 默认使用空日志记录器
        #endregion

        #region 读取方法
        /// <summary>
        /// 从 DBF 文件读取数据到 DataTable。
        /// </summary>
        /// <param name="filePath">DBF 文件路径</param>
        /// <param name="encoding">字符编码，默认为GB2312</param>
        /// <returns>包含 DBF 数据的 DataTable</returns>
        public DataTable ReadDBFToDataTable(string filePath, System.Text.Encoding encoding = null)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath)); // 参数验证
            var dataTable = _dataTablePool.Get(); // 从对象池获取DataTable
            dataTable.Clear(); // 确保DataTable是干净的

            try
            {
                using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read); // 打开文件流
                using var reader = new DBFReader(stream); // 创建DBF读取器
                reader.CharEncoding = encoding ?? _defaultEncoding; // 设置字符编码
                var fields = reader.Fields; // 获取字段定义

                // 创建 DataTable 列
                foreach (var field in fields)
                {
                    dataTable.Columns.Add(field.Name, DotNetDbfUtil.GetFieldTypeFromDBF(field.DataType)); // 添加列
                }

                // 读取数据
                object[] record;
                while ((record = reader.NextRecord()) != null)
                {
                    var dataRow = dataTable.NewRow(); // 创建新行
                    for (int i = 0; i < fields.Length; i++)
                    {
                        dataRow[fields[i].Name] = record[i] ?? DBNull.Value; // 设置字段值，处理空值
                    }
                    dataTable.Rows.Add(dataRow); // 添加行
                }
                _logger.LogDebug($"成功从文件 {filePath} 读取 {dataTable.Rows.Count} 条记录"); // 记录成功日志
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"读取 DBF 文件 {filePath} 时发生错误"); // 记录错误日志
                _dataTablePool.Return(dataTable); // 发生异常时，将DataTable归还对象池
                throw; // 重新抛出异常
            }

            return dataTable; // 返回填充的DataTable
        }

        /// <summary>
        /// 异步从 DBF 文件读取数据到 DataTable。
        /// </summary>
        /// <param name="filePath">DBF 文件路径</param>
        /// <param name="encoding">字符编码，默认为GB2312</param>
        /// <returns>包含 DBF 数据的 DataTable</returns>
        public async Task<DataTable> ReadDBFToDataTableAsync(string filePath, System.Text.Encoding encoding = null)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath)); // 参数验证

            return await Task.Run(() => ReadDBFToDataTable(filePath, encoding)); // 在线程池上异步执行
        }

        #endregion

        #region 写入方法

        #endregion

        #region 辅助方法
        /// <summary>
        /// 释放 DataTable 资源，将其返回对象池。
        /// </summary>
        /// <param name="dataTable">要释放的 DataTable</param>
        public void ReleaseDataTable(DataTable dataTable)
        {
            if (dataTable != null)
            {
                dataTable.Clear(); // 清空数据
                _dataTablePool.Return(dataTable); // 返回池
            }
        }

        /// <summary>
        /// 获取 DBF 文件的字段结构。
        /// </summary>
        /// <param name="filePath">DBF 文件路径</param>
        /// <param name="encoding">字符编码，默认为GB2312</param>
        /// <returns>字段定义数组</returns>
        public DBFField[] GetDBFStructure(string filePath, System.Text.Encoding encoding = null)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath)); // 参数验证

            try
            {
                using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read); // 打开文件流
                using var reader = new DBFReader(stream) { CharEncoding = encoding ?? _defaultEncoding }; // 创建DBF读取器
                return reader.Fields; // 返回字段定义
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取 DBF 文件 {filePath} 结构时发生错误"); // 记录错误日志
                throw; // 重新抛出异常
            }
        }
        #endregion
    }

    #region 内部辅助类
    /// <summary>
    /// DataTable 对象池策略类，用于管理 DataTable 实例的创建和重用。
    /// </summary>
    internal class DataTablePoolPolicy : IPooledObjectPolicy<DataTable>
    {
        /// <summary>
        /// 创建新的 DataTable 实例。
        /// </summary>
        /// <returns>新的 DataTable 实例</returns>
        public DataTable Create() => new(); // 使用 C# 10 的目标类型 new 表达式

        /// <summary>
        /// 重置 DataTable 以便重用。
        /// </summary>
        /// <param name="obj">要重置的 DataTable</param>
        /// <returns>是否可以重用</returns>
        public bool Return(DataTable obj)
        {
            try
            {
                obj.Clear(); // 清除所有行
                obj.Columns.Clear(); // 清除所有列
                return true; // 可以重用
            }
            catch
            {
                return false; // 重置失败，不能重用
            }
        }
    }
    #endregion
}
