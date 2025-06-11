using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OW.DDD;
using OW.EntityFrameworkCore;
using OwDbBase.Tasks;
using PowerLms.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace PowerLmsServer.EfData
{
    /// <summary>
    /// 项目使用的主要数据库上下文。
    /// </summary>
    public class PowerLmsUserDbContext : OwDbContext
    {
        /// <summary>
        /// 创建新的数据库上下文实例。
        /// </summary>
        public PowerLmsUserDbContext(DbContextOptions<PowerLmsUserDbContext> options,
                                     ILogger<PowerLmsUserDbContext> logger,
                                     IServiceProvider serviceProvider)
            : base(options, logger, serviceProvider)  // 调用基类构造函数
        {
            //Database.SetInitializer<Context>(new MigrateDatabaseToLatestVersion<Context, ReportingDbMigrationsConfiguration>());
            Initialize();
        }

        void Initialize()
        {
        }

        class EntityEntryEqualityComparer : EqualityComparer<EntityEntry>
        {
            public override bool Equals(EntityEntry b1, EntityEntry b2)
            {
                return b1.Entity.Equals(b2.Entity);
            }

            public override int GetHashCode(EntityEntry bx)
            {
                return bx.Entity.GetHashCode();
            }
        }

        #region 方法

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="optionsBuilder"></param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Data Source=.;Database=PowerLmsUserDevelopment;Integrated Security=True;Trusted_Connection=True;MultipleActiveResultSets=true;Pooling=True;TrustServerCertificate=True;Application Name=PowerLms;");
                Trace.WriteLine("OnConfiguring被调用");
            }
            base.OnConfiguring(optionsBuilder);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AccountPlOrganization>().HasKey(nameof(AccountPlOrganization.UserId), nameof(AccountPlOrganization.OrgId));

            modelBuilder.Entity<PlBusinessHeader>().HasKey(nameof(PlBusinessHeader.CustomerId), nameof(PlBusinessHeader.UserId), nameof(PlBusinessHeader.OrderTypeId));

            #region 权限相关
            modelBuilder.Entity<AccountRole>().HasKey(nameof(AccountRole.UserId), nameof(AccountRole.RoleId));
            modelBuilder.Entity<RolePermission>().HasKey(nameof(RolePermission.RoleId), nameof(RolePermission.PermissionId));
            #endregion 权限相关

            base.OnModelCreating(modelBuilder);

            #region 应用日志相关
            modelBuilder.Entity<OwAppLogStore>().HasData(
                new OwAppLogStore
                {
                    Id = GeneralInfoLogEntry.TypeId,
                    FormatString = "用户:{LoginName}({CompanyName}){OperationType}成功",
                    LogLevel = Microsoft.Extensions.Logging.LogLevel.Information,
                }
            );

            // 配置OwAppLogView视图的映射
            modelBuilder.Entity<OwAppLogView>()
                .HasNoKey()
                .ToView("OwAppLogView");
            #endregion  //应用日志相关
        }

        /// <summary>
        /// 删除一组指定Id的对象。立即生效。
        /// </summary>
        /// <param name="deleteIds"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public int Delete(List<Guid> deleteIds, string tableName)
        {
            var ids = string.Join(',', deleteIds.Select(c => c.ToString()));
            var sql = $"Delete From {tableName} Where [Id] In ({ids})";
            return Database.ExecuteSqlRaw(sql);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns></returns>
        public override ValueTask DisposeAsync()
        {
            return base.DisposeAsync();
        }
        #endregion 方法

        #region 系统资源相关

        /// <summary>
        /// 
        /// </summary>
        public DbSet<OwSystemLog> OwSystemLogs { get; set; }

        /// <summary>
        /// 系统资源表。包含数据字典目录。
        /// </summary>
        public DbSet<SystemResource> DD_SystemResources { get; set; }

        /// <summary>
        /// 数据字典目录。
        /// </summary>
        public DbSet<DataDicCatalog> DD_DataDicCatalogs { get; set; }

        /// <summary>
        /// 简单数据字典表。
        /// </summary>
        public DbSet<SimpleDataDic> DD_SimpleDataDics { get; set; }

        /// <summary>
        /// 业务大类字典表。
        /// </summary>
        public DbSet<BusinessTypeDataDic> DD_BusinessTypeDataDics { get; set; }

        /// <summary>
        /// 港口数据字典表。
        /// </summary>
        public DbSet<PlPort> DD_PlPorts { get; set; }

        /// <summary>
        /// 航线表。
        /// </summary>
        public DbSet<PlCargoRoute> DD_PlCargoRoutes { get; set; }

        /// <summary>
        /// 汇率表。
        /// </summary>
        public DbSet<PlExchangeRate> DD_PlExchangeRates { get; set; }

        /// <summary>
        /// 单位换算表。
        /// </summary>
        public DbSet<UnitConversion> DD_UnitConversions { get; set; }

        /// <summary>
        /// 费用种类表。
        /// </summary>
        public DbSet<FeesType> DD_FeesTypes { get; set; }

        /// <summary>
        /// 业务编码规则表。
        /// </summary>
        public DbSet<JobNumberRule> DD_JobNumberRules { get; set; }

        /// <summary>
        /// 可重用的序号表。
        /// </summary>
        public DbSet<JobNumberReusable> JobNumberReusables { get; set; }

        /// <summary>
        /// 国家表。
        /// </summary>
        public DbSet<PlCountry> DD_PlCountrys { get; set; }

        /// <summary>
        /// 币种表。
        /// </summary>
        public DbSet<PlCurrency> DD_PlCurrencys { get; set; }

        public DbSet<OtherNumberRule> DD_OtherNumberRules { get; set; }

        /// <summary>
        /// 箱型。
        /// </summary>
        public DbSet<ShippingContainersKind> DD_ShippingContainersKinds { get; set; }
        #endregion 系统资源相关

        #region 多语言相关

        /// <summary>
        /// 多语言资源表。
        /// </summary>
        public DbSet<Multilingual> Multilinguals { get; set; }

        ///// <summary>
        ///// 语言字典。已并入简单字典。
        ///// </summary>
        //public DbSet<LanguageDataDic> LanguageDataDics { get; set; }
        #endregion 多语言相关

        #region 账号相关
        /// <summary>
        /// 账号表。
        /// </summary>
        public DbSet<Account> Accounts { get; set; }

        /// <summary>
        /// 用户所属组织机构表。
        /// </summary>
        public DbSet<AccountPlOrganization> AccountPlOrganizations { get; set; }

        #endregion 账号相关

        #region 组织机构相关

        /// <summary>
        /// 开户行信息表。
        /// </summary>
        public DbSet<BankInfo> BankInfos { get; set; }

        /// <summary>
        /// 商户。
        /// </summary>
        public DbSet<PlMerchant> Merchants { get; set; }

        /// <summary>
        /// 组织机构表。
        /// </summary>
        public DbSet<PlOrganization> PlOrganizations { get; set; }
        #endregion 组织机构相关

        #region 客户资料相关

        /// <summary>
        /// 客户资料表。
        /// </summary>
        public DbSet<PlCustomer> PlCustomers { get; set; }

        /// <summary>
        /// 客户资料的联系人。
        /// </summary>
        public DbSet<PlCustomerContact> PlCustomerContacts { get; set; }

        /// <summary>
        /// 客户资料的开票信息。
        /// </summary>
        public DbSet<PlTaxInfo> PlCustomerTaxInfos { get; set; }

        /// <summary>
        /// 客户提单内容表。
        /// </summary>
        public DbSet<PlTidan> PlCustomerTidans { get; set; }

        /// <summary>
        /// 业务负责人表。
        /// </summary>
        public DbSet<PlBusinessHeader> PlCustomerBusinessHeaders { get; set; }

        /// <summary>
        /// 黑名单客户跟踪表。
        /// </summary>
        public DbSet<CustomerBlacklist> CustomerBlacklists { get; set; }

        /// <summary>
        /// 装货地址表。
        /// </summary>
        public DbSet<PlLoadingAddr> PlCustomerLoadingAddrs { get; set; }
        #endregion  客户资料相关

        /// <summary>
        /// 文件信息表。
        /// </summary>
        public DbSet<PlFileInfo> PlFileInfos { get; set; }

        /// <summary>
        /// 验证码信息表。
        /// </summary>
        public DbSet<CaptchaInfo> CaptchaInfos { get; set; }

        #region 权限相关

        /// <summary>
        /// 角色表。
        /// </summary>
        [Comment("角色表。")]
        public DbSet<PlRole> PlRoles { get; set; }

        /// <summary>
        /// 记录账号与角色关系的表。
        /// </summary>
        [Comment("记录账号与角色关系的表。")]
        public DbSet<AccountRole> PlAccountRoles { get; set; }

        /// <summary>
        /// 记录角色和权限的关系表。
        /// </summary>
        [Comment("记录角色和权限的关系表。")]
        public DbSet<RolePermission> PlRolePermissions { get; set; }

        /// <summary>
        /// 权限表。
        /// </summary>
        [Comment("权限表。")]
        public DbSet<PlPermission> PlPermissions { get; set; }
        #endregion 权限相关

        /// <summary>
        /// 航线管理。
        /// </summary>
        public DbSet<ShippingLane> ShippingLanes { get; set; }

        #region 业务相关

        /// <summary>
        /// 业务总表。
        /// </summary>
        public DbSet<PlJob> PlJobs { get; set; }

        /// <summary>
        /// 空运出口表。
        /// </summary>
        public DbSet<PlEaDoc> PlEaDocs { get; set; }

        /// <summary>
        /// 现场出重子表。
        /// </summary>
        public DbSet<HuochangChuchong> HuochangChuchongs { get; set; }

        /// <summary>
        /// 业务单的费用表。
        /// </summary>
        public DbSet<DocFee> DocFees { get; set; }

        /// <summary>
        /// 业务单的账单。
        /// </summary>
        public DbSet<DocBill> DocBills { get; set; }

        /// <summary>
        /// 空运进口单表。
        /// </summary>
        public DbSet<PlIaDoc> PlIaDocs { get; set; }

        /// <summary>
        /// 海运进口单表。
        /// </summary>
        public DbSet<PlIsDoc> PlIsDocs { get; set; }

        /// <summary>
        /// 海运出口单表。
        /// </summary>
        public DbSet<PlEsDoc> PlEsDocs { get; set; }

        /// <summary>
        /// 海运箱量表。
        /// </summary>
        public DbSet<ContainerKindCount> ContainerKindCounts { get; set; }


        #endregion

        #region 财务相关
        /// <summary>
        /// 业务费用收付款申请单.
        /// </summary>
        public DbSet<DocFeeRequisition> DocFeeRequisitions { get; set; }

        /// <summary>
        /// 业务费用收付款申请单明细项。
        /// </summary>
        public DbSet<DocFeeRequisitionItem> DocFeeRequisitionItems { get; set; }

        /// <summary>
        /// 费用结算单。
        /// </summary>
        public DbSet<PlInvoices> PlInvoicess { get; set; }

        /// <summary>
        /// 费用结算单明细。
        /// </summary>
        public DbSet<PlInvoicesItem> PlInvoicesItems { get; set; }

        /// <summary>
        /// 费用方案。
        /// </summary>
        public DbSet<DocFeeTemplate> DocFeeTemplates { get; set; }

        /// <summary>
        /// 费用方案明细项。
        /// </summary>
        public DbSet<DocFeeTemplateItem> DocFeeTemplateItems { get; set; }

        #endregion

        #region 流程相关

        /// <summary>
        /// 流程模板表。
        /// </summary>
        public DbSet<OwWfKindCodeDic> WfKindCodeDics { get; set; }

        /// <summary>
        /// 流程模板表。
        /// </summary>
        public DbSet<OwWfTemplate> WfTemplates { get; set; }

        /// <summary>
        /// 流程模板节点表。
        /// </summary>
        public DbSet<OwWfTemplateNode> WfTemplateNodes { get; set; }

        /// <summary>
        /// 流程模板节点参与者表。
        /// </summary>
        public DbSet<OwWfTemplateNodeItem> WfTemplateNodeItems { get; set; }

        /// <summary>
        /// 工作流实例表
        /// </summary>
        public DbSet<OwWf> OwWfs { get; set; }

        /// <summary>
        /// 工作流实例节点表。
        /// </summary>
        public DbSet<OwWfNode> OwWfNodes { get; set; }

        /// <summary>
        /// 工作流实例节点详细信息表。
        /// </summary>
        public DbSet<OwWfNodeItem> OwWfNodeItems { get; set; }

        #endregion

        #region 应用日志相关

        /// <summary>
        /// 应用日志源条目。
        /// </summary>
        public DbSet<OwAppLogStore> OwAppLogStores { get; set; }

        /// <summary>
        /// 应用日志详细信息。
        /// </summary>
        public DbSet<OwAppLogItemStore> OwAppLogItemStores { get; set; }

        /// <summary>
        /// 应用日志视图，联合了OwAppLogStore和OwAppLogItemStore的数据。
        /// </summary>
        public DbSet<OwAppLogView> OwAppLogViews { get; set; }

        #endregion 应用日志相关

        #region 税务发票相关

        /// <summary>
        /// 开票渠道表。
        /// </summary>
        public DbSet<TaxInvoiceChannel> TaxInvoiceChannels { get; set; }

        /// <summary>
        /// 渠道账号表。
        /// </summary>
        public DbSet<TaxInvoiceChannelAccount> TaxInvoiceChannelAccounts { get; set; }

        /// <summary>
        /// 机构渠道账号表。
        /// </summary>
        public DbSet<OrgTaxChannelAccount> OrgTaxChannelAccounts { get; set; }

        /// <summary>
        /// 税务发票信息表。
        /// </summary>
        public DbSet<TaxInvoiceInfo> TaxInvoiceInfos { get; set; }

        /// <summary>
        /// 税务发票信息明细表。
        /// </summary>
        public DbSet<TaxInvoiceInfoItem> TaxInvoiceInfoItems { get; set; }

        #endregion 税务发票相关

        #region 消息系统
        /// <summary>
        /// 消息表。
        /// </summary>
        public DbSet<OwMessage> OwMessages { get; set; }
        #endregion 消息系统

        #region 任务系统

        /// <summary>
        /// 任务存储表。
        /// </summary>
        public DbSet<OwTaskStore> OwTaskStores { get; set; }
        #endregion 任务系统
    }

    /// <summary>
    /// 数据库上下文的扩展方法。
    /// </summary>
    public static class PowerLmsUserDbContextExtensions
    {
    }
}
