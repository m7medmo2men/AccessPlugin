using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Data.OleDb;
using MicrosoftAccessAddon;

namespace WindowsService1
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
        }


        protected override void OnStart(string[] args)
        {
            
            try
            {


                /*
                DataTable d = new DataTable();
                Operation op = new Operation();


                d = op.GetData("Select top 10 * FROM SALESQ Where Date >= #2018/01/01#;");
                foreach (DataRow row in d.Rows)
                {
                    string record = "";
                    for (int i = 0; i < row.ItemArray.Length; i++)
                    {
                        if (i != row.ItemArray.Length) record += row.ItemArray[i].ToString() + ", ";
                        else record += row.ItemArray[i].ToString();
                    }
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\POS Log\" + DateTime.Today.ToString("ddMMyyyy") + "Thread.txt", true))
                    {
                        //file.WriteLine(row.ItemArray[20].ToString() + " " + row.ItemArray[1].ToString());
                        file.WriteLine(record);
                    }
                }*/
                // d = op.GetData("SELECT * From SALESQ");



                // string s = op.GetData("Provider = Microsoft.Jet.OLEDB.4.0; Data Source = C:\\Users\\admin\\source\\repos\\WinFormsApp1\\WinFormsApp1\\viel.mdb; Jet OLEDB:Database Password = 201720182019@smc;", "Select * From SALESQ", 2, "1/1/2018", "yyyy-MM-dd");
                /*using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\POS Log\" + DateTime.Today.ToString("ddMMyyyy") + "Thread.txt", true))
                {
                    file.WriteLine(s);
                }*/
                DataTable d = new DataTable();
                Operation op = new Operation();
                
                d = op.GetData("Provider = Microsoft.Jet.OLEDB.4.0; Data Source = C:\\Users\\admin\\source\\repos\\WinFormsApp1\\WinFormsApp1\\viel.mdb; Jet OLEDB:Database Password = 201720182019@smc;", "SELECT * From SALESQ", 20, "2006/11/21", "yyyy/MM/dd"); // Column is datetime typen and column names
                //d = op.GetData("Provider = Microsoft.Jet.OLEDB.4.0; Data Source = C:\\Users\\admin\\source\\repos\\WinFormsApp1\\WinFormsApp1\\viel.mdb; Jet OLEDB:Database Password = 201720182019@smc;", "SELECT slsid, tot, Date From SALESQ", 2, "1/1/2018", "M/d/yyyy"); // Column is datetime typen and column names
                //d = op.GetData("Provider = Microsoft.Jet.OLEDB.4.0; Data Source = C:\\Users\\admin\\source\\repos\\WinFormsApp1\\WinFormsApp1\\viel.mdb; Jet OLEDB:Database Password = 201720182019@smc;", "Select * From SALESQ", 2, "1/1/2018", "M/d/yyyy"); // Column is datetime type and *
                Log(d.Rows.Count.ToString());
                foreach (DataRow row in d.Rows)
                {
                    string record = "";
                    for (int i = 0; i < row.ItemArray.Length; i++)
                    {
                        if (i != row.ItemArray.Length) record += row.ItemArray[i].ToString() + ", ";
                        else record += row.ItemArray[i].ToString();
                    }
                    Log(record);
                }
                


            }
            catch (Exception e)
            {
                /*using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\POS Log\" + DateTime.Today.ToString("ddMMyyyy") + "Thread.txt", true))
                {
                    file.WriteLine(e);
                }*/
                Log(e.ToString());
            }

        }

        protected override void OnStop()
        {
        }

        private void Log(string logText)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\POS Log\" + DateTime.Today.ToString("ddMMyyyy") + "Errors.txt", true))
            {
                file.WriteLine(logText);
            }
        }
    }
}
