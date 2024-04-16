using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 工作流及模板相关操作的控制器类。
    /// </summary>
    public class WorkflowController : PlControllerBase
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public WorkflowController(AccountManager accountManager, IServiceProvider serviceProvider, PowerLmsUserDbContext dbContext, EntityManager entityManager, IMapper mapper)
        {
            _AccountManager = accountManager;
            _ServiceProvider = serviceProvider;
            _DbContext = dbContext;
            _EntityManager = entityManager;
            _Mapper = mapper;
        }

        private AccountManager _AccountManager;
        private IServiceProvider _ServiceProvider;
        private PowerLmsUserDbContext _DbContext;
        private EntityManager _EntityManager;
        private IMapper _Mapper;

        #region 模板表相关

        /// <summary>
        /// 增加新工作流模板节点。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddWorkflowTemplateReturnDto> AddWorkflowTemplate(AddWorkflowTemplateParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddWorkflowTemplateReturnDto();
            model.Item.GenerateNewId();

            _DbContext.WfTemplates.Add(model.Item);
            model.Item.CreateDateTime = OwHelper.WorldNow;
            model.Item.CreateBy = context.User.Id;
            model.Item.OrgId = context.User.OrgId;
            _DbContext.SaveChanges();
            result.Id = model.Item.Id;
            return result;
        }

        /// <summary>
        /// 获取全部工作流模板节点。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。Id,DisplayName,DocTypeCode。不区分大小写。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllWorkflowTemplateReturnDto> GetAllWorkflowTemplate([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllWorkflowTemplateReturnDto();
            var dbSet = _DbContext.WfTemplates.Where(c => c.OrgId == context.User.OrgId);
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            foreach (var item in conditional)
                if (string.Equals(item.Key, nameof(OwWfTemplate.DisplayName), StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.DisplayName.Contains(item.Value));
                }
                else if (string.Equals(item.Key, nameof(OwWfTemplate.DocTypeCode), StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.DocTypeCode.Contains(item.Value));
                }
                else if (string.Equals(item.Key, "Id", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.Id == id);
                }
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 修改工作流模板节点信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的工作流模板节点不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyWorkflowTemplateReturnDto> ModifyWorkflowTemplate(ModifyWorkflowTemplateParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyWorkflowTemplateReturnDto();
            if (!_EntityManager.Modify(model.Items)) return NotFound();
            //foreach (var item in model.Items)
            //{
            //    item.UpdateBy = context.User.Id;
            //    item.UpdateDateTime = OwHelper.WorldNow;
            //}
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 批量删除工作流模板信息。(物理硬删除)
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id中，至少有一个不存在相应实体。</response>  
        [HttpDelete]
        public ActionResult<RemoveWorkflowTemplateReturnDto> RemoveWorkflowTemplate(RemoveWorkflowTemplatePatamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveWorkflowTemplateReturnDto();

            var dbSet = _DbContext.WfTemplates;
            var items = dbSet.Where(c => model.Ids.Contains(c.Id)).ToArray();
            if (items.Length != model.Ids.Count) return BadRequest("指定Id中，至少有一个不存在相应实体。");
            _DbContext.RemoveRange(items);
            _DbContext.SaveChanges();
            return result;
        }
        #endregion 模板表相关

        #region 模板节点表相关

        /// <summary>
        /// 增加新工作流模板节点。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<AddWfTemplateNodeReturnDto> AddWfTemplateNode(AddWfTemplateNodeParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new AddWfTemplateNodeReturnDto();
            model.Item.GenerateNewId();

            _DbContext.WfTemplateNodes.Add(model.Item);
            //model.Item.CreateDateTime = OwHelper.WorldNow;
            //model.Item.CreateBy = context.User.Id;
            //model.Item.OrgId = context.User.OrgId;
            _DbContext.SaveChanges();
            result.Id = model.Item.Id;
            return result;
        }

        /// <summary>
        /// 获取全部工作流模板节点。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。Id,DisplayName,NextId，ParentId，。不区分大小写。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllWfTemplateNodeReturnDto> GetAllWfTemplateNode([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllWfTemplateNodeReturnDto();
            var dbSet = _DbContext.WfTemplateNodes/*.Where(c => c.OrgId == context.User.OrgId)*/;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            foreach (var item in conditional)
                if (string.Equals(item.Key, nameof(OwWfTemplateNode.DisplayName), StringComparison.OrdinalIgnoreCase))
                {
                    coll = coll.Where(c => c.DisplayName.Contains(item.Value));
                }
                else if (string.Equals(item.Key, nameof(OwWfTemplateNode.Id), StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.Id == id);
                }
                else if (string.Equals(item.Key, nameof(OwWfTemplateNode.ParentId), StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.ParentId == id);
                }
                else if (string.Equals(item.Key, nameof(OwWfTemplateNode.NextId), StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(item.Value, out var id))
                        coll = coll.Where(c => c.NextId == id);
                }
            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 修改工作流模板节点信息。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id的工作流模板节点不存在。</response>  
        [HttpPut]
        public ActionResult<ModifyWfTemplateNodeReturnDto> ModifyWfTemplateNode(ModifyWfTemplateNodeParamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new ModifyWfTemplateNodeReturnDto();
            if (!_EntityManager.Modify(model.Items)) return NotFound();
            //foreach (var item in model.Items)
            //{
            //    item.UpdateBy = context.User.Id;
            //    item.UpdateDateTime = OwHelper.WorldNow;
            //}
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 批量删除工作流模板信息。(物理硬删除)
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="404">指定Id中，至少有一个不存在相应实体。</response>  
        [HttpDelete]
        public ActionResult<RemoveWfTemplateNodeReturnDto> RemoveWfTemplateNode(RemoveWfTemplateNodePatamsDto model)
        {
            if (_AccountManager.GetAccountFromToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new RemoveWfTemplateNodeReturnDto();

            var dbSet = _DbContext.WfTemplateNodes;
            var items = dbSet.Where(c => model.Ids.Contains(c.Id)).ToArray();
            if (items.Length != model.Ids.Count) return BadRequest("指定Id中，至少有一个不存在相应实体。");
            _DbContext.RemoveRange(items);
            _DbContext.SaveChanges();
            return result;
        }
        #endregion 模板节点表相关

    }

    #region 模板节点表相关
    /// <summary>
    /// 批量删除工作流模板信息功能参数封装类。
    /// </summary>
    public class RemoveWfTemplateNodePatamsDto : RemoveItemsParamsDtoBase
    {

    }

    /// <summary>
    /// 批量删除工作流模板信息功能返回值封装类。
    /// </summary>
    public class RemoveWfTemplateNodeReturnDto : RemoveItemsReturnDtoBase
    {
    }

    /// <summary>
    /// 修改工作流模板节点功能的返回值封装类。
    /// </summary>
    public class ModifyWfTemplateNodeReturnDto : ModifyReturnDtoBase
    {
    }

    /// <summary>
    /// 修改工作流模板节点功能的参数封装类。
    /// </summary>
    public class ModifyWfTemplateNodeParamsDto : ModifyParamsDtoBase<OwWfTemplateNode>
    {
    }

    /// <summary>
    /// 查询工作流模板节点对象返回值封装类。
    /// </summary>
    public class GetAllWfTemplateNodeReturnDto : PagingReturnDtoBase<OwWfTemplateNode>
    {
    }

    /// <summary>
    /// 增加工作流模板节点对象功能参数封装类。
    /// </summary>
    public class AddWfTemplateNodeParamsDto : AddParamsDtoBase<OwWfTemplateNode>
    {
    }

    /// <summary>
    /// 增加工作流模板节点对象功能返回值封装类。
    /// </summary>
    public class AddWfTemplateNodeReturnDto : AddReturnDtoBase
    {
    }
    #endregion 模板节点表相关

    #region 模板表相关
    /// <summary>
    /// 批量删除工作流模板信息功能参数封装类。
    /// </summary>
    public class RemoveWorkflowTemplatePatamsDto : RemoveItemsParamsDtoBase
    {

    }

    /// <summary>
    /// 批量删除工作流模板信息功能返回值封装类。
    /// </summary>
    public class RemoveWorkflowTemplateReturnDto : RemoveItemsReturnDtoBase
    {
    }

    /// <summary>
    /// 修改工作流模板节点功能的返回值封装类。
    /// </summary>
    public class ModifyWorkflowTemplateReturnDto : ModifyReturnDtoBase
    {
    }

    /// <summary>
    /// 修改工作流模板节点功能的参数封装类。
    /// </summary>
    public class ModifyWorkflowTemplateParamsDto : ModifyParamsDtoBase<OwWfTemplate>
    {
    }

    /// <summary>
    /// 查询工作流模板节点对象返回值封装类。
    /// </summary>
    public class GetAllWorkflowTemplateReturnDto : PagingReturnDtoBase<OwWfTemplate>
    {
    }

    /// <summary>
    /// 增加工作流模板节点对象功能参数封装类。
    /// </summary>
    public class AddWorkflowTemplateParamsDto : AddParamsDtoBase<OwWfTemplate>
    {
    }

    /// <summary>
    /// 增加工作流模板节点对象功能返回值封装类。
    /// </summary>
    public class AddWorkflowTemplateReturnDto : AddReturnDtoBase
    {
    }
    #endregion 模板表相关
}
