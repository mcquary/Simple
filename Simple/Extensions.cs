using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace SimpleORMSample
{
    public static class Extensions
    {
        public static void ToObjectArray<T>(T item, ref DataRow dr) where T : class
        {
            List<object> _obj = new List<object>();
            var fields = item.GetType().GetProperties().OrderBy(x => x.MetadataToken);
            foreach (PropertyInfo info in fields)
            {
                var attr = info.GetCustomAttributes(false);
                var skip = false;
                foreach (var attribute in attr)
                {
                    if (attribute.GetType() == typeof(XmlIgnoreAttribute))
                        skip = true;
                }
                if (skip)
                    continue;
                var checkValue = (object)info.GetValue(item, null);
                if (checkValue == null)
                    checkValue = DBNull.Value;
                dr.SetField(info.Name, checkValue);
            }
        }

        public static void Save<T>(this T item, string connectionName) where T : class, new()
        {
            var db = new SimpleORM(connectionName);
            db.Save(item);
        }

        public static DataTable CreateFilledDataTable<T>(this T item) where T : class
        {
            using (DataTable dt = new DataTable())
            {
                var type = typeof(T);

                var fields = type.GetProperties().OrderBy(x => x.MetadataToken);
                foreach (PropertyInfo info in fields)
                {
                    var attributes = info.GetCustomAttributes(false);
                    var skip = false;
                    foreach (var attribute in attributes)
                    {
                        if (attribute.GetType() == typeof(XmlIgnoreAttribute))
                            skip = true;
                    }
                    if (skip)
                        continue;
                    DataColumn newColumn;
                    List<DataColumn> primaryKeyList = new List<DataColumn>();
                    // add primary key attributes for tables...
                    if (!info.PropertyType.Name.ToLower().Contains("nullable"))
                    {
                        newColumn = new DataColumn(info.Name, info.PropertyType);
                        if (info.PropertyType == typeof(DateTime))
                            newColumn.DateTimeMode = DataSetDateTime.Unspecified;
                    }
                    else
                    {
                        var newType = Nullable.GetUnderlyingType(info.PropertyType.UnderlyingSystemType);
                        newColumn = new DataColumn(info.Name, newType);
                        newColumn.AllowDBNull = true;
                        if (newType == typeof(DateTime))
                            newColumn.DateTimeMode = DataSetDateTime.Unspecified;
                    }

                    foreach (var key in attributes)
                    {
                        if (key.GetType() == typeof(PrimaryKeyAttribute))
                        {
                            primaryKeyList.Add(newColumn);
                            break;
                        }
                    }
                    dt.Columns.Add(newColumn);
                    if (primaryKeyList.Count > 0)
                        dt.PrimaryKey = primaryKeyList.ToArray();
                }

                var attr = type.GetCustomAttributes(false);
                var tableName = string.Empty;

                foreach (var attribute in attr)
                {
                    if (attribute.GetType() == typeof(TableNameAttribute))
                    {
                        var temp = (TableNameAttribute)attribute;
                        tableName = temp.TableName;
                    }
                }

                //if (tableName == string.Empty)
                //    dt.TableName = SubSonic.Linq.Structure.ImplicitMapping.Plural(type.Name);
                //else
                dt.TableName = tableName;

                DataRow dr = dt.NewRow();
                ToObjectArray(item, ref dr);
                dt.Rows.Add(dr);

                return dt;
            }
        }

        public static DataTable CreateFilledDataTable<T>(this List<T> list) where T : class
        {
            using (DataTable dt = new DataTable())
            {
                var type = typeof(T);
                var fields = type.GetProperties().OrderBy(x => x.MetadataToken);
                foreach (PropertyInfo info in fields)
                {
                    var attributes = info.GetCustomAttributes(false);
                    var skip = false;
                    foreach (var attribute in attributes)
                    {
                        if (attribute.GetType() == typeof(XmlIgnoreAttribute))
                            skip = true;
                    }
                    if (skip)
                        continue;
                    DataColumn newColumn;
                    List<DataColumn> primaryKeyList = new List<DataColumn>();
                    // add primary key attributes for tables...
                    if (!info.PropertyType.Name.ToLower().Contains("nullable"))
                    {
                        newColumn = new DataColumn(info.Name, info.PropertyType);
                        if (info.PropertyType == typeof(DateTime))
                            newColumn.DateTimeMode = DataSetDateTime.Unspecified;
                    }
                    else
                    {
                        var newType = Nullable.GetUnderlyingType(info.PropertyType.UnderlyingSystemType);
                        newColumn = new DataColumn(info.Name, newType);
                        newColumn.AllowDBNull = true;
                        if (newType == typeof(DateTime))
                            newColumn.DateTimeMode = DataSetDateTime.Unspecified;
                    }

                    foreach (var key in attributes)
                    {
                        if (key.GetType() == typeof(PrimaryKeyAttribute))
                        {
                            primaryKeyList.Add(newColumn);
                            break;
                        }
                    }
                    dt.Columns.Add(newColumn);
                    if (primaryKeyList.Count > 0)
                        dt.PrimaryKey = primaryKeyList.ToArray();
                }

                var attr = type.GetCustomAttributes(false);
                var tableName = string.Empty;

                foreach (var attribute in attr)
                {
                    if (attribute.GetType() == typeof(TableNameAttribute))
                    {
                        var temp = (TableNameAttribute)attribute;
                        tableName = temp.TableName;
                    }
                }

                //if (tableName == string.Empty)
                //    dt.TableName = SubSonic.Linq.Structure.ImplicitMapping.Plural(type.Name);
                //else
                dt.TableName = tableName;

                foreach (var item in list)
                {
                    DataRow dr = dt.NewRow();
                    ToObjectArray(item, ref dr);
                    dt.Rows.Add(dr);
                }
                return dt;
            }
        }
    }
}
