using PowerLms.Data;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers
{
    #region 业务总表
    /// <summary>
    /// 切换业务状态接口参数封装类。
    /// </summary>
    public class ChangeStateParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要修改业务的唯一Id。
        /// </summary>
        public Guid JobId { get; set; }

        /// <summary>
        /// 新的业务状态。不改变则为空。
        /// NewJob初始=0，Operating正操作=2，Operated操作完成=4，Checked已审核=8，Closed已关闭=16.
        /// </summary>
        public int? JobState { get; set; }

        /// <summary>
        /// 新的操作状态。不改变则为空。
        /// 具体规则参加各业务表单 Status 字段说明。
        /// </summary>
        public byte? OperateState { get; set; }
    }

    /// <summary>
    /// 切换业务状态接口返回值封装类。
    /// </summary>
    public class ChangeStateReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功这里是切换后的业务状态。
        /// </summary>
        public int JobState { get; set; }

        /// <summary>
        /// 如果成功这里是切换后的操作状态。
        /// </summary>
        public int OperateState { get; set; }
    }

    /// <summary>
    /// 标记删除业务总表功能的参数封装类。
    /// </summary>
    public class RemovePlJobParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除业务总表功能的返回值封装类。
    /// </summary>
    public class RemovePlJobReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有业务总表功能的返回值封装类。
    /// </summary>
    public class GetAllPlJobReturnDto : PagingReturnDtoBase<PlJob>
    {
    }

    /// <summary>
    /// 增加新业务总表功能参数封装类。
    /// </summary>
    public class AddPlJobParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新业务总表信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public PlJob PlJob { get; set; }
    }

    /// <summary>
    /// 增加新业务总表功能返回值封装类。
    /// </summary>
    public class AddPlJobReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新业务总表的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改业务总表信息功能参数封装类。
    /// </summary>
    public class ModifyPlJobParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 业务总表数据。
        /// </summary>
        public PlJob PlJob { get; set; }
    }

    /// <summary>
    /// 修改业务总表信息功能返回值封装类。
    /// </summary>
    public class ModifyPlJobReturnDto : ReturnDtoBase
    {
    }

    /// <summary>
    /// 审核任务及下属所有费用功能参数封装类。
    /// </summary>
    public class AuditJobAndDocFeeParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要审核的任务Id。
        /// </summary>
        public Guid JobId { get; set; }

        /// <summary>
        /// true审核，false标识取消审核。
        /// true时，要审核任务的 JobStata 必须是4时才能调用，成功后 JobStata 自动切换为8。
        /// false时，要取消审核任务的 JobStata 必须是8才能调用，成功后 JobStata 自动切换为4（此时不会更改下属费用的状态）。
        /// </summary>
        public bool IsAudit { get; set; }
    }

    /// <summary>
    /// 审核任务及下属所有费用功能返回值封装类。
    /// </summary>
    public class AuditJobAndDocFeeReturnDto : ReturnDtoBase
    {
    }

    #endregion 业务总表

}
