using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLmsServer
{
    /// <summary>
    /// 封装一些项目内用常量或只读数据。
    /// </summary>
    public static class ProjectContent
    {
        /// <summary>
        /// air export(空运出口)业务类型Id。
        /// </summary>
        public static readonly Guid AeId = Guid.Parse("7D4123A5-BF7C-4960-80DA-7D1C112F6949");

        /// <summary>
        /// air import (空运进口)业务类型Id。
        /// </summary>
        public static readonly Guid AiId = Guid.Parse("1E4FF925-6492-4E4A-BFB6-E361305E5EAF");

        /// <summary>
        /// sea export (海运出口)业务类型Id。
        /// </summary>
        public static readonly Guid SeId = Guid.Parse("E31A506D-6FEE-4C0B-BDC3-87D691DDAE22");

        /// <summary>
        /// sea import (海运进口)业务类型Id。
        /// </summary>
        public static readonly Guid SiId = Guid.Parse("D061F777-5B2B-4D22-A982-E883B0AE89A8");

        /// <summary>
        /// land export(陆地出口)类型Id。
        /// </summary>
        public static readonly Guid JeId = Guid.Parse("F0E8FCE0-2BC6-4EA5-8AF7-F025EFD0AF62");

        /// <summary>
        /// land import(陆地进口)类型Id。
        /// </summary>
        public static readonly Guid JiId = Guid.Parse("90BA6C07-476D-4A64-9C75-09655EAE177A");

        /// <summary>
        /// railway export(铁路出口)类型Id。
        /// </summary>
        public static readonly Guid ReId = Guid.Parse("56875097-8334-4F66-B386-C526F199B77B");

        /// <summary>
        /// railway import(铁路进口)类型Id。
        /// </summary>
        public static readonly Guid RiId = Guid.Parse("614A7BE3-AC2C-4223-A8AC-17C27B02A650");

        /// <summary>
        /// international business(贸易业务)类型Id。
        /// </summary>
        public static readonly Guid OtId = Guid.Parse("C69DCF32-1869-4FFF-8D0C-CC5855B880D2");

        /// <summary>
        /// warehousing and Storage(仓储业务)类型Id。
        /// </summary>
        public static readonly Guid WhId = Guid.Parse("D60B9F6B-70C8-4EAC-9776-08AF7DB4DFCD");
    }
}
