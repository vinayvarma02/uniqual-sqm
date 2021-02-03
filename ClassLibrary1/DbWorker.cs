using System;
using System.Data;
using Oracle.ManagedDataAccess.Client;
//using System.Data.OracleClient;

namespace DbLibrary
{
    public  class DbWorker
    {

        public string GetQuery(string[] columnNames, string tableName, string whereClause = "", string orderByClause = "")
        {
            string columns = string.Join(",", columnNames);
            string sqlQuery = "Select " + columns + " from " + tableName + (string.IsNullOrEmpty(whereClause) ? string.Empty : (" where " + whereClause)) + " " + orderByClause;
            return sqlQuery;
        }
        public int Getintvalue(string query) //,out Exception dBException
        {
            // dBException = null;
            DbAdapter adapter = null;
            int value = 0;
            try
            {
                adapter = SqlQueryHelper.GetDbAdapter(SqlQueryHelper.DBAdapterSetting);
                value = Convert.ToInt32(GetCommandForAdapter(adapter, query).ExecuteScalar());
            }
            catch (Exception ex)
            {

            }
            finally
            {
                adapter.OracleConn.Close();
            }
            return value;
        }
        public string Getstringvalue(string query) //,out Exception dBException
        {
            // dBException = null;
            DbAdapter adapter = null;
            string value = string.Empty;
            try
            {
                adapter = SqlQueryHelper.GetDbAdapter(SqlQueryHelper.DBAdapterSetting);
                var var = GetCommandForAdapter(adapter, query).ExecuteScalar();
                if(var!=null)
                    value = GetCommandForAdapter(adapter, query).ExecuteScalar().ToString();
            }
            catch (Exception ex)
            {
               //  Logger.LogManager.Log(ex);
            }
            finally
            {
                adapter.OracleConn.Close();
            }
            return value;
        }

        public int RunDmlQuery(string query) 
        {
            DbAdapter adapter = null;
            int rowAffected = 0;
            try
            {
                adapter = SqlQueryHelper.GetDbAdapter(SqlQueryHelper.DBAdapterSetting);
                rowAffected = GetCommandForAdapter(adapter, query).ExecuteNonQuery();
            }
            catch (Exception ex)
            {
               // dBException = ex;
              //  Logger.LogManager.Log(ex);
            }
            finally
            {
                adapter.OracleConn.Close();
            }
            return rowAffected;
        }


        public int RunDmlDeleteQuery(string query) //,out Exception dBException
        {
            DbAdapter adapter = null;
            int rowAffected = 0;
            try
            {
                rowAffected = GetCommandForAdapter(adapter, query).ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                // dBException = ex;
                //  Logger.LogManager.Log(ex);
            }
            finally
            {
                adapter.OracleConn.Close();
            }
            return rowAffected;
        }

        public DataTable ReadTable(string query,out Exception dbException)
        {
            dbException = null;
            DbAdapter adapter = null;
            DataTable dtInfo = new DataTable();
            try
            {
              
                adapter = SqlQueryHelper.GetDbAdapter(SqlQueryHelper.DBAdapterSetting);
                OracleDataAdapter dataHolder = new OracleDataAdapter(GetCommandForAdapter(adapter, query));
                //  OracleDataReader dataHolder = new OracleDataReader(); //GetCommandForAdapter(adapter, query).ExecuteReader();
                dataHolder.Fill(dtInfo);

               // dataHolder.Fill(dtInfo);
                //dtInfo.Load(dataHolder);// dataHolder.Fill(dtInfo);
            }
            catch (Exception ex)
            {
                dbException = ex;
               
            }
            finally { adapter.OracleConn.Close(); }
            return dtInfo;
        }


        private OracleCommand GetCommandForAdapter(DbAdapter adapter, string query)
        {
            OracleCommand searchCmd = null;
            try
            {
                searchCmd = new OracleCommand(query);
                adapter.Open();
                searchCmd.CommandType = CommandType.Text;
                searchCmd.Connection = adapter.OracleConn;
            }
            catch (Exception ex)
            {
              //  Logger.LogManager.Log(ex);
            }
            return searchCmd;
        }
        public string GetSpeedfromPro(string WIreCenteID,double Lat,double Lon)
        {            
            OracleCommand _oracleCommand = null;
            DbAdapter adapter = null;
            string val = string.Empty;
            try
            {
                adapter = SqlQueryHelper.GetDbAdapter(SqlQueryHelper.DBAdapterSetting);
                adapter.Open();
                _oracleCommand = new OracleCommand("SWGISLOC.SP_GET_NEAREST_HNO",adapter.OracleConn);
                  _oracleCommand.BindByName = true;
                _oracleCommand.CommandType = CommandType.StoredProcedure;
                _oracleCommand.Parameters.Add("WC_CLLI", OracleDbType.Varchar2, WIreCenteID, ParameterDirection.Input);
                _oracleCommand.Parameters.Add("v_LAT", OracleDbType.Varchar2,Lat , ParameterDirection.Input);
                _oracleCommand.Parameters.Add("v_LON", OracleDbType.Varchar2, Lon, ParameterDirection.Input);
               _oracleCommand.Parameters.Add("DOWN_SPEED", OracleDbType.Varchar2).Direction = ParameterDirection.Output;
               // _oracleCommand.Parameters.Add("DOWN_SPEED", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
                 _oracleCommand.ExecuteNonQuery();
                val = _oracleCommand.Parameters["DOWN_SPEED"].Value.ToString();
             }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                
            }
            return val;

        }

    }
}
