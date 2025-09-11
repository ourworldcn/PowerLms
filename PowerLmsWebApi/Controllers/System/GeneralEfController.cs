using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// 通用CRUD控制器基类，提供标准化的增删改查功能。
    /// </summary>
    /// <typeparam name="TEntity">实体类型，必须继承自GuidKeyObjectBase</typeparam>
    /// <typeparam name="TGetAllReturnDto">获取所有实体返回DTO类型</typeparam>
    /// <typeparam name="TAddParamsDto">添加实体参数DTO类型</typeparam>
    /// <typeparam name="TAddReturnDto">添加实体返回DTO类型</typeparam>
    /// <typeparam name="TModifyParamsDto">修改实体参数DTO类型</typeparam>
    /// <typeparam name="TModifyReturnDto">修改实体返回DTO类型</typeparam>
    /// <typeparam name="TRemoveParamsDto">删除实体参数DTO类型，必须继承自RemoveParamsDtoBase</typeparam>
    /// <typeparam name="TRemoveReturnDto">删除实体返回DTO类型</typeparam>
    public abstract class GenericEfController<TEntity, TGetAllReturnDto, TAddParamsDto, TAddReturnDto, TModifyParamsDto, TModifyReturnDto,
        TRemoveParamsDto, TRemoveReturnDto> : PlControllerBase
        where TEntity : GuidKeyObjectBase
        where TGetAllReturnDto : PagingReturnDtoBase<TEntity>, new()
        where TAddParamsDto : AddParamsDtoBase<TEntity>, new()
        where TAddReturnDto : AddReturnDtoBase, new()
        where TModifyParamsDto : ModifyParamsDtoBase<TEntity>, new()
        where TModifyReturnDto : ModifyReturnDtoBase, new()
        where TRemoveParamsDto : RemoveParamsDtoBase, new()
        where TRemoveReturnDto : RemoveReturnDtoBase, new()
    {
        /// <summary>
        /// 账号管理器
        /// </summary>
        protected readonly AccountManager _accountManager;
        /// <summary>
        /// 服务提供者
        /// </summary>
        protected readonly IServiceProvider _serviceProvider;
        /// <summary>
        /// 数据库上下文
        /// </summary>
        protected readonly PowerLmsUserDbContext _dbContext;
        /// <summary>
        /// AutoMapper映射器
        /// </summary>
        protected readonly IMapper _mapper;
        /// <summary>
        /// 实体管理器
        /// </summary>
        protected readonly EntityManager _entityManager;
        /// <summary>
        /// 授权管理器
        /// </summary>
        protected readonly AuthorizationManager _authorizationManager;
        /// <summary>
        /// 日志记录器
        /// </summary>
        protected readonly ILogger<GenericEfController<TEntity, TGetAllReturnDto, TAddParamsDto, TAddReturnDto, TModifyParamsDto, TModifyReturnDto, TRemoveParamsDto, TRemoveReturnDto>> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="accountManager">账号管理器</param>
        /// <param name="serviceProvider">服务提供者</param>
        /// <param name="dbContext">数据库上下文</param>
        /// <param name="mapper">AutoMapper映射器</param>
        /// <param name="entityManager">实体管理器</param>
        /// <param name="authorizationManager">授权管理器</param>
        /// <param name="logger">日志记录器</param>
        public GenericEfController(
            AccountManager accountManager,
            IServiceProvider serviceProvider,
            PowerLmsUserDbContext dbContext,
            IMapper mapper,
            EntityManager entityManager,
            AuthorizationManager authorizationManager,
            ILogger<GenericEfController<TEntity, TGetAllReturnDto, TAddParamsDto, TAddReturnDto, TModifyParamsDto, TModifyReturnDto, TRemoveParamsDto, TRemoveReturnDto>> logger)
        {
            _accountManager = accountManager;
            _serviceProvider = serviceProvider;
            _dbContext = dbContext;
            _mapper = mapper;
            _entityManager = entityManager;
            _authorizationManager = authorizationManager;
            _logger = logger;
        }

        /// <summary>
        /// 获取数据库上下文中的实体集合。
        /// </summary>
        /// <returns>实体集合</returns>
        protected abstract DbSet<TEntity> GetDbSet();

        /// <summary>
        /// 获取指定操作的权限代码。
        /// </summary>
        /// <param name="operation">操作类型：1=查询，2=添加，3=修改，4=删除</param>
        /// <returns>权限代码，返回null表示不需要权限检查</returns>
        protected abstract string GetPermissionCode(int operation);

        /// <summary>
        /// 创建新实体前的准备工作。
        /// </summary>
        /// <param name="entity">待创建的实体</param>
        /// <param name="context">用户上下文</param>
        protected virtual void PrepareNewEntity(TEntity entity, OwContext context)
        {
            // 如果实体是ICreatorInfo，设置创建者和创建时间
            if (entity is ICreatorInfo creatorInfo)
            {
                creatorInfo.CreateBy = context.User.Id;
                creatorInfo.CreateDateTime = OwHelper.WorldNow;
            }
        }

        /// <summary>
        /// 修改实体前的验证。
        /// </summary>
        /// <param name="entity">待修改的实体</param>
        /// <param name="original">原始实体</param>
        /// <param name="context">用户上下文</param>
        /// <returns>如果验证通过返回true，否则返回false</returns>
        protected virtual bool ValidateModify(TEntity entity, TEntity original, OwContext context)
        {
            return true;
        }

        /// <summary>
        /// 删除实体前的验证。
        /// </summary>
        /// <param name="entity">待删除的实体</param>
        /// <param name="context">用户上下文</param>
        /// <returns>如果验证通过返回true，否则返回false</returns>
        protected virtual bool ValidateDelete(TEntity entity, OwContext context)
        {
            return true;
        }

        /// <summary>
        /// 从添加参数中提取实体。
        /// </summary>
        /// <param name="parameters">参数DTO</param>
        /// <returns>实体对象</returns>
        protected virtual TEntity ExtractEntityFromAddParams(TAddParamsDto parameters)
        {
            // 使用 Item 而不是 Entity
            return parameters.Item;
        }

        /// <summary>
        /// 从修改参数中提取实体集合。
        /// </summary>
        /// <param name="parameters">参数DTO</param>
        /// <returns>实体集合</returns>
        protected virtual IEnumerable<TEntity> ExtractEntitiesFromModifyParams(TModifyParamsDto parameters)
        {
            // 直接返回 Items 集合
            return parameters.Items;
        }

        /// <summary>
        /// 设置返回DTO中的Id。
        /// </summary>
        /// <param name="returnDto">返回DTO</param>
        /// <param name="id">实体Id</param>
        protected virtual void SetReturnDtoId(TAddReturnDto returnDto, Guid id)
        {
            returnDto.Id = id;
        }

        /// <summary>
        /// 获取实体列表。
        /// </summary>
        /// <param name="model">分页参数</param>
        /// <param name="conditional">查询条件</param>
        /// <returns>实体列表</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpGet]
        public virtual ActionResult<TGetAllReturnDto> GetAll([FromQuery] PagingParamsDtoBase model, [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_accountManager.GetOrLoadContextByToken(model.Token, _serviceProvider) is not OwContext context)
                return Unauthorized();

            // 权限检查
            string permissionCode = GetPermissionCode(1); // 查询操作
            if (permissionCode != null)
            {
                if (!_authorizationManager.Demand(out string errorMessage, permissionCode))
                    return StatusCode((int)HttpStatusCode.Forbidden, errorMessage);
            }

            var result = new TGetAllReturnDto();
            var dbSet = GetDbSet();

            try
            {
                // 构建查询
                var query = dbSet.AsQueryable();
                if (conditional != null)
                {
                    query = EfHelper.GenerateWhereAnd(query, conditional);
                }

                // 应用排序
                query = query.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();

                // 应用额外的查询条件
                query = ApplyAdditionalQueryFilters(query, context);

                // 执行查询并分页
                var pagingResult = _entityManager.GetAll(query, model.StartIndex, model.Count);
                _mapper.Map(pagingResult, result);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取{EntityType}列表时发生异常", typeof(TEntity).Name);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"获取数据时发生异常: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// 应用额外的查询过滤条件。子类可重写此方法以添加特定的过滤逻辑。
        /// </summary>
        /// <param name="query">原始查询</param>
        /// <param name="context">用户上下文</param>
        /// <returns>修改后的查询</returns>
        protected virtual IQueryable<TEntity> ApplyAdditionalQueryFilters(IQueryable<TEntity> query, OwContext context)
        {
            return query;
        }

        /// <summary>
        /// 添加新实体。
        /// </summary>
        /// <param name="model">添加参数</param>
        /// <returns>添加结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>  
        /// <response code="400">请求参数错误。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public virtual ActionResult<TAddReturnDto> Add(TAddParamsDto model)
        {
            if (_accountManager.GetOrLoadContextByToken(model.Token, _serviceProvider) is not OwContext context)
            {
                _logger.LogWarning("无效的令牌{token}", model.Token);
                return Unauthorized();
            }

            // 权限检查
            string permissionCode = GetPermissionCode(2); // 添加操作
            if (permissionCode != null)
            {
                if (!_authorizationManager.Demand(out string errorMessage, permissionCode))
                    return StatusCode((int)HttpStatusCode.Forbidden, errorMessage);
            }

            var result = new TAddReturnDto();

            try
            {
                // 验证添加参数
                if (model.Item == null)
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "添加参数不能为空";
                    return BadRequest(result);
                }

                // 从参数中提取实体
                var entity = ExtractEntityFromAddParams(model);

                // 生成新ID
                entity.GenerateNewId();

                // 进行实体准备工作
                PrepareNewEntity(entity, context);

                // 添加到数据库
                GetDbSet().Add(entity);
                _dbContext.SaveChanges();

                // 设置返回值
                SetReturnDtoId(result, entity.Id);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加{EntityType}时发生异常", typeof(TEntity).Name);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"添加实体时发生异常: {ex.Message}";
                return BadRequest(result);
            }
        }

        /// <summary>
        /// 修改实体。
        /// </summary>
        /// <param name="model">修改参数</param>
        /// <returns>修改结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>  
        /// <response code="400">请求参数错误。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的实体不存在。</response>  
        [HttpPut]
        public virtual ActionResult<TModifyReturnDto> Modify(TModifyParamsDto model)
        {
            if (_accountManager.GetOrLoadContextByToken(model.Token, _serviceProvider) is not OwContext context)
                return Unauthorized();

            // 权限检查
            string permissionCode = GetPermissionCode(3); // 修改操作
            if (permissionCode != null)
            {
                if (!_authorizationManager.Demand(out string errorMessage, permissionCode))
                    return StatusCode((int)HttpStatusCode.Forbidden, errorMessage);
            }

            var result = new TModifyReturnDto();

            try
            {
                // 验证要修改的项
                if (model.Items == null || model.Items.Count == 0)
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "没有提供要修改的实体";
                    return BadRequest(result);
                }

                // 获取要修改的实体集合
                var entities = ExtractEntitiesFromModifyParams(model);

                // 验证每个实体
                foreach (var entity in entities)
                {
                    // 查找原始实体
                    var original = GetDbSet().Find(entity.Id);
                    if (original == null)
                    {
                        result.HasError = true;
                        result.ErrorCode = 404;
                        result.DebugMessage = $"ID为{entity.Id}的实体不存在";
                        return NotFound(result);
                    }

                    // 验证修改
                    if (!ValidateModify(entity, original, context))
                    {
                        result.HasError = true;
                        result.ErrorCode = 400;
                        result.DebugMessage = $"实体(ID={entity.Id})修改验证失败";
                        return BadRequest(result);
                    }
                }

                // 批量修改
                if (!_entityManager.Modify(entities.ToList()))
                {
                    result.HasError = true;
                    result.ErrorCode = 500;
                    result.DebugMessage = "修改实体时发生错误";
                    return BadRequest(result);
                }

                // 保存更改
                _dbContext.SaveChanges();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "修改{EntityType}时发生异常", typeof(TEntity).Name);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"修改实体时发生异常: {ex.Message}";
                return BadRequest(result);
            }
        }

        /// <summary>
        /// 删除实体。
        /// </summary>
        /// <param name="model">删除参数</param>
        /// <returns>删除结果</returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode。</response>  
        /// <response code="400">请求参数错误或实体不允许删除。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        /// <response code="404">指定Id的实体不存在。</response>  
        [HttpDelete]
        public virtual ActionResult<TRemoveReturnDto> Remove(TRemoveParamsDto model)
        {
            if (_accountManager.GetOrLoadContextByToken(model.Token, _serviceProvider) is not OwContext context)
                return Unauthorized();

            // 权限检查
            string permissionCode = GetPermissionCode(4); // 删除操作
            if (permissionCode != null)
            {
                if (!_authorizationManager.Demand(out string errorMessage, permissionCode))
                    return StatusCode((int)HttpStatusCode.Forbidden, errorMessage);
            }

            var result = new TRemoveReturnDto();

            try
            {
                // 查找实体
                var entity = GetDbSet().Find(model.Id);
                if (entity == null)
                    return NotFound($"ID为{model.Id}的实体不存在");

                // 验证删除
                if (!ValidateDelete(entity, context))
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "实体当前状态不允许删除";
                    return BadRequest(result);
                }

                // 执行删除
                _entityManager.Remove(entity);
                _dbContext.SaveChanges();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除{EntityType}时发生异常", typeof(TEntity).Name);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"删除实体时发生异常: {ex.Message}";
                return BadRequest(result);
            }
        }
    }

    /*
    /// <summary>
    /// 简化版的通用CRUD控制器基类，使用相同的实体类型参数。
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TEntityDto">实体DTO类型</typeparam>
    public abstract class SimpleCrudController<TEntity, TEntityDto> : 
        GenericCrudController<TEntity, 
            PagingReturnDtoBase<TEntity>, 
            SimpleAddParamsDto<TEntityDto>, 
            SimpleAddReturnDto, 
            SimpleModifyParamsDto<TEntityDto>, 
            SimpleReturnDto, 
            RemoveParamsDtoBase, 
            SimpleReturnDto>
        where TEntity : GuidKeyObjectBase
        where TEntityDto : class
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        protected SimpleCrudController(
            AccountManager accountManager,
            IServiceProvider serviceProvider,
            PowerLmsUserDbContext dbContext,
            IMapper mapper,
            EntityManager entityManager,
            AuthorizationManager authorizationManager,
            ILogger<SimpleCrudController<TEntity, TEntityDto>> logger)
            : base(accountManager, serviceProvider, dbContext, mapper, entityManager, authorizationManager, 
                  (ILogger<GenericCrudController<TEntity, PagingReturnDtoBase<TEntity>, SimpleAddParamsDto<TEntityDto>, SimpleAddReturnDto, SimpleModifyParamsDto<TEntityDto>, SimpleReturnDto, RemoveParamsDtoBase, SimpleReturnDto>>)logger)
        {
        }

        /// <summary>
        /// 从DTO转换为实体
        /// </summary>
        /// <param name="dto">DTO对象</param>
        /// <returns>实体对象</returns>
        protected abstract TEntity ConvertDtoToEntity(TEntityDto dto);

        /// <summary>
        /// 从参数中提取实体
        /// </summary>
        /// <param name="parameters">参数DTO</param>
        /// <returns>实体对象</returns>
        protected override TEntity ExtractEntityFromAddParams(SimpleAddParamsDto<TEntityDto> parameters)
        {
            return ConvertDtoToEntity(parameters.Item);
        }

        /// <summary>
        /// 从参数中提取实体
        /// </summary>
        /// <param name="parameters">参数DTO</param>
        /// <returns>实体对象</returns>
        protected override TEntity ExtractEntityFromModifyParams(SimpleModifyParamsDto<TEntityDto> parameters)
        {
            return ConvertDtoToEntity(parameters.Item);
        }

        /// <summary>
        /// 设置返回DTO中的Id
        /// </summary>
        /// <param name="returnDto">返回DTO</param>
        /// <param name="id">实体Id</param>
        protected override void SetReturnDtoId(SimpleAddReturnDto returnDto, Guid id)
        {
            returnDto.Id = id;
        }
    }

    #region 简化DTO类

    /// <summary>
    /// 简化版的添加参数DTO类
    /// </summary>
    /// <typeparam name="T">实体DTO类型</typeparam>
    public class SimpleAddParamsDto<T> : TokenDtoBase where T : class
    {
        /// <summary>
        /// 实体数据
        /// </summary>
        public T Item { get; set; }
    }

    /// <summary>
    /// 简化版的添加返回DTO类
    /// </summary>
    public class SimpleAddReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// 新增实体的ID
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 简化版的修改参数DTO类
    /// </summary>
    /// <typeparam name="T">实体DTO类型</typeparam>
    public class SimpleModifyParamsDto<T> : TokenDtoBase where T : class
    {
        /// <summary>
        /// 实体数据
        /// </summary>
        public T Item { get; set; }
    }

    /// <summary>
    /// 简化版的返回DTO类
    /// </summary>
    public class SimpleReturnDto : ReturnDtoBase
    {
    }
    #endregion

    // 简单实现示例
public class CustomerController : SimpleCrudController<PlCustomer, PlCustomerDto>
{
    public CustomerController(
        AccountManager accountManager,
        IServiceProvider serviceProvider,
        PowerLmsUserDbContext dbContext,
        IMapper mapper,
        EntityManager entityManager,
        AuthorizationManager authorizationManager,
        ILogger<CustomerController> logger)
        : base(accountManager, serviceProvider, dbContext, mapper, entityManager, authorizationManager, logger)
    {
    }

    protected override DbSet<PlCustomer> GetDbSet()
    {
        return _dbContext.PlCustomers;
    }

    protected override String GetPermissionCode(int operation)
    {
        return operation switch
        {
            1 => "Customer.View",
            2 => "Customer.Add",
            3 => "Customer.Edit",
            4 => "Customer.Delete",
            _ => null
        };
    }

    protected override PlCustomer ConvertDtoToEntity(PlCustomerDto dto)
    {
        return _mapper.Map<PlCustomer>(dto);
    }
}

    */
}
