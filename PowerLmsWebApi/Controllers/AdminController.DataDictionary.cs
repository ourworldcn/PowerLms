using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NPOI.HSSF.UserModel;
using PowerLms.Data;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.Net;

namespace PowerLmsWebApi.Controllers
{
    /// <summary>
    /// ����Ա���ܿ�������
    /// </summary>
    public partial class AdminController : PlControllerBase
    {
        #region �����ֵ����
        #region �ֵ�Ŀ¼

        /// <summary>
        /// ��ȡ���������ֵ��Ŀ¼�б�
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">֧��ͨ�ò�ѯ��</param>
        /// <returns></returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode ��</response>  
        /// <response code="401">��Ч���ơ�</response>  
        [HttpGet]
        public ActionResult<GetAllDataDicCatalogReturnDto> GetAllDataDicCatalog([FromQuery] PagingParamsDtoBase model, [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllDataDicCatalogReturnDto();

            var dbSet = _DbContext.DD_DataDicCatalogs;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            if (_AccountManager.IsAdmin(context.User))  //���ǳ���
                coll = coll.Where(c => c.OrgId == null);
            else
            {
                var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                if (!merchantId.HasValue) return BadRequest("δ֪���̻�Id");
                if (context.User.OrgId is null) //��û��ָ������
                {
                    coll = coll.Where(c => c.OrgId == merchantId);
                }
                else
                {
                    coll = coll.Where(c => c.OrgId == context.User.OrgId);
                }
            }

            // ʹ��EfHelper.GenerateWhereAnd����ͨ�ò�ѯ��������
            coll = EfHelper.GenerateWhereAnd(coll, conditional);

            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// �޸������ֵ�Ŀ¼��
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode ��</response>  
        /// <response code="400">ָ��ʵ���Id�����ڡ�ͨ������Bug.�ڼ�������¿����ǲ������⡣</response>  
        /// <response code="401">��Ч���ơ�</response>  
        /// <response code="403">Ȩ�޲��㡣</response>  
        [HttpPut]
        public ActionResult<ModifyDataDicCatalogReturnDto> ModifyDataDicCatalog(ModifyDataDicCatalogParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out var err, "B.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ModifyDataDicCatalogReturnDto();
            if (!_EntityManager.Modify(model.Items))
            {
                var errResult = new StatusCodeResult(OwHelper.GetLastError()) { };
                return errResult;
            }
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// ɾ��һ�������ֵ�Ŀ¼����ɾ�������ݡ�
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode ��</response>  
        /// <response code="400">ָ��ʵ���Id�����ڡ�ͨ������Bug.�ڼ�������¿����ǲ������⡣</response>  
        /// <response code="401">��Ч���ơ�</response>  
        /// <response code="403">Ȩ�޲��㡣</response>  
        [HttpDelete]
        public ActionResult<RemoveDataDicCatalogReturnDto> RemoveDataDicCatalog(RemoveDataDicCatalogParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RemoveDataDicCatalogReturnDto();
            var id = model.Id;
            var item = _DbContext.DD_DataDicCatalogs.Find(id);
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }

        #endregion �ֵ�Ŀ¼

        #region �����ֵ���ز���

        /// <summary>
        /// ���Ƽ������ֵ䡣
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode ��</response>  
        /// <response code="401">��Ч���ơ�</response> 
        /// <response code="403">Ȩ�޲��㡣</response>  
        [HttpPost]
        public ActionResult<CopySimpleDataDicReturnDto> CopySimpleDataDic(CopySimpleDataDicParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);

            var result = new CopySimpleDataDicReturnDto();
            //var merch = _DbContext.Merchants.Find(model.SrcOrgId);
            //if (merch == null) return NotFound();
            #region ���Ƽ��ֵ�
            var baseCatalogs = _DbContext.DD_DataDicCatalogs.Where(c => c.OrgId == model.SrcOrgId).AsNoTracking();  //�����ֵ�Ŀ¼����
            foreach (var catalog in baseCatalogs)
            {
                if (model.CatalogCodes.Contains(catalog.Code))
                    _DataManager.CopyTo(catalog, model.DestOrgId);
            }
            //_DataManager.CopyAllSpecialDataDicBase(model.Id);
            #endregion ���Ƽ��ֵ�

            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// ��ȡϵͳ��Դ�б�
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<GetSystemResourceReturnDto> GetSystemResource()
        {
            var result = new GetSystemResourceReturnDto();
            result.Resources.AddRange(_DbContext.DD_SystemResources.AsNoTracking());
            return result;
        }

        /// <summary>
        /// ͨ�õĵ��������ֵ䡣�൱���������ٵ��롣
        /// </summary>
        /// <param name="formFile"></param>
        /// <param name="token"></param>
        /// <param name="rId">����Դ�б��л�ȡ��ָ����Դ��Id����:6AE3BBB3-BAC9-4509-BF82-C8578830CD24 ��ʾ ��������Դ��Id�ǲ���仯�ġ�</param>
        /// <returns></returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode ��</response>  
        /// <response code="401">��Ч���ơ�</response>  
        /// <response code="403">Ȩ�޲��㡣</response>  
        [HttpPost]
        public ActionResult<ImportDataDicReturnDto> ImportDataDic(IFormFile formFile, Guid token, Guid rId)
        {
            if (_AccountManager.GetOrLoadContextByToken(token, _ServiceProvider) is not OwContext context) return Unauthorized(OwHelper.GetLastErrorMessage());
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ImportDataDicReturnDto();
            var srTask = _DbContext.DD_SystemResources.FindAsync(rId).AsTask();
            var workbook = _NpoiManager.GetWorkbookFromStream(formFile.OpenReadStream());
            var sheet = workbook.GetSheetAt(0);
            var sr = srTask.Result;
            switch (sr.Name)
            {
                case nameof(_DbContext.Multilinguals):
                    {
                        _DbContext.TruncateTable(nameof(_DbContext.Multilinguals));
                        _NpoiManager.WriteToDb(sheet, _DbContext, _DbContext.Multilinguals);
                        _DbContext.SaveChanges();
                    }
                    break;
                default:
                    result.ErrorCode = 400;
                    break;
            }
            return result;
        }

        /// <summary>
        /// ͨ�û�ȡ�����ֵ���ܡ�
        /// </summary>
        /// <param name="token"></param>
        /// <param name="rId"></param>
        /// <returns></returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode ��</response>  
        /// <response code="401">��Ч���ơ�</response>  
        [HttpGet]
        public ActionResult ExportDataDic(Guid token, Guid rId)
        {
            if (_AccountManager.GetOrLoadContextByToken(token, _ServiceProvider) is not OwContext context) return Unauthorized(OwHelper.GetLastErrorMessage());
            var srTask = _DbContext.DD_SystemResources.FindAsync(rId).AsTask();
            var sr = srTask.Result;
            using var workbook = new HSSFWorkbook();
            var sheet = workbook.CreateSheet("Sheet0");//����һ������ΪSheet0�ı�  
            var fileName = $"{sr.Name}.xls";
            switch (sr.Name)
            {
                case nameof(_DbContext.Multilinguals):
                    {
                        _NpoiManager.WriteToExcel(_DbContext.Multilinguals.AsNoTracking(), typeof(Multilingual).GetProperties().Select(c => c.Name).ToArray(), sheet);
                    }
                    break;
                default:
                    break;
            }
            var stream = new MemoryStream();
            workbook.Write(stream, true);
            workbook.Close();
            stream.Seek(0, SeekOrigin.Begin);
            return File(stream, "application/octet-stream", fileName);
            //var path = Path.Combine(AppContext.BaseDirectory, "ϵͳ��Դ", "ϵͳ��Դ.xlsx");
            //stream = new FileStream(path, FileMode.Open);
            //return new PhysicalFileResult(path, "application/octet-stream") { FileDownloadName = Path.GetFileName(path) };
        }

        /// <summary>
        /// ����ģ�塣
        /// </summary>
        /// <param name="token"></param>
        /// <param name="rId"></param>
        /// <returns></returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode ��</response>  
        /// <response code="401">��Ч���ơ�</response>  
        [HttpGet]
        public ActionResult ExportDataDicTemplate(Guid token, Guid rId)
        {
            if (_AccountManager.GetOrLoadContextByToken(token, _ServiceProvider) is not OwContext context) return Unauthorized(OwHelper.GetLastErrorMessage());
            var srTask = _DbContext.DD_SystemResources.FindAsync(rId).AsTask();
            var sr = srTask.Result;
            using var workbook = new HSSFWorkbook();
            var sheet = workbook.CreateSheet("Sheet0");//����һ������ΪSheet0�ı�  
            var fileName = $"{sr.Name}.xls";
            switch (sr.Name)
            {
                case nameof(_DbContext.Multilinguals):
                    {
                        _NpoiManager.WriteToExcel(_DbContext.Multilinguals.Take(0), typeof(Multilingual).GetProperties().Select(c => c.Name).ToArray(), sheet);
                    }
                    break;
                default:
                    break;
            }
            var stream = new MemoryStream();
            workbook.Write(stream, true);
            workbook.Close();
            stream.Seek(0, SeekOrigin.Begin);
            return File(stream, "application/octet-stream", fileName);
        }

        #endregion �����ֵ���ز���

        #region ���ֵ��CRUD

        /// <summary>
        /// ��ȡָ����������ֵ��ȫ�����ݡ�
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">֧��ͨ�ò�ѯ������ </param>
        /// <returns></returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode ��</response>  
        /// <response code="400">ָ�����Id��Ч��</response>  
        /// <response code="401">��Ч���ơ�</response>  
        [HttpGet]
        public ActionResult<GetAllDataDicReturnDto> GetAllDataDic([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllDataDicReturnDto();
            var dbSet = _DbContext.DD_SimpleDataDics;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();

            // ʹ��EfHelper.GenerateWhereAnd����ͨ�ò�ѯ��������
            coll = EfHelper.GenerateWhereAnd(coll, conditional);

            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// �������ֵ�Ŀ¼���ȡ�����ֵ������û�о����̻��޷�ʹ�ã�������о����̻��������û��ſ�ʹ�á�
        /// ����Լ��ֵ䡣
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode ��</response>  
        /// <response code="400">δ֪���̻�Id��</response>  
        /// <response code="401">��Ч���ơ�</response>  
        /// <response code="404">�Ҳ���ָ��Ŀ¼Code���ֵ䡣</response>  
        [HttpGet]
        public ActionResult<GetDataDicByCatalogCodeReturnDto> GetDataDicByCatalogCode([FromQuery] GetDataDicByCatalogCodeParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetDataDicByCatalogCodeReturnDto();
            var dbSet = _DbContext.DD_DataDicCatalogs;
            var coll = dbSet.AsNoTracking();
            if (_AccountManager.IsAdmin(context.User))  //���ǳ���
                coll = coll.Where(c => c.OrgId == null);
            else
            {
                var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                if (!merchantId.HasValue) return BadRequest("δ֪���̻�Id");
                if (context.User.OrgId is null) //��û��ָ������
                {
                    coll = coll.Where(c => c.OrgId == merchantId);
                }
                else
                {
                    coll = coll.Where(c => c.OrgId == context.User.OrgId);
                }
            }

            var catalog = coll.FirstOrDefault(c => c.Code == model.SimpleDicCatalogCode);
            if (catalog is null)
            {
                return NotFound("�Ҳ���ָ��Ŀ¼Code���ֵ䡣");
            }
            var dataDics = _DbContext.DD_SimpleDataDics.Where(c => c.DataDicId == catalog.Id).AsNoTracking();
            result.Result.AddRange(dataDics);

            return result;
        }

        /// <summary>
        /// ����һ�������ֵ�(Ŀ¼)������CopyToChildren��ֵ�����ڳ��ܶ�������һ��ȫ���ֵ�Ŀ¼(OrgIdΪ��)�������̹ܶ�������һ���̻����ֵ�Ŀ¼(OrgIdΪ�̻�Id)��
        /// ֻ�е���ѡ�˸���ѡ��(CopyToChildren=true)ʱ���ŻὫ�ֵ�Ŀ¼���Ƶ���˾�ͻ��������ܻḴ�Ƶ����й�˾�ͻ������̹ܻḴ�Ƶ����̻������й�˾�ͻ�����
        /// ���ǵ���������״�ṹ������ѭ�����Ĳ㼶��ϵ���и��ơ�
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode ��</response>  
        /// <response code="302">��Ҫ����ԱȨ�ޡ�</response>  
        /// <response code="400">δ֪���̻�Id��</response>  
        /// <response code="401">��Ч���ơ�</response>  
        [HttpPost]
        public ActionResult<AddDataDicCatalogReturnDto> AddDataDicCatalog(AddDataDicCatalogParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();

            if (!context.User.IsSuperAdmin) //���ǳ���
            {
                string err;
                if (!_AuthorizationManager.Demand(out err, "B.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);
                if (!context.User.IsAdmin()) return StatusCode((int)HttpStatusCode.Forbidden, "��Ҫ����ԱȨ�ޡ�");
            }

            // �����û�����������ȷ��OrgId
            Guid? targetOrgId = null;
            if (context.User.IsSuperAdmin)
            {
                targetOrgId = null; // ����ʹ��ȫ��Ŀ¼
            }
            else if (context.User.IsMerchantAdmin)
            {
                var merchId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                if (!merchId.HasValue)
                    return BadRequest("�޷���ȡ�̻�ID");
                targetOrgId = merchId;
            }
            else if (context.User.OrgId.HasValue)
            {
                targetOrgId = context.User.OrgId;
            }

            // ���ͬһ�������Ƿ��Ѵ�����ͬCode��Ŀ¼
            if (_DbContext.DD_DataDicCatalogs.Any(c => c.OrgId == targetOrgId && c.Code == model.Item.Code))
                return BadRequest($"�Ѵ�����ͬCode��Ŀ¼: {model.Item.Code}");

            // �������ֵ�Ŀ¼��ʹ��AutoMapper��������
            var mainCatalog = _Mapper.Map<DataDicCatalog>(model.Item);
            mainCatalog.OrgId = targetOrgId;
            mainCatalog.GenerateNewId();

            _DbContext.DD_DataDicCatalogs.Add(mainCatalog);

            // ֻ�й�ѡ�˸���ѡ��ʱ�Ÿ��Ƶ���˾�ͻ���
            if (model.CopyToChildren)
            {
                try
                {
                    // ��ȡĿ��������� - ʹ�� ToList() ȷ����ѯִ��
                    var targetOrgs = new List<PlOrganization>();

                    if (context.User.IsSuperAdmin)
                    {
                        // ���ܣ���ȡ���й�˾�ͻ���
                        targetOrgs = _DbContext.PlOrganizations
                            .Where(o => o.Otc == 2)
                            .AsNoTracking()
                            .ToList();
                    }
                    else if (context.User.IsMerchantAdmin && targetOrgId.HasValue)
                    {
                        // �̹ܣ���ȡ�����̻������й�˾�ͻ���
                        var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                        if (!merchantId.HasValue) return BadRequest("�޷���ȡ�̻�ID");

                        var dictOrgs = _OrgManager.GetOrLoadOrgCacheItem(merchantId.Value).Orgs;
                        var allowOrgObjs = dictOrgs.Values.ToArray();
                        if (allowOrgObjs != null)
                        {
                            targetOrgs = allowOrgObjs
                                .Where(o => o.Otc == 2)
                                .ToList();
                        }
                    }

                    // ��ȡ�Ѵ�����ͬCode��Ŀ¼�Ļ���Id
                    var existingCatalogOrgIds = _DbContext.DD_DataDicCatalogs
                        .Where(c => c.Code == model.Item.Code)
                        .AsNoTracking()
                        .Select(c => c.OrgId)
                        .ToList();

                    var existingIdSet = new HashSet<Guid?>(existingCatalogOrgIds);

                    // ����ÿ��Ŀ�����
                    foreach (var org in targetOrgs)
                    {
                        // �����Ѵ�����ͬCode�Ļ���
                        if (existingIdSet.Contains(org.Id))
                            continue;

                        // ������Ŀ¼ - ֻʹ��DataDicCatalogʵ��ӵ�е�����
                        var newCatalog = new DataDicCatalog
                        {
                            Code = mainCatalog.Code,
                            DisplayName = mainCatalog.DisplayName,
                            OrgId = org.Id
                        };

                        _DbContext.DD_DataDicCatalogs.Add(newCatalog);
                        existingIdSet.Add(org.Id);
                    }
                }
                catch (Exception ex)
                {
                    // ���񲢼�¼�쳣������Ȼ������Ŀ¼
                    var log = new OwSystemLog
                    {
                        ExtraString = $"�����ֵ�Ŀ¼���ӻ���ʱ����: {ex.Message}",
                        ActionId = "DataDic.AddDataDicCatalog.CopyToChildren",
                        WorldDateTime = DateTime.Now,
                        OrgId = context.User.OrgId,
                    };

                    // �����Ҫ�洢�����Ķ�ջ���٣�����ʹ��JsonObjectString����
                    if (!string.IsNullOrEmpty(ex.StackTrace))
                    {
                        log.JsonObjectString = ex.StackTrace;
                    }

                    _DbContext.OwSystemLogs.Add(log);
                }
            }

            _DbContext.SaveChanges();
            return new AddDataDicCatalogReturnDto { Id = mainCatalog.Id };
        }

        /// <summary>
        /// ��ָ���������ֵ�����һ�
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode ��</response>  
        /// <response code="400">��ͬһ���ͬһ��֯������ָ�����ظ���Code��</response>  
        /// <response code="401">��Ч���ơ�</response>  
        /// <response code="403">Ȩ�޲��㡣</response>  
        [HttpPost]
        public ActionResult<AddSimpleDataDicReturnDto> AddSimpleDataDic(AddSimpleDataDicParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new AddSimpleDataDicReturnDto();
            if (_DbContext.DD_SimpleDataDics.Any(c => c.DataDicId == model.Item.DataDicId && c.Code == model.Item.Code))   //���ظ�
                return BadRequest("Id�ظ�");
            if (model.Item.DataDicId is null)
                return BadRequest($"{nameof(model.Item.DataDicId)} ����Ϊ�ա�");
            model.Item.GenerateNewId();
            var id = model.Item.Id;
            _DbContext.DD_SimpleDataDics.Add(model.Item);
            if (model.CopyToChildren)    //����Ҫ���´���
            {
                if (_DbContext.DD_DataDicCatalogs.FirstOrDefault(c => c.Id == model.Item.DataDicId) is DataDicCatalog catalog)  //�����ֵ�
                {
                    var allCatalog = _DbContext.DD_DataDicCatalogs.Where(c => c.Code == catalog.Code && c.Id != model.Item.DataDicId);  //�ų��������ֵ�
                    foreach (var item in allCatalog)
                    {
                        var sdd = (SimpleDataDic)model.Item.Clone();
                        sdd.DataDicId = item.Id;
                        _DbContext.DD_SimpleDataDics.Add(sdd);
                    }
                }
            }
            _DbContext.SaveChanges();
            result.Id = id;
            return result;
        }

        /// <summary>
        /// �޸ļ��ֵ���
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode ��</response>  
        /// <response code="400">ָ��ʵ���Id�����ڡ�ͨ������Bug.�ڼ�������¿����ǲ������⡣</response>  
        /// <response code="401">��Ч���ơ�</response>  
        /// <response code="403">Ȩ�޲��㡣</response>  
        [HttpPut]
        public ActionResult<ModifySimpleDataDicReturnDto> ModifySimpleDataDic(ModifySimpleDataDicParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ModifySimpleDataDicReturnDto();
            if (!_EntityManager.ModifyWithMarkDelete(model.Items))
            {
                var errResult = new StatusCodeResult(OwHelper.GetLastError()) { };
                return errResult;
            }
            foreach (var item in model.Items)
            {
                _DbContext.Entry(item).Property(c => c.DataDicId).IsModified = false;
                _DbContext.Entry(item).Property(c => c.CreateAccountId).IsModified = false;
                _DbContext.Entry(item).Property(c => c.CreateDateTime).IsModified = false;
            }
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// ɾ���������ֵ��е�һ�
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode ��</response>  
        /// <response code="400">ָ��ʵ���Id�����ڡ�ͨ������Bug.�ڼ�������¿����ǲ������⡣</response>  
        /// <response code="401">��Ч���ơ�</response>  
        /// <response code="403">Ȩ�޲��㡣</response>  
        [HttpDelete]
        public ActionResult<RemoveSimpleDataDicReturnDto> RemoveSimpleDataDic(RemoveSimpleDataDicParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RemoveSimpleDataDicReturnDto();
            var id = model.Id;
            DbSet<SimpleDataDic> dbSet = _DbContext.DD_SimpleDataDics;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// �ָ�ָ���ļ������ֵ䡣
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">δ����ϵͳ�����󡣵����ܳ���Ӧ�ô��󣬾���μ� HasError �� ErrorCode ��</response>  
        /// <response code="400">ָ��ʵ���Id�����ڡ�ͨ������Bug.�ڼ�������¿����ǲ������⡣</response>  
        /// <response code="401">��Ч���ơ�</response>  
        /// <response code="403">Ȩ�޲��㡣</response>  
        [HttpPost]
        public ActionResult<RestoreSimpleDataDicReturnDto> RestoreSimpleDataDic(RestoreSimpleDataDicParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            string err;
            if (!_AuthorizationManager.Demand(out err, "B.0")) return StatusCode((int)HttpStatusCode.Forbidden, err);

            var result = new RestoreSimpleDataDicReturnDto();
            if (!_EntityManager.Restore<SimpleDataDic>(model.Id))
            {
                var errResult = new StatusCodeResult(OwHelper.GetLastError()) { };
                return errResult;
            }
            _DbContext.SaveChanges();
            return result;
        }

        #endregion ���ֵ��CRUD


        #endregion �����ֵ����
    }
}