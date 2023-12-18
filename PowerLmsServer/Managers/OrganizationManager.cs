using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
    /// 组织机构管理器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Scoped)]
    public class OrganizationManager
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public OrganizationManager(PowerLmsUserDbContext dbContext)
        {
            _DbContext = dbContext;
        }

        PowerLmsUserDbContext _DbContext;

        /// <summary>
        /// 获取指定账户所属的商户Id。
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="MerchantId"></param>
        /// <returns>true指定账户所属商户Id,如果不属于任何商户则返回null。false 没有找到指定的用户Id。</returns>
        public bool GetMerchantId(Guid accountId, out Guid? MerchantId)
        {
            var userOrg = _DbContext.AccountPlOrganizations.AsNoTracking().FirstOrDefault(c => c.UserId == accountId);    //随机找到一个所属的组织机构
            if (userOrg == null)
            {
                MerchantId = null;
                return false;
            };
            var org = _DbContext.PlOrganizations.FirstOrDefault(c => c.Id == userOrg.OrgId);
            if (org == null)    //若不是组织机构
            {
                var merch = _DbContext.Merchants.Find(userOrg.OrgId);
                if (merch is null)  //若也不是商户Id
                {
                    MerchantId = null;
                    return false;
                }
                MerchantId = merch.Id;
                return true;
            }
            return GetMerchantIdFromOrgId(org.Id, out MerchantId);
        }

        /// <summary>
        /// 取指定组织机构Id所属的商户Id。
        /// </summary>
        /// <param name="orgId">机构Id。</param>
        /// <param name="MerchantId"></param>
        /// <returns></returns>
        public bool GetMerchantIdFromOrgId(Guid orgId, out Guid? MerchantId)
        {
            var org = _DbContext.PlOrganizations.Find(orgId);   //找到组织机构对象
            if (org == null)
            {
                MerchantId = null;
                return false;
            }
            for (; org is not null; org = org.Parent)
            {
                if (org.ParentId is null)
                {
                    MerchantId = org.MerchantId;
                    return true;
                }
            }
            MerchantId = null;
            return false;
        }
    }
}
