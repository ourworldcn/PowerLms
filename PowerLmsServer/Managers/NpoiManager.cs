using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.ObjectPool;
using NPOI.SS.UserModel;
using NPOI.Util;
using NPOI.XWPF.UserModel;
using PowerLms.Data;
using PowerLmsServer.EfData;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
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
        /// 从流获取Excel工作表。
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public IWorkbook GetWorkbookFromStream(Stream stream)
        {
            IWorkbook workbook = WorkbookFactory.Create(stream);
            return workbook;
        }

        /// <summary>
        /// 获取指定Excel工作表的列名。
        /// </summary>
        /// <param name="sheet"></param>
        /// <returns></returns>
        public List<(string, int)> GetColumnName(ISheet sheet)
        {
            IRow cells = sheet.GetRow(sheet.FirstRowNum);
            int cellsCount = cells.PhysicalNumberOfCells;
            int emptyCount = 0;
            int cellIndex = sheet.FirstRowNum;
            List<(string, int)> listColumns = new List<(string, int)> { };
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
                    listColumns.Add((cells.GetCell(i).StringCellValue, i));
                }
                //这里根据逻辑需要，空列超过多少判断
                if (emptyCount == 0)
                {
                    isFindColumn = true;
                }
                cellIndex++;
                cells = sheet.GetRow(cellIndex);
            }
            return listColumns;
        }

        /// <summary>
        /// 将表转换为Json。
        /// </summary>
        /// <param name="sheet"></param>
        /// <returns></returns>
        public string GetJson(ISheet sheet)
        {
            var cols = GetColumnName(sheet);
            List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
            for (int rowIndex = 1; rowIndex < sheet.PhysicalNumberOfRows; rowIndex++)
            {
                var row = sheet.GetRow(rowIndex);
                Dictionary<string, object> dic = new Dictionary<string, object>();
                for (int columnIndex = 0; columnIndex < cols.Count; columnIndex++)
                {
                    var cell = row.GetCell(columnIndex);
                    dic[cols[columnIndex].Item1] = cell?.CellType switch
                    {
                        CellType.String => cell.StringCellValue,
                        CellType.Numeric => cell.NumericCellValue,
                        CellType.Boolean => cell.BooleanCellValue,
                        null => null,
                        _ => cell.StringCellValue,
                    };
                }
                list.Add(dic);
            }
            var str = JsonSerializer.Serialize(list);
            return str;
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

        /// <summary>
        /// 将指定表复制到数据库指定表中。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dt"></param>
        /// <param name="context"></param>
        /// <param name="destinationTableName"></param>
        public void WriteToDb<T>(DataTable dt, DbContext context, string destinationTableName) where T : class
        {
            SqlBulkCopy bulkCopy = new SqlBulkCopy(context.Database.GetConnectionString(), SqlBulkCopyOptions.KeepIdentity);
            bulkCopy.DestinationTableName = destinationTableName;
            bulkCopy.BatchSize = Math.Max(2000, dt.Rows.Count);
            try
            {
                bulkCopy.WriteToServer(dt); //"6ae3bbb3-bac9-4509-bf82-c8578830cd24"
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 将表写入指定的对象集合。需要调用<see cref="DbContext.SaveChanges()"/>才会写入数据库。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sheet"></param>
        /// <param name="context"></param>
        /// <param name="destSet"></param>
        public void WriteToDb<T>(ISheet sheet, DbContext context, DbSet<T> destSet) where T : class
        {
            var str = GetJson(sheet);
            var list = JsonSerializer.Deserialize<List<T>>(str);
            context.InsertOrUpdate(list as IEnumerable<T>);
        }

    }
}
