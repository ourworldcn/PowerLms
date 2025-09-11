using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NPOI.HSSF.UserModel;
using NuGet.Common;
using PowerLms.Data;
using PowerLmsServer.EfData;
using PowerLmsServer.Managers;
using PowerLmsWebApi.Dto;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.Net;
using OW.Data;
using NuGet.Packaging;
using NuGet.Protocol;
using AutoMapper;
using NPOI.SS.Formula.Functions;
using System.Text;
using Microsoft.Extensions.ObjectPool;
using System.Text.RegularExpressions;
using AutoMapper.Internal.Mappers;
using PowerLmsServer;
using Microsoft.EntityFrameworkCore.Internal;
using System.ComponentModel;

namespace PowerLmsWebApi.Controllers.System
{
    /// <summary>
    /// 管理员控制器 - 基础数据管理部分
    /// </summary>
    public partial class AdminController : PlControllerBase
    {
        #region 国家相关
        /// <summary>
        /// 获取国家。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">查询的条件。支持通用查询条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlCountryReturnDto> GetAllPlCountry([FromQuery] PagingParamsDtoBase model, [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlCountryReturnDto();
            var dbSet = _DbContext.DD_PlCountrys;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            if (_AccountManager.IsAdmin(context.User))  //若是超管
                coll = coll.Where(c => c.OrgId == null);
            else
            {
                var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                if (!merchantId.HasValue) return BadRequest("未知的商户Id");
                if (context.User.OrgId is null) //若没有指定机构
                {
                    coll = coll.Where(c => c.OrgId == merchantId);
                }
                else
                {
                    coll = coll.Where(c => c.OrgId == context.User.OrgId);
                }
            }

            // 使用EfHelper.GenerateWhereAnd进行通用查询条件处理
            coll = EfHelper.GenerateWhereAnd(coll, conditional);

            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加国家记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">参数错误。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<AddPlCountryReturnDto> AddPlCountry(AddPlCountryParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "B.5")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new AddPlCountryReturnDto();
            model.Item.GenerateNewId();
            var id = model.Item.Id;
            _DbContext.DD_PlCountrys.Add(model.Item);
            _DbContext.SaveChanges();
            result.Id = id;
            return result;
        }

        /// <summary>
        /// 修改国家记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPut]
        public ActionResult<ModifyPlCountryReturnDto> ModifyPlCountry(ModifyPlCountryParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "B.5")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ModifyPlCountryReturnDto();
            if (!_EntityManager.ModifyWithMarkDelete(model.Items))
            {
                var errResult = new StatusCodeResult(OwHelper.GetLastError()) { };
                return errResult;
            }
            foreach (var item in model.Items)
            {
                _DbContext.Entry(item).Property(c => c.IsDelete).IsModified = false;
            }
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除国家的记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpDelete]
        public ActionResult<RemovePlCountryReturnDto> RemovePlCountry(RemovePlCountryParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "B.5")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RemovePlCountryReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.DD_PlCountrys;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            //if (item.DataDicType == 1) //若是简单字典
            //    _DbContext.Database.ExecuteSqlRaw($"delete from {nameof(_DbContext.SimpleDataDics)} where {nameof(SimpleDataDic.DataDicId)}='{id.ToString()}'");
            //else //其他字典待定
            //{

            //}
            return result;
        }

        /// <summary>
        /// 恢复指定的被删除国家记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<RestorePlCountryReturnDto> RestorePlCountry(RestorePlCountryParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "B.5")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RestorePlCountryReturnDto();
            if (!_EntityManager.Restore<PlCountry>(model.Id))
            {
                var errResult = new StatusCodeResult(OwHelper.GetLastError()) { };
                return errResult;
            }
            _DbContext.SaveChanges();
            return result;
        }

        #endregion 国家相关

        #region 币种相关
        /// <summary>
        /// 获取币种。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">支持通用查询条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlCurrencyReturnDto> GetAllPlCurrency([FromQuery] PagingParamsDtoBase model, [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlCurrencyReturnDto();
            var dbSet = _DbContext.DD_PlCurrencys;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            if (_AccountManager.IsAdmin(context.User))  //若是超管
                coll = coll.Where(c => c.OrgId == null);
            else
            {
                var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                if (!merchantId.HasValue) return BadRequest("未知的商户Id");
                if (context.User.OrgId is null) //若没有指定机构
                {
                    coll = coll.Where(c => c.OrgId == merchantId);
                }
                else
                {
                    coll = coll.Where(c => c.OrgId == context.User.OrgId);
                }
            }

            // 使用EfHelper.GenerateWhereAnd进行通用查询条件处理
            coll = EfHelper.GenerateWhereAnd(coll, conditional);

            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 增加币种记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">参数错误。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<AddPlCurrencyReturnDto> AddPlCurrency(AddPlCurrencyParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "B.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new AddPlCurrencyReturnDto();
            model.Item.GenerateNewId();
            var id = model.Item.Id;
            _DbContext.DD_PlCurrencys.Add(model.Item);
            _DbContext.SaveChanges();
            result.Id = id;
            return result;
        }

        /// <summary>
        /// 修修改币种记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPut]
        public ActionResult<ModifyPlCurrencyReturnDto> ModifyPlCurrency(ModifyPlCurrencyParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "B.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ModifyPlCurrencyReturnDto();
            if (!_EntityManager.ModifyWithMarkDelete(model.Items))
            {
                var errResult = new StatusCodeResult(OwHelper.GetLastError()) { };
                return errResult;
            }
            foreach (var item in model.Items)
            {
                _DbContext.Entry(item).Property(c => c.IsDelete).IsModified = false;
            }
            _DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// 删除币种的记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpDelete]
        public ActionResult<RemovePlCurrencyReturnDto> RemovePlCurrency(RemovePlCurrencyParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "B.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RemovePlCurrencyReturnDto();
            var id = model.Id;
            var dbSet = _DbContext.DD_PlCurrencys;
            var item = dbSet.Find(id);
            if (item is null) return BadRequest();
            _EntityManager.Remove(item);
            _DbContext.SaveChanges();
            //if (item.DataDicType == 1) //若是简单字典
            //    _DbContext.Database.ExecuteSqlRaw($"delete from {nameof(_DbContext.SimpleDataDics)} where {nameof(SimpleDataDic.DataDicId)}='{id.ToString()}'");
            //else //其他字典待定
            //{

            //}
            return result;
        }

        /// <summary>
        /// 恢复指定的被删除币种记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<RestorePlCurrencyReturnDto> RestorePlCurrency(RestorePlCurrencyParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "B.3")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new RestorePlCurrencyReturnDto();
            if (!_EntityManager.Restore<PlCurrency>(model.Id))
            {
                var errResult = new StatusCodeResult(OwHelper.GetLastError()) { };
                return errResult;
            }
            _DbContext.SaveChanges();
            return result;
        }

        #endregion 币种相关

        #region 汇率相关

        /// <summary>
        /// 导入汇率对象.符合条件的汇率对象会被导入到当前用户登录的机构中。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">条件格式错误或未找到符合条件的汇率记录。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpPost]
        public ActionResult<ImportPlExchangeRateReturnDto> ImportPlExchangeRate(ImportPlExchangeRateParamsDto model)
        {
            // 验证令牌并获取上下文
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();

            var result = new ImportPlExchangeRateReturnDto();

            try
            {
                // 确保条件字典键不区分大小写
                var conditionalIgnoreCase = model.Conditional != null
                    ? new Dictionary<string, string>(model.Conditional, StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, string>();

                // 获取汇率数据集
                var dbSet = _DbContext.DD_PlExchangeRates;

                // 构建基本查询
                var sourceColl = dbSet.AsNoTracking();

                // 根据用户角色确定源数据筛选条件
                if (_AccountManager.IsAdmin(context.User)) // 若是超管，可以查看全局汇率
                {
                    //不做限制
                }
                else
                {
                    // 并根据用户的机构进行筛选
                    var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                    if (!merchantId.HasValue)
                        return BadRequest("未知的商户Id");

                    if (context.User.OrgId is null) // 若没有指定机构
                    {
                        // 返回错误信息，要求用户指定机构
                        return BadRequest("请指定用户的登录机构");
                    }
                    else // 若指定了机构
                    {
                        // 获取当前登录机构及其所有子机构包含下属公司的所有机构Id
                        var allOrgs = _OrgManager.GetOrLoadOrgCacheItem(merchantId.Value).Orgs.Values.ToArray();

                        var allOrgIds = allOrgs.Select(c => c.Id).ToList();  // 获取所有机构ID

                        var currentCompany = _OrgManager.GetCurrentCompanyByUser(context.User);
                        if (currentCompany != null)
                        {
                            var companyOrgIds = _OrgManager.GetOrgIdsByCompanyId(currentCompany.Id);
                            sourceColl = sourceColl.Where(c => c.OrgId.HasValue && companyOrgIds.Contains(c.OrgId.Value));
                        }
                        else
                        {
                            sourceColl = sourceColl.Where(c => c.OrgId.HasValue && allOrgIds.Contains(c.OrgId.Value));
                        }
                    }
                }

                // 使用EfHelper.GenerateWhereAnd进行通用查询条件处理
                var filteredQuery = EfHelper.GenerateWhereAnd(sourceColl, conditionalIgnoreCase);
                if (filteredQuery == null)
                {
                    return BadRequest(OwHelper.GetLastErrorMessage() ?? "条件格式错误");
                }

                // 执行查询并获取源汇率数据
                var sourceRates = filteredQuery.ToList();
                if (sourceRates.Count == 0)
                {
                    result.HasError = false;    //不是错误
                    result.DebugMessage = "未找到符合条件的汇率记录";
                    return result;
                }

                // 确定目标组织ID - 导入到当前用户的机构中
                Guid? targetOrgId;
                if (context.User.OrgId.HasValue) // 若用户属于某个机构
                {
                    targetOrgId = context.User.OrgId;
                }
                else
                {
                    return BadRequest("未知的商户Id");
                }

                // 导入汇率记录到目标机构
                int importedCount = 0;
                foreach (var sourceRate in sourceRates)
                {
                    // 创建新汇率对象，为其分配新的ID
                    var newRate = new PlExchangeRate
                    {
                        Id = Guid.NewGuid(),
                        OrgId = targetOrgId,
                        ShortcutName = sourceRate.ShortcutName,
                        IsDelete = false,

                        BusinessTypeId = sourceRate.BusinessTypeId,
                        SCurrency = sourceRate.SCurrency,
                        DCurrency = sourceRate.DCurrency,
                        Radix = sourceRate.Radix,
                        Exchange = sourceRate.Exchange,
                        BeginDate = sourceRate.BeginDate,
                        EndData = sourceRate.EndData
                    };

                    // 添加到数据库
                    _DbContext.DD_PlExchangeRates.Add(newRate);
                    importedCount++;
                }

                // 保存更改
                _DbContext.SaveChanges();

                // 设置返回结果
                result.HasError = false;
                result.DebugMessage = $"成功导入{importedCount}条汇率记录";

                return result;
            }
            catch (Exception ex)
            {
                result.HasError = true;
                result.DebugMessage = "导入汇率时发生错误：" + ex.ToString();
                return result;
            }
        }

        /// <summary>
        /// 获取汇率。
        /// </summary>
        /// <param name="model"></param>
        /// <param name="conditional">支持通用查询条件。</param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetAllPlExchangeRateReturnDto> GetAllPlExchangeRate([FromQuery] PagingParamsDtoBase model,
            [FromQuery] Dictionary<string, string> conditional = null)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetAllPlExchangeRateReturnDto();
            var dbSet = _DbContext.DD_PlExchangeRates;
            var coll = dbSet.OrderBy(model.OrderFieldName, model.IsDesc).AsNoTracking();
            if (_AccountManager.IsAdmin(context.User))  //若是超管
                coll = coll.Where(c => c.OrgId == null);
            else
            {
                var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
                if (!merchantId.HasValue) return BadRequest("未知的商户Id");
                if (context.User.OrgId is null) //若没有指定机构
                {
                    coll = coll.Where(c => c.OrgId == merchantId);
                }
                else
                {
                    coll = coll.Where(c => c.OrgId == context.User.OrgId);
                }
            }

            // 使用EfHelper.GenerateWhereAnd进行通用查询条件处理
            coll = EfHelper.GenerateWhereAnd(coll, conditional);

            var prb = _EntityManager.GetAll(coll, model.StartIndex, model.Count);
            _Mapper.Map(prb, result);
            return result;
        }

        /// <summary>
        /// 扩展获取汇率。返回当前用户登录机构的汇率，且符合条件的所有汇率对象。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定类别Id无效。</response>  
        /// <response code="401">无效令牌。</response>  
        [HttpGet]
        public ActionResult<GetCurrentOrgExchangeRateReturnDto> GetCurrentOrgExchangeRate([FromQuery] GetCurrentOrgExchangeRateParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            var result = new GetCurrentOrgExchangeRateReturnDto();
            model.StartDateTime ??= DateTime.Now;
            model.EndDateTime ??= DateTime.Now;

            var dbSet = _DbContext.DD_PlExchangeRates;
            var coll = dbSet.AsNoTracking();
            coll = coll.Where(c => c.OrgId == context.User.OrgId);
            coll = coll.Where(c => c.BeginDate <= model.StartDateTime && c.EndData >= model.EndDateTime);

            var merchantId = _OrgManager.GetMerchantIdByUserId(context.User.Id);
            if (!merchantId.HasValue)
                return result;

            var orgs = _OrgManager.GetOrLoadOrgCacheItem(merchantId.Value).Orgs.Values.ToArray();
            if (!orgs.Any(o => o.Id == context.User.OrgId.Value))
                return BadRequest($"找不到指定的登录公司Id={merchantId}");

            var org = orgs.First(o => o.Id == context.User.OrgId.Value);
            if (string.IsNullOrWhiteSpace(org.BaseCurrencyCode))
                return BadRequest($"公司本币设置错误，本币代码为:{org.BaseCurrencyCode}");

            coll = coll.Where(c => c.DCurrency == org.BaseCurrencyCode);

            result.Result.AddRange(coll);
            return result;
        }

        /// <summary>
        /// 增加一个汇率记录。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">参数错误。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPost]
        public ActionResult<AddPlExchangeRateReturnDto> AddPlExchangeRate(AddPlExchangeRateParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "B.4")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new AddPlExchangeRateReturnDto();
            model.Item.GenerateNewId();
            var id = model.Item.Id;
            _DbContext.DD_PlExchangeRates.Add(model.Item);
            _DbContext.SaveChanges();
            result.Id = id;
            return result;
        }

        /// <summary>
        /// 修改汇率项。
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <response code="200">未发生系统级错误。但可能出现应用错误，具体参见 HasError 和 ErrorCode 。</response>  
        /// <response code="400">指定实体的Id不存在。通常这是Bug.在极端情况下可能是并发问题。</response>  
        /// <response code="401">无效令牌。</response>  
        /// <response code="403">权限不足。</response>  
        [HttpPut]
        public ActionResult<ModifyPlExchangeRateReturnDto> ModifyPlExchangeRate(ModifyPlExchangeRateParamsDto model)
        {
            if (_AccountManager.GetOrLoadContextByToken(model.Token, _ServiceProvider) is not OwContext context) return Unauthorized();
            if (!_AuthorizationManager.Demand(out string err, "B.4")) return StatusCode((int)HttpStatusCode.Forbidden, err);
            var result = new ModifyPlExchangeRateReturnDto();
            if (!_EntityManager.Modify(model.Items))
            {
                var errResult = new StatusCodeResult(OwHelper.GetLastError()) { };
                return errResult;
            }
            _DbContext.SaveChanges();
            return result;
        }

        #endregion 汇率相关

    }
}
