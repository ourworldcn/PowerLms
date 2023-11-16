using Microsoft.Extensions.DependencyInjection;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.Util;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// 多语言管理器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
    public class MultilingualManager
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public MultilingualManager(NpoiManager npoiManager)
        {
            _NpoiManager = npoiManager;
        }

        NpoiManager _NpoiManager;
        
    }
}
