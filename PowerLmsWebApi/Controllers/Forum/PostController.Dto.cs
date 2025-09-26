using OW.Forum;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers.Forum
{
    /// <summary>获取所有论坛帖子功能的返回值封装类。</summary>
    public class GetAllOwPostReturnDto : PagingReturnDtoBase<OwPost>
    {
    }

    /// <summary>增加新论坛帖子功能参数封装类。</summary>
    public class AddOwPostParamsDto : AddParamsDtoBase<OwPost>
    {
    }

    /// <summary>增加新论坛帖子功能返回值封装类。</summary>
    public class AddOwPostReturnDto : AddReturnDtoBase
    {
    }

    /// <summary>修改论坛帖子功能的参数封装类。</summary>
    public class ModifyOwPostParamsDto : ModifyParamsDtoBase<OwPost>
    {
    }

    /// <summary>修改论坛帖子功能的返回值封装类。</summary>
    public class ModifyOwPostReturnDto : ModifyReturnDtoBase
    {
    }

    /// <summary>删除论坛帖子的功能参数封装类。</summary>
    public class RemoveOwPostParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>删除论坛帖子的功能返回值封装类。</summary>
    public class RemoveOwPostReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>通过帖子Id获取详细信息的功能返回值封装类。</summary>
    public class GetOwPostByIdReturnDto : ReturnDtoBase
    {
        /// <summary>论坛帖子详细信息。</summary>
        public OwPost Result { get; set; }
    }

    /// <summary>设置帖子置顶状态的功能参数封装类。</summary>
    public class SetOwPostTopParamsDto : TokenDtoBase
    {
        /// <summary>帖子Id。</summary>
        public Guid PostId { get; set; }

        /// <summary>是否置顶。</summary>
        public bool IsTop { get; set; }
    }

    /// <summary>设置帖子置顶状态的功能返回值封装类。</summary>
    public class SetOwPostTopReturnDto : ReturnDtoBase
    {
    }
}