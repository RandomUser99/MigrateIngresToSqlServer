using System;
using System.Collections.Generic;
using System.Data;

namespace MigrateIngresToSqlServer
{
    public static class DataColumnFactory
    {
        public static DataColumn Get(KeyValuePair<string, string> field)
        {
            if (field.Value == "smallint")
            {
                return new DataColumn(field.Key, typeof(int)) {AllowDBNull = true};
            }

            if (field.Value.StartsWith("varchar(") | field.Value.StartsWith("char("))
            {
                return new DataColumn(field.Key, typeof(string)) {AllowDBNull = true, MaxLength = 500};
            }

            if (field.Value == "date")
            {
                return new DataColumn(field.Key, typeof(DateTime)) {AllowDBNull = true};
            }

            if (field.Value == "integer")
            {
                return new DataColumn(field.Key, typeof(int)) {AllowDBNull = true};
            }

            if (field.Value == "money" | field.Value == "float")
            {
                return new DataColumn(field.Key, typeof(decimal)) {AllowDBNull = true};
            }

            return new DataColumn(field.Key, typeof(string)) { AllowDBNull = true, MaxLength = 500 };
        }
    }
}
