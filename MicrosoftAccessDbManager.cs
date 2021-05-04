using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using console_middleware.models;

namespace console_middleware.DataSourceManagers
{
    public class MicrosoftAccessDbManager : IDBManager
    {
        public bool lastAttempt = false, connectionFailedNotQuery = false;
        
        public List<SaleTransaction> getSalesDateRange(Store storeDB, DateTime startDate, DateTime endDate)
        {
            StoreDB store = (StoreDB)storeDB;

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
                functions.Add(() => ModifyFinalInteger(modifiedQuery, datetimeColumnName, connectionString, store, startDate, endDate));
                functions.Add(() => ModifyFinalString(modifiedQuery, datetimeColumnName, connectionString, store, startDate, endDate));
                functions.Add(() => ModifyFinalDateTime(modifiedQuery, datetimeColumnName, connectionString, store, startDate, endDate, false));


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
                    DataTable results = executeReturnQuery(connectionString, query, store);
                    fullResults.Merge(results);
                }

                CommonFunctions.Log("\nHandling DateTime Column of Type INTEGER. Attempt query:\n" + FinalQuery);
                lastAttempt = true;
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
                    DataTable results = executeReturnQuery(connectionString, query, store);;
                    fullResults.Merge(results);
                }

                CommonFunctions.Log("\nHandling DateTime Column of Type INTEGER. Attempt query:\n" + FinalQuery);

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
            string portTemp,
                   connectionStringLocal = store.DbConnectionString,
                   dbIP = store.DbServerIP,
                   port = store.DbTcpPort,
                   dbName = store.DbName,
                   dbUsername = store.DbUsername,
                   dbPassword = store.DbPassword,
                   timeout = "5";

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
                portTemp = port.Trim() == "" || port == "N/A" || port == null ? "3306" : port;
                connectionStringLocal = "Server=" + dbIP + ";Port=" + portTemp + ";Database=" + dbName + ";Uid=" + dbUsername + ";Pwd=" + dbPassword + ";Connection Timeout=" + timeout + ";";
                TestConnection(connectionStringLocal, store);

                CommonFunctions.Log("\nSuccessfully connected to store DB.\nConnection String (Type1):" + connectionStringLocal);

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
            using (OleDbConnection conn = new OleDbConnection())
            {
                conn.ConnectionString = connectionString;
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
        /*public static string getDatetimeColumnNameIf(DataTable headersTable, int datetimeMap)
        {
            // get 'datetimeColumn' Name from HeaderNames (using 'datetimeMap').
            string nameCol = headersTable.Columns[datetimeMap - 1].ColumnName;
            string retrievedDateName = headersTable.Rows[datetimeMap][nameCol].ToString();
            return "[" + retrievedDateName + "]";
        }*/
    }
}