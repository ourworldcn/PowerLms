/*
 * PowerLms - 货运物流业务管理系统
 * 系统初始化服务 - 数据种子处理模块
 * 
 * 功能说明：
 * - 基于JSON流的Excel数据处理
 * - 支持增量数据导入，避免重复插入
 * - 复用OwDataUnit + OwNpoiUnit基础设施组件
 * - 高性能内存管理和批量数据处理
 * 
 * 技术特点：
 * - JSON流转换降低内存分配
 * - PooledList优化大数据集处理
 * - 类型安全的反序列化机制
 * - 统一的错误处理和日志记录
 * 
 * 作者：PowerLms开发团队
 * 创建时间：2024年
 * 最后修改：2024年12月 - 简化为JSON流处理，移除复杂映射逻辑
 */

using Microsoft.EntityFrameworkCore;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using PowerLmsServer.EfData;
using Microsoft.Extensions.Logging;
using OW.Data;
using NPOI;
using System.Text;
using System.Text.Json;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 初始化服务的数据种子处理部分
    /// </summary>
    public partial class InitializerService
    {
        #region Excel数据处理

        /// <summary>
        /// 从Excel文件初始化数据库数据 - 基于JSON流的简化版本
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        /// <returns>是否成功初始化</returns>
        /// <remarks>
        /// 简化的处理流程：
        /// 1. 读取Excel文件
        /// 2. 遍历工作表，使用JSON流转换
        /// 3. 调用基础库批量插入数据库
        /// 4. 记录处理结果和错误信息
        /// </remarks>
        public bool InitializeDataFromExcel(PowerLmsUserDbContext dbContext)
        {
            try
            {
                _logger.LogInformation("开始从Excel文件初始化数据库数据（JSON流优化版本）");
                var excelFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "系统资源", "预初始化数据.xlsx");
                if (!File.Exists(excelFilePath))
                {
                    _logger.LogWarning("Excel初始化文件不存在: {FilePath}", excelFilePath);
                    return false;
                }
                using var fileStream = new FileStream(excelFilePath, FileMode.Open, FileAccess.Read);
                using var workbook = new XSSFWorkbook(fileStream);
                ProcessWorkbookViaJsonStream(workbook, dbContext);
                _logger.LogInformation("Excel数据初始化处理完成");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从Excel文件初始化数据时发生错误");
                return false;
            }
        }

        #endregion

        #region 批量数据处理工具

        /// <summary>
        /// 批量数据处理工具方法 - 使用PooledList优化内存使用
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="source">数据源</param>
        /// <param name="batchSize">批次大小</param>
        /// <param name="processor">批次处理器</param>
        /// <returns>处理结果统计</returns>
        /// <exception cref="ArgumentNullException">当参数为null时抛出</exception>
        /// <exception cref="ArgumentOutOfRangeException">当批次大小无效时抛出</exception>
        public (int TotalProcessed, int BatchCount) ProcessInBatches<T>(IEnumerable<T> source, int batchSize, Action<PooledList<T>> processor)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(processor);
            if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize), "批次大小必须大于0");
            int totalProcessed = 0;
            int batchCount = 0;
            using var currentBatch = new PooledList<T>(batchSize);
            foreach (var item in source)
            {
                currentBatch.Add(item);
                if (currentBatch.Count >= batchSize)
                {
                    processor(currentBatch);
                    totalProcessed += currentBatch.Count;
                    batchCount++;
                    currentBatch.Clear(); // 清空但保留容量，避免重新分配
                }
            }
            if (currentBatch.Count > 0) // 处理最后一个不满批次的数据
            {
                processor(currentBatch);
                totalProcessed += currentBatch.Count;
                batchCount++;
            }
            return (totalProcessed, batchCount);
        }

        #endregion
    }
}