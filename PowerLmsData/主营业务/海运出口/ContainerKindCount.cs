/*
 * 项目：PowerLms | 模块：海运业务数据实体
 * 功能：箱型箱量子表
 * 业务背景：
 *   记录海运出口/进口业务中使用的集装箱类型和数量。
 *   一个业务单据可以包含多种箱型（如20GP、40HQ等）。
 * 技术要点：
 *   - 子表实体，通过ParentId关联主单据
 *   - 实现IOwSubtables接口，支持EF Helper的子表批量操作
 * 作者：zc | 创建：2024-07-30
 */

using Microsoft.EntityFrameworkCore;
using OW.Data;
using System;
using System.ComponentModel.DataAnnotations;

namespace PowerLms.Data
{
    /// <summary>
    /// 箱型箱量子表（海运业务通用）。
    /// 记录海运业务单据使用的集装箱类型和数量。
    /// </summary>
    [Comment("箱型箱量子表")]
    [Index(nameof(ParentId), IsUnique = false)]
    public class ContainerKindCount : GuidKeyObjectBase, IOwSubtables
    {
        /// <summary>
        /// 所属业务单据Id。
        /// 关联PlEsDoc（海运出口单）或PlIsDoc（海运进口单）。
        /// </summary>
        [Comment("所属业务单据Id")]
        public Guid? ParentId { get; set; }

        /// <summary>
        /// 箱型。
        /// 常见值：20GP、40GP、40HQ、45HQ等，引用自ShippingContainersKind字典。
        /// </summary>
        [Comment("箱型")]
        [MaxLength(64)]
        public string Kind { get; set; }

        /// <summary>
        /// 数量。
        /// 该箱型的集装箱数量。
        /// </summary>
        [Comment("数量")]
        public int Count { get; set; }
    }
}
