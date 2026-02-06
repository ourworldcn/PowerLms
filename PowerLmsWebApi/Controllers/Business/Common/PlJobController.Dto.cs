using PowerLms.Data;
using PowerLmsWebApi.Dto;
using System.ComponentModel.DataAnnotations;

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

    /// <summary>
    /// 复制工作号功能的参数封装类。
    /// </summary>
    public class CopyJobParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要复制的源任务Id。
        /// </summary>
        public Guid SourceJobId { get; set; }

        /// <summary>
        /// 指定忽略的属性名。
        /// 除 Job对象本体属性外，其它实体的属性要在其属性名前加 实体名 并以.分割，如:PlEsDoc.CargoRouteId。
        /// </summary>
        public List<string> IgnorePropertyNames { get; set; } = new List<string>();

        /// <summary>
        /// 强制指定的新值，键是属性名，值字符串化的属性值。
        /// 除 Job对象本体属性外，其它实体的属性要在其属性名前加 实体名 并以.分割，如:PlEsDoc.CargoRouteId。不能给费用对象指定新值。
        /// 支持 DocFee 的属性。
        /// </summary>
        public Dictionary<string, string> NewValues { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// 复制工作号功能的返回值封装类。
    /// </summary>
    public class CopyJobReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 复制的新任务Id。
        /// </summary>
        public Guid Result { get; set; }
    }

    /// <summary>
    /// 验证工作号唯一性功能的参数封装类。
    /// </summary>
    public class ValidateJobNoParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要验证的工作号。
        /// </summary>
        [Required]
        public string JobNo { get; set; }

        /// <summary>
        /// 要排除的工作号ID（用于编辑场景，避免与自身冲突）。
        /// </summary>
        public Guid? ExcludeJobId { get; set; }
    }

    /// <summary>
    /// 验证工作号唯一性功能的返回值封装类。
    /// </summary>
    public class ValidateJobNoReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 工作号是否在当前机构内唯一。
        /// </summary>
        public bool IsUnique { get; set; }

        /// <summary>
        /// 验证结果说明信息。
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 如果工作号重复，这里返回冲突的工作号ID。
        /// </summary>
        public Guid? ConflictJobId { get; set; }
    }
    #endregion 业务总表

    #region 货场出重单
    /// <summary>
    /// 标记删除货场出重单功能的参数封装类。
    /// </summary>
    public class RemoveHuochangChuchongParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除货场出重单功能的返回值封装类。
    /// </summary>
    public class RemoveHuochangChuchongReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有货场出重单功能的返回值封装类。
    /// </summary>
    public class GetAllHuochangChuchongReturnDto : PagingReturnDtoBase<HuochangChuchong>
    {
    }

    /// <summary>
    /// 增加新货场出重单功能参数封装类。
    /// </summary>
    public class AddHuochangChuchongParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新货场出重单信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public HuochangChuchong HuochangChuchong { get; set; }
    }

    /// <summary>
    /// 增加新货场出重单功能返回值封装类。
    /// </summary>
    public class AddHuochangChuchongReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新货场出重单的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改货场出重单信息功能参数封装类。
    /// </summary>
    public class ModifyHuochangChuchongParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 货场出重单数据。
        /// </summary>
        public HuochangChuchong HuochangChuchong { get; set; }
    }

    /// <summary>
    /// 修改货场出重单信息功能返回值封装类。
    /// </summary>
    public class ModifyHuochangChuchongReturnDto : ReturnDtoBase
    {
    }
    #endregion 货场出重单

    #region 账期管理

    /// <summary>
    /// 关闭账期功能的参数封装类。
    /// </summary>
    public class CloseAccountingPeriodParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要关闭的账期，格式YYYYMM。如果不指定，使用机构参数中的当前账期。
        /// </summary>
        public string AccountingPeriod { get; set; }

        /// <summary>
        /// 是否强制关闭，即使存在未审核的工作号也强制关闭。默认false。
        /// </summary>
        public bool ForceClose { get; set; } = false;
    }

    /// <summary>
    /// 关闭账期功能的返回值封装类。
    /// </summary>
    public class CloseAccountingPeriodReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 本次关闭操作影响的工作号数量。
        /// </summary>
        public int AffectedJobCount { get; set; }

        /// <summary>
        /// 关闭后的新账期。
        /// </summary>
        public string NewAccountingPeriod { get; set; }

        /// <summary>
        /// 本次关闭的账期。
        /// </summary>
        public string ClosedPeriod { get; set; }

        /// <summary>
        /// 关闭操作的详细信息。
        /// </summary>
        public string Message { get; set; }
    }

    /// <summary>
    /// 预览账期关闭影响范围功能的参数封装类。
    /// </summary>
    public class PreviewAccountingPeriodCloseParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要预览的账期，格式YYYYMM。如果不指定，使用机构参数中的当前账期。
        /// </summary>
        public string AccountingPeriod { get; set; }
    }

    /// <summary>
    /// 预览账期关闭影响范围功能的返回值封装类。
    /// </summary>
    public class PreviewAccountingPeriodCloseReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 当前账期。
        /// </summary>
        public string CurrentPeriod { get; set; }

        /// <summary>
        /// 可关闭的工作号数量。
        /// </summary>
        public int ClosableJobCount { get; set; }

        /// <summary>
        /// 不可关闭的工作号数量。
        /// </summary>
        public int UnClosableJobCount { get; set; }

        /// <summary>
        /// 可关闭的工作号列表（只返回前20个）。
        /// </summary>
        public List<PlJob> ClosableJobs { get; set; } = new List<PlJob>();

        /// <summary>
        /// 不可关闭的工作号列表（只返回前20个）。
        /// </summary>
        public List<PlJob> UnClosableJobs { get; set; } = new List<PlJob>();

        /// <summary>
        /// 关闭后的新账期。
        /// </summary>
        public string NextPeriod { get; set; }

        /// <summary>
        /// 是否可以执行关闭操作。
        /// </summary>
        public bool CanClose { get; set; }

        /// <summary>
        /// 不能关闭的原因。
        /// </summary>
        public string ReasonCannotClose { get; set; }
    }

    #endregion 账期管理

    #region 账期反关闭

    /// <summary>
    /// 账期反关闭功能的参数封装类。
    /// </summary>
    public class ReopenAccountingPeriodParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要反关闭到的目标账期,格式YYYYMM(如"202507")。
        /// </summary>
        [Required]
        public string TargetAccountingPeriod { get; set; }
        /// <summary>
        /// 是否同时解关该账期的工作号。
        /// </summary>
        public bool IsUncloseJobs { get; set; }
    }

    /// <summary>
    /// 账期反关闭功能的返回值封装类。
    /// </summary>
    public class ReopenAccountingPeriodReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 反关闭前的账期(YYYYMM格式)。
        /// </summary>
        public string OldAccountingPeriod { get; set; }
        /// <summary>
        /// 反关闭后的账期(YYYYMM格式)。
        /// </summary>
        public string NewAccountingPeriod { get; set; }
        /// <summary>
        /// 解关的工作号数量。
        /// </summary>
        public int UnclosedJobCount { get; set; }
        /// <summary>
        /// 操作详细信息。
        /// </summary>
        public string Message { get; set; }
    }

    #endregion 账期反关闭

}


