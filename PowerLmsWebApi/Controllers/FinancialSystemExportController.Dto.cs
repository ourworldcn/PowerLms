using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers
{
    #region DTO定义

    /// <summary>
    /// 发票导出为DBF文件的请求参数。
    /// </summary>
    public class ExportInvoiceToDbfParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 导出条件字典，键为字段名，值为条件值。
        /// 用于筛选要导出的发票数据，支持EfHelper.GenerateWhereAnd的所有查询操作。
        /// </summary>
        public Dictionary<string, string> ExportConditions { get; set; } = new();

        /// <summary>
        /// 文件显示名称。
        /// 用于fileService.CreateFile中的displayName参数，如果不指定则使用默认格式。
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 文件备注信息。
        /// 用于fileService.CreateFile中的remark参数，如果不指定则使用默认格式。
        /// </summary>
        public string Remark { get; set; }
    }

    /// <summary>
    /// 发票导出为DBF文件的返回结果。
    /// </summary>
    public class ExportInvoiceToDbfReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 导出任务的唯一标识ID。
        /// 可通过系统的通用任务状态查询接口跟踪进度和获取生成的文件。
        /// </summary>
        public Guid TaskId { get; set; }

        /// <summary>
        /// 任务创建成功的提示消息。
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 预计要处理的发票数量。
        /// </summary>
        public int ExpectedInvoiceCount { get; set; }
    }

    #endregion
}
