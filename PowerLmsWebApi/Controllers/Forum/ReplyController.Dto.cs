using OW.Forum;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers.Forum
{
    /// <summary>获取所有论坛回复功能的返回值封装类。</summary>
    public class GetAllOwReplyReturnDto : PagingReturnDtoBase<OwReply>
    {
    }

    /// <summary>增加新论坛回复功能参数封装类。</summary>
    public class AddOwReplyParamsDto : AddParamsDtoBase<OwReply>
    {
    }

    /// <summary>增加新论坛回复功能返回值封装类。</summary>
    public class AddOwReplyReturnDto : AddReturnDtoBase
    {
    }

    /// <summary>修改论坛回复功能的参数封装类。</summary>
    public class ModifyOwReplyParamsDto : ModifyParamsDtoBase<OwReply>
    {
    }

    /// <summary>修改论坛回复功能的返回值封装类。</summary>
    public class ModifyOwReplyReturnDto : ModifyReturnDtoBase
    {
    }

    /// <summary>删除论坛回复的功能参数封装类。</summary>
    public class RemoveOwReplyParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>删除论坛回复的功能返回值封装类。</summary>
    public class RemoveOwReplyReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>通过回复Id获取详细信息的功能返回值封装类。</summary>
    public class GetOwReplyByIdReturnDto : ReturnDtoBase
    {
        /// <summary>论坛回复详细信息。</summary>
        public OwReply Result { get; set; }
    }
}