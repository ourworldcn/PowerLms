using AutoMapper;
using MathNet.Numerics.Optimization.LineSearch;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.Extensions.DependencyInjection;
using NPOI.HPSF;
using NPOI.SS.Formula.Functions;
using NPOI.SS.Formula.PTG;
using NPOI.Util;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 数据字典的服务。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped)]
    public class DataDicManager
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public DataDicManager(PowerLmsUserDbContext dbContext, OwContext accountManager, IMapper mapper)
        {
            _DbContext = dbContext;
            _OwContext = accountManager;
            _Mapper = mapper;
        }

        PowerLmsUserDbContext _DbContext;

        /// <summary>
        /// 获取该管理器的数据库上下文。
        /// </summary>
        public PowerLmsUserDbContext DbContext => _DbContext;
        OwContext _OwContext;
        IMapper _Mapper;

        /// <summary>
        /// 复制数据字典。调用者需要自己保存更改。
        /// </summary>
        /// <param name="catalog"></param>
        /// <param name="orgId">新组织机构Id。</param>
        public void CopyTo(DataDicCatalog catalog, Guid orgId)
        {
            var cata = (DataDicCatalog)catalog.Clone();
            cata.GenerateNewId();
            cata.OrgId = orgId;
            _DbContext.Add(cata);
            var baseDataDic = _DbContext.DD_SimpleDataDics.Where(c => c.DataDicId == catalog.Id).AsNoTracking().ToArray(); //基本字典数据
            baseDataDic.ForEach(c =>
            {
                c.GenerateNewId();
                c.DataDicId = cata.Id;
                c.CreateDateTime = _OwContext.CreateDateTime;
                c.CreateAccountId = _OwContext.User.Id;
            });
            _DbContext.AddRange(baseDataDic);
        }

        /// <summary>
        /// 复制特殊字典到指定的组织机构中。
        /// </summary>
        /// <typeparam name="T">字典元素的类型。</typeparam>
        /// <param name="dataDics"></param>
        /// <param name="orgId"></param>
        public void CopyTo<T>(IEnumerable<T> dataDics, Guid orgId) where T : SpecialDataDicBase
        {
            List<T> list = new List<T>();
            foreach (var dataDic in dataDics)
            {
                var tmp = _Mapper.Map<T>(dataDic);
                tmp.GenerateNewId();
                tmp.OrgId = orgId;
                list.Add(tmp);
            }
            _DbContext.AddRange(list);
        }

        /// <summary>
        /// 将一组特殊字典，追加到指定的组织机构中。
        /// </summary>
        /// <param name="dataDics">每个对象被更改属性后追加到指定的指定组织机构中。</param>
        /// <param name="orgId"></param>
        public void AddTo<T>(IEnumerable<T> dataDics, Guid orgId) where T : SpecialDataDicBase
        {
            foreach (var item in dataDics)
            {
                item.GenerateNewId();
                item.OrgId = orgId;
                _DbContext.Add(item);
            }
        }
    }

    /// <summary>
    /// <see cref="DataDicManager"/>类扩展方法封装类。
    /// </summary>
    public static class DataDicManagerExtensions
    {
        /// <summary>
        /// 复制所有特殊字典到一个新组织机构。
        /// </summary>
        /// <param name="mng"></param>
        /// <param name="orgId"></param>
        public static void CopyAllSpecialDataDicBase(this DataDicManager mng, Guid orgId)
        {
            mng.AddTo(mng.DbContext.DD_FeesTypes.Where(c => c.OrgId == null).AsNoTracking(), orgId);
            mng.AddTo(mng.DbContext.DD_JobNumberRules.Where(c => c.OrgId == null).AsNoTracking(), orgId);
            mng.AddTo(mng.DbContext.DD_PlCargoRoutes.Where(c => c.OrgId == null).AsNoTracking(), orgId);
            mng.AddTo(mng.DbContext.DD_PlCountrys.Where(c => c.OrgId == null).AsNoTracking(), orgId);
            mng.AddTo(mng.DbContext.DD_PlCurrencys.Where(c => c.OrgId == null).AsNoTracking(), orgId);
            mng.AddTo(mng.DbContext.DD_PlExchangeRates.Where(c => c.OrgId == null).AsNoTracking(), orgId);
            mng.AddTo(mng.DbContext.DD_PlPorts.Where(c => c.OrgId == null).AsNoTracking(), orgId);
            mng.AddTo(mng.DbContext.DD_UnitConversions.Where(c => c.OrgId == null).AsNoTracking(), orgId);
        }
    }
}
