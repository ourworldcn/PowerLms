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
    #endregion 业务总表

    /// <summary>
    /// 按复杂的多表条件返回费用功能的返回值封装类。
    /// </summary>
    public class GetDocFeeReturnDto
    {
        /// <summary>
        /// 集合元素的最大总数量。
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// 返回的集合。
        /// </summary>
        public List<DocFee> Result { get; set; } = new List<DocFee>();
    }

    /// <summary>
    /// 按复杂的多表条件返回费用功能的参数封装类。
    /// </summary>
    public class GetDocFeeParamsDto : PagingParamsDtoBase
    {
    }

    /// <summary>
    /// 审核单笔费用功能参数封装类。
    /// </summary>
    public class AuditDocFeeParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 要审核的费用Id。
        /// </summary>
        public Guid FeeId { get; set; }

        /// <summary>
        /// 审核标志，true审核完成，false取消审核完成。
        /// </summary>
        public bool IsAudit { get; set; }
    }

    /// <summary>
    /// 审核单笔费用功能返回值封装类。
    /// </summary>
    public class AuditDocFeeReturnDto : ReturnDtoBase
    {
    }

    #region 业务单的账单
    /// <summary>
    /// 标记删除业务单的账单功能的参数封装类。
    /// </summary>
    public class RemoveDocBillParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除业务单的账单功能的返回值封装类。
    /// </summary>
    public class RemoveDocBillReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 根据业务Id，获取相关账单对象功能的参数封装类。
    /// </summary>
    public class GetDocBillsByJobIdParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 业务Id的集合。
        /// </summary>
        public List<Guid> Ids { get; set; } = new List<Guid>();
    }

    /// <summary>
    /// 根据业务Id，获取相关账单对象功能的返回值封装类内的元素类型。
    /// </summary>
    public class GetDocBillsByJobIdItemDto
    {
        /// <summary>
        /// 业务Id。
        /// </summary>
        public Guid JobId { get; set; }

        /// <summary>
        /// 相关的账单。
        /// </summary>
        public List<DocBill> Bills { get; set; } = new List<DocBill>();
    }

    /// <summary>
    /// 根据业务Id，获取相关账单对象功能的返回值封装类。
    /// </summary>
    public class GetDocBillsByJobIdReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 根据业务Id，获取相关账单对象功能的返回值内的元素类型。
        /// </summary>
        /// <summary>
        /// 返回的账单。
        /// </summary>
        public List<GetDocBillsByJobIdItemDto> Result { get; set; } = new List<GetDocBillsByJobIdItemDto>();
    }

    /// <summary>
    /// 获取所有业务单的账单功能的返回值封装类。
    /// </summary>
    public class GetAllDocBillReturnDto : PagingReturnDtoBase<DocBill>
    {
    }

    /// <summary>
    /// 增加新业务单的账单功能参数封装类。
    /// </summary>
    public class AddDocBillParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新业务单的账单信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public DocBill DocBill { get; set; }

        /// <summary>
        /// 绑定的费用Id集合。
        /// </summary>
        public List<Guid> FeeIds { get; set; } = new List<Guid>();
    }

    /// <summary>
    /// 增加新业务单的账单功能返回值封装类。
    /// </summary>
    public class AddDocBillReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新业务单的账单的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改业务单的账单信息功能参数封装类。
    /// </summary>
    public class ModifyDocBillParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 业务单的账单数据。
        /// </summary>
        public DocBill DocBill { get; set; }

        /// <summary>
        /// 账单绑定的费用Id集合，不在该集合的费用对象将不再绑定到账单上。
        /// </summary>
        public List<Guid> FeeIds { get; set; } = new List<Guid>();
    }

    /// <summary>
    /// 修改业务单的账单信息功能返回值封装类。
    /// </summary>
    public class ModifyDocBillReturnDto : ReturnDtoBase
    {
    }
    #endregion 业务单的账单

    #region 业务单的费用单
    /// <summary>
    /// 标记删除业务单的费用单功能的参数封装类。
    /// </summary>
    public class RemoveDocFeeParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除业务单的费用单功能的返回值封装类。
    /// </summary>
    public class RemoveDocFeeReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有业务单的费用单功能的返回值封装类。
    /// </summary>
    public class GetAllDocFeeReturnDto : PagingReturnDtoBase<DocFee>
    {
    }

    /// <summary>
    /// 增加新业务单的费用单功能参数封装类。
    /// </summary>
    public class AddDocFeeParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新业务单的费用单信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public DocFee DocFee { get; set; }
    }

    /// <summary>
    /// 增加新业务单的费用单功能返回值封装类。
    /// </summary>
    public class AddDocFeeReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新业务单的费用单的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改业务单的费用单信息功能参数封装类。
    /// </summary>
    public class ModifyDocFeeParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 业务单的费用单数据。
        /// </summary>
        public DocFee DocFee { get; set; }
    }

    /// <summary>
    /// 修改业务单的费用单信息功能返回值封装类。
    /// </summary>
    public class ModifyDocFeeReturnDto : ReturnDtoBase
    {
    }
    #endregion 业务单的费用单

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

    #region 空运出口单
    /// <summary>
    /// 标记删除空运出口单功能的参数封装类。
    /// </summary>
    public class RemovePlEaDocParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除空运出口单功能的返回值封装类。
    /// </summary>
    public class RemovePlEaDocReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有空运出口单功能的返回值封装类。
    /// </summary>
    public class GetAllPlEaDocReturnDto : PagingReturnDtoBase<PlEaDoc>
    {
    }

    /// <summary>
    /// 增加新空运出口单功能参数封装类。
    /// </summary>
    public class AddPlEaDocParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新空运出口单信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public PlEaDoc PlEaDoc { get; set; }
    }

    /// <summary>
    /// 增加新空运出口单功能返回值封装类。
    /// </summary>
    public class AddPlEaDocReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新空运出口单的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改空运出口单信息功能参数封装类。
    /// </summary>
    public class ModifyPlEaDocParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 空运出口单数据。
        /// </summary>
        public PlEaDoc PlEaDoc { get; set; }
    }

    /// <summary>
    /// 修改空运出口单信息功能返回值封装类。
    /// </summary>
    public class ModifyPlEaDocReturnDto : ReturnDtoBase
    {
    }
    #endregion 空运出口单

    #region 空运进口单相关

    /// <summary>
    /// 标记删除空运进口单功能的参数封装类。
    /// </summary>
    public class RemovePlIaDocParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>
    /// 标记删除空运进口单功能的返回值封装类。
    /// </summary>
    public class RemovePlIaDocReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>
    /// 获取所有空运进口单功能的返回值封装类。
    /// </summary>
    public class GetAllPlIaDocReturnDto : PagingReturnDtoBase<PlIaDoc>
    {
    }

    /// <summary>
    /// 增加新空运进口单功能参数封装类。
    /// </summary>
    public class AddPlIaDocParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 新空运进口单信息。其中Id可以是任何值，返回时会指定新值。
        /// </summary>
        public PlIaDoc PlIaDoc { get; set; }
    }

    /// <summary>
    /// 增加新空运进口单功能返回值封装类。
    /// </summary>
    public class AddPlIaDocReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 如果成功添加，这里返回新空运进口单的Id。
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 修改空运进口单信息功能参数封装类。
    /// </summary>
    public class ModifyPlIaDocParamsDto : TokenDtoBase
    {
        /// <summary>
        /// 空运进口单数据。
        /// </summary>
        public PlIaDoc PlIaDoc { get; set; }
    }

    /// <summary>
    /// 修改空运进口单信息功能返回值封装类。
    /// </summary>
    public class ModifyPlIaDocReturnDto : ReturnDtoBase
    {
    }
    #endregion  空运进口单相关

}
