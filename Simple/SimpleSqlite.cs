using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Simple
{
    public class SimpleSqlite
    {
        private string _connectionName;
        private ConnectionStringSettings _connectionString = new ConnectionStringSettings();
        private List<SQLiteParameter> _sqlParameters = new List<SQLiteParameter>();
        private Dictionary<string, int> _fields = new Dictionary<string, int>();

        public int Timeout { get; set; }

        public SimpleSqlite(string connectionName)
        {
            _connectionString = ConfigurationManager.ConnectionStrings[connectionName];
        }

        public SimpleSqlite(ConnectionStringSettings connectionStringSetting)
        {
            _connectionString = connectionStringSetting;
        }

        public string ConnectionName
        {
            get { return _connectionName; }
            set
            {
                _connectionName = value;
                try
                {
                    _connectionString = ConfigurationManager.ConnectionStrings[value];
                    var provider = _connectionString.ProviderName.ToUpper();
                }
                catch
                { }
            }
        }

        public ConnectionStringSettings ConnectionString
        {
            get { return _connectionString; }
            set
            {
                _connectionString = value;
                _connectionName = _connectionString.Name;
            }
        }

        private List<SQLiteParameter> Parameters
        {
            get { return _sqlParameters; }
        }

        public void AddParameter(string name, object value)
        {
            if (_connectionString == null)
            {
                _sqlParameters.Add(new SQLiteParameter(name, value));
                return;
            }
            var provider = _connectionString.ProviderName.ToUpper();
            _sqlParameters.Add(new SQLiteParameter(name, value));
        }

        public void Clear()
        {
            _sqlParameters.Clear();
        }

        #region Execute Reader
        public List<T> ExecuteReader<T>(string queryText)
        {
            var item = ExecuteReaderAsync<T>(queryText, CancellationToken.None);
            item.Wait();
            return item.Result;
        }

        public List<Dictionary<string, object>> ExecuteReader(string queryText)
        {
            var item = ExecuteReaderAsync(queryText, CancellationToken.None);
            item.Wait();
            return item.Result;
        }


        #endregion
        #region Execute Reader Async
        public async Task<List<T>> ExecuteReaderAsync<T>(string queryText)
        {
            return await ExecuteReaderAsync<T>(queryText, CancellationToken.None);
        }

        public async Task<List<T>> ExecuteReaderAsync<T>(string queryText, CancellationToken cancellationToken)
        {
            using (SQLiteConnection connection = new SQLiteConnection(_connectionString.ConnectionString))
            {
                using (SQLiteCommand command = new SQLiteCommand(queryText, connection))
                {
                    command.CommandTimeout = Timeout;

                    if (Parameters.Count != 0)
                    {
                        foreach (var param in Parameters)
                            command.Parameters.Add(param);
                    }

                    await connection.OpenAsync(cancellationToken);

                    using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                        var result = await GetValues<T>(reader, cancellationToken);
                        reader.Close();
                        command.Parameters.Clear();
                        connection.Close();
                        return result;
                    }
                }
            }
        }
        public async Task<List<Dictionary<string, object>>> ExecuteReaderAsync(string queryText, CancellationToken cancellationToken)
        {
            using (SQLiteConnection connection = new SQLiteConnection(_connectionString.ConnectionString))
            {
                using (SQLiteCommand command = new SQLiteCommand(queryText, connection))
                {
                    command.CommandTimeout = Timeout;

                    if (Parameters.Count != 0)
                    {
                        foreach (var param in Parameters)
                            command.Parameters.Add(param);
                    }

                    await connection.OpenAsync(cancellationToken);

                    using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                        var result = await GetDictionaryValues(reader, cancellationToken);
                        reader.Close();
                        command.Parameters.Clear();
                        connection.Close();
                        return result;
                    }
                }
            }
        }
        public async Task<List<Dictionary<string, object>>> ExecuteReaderAsync(string queryText)
        {
            return await ExecuteReaderAsync(queryText, CancellationToken.None);
        }
        #endregion
        #region Execute Command
        public int ExecuteCommand(string queryText)
        {
            var item = ExecuteCommandAsync(queryText, CancellationToken.None);
            item.Wait();
            return item.Result;
        }
        #endregion
        #region Execute Command Async
        public async Task<int> ExecuteCommandAsync(string queryText)
        {
            return await ExecuteCommandAsync(queryText, CancellationToken.None);
        }


        private async Task<int> ExecuteCommandAsync(string queryText, CancellationToken cancellationToken)
        {
            using (SQLiteConnection connection = new SQLiteConnection(_connectionString.ConnectionString))
            {
                using (var transaction = connection.BeginTransaction())
                {

                    using (SQLiteCommand command = new SQLiteCommand(queryText, connection))
                    {
                        command.CommandTimeout = Timeout;

                        command.Transaction = transaction;

                        if (Parameters.Count != 0)
                        {
                            foreach (var param in Parameters)
                                command.Parameters.Add(param);
                        }

                        try
                        {
                            await connection.OpenAsync(cancellationToken);

                            var result = await command.ExecuteNonQueryAsync(cancellationToken);
                            transaction.Commit();
                            command.Parameters.Clear();
                            connection.Close();
                            return result;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
        }
        #endregion
        #region Mapping Functions
        private async Task<List<Dictionary<string, object>>> GetDictionaryValues(DbDataReader reader, CancellationToken token)
        {
            var values = new List<Dictionary<string, object>>();

            var fieldCount = reader.FieldCount;
            while (await reader.ReadAsync())
            {
                var dict = new Dictionary<string, object>();
                try
                {
                    for (int i = 0; i < fieldCount; i++)
                    {
                        var field = reader.GetValue(i);
                        if (field != DBNull.Value)
                            dict.Add(reader.GetName(i), field);
                        else
                            dict.Add(reader.GetName(i), null);
                    }
                    values.Add(dict);
                }
                catch (Exception ex)
                {

                }
            }
            return values;
        }
        private async Task<List<T>> GetValues<T>(DbDataReader reader, CancellationToken token)
        {
            var list = new List<T>();

            while (await reader.ReadAsync())
            {
                var type = typeof(T);
                var fields = type.GetProperties().OrderBy(x => x.MetadataToken);
                var row = (T)Activator.CreateInstance(type);
                foreach (PropertyInfo info in fields)
                {
                    var attributes = info.GetCustomAttributes(false).ToList();
                    var skip = false;
                    var columnName = info.Name;

                    foreach (var attribute in attributes)
                    {
                        if (attribute.GetType() == typeof(XmlIgnoreAttribute))
                            skip = true;
                    }
                    if (skip)
                        continue;
                    try
                    {
                        info.SetValue(row, reader[columnName], null);
                        
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                }
                list.Add(row);
            }
            return list;
        }
        #endregion


    }
}
