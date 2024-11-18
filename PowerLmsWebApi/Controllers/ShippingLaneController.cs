using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 航线管理控制器。
    /// </summary>
    public class ShippingLaneController : PlControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public ShippingLaneController(IServiceProvider serviceProvider, AccountManager accountManager, PowerLmsUserDbContext dbContext, OrganizationManager organizationManager, EntityManager entityManager, IMapper mapper, NpoiManager npoiManager)
        {
            _ServiceProvider = serviceProvider;
            _AccountManager = accountManager;
            _DbContext = dbContext;
            _OrganizationManager = organizationManager;
            _EntityManager = entityManager;
            _Mapper = mapper;
            _NpoiManager = npoiManager;
        }
        IServiceProvider _ServiceProvider;
        AccountManager _AccountManager;

        readonly PowerLmsUserDbContext _DbContext;
        OrganizationManager _OrganizationManager;

        EntityManager _EntityManager;
        IMapper _Mapper;
        private readonly NpoiManager _NpoiManager;

        /// <summary>
        /// 增加新航线费用。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddShippingLaneReturnDto> AddShippingLane(AddShippingLaneParamsDto model)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddShippingLaneReturnDto();
            model.Item.GenerateNewId();

            _DbContext.ShippingLanes.Add(model.Item);
            model.Item.CreateDateTime = OwHelper.WorldNow;
            model.Item.CreateBy = context.User.Id;
            model.Item.OrgId = context.User.OrgId;
            model.Item.UpdateBy = context.User.Id;
            model.Item.UpdateDateTime = OwHelper.WorldNow;
            _DbContext.SaveChanges();
            result.Id = model.Item.Id;
            return result;
        }

        /// <summary>
        /// 获取全部航线费用。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。实体属性名不区分大小写。
        /// 通用条件写法:所有条件都是字符串，对区间的写法是用逗号分隔（字符串类型暂时不支持区间且都是模糊查询）如"2024-1-1,2024-1-2"。
        /// 对强制取null的约束，则写"null"。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllShippingLaneReturnDto> GetAllShippingLane([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllShippingLaneReturnDto();
            var dbSet = _DbContext.ShippingLanes.Where(c => c.OrgId == context.User.OrgId);
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            coll = EfHelper.GenerateWhereAnd(coll, conditional);
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 修改航线费用信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的航线费用不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyShippingLaneReturnDto> ModifyShippingLane(ModifyShippingLaneParamsDto model)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyShippingLaneReturnDto();
            if (!_EntityManager.Modify(model.Items)) return NotFound();
            foreach (var item in model.Items)
            {
                item.UpdateBy = context.User.Id;
                item.UpdateDateTime = OwHelper.WorldNow;
            }
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 批量删除航线信息。(物理硬删除)
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id中，至少有一个不存在相应实体。</response>  
        [HttpDelete]
        public ActionResult<RemoveShippingLaneReturnDto> RemoveShippingLane(RemoveShippingLanePatamsDto model)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveShippingLaneReturnDto();

            var dbSet = _DbContext.ShippingLanes;
            var items = dbSet.Where(c => model.Ids.Contains(c.Id)).ToArray();
            if (items.Length != model.Ids.Count) return BadRequest("指定Id中，至少有一个不存在相应实体。");
            _DbContext.RemoveRange(items);
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 导入空运数据。
        /// </summary>
        /// <param name="file"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult<ImportShippingLaneReturnDto> ImportShippingLane(IFormFile file, Guid token)
        {
            if (_AccountManager.GetOrLoadAccountFromToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ImportShippingLaneReturnDto();
            var workbook = _NpoiManager.GetWorkbookFromStream(file.OpenReadStream());
            var sheet = workbook.GetSheetAt(0);
            var jostr = _NpoiManager.GetJson(sheet, 2);
            var jsonOptions = new JsonSerializerOptions { NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals };
            jsonOptions.Converters.Add(new NullableDecimalConvert { });
            var collSrc = JsonSerializer.Deserialize<IEnumerable<ShippingLaneEto>>(jostr, jsonOptions);
            var collDest = _Mapper.Map<IEnumerable<ShippingLane>>(collSrc);
            collDest.SafeForEach(c =>
            {
                c.UpdateBy = context?.User.Id;
                c.CreateBy = context?.User.Id;
                c.CreateDateTime = OwHelper.WorldNow;
                c.OrgId = context?.User.OrgId;
            });
            _DbContext.ShippingLanes.AddRange(collDest);
            _DbContext.SaveChanges();
            return result;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class NullableDecimalConvert : JsonConverter<decimal?>
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="typeToConvert"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();
            return decimal.TryParse(str, out var deci) ? deci : null;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="options"></param>
        public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
        {
            if (value is null) writer.WriteNullValue();
            else writer.WriteNumberValue(value.Value);
        }
    }
    /// <summary>
    /// 航线运价数据类Excel导入时的转换封装类。
    /// </summary>
    [AutoMap(typeof(ShippingLane), ReverseMap = true)]
    public class ShippingLaneEto : GuidKeyObjectBase
    {
        /// <summary>
        /// 启运港编码。
        /// </summary>
        [Comment("启运港编码")]
        [Column(TypeName = "varchar"), MaxLength(32), Required(AllowEmptyStrings = false)]   //最多32个ASCII字符
        [JsonPropertyName("起运港")]
        public virtual string StartCode { get; set; }

        /// <summary>
        /// 目的港编码。
        /// </summary>
        [Comment("目的港编码")]
        [Column(TypeName = "varchar"), MaxLength(32), Required(AllowEmptyStrings = false)]   //最多32个ASCII字符
        [JsonPropertyName("目的港")]
        public virtual string EndCode { get; set; }

        /// <summary>
        /// 航空公司。
        /// </summary>
        [Comment("航空公司")]
        [MaxLength(64)]
        [JsonPropertyName("航空公司")]
        public string Shipper { get; set; }

        /// <summary>
        /// 航班周期。
        /// </summary>
        [Comment("航班周期")]
        [MaxLength(64)]
        [JsonPropertyName("航班周期")]
        public string VesslRate { get; set; }

        /// <summary>
        /// 到达时长。单位:天。
        /// </summary>
        [Comment("到达时长。单位:天。")]
        [JsonPropertyName("到达天数")]
        public decimal? ArrivalTimeInDay { get; set; }

        /// <summary>
        /// 包装规范。
        /// </summary>
        [Comment("包装规范")]
        [MaxLength(32)]
        [JsonPropertyName("包装规范")]
        public string Packing { get; set; }

        /// <summary>
        /// KGS M.
        /// </summary>
        [Comment("KGS M"), Precision(18, 4)]
        [JsonPropertyName("KGSm")]
        public decimal? KgsM { get; set; }

        /// <summary>
        /// KGS N.
        /// </summary>
        [Comment("KGS N"), Precision(18, 4)]
        [JsonPropertyName("KGSN")]
        public decimal? KgsN { get; set; }

        /// <summary>
        /// KGS45.
        /// </summary>
        [Comment("KGS45"), Precision(18, 4)]
        [JsonPropertyName("KGS45")]
        public decimal? A45 { get; set; }

        /// <summary>
        /// KGS100.
        /// </summary>
        [Comment("KGS100"), Precision(18, 4)]
        [JsonPropertyName("KGS100")]
        public decimal? A100 { get; set; }

        /// <summary>
        /// KGS300.
        /// </summary>
        [Comment("KGS300"), Precision(18, 4)]
        [JsonPropertyName("KGS300")]
        public decimal? A300 { get; set; }

        /// <summary>
        /// KGS500.
        /// </summary>
        [Comment("KGS500"), Precision(18, 4)]
        [JsonPropertyName("KGS500")]
        public decimal? A500 { get; set; }

        /// <summary>
        /// KGS1000.
        /// </summary>
        [Comment("KGS1000"), Precision(18, 4)]
        [JsonPropertyName("KGS1000")]
        public decimal? A1000 { get; set; }

        /// <summary>
        /// KGS2000.
        /// </summary>
        [Comment("KGS2000"), Precision(18, 4)]
        [JsonPropertyName("KGS2000")]
        public decimal? A2000 { get; set; }

        /// <summary>
        /// 生效日期。
        /// </summary>
        [Comment("生效日期")]
        [Column(TypeName = "datetime2(2)")]
        [JsonPropertyName("生效日期")]
        public DateTime? StartDateTime { get; set; }

        /// <summary>
        /// 终止日期。
        /// </summary>
        [Comment("终止日期")]
        [Column(TypeName = "datetime2(2)")]
        [JsonPropertyName("失效日期")]
        public DateTime? EndDateTime { get; set; }

        /// <summary>
        /// 备注。
        /// </summary>
        [Comment("备注")]
        [MaxLength(128)]
        [JsonPropertyName("备注")]
        public string Remark { get; set; }

        /// <summary>
        /// 联系人。
        /// </summary>
        [Comment("联系人。")]
        [MaxLength(64)]
        [JsonPropertyName("订舱人联系方式")]
        public string Contact { get; set; }
    }

    /// <summary>
    /// 导入空运数据的返回值数据封装类。
    /// </summary>
    public class ImportShippingLaneReturnDto
    {
    }

    /// <summary>
    /// 批量删除航线信息功能参数封装类。
    /// </summary>
    public class RemoveShippingLanePatamsDto : RemoveItemsParamsDtoBase
    {

    }

    /// <summary>
    /// 批量删除航线信息功能返回值封装类。
    /// </summary>
    public class RemoveShippingLaneReturnDto : RemoveItemsReturnDtoBase
    {
    }

    /// <summary>
    /// 修改航线费用功能的返回值封装类。
    /// </summary>
    public class ModifyShippingLaneReturnDto : ModifyReturnDtoBase
    {
    }

    /// <summary>
    /// 修改航线费用功能的参数封装类。
    /// </summary>
    public class ModifyShippingLaneParamsDto : ModifyParamsDtoBase<ShippingLane>
    {
    }

    /// <summary>
    /// 查询航线费用对象返回值封装类。
    /// </summary>
    public class GetAllShippingLaneReturnDto : PagingReturnDtoBase<ShippingLane>
    {
    }

    /// <summary>
    /// 增加航线费用对象功能参数封装类。
    /// </summary>
    public class AddShippingLaneParamsDto : AddParamsDtoBase<ShippingLane>
    {
    }

    /// <summary>
    /// 增加航线费用对象功能返回值封装类。
    /// </summary>
    public class AddShippingLaneReturnDto : AddReturnDtoBase
    {
    }
}
