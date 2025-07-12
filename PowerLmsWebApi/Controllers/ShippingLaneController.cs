using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Net;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 航价管理控制器。
    /// </summary>
    public class ShippingLaneController : PlControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public ShippingLaneController(IServiceProvider serviceProvider, AccountManager accountManager, PowerLmsUserDbContext dbContext, OrgManager<PowerLmsUserDbContext> orgManager, EntityManager entityManager, IMapper mapper, NpoiManager npoiManager, AuthorizationManager authorizationManager)
        {
            _ServiceProvider = serviceProvider;
            _AccountManager = accountManager;
            _DbContext = dbContext;
            _OrgManager = orgManager;
            _EntityManager = entityManager;
            _Mapper = mapper;
            _NpoiManager = npoiManager;
            _AuthorizationManager = authorizationManager;
        }
        
        IServiceProvider _ServiceProvider;
        AccountManager _AccountManager;
        readonly PowerLmsUserDbContext _DbContext;
        OrgManager<PowerLmsUserDbContext> _OrgManager;
        EntityManager _EntityManager;
        IMapper _Mapper;
        private readonly NpoiManager _NpoiManager;
        readonly AuthorizationManager _AuthorizationManager;
        
        /// <summary>
        /// 添加新航线方案。
        /// </summary>
        [HttpPost]
        public ActionResult<AddShippingLaneReturnDto> AddShippingLane(AddShippingLaneParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "A.1.1")) return StatusCode((int)HttpStatusCode.Forbidden, err);
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
        /// 获取全部航线方案。
        /// </summary>
        [HttpGet]
        public ActionResult<GetAllShippingLaneReturnDto> GetAllShippingLane([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllShippingLaneReturnDto();
            var dbSet = _DbContext.ShippingLanes.Where(c => c.OrgId == context.User.OrgId);
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            coll = EfHelper.GenerateWhereAnd(coll, conditional);
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 修改航线方案信息。
        /// </summary>
        [HttpPut]
        public ActionResult<ModifyShippingLaneReturnDto> ModifyShippingLane(ModifyShippingLaneParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "A.1.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
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
        /// 标记删除航线信息。(这是硬删除)
        /// </summary>
        [HttpDelete]
        public ActionResult<RemoveShippingLaneReturnDto> RemoveShippingLane(RemoveShippingLanePatamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "A.1.2")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RemoveShippingLaneReturnDto();

            var dbSet = _DbContext.ShippingLanes;
            var items = dbSet.Where(c => model.Ids.Contains(c.Id)).ToArray();
            if (items.Length != model.Ids.Count) return BadRequest("指定Id中，至少有一个不对应实体。");
            _DbContext.RemoveRange(items);
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 导入航线数据。
        /// </summary>
        [HttpPost]
        public ActionResult<ImportShippingLaneReturnDto> ImportShippingLane(IFormFile file, Guid token)
        {
            if (_AccountManager.GetOrLoadContextByToken(token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "A.1.4")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ImportShippingLaneReturnDto();
            var workbook = _NpoiManager.GetWorkbookFromStream(file.OpenReadStream());
            var sheet = workbook.GetSheetAt(0);
            var jostr = _NpoiManager.GetJson(sheet, 2);
            var jsonOptions = new JsonSerializerOptions { NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals };
            jsonOptions.Converters.Add(new NullableDecimalConvert());
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
}