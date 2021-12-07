using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OracleClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFMVVM_ORACLETEST.Database
{
    public sealed class OracleDBManager : Singleton<OracleDBManager>
    {
        public event ExceptionEventHandler ExceptionEvent;
        public string LastExceptionString = string.Empty;
        public string ConnectionString = string.Empty;
        public string Address = string.Empty;
        public string Port = string.Empty;
        private OracleCommand LastExecutedCommand = null;
        private int RetryCnt = 0;

        public bool IsRunning { get { return CheckDBConnected(); } }
        public OracleConnection Connection { get; private set; }
        public OracleDBManager() { }
        public static OracleDBManager GetNewInstanceConnection()
        { if (Instance == null) return null;
            OracleDBManager dbManager = new OracleDBManager
            { ConnectionString = Instance.ConnectionString };
            dbManager.GetConnection();
            dbManager.ExceptionEvent = Instance.ExceptionEvent;
            return dbManager;
        }
        public bool GetConnection()
        {
            try
            {
                if (this.Connection != null) { this.Connection.Close(); this.Connection.Dispose(); this.Connection = null; }
                if (ConnectionString == string.Empty) SetConnectionString();
                Connection = new OracleConnection(ConnectionString);
                if (this.Address != string.Empty) //주소가 없을 경우 그냥 리턴
                    Connection.Open(); }
            catch (Exception ex)
            {
                System.Reflection.MemberInfo info = System.Reflection.MethodInfo.GetCurrentMethod();
                string id = string.Format("{0}.{1}", info.ReflectedType.Name, info.Name);
                if (this.ExceptionEvent != null) this.ExceptionEvent(id, ex); return false;
            }
            if (Connection.State == ConnectionState.Open) return true;
            else return false;
        }
        public int ExecuteNonQuery(string query)
        {
            lock (this)
            { RetryCnt = 0; int result = Execute_NonQuery(query); return result; }
        }
        public int ExecuteNonQuery(string query, params object[] oParams)
        {
            lock (this) { RetryCnt = 0; int result = Execute_NonQuery(query, oParams); return result; }
        }
        public bool HasRows(string query)
        {
            lock (this)
            {
                RetryCnt = 0;
                OracleDataReader result = ExecuteReader(query);
                return result.HasRows;
            }
        }
        public OracleDataReader ExecuteReaderQuery(string query)
        {
            lock (this)
            { RetryCnt = 0;
                OracleDataReader result = ExecuteReader(query);
                return result;
            }
        }
        public DataSet ExecuteDsQuery(DataSet ds, string query) 
        { 
            ds.Reset(); 
            lock (this) 
            { RetryCnt = 0; 
                return ExecuteDataAdt(ds, query); 
            } 
        } 
        public int ExecuteProcedure(string procName, params string[] pValues) 
        { 
            lock (this) 
            { 
                RetryCnt = 0; 
                return ExecuteProcedureAdt(procName, pValues); 
            } 
        } 
        public void Close() 
        { 
            Connection.Close(); 
        } 
        public void QueryCancel() 
        { 
            if (this.LastExecutedCommand != null) 
            {
                this.LastExecutedCommand.Cancel(); 
            } 
        }
        #region private.......................................................... 
        private void SetConnectionString() 
        {
            string user = XmlManager.GetValue("DATABASE", "USER"); 
            string pwd = XmlManager.GetValue("DATABASE", "PWD"); 
            string port = XmlManager.GetValue("DATABASE", "PORT"); 
            string svr = XmlManager.GetValue("DATABASE", "SERVICE_NAME"); 
            string addr01 = XmlManager.GetValue("DATABASE", "D_ADDR01"); 
            string addr02 = XmlManager.GetValue("DATABASE", "D_ADDR02"); 
            string address01 = string.Format("(ADDRESS = (PROTOCOL = TCP)(HOST = {0})(PORT = {1}))", addr01, port); 
            string address02 = string.Format("(ADDRESS = (PROTOCOL = TCP)(HOST = {0})(PORT = {1}))", addr02, port); 
            string dataSource = string.Format(@"(DESCRIPTION =(ADDRESS_LIST ={0}{1})(CONNECT_DATA =(", address01, address02); 
            dataSource += svr == string.Empty ? string.Format("SID = {0})))", svr) : string.Format("SERVICE_NAME = {0})))", svr); 

            this.Address = addr01; 
            this.Port = port; 
            this.ConnectionString = "User Id=" + user + ";Password=" + pwd + ";Data Source=" + dataSource; 
        } 
        private int Execute_NonQuery(string query) 
        { 
            int result = 0; 
            try 
            { 
                OracleCommand cmd = new OracleCommand 
                { 
                    Connection = this.Connection, CommandText = query 
                }; 
                LastExecutedCommand = cmd; 
                result = cmd.ExecuteNonQuery(); 
            } 
            catch (Exception ex) 
            { 
                //연결 해제 여부 확인 후 해제 시 재연결 후 다시 시도...
                if (RetryCnt < 1 && CheckDBConnected() == false) 
                { 
                    RetryCnt++; GetConnection(); Exception ex02 = new Exception("Reconnect to database"); 
                    if (this.ExceptionEvent != null) this.ExceptionEvent(string.Empty, ex02); 
                    result = Execute_NonQuery(query); 
                    return result; 
                } //사용자 Cancel
                if (ex.Message.Contains("ORA-01013")) 
                { 
                    return -1; 
                } 
                System.Reflection.MemberInfo info = System.Reflection.MethodInfo.GetCurrentMethod(); 
                string id = string.Format("{0}.{1}\n[{2}]", info.ReflectedType.Name, info.Name, query); 
                if (this.ExceptionEvent != null) this.ExceptionEvent(id, ex); 
                this.LastExceptionString = ex.Message; 
                result = -1; 
            } 
            return result; 
        } 
        private int Execute_NonQuery(string query, params object[] oParams) 
        { 
            int result = 0; 
            try 
            { 
                OracleCommand cmd = new OracleCommand 
                { 
                    Connection = this.Connection, CommandText = query 
                }; 
                for (int i = 0; i < oParams.Length; i += 2) 
                { 
                    cmd.Parameters.Add(oParams[i].ToString(), oParams[i + 1]); 
                } 
                LastExecutedCommand = cmd; result = cmd.ExecuteNonQuery(); 
            } 
            catch (Exception ex) 
            { //연결 해제 여부 확인 후 해제 시 재연결 후 다시 시도...
              if (RetryCnt < 1 && CheckDBConnected() == false) 
                { RetryCnt++; 
                  GetConnection(); 
                  Exception ex02 = new Exception("Reconnect to database"); 
                  if (this.ExceptionEvent != null) this.ExceptionEvent(string.Empty, ex02); 
                  result = Execute_NonQuery(query, oParams); 
                  return result; 
            } //사용자 Cancel
              if (ex.Message.Contains("ORA-01013")) 
                { 
                    return -1; 
                } 
                System.Reflection.MemberInfo info = System.Reflection.MethodInfo.GetCurrentMethod(); 
                string id = string.Format("{0}.{1}\n[{2}]", info.ReflectedType.Name, info.Name, query); 
                if (this.ExceptionEvent != null) 
                    this.ExceptionEvent(id, ex); 
                this.LastExceptionString = ex.Message; result = -1; 
            } 
            return result; 
        } 
        private OracleDataReader ExecuteReader(string query) 
        { OracleDataReader result = null; 
            try 
            { 
                OracleCommand cmd = new OracleCommand { Connection = this.Connection, CommandText = query }; 
                LastExecutedCommand = cmd; 
                result = cmd.ExecuteReader(); 
            } 
            catch (Exception ex) 
            { //연결 해제 여부 확인 후 해제 시 재연결 후 다시 시도...
              if (RetryCnt < 1 && CheckDBConnected() == false) 
                { 
                    RetryCnt++; 
                    GetConnection(); 
                    Exception ex02 = new Exception("Reconnect to database"); 
                    if (this.ExceptionEvent != null) 
                        this.ExceptionEvent(string.Empty, ex02); 
                    result = ExecuteReader(query); 
                    return result; 
                } //사용자 Cancel
                  if (ex.Message.Contains("ORA-01013")) 
                { 
                    return null; 
                } 
                System.Reflection.MemberInfo info = System.Reflection.MethodInfo.GetCurrentMethod(); 
                string id = string.Format("{0}.{1}\n[{2}]", info.ReflectedType.Name, info.Name, query); 
                if (this.ExceptionEvent != null) 
                    this.ExceptionEvent(id, ex); 
                this.LastExceptionString = ex.Message; 
                return null; 
            } 
            return result; 
        } 
        private DataSet ExecuteDataAdt(DataSet ds, string query) 
        { 
            try 
            { 
                OracleDataAdapter cmd = new OracleDataAdapter 
                { 
                    SelectCommand = new OracleCommand() 
                }; 
                cmd.SelectCommand.Connection = this.Connection; 
                cmd.SelectCommand.CommandText = query; 
                LastExecutedCommand = cmd.SelectCommand; 
                cmd.Fill(ds); 
            } 
            catch (Exception ex) 
            { //연결 해제 여부 확인 후 해제 시 재연결 후 다시 시도...
              if (RetryCnt < 1 && CheckDBConnected() == false) 
                { 
                    RetryCnt++; 
                    GetConnection(); 
                    Exception ex02 = new Exception("Reconnect to database"); 
                    if (this.ExceptionEvent != null) this.ExceptionEvent(string.Empty, ex02); 
                    ds = ExecuteDataAdt(ds, query); 
                    return ds; 
                } //사용자 Cancel
                  if (ex.Message.Contains("ORA-01013")) 
                { 
                    return null; 
                } 
                System.Reflection.MemberInfo info = System.Reflection.MethodInfo.GetCurrentMethod(); 
                string id = string.Format("{0}.{1}\n[{2}]", info.ReflectedType.Name, info.Name, query); 
                if (this.ExceptionEvent != null) this.ExceptionEvent(id, ex); 
                this.LastExceptionString = ex.Message; return null; 
            } 
            return ds; 
        } 
        private int ExecuteProcedureAdt(string procName, params string[] pValues) 
        { 
            int result = -1; 
            try 
            { 
                OracleCommand cmd = new OracleCommand(procName, this.Connection) 
                { 
                    CommandType = CommandType.StoredProcedure 
                }; 
                for (int i = 0; i < pValues.Length; ++i) 
                { 
                    string par = string.Format("PARAM{0}", i + 1); 
                    cmd.Parameters.Add(par, OracleType.VarChar).Value = pValues[i]; 
                } 
                cmd.Parameters.Add("execResult", OracleType.Int32); 
                cmd.Parameters["execResult"].Direction = ParameterDirection.Output; 
                LastExecutedCommand = cmd; cmd.ExecuteNonQuery(); 
                result = int.Parse(cmd.Parameters["execResult"].Value.ToString()); 
            } 
            catch (Exception ex) 
            { //연결 해제 여부 확인 후 해제 시 재연결 후 다시 시도...
              if (RetryCnt < 1 && CheckDBConnected() == false) 
                { 
                    RetryCnt++; 
                    GetConnection(); 
                    Exception ex02 = new Exception("Reconnect to database"); 
                    if (this.ExceptionEvent != null) this.ExceptionEvent(string.Empty, ex02); 
                    result = ExecuteProcedureAdt(procName, pValues); 
                    return result; 
                } //사용자 Cancel
                  if (ex.Message.Contains("ORA-01013")) 
                { 
                    return result; 
                } 
                System.Reflection.MemberInfo info = System.Reflection.MethodInfo.GetCurrentMethod(); 
                string id = string.Format("{0}.{1}\n[{2}]", info.ReflectedType.Name, info.Name, procName); 
                if (this.ExceptionEvent != null) this.ExceptionEvent(id, ex); 
                this.LastExceptionString = ex.Message; 
            } 
            return result; 
        } 
        private bool CheckDBConnected() 
        { 
            string query = "SELECT 1 FROM DUAL"; 
            OracleDataReader result = null; 
            try 
            { 
                OracleCommand cmd = new OracleCommand { Connection = this.Connection, CommandText = query }; 
                result = cmd.ExecuteReader(); 
            } 
            catch 
            { } 
            if (result != null && result.HasRows) 
                return true; 
            return false; 
        } 
        #endregion private.................................................................. }

    }
}
