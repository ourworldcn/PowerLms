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
    }
}
