using System;
using Oracle.ManagedDataAccess.Client;
namespace DbLibrary
{
    public class DbAdapter
    {
        public string ConnectionString { set; get; }
        public OracleConnection OracleConn { set; get; }

        public string GetQuery(string[] columnNames, string tableName, string whereClause = "", string orderByClause = "")
        {
            string columns = string.Join(",", columnNames);
            string sqlQuery = "Select " + columns + " from " + tableName + (string.IsNullOrEmpty(whereClause) ? string.Empty : (" where " + whereClause)) + " " + orderByClause;
            return sqlQuery;
        }
        public DbAdapter(string username, string password, string ipAddress, string portNumber, string database)
        {
            this.ConnectionString = "Data Source=(DESCRIPTION=(ADDRESS_LIST=" +
                "(ADDRESS=(PROTOCOL=TCP)(HOST=" + ipAddress + ")(PORT =" + portNumber + ")))" +
                "(CONNECT_DATA =" + "(SERVICE_NAME ="+ database + ")));" + "User Id="+ username + ";Password="+ password + ";";
            OracleConn = new OracleConnection(this.ConnectionString);
        }
        public bool Open()
        {
            try
            {
                OracleConn.Open();
            }
            catch (Exception ex)
            {
               
                return false;
            }

            return true;
        }
        

    }
}
