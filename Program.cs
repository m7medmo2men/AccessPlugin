using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using console_middleware.DataSourceManagers;

using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Runtime.InteropServices;
namespace console_middleware
{

    public class NetworkShareAccesser : IDisposable
    {
        private string _remoteUncName;
        private string _remoteComputerName;

        public string RemoteComputerName
        {
            get
            {
                return this._remoteComputerName;
            }
            set
            {
                this._remoteComputerName = value;
                this._remoteUncName = @"\\" + this._remoteComputerName;
            }
        }

        public string UserName
        {
            get;
            set;
        }
        public string Password
        {
            get;
            set;
        }

        #region Consts

        private const int RESOURCE_CONNECTED = 0x00000001;
        private const int RESOURCE_GLOBALNET = 0x00000002;
        private const int RESOURCE_REMEMBERED = 0x00000003;

        private const int RESOURCETYPE_ANY = 0x00000000;
        private const int RESOURCETYPE_DISK = 0x00000001;
        private const int RESOURCETYPE_PRINT = 0x00000002;

        private const int RESOURCEDISPLAYTYPE_GENERIC = 0x00000000;
        private const int RESOURCEDISPLAYTYPE_DOMAIN = 0x00000001;
        private const int RESOURCEDISPLAYTYPE_SERVER = 0x00000002;
        private const int RESOURCEDISPLAYTYPE_SHARE = 0x00000003;
        private const int RESOURCEDISPLAYTYPE_FILE = 0x00000004;
        private const int RESOURCEDISPLAYTYPE_GROUP = 0x00000005;

        private const int RESOURCEUSAGE_CONNECTABLE = 0x00000001;
        private const int RESOURCEUSAGE_CONTAINER = 0x00000002;


        private const int CONNECT_INTERACTIVE = 0x00000008;
        private const int CONNECT_PROMPT = 0x00000010;
        private const int CONNECT_REDIRECT = 0x00000080;
        private const int CONNECT_UPDATE_PROFILE = 0x00000001;
        private const int CONNECT_COMMANDLINE = 0x00000800;
        private const int CONNECT_CMD_SAVECRED = 0x00001000;

        private const int CONNECT_LOCALDRIVE = 0x00000100;

        #endregion

        #region Errors

        private const int NO_ERROR = 0;

        private const int ERROR_ACCESS_DENIED = 5;
        private const int ERROR_ALREADY_ASSIGNED = 85;
        private const int ERROR_BAD_DEVICE = 1200;
        private const int ERROR_BAD_NET_NAME = 67;
        private const int ERROR_BAD_PROVIDER = 1204;
        private const int ERROR_CANCELLED = 1223;
        private const int ERROR_EXTENDED_ERROR = 1208;
        private const int ERROR_INVALID_ADDRESS = 487;
        private const int ERROR_INVALID_PARAMETER = 87;
        private const int ERROR_INVALID_PASSWORD = 1216;
        private const int ERROR_MORE_DATA = 234;
        private const int ERROR_NO_MORE_ITEMS = 259;
        private const int ERROR_NO_NET_OR_BAD_PATH = 1203;
        private const int ERROR_NO_NETWORK = 1222;

        private const int ERROR_BAD_PROFILE = 1206;
        private const int ERROR_CANNOT_OPEN_PROFILE = 1205;
        private const int ERROR_DEVICE_IN_USE = 2404;
        private const int ERROR_NOT_CONNECTED = 2250;
        private const int ERROR_OPEN_FILES = 2401;

        #endregion

        #region PInvoke Signatures

        [DllImport("Mpr.dll")]
        private static extern int WNetUseConnection(
            IntPtr hwndOwner,
            NETRESOURCE lpNetResource,
            string lpPassword,
            string lpUserID,
            int dwFlags,
            string lpAccessName,
            string lpBufferSize,
            string lpResult
            );

        [DllImport("Mpr.dll")]
        private static extern int WNetCancelConnection2(
            string lpName,
            int dwFlags,
            bool fForce
            );

        [StructLayout(LayoutKind.Sequential)]
        private class NETRESOURCE
        {
            public int dwScope = 0;
            public int dwType = 0;
            public int dwDisplayType = 0;
            public int dwUsage = 0;
            public string lpLocalName = "";
            public string lpRemoteName = "";
            public string lpComment = "";
            public string lpProvider = "";
        }

        #endregion

        /// <summary>
        /// Creates a NetworkShareAccesser for the given computer name. The user will be promted to enter credentials
        /// </summary>
        /// <param name="remoteComputerName"></param>
        /// <returns></returns>
        public static NetworkShareAccesser Access(string remoteComputerName)
        {
            return new NetworkShareAccesser(remoteComputerName);
        }

        /// <summary>
        /// Creates a NetworkShareAccesser for the given computer name using the given domain/computer name, username and password
        /// </summary>
        /// <param name="remoteComputerName"></param>
        /// <param name="domainOrComuterName"></param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        public static NetworkShareAccesser Access(string remoteComputerName, string domainOrComuterName, string userName, string password)
        {
            return new NetworkShareAccesser(remoteComputerName,
                                            domainOrComuterName + @"\" + userName,
                                            password);
        }

        /// <summary>
        /// Creates a NetworkShareAccesser for the given computer name using the given username (format: domainOrComputername\Username) and password
        /// </summary>
        /// <param name="remoteComputerName"></param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        public static NetworkShareAccesser Access(string remoteComputerName, string userName, string password)
        {
            return new NetworkShareAccesser(remoteComputerName,
                                            userName,
                                            password);
        }

        private NetworkShareAccesser(string remoteComputerName)
        {
            RemoteComputerName = remoteComputerName;

            this.ConnectToShare(this._remoteUncName, null, null, true);
        }

        private NetworkShareAccesser(string remoteComputerName, string userName, string password)
        {
            RemoteComputerName = remoteComputerName;
            UserName = userName;
            Password = password;

            this.ConnectToShare(this._remoteUncName, this.UserName, this.Password, false);
        }

        private void ConnectToShare(string remoteUnc, string username, string password, bool promptUser)
        {
            NETRESOURCE nr = new NETRESOURCE
            {
                dwType = RESOURCETYPE_DISK,
                lpRemoteName = remoteUnc
            };

            int result;
            if (promptUser)
            {
                result = WNetUseConnection(IntPtr.Zero, nr, "", "", CONNECT_INTERACTIVE | CONNECT_PROMPT, null, null, null);
            }
            else
            {
                result = WNetUseConnection(IntPtr.Zero, nr, password, username, 0, null, null, null);
            }

            if (result != NO_ERROR)
            {
                throw new Win32Exception(result);
            }
        }

        private void DisconnectFromShare(string remoteUnc)
        {
            int result = WNetCancelConnection2(remoteUnc, CONNECT_UPDATE_PROFILE, false);
            if (result != NO_ERROR)
            {
                throw new Win32Exception(result);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            this.DisconnectFromShare(this._remoteUncName);
        }
    }

    // ---------------------------------------------
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
    
    class Program
    {

        static void Main(string[] args)
        {
            
            
            //System.IO.File.Copy(@"\\10.150.200.78\git\tst.txt\", "D:\\test.txt", true);

            /*IDBManager dBManager = (IDBManager)Activator.CreateInstance(Type.GetType("console_middleware.DataSourceManagers.MicrosoftAccessDbManager"));

            // var myObject = (IDBManager)Activator.CreateInstance(Type.GetType("console_middleware.DataSourceManagers.MySQLDbManager"));

            MiddlewareOperations mwOperations = new MiddlewareOperations();

            mwOperations.processStores();*/


            string networkPath = @"\\10.150.200.78\c$\Windows\Temp";
            string[] arr =networkPath.Split("\\");
            // public static string networkPath = @"\\10.20.62.66\c$\Windows\Temp";

            NetworkCredential credentials = new NetworkCredential(@"Administrator", "Pos_Admin@123", "10.150.200.78");
            // public static NetworkCredential credentials = new NetworkCredential(@"admin", "", "10.20.62.66");

            /*ConnectToSharedFolder c = new ConnectToSharedFolder(networkPath, credentials);
            Console.WriteLine(File.Exists(@"\\10.150.200.78\c$\Windows\Temp\test.txt"));
            c.Dispose();*/
            using (new ConnectToSharedFolder(networkPath, credentials))
            {
                Console.WriteLine(File.Exists(@"\\10.150.200.78\c$\Windows\Temp\test.txt"));
                Console.WriteLine("SADsa'");
                DataTable myDataTable = new DataTable();
                using (var connection = new OleDbConnection($"Provider = Microsoft.ACE.OLEDB.12.0; Data Source = \\\\10.150.200.78\\c$\\Windows\\Temp\\String.mdb; Jet OLEDB:Database Password = string;"))
                //using (var connection = new OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;" + "data source=C:\\Users\\admin\\source\\repos\\WinFormsApp1\\WinFormsApp1\\viel.mdb;Jet OLEDB:Database Password=201720182019@smc"))
                {
                    connection.Open();

                    // Execute Queries
                    OleDbCommand cmd = connection.CreateCommand();
                    cmd.CommandText = "Select * From StringTable";
                    OleDbDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection); // close conn after complete

                    myDataTable.Load(reader);
                    foreach (DataRow row in myDataTable.Rows)
                    {
                        string record = "";
                        for (int i = 0; i < row.ItemArray.Length; i++)
                        {
                            if (i != row.ItemArray.Length) record += row.ItemArray[i].ToString() + ", ";
                            else record += row.ItemArray[i].ToString();
                        }
                        Console.WriteLine(record);
                    }
                }
                
            }
        
        }
    }
}
