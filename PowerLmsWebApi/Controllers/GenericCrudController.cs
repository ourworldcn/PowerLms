using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// ͨ��CRUD���������࣬�ṩ��׼������ɾ�Ĳ鹦�ܡ�
    /// </summary>
    /// <typeparam name="TEntity">ʵ�����ͣ�����̳���GuidKeyObjectBase</typeparam>
    /// <typeparam name="TGetAllReturnDto">��ȡ����ʵ�巵��DTO����</typeparam>
    /// <typeparam name="TAddParamsDto">���ʵ�����DTO����</typeparam>
    /// <typeparam name="TAddReturnDto">���ʵ�巵��DTO����</typeparam>
    /// <typeparam name="TModifyParamsDto">�޸�ʵ�����DTO����</typeparam>
    /// <typeparam name="TModifyReturnDto">�޸�ʵ�巵��DTO����</typeparam>
    /// <typeparam name="TRemoveParamsDto">ɾ��ʵ�����DTO���ͣ�����̳���RemoveParamsDtoBase</typeparam>
    /// <typeparam name="TRemoveReturnDto">ɾ��ʵ�巵��DTO����</typeparam>
    public abstract class GenericCrudController<TEntity, TGetAllReturnDto, TAddParamsDto, TAddReturnDto, TModifyParamsDto, TModifyReturnDto,
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
        /// �˺Ź�����
        /// </summary>
        protected readonly AccountManager _accountManager;
        /// <summary>
        /// �����ṩ��
        /// </summary>
        protected readonly IServiceProvider _serviceProvider;
        /// <summary>
        /// ���ݿ�������
        /// </summary>
        protected readonly PowerLmsUserDbContext _dbContext;
        /// <summary>
        /// AutoMapperӳ����
        /// </summary>
        protected readonly IMapper _mapper;
        /// <summary>
        /// ʵ�������
        /// </summary>
        protected readonly EntityManager _entityManager;
        /// <summary>
        /// ��Ȩ������
        /// </summary>
        protected readonly AuthorizationManager _authorizationManager;
        /// <summary>
        /// ��־��¼��
        /// </summary>
        protected readonly ILogger<GenericCrudController<TEntity, TGetAllReturnDto, TAddParamsDto, TAddReturnDto, TModifyParamsDto, TModifyReturnDto, TRemoveParamsDto, TRemoveReturnDto>> _logger;

        /// <summary>
        /// ���캯��
        /// </summary>
        /// <param name="accountManager">�˺Ź�����</param>
        /// <param name="serviceProvider">�����ṩ��</param>
        /// <param name="dbContext">���ݿ�������</param>
        /// <param name="mapper">AutoMapperӳ����</param>
        /// <param name="entityManager">ʵ�������</param>
        /// <param name="authorizationManager">��Ȩ������</param>
        /// <param name="logger">��־��¼��</param>
        public GenericCrudController(
            AccountManager accountManager,
            IServiceProvider serviceProvider,
            PowerLmsUserDbContext dbContext,
            IMapper mapper,
            EntityManager entityManager,
            AuthorizationManager authorizationManager,
            ILogger<GenericCrudController<TEntity, TGetAllReturnDto, TAddParamsDto, TAddReturnDto, TModifyParamsDto, TModifyReturnDto, TRemoveParamsDto, TRemoveReturnDto>> logger)
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
        /// ��ȡ���ݿ��������е�ʵ�弯�ϡ�
        /// </summary>
        /// <returns>ʵ�弯��</returns>
        protected abstract DbSet<TEntity> GetDbSet();

        /// <summary>
        /// ��ȡָ��������Ȩ�޴��롣
        /// </summary>
        /// <param name="operation">�������ͣ�1=��ѯ��2=��ӣ�3=�޸ģ�4=ɾ��</param>
        /// <returns>Ȩ�޴��룬����null��ʾ����ҪȨ�޼��</returns>
        protected abstract string GetPermissionCode(int operation);

        /// <summary>
        /// ������ʵ��ǰ��׼��������
        /// </summary>
        /// <param name="entity">��������ʵ��</param>
        /// <param name="context">�û�������</param>
        protected virtual void PrepareNewEntity(TEntity entity, OwContext context)
        {
            // ���ʵ����ICreatorInfo�����ô����ߺʹ���ʱ��
            if (entity is ICreatorInfo creatorInfo)
            {
                creatorInfo.CreateBy = context.User.Id;
                creatorInfo.CreateDateTime = OwHelper.WorldNow;
            }
        }

        /// <summary>
        /// �޸�ʵ��ǰ����֤��
        /// </summary>
        /// <param name="entity">���޸ĵ�ʵ��</param>
        /// <param name="original">ԭʼʵ��</param>
        /// <param name="context">�û�������</param>
        /// <returns>�����֤ͨ������true�����򷵻�false</returns>
        protected virtual bool ValidateModify(TEntity entity, TEntity original, OwContext context)
        {
            return true;
        }

        /// <summary>
        /// ɾ��ʵ��ǰ����֤��
        /// </summary>
        /// <param name="entity">��ɾ����ʵ��</param>
        /// <param name="context">�û�������</param>
        /// <returns>�����֤ͨ������true�����򷵻�false</returns>
        protected virtual bool ValidateDelete(TEntity entity, OwContext context)
        {
            return true;
        }

        /// <summary>
        /// �Ӳ�������ȡʵ�塣
        /// </summary>
        /// <param name="parameters">����DTO</param>
        /// <returns>ʵ�����</returns>
        protected abstract TEntity ExtractEntityFromAddParams(TAddParamsDto parameters);

        /// <summary>
        /// �Ӳ�������ȡʵ�塣
        /// </summary>
        /// <param name="parameters">����DTO</param>
        /// <returns>ʵ�����</returns>
        protected abstract TEntity ExtractEntityFromModifyParams(TModifyParamsDto parameters);

        /// <summary>
        /// ���÷���DTO�е�Id��
        /// </summary>
        /// <param name="returnDto">����DTO</param>
        /// <param name="id">ʵ��Id</param>
        protected abstract void SetReturnDtoId(TAddReturnDto returnDto, Guid id);

        /// <summary>
        /// ��ȡʵ���б�
        /// </summary>
        /// <param name="model">��ҳ����</param>
        /// <param name="conditional">��ѯ����</param>
        /// <returns>ʵ���б�</returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode��</response>  
        /// <response code="401">��Ч���ơ�</response>  
        /// <response code="403">Ȩ�޲��㡣</response>  
        [HttpGet]
        public virtual ActionResult<TGetAllReturnDto> GetAll([FromQuery] PagingParamsDtoBase model, [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_accountManager.GetOrLoadContextByToken(model.Token, _serviceProvider) is not OwContext context)
                return Unauthorized();

            // Ȩ�޼��
            string permissionCode = GetPermissionCode(1); // ��ѯ����
            if (permissionCode != null)
            {
                string errorMessage;
                if (!_authorizationManager.Demand(out errorMessage, permissionCode))
                    return StatusCode((int)HttpStatusCode.Forbidden, errorMessage);
            }

            var result = new TGetAllReturnDto();
            var dbSet = GetDbSet();

            try
            {
                // ������ѯ
                var query = dbSet.AsQueryable();
                if (conditional != null)
                {
                    query = EfHelper.GenerateWhereAnd(query, conditional);
                }

                // Ӧ������
                query = query.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();

                // Ӧ�ö���Ĳ�ѯ����
                query = ApplyAdditionalQueryFilters(query, context);

                // ִ�в�ѯ����ҳ
                var pagingResult = _entityManager.GetAll(query, model.StartIndex, model.Count);
                _mapper.Map(pagingResult, result);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "��ȡ{EntityType}�б�ʱ�����쳣", typeof(TEntity).Name);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"��ȡ����ʱ�����쳣: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// Ӧ�ö���Ĳ�ѯ�����������������д�˷���������ض��Ĺ����߼���
        /// </summary>
        /// <param name="query">ԭʼ��ѯ</param>
        /// <param name="context">�û�������</param>
        /// <returns>�޸ĺ�Ĳ�ѯ</returns>
        protected virtual IQueryable<TEntity> ApplyAdditionalQueryFilters(IQueryable<TEntity> query, OwContext context)
        {
            return query;
        }

        /// <summary>
        /// �����ʵ�塣
        /// </summary>
        /// <param name="model">��Ӳ���</param>
        /// <returns>��ӽ��</returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode��</response>  
        /// <response code="400">�����������</response>  
        /// <response code="401">��Ч���ơ�</response>  
        /// <response code="403">Ȩ�޲��㡣</response>  
        [HttpPost]
        public virtual ActionResult<TAddReturnDto> Add(TAddParamsDto model)
        {
            if (_accountManager.GetOrLoadContextByToken(model.Token, _serviceProvider) is not OwContext context)
            {
                _logger.LogWarning("��Ч������{token}", model.Token);
                return Unauthorized();
            }

            // Ȩ�޼��
            string permissionCode = GetPermissionCode(2); // ��Ӳ���
            if (permissionCode != null)
            {
                string errorMessage;
                if (!_authorizationManager.Demand(out errorMessage, permissionCode))
                    return StatusCode((int)HttpStatusCode.Forbidden, errorMessage);
            }

            var result = new TAddReturnDto();

            try
            {
                // �Ӳ�������ȡʵ��
                var entity = ExtractEntityFromAddParams(model);

                // ������ID
                entity.GenerateNewId();

                // ����ʵ��׼������
                PrepareNewEntity(entity, context);

                // ��ӵ����ݿ�
                GetDbSet().Add(entity);
                _dbContext.SaveChanges();

                // ���÷���ֵ
                SetReturnDtoId(result, entity.Id);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "���{EntityType}ʱ�����쳣", typeof(TEntity).Name);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"���ʵ��ʱ�����쳣: {ex.Message}";
                return BadRequest(result);
            }
        }

        /// <summary>
        /// �޸�ʵ�塣
        /// </summary>
        /// <param name="model">�޸Ĳ���</param>
        /// <returns>�޸Ľ��</returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode��</response>  
        /// <response code="400">�����������</response>  
        /// <response code="401">��Ч���ơ�</response>  
        /// <response code="403">Ȩ�޲��㡣</response>  
        /// <response code="404">ָ��Id��ʵ�岻���ڡ�</response>  
        [HttpPut]
        public virtual ActionResult<TModifyReturnDto> Modify(TModifyParamsDto model)
        {
            if (_accountManager.GetOrLoadContextByToken(model.Token, _serviceProvider) is not OwContext context)
                return Unauthorized();

            // Ȩ�޼��
            string permissionCode = GetPermissionCode(3); // �޸Ĳ���
            if (permissionCode != null)
            {
                string errorMessage;
                if (!_authorizationManager.Demand(out errorMessage, permissionCode))
                    return StatusCode((int)HttpStatusCode.Forbidden, errorMessage);
            }

            var result = new TModifyReturnDto();

            try
            {
                // �Ӳ�������ȡʵ��
                var entity = ExtractEntityFromModifyParams(model);

                // ����ԭʼʵ��
                var original = GetDbSet().Find(entity.Id);
                if (original == null)
                    return NotFound($"IDΪ{entity.Id}��ʵ�岻����");

                // ��֤�޸�
                if (!ValidateModify(entity, original, context))
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "�޸���֤ʧ��";
                    return BadRequest(result);
                }

                // ִ���޸�
                if (!_entityManager.Modify(new[] { entity }))
                    return NotFound();

                // �������
                _dbContext.SaveChanges();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "�޸�{EntityType}ʱ�����쳣", typeof(TEntity).Name);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"�޸�ʵ��ʱ�����쳣: {ex.Message}";
                return BadRequest(result);
            }
        }

        /// <summary>
        /// ɾ��ʵ�塣
        /// </summary>
        /// <param name="model">ɾ������</param>
        /// <returns>ɾ�����</returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode��</response>  
        /// <response code="400">������������ʵ�岻����ɾ����</response>  
        /// <response code="401">��Ч���ơ�</response>  
        /// <response code="403">Ȩ�޲��㡣</response>  
        /// <response code="404">ָ��Id��ʵ�岻���ڡ�</response>  
        [HttpDelete]
        public virtual ActionResult<TRemoveReturnDto> Remove(TRemoveParamsDto model)
        {
            if (_accountManager.GetOrLoadContextByToken(model.Token, _serviceProvider) is not OwContext context)
                return Unauthorized();

            // Ȩ�޼��
            string permissionCode = GetPermissionCode(4); // ɾ������
            if (permissionCode != null)
            {
                string errorMessage;
                if (!_authorizationManager.Demand(out errorMessage, permissionCode))
                    return StatusCode((int)HttpStatusCode.Forbidden, errorMessage);
            }

            var result = new TRemoveReturnDto();

            try
            {
                // ����ʵ��
                var entity = GetDbSet().Find(model.Id);
                if (entity == null)
                    return NotFound($"IDΪ{model.Id}��ʵ�岻����");

                // ��֤ɾ��
                if (!ValidateDelete(entity, context))
                {
                    result.HasError = true;
                    result.ErrorCode = 400;
                    result.DebugMessage = "ʵ�嵱ǰ״̬������ɾ��";
                    return BadRequest(result);
                }

                // ִ��ɾ��
                _entityManager.Remove(entity);
                _dbContext.SaveChanges();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ɾ��{EntityType}ʱ�����쳣", typeof(TEntity).Name);
                result.HasError = true;
                result.ErrorCode = 500;
                result.DebugMessage = $"ɾ��ʵ��ʱ�����쳣: {ex.Message}";
                return BadRequest(result);
            }
        }
    }

    /*
    /// <summary>
    /// �򻯰��ͨ��CRUD���������࣬ʹ����ͬ��ʵ�����Ͳ�����
    /// </summary>
    /// <typeparam name="TEntity">ʵ������</typeparam>
    /// <typeparam name="TEntityDto">ʵ��DTO����</typeparam>
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
        /// ���캯��
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
        /// ��DTOת��Ϊʵ��
        /// </summary>
        /// <param name="dto">DTO����</param>
        /// <returns>ʵ�����</returns>
        protected abstract TEntity ConvertDtoToEntity(TEntityDto dto);

        /// <summary>
        /// �Ӳ�������ȡʵ��
        /// </summary>
        /// <param name="parameters">����DTO</param>
        /// <returns>ʵ�����</returns>
        protected override TEntity ExtractEntityFromAddParams(SimpleAddParamsDto<TEntityDto> parameters)
        {
            return ConvertDtoToEntity(parameters.Item);
        }

        /// <summary>
        /// �Ӳ�������ȡʵ��
        /// </summary>
        /// <param name="parameters">����DTO</param>
        /// <returns>ʵ�����</returns>
        protected override TEntity ExtractEntityFromModifyParams(SimpleModifyParamsDto<TEntityDto> parameters)
        {
            return ConvertDtoToEntity(parameters.Item);
        }

        /// <summary>
        /// ���÷���DTO�е�Id
        /// </summary>
        /// <param name="returnDto">����DTO</param>
        /// <param name="id">ʵ��Id</param>
        protected override void SetReturnDtoId(SimpleAddReturnDto returnDto, Guid id)
        {
            returnDto.Id = id;
        }
    }

    #region ��DTO��

    /// <summary>
    /// �򻯰����Ӳ���DTO��
    /// </summary>
    /// <typeparam name="T">ʵ��DTO����</typeparam>
    public class SimpleAddParamsDto<T> : TokenDtoBase where T : class
    {
        /// <summary>
        /// ʵ������
        /// </summary>
        public T Item { get; set; }
    }

    /// <summary>
    /// �򻯰����ӷ���DTO��
    /// </summary>
    public class SimpleAddReturnDto : ReturnDtoBase
    {
        /// <summary>
        /// ����ʵ���ID
        /// </summary>
        public Guid Id { get; set; }
    }

    /// <summary>
    /// �򻯰���޸Ĳ���DTO��
    /// </summary>
    /// <typeparam name="T">ʵ��DTO����</typeparam>
    public class SimpleModifyParamsDto<T> : TokenDtoBase where T : class
    {
        /// <summary>
        /// ʵ������
        /// </summary>
        public T Item { get; set; }
    }

    /// <summary>
    /// �򻯰�ķ���DTO��
    /// </summary>
    public class SimpleReturnDto : ReturnDtoBase
    {
    }
    #endregion

    // ��ʵ��ʾ��
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

    protected override string GetPermissionCode(int operation)
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
