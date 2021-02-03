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
        
        public static void LoadServerConnection(bool isQualConnection = true,bool isloopQual = false,int lqDBval = 1)
        {
            try
            {
                DBAdapterSetting = new Dictionary<DbParameterEnum, string>();
                if (DBKey.Default.RunMode == "0") //CTL Connections
                {
                    if(isloopQual)
                    {
                        if(lqDBval == 1) // martin
                        {
                            DBAdapterSetting.Add(DbParameterEnum.IpAddress, DBKey.Default.lqmartinServer);
                            DBAdapterSetting.Add(DbParameterEnum.PortNumber, DBKey.Default.lqPort);
                            DBAdapterSetting.Add(DbParameterEnum.DatabaseName, DBKey.Default.lqmartinDatabase);
                            DBAdapterSetting.Add(DbParameterEnum.UserName, DBKey.Default.lqmartinUserID);
                            DBAdapterSetting.Add(DbParameterEnum.Password, DBKey.Default.lqmartinPassword);

                        }
                        else if(lqDBval == 2) // Lfacs
                        {
                            DBAdapterSetting.Add(DbParameterEnum.IpAddress, DBKey.Default.lqLfacsServer);
                            DBAdapterSetting.Add(DbParameterEnum.PortNumber, DBKey.Default.lqPort);
                            DBAdapterSetting.Add(DbParameterEnum.DatabaseName, DBKey.Default.lqLfacsDatabase);
                            DBAdapterSetting.Add(DbParameterEnum.UserName, DBKey.Default.lqLfacsUserID);
                            DBAdapterSetting.Add(DbParameterEnum.Password, DBKey.Default.lqLfacsPassword);

                        }
                        return;
                    }
                    if (isQualConnection)
                    {
                        DBAdapterSetting.Add(DbParameterEnum.IpAddress, DBKey.Default.qual_Server);
                        DBAdapterSetting.Add(DbParameterEnum.PortNumber, DBKey.Default.Qual_PORT);
                        DBAdapterSetting.Add(DbParameterEnum.DatabaseName, DBKey.Default.qual_DATABASE);
                        DBAdapterSetting.Add(DbParameterEnum.UserName, DBKey.Default.USER_QS);
                        DBAdapterSetting.Add(DbParameterEnum.Password, DBKey.Default.PASSWORD_QS);
                    }
                    else
                    {
                        DBAdapterSetting.Add(DbParameterEnum.IpAddress, DBKey.Default.qual_Server);
                        DBAdapterSetting.Add(DbParameterEnum.PortNumber, DBKey.Default.Qual_PORT);
                        DBAdapterSetting.Add(DbParameterEnum.DatabaseName, DBKey.Default.qual_DATABASE);
                        DBAdapterSetting.Add(DbParameterEnum.UserName, DBKey.Default.USER_SWGISLOC);
                        DBAdapterSetting.Add(DbParameterEnum.Password, DBKey.Default.PASSWORD_SWGISLOC);
                    }
                }
                else if(DBKey.Default.RunMode == "1") //lodal dev connections
                {
                    if (isQualConnection)
                    {
                        DBAdapterSetting.Add(DbParameterEnum.IpAddress, "172.16.8.144");
                        DBAdapterSetting.Add(DbParameterEnum.PortNumber, "1521");
                        DBAdapterSetting.Add(DbParameterEnum.DatabaseName, "orclcp");
                        DBAdapterSetting.Add(DbParameterEnum.UserName, "qual_schema");
                        DBAdapterSetting.Add(DbParameterEnum.Password, "cyient#3");
                    }
                    else
                    {
                        DBAdapterSetting.Add(DbParameterEnum.IpAddress, "172.16.8.144");
                        DBAdapterSetting.Add(DbParameterEnum.PortNumber, "1521");
                        DBAdapterSetting.Add(DbParameterEnum.DatabaseName, "orclcp");
                        DBAdapterSetting.Add(DbParameterEnum.UserName,"SWGISLOC");
                        DBAdapterSetting.Add(DbParameterEnum.Password, "SWGISLOC");
                    }
                }
            }
            catch (Exception ex)
            {// LogManager.Log(ex);
            }

        }


        
    }
}
