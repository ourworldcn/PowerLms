using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using PowerLmsServer.EfData;
using PowerLmsWebApi.Dto;
using System.ComponentModel.DataAnnotations;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 一般性的Ef操作控制器。
    /// </summary>
    [ApiExplorerSettings(IgnoreApi =true)]
    public class GeneralEfController : PlControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GeneralEfController(PowerLmsUserDbContext dbContext)
        {
            _DbContext = dbContext;
        }

        PowerLmsUserDbContext _DbContext;

        /// <summary>
        /// 通用查询获取一组实体。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">通用条件。</param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<GeneralEfGetAllReturnDto> GetAll([FromQuery] ConditionalQueryParamsDto model, [FromQuery] Dictionary<string, string> conditional = null)
        {
            var result = new GeneralEfGetAllReturnDto();
            var resultColl = _DbContext.WfKindCodeDics.AsNoTrackingWithIdentityResolution().ToArray();
            result.Result.AddRange(resultColl);
            return result;
        }
    }

    /// <summary>
    /// 通用查询条件。
    /// </summary>
    public class ConditionalQueryParamsDto
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public ConditionalQueryParamsDto()
        {

        }

        /// <summary>
        /// 起始位置，从0开始。
        /// </summary>
        [Required, Range(0, int.MaxValue)]
        public int StartIndex { get; set; }

        /// <summary>
        /// 最大返回数量。
        /// 默认值-1，不限定返回数量。
        /// </summary>
        [Range(-1, int.MaxValue)]
        public int Count { get; set; } = -1;

        /// <summary>
        /// 排序的字段名。默认值:"Id"。
        /// </summary>
        public string OrderFieldName { get; set; } = nameof(GuidKeyObjectBase.Id);

        /// <summary>
        /// 是否降序排序：true降序排序，false升序排序（省略或默认）。
        /// </summary>
        public bool IsDesc { get; set; }

        /// <summary>
        /// 实体类型名称，注意不是表名。如: OwWfKindCodeDic
        /// </summary>
        public string EntityTypeName { get; set; }

    }

    /// <summary>
    /// 
    /// </summary>
    public class GeneralEfGetAllReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public GeneralEfGetAllReturnDto()
        {

        }

        /// <summary>
        /// 集合元素的最大总数量。
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// 返回的集合。
        /// </summary>
        public List<object> Result { get; set; } = new List<object>();
    }

}
