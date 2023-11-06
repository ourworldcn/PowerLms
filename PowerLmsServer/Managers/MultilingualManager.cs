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
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="db"></param>
        public void Import(Stream stream, PowerLmsUserDbContext db)
        {
            IWorkbook workbook = WorkbookFactory.Create(stream);
            var sheet = workbook.GetSheetAt(0);
            db.TruncateTable("[dbo].[LanguageDataDics]");
            
            var dt = _NpoiManager.ReadExcelFunc(workbook, sheet);
            _NpoiManager.WriteDb<LanguageDataDic>(dt, db, nameof(db.LanguageDataDics), default, default);
        }

    }
}
