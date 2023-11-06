using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NPOI.SS.UserModel;
using NPOI.Util;
using PowerLms.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// NOPI基础代码辅助管理器。
    /// </summary>
    public class NpoiManager
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public NpoiManager()
        {

        }

        /// <summary>
        /// 读取Excel信息
        /// </summary>
        /// <param name="workbook">工作区</param>
        /// <param name="sheet">sheet</param>
        /// <returns></returns>
        public DataTable ReadExcelFunc(IWorkbook workbook, ISheet sheet)
        {
            DataTable dt = new DataTable();
            //获取列信息
            IRow cells = sheet.GetRow(sheet.FirstRowNum);
            int cellsCount = cells.PhysicalNumberOfCells;
            int emptyCount = 0;
            int cellIndex = sheet.FirstRowNum;
            List<string> listColumns = new List<string>();
            bool isFindColumn = false;
            while (!isFindColumn)
            {
                emptyCount = 0;
                listColumns.Clear();
                for (int i = 0; i < cellsCount; i++)
                {
                    if (string.IsNullOrEmpty(cells.GetCell(i).StringCellValue))
                    {
                        emptyCount++;
                    }
                    listColumns.Add(cells.GetCell(i).StringCellValue);
                }
                //这里根据逻辑需要，空列超过多少判断
                if (emptyCount == 0)
                {
                    isFindColumn = true;
                }
                cellIndex++;
                cells = sheet.GetRow(cellIndex);
            }

            foreach (string columnName in listColumns)
            {
                if (dt.Columns.Contains(columnName))
                {
                    //如果允许有重复列名，自己做处理
                    continue;
                }
                dt.Columns.Add(columnName, typeof(string));
            }
            //开始获取数据
            int rowsCount = sheet.PhysicalNumberOfRows;
            cellIndex += 1;
            DataRow dr = null;
            for (int i = cellIndex; i < rowsCount; i++)
            {
                cells = sheet.GetRow(i);
                dr = dt.NewRow();
                for (int j = 0; j < dt.Columns.Count; j++)
                {
                    //这里可以判断数据类型
                    switch (cells.GetCell(j).CellType)
                    {
                        case CellType.String:
                            dr[j] = cells.GetCell(j).StringCellValue;
                            break;
                        case CellType.Numeric:
                            dr[j] = cells.GetCell(j).NumericCellValue.ToString();
                            break;
                        case CellType.Unknown:
                            dr[j] = cells.GetCell(j).StringCellValue;
                            break;
                    }
                }
                dt.Rows.Add(dr);
            }
            return dt;
        }

        public void WriteDb<T>(DataTable dt, DbContext context, string destinationTableName, string idFieldName, Func<DataRow, T> getTCallback) where T : class
        {
            SqlBulkCopy bulkCopy = new SqlBulkCopy(context.Database.GetConnectionString(), SqlBulkCopyOptions.KeepIdentity);
            bulkCopy.DestinationTableName = destinationTableName;
            bulkCopy.BatchSize = Math.Max(2000, dt.Rows.Count);
            
            try
            {
                bulkCopy.WriteToServer(dt);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
