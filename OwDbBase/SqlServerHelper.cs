using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OwDbBase
{
    public static class SqlServerHelper
    {
        public static string GetCompressionSql(ITableIndex index)
        {
            return $"ALTER INDEX {index.Name} ON {index.Table.Name} REBUILD PARTITION = ALL WITH (DATA_COMPRESSION = PAGE)";
        }

        public static string GetCompressionSql(ITable table)
        {
            return $"ALTER TABLE {table.Name} REBUILD PARTITION = ALL WITH (DATA_COMPRESSION = PAGE)";
        }
    }
}
