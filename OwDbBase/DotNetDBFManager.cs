/*
 * 文件名：DotNetDBFManager.cs
 * 作者：OW
 * 创建日期：2023年10月25日
 * 修改日期：2023年10月25日
 * 描述：该文件包含 DotNetDBFManager 类的实现，用于操作 DBF 文件。
 */

using System;
using System.Data;
using System.IO;
using DotNetDBF;

namespace OW.Data
{
    /// <summary>
    /// DotNetDBFManager 类，用于操作 DBF 文件。
    /// </summary>
    public class DotNetDBFManager
    {
        /// <summary>
        /// 从 DBF 文件读取数据到 DataTable。
        /// </summary>
        /// <param name="filePath">DBF 文件路径。</param>
        /// <returns>包含 DBF 数据的 DataTable。</returns>
        public DataTable ReadDBFToDataTable(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));

            var dataTable = new DataTable();

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            using (var reader = new DBFReader(stream))
            {
                reader.CharEncoding = System.Text.Encoding.UTF8;
                var fields = reader.Fields;

                // 创建 DataTable 列
                foreach (var field in fields)
                {
                    dataTable.Columns.Add(field.Name, GetFieldTypeFromDBF(field.DataType));
                }

                // 读取数据
                object[] record;
                while ((record = reader.NextRecord()) != null)
                {
                    var dataRow = dataTable.NewRow();
                    for (int i = 0; i < fields.Length; i++)
                    {
                        dataRow[fields[i].Name] = record[i];
                    }
                    dataTable.Rows.Add(dataRow);
                }
            }

            return dataTable;
        }

        /// <summary>
        /// 将 DataTable 写入 DBF 文件。
        /// </summary>
        /// <param name="filePath">DBF 文件路径。</param>
        /// <param name="dataTable">要写入的 DataTable。</param>
        public void WriteDataTableToDBF(string filePath, DataTable dataTable)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));
            if (dataTable == null || dataTable.Columns.Count == 0)
                throw new ArgumentNullException(nameof(dataTable));

            using (var stream = File.Open(filePath, FileMode.Create, FileAccess.Write))
            using (var writer = new DBFWriter(stream))
            {
                writer.CharEncoding = System.Text.Encoding.UTF8;

                // 获取字段信息
                var fields = new DBFField[dataTable.Columns.Count];
                for (int i = 0; i < dataTable.Columns.Count; i++)
                {
                    var column = dataTable.Columns[i];
                    var fieldType = GetDBFFieldType(column.DataType);
                    fields[i] = new DBFField(column.ColumnName, fieldType);
                }

                writer.Fields = fields;

                // 写入数据
                foreach (DataRow row in dataTable.Rows)
                {
                    var recordData = new object[fields.Length];
                    for (int i = 0; i < fields.Length; i++)
                    {
                        recordData[i] = row[i];
                    }
                    writer.AddRecord(recordData);
                }
            }
        }

        /// <summary>
        /// 获取 DBF 字段类型。
        /// </summary>
        /// <param name="type">字段类型。</param>
        /// <returns>DBF 字段类型。</returns>
        private NativeDbType GetDBFFieldType(Type type)
        {
            if (type == typeof(string))
                return NativeDbType.Char;
            if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte))
                return NativeDbType.Numeric;
            if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
                return NativeDbType.Float;
            if (type == typeof(DateTime))
                return NativeDbType.Date;
            if (type == typeof(bool))
                return NativeDbType.Logical;

            throw new ArgumentException("不支持的数据类型");
        }

        /// <summary>
        /// 根据 DBF 字段类型获取 DataTable 列的类型。
        /// </summary>
        /// <param name="dbfType">DBF 字段类型。</param>
        /// <returns>DataTable 列的类型。</returns>
        private Type GetFieldTypeFromDBF(NativeDbType dbfType)
        {
            return dbfType switch
            {
                NativeDbType.Char => typeof(string),
                NativeDbType.Numeric => typeof(decimal),
                NativeDbType.Float => typeof(double),
                NativeDbType.Date => typeof(DateTime),
                NativeDbType.Logical => typeof(bool),
                _ => throw new ArgumentException("不支持的数据类型"),
            };
        }
    }
}



