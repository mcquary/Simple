using System;

namespace Simple
{

        [AttributeUsage(AttributeTargets.Property)]
        public class PrimaryKeyAttribute : Attribute
        {
            public PrimaryKeyAttribute() { }
        }

        [AttributeUsage(AttributeTargets.Class)]
        public class TableNameAttribute : Attribute
        {
            public TableNameAttribute(string tableName)
            {
                TableName = tableName;

            }
            public string TableName { get; set; }
        }
    
}
