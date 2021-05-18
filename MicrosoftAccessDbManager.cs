using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using console_middleware.models;
using System.Net;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace console_middleware.DataSourceManagers
{
    public class ConnectToSharedFolder : IDisposable
    {
        readonly string _networkName;

        public ConnectToSharedFolder(string networkName, NetworkCredential credentials)
        {
            _networkName = networkName;

            var netResource = new NetResource
            {
                Scope = ResourceScope.GlobalNetwork,
                ResourceType = ResourceType.Disk,
                DisplayType = ResourceDisplaytype.Share,
                RemoteName = networkName
            };

            var userName = string.IsNullOrEmpty(credentials.Domain)
                ? credentials.UserName
                : string.Format(@"{0}\{1}", credentials.Domain, credentials.UserName);
            // var userName = @"\\10.150.200.78\Administrator";

            var result = WNetAddConnection2(
                netResource,
                credentials.Password,
                userName,
                0);

            if (result != 0)
            {
                throw new Win32Exception(result, "Error connecting to remote share");
            }
        }

        ~ConnectToSharedFolder()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            WNetCancelConnection2(_networkName, 0, true);
        }

        [DllImport("mpr.dll")]
        private static extern int WNetAddConnection2(NetResource netResource,
            string password, string username, int flags);

        [DllImport("mpr.dll")]
        private static extern int WNetCancelConnection2(string name, int flags,
            bool force);

        [StructLayout(LayoutKind.Sequential)]
        public class NetResource
        {
            public ResourceScope Scope;
            public ResourceType ResourceType;
            public ResourceDisplaytype DisplayType;
            public int Usage;
            public string LocalName;
            public string RemoteName;
            public string Comment;
            public string Provider;
        }

        public enum ResourceScope : int
        {
            Connected = 1,
            GlobalNetwork,
            Remembered,
            Recent,
            Context
        };

        public enum ResourceType : int
        {
            Any = 0,
            Disk = 1,
            Print = 2,
            Reserved = 8,
        }

        public enum ResourceDisplaytype : int
        {
            Generic = 0x0,
            Domain = 0x01,
            Server = 0x02,
            Share = 0x03,
            File = 0x04,
            Group = 0x05,
            Network = 0x06,
            Root = 0x07,
            Shareadmin = 0x08,
            Directory = 0x09,
            Tree = 0x0a,
            Ndscontainer = 0x0b
        }
    }
    public class MicrosoftAccessDbManager : IDBManager
    {
        public bool lastAttempt = false, connectionFailedNotQuery = false;
        
        public List<SaleTransaction> getSalesDateRange(Store storeDB, DateTime startDate, DateTime endDate)
        {

            try {

                CommonFunctions.Log("\nMicrosoftAccessDbManager.getRemoteFile(): Trying To Connect To Remote Server");
                StoreDB store = (StoreDB)storeDB;
                string networkPath = store.DbServerIP;
                NetworkCredential credentials = new NetworkCredential(store.DbServerName, store.DbServerPassword, "10.150.200.78");
                using (new ConnectToSharedFolder(networkPath, credentials))
                {
                    CommonFunctions.Log("\nSuccessfully connecting to Remote Server.\n");
                    CommonFunctions.Log("\nMicrosoftAccessDbManager.getSalesDateRange(): Getting Sales for StoreID: " + store.StoreID);

                    string connectionString = setConnectionString(store),
                        query = store.SqlQuery,
                        datetimeFormat = store.DateTimeFormat,
                        modifiedQuery, datetimeColumnName = "";

                    if (connectionString != "")
                    {
                        store.ConnectionAttempts += "S" + DateTime.Now.ToString("HH:mm");

                        CommonFunctions.updateConnectionString(store, connectionString);

                        modifiedQuery = CommonFunctions.removeSemiColon(query);

                        datetimeColumnName = getDatetimeColumnName(modifiedQuery, connectionString, store);

                        CommonFunctions.Log("\nMicrosoftAccessDbManager.getSalesDateRange(): Retrieving Sales.");

                        List<Func<DataTable>> functions = new List<Func<DataTable>>();
                        functions.Add(() => ModifyFinalDateTime(modifiedQuery, datetimeColumnName, connectionString, store, startDate, endDate, false));
                        functions.Add(() => ModifyFinalString(modifiedQuery, datetimeColumnName, connectionString, store, startDate, endDate));
                        functions.Add(() => ModifyFinalInteger(modifiedQuery, datetimeColumnName, connectionString, store, startDate, endDate));

                        DataTable resultsTable, zeroSalesTable = null;
                        foreach (Func<DataTable> func in functions)
                        {
                            resultsTable = func();
                            if (resultsTable != null && resultsTable.Rows.Count > 0)
                            {
                                store.QueryAttempts += "S" + DateTime.Now.ToString("HH:mm");
                                return CommonFunctions.setSaleTranscationList(store, resultsTable);
                            }

                            if (resultsTable != null && resultsTable.Rows.Count == 0)
                                zeroSalesTable = resultsTable;
                        }

                        if (zeroSalesTable != null)
                        {
                            store.QueryAttempts += "S" + DateTime.Now.ToString("HH:mm");
                            return new List<SaleTransaction>();
                        }
                        store.QueryAttempts += connectionFailedNotQuery == false ? "F" + DateTime.Now.ToString("HH:mm") : "";
                        connectionFailedNotQuery = false;
                    }
                    
                    CommonFunctions.Log("\n**** MicrosoftAccessDbManager.getSalesDateRange(): Failed to get Sales.");
                    return null;
                }
            } catch (Exception e) {
                CommonFunctions.Log("\n**** Failed to connect to Remote Server");
                CommonFunctions.Log("**** Exception Message: " + e.Message);
            }
            return null;
            
        }
        private DataTable ModifyFinalDateTime(string modifiedQuery, string datetimeColumnName, string connectionString, StoreDB store, DateTime startDate, DateTime endDate, bool replace)
        {
            try
            {
                string FinalQuery = modifiedQuery;
                if (modifiedQuery.ToLower().Contains("where"))
                {
                    FinalQuery = modifiedQuery.ToLower().Replace("where", "WHERE AND ");
                }
                else if (modifiedQuery.ToLower().Contains("group by"))
                {
                    FinalQuery = modifiedQuery.ToLower().Replace("group by", " WHERE GROUP BY ");
                }
                else
                {
                    FinalQuery = modifiedQuery + " WHERE ";
                }

                DataTable fullResults = new DataTable();
                for (DateTime d = startDate; d <= endDate; d = d.AddDays(1.0))
                {

                    string query = FinalQuery.Replace("WHERE", $"WHERE {datetimeColumnName} = #{d.ToString(store.DateTimeFormat)}#");
                    Console.WriteLine(query);
                    DataTable results = executeReturnQuery(connectionString, query, store);
                    fullResults.Merge(results);
                }

                CommonFunctions.Log("\nHandling DateTime Column of Type INTEGER. Attempt query:\n" + FinalQuery);
                return fullResults;
            }
            catch (Exception e)
            {
                CommonFunctions.Log("\n**** Handling DateTime Column of Type DATETIME (using replace: " + replace + ") attempt failed.");
                CommonFunctions.Log("**** Exception Message: " + e.Message);
                return null;
            }
        }
        private DataTable ModifyFinalString(string modifiedQuery, string datetimeColumnName, string connectionString, StoreDB store, DateTime startDate, DateTime endDate)
        {
            try
            {
                string FinalQuery = modifiedQuery;
                if (modifiedQuery.ToLower().Contains("where"))
                {
                    FinalQuery = modifiedQuery.ToLower().Replace("where", "WHERE AND ");
                }
                else if (modifiedQuery.ToLower().Contains("group by"))
                {
                    FinalQuery = modifiedQuery.ToLower().Replace("group by", " WHERE GROUP BY ");
                }
                else
                {
                    FinalQuery = modifiedQuery + " WHERE ";
                }

                DataTable fullResults = new DataTable();
                for (DateTime d = startDate; d <= endDate; d = d.AddDays(1.0))
                {
                    string query = FinalQuery.Replace("WHERE", $"WHERE {datetimeColumnName} = \"{d.ToString(store.DateTimeFormat)}\"");
                    Console.WriteLine(query);
                    DataTable results = executeReturnQuery(connectionString, query, store);
                    fullResults.Merge(results);
                }
                return fullResults;
            }
            catch (Exception e)
            {
                CommonFunctions.Log("\n**** Handling DateTime Column of Type STRING attempt failed.");
                CommonFunctions.Log("**** Exception Message: " + e.Message);
                return null;
            }
        }
        private DataTable ModifyFinalInteger(string modifiedQuery, string datetimeColumnName, string connectionString, StoreDB store, DateTime startDate, DateTime endDate)
        {
            try
            {
                
                string FinalQuery = modifiedQuery;
                if (modifiedQuery.ToLower().Contains("where"))
                {
                    FinalQuery = modifiedQuery.ToLower().Replace("where", "WHERE AND ");
                }
                else if (modifiedQuery.ToLower().Contains("group by"))
                {
                    FinalQuery = modifiedQuery.ToLower().Replace("group by", " WHERE GROUP BY ");
                }
                else
                {
                    FinalQuery = modifiedQuery + " WHERE ";
                }

                DataTable fullResults = new DataTable();
                for (DateTime d = startDate; d <= endDate; d = d.AddDays(1.0))
                {

                    string query = FinalQuery.Replace("WHERE", $"WHERE {datetimeColumnName} = {d.ToString(store.DateTimeFormat)}");
                    Console.WriteLine(query);
                    DataTable results = executeReturnQuery(connectionString, query, store);;
                    fullResults.Merge(results);
                }

                CommonFunctions.Log("\nHandling DateTime Column of Type INTEGER. Attempt query:\n" + FinalQuery);
                lastAttempt = true;
                return fullResults;
            }
            catch (Exception e)
            {
                CommonFunctions.Log("\n**** Handling DateTime Column of Type INTEGER attempt failed.");
                CommonFunctions.Log("**** Exception Message: " + e.Message);
                return null;
            }
        }
        private string getDatetimeColumnName(string modifiedQuery, string connectionString, StoreDB store)
        {
            string headersQuery, headerNames = "", datetimeColumnName = "",
             NoSpacemodifiedQuery = modifiedQuery.ToLower().Replace(" ", "");

            // create query to retrieve header names (in case of 'select *')
            if (NoSpacemodifiedQuery.Contains("select*"))
            {
                // modify the query to retrieve only 1 row.
                headersQuery = modifiedQuery.Replace("*", " TOP 1 * ");

                // get "HeaderNames" & "datetimeColumnName".
                try
                {
                    DataTable headersTable = executeReturnQuery(connectionString, headersQuery, store);

                    headerNames = getHeaderNames(headersTable); // My Version

                    datetimeColumnName = "[" + headersTable.Columns[store.MapDate - 1].ColumnName.ToString() + "]"; 
                    
                    modifiedQuery = CommonFunctions.replaceSelectAllWithHeaderName(modifiedQuery, headerNames);

                    CommonFunctions.Log("\nMicrosoftAccessDbManager.getSales(): Successfully retrieved datatable headers.");
                    CommonFunctions.Log("HeadersQuery: " + headersQuery);
                    CommonFunctions.Log("Header Names: " + headerNames);
                }
                catch (Exception e)
                {
                    CommonFunctions.Log("\n**** MicrosoftAccessDbManager.getSales(): Failed to retrieve datatable headers");
                    CommonFunctions.Log("HeadersQuery: " + headersQuery);
                    CommonFunctions.Log("Exception Message: " + e.Message);
                    return null;
                }
            }
            else
            {
                // extracting column names from Query string.
                int subStringStartIndex = modifiedQuery.ToLower().IndexOf("select") + 6;
                int subStringLength = modifiedQuery.ToLower().IndexOf("from") - subStringStartIndex;

                string columnNamesString = modifiedQuery.Substring(subStringStartIndex, subStringLength);

                datetimeColumnName = CommonFunctions.getDatetimeColumnNameElse(columnNamesString, store.MapDate);

                datetimeColumnName = CommonFunctions.removeAliasFromDatetimeColumnName(datetimeColumnName);
            }
            return datetimeColumnName;
        }
        private string setConnectionString(StoreDB store)
        {
            // dbConnectionString = (string)storeJSONDetails["DbConnectionString"]  // to be added !!
            // Provider = Microsoft.ACE.OLEDB.12.0; Data Source = D:\Work\Databases\String.mdb; Jet OLEDB:Database Password = string;
            string connectionStringLocal = store.DbConnectionString,
                   dbIP = store.DbServerIP,
                   dbName = store.DbName,
                   dbUsername = store.DbUsername,
                   dbPassword = store.DbPassword,
                   temp = "";
            CommonFunctions.Log("\nMicrosoftAccessDbManager.setConnectionString(): Setting Connection String to connect with store DB.");

            if (connectionStringLocal != "")
            {
                try // Default Connection String
                {
                    TestConnection(connectionStringLocal, store);

                    CommonFunctions.Log("\nSuccessfully connected to store DB.\nConnection String (Default):" + connectionStringLocal);

                    return connectionStringLocal;
                }
                catch (Exception e)
                {
                    CommonFunctions.Log("\n**** Failed to connect to store DB.");
                    CommonFunctions.Log("**** Connection String (Default): " + connectionStringLocal);
                    CommonFunctions.Log("**** Exception Message: " + e.Message);
                }
            }

            try // Type 1
            {
                string networkPath = store.DbServerIP;
                string DBName = store.DbName;
                networkPath += "\\" + DBName + ".mdb";
                string pass = store.DbPassword;                
                //temp = @$"Provider=Microsoft.ACE.OLEDB.12.0;Data Source= {networkPath}; Jet OLEDB: Database Password = {pass};Persist Security Info=False;";
                connectionStringLocal = $"Provider=Microsoft.ACE.OLEDB.12.0; Data Source = {networkPath}; Jet OLEDB:Database Password = {pass};";
                TestConnection(connectionStringLocal, store);

                CommonFunctions.Log("\nSuccessfully connected to store DB.\nConnection String :" + connectionStringLocal);

                return connectionStringLocal;
            }
            catch (Exception e)
            {
                CommonFunctions.Log("\n**** Failed to connect to store DB.");
                CommonFunctions.Log("**** Connection String (Type1): " + connectionStringLocal);
                CommonFunctions.Log("**** Exception Message: " + e.Message);
            }
            store.ConnectionAttempts += "F" + DateTime.Now.ToString("HH:mm");
            return "";
        }
        private DataTable executeReturnQuery(string connectionString, string query, Store store)
        {
            using (DataTable resultsTable = new DataTable())
            {
                using (OleDbConnection conn = new OleDbConnection())
                {
                    try
                    {
                        conn.ConnectionString = connectionString;
                        conn.Open();
                    }
                    catch
                    {
                        if (lastAttempt == true)
                        {
                            store.ConnectionAttempts += "F" + DateTime.Now.ToString("HH:mm");
                            lastAttempt = false;
                            connectionFailedNotQuery = true;
                        }
                    }

                    OleDbCommand cmd = conn.CreateCommand();
                    cmd.CommandText = query;
                    OleDbDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                    resultsTable.Load(reader);
                    conn.Close();
                    conn.Dispose();
                }
                return resultsTable;
            }
        }
        private void TestConnection(string connectionString, Store store)
        {
            using (var conn = new OleDbConnection(connectionString))
            {
                //conn.ConnectionString = connectionString;
                conn.Open();
                conn.Close();
                conn.Dispose();
            }
        }
        public string getHeaderNames(DataTable headersTable)
        {
            string headerNames = "";
            foreach (DataColumn column in headersTable.Columns)
            {
                headerNames += "[" + column.ColumnName + "],";
            }

            return headerNames = headerNames.Substring(0, headerNames.Length - 1);
        }

        public void getRemoteFile(Store store, string fileName) 
        {
            CommonFunctions.Log("\nMicrosoftAccessDbManager.getRemoteFile(): getting the file from the remote folder");
            string networkPath = @"\\{10.150.200.78}\c$\Windows\Temp";
            NetworkCredential credentials = new NetworkCredential(@"Administrator", "Pos_Admin@123", "10.150.200.78");
            string myNetworkPath = string.Empty;
            try {
                using (new ConnectToSharedFolder(networkPath, credentials))
                {
                    myNetworkPath = networkPath + "\\" + fileName;
                    System.IO.File.Copy(myNetworkPath, @"D:\\test.txt", true);
                }
                CommonFunctions.Log("\nSuccessfully get the file.\n");
            } catch (Exception e) {
                CommonFunctions.Log("\n**** Failed to get the file from the remote folder");
                CommonFunctions.Log("**** Exception Message: " + e.Message);
            }
            
        }

        public string extractIP(StoreDB store) {
            string ip = store.DbServerIP.Split("\\")[2];
            return ip;
        }
        
    }
}
