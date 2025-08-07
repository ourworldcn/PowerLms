/*
 * 项目：PowerLms货运物流业务管理系统
 * 模块：财务数据 - 科目配置
 * 文件说明：
 * - 功能1：财务科目配置实体定义，支持金蝶财务系统对接
 * - 功能2：凭证生成规则配置，支持多种财务过程的科目映射
 * - 功能3：DBF文件生成配置，支持标准财务软件数据交换
 * 
 * 财务系统接口与凭证生成说明：
 * 
 * 1. 财务过程定义（系统内不映射）：
 * - PBI: 发票挂账（B账）- POST Bill Invoice
 * - RF: 实收 - Receive Funds
 * - PF: 实付 - Pay Funds
 * - ARA: 计提应收账款（A账挂账）- Accrue Receivable A 帐
 * - APA: 计提应付账款（A账挂账）- Accrue Payable A 帐
 * - ATR: 计提税金及主营业务收入 - Accrue Tax & Revenue
 * - ARAB: 计提A账应收本位币挂账 - Accrue Receivable A-account Base Currency
 * - APAB: 计提A账应付本位币挂账 - Accrue Payable A-account Base Currency
 * 
 * 2. 科目配置规则：
 * - 子项CODE = 过程CODE前缀 + 业务缩写
 * - GEN_前缀表示公共（非专属过程）科目
 * - 每个过程具有独立的科目编码体系
 * 
 * 3. DBF文件生成规则：
 * - 必须在同一凭证号内保持一致的字段：
 *   * FCLSNAME1（核销类别）
 *   * FOBJID1（客户财务简称）
 *   * FOBJNAME1（客户名称）
 *   * FTRANSID（客户财务编码）
 * - 字段映射示例：FAcctID取科目编码，FPREPARE取制单人，Fgroup取凭证类别
 * 
 * 4. 主要凭证生成流程：
 * 
 * 4.1 发票挂账（B账）- PBI：
 * - 分录0：应收账款（借）= 价税合计
 * - 分录1：主营业务收入（贷）= 价额（价税合计−税额）
 * - 分录2：应交税金（贷）= 税额
 * - 共同字段：FDATE/FTRANSDATE/FPeriod取发票开票日期
 * 
 * 4.2 实收 - RF：
 * - 分录0：银行存款（借）= 结算总额
 * - 分录1：应收账款（贷）= 结算总额
 * - 支持外币：外币金额和本位币金额分别记录
 * 
 * 4.3 实付 - PF：
 * - 分录0：应付账款（借）= 付款金额
 * - 分录1：银行存款（贷）= 付款金额
 * 
 * 4.4 计提A账应收本位币挂账 - ARAB：
 * - 统计条件：工作号财务日期月初到操作日，IO=收入
 * - 按结算单位、国内外、代垫类型分组汇总
 * - 分录0：计提总应收（借）= 所有应收汇总
 * - 分录1-4：各类应收明细（贷）= 分类汇总金额
 * 
 * 4.5 计提A账应付本位币挂账 - APAB：
 * - 统计条件：工作号财务日期月初到操作日，IO=支出
 * - 按结算单位、国内外、代垫类型分组汇总
 * - 分录0：计提总应付（贷）= 所有应付汇总
 * - 分录1-4：各类应付明细（借）= 分类汇总金额
 * 
 * 5. 技术约束：
 * - 汇率默认取结算明细首行，无外币时固定为1
 * - 任务需加锁，避免重复生成凭证
 * - 异常时回滚已标记状态并记录日志
 * - 计提类过程支持重复记账，需业务逻辑处理重复性检查
 * 
 * 技术要点：
 * - 基于Entity Framework Core的数据持久化
 * - 支持多租户数据隔离（ISpecificOrg）
 * - 支持软删除和审计追踪（IMarkDelete、ICreatorInfo）
 * - 唯一索引确保同组织内编码唯一性
 * 
 * 作者：zc
 * 创建：2024年
 * 修改：2025-01-27 整合财务系统接口与凭证生成需求文档
 */

using Microsoft.EntityFrameworkCore;
using OW.Data;
using System;
using System.ComponentModel.DataAnnotations;

namespace PowerLms.Data
{
    /// <summary>
    /// 财务科目设置表
    /// 
    /// 核心功能：
    /// - 财务科目配置：管理与金蝶财务系统对接的科目编码和配置
    /// - 凭证生成配置：为不同财务过程提供科目映射规则
    /// - 多过程支持：支持发票挂账、实收实付、计提等多种财务过程
    /// - 分类管理：通过编码前缀区分不同业务过程的科目配置
    /// 
    /// 科目编码体系：
    /// - GEN_*：通用科目（如GEN_COGS主营业务成本、GEN_PREPARER制单人）
    /// - PBI_*：发票挂账科目（如PBI_SALES_REVENUE主营业务收入）
    /// - RF_*：实收科目（如RF_BANK_DEPOSIT银行存款）
    /// - PF_*：实付科目（如PF_ACC_PAYABLE应付账款）
    /// - ARAB_*：A账应收计提科目（如ARAB_DOMESTIC_NON_ADVANCE国内非代垫应收）
    /// - APAB_*：A账应付计提科目（如APAB_FOREIGN_ADVANCE国外代垫应付）
    /// 
    /// 使用场景：
    /// - 凭证自动生成：根据业务数据和科目配置自动生成财务凭证
    /// - 财务系统对接：为DBF文件生成提供科目编码映射
    /// - 多组织支持：不同组织可配置不同的科目体系
    /// - 审计追踪：记录科目配置的创建和修改历史
    /// </summary>
    [Comment("财务科目设置表")]
    [Index(nameof(OrgId), nameof(Code), IsUnique = true)]
    public class SubjectConfiguration : GuidKeyObjectBase, ISpecificOrg, IMarkDelete, ICreatorInfo
    {
        /// <summary>
        /// 所属组织机构Id
        /// 
        /// 多租户隔离说明：
        /// - 支持不同组织配置独立的财务科目体系
        /// - 与OrgId结合的唯一索引确保同组织内编码唯一性
        /// - 为null时表示系统级别的通用配置
        /// </summary>
        [Comment("所属组织机构Id")]
        public Guid? OrgId { get; set; }

        /// <summary>
        /// 服务器用此编码来标识该数据用于什么地方。支持以下配置项编码（可能持续增加）：
        /// 
        /// 通用科目 (GEN_前缀) - 公共科目配置：
        /// - GEN_COGS: 主营业务成本
        /// - GEN_ADVANCE_PAYMENT: 代垫项（代收代付科目）
        /// - GEN_PREPARER: 制单人（金蝶制单人名称）
        /// - GEN_TOTAL_RECEIVABLE: 计提总应收（科目代码：531）
        /// - GEN_TOTAL_PAYABLE: 计提总应付（科目代码：532）
        /// 
        /// 发票挂账科目 (PBI_前缀) - POST Bill Invoice：
        /// - PBI_SALES_REVENUE: 主营业务收入
        /// - PBI_TAX_PAYABLE: 应交税金
        /// - PBI_ACC_RECEIVABLE: 应收账款
        /// 
        /// 实收科目 (RF_前缀) - Receive Funds：
        /// - RF_BANK_DEPOSIT: 银行存款（收款银行存款）
        /// - RF_ACC_RECEIVABLE: 应收账款（冲销应收）
        /// 
        /// 实付科目 (PF_前缀) - Pay Funds：
        /// - PF_BANK_DEPOSIT: 银行存款（付款银行存款）
        /// - PF_ACC_PAYABLE: 应付账款
        /// 
        /// A账应收计提科目 (ARAB_前缀) - Accrue Receivable A-account Base Currency：
        /// - ARAB_DOMESTIC_NON_ADVANCE: 非代垫国内客户计提应收（科目代码：113.001.01）
        /// - ARAB_DOMESTIC_ADVANCE: 代垫国内客户计提应收（科目代码：113.001.02）
        /// - ARAB_FOREIGN_NON_ADVANCE: 非代垫国外客户计提应收（科目代码：113.002）
        /// - ARAB_FOREIGN_ADVANCE: 代垫国外客户计提应收（科目代码：预留）
        /// 
        /// A账应付计提科目 (APAB_前缀) - Accrue Payable A-account Base Currency：
        /// - APAB_DOMESTIC_NON_ADVANCE: 非代垫国内客户计提应付（科目代码：203.001.01）
        /// - APAB_DOMESTIC_ADVANCE: 代垫国内客户计提应付（科目代码：203.001.02）
        /// - APAB_FOREIGN_NON_ADVANCE: 非代垫国外客户计提应付（科目代码：203.002）
        /// - APAB_FOREIGN_ADVANCE: 代垫国外客户计提应付（科目代码：预留）
        /// 
        /// 编码规则：
        /// - 子项CODE = 过程CODE前缀 + 业务缩写
        /// - 新增时须使用对应过程CODE前缀或GEN_，并保证在科目配置表内唯一
        /// - 支持未来扩展新的财务过程和科目类型
        /// </summary>
        [Comment("配置项编码")]
        [MaxLength(32), Unicode(false)]
        [Required(AllowEmptyStrings = false)]
        public string Code { get; set; }

        /// <summary>
        /// 会计科目编码。
        /// 
        /// 对应金蝶财务系统中的科目编码，用于DBF文件生成时的FAcctID字段。
        /// 示例：113.001.01（国内非代垫应收）、203.002（国外应付）等。
        /// </summary>
        [Comment("会计科目编码")]
        [MaxLength(32), Unicode(false)]
        public string SubjectNumber { get; set; }

        /// <summary>
        /// 显示名称。
        /// 
        /// 用于界面显示的友好名称，如"主营业务收入"、"银行存款"等。
        /// 在凭证摘要生成时可能会用到此字段。
        /// </summary>
        [Comment("显示名称。")]
        [MaxLength(128)]
        public string DisplayName { get; set; }

        /// <summary>
        /// 凭证类别字。
        /// 
        /// 用于DBF文件生成时的Fgroup字段，标识凭证类型：
        /// - "转"：转账凭证（如计提类凭证）
        /// - "收"：收款凭证（如实收类凭证）
        /// - "付"：付款凭证（如实付类凭证）
        /// - "记"：记账凭证（如其他业务凭证）
        /// 
        /// 在同一凭证内必须保持一致。
        /// </summary>
        [Comment("凭证类别字")]
        [MaxLength(8)]
        public string VoucherGroup { get; set; }

        /// <summary>
        /// 核算类别。
        /// 
        /// 用于DBF文件生成时的FCLSNAME1字段，标识核算维度：
        /// - "客户"：按客户核算的科目
        /// - "供应商"：按供应商核算的科目
        /// - "部门"：按部门核算的科目
        /// - "员工"：按员工核算的科目
        /// - "项目"：按项目核算的科目
        /// - "地区"：按地区核算的科目
        /// 
        /// 在同一凭证内的相关分录必须保持一致。
        /// </summary>
        [Comment("核算类别")]
        [MaxLength(8)]
        public string AccountingCategory { get; set; }

        /// <summary>
        /// 制单人（金蝶制单人名称）
        /// 
        /// 用于DBF文件生成时的FPREPARE字段，标识凭证制单人。
        /// 通常配置在GEN_PREPARER科目中，所有凭证使用统一的制单人。
        /// </summary>
        [Comment("制单人（金蝶制单人名称）")]
        [MaxLength(64)]
        public string Preparer { get; set; }

        /// <summary>
        /// 备注
        /// 
        /// 用于记录科目配置的详细说明、使用场景、注意事项等信息。
        /// 如科目的具体用途、计算规则、特殊处理要求等。
        /// </summary>
        [Comment("备注")]
        public string Remark { get; set; }

        #region IMarkDelete
        /// <summary>
        /// 是否已标记为删除。false(默认)未标记为删除，true标记为删除。
        /// 
        /// 软删除机制：
        /// - 财务科目配置通常不能物理删除，以保证历史凭证的完整性
        /// - 标记删除后不再显示在配置列表中，但历史数据仍然有效
        /// - 支持恢复操作，取消删除标记
        /// </summary>
        [Comment("是否已标记为删除。false(默认)未标记为删除，true标记为删除。")]
        public bool IsDelete { get; set; }
        #endregion

        #region ICreatorInfo
        /// <summary>
        /// 创建者的唯一标识
        /// 
        /// 审计追踪：
        /// - 记录科目配置的创建人员，用于权限控制和审计
        /// - 支持查询某个用户创建的所有科目配置
        /// - 配合权限系统控制科目配置的修改权限
        /// </summary>
        [Comment("创建者的唯一标识")]
        public Guid? CreateBy { get; set; }

        /// <summary>
        /// 创建的时间
        /// 
        /// 时间精度说明：
        /// - 使用Precision(3)支持毫秒级精度
        /// - 默认值为系统当前时间（OwHelper.WorldNow）
        /// - 用于审计追踪和版本控制
        /// </summary>
        [Comment("创建的时间")]
        [Precision(3)]
        public DateTime CreateDateTime { get; set; } = OwHelper.WorldNow;
        #endregion IMarkDelete
    }
}