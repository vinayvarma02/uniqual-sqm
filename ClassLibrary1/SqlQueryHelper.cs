using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Key = DbLibrary.DbParameterEnum;
using DBKey = DbLibrary.Properties.Settings;

namespace DbLibrary
{
    public class SqlQueryHelper
    {
        public static Dictionary<DbParameterEnum, string> DBAdapterSetting { set; get; }
      
        static  SqlQueryHelper()
        { 
           // DBAdapterSetting = new Dictionary<DbParameterEnum, string>();
            LoadServerConnection();
        }

        public static DbAdapter GetDbAdapter(Dictionary<DbParameterEnum, string> dbSettings)
        {
           
            return new DbAdapter(dbSettings[Key.UserName], dbSettings[Key.Password],
                dbSettings[Key.IpAddress], dbSettings[Key.PortNumber], dbSettings[Key.DatabaseName]);
        }
        
        public static void LoadServerConnection(bool isQualConnection = true)
        {
            try
            {
                DBAdapterSetting = new Dictionary<DbParameterEnum, string>();
                if (DBKey.Default.RunMode == "0") //CTL Connections
                {
                    if (isQualConnection)
                    {
                        DBAdapterSetting.Add(DbParameterEnum.IpAddress, DBKey.Default.qual_Server);// "172.16.8.144");
                        DBAdapterSetting.Add(DbParameterEnum.PortNumber, DBKey.Default.Qual_PORT);// "1521");
                        DBAdapterSetting.Add(DbParameterEnum.DatabaseName, DBKey.Default.qual_DATABASE);// "auroradb");
                        DBAdapterSetting.Add(DbParameterEnum.UserName, DBKey.Default.USER_QS);// "qual_schema");
                        DBAdapterSetting.Add(DbParameterEnum.Password, DBKey.Default.PASSWORD_QS);// "cyient#3");
                    }
                    else
                    {
                        DBAdapterSetting.Add(DbParameterEnum.IpAddress, DBKey.Default.qual_Server);// "172.16.8.144");
                        DBAdapterSetting.Add(DbParameterEnum.PortNumber, DBKey.Default.Qual_PORT);// "1521");
                        DBAdapterSetting.Add(DbParameterEnum.DatabaseName, DBKey.Default.qual_DATABASE);// "auroradb");
                        DBAdapterSetting.Add(DbParameterEnum.UserName, DBKey.Default.USER_SWGISLOC);// "SWGISLOC");
                        DBAdapterSetting.Add(DbParameterEnum.Password, DBKey.Default.PASSWORD_SWGISLOC);// "SWGISLOC");
                    }
                }
                else if(DBKey.Default.RunMode == "1") //lodal dev connections
                {
                    if (isQualConnection)
                    {
                        DBAdapterSetting.Add(DbParameterEnum.IpAddress, "172.16.8.144");// "172.16.8.144");
                        DBAdapterSetting.Add(DbParameterEnum.PortNumber, "1521");// "1521");
                        DBAdapterSetting.Add(DbParameterEnum.DatabaseName, "orclcp");// "auroradb");
                        DBAdapterSetting.Add(DbParameterEnum.UserName, "qual_schema");// "qual_schema");
                        DBAdapterSetting.Add(DbParameterEnum.Password, "cyient#3");// "cyient#3");
                    }
                    else
                    {
                        DBAdapterSetting.Add(DbParameterEnum.IpAddress, "172.16.8.144");// "172.16.8.144");
                        DBAdapterSetting.Add(DbParameterEnum.PortNumber, "1521");// "1521");
                        DBAdapterSetting.Add(DbParameterEnum.DatabaseName, "orclcp");// "auroradb");
                        DBAdapterSetting.Add(DbParameterEnum.UserName,"SWGISLOC");// "SWGISLOC");
                        DBAdapterSetting.Add(DbParameterEnum.Password, "SWGISLOC");// "SWGISLOC");
                    }
                }
            }
            catch (Exception ex)
            {// LogManager.Log(ex);
            }

        }



    }
}
