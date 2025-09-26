using OW.Forum;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers.Forum
{
    /// <summary>获取所有论坛板块功能的返回值封装类。</summary>
    public class GetAllOwForumCategoryReturnDto : PagingReturnDtoBase<OwForumCategory>
    {
    }

    /// <summary>增加新论坛板块功能参数封装类。</summary>
    public class AddOwForumCategoryParamsDto : AddParamsDtoBase<OwForumCategory>
    {
    }

    /// <summary>增加新论坛板块功能返回值封装类。</summary>
    public class AddOwForumCategoryReturnDto : AddReturnDtoBase
    {
    }

    /// <summary>修改论坛板块功能的参数封装类。</summary>
    public class ModifyOwForumCategoryParamsDto : ModifyParamsDtoBase<OwForumCategory>
    {
    }

    /// <summary>修改论坛板块功能的返回值封装类。</summary>
    public class ModifyOwForumCategoryReturnDto : ModifyReturnDtoBase
    {
    }

    /// <summary>删除论坛板块的功能参数封装类。</summary>
    public class RemoveOwForumCategoryParamsDto : RemoveParamsDtoBase
    {
    }

    /// <summary>删除论坛板块的功能返回值封装类。</summary>
    public class RemoveOwForumCategoryReturnDto : RemoveReturnDtoBase
    {
    }

    /// <summary>通过板块Id获取详细信息的功能返回值封装类。</summary>
    public class GetOwForumCategoryByIdReturnDto : ReturnDtoBase
    {
        /// <summary>论坛板块详细信息。</summary>
        public OwForumCategory Result { get; set; }
    }
}