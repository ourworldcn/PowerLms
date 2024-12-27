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

    }
}
