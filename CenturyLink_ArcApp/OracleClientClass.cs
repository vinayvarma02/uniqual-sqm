using System;
using System.Data;
using System.Data.OracleClient;

namespace CenturyLink_ArcApp
{
    public static class OracleClientClass
    {
        public static string GetNearestSpeed(string WIreCenteID, double Lat, double Lon,string strAddrId)
        {
            string speed = string.Empty;
            //using (OracleConnection objConn = new OracleConnection("Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=neisl01pdb.corp.intranet)" +
            //    "(PORT =1533)))(CONNECT_DATA =(SERVICE_NAME =neisl01p)));User Id=qual_schema;Password=CtlQual2020;"))
            using (OracleConnection objConn = new OracleConnection("Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=racorad30-scan.test.intranet)" +
                "(PORT =1521)))(CONNECT_DATA =(SERVICE_NAME =neisl01t_19)));User Id=SWGISLOC;Password=CtlSwGis2020;"))
            {
                OracleCommand objCmd = new OracleCommand();
                objCmd.Connection = objConn;
                objCmd.CommandText = "SWGISLOC.SP_GET_NEAREST_HNO";
                objCmd.CommandType = CommandType.StoredProcedure;

                //objCmd.Parameters.Add("v_LAT", OracleType.Number).Value = Lat;
                //objCmd.Parameters.Add("v_LAT", OracleType.Number).Direction = ParameterDirection.Input;
                //objCmd.Parameters.Add("v_LON", OracleType.Number).Value = Lon;
                //objCmd.Parameters.Add("v_LON", OracleType.Number).Direction = ParameterDirection.Input;
                objCmd.Parameters.Add("WC_CLLI", OracleType.VarChar).Value = WIreCenteID;
                objCmd.Parameters.Add("WC_CLLI", OracleType.VarChar).Direction = ParameterDirection.Input;
                objCmd.Parameters.Add("V_ADDRESS_ID", OracleType.VarChar).Value = strAddrId;
                objCmd.Parameters.Add("V_ADDRESS_ID", OracleType.VarChar).Direction = ParameterDirection.Input;
                objCmd.Parameters.Add("DOWN_SPEED", OracleType.VarChar,100).Direction = ParameterDirection.Output;
              //  objCmd.Parameters.Add("v_addressid", OracleType.VarChar, 100).Direction = ParameterDirection.;

                try
                {
                    objConn.Open();
                    objCmd.ExecuteNonQuery();
                    int val;
                    if (objCmd.Parameters["DOWN_SPEED"].Value.ToString() == "")
                    { val = 0; } 
                    else
                    { val = Convert.ToInt32(objCmd.Parameters["DOWN_SPEED"].Value); val = val * 100; }
                        
                    speed = val.ToString();
                }
                catch (Exception ex)
                {
                   // LogManager.WriteLogandConsole("Exception: {0}"+ ex.ToString());
                }
                finally { objConn.Close(); }
                return speed;
            }
        }



        //public static OracleConnection Getmartinloopconnection()
        //{
        //    using (OracleConnection objConn = new OracleConnection("Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=neisl01pdb.corp.intranet)" +
        //        "(PORT =1533)))(CONNECT_DATA =(SERVICE_NAME =neisl01p)));User Id=qual_schema;Password=CtlQual2020;"))
        //    {
        //        OracleCommand objCmd = new OracleCommand();
        //        objCmd.Connection = objConn;
        //        try
        //        {
        //            objConn.Open();
                   
        //        }
        //        catch (Exception ex)
        //        {
        //            // LogManager.WriteLogandConsole("Exception: {0}"+ ex.ToString());
        //        }
        //        // finally { objConn.Close(); }
        //        return objConn;
        //    }
        //}
    }

}



