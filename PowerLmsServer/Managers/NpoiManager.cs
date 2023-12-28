using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using NPOI.HSSF.UserModel;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.Util;
using NPOI.XSSF.Streaming.Values;
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
using OW.Data;

namespace PowerLmsServer.Managers
{
    /// <summary>
    /// NOPI基础代码辅助管理器。
    /// </summary>
    [OwAutoInjection(ServiceLifetime.Singleton)]
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
                        CellType.Blank => default,
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
            context.AddOrUpdate(list as IEnumerable<T>);
        }

        /// <summary>
        /// 将数据集合写入excel表。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">可以是空集合，此时仅写入表头。</param>
        /// <param name="columnNames"></param>
        /// <param name="sheet"></param>
        public void WriteToExcel<T>(IEnumerable<T> collection, string[] columnNames, ISheet sheet)
        {
            if (!collection.TryGetNonEnumeratedCount(out var rowCount))
                rowCount = collection.Count();//行数  
            int columnCount = columnNames.Length;//列数  
            var mapping = columnNames.Select(c => (c, typeof(T).GetProperty(c))).ToArray();

            foreach (var obj in collection)
            {
                foreach (var pi in mapping)
                {
                    pi.Item2.GetValue(obj, null);
                }
            }
            int i;
            //设置列头  
            var row = sheet.CreateRow(0);//excel第一行设为列头  
            for (i = 0; i < columnCount; i++)
            {
                var cell = row.CreateCell(i);
                cell.SetValue(mapping[i].c);
            }
            //设置每行每列的单元格
            i = 0;
            foreach (var item in collection)
            {
                row = sheet.CreateRow(i + 1);   //excel第二行开始写入数据
                for (int j = 0; j < columnCount; j++)
                {
                    var cell = row.CreateCell(j);   //cell.SetCellValue(dt.Rows[i][j].ToString());
                    var obj = mapping[j].Item2.GetValue(item);    //取属性值

                    cell.SetValue(obj);
                }
                i++;
            }
        }
    }

    /// <summary>
    /// 扩展方法封装类。
    /// </summary>
    public static class NpoiManagerExtensions
    {
        /// <summary>
        /// 设置单元格的值。
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="val"></param>
        public static void SetValue(this NPOI.SS.UserModel.ICell cell, object val)
        {
            switch (Type.GetTypeCode(val.GetType()))
            {
                case TypeCode.Empty:
                    cell.SetBlank();
                    break;
                case TypeCode.Object:
                    if (val is null)
                        cell.SetBlank();
                    else if (val is Guid guid)
                        cell.SetCellValue(guid.ToString());
                    break;
                case TypeCode.DBNull:
                    cell.SetBlank();
                    break;
                case TypeCode.Boolean:
                    cell.SetCellValue((bool)val);
                    break;
                case TypeCode.Char:
                    cell.SetCellValue(val.ToString());
                    break;
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    cell.SetCellValue(Convert.ToDouble(val));
                    break;
                case TypeCode.DateTime:
                    cell.SetCellValue((DateTime)val);
                    break;
                case TypeCode.String:
                    cell.SetCellValue(val as string);
                    break;
                default:
                    break;
            }
        }
    }
}
