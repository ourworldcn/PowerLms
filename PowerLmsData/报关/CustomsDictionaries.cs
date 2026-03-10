/*
/*
 * 项目：PowerLms | 模块：报关
 * 功能：报关专用字典实体（HSCODE、检疫代码、行政区划、国内口岸、检疫地区、报关港口）
 * 技术要点：独立字典表，字段严格按设计文档定义，支持多租户
 * 作者：zc | 创建：2026-03
 */
using Microsoft.EntityFrameworkCore;
using OW.Data;
using System;
using System.ComponentModel.DataAnnotations;

namespace PowerLms.Data
{
    /// <summary>
    /// 报关HSCODE基础表。
    /// </summary>
    [Comment("报关HSCODE基础表")]
    public class CdHsCode : GuidKeyObjectBase
    {
        /// <summary>
        /// 所属机构Id（多租户）。
        /// </summary>
        [Comment("所属机构Id")]
        public Guid? OrgId { get; set; }
        /// <summary>
        /// 货物编号（HSCODE）。
        /// </summary>
        [Comment("货物编号（HSCODE）")]
        [MaxLength(20)]
        public string HsCode { get; set; }
        /// <summary>
        /// 货物描述。
        /// </summary>
        [Comment("货物描述")]
        [MaxLength(255)]
        public string GoodsDesc { get; set; }
        /// <summary>
        /// 监管条件。
        /// </summary>
        [Comment("监管条件")]
        [MaxLength(32)]
        public string ControlMa { get; set; }
        /// <summary>
        /// 备注（大文本）。
        /// </summary>
        [Comment("备注")]
        public string NoteS { get; set; }
        /// <summary>
        /// 单位1。
        /// </summary>
        [Comment("单位1")]
        [MaxLength(32)]
        public string Unit1 { get; set; }
        /// <summary>
        /// 单位2。
        /// </summary>
        [Comment("单位2")]
        [MaxLength(32)]
        public string Unit2 { get; set; }
        /// <summary>
        /// 申报要素备注。
        /// </summary>
        [Comment("申报要素备注")]
        [MaxLength(255)]
        public string Remark { get; set; }
    }

    /// <summary>
    /// 报关CIQCODE检疫代码表。
    /// </summary>
    [Comment("报关CIQCODE检疫代码表")]
    public class CdGoodsVsCiqCode : GuidKeyObjectBase
    {
        /// <summary>
        /// 所属机构Id（多租户）。
        /// </summary>
        [Comment("所属机构Id")]
        public Guid? OrgId { get; set; }
        /// <summary>
        /// HS编码。
        /// </summary>
        [Comment("HS编码")]
        [MaxLength(20)]
        public string HsCode { get; set; }
        /// <summary>
        /// CIQ分类名称。
        /// </summary>
        [Comment("CIQ分类名称")]
        [MaxLength(255)]
        public string CiqName { get; set; }
        /// <summary>
        /// HS名称。
        /// </summary>
        [Comment("HS名称")]
        [MaxLength(255)]
        public string HsName { get; set; }
    }

    /// <summary>
    /// 国内行政区划表。
    /// </summary>
    [Comment("国内行政区划表")]
    public class CdPlace : GuidKeyObjectBase
    {
        /// <summary>
        /// 所属机构Id（多租户）。
        /// </summary>
        [Comment("所属机构Id")]
        public Guid? OrgId { get; set; }
        /// <summary>
        /// 编码。
        /// </summary>
        [Comment("编码")]
        [MaxLength(50)]
        public string Code { get; set; }
        /// <summary>
        /// 中文名。
        /// </summary>
        [Comment("中文名")]
        [MaxLength(255)]
        public string Cname { get; set; }
        /// <summary>
        /// 英文名。
        /// </summary>
        [Comment("英文名")]
        [MaxLength(255)]
        public string Ename { get; set; }
    }

    /// <summary>
    /// 国内口岸代码表。
    /// </summary>
    [Comment("国内口岸代码表")]
    public class CdDomesticPort : GuidKeyObjectBase
    {
        /// <summary>
        /// 所属机构Id（多租户）。
        /// </summary>
        [Comment("所属机构Id")]
        public Guid? OrgId { get; set; }
        /// <summary>
        /// 编码。
        /// </summary>
        [Comment("编码")]
        [MaxLength(50)]
        public string Code { get; set; }
        /// <summary>
        /// 中文名。
        /// </summary>
        [Comment("中文名")]
        [MaxLength(255)]
        public string Cname { get; set; }
        /// <summary>
        /// 英文名。
        /// </summary>
        [Comment("英文名")]
        [MaxLength(255)]
        public string Ename { get; set; }
    }

    /// <summary>
    /// 国内地区代码（检疫用）表。
    /// </summary>
    [Comment("国内地区代码（检疫用）表")]
    public class CdInspectionPlace : GuidKeyObjectBase
    {
        /// <summary>
        /// 所属机构Id（多租户）。
        /// </summary>
        [Comment("所属机构Id")]
        public Guid? OrgId { get; set; }
        /// <summary>
        /// 编码。
        /// </summary>
        [Comment("编码")]
        [MaxLength(50)]
        public string Code { get; set; }
        /// <summary>
        /// 中文名。
        /// </summary>
        [Comment("中文名")]
        [MaxLength(255)]
        public string Cname { get; set; }
        /// <summary>
        /// 英文名。
        /// </summary>
        [Comment("英文名")]
        [MaxLength(255)]
        public string Ename { get; set; }
    }

    /// <summary>
    /// 报关专用港口表。与通用港口PlPort不同，使用海关特定代码体系。
    /// </summary>
    [Comment("报关专用港口表，使用海关特定代码体系")]
    public class CdPort : GuidKeyObjectBase
    {
        /// <summary>
        /// 所属机构Id（多租户）。
        /// </summary>
        [Comment("所属机构Id")]
        public Guid? OrgId { get; set; }
        /// <summary>
        /// 编码。
        /// </summary>
        [Comment("编码")]
        [MaxLength(50)]
        public string Code { get; set; }
        /// <summary>
        /// 中文名。
        /// </summary>
        [Comment("中文名")]
        [MaxLength(255)]
        public string Cname { get; set; }
        /// <summary>
        /// 英文名。
        /// </summary>
        [Comment("英文名")]
        [MaxLength(255)]
        public string Ename { get; set; }
        /// <summary>
        /// 国家代码。
        /// </summary>
        [Comment("国家代码")]
        [MaxLength(50)]
        public string CountryCode { get; set; }
    }
}