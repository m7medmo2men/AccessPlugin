using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Net;
using System.Data.OleDb;
using System.Data;
using Newtonsoft.Json;
using System.Collections;
using System.Text.RegularExpressions;
using System.Threading;

namespace MicrosoftAccessAddon
{

    public class Operation
    {
        public Operation()
        {
               
        }
        public DataTable GetData(string MyQuery)
        {

            DataTable myDataTable = new DataTable();
            using (var connection = new OleDbConnection("Provider = Microsoft.Jet.OLEDB.4.0; Data Source  =D:\\Work\\MicrosoftAccessAddon\\viel.mdb; Jet OLEDB:Database Password = 201720182019@smc;"))
            //using (var connection = new OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;" + "data source=C:\\Users\\admin\\source\\repos\\WinFormsApp1\\WinFormsApp1\\viel.mdb;Jet OLEDB:Database Password=201720182019@smc"))
            {
                connection.Open();

                // Execute Queries
                OleDbCommand cmd = connection.CreateCommand();
                cmd.CommandText = MyQuery;
                OleDbDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection); // close conn after complete
               
                // headerNames = headerNames.Substring(0, headerNames.Length - 1);
                // Log(table.Rows[2][nameCol].ToString());
                
                // get 'datetimeColumn' Name from HeaderNames (using 'datetimeMap' value).
                /*if (headers.Columns[datetimeMap].ColumnName.ToString().Contains(" "))
                {
                    datetimeColumnName = "[" + headers.Columns[datetimeMap].ColumnName.ToString() + "]";
                }
                else
                {
                    datetimeColumnName = headers.Columns[datetimeMap].ColumnName.ToString();
                }*/
                /*foreach (DataRow row in table.Rows)
                {
                    if (row[nameCol].ToString().Contains(" "))
                        headerNames += "[" + row[nameCol].ToString() + "],";
                    else
                        headerNames += row[nameCol].ToString() + ",";
                }
                headerNames = headerNames.Substring(0, headerNames.Length - 1);
                Log(headerNames);*/
                /*foreach (DataRow row in table.Rows)
                {
                    Log(row[nameCol].ToString());
                    //Log(row.ItemArray[2].ToString());
                }*/
                myDataTable.Load(reader);

            }

            return myDataTable;
        }

        public DataTable GetData(string ConnectionString, string accessQuery, int datetimeMap, string date, string DateTimeFormat)
        {
            try
            {
                OleDbConnection conn = new OleDbConnection(ConnectionString);
                try
                {
                    conn.Open();
                    Log("\nMicrosoftAccessAddon.GetData: Successfully opened a connection to database - ConnectionString: " + ConnectionString);
                }
                catch (Exception e)
                {
                    Log("\n***** MicrosoftAccessAddon.GetData: Failed to open a connection to database - ConnectionString: " + ConnectionString);
                    Log("***** Exception Message: " + e.Message);
                    return null;
                }


                DataTable results = new DataTable();
                OleDbCommand cmd = conn.CreateCommand();
                 cmd.CommandText = accessQuery;
                cmd.CommandText = ModifyAccessQuery(ConnectionString, accessQuery, datetimeMap, date, DateTimeFormat);
                OleDbDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                results.Load(reader);
                //conn.Close();
                //conn.Dispose();
                
                //string result = JsonConvert.SerializeObject(results);
                //Log(result);
                Log("\nMicrosoftAccessAddon.GetData: Successfully returned data to middleware");
                Log("-----------------------------------------------------------------------------------------------------");
                return results;
                
            }
            catch (Exception e)
            {
                Log("\n***** MySQLAddon.GetData: Failed to get data.");
                Log("***** Exception Message: " + e.Message);
                Log("-----------------------------------------------------------------------------------------------------");

                return null;
            }
        }

        /*public string GetMissingData(string ConnectionString, string MySqlQuery, int datetimeMap, string startDate, string endDate, string DateTimeFormat)
        {
            try
            {
                MySqlConnection conn = new MySqlConnection(ConnectionString);
                try
                {
                    conn.Open();
                    Log("\nMySQLAddon.GetMissingData: Successfully opened a connection to database - ConnectionString: " + ConnectionString);
                }
                catch (Exception e)
                {
                    Log("\n***** MySQLAddon.GetMissingData: Failed to open a connection to database - ConnectionString: " + ConnectionString);
                    Log("***** Exception Message: " + e.Message);
                    return null;
                }

                DataTable results = new DataTable();
                MySqlCommand cmd = new MySqlCommand();
                cmd.Connection = conn;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = ModifyMySqlQuery(ConnectionString, MySqlQuery, datetimeMap, startDate, endDate, DateTimeFormat);
                MySqlDataAdapter adapter = new MySqlDataAdapter();
                adapter.SelectCommand = cmd;
                adapter.Fill(results);

                conn.Close();
                conn.Dispose();
                string result = JsonConvert.SerializeObject(results);

                Log("\nMySQLAddon.GetMissingData: Successfully returning data to middleware");
                Log("-----------------------------------------------------------------------------------------------------");

                return result;
            }
            catch (Exception e)
            {
                Log("\n***** MySQLAddon.GetMissingData: Failed to get data.");
                Log("***** Exception Message: " + e.Message);
                Log("-----------------------------------------------------------------------------------------------------");

                return null;
            }
        }*/


        private string ModifyAccessQuery(string ConnectionString, string accessQuery, int datetimeMap, string date, string DateTimeFormat)
        {
            // DateTimeMap : index of Column of DateTimeMap
            // date : QueryDate
            // DateTimeFormat : El Format El Mewgoda fe El Database

            Log("\nMicrosoftAccessAddon.ModifyAccessQuery(5x): Entered.");

            string modifiedQuery, headersQuery, datetimeColumnName = "", headerNames = "", FinalQuery;

            // Log(date);
            // date = DateTime.ParseExact(date, DateTimeFormat, System.Globalization.CultureInfo.InvariantCulture).ToString("yyyyMMdd");
            // Log(date);


            // remove ; from the end of the query.
            char lastCharInQuery = accessQuery[accessQuery.Length - 1];
            if (lastCharInQuery == ';')
            {
                modifiedQuery = accessQuery.Substring(0, accessQuery.Length - 1);
            }
            else
            {
                modifiedQuery = accessQuery;
            }

            // establish a connection to Database.
            OleDbConnection conn2 = new OleDbConnection(ConnectionString);
            conn2.Open();



            string NoSpacemodifiedQuery = modifiedQuery.ToLower().Replace(" ", "");

            // if modifiedQuery contains *, replace it with datatable headers.
            // we do this, to convert the datetime column (in "FinalQuery") to a specific format.
            if (NoSpacemodifiedQuery.Contains("select*"))
            {

                // get "HeaderNames" & "datetimeColumnName".
                try
                {
                    OleDbCommand cmd = conn2.CreateCommand();
                    cmd.CommandText = "select * from SALESQ"; // Dummy Query
                    OleDbDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection); // close conn after complete
                    var table = reader.GetSchemaTable();
                    var nameCol = table.Columns["ColumnName"];
                    foreach (DataRow row in table.Rows)
                    {
                        if (row[nameCol].ToString().Contains(" "))
                            headerNames += "[" + row[nameCol].ToString() + "],";
                        else
                            headerNames += "[" + row[nameCol].ToString() + "],";
                    }

                    headerNames = headerNames.Substring(0, headerNames.Length - 1);
                    // get 'datetimeColumn' Name from HeaderNames (using 'datetimeMap' value).
                    string retrievedDateName = table.Rows[datetimeMap][nameCol].ToString();

                    Log(table.Rows[datetimeMap][nameCol].ToString());
                    if (retrievedDateName.Contains(" "))
                        datetimeColumnName = "[" + retrievedDateName + "]";
                    else //to be checked again
                        datetimeColumnName = "[" + retrievedDateName + "]";
                    Log(headerNames);
                    Log(datetimeColumnName);
                    Log("\nMicrosoftAccessAddon.ModifyAccessQuery(5x): Successfully retrieved datatable headers - HeadersQuery: " + "Dummy Query" +
                        "");
                }
                catch (Exception e)
                {
                    Log("\n***** MicrosoftAccessAddon.ModifyAccessQuery(5x): Failed to retrieve datatable headers - HeadersQuery: " + "Dummy Query");
                    Log("***** Exception Message: " + e.Message);
                    return null;
                }

                // in 'modifiedQuery' replace '*' with retrieved headernames.
                // Hansheel el * we ne7ot el header names;

                int indexOfSelect = modifiedQuery.ToLower().IndexOf("select") + 6;

                for (int f = indexOfSelect; f < modifiedQuery.Length; f++)
                {
                    if (modifiedQuery[f] == ' ')
                        continue;
                    else if (modifiedQuery[f] == '*')
                    {
                        modifiedQuery = modifiedQuery.Substring(0, indexOfSelect + 1) + headerNames + modifiedQuery.Substring(f + 1);
                        break;
                    }
                    else
                        break;
                }

            }
            else
            {
                int subStringStartIndex = modifiedQuery.ToLower().IndexOf("select") + 6;
                int subStringLength = modifiedQuery.ToLower().IndexOf("from") - subStringStartIndex;

                string columnNamesString = modifiedQuery.Substring(subStringStartIndex, subStringLength);
                Stack myStack = new Stack();
                List<string> columnNamesArray = new List<string>();

                // For DEBUGING only.
                // Log("\nMySQLAddon.ModifySQLQuery(5x): Modified Query : " + modifiedQuery);
                // Log("Column Names : " + columnNamesString);

                // get individual column name - split by ',' considering there could be ',' or ' ' between parentheses in a column name.
                string tempColumnName = "";

                for (int i = 0; i < columnNamesString.Length; i++)
                {
                    tempColumnName += columnNamesString[i];

                    if (columnNamesString[i] == '(' || columnNamesString[i] == '[')
                    {
                        myStack.Push(columnNamesString[i]);
                    }
                    else if (columnNamesString[i] == ')' || columnNamesString[i] == ']')
                    {
                        myStack.Pop();
                    }

                    if (myStack.Count == 0 && columnNamesString[i] == ',' || i == columnNamesString.Length - 1)
                    {
                        tempColumnName = tempColumnName.Substring(0, tempColumnName.Length - 1);
                        columnNamesArray.Add(tempColumnName.Trim());
                        tempColumnName = "";
                        if (columnNamesArray.Count - 1 == datetimeMap)
                        {
                            datetimeColumnName = columnNamesArray[columnNamesArray.Count - 1].Trim();
                        }
                    }
                }

                // handle datetimecolumn with Alias name - REMOVE Alias name from 'datetimeColumnName'.
                if (datetimeColumnName.ToLower().Contains(" as "))
                {
                    int EndIndex = datetimeColumnName.ToLower().IndexOf(" as ");
                    datetimeColumnName = datetimeColumnName.Substring(0, EndIndex);
                    Log("datetimeColumnName: " + datetimeColumnName);
                }
                else
                {
                    int a = datetimeColumnName.Length - 1;
                    while (a >= 0 && datetimeColumnName[a] != ' ' && datetimeColumnName[a] != ']' && datetimeColumnName[a] != ')')
                    {
                        a--;
                    }

                    if (a > 0)
                    {
                        datetimeColumnName = datetimeColumnName.Substring(0, a + 1);
                    }
                }
                // Log("datetimeColumnName: " + datetimeColumnName);
            }
            Log(datetimeColumnName);
            // add datetimeColumnName to SQL statement.
            if (modifiedQuery.ToLower().Contains("where"))
            {
                FinalQuery = modifiedQuery + " WHERE Format(" + datetimeColumnName + ", \"" + DateTimeFormat + "\") = " + "\"" + date + "\"" + " AND ";
                //FinalQuery = modifiedQuery.ToLower().Replace("where", " WHERE Format(" + datetimeColumnName + ", \"yyyyMMdd\") = " + date + " AND ");
            }
            else if (modifiedQuery.ToLower().Contains("group by"))
            {
                FinalQuery = modifiedQuery + " WHERE Format(" + datetimeColumnName + ", \"" + DateTimeFormat + "\") = " + "\"" + date + "\"";
                //FinalQuery = modifiedQuery.ToLower().Replace("group by", " WHERE Format(" + datetimeColumnName + ", \"yyyyMMdd\") = " + date + " GROUP BY");
            }
            else
            {
                FinalQuery = modifiedQuery + " WHERE Format(" + datetimeColumnName + ", \"" + DateTimeFormat + "\") = " + "\"" + date + "\"";
                //FinalQuery = modifiedQuery +  " WHERE " + datetimeColumnName + " = #2018/1/1# ;";
            }
            
            // testing: FinalQuery (before returning it).
            try
            {
                try
                {
                    // throw new Exception("Ana el 3amel kda");
                    conn2 = new OleDbConnection(ConnectionString);
                    conn2.Open();
                    OleDbCommand cmd = conn2.CreateCommand();
                    cmd.CommandText = FinalQuery;
                    OleDbDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                    DataTable results = new DataTable();
                    results.Load(reader);
                    if (results.Rows.Count != 0)
                    {
                        Log("\nMicrosoftAccessAddon.ModifyAccessQuery(5x): Successfully executed First Attempt FinalQuery - First FinalQuery: " + FinalQuery);

                    } else
                    {
                        conn2.Close();
                        conn2.Dispose();
                        throw new Exception("Zero Rows Returned we had to try the second case");
                    }
                    

                }
                catch (Exception e)
                {
                    Log("\n***** MicrosoftAccessAddon.ModifyAccessQuery(5x): Failed to executed First Attempt FinalQuery - First FinalQuery: " + " "); // repalce " "  with final query
                    Log("***** Exception Message: " + e.Message);
                    Log("***** Starting Second Attempt.");


                    //date = "19981010";
                    //DateTimeFormat = "yyyyddMM";
                    DateTime dateTime = DateTime.ParseExact(date, DateTimeFormat, System.Globalization.CultureInfo.InvariantCulture);
                    
                    string storeFormat = "yyyy-dd-MM";
                    string x = dateTime.ToString(storeFormat);
                    
                    FinalQuery = modifiedQuery;

                    if (FinalQuery.ToLower().Contains("where"))
                    {
                        FinalQuery = Regex.Replace(FinalQuery, "where", "WHERE " + datetimeColumnName + " = " + dateTime.ToString(DateTimeFormat) + " AND ", RegexOptions.IgnoreCase);

                    }
                    else if (FinalQuery.ToLower().Contains("group by"))
                    {
                        FinalQuery = Regex.Replace(FinalQuery, "group by", "WHERE " + datetimeColumnName + " = " + dateTime.ToString(DateTimeFormat) + " GROUP BY ", RegexOptions.IgnoreCase);
                    }
                    else
                    {
                        //FinalQuery = FinalQuery + " WHERE " + datetimeColumnName + " = " + dateTime.ToString(storeFormat) + "";
                        FinalQuery = FinalQuery + $" WHERE {datetimeColumnName} = \"{dateTime.ToString(storeFormat)}\"";
                    }

                    conn2 = new OleDbConnection(ConnectionString);
                    conn2.Open();
                    OleDbCommand cmd = conn2.CreateCommand();
                    cmd.CommandText = FinalQuery;
                    OleDbDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                    DataTable results = new DataTable();
                    results.Load(reader);
                   
                    Log("\nMicrosoftAccessAddon.ModifyAccessQuery(5x): Successfully executed Second Attempt FinalQuery - Second FinalQuery: " + FinalQuery);

                        //----------------------------------------------------------------------------------------------------------
                    }
                }
                catch (Exception e)
                {
                    Log("\n***** MicrosoftAccessAddon.ModifyAccessQuery(5x): Failed to executed Second Attempt FinalQuery - Second FinalQuery: " +  " "); // replace " " with final query
                    Log("***** Exception Message: " + e.Message);
                    return null;
                }
                return FinalQuery;
            }
            
        /* private string ModifyMySqlQuery(string ConnectionString, string MySqlQuery, int datetimeMap, string startDate, string endDate, string DateTimeFormat)
        {
            Log("\nMySQLAddon.ModifySQLQuery(6x): Entered.");

            string modifiedQuery, headersQuery, datetimeColumnName = "", headerNames = "", FinalQuery;

            string Start_Date = DateTime.Parse(startDate).ToString("yyyyMMdd");
            string End_Date = DateTime.Parse(endDate).ToString("yyyyMMdd");

            // remove ; from the end of the query.
            char lastCharInQuery = MySqlQuery[MySqlQuery.Length - 1];
            if (lastCharInQuery == ';')
            {
                modifiedQuery = MySqlQuery.Substring(0, MySqlQuery.Length - 1);
            }
            else
            {
                modifiedQuery = MySqlQuery;
            }

            // establish a connection to Database.
            MySqlConnection conn = new MySqlConnection(ConnectionString);
            conn.Open();

            string NoSpacemodifiedQuery = modifiedQuery.ToLower().Replace(" ", "");

            // if modifiedQuery contains *, replace it with datatable headers.
            // we do this, to convert the datetime column (in "FinalQuery") to a specific format.
            if (NoSpacemodifiedQuery.Contains("select*"))
            {
                int lastIndexOfSelect = modifiedQuery.ToLower().IndexOf("select") + 6;
                string queryWithoutSelect = modifiedQuery.Substring(lastIndexOfSelect, modifiedQuery.Length - lastIndexOfSelect);
                headersQuery = modifiedQuery + " LIMIT 0,0";

                // get 'HeaderNames' & 'datetimeColumnName' using headersQuery.
                try
                {
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = conn;
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = headersQuery;
                    MySqlDataAdapter adapter = new MySqlDataAdapter();
                    adapter.SelectCommand = cmd;

                    DataTable headers = new DataTable();

                    adapter.Fill(headers);
                    conn.Close();

                    // store retrieved header names in "HeaderNames" (& remove extra ,).
                    foreach (DataColumn column in headers.Columns)
                    {
                        if (column.ColumnName.Contains(" "))
                        {
                            headerNames += "[" + column.ColumnName + "],";
                        }
                        else
                        {
                            headerNames += column.ColumnName + ",";
                        }
                    }

                    headerNames = headerNames.Substring(0, headerNames.Length - 1);

                    // get datetimeColumn Name from HeaderNames (using datetimeMap value).
                    if (headers.Columns[datetimeMap].ColumnName.ToString().Contains(" "))
                    {
                        datetimeColumnName = "[" + headers.Columns[datetimeMap].ColumnName.ToString() + "]";
                    }
                    else
                    {
                        datetimeColumnName = headers.Columns[datetimeMap].ColumnName.ToString();
                    }

                    Log("\nMySQLAddon.ModifySQLQuery(6x): Failed to retrieve datatable headers - HeadersQuery: " + headersQuery);
                }
                catch (Exception e)
                {
                    Log("\n***** MySQLAddon.ModifySQLQuery(6x): Failed to retrieve datatable headers - HeadersQuery: " + headersQuery);
                    Log("***** Exception Message: " + e.Message);
                    return null;
                }

                // in 'modifiedQuery' replace '*' with retrieved headernames.
                int indexOfSelect = modifiedQuery.ToLower().IndexOf("select") + 6;

                for (int f = indexOfSelect; f < modifiedQuery.Length; f++)
                {
                    if (modifiedQuery[f] == ' ')
                        continue;
                    else if (modifiedQuery[f] == '*')
                    {
                        modifiedQuery = modifiedQuery.Substring(0, indexOfSelect + 1) + headerNames + modifiedQuery.Substring(f + 1);
                        break;
                    }
                    else
                        break;
                }
            }
            else
            {
                int subStringStartIndex = modifiedQuery.ToLower().IndexOf("select") + 6;
                int subStringLength = modifiedQuery.ToLower().IndexOf("from") - subStringStartIndex;

                string columnNamesString = modifiedQuery.Substring(subStringStartIndex, subStringLength);

                Stack myStack = new Stack();
                List<string> columnNamesArray = new List<string>();

                // for DEBUGING only.
                // Log("\nMySQLAddon.ModifySQLQuery(6x): Modified Query : " + modifiedQuery);
                // Log("Column Names : " + columnNamesString);

                // get individual column name - split by ',' considering there could be ',' or ' ' between parentheses in a column name.
                string tempColumnName = "";

                for (int i = 0; i < columnNamesString.Length; i++)
                {
                    tempColumnName += columnNamesString[i];

                    if (columnNamesString[i] == '(' || columnNamesString[i] == '[')
                    {
                        myStack.Push(columnNamesString[i]);
                    }
                    else if (columnNamesString[i] == ')' || columnNamesString[i] == ']')
                    {
                        myStack.Pop();
                    }

                    if (myStack.Count == 0 && columnNamesString[i] == ',' || i == columnNamesString.Length - 1)
                    {
                        tempColumnName = tempColumnName.Substring(0, tempColumnName.Length - 1);
                        columnNamesArray.Add(tempColumnName.Trim());
                        tempColumnName = "";
                        if (columnNamesArray.Count - 1 == datetimeMap)
                        {
                            datetimeColumnName = columnNamesArray[columnNamesArray.Count - 1].Trim();
                        }
                    }
                }

                // handle datetimecolumn with Alias name - REMOVE Alias name from 'datetimeColumnName'.
                if (datetimeColumnName.ToLower().Contains(" as "))
                {
                    int EndIndex = datetimeColumnName.ToLower().IndexOf(" as ");
                    datetimeColumnName = datetimeColumnName.Substring(0, EndIndex);
                }
                else
                {
                    int a = datetimeColumnName.Length - 1;
                    while (a >= 0 && datetimeColumnName[a] != ' ' && datetimeColumnName[a] != ']' && datetimeColumnName[a] != ')')
                    {
                        a--;
                    }

                    if (a > 0)
                    {
                        datetimeColumnName = datetimeColumnName.Substring(0, a + 1);
                    }
                }
                // Log("datetimeColumnName: " + datetimeColumnName);
            }

            if (modifiedQuery.ToLower().Contains("where"))
            {
                FinalQuery = modifiedQuery.ToLower().Replace("where", "WHERE date_format(" + datetimeColumnName + ", \"%Y%m%d\") BETWEEN date_format(" + Start_Date + ",\"%Y%m%d\") AND date_format(" + End_Date + ",\"%Y%m%d\") AND");
            }
            else if (modifiedQuery.ToLower().Contains("group by"))
            {
                FinalQuery = modifiedQuery.ToLower().Replace("group by", "WHERE date_format(" + datetimeColumnName + ", \"%Y%m%d\") BETWEEN date_format(" + Start_Date + ",\"%Y%m%d\") AND date_format(" + End_Date + ",\"%Y%m%d\") GROUP BY ");
            }
            else
            {
                FinalQuery = modifiedQuery + " WHERE date_format(" + datetimeColumnName + ", \"%Y%m%d\") BETWEEN date_format(" + Start_Date + ",\"%Y%m%d\") AND date_format(" + End_Date + ",\"%Y%m%d\")";
            }

            // testing: FinalQuery (before returning it).
            try
            {
                try
                {
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = conn;
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = FinalQuery;
                    MySqlDataAdapter adapter = new MySqlDataAdapter();
                    adapter.SelectCommand = cmd;

                    DataTable results = new DataTable();

                    adapter.Fill(results);
                    conn.Close();
                    conn.Dispose();

                    Log("\nMySQLAddon.ModifySQLQuery(6x): Successfully executed First Attempt FinalQuery - First FinalQuery: " + FinalQuery);
                }
                catch (Exception e)
                {
                    Log("\n***** MySQLAddon.ModifySQLQuery(6x): Failed to executed First Attempt FinalQuery - First FinalQuery: " + FinalQuery);
                    Log("***** Exception Message: " + e.Message);
                    Log("***** Starting Second Attempt.");

                    if (modifiedQuery.ToLower().Contains("where"))
                    {
                        FinalQuery = modifiedQuery.ToLower().Replace("where", "WHERE " + datetimeColumnName + " BETWEEN date_format(" + Start_Date + ",\"%Y%m%d\") AND date_format(" + End_Date + ",\"%Y%m%d\") AND");
                    }
                    else if (modifiedQuery.ToLower().Contains("group by"))
                    {
                        FinalQuery = modifiedQuery.ToLower().Replace("group by", "WHERE " + datetimeColumnName + " BETWEEN date_format(" + Start_Date + ",\"%Y%m%d\") AND date_format(" + End_Date + ",\"%Y%m%d\") GROUP BY ");
                    }
                    else
                    {
                        FinalQuery = modifiedQuery + " WHERE " + datetimeColumnName + " BETWEEN date_format(" + Start_Date + ",\"%Y%m%d\") AND date_format(" + End_Date + ",\"%Y%m%d\")";
                    }

                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = conn;
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = FinalQuery;
                    MySqlDataAdapter adapter = new MySqlDataAdapter();
                    adapter.SelectCommand = cmd;

                    DataTable results = new DataTable();

                    adapter.Fill(results);
                    conn.Close();
                    conn.Dispose();

                    Log("\nMySQLAddon.ModifySQLQuery(6x): Successfully executed Second Attempt FinalQuery - Second FinalQuery: " + FinalQuery);
                }
            }
            catch (Exception e)
            {
                Log("\n***** MySQLAddon.ModifySQLQuery(6x): Failed to executed Second Attempt FinalQuery - Second FinalQuery: " + FinalQuery);
                Log("***** Exception Message: " + e.Message);
                return null;
            }
            return FinalQuery;
        }
        */
        private void Log(string logText)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\POS Log\" + DateTime.Today.ToString("ddMMyyyy") + ".txt", true))
            {
                file.WriteLine(logText);
            }
        }

        /*private DataTable ModifyFinalString(string modifiedQuery, string datetimeColumnName, string connectionString, StoreDB store, DateTime startDate, DateTime endDate)
            {
                try
                {
                    string FinalQuery = modifiedQuery;

                    if (FinalQuery.ToLower().Contains("where"))
                    {
                        FinalQuery = Regex.Replace(FinalQuery, "where", "WHERE " + datetimeColumnName + " BETWEEN '" + startDate.ToString(store.DateTimeFormat) + "' AND '" + endDate.ToString(store.DateTimeFormat) + "' AND ", RegexOptions.IgnoreCase);
                        // FinalQuery = FinalQuery.ToLower().Replace("where", "WHERE " + datetimeColumnName + " BETWEEN '" + startDate.ToString(store.DateTimeFormat) + "' AND '" + endDate.ToString(store.DateTimeFormat) + "' AND ");
                    }
                    else if (FinalQuery.ToLower().Contains("group by"))
                    {
                        FinalQuery = Regex.Replace(FinalQuery, "group by", "WHERE " + datetimeColumnName + " BETWEEN '" + startDate.ToString(store.DateTimeFormat) + "' AND '" + endDate.ToString(store.DateTimeFormat) + "' GROUP BY ", RegexOptions.IgnoreCase);
                        // FinalQuery = FinalQuery.ToLower().Replace("group by", "WHERE " + datetimeColumnName + " BETWEEN '" + startDate.ToString(store.DateTimeFormat) + "' AND '" + endDate.ToString(store.DateTimeFormat) + "' GROUP BY ");
                    }
                    else
                    {
                        FinalQuery = FinalQuery + " WHERE " + datetimeColumnName + " BETWEEN '" + startDate.ToString(store.DateTimeFormat) + "' AND '" + endDate.ToString(store.DateTimeFormat) + "'";
                    }

                    Console.WriteLine("\nHandling DateTime Column of Type STRING. Attempt query:\n" + FinalQuery);
                    return executeReturnQuery(connectionString, FinalQuery, store);
                }
                catch (Exception e)
                {
                    Console.WriteLine("\n**** Handling DateTime Column of Type STRING attempt failed.");
                    Console.WriteLine("**** Exception Message: " + e.Message);
                    return null;
                }
            }*/

    }
}
