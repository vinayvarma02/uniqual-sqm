using DbLibrary;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using static CenturyLink_ArcApp.Program;
using configKey = CenturyLink_ArcApp.Properties.Settings;

namespace CenturyLink_ArcApp
{
    public class Jobs : IJob
    {
        public static Dictionary<string, List<string>> dictLocNotFound;
        public string jobTableName;
        public string jobCreateDate { set; get; }
        public string designations { get; set; }
        public DateTime jobDate
        {
            get;
            set;
        }
        public int jobID { get; set; }
        public string jobOwner
        {
            get;


            set;

        }
        public int jobStatus
        {
            get;


            set;

        }
        public List<IPolygon> NeedtoModifyPolygons
        {
            get;
            set;
        }
        public IFeatureClass OutFeatureClass
        {
            get;
            set;
        }
        public string wireCenterID
        {
            get;
            set;

        }
        public int jobExecutionStatus
        {
            get;
            set;
        }
        public int jobStartDate
        {
            get;
            set;

        }
        public int jobEndDate
        {
            get;
            set;

        }
        public int qualType { get; set; }
        public int source { get; set; }
        public int qualStatus { get; set; }
        public string wireCenterName { get; set; }
        public int polygonsSuccess { get; set; }
        public int polygonsFailed { get; set; }
        public int ProcessedDesignations { get; internal set; }
        public int constDesgRecieved { get; set; }
        public int constDesgProcessed { get; set; }
        public int polygonswithExceptions { get; set; }

        public List<string> deadDesignation = new List<string>();
        public Dictionary<string, List<string>> designationLst;

        public Dictionary<string, List<string>> dictfailed;
        public Dictionary<string, List<string>> dictsuccess;
        public Dictionary<string, List<string>> dictexception;

        public int createmode { get; set; }

        public Jobs()
        {
            LoadDeadDesgtoLst();
        }

        private void LoadDeadDesgtoLst()
        {
            deadDesignation = new List<string>();
            string deadDegfilePath = AppDomain.CurrentDomain.BaseDirectory + configKey.Default.DeadDesignationsFileName;
            List<string> filedeadDesignation = System.IO.File.ReadLines(deadDegfilePath).ToList();
            foreach (var item in filedeadDesignation)
            {
                deadDesignation.Add(item.ToUpper());
            }

        }

        public static Jobs CreatInstance(DataRow dr, string jobID)
        {
            Jobs currentJob = new Jobs();
            try
            {
                currentJob.jobID = Convert.ToInt16(jobID);
                currentJob.wireCenterID = (dr[0].ToString());
                currentJob.designations = (dr[1].ToString());
                currentJob.jobStatus = Convert.ToInt16(dr[2].ToString());
                currentJob.qualStatus =  Convert.ToInt16(dr[4].ToString());
                if (currentJob.qualStatus != 3)
                    currentJob.qualType = Convert.ToInt16(dr[3].ToString());
                currentJob.wireCenterName = GetWireCenterNamefromID(currentJob.wireCenterID);
                currentJob.createmode = Convert.ToInt16(dr[5].ToString());
                currentJob.source = Convert.ToInt16(dr[6].ToString());
                currentJob.designationLst = new Dictionary<string, List<string>>();
                if (currentJob.qualStatus != 3)
                {
                    string[] desigSplit = currentJob.designations.Split(',');
                    foreach (var item in desigSplit)
                    {
                        if (!string.IsNullOrEmpty(item) && !currentJob.designationLst.ContainsKey(item) && !currentJob.deadDesignation.Contains(item.ToUpper()))
                            currentJob.designationLst.Add(item, new List<string> { JobPolygonStatusProgressEnum.deignationCreated.ToString(), "" });
                    }
                }
            }
            catch (Exception ex) { LogManager.WriteLogandConsole(ex); isExecutionResult = false; }
            return currentJob;
        }

        #region DB & SDE Methods

        /// <summary>
        /// Delete Termina View
        /// </summary>
        /// <param name="dbViewName"></param>
        internal void DeleteView(string dbViewName)
        {
            try
            { 
                string query = "drop view " + dbViewName;
                DbWorker dbWorker = new DbWorker();
                int rowsAffected = dbWorker.RunDmlQuery(query);
            }
            catch (Exception ex) { LogManager.WriteLogandConsole(ex); isExecutionResult = false; }
        }
        internal void DeleteRecords(string tablename, string wirecenterid)
        {
            try
            {
                string query = "delete from " + tablename + " where WC_ID='" + wirecenterid + "'";
                DbWorker dbWorker = new DbWorker();
                int rowsAffected = dbWorker.RunDmlQuery(query);
            }
            catch (Exception ex) { LogManager.WriteLogandConsole(ex); isExecutionResult = false; }
        }
        internal void DeleteTable(string dbViewName)
        {
            try
            { //drop view "SWGISLOC"."TERMINAL_DATA4SQM_1686"
                string query = "drop table " + dbViewName;
                DbWorker dbWorker = new DbWorker();
                int rowsAffected = dbWorker.RunDmlQuery(query);
            }
            catch (Exception ex) { LogManager.WriteLogandConsole(ex); isExecutionResult = false; }
        }

        public string GetWCID(string wcid)
        {
            string wirecenter = "";
            try
            {
                string query = "select wc_id from cl_wire_center where  id='" + wcid + "'";
                DbWorker dbWorker = new DbWorker();
                wirecenter = dbWorker.Getstringvalue(query);
            }
            catch (Exception ex) { LogManager.WriteLogandConsole(ex); isExecutionResult = false; }
            return wirecenter;
        }
        public string[] GeoCodeMain(string[] args, string pxLicenseFile, string pxLicenseCode)
        {
            string[] opLatLon = new string[2];
            string[] dataStuff = new string[args.Length - 5];
            for (int i = 5; i < args.Length; i++)
                dataStuff[i - 5] = args[i];
            SingleCallGeocode scg = new SingleCallGeocode(args[0], pxLicenseFile, pxLicenseCode, dataStuff);
            opLatLon = scg.DoGeocode();
            return opLatLon;
        }

        public void CreateCopperTables(string wcid, string jobId, string WCLFAX)
        {
            try
            {
                string AddressQuery = string.Empty;
                if (string.IsNullOrEmpty(designations) || designations.Contains("Xmin"))
                {
                    AddressQuery = "create table LU_" + wcid + "_" + jobId + " as SELECT MAX(to_number(a.mod_speed_down)) AS mod_speed_down,B.address_id,b.house_number, b.street_name,b.community,b.state,b.zip_code,b.serving_terminal FROM " +
                      " (SELECT DISTINCT sacf.wire_center_clli, sa.address_id,sacf.cala,sacf.house_number,sacf.street_name,sacf.unit,sacf.floor,sacf.building,sacf.community,sacf.state," +
                      " sacf.zip_code,sa.serving_terminal,sacf.tar_code,sacf.contract_type,sacf.datum_code,sa.circuit_identifier,sacf.change_date FROM service_address_custom_fields@loop_qual_lfacs" +
                      " sacf, " + configKey.Default.service_addresses + " sa WHERE sa.wire_center_clli(+) = sacf.wire_center_clli AND sa.house_number(+) = sacf.house_number AND sa.street_name(+) = sacf.street_name AND " +
                      " nvl(sa.unit(+), '.') = sacf.unit AND nvl(sa.floor(+), '.') = sacf.floor AND nvl(sa.building(+), '.') = sacf.building AND sa.wire_center_clli = '" + WCLFAX + "' UNION " +
                      " SELECT DISTINCT sacf.wire_center_clli,sa.address_id,  sacf.cala,  sacf.house_number, sacf.street_name, sacf.unit,  sacf.floor,  sacf.building, sacf.community,  sacf.state, " +
                      " sacf.zip_code, sa.serving_terminal, sacf.tar_code,  sacf.contract_type,  sacf.datum_code,  sa.circuit_identifier, sacf.change_date FROM service_address_custom_fields@loop_qual_marten " +
                      "   sacf, " + configKey.Default.service_addresses + " sa WHERE  sa.wire_center_clli(+) = sacf.wire_center_clli  AND sa.house_number(+) = sacf.house_number  AND sa.street_name(+) = sacf.street_name " +
                      " AND nvl(sa.unit(+), '.') = sacf.unit  AND nvl(sa.floor(+), '.') = sacf.floor  AND nvl(sa.building(+), '.') = sacf.building  AND sa.wire_center_clli = '" + WCLFAX + "') b" +
                      "   LEFT JOIN swgisloc.living_units a ON a.wire_center_id = b.wire_center_clli  AND a.luid = b.address_id AND a.serving_terminal = b.serving_terminal GROUP BY mod_speed_down, B.address_id, house_number, street_name, " +
                      " b.community, b.state,  b.zip_code ,B.serving_terminal";
                }
                else
                {
                    string[] addresses = designations.Split(',');
                    string addressid = string.Empty;
                    for (int i = 0; i < addresses.Length; i++)
                    {
                        if (i == 0)
                            addressid = "'" + addresses[i].ToString() + "'";
                        else
                            addressid += ",'" + addresses[i].ToString() + "'";

                    }
                    AddressQuery = "create table LU_" + wcid + "_" + jobId + " as SELECT MAX(to_number(a.mod_speed_down)) AS mod_speed_down,B.address_id,b.house_number, b.street_name,b.community,b.state,b.zip_code,b.serving_terminal FROM " +
                   " (SELECT DISTINCT sacf.wire_center_clli, sa.address_id,sacf.cala,sacf.house_number,sacf.street_name,sacf.unit,sacf.floor,sacf.building,sacf.community,sacf.state," +
                   " sacf.zip_code,sa.serving_terminal,sacf.tar_code,sacf.contract_type,sacf.datum_code,sa.circuit_identifier,sacf.change_date FROM service_address_custom_fields@loop_qual_lfacs" +
                   " sacf, " + configKey.Default.service_addresses + " sa WHERE sa.wire_center_clli(+) = sacf.wire_center_clli AND sa.house_number(+) = sacf.house_number AND sa.street_name(+) = sacf.street_name AND " +
                   " nvl(sa.unit(+), '.') = sacf.unit AND nvl(sa.floor(+), '.') = sacf.floor AND nvl(sa.building(+), '.') = sacf.building AND sa.wire_center_clli = '" + WCLFAX + "' AND sa.address_id in (" + addressid + ") UNION " +
                   " SELECT DISTINCT sacf.wire_center_clli,sa.address_id,  sacf.cala,  sacf.house_number, sacf.street_name, sacf.unit,  sacf.floor,  sacf.building, sacf.community,  sacf.state, " +
                   " sacf.zip_code, sa.serving_terminal, sacf.tar_code,  sacf.contract_type,  sacf.datum_code,  sa.circuit_identifier, sacf.change_date FROM service_address_custom_fields@loop_qual_marten " +
                   "   sacf, " + configKey.Default.service_addresses + " sa WHERE  sa.wire_center_clli(+) = sacf.wire_center_clli  AND sa.house_number(+) = sacf.house_number  AND sa.street_name(+) = sacf.street_name " +
                   " AND nvl(sa.unit(+), '.') = sacf.unit  AND nvl(sa.floor(+), '.') = sacf.floor  AND nvl(sa.building(+), '.') = sacf.building  AND sa.wire_center_clli = '" + WCLFAX + "' AND sa.address_id in (" + addressid + ")) b" +
                   "   LEFT JOIN swgisloc.living_units a ON a.wire_center_id = b.wire_center_clli  AND a.luid = b.address_id AND a.serving_terminal = b.serving_terminal GROUP BY mod_speed_down, B.address_id, house_number, street_name, " +
                   " b.community, b.state,  b.zip_code ,B.serving_terminal";
                }
                DbWorker dbWorker = new DbWorker();
                Exception ex = null;
                dbWorker.RunDmlQuery(AddressQuery);
                string querylat = "ALTER TABLE LU_" + wcid + "_" + jobId + " ADD (geo_lat NUMBER)";
                string querylon = "ALTER TABLE LU_" + wcid + "_" + jobId + " ADD (geo_lon NUMBER)";
                dbWorker.RunDmlQuery(querylat);
                dbWorker.RunDmlQuery(querylon);
                DataTable dtserAddress = dbWorker.ReadTable("Select * From LU_" + wcid + "_" + jobId, out ex);
                LogManager.WriteLogandConsole("INFO : reading GeoCode License from : " + configKey.Default.GeoCode_LicFile);
                SingleCallGeocode scg = null;
                string[] licArgs = new string[6]
                   { configKey.Default.gdxFilesPath,"-l",configKey.Default.GeoCode_LicFile ,"-c",configKey.Default.GeoCodeLicCode, "dummy,dummy" };//@"C:\GeoCode_LicFile\QwestCommunicationsCompany7742.lic , 7742"
                if (licArgs.Length < 6)
                {
                    LogManager.WriteLogandConsole("Usage: SingleCallGeocode <path to PxPoint data-dir> -l <license file> -c <license code> \"address1\" ... \"address n\"");
                    Environment.Exit(-1);
                }
                string pxLicenseFile = null;
                string pxLicenseCode = null;
                for (int i = 1; i < 5; i += 2)
                {
                    if (licArgs[i].Equals("-l"))
                        pxLicenseFile = licArgs[i + 1];
                    else if (licArgs[i].Equals("-c"))
                        pxLicenseCode = licArgs[i + 1];
                }
                string[] dataStuff = new string[licArgs.Length - 5];
                for (int i = 5; i < licArgs.Length; i++)
                    dataStuff[i - 5] = licArgs[i];
                // check to be sure user specified license file and license code params
                LogManager.WriteLogandConsole("INFO : Initializing GeoCode License ");
                if ((null != pxLicenseFile) && (null != pxLicenseCode))
                {
                    scg = new SingleCallGeocode(licArgs[0], pxLicenseFile, pxLicenseCode, dataStuff);
                    scg.InitPxPoint();
                    // initialize single call - note this will exit the program if any
                }
                LogManager.WriteLogandConsole("INFO : Updating lat Lons from pxPoint API ");
                int CNT = 0;
                int delcount = 0;
                dictLocNotFound = new Dictionary<string, List<string>>();

                foreach (DataRow item in dtserAddress.Rows)
                {
                    CNT++;
                    LogManager.WriteLogandConsole("CNT ; " + CNT);
                    string serAddress = item[2].ToString() + " " + item[3].ToString() + "," + item[4].ToString() + " " + item[5].ToString() + " " + item[6].ToString();
                    string[] arguments = new string[6]
                    { configKey.Default.gdxFilesPath,"-l",configKey.Default.GeoCode_LicFile,"-c",configKey.Default.GeoCodeLicCode, serAddress };
                    string[] latlon = GeoCodeMain(arguments, pxLicenseFile, pxLicenseCode);
                    //check for null in the latlon and log exception..
                    string qqq = string.Empty;
                    if (string.IsNullOrEmpty(latlon[0]))
                    {
                        qqq = "Delete from LU_" + wcid + "_" + jobId + " WHERE HOUSE_NUMBER = '" + item[2].ToString() + "'" +
                       " and STREET_NAME = '" + item[3].ToString() + "' AND ZIP_CODE = '" + item[6].ToString() + "'";
                        delcount++;
                        LogManager.WriteLogandConsole("Info: Location Not Found for " + item[1]);
                    }
                    else
                    {
                        qqq = "UPDATE LU_" + wcid + "_" + jobId + " SET GEO_LAT = '" + latlon[0] + "' , GEO_LON =  '" + latlon[1] + "' WHERE HOUSE_NUMBER = '" + item[2].ToString() + "'" +
                           " and STREET_NAME = '" + item[3].ToString() + "' AND ZIP_CODE = '" + item[6].ToString() + "'";

                    }
                    dbWorker.RunDmlQuery(qqq);
                }
                if (delcount != 0)
                    LogManager.WriteLogandConsole("Info: No of locations missing " + delcount);
                scg.ClosePxPoint();
                string query1 = "ALTER TABLE LU_" + wcid + "_" + jobId + " ADD (shape SDO_GEOMETRY)";
                string query2 = "UPDATE LU_" + wcid + "_" + jobId + " SET shape =   SDO_GEOMETRY(2001,4326,SDO_POINT_TYPE(GEO_LON, GEO_LAT, NULL),NULL,NULL)";
                string query3 = "create index LU_" + wcid + "_" + jobId + "_IDX ON LU_" + wcid + "_" + jobId + "_1" + "(SHAPE) INDEXTYPE IS MDSYS.SPATIAL_INDEX";
                dbWorker.RunDmlQuery(query1);
                dbWorker.RunDmlQuery(query2);
                dbWorker.RunDmlQuery(query3);
                LogManager.WriteLogandConsole("INFO : CreateCopperTables completed ");
            }
            catch (Exception ex) { LogManager.WriteLogandConsole(ex); isExecutionResult = false; }

        }

        internal void updateChangeRequestlogtbl(string Excepwcid,string designationid,string excepOrFailType,string status,int qualstatus)
        {
            try
            {
                string qualstatusType = string.Empty;
                if (qualstatus == 1)
                    qualstatusType = "SERVICE";
                if (qualstatus == 2)
                    qualstatusType = "CONSTRUCTION";
                if (qualstatus == 3)
                    qualstatusType = "NO-BUILD";
                DbWorker dbWorker = new DbWorker();
                string updtQuery = string.Format("UPDATE  {0} SET {1} = '{2}',{3} = '{4}' WHERE {5} = {6} AND {7}={8} AND {9}='{10}'",
                        "CHANGE_REQUEST_LOG", "Status", status, "Remarks", excepOrFailType, "WC_ID", "'"+Excepwcid+"'", "designation_id", "'"+designationid+"'","Type", qualstatusType);
                 dbWorker.RunDmlQuery(updtQuery);
            }

            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
                isExecutionResult = false;
            }
        }

        private string getWcid_lfac(string wc_id)
        {
            string wirecenter = "";
            string query = "";
            DbWorker dbWorker;
            try
            {
                //select LEGACY_CMP from cl_wire_center where WC_ID = 'STGRUTMA'
                query = "select LEGACY_CMP from cl_wire_center where  WC_ID='" + wc_id + "'";
                dbWorker = new DbWorker();
                string legacycmpvalue = dbWorker.Getstringvalue(query);
                if (legacycmpvalue == "Qwest")
                {
                    //"select wc_id from cl_wire_center where  id='" + wcid + "'";
                    query = "select wc_id from cl_wire_center where  WC_ID='" + wc_id + "'";
                    dbWorker = new DbWorker();
                    wirecenter = dbWorker.Getstringvalue(query);
                }
                else
                {
                    query = "select LFACS_WCID from cl_wire_center where  WC_ID='" + wc_id + "'";
                    dbWorker = new DbWorker();
                    wirecenter = dbWorker.Getstringvalue(query);
                }
            }
            catch (Exception ex) { LogManager.WriteLogandConsole(ex); isExecutionResult = false; }
            return wirecenter;
        }

        internal string getWcLfacId(string wcid)
        {
            string wirecenter = "";
            string query = "";
            DbWorker dbWorker;
            try
            {
                query = "select LEGACY_CMP from cl_wire_center where  ID='" + wcid + "'";
                dbWorker = new DbWorker();
                string legacycmpvalue = dbWorker.Getstringvalue(query);
                if (legacycmpvalue == "Qwest")
                {
                    query = "select wc_id from cl_wire_center where  ID='" + wcid + "'";
                    dbWorker = new DbWorker();
                    wirecenter = dbWorker.Getstringvalue(query);
                }
                else
                {
                    query = "select LFACS_WCID from cl_wire_center where  ID='" + wcid + "'";
                    dbWorker = new DbWorker();
                    wirecenter = dbWorker.Getstringvalue(query);
                }
            }
            catch (Exception ex) { LogManager.WriteLogandConsole(ex); isExecutionResult = false; }
            return wirecenter;
        }



        internal void UpdateRecordIntoLoopQual(List<CopperPolygonClass> copperlist)
        {
            int rowAffected;
            try
            {

                string lfacwcid = getWcid_lfac(copperlist[0].WC_CLLI);

                DbWorker dbWorker = new DbWorker();
                Exception ex;
                SqlQueryHelper.LoadServerConnection(false, true, 1); //connecting to the loopqual martin database..
                if (copperlist.Count > 0)
                {
                    DataTable dt1 = dbWorker.ReadTable("Select * from ULQDD001.SERVICE_ADDRESSES where wire_center_clli = '" + lfacwcid + "'", out ex);//replaced copperlist[0].WC_CLLI with lfacwcid
                    if (dt1.Rows.Count > 0)
                    {
                        foreach (var item in copperlist)
                        {

                            string updtQuery = "update ULQDD001.SERVICE_ADDRESSES set PARCEL_LAT = " + item.LU_Lat + ",PARCEL_LON = " + item.LU_Lon + ", PARCEL_APN = '" + item.APN + "',PARCEL_FIPS = '" + item.FIPS + "' WHERE wire_center_clli = '" + lfacwcid + "' AND ADDRESS_ID = '" + item.LUID + "'";
                            rowAffected = dbWorker.RunDmlQuery(updtQuery);
                        }
                    }
                    else
                    {
                        SqlQueryHelper.LoadServerConnection(false, true, 2);//connecting to the loopqual lFacs database..
                        foreach (var item in copperlist)
                        {
                            string updtQuery = "update ULQDD001.SERVICE_ADDRESSES set PARCEL_LAT = " + item.LU_Lat + ",PARCEL_LON = " + item.LU_Lon + ", PARCEL_APN = '" + item.APN + "',PARCEL_FIPS = '" + item.FIPS + "' WHERE wire_center_clli = '" + lfacwcid + "' AND ADDRESS_ID = '" + item.LUID + "'";
                            rowAffected = dbWorker.RunDmlQuery(updtQuery);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
                isExecutionResult = false;
            }

        }

        public IPropertySet LoadODSDEVProperties()
        {
            IPropertySet propertySet = new ESRI.ArcGIS.esriSystem.PropertySetClass();

            propertySet.SetProperty(Constants.DBProperties.server, configKey.Default.ODSDEV_Server); //"172.16.8.144"
            propertySet.SetProperty(Constants.DBProperties.instance, configKey.Default.ODSDEV_INSTANCE);//"sde:oracle11g:auroradb");
            propertySet.SetProperty(Constants.DBProperties.authMode, configKey.Default.AUTHENTICATION_MODE);// "DBMS");
            propertySet.SetProperty(Constants.DBProperties.database, configKey.Default.ODSDEV_DATABASE);// "auroradb");
            propertySet.SetProperty(Constants.DBProperties.user, configKey.Default.ODSDEV_USER);//"SWGISLOC");
            propertySet.SetProperty(Constants.DBProperties.password, configKey.Default.ODSDEV_PASSWORD);//"SWGISLOC");
            propertySet.SetProperty(Constants.DBProperties.version, configKey.Default.VERSION);//"sde.DEFAULT");

            return propertySet;
        }

        public IPropertySet LoadLanduseProperties()
        {
            IPropertySet propertySet = new ESRI.ArcGIS.esriSystem.PropertySetClass();

            propertySet.SetProperty(Constants.DBProperties.server, configKey.Default.LandUseDBServer); //"172.16.8.144"
            propertySet.SetProperty(Constants.DBProperties.instance, configKey.Default.LandUseDBInstance);//"sde:oracle11g:auroradb");
            propertySet.SetProperty(Constants.DBProperties.authMode, configKey.Default.AUTHENTICATION_MODE);// "DBMS");
            propertySet.SetProperty(Constants.DBProperties.database, configKey.Default.LanduseDBDatabase);// "auroradb");
            propertySet.SetProperty(Constants.DBProperties.user, configKey.Default.LandUseDBUserID);//"SWGISLOC");
            propertySet.SetProperty(Constants.DBProperties.password, configKey.Default.LandUseDBPassword);//"SWGISLOC");
            propertySet.SetProperty(Constants.DBProperties.version, configKey.Default.VERSION);//"sde.DEFAULT");

            return propertySet;
        }


        /// <summary>
        /// Read db properties to PropertySet 
        /// </summary>
        /// <returns>IPropertySet</returns>
        public IPropertySet LoadSDEswgisProperties()
        {
            IPropertySet propertySet = new ESRI.ArcGIS.esriSystem.PropertySetClass();
            if (configKey.Default.RunMode == "0")
            {
                propertySet.SetProperty(Constants.DBProperties.server, configKey.Default.Qual_Server); //"172.16.8.144"//racorad30-scan.test.intranet
                propertySet.SetProperty(Constants.DBProperties.instance, configKey.Default.Qual_INSTANCE);//"sde:oracle11g:auroradb");
                propertySet.SetProperty(Constants.DBProperties.authMode, configKey.Default.AUTHENTICATION_MODE);// "racorad30-scan.test.intranet:1521/neisl01t_19");
                propertySet.SetProperty(Constants.DBProperties.database, configKey.Default.Qual_DATABASE);// "auroradb");//neisl01t_19
                propertySet.SetProperty(Constants.DBProperties.user, configKey.Default.Qual_USER_SWGISLOC);//"SWGISLOC");
                propertySet.SetProperty(Constants.DBProperties.password, configKey.Default.Qual_PASSWORD_SWGISLOC);//"SWGISLOC");
                propertySet.SetProperty(Constants.DBProperties.version, configKey.Default.VERSION);//"sde.DEFAULT");
                                                                                                   //propertySet.SetProperty(Constants.DBProperties.port, configKey.Default.Qual_PORT);
            }
            else if (configKey.Default.RunMode == "1")
            {
                propertySet.SetProperty(Constants.DBProperties.server, "172.16.8.144"); //"172.16.8.144"
                propertySet.SetProperty(Constants.DBProperties.instance, "sde:oracle11g:orclcp");//"sde:oracle11g:auroradb");
                propertySet.SetProperty(Constants.DBProperties.authMode, "DBMS");// "DBMS");
                propertySet.SetProperty(Constants.DBProperties.database, "orclcp");// "auroradb");
                propertySet.SetProperty(Constants.DBProperties.user, "SWGISLOC");//"SWGISLOC");
                propertySet.SetProperty(Constants.DBProperties.password, "SWGISLOC");//"SWGISLOC");
                propertySet.SetProperty(Constants.DBProperties.version, "sde.DEFAULT");//"sde.DEFAULT");
            }
            return propertySet;
        }

        /// <summary>
        /// Generate Query for JOb Logs
        /// </summary>
        /// <param name="jobentity"></param>
        /// <returns>Query</returns>
        public string JobStatsInsertQuery(JobEntity entity)
        {
            string Query = string.Empty;
            string dateTimeStamp = "'" + DateTime.Now.ToString("dd-MMM-yyyy hh:mm:ss") + "'";
            //INSERT INTO table
            if (this.qualStatus == Constants.QualStatus.InConstruction)
            {
                return Query = string.Format("INSERT INTO {0} ({1},{2},{3},{4},{5},{6},{7},{8},{9},{10}) VALUES ({11},{12},{13},{14},{15},{16},{17},{18},{19},{20})",
               entity.jobStats_Tablename, entity.jobId, entity.jobStats_logDate, entity.jobStats_noofDesgRecieved, entity.jobStats_noofDesgProcessed,
               entity.jobStats_noofProjsRecieved, entity.jobStats_noofProjsProcessed,
               entity.jobStats_noofPolygonsSuccess, entity.jobStats_noofDesgFailed, entity.jobStats_noofoPolygonsExceptions, entity.jobStats_QualStatus,
               this.jobID, dateTimeStamp, this.constDesgRecieved, this.constDesgProcessed, this.designationLst.Count, this.ProcessedDesignations, this.polygonsSuccess,
               this.polygonsFailed, this.polygonswithExceptions, this.qualStatus);
            }
            else if (this.qualStatus == Constants.QualStatus.NoBuild)
            {
                return Query = string.Format("INSERT INTO {0} ({1},{2},{3},{4},{5}) VALUES ({6},{7},{8},{9},{10})",
                              entity.jobStats_Tablename, entity.jobId, entity.jobStats_logDate, entity.jobStats_QualStatus,
                              entity.jobStats_noofPolygonsSuccess, entity.jobStats_noofDesgFailed,
                              this.jobID, dateTimeStamp, this.qualStatus, this.polygonsSuccess, this.polygonsFailed);
            }
            else
            {
                return Query = string.Format("INSERT INTO {0} ({1},{2},{3},{4},{5},{6},{7},{8}) VALUES ({9},{10},{11},{12},{13},{14},{15},{16})",
                               entity.jobStats_Tablename, entity.jobId, entity.jobStats_logDate, entity.jobStats_noofDesgRecieved, entity.jobStats_noofDesgProcessed,
                               entity.jobStats_noofPolygonsSuccess, entity.jobStats_noofDesgFailed, entity.jobStats_noofoPolygonsExceptions, entity.jobStats_QualStatus,
                               this.jobID, dateTimeStamp, this.designationLst.Count, this.ProcessedDesignations, this.polygonsSuccess,
                               this.polygonsFailed, this.polygonswithExceptions, this.qualStatus);
            }

        }

        public string JobStatsCoperInsertQuery(JobEntity entity)
        {
            string Query = string.Empty;
            string dateTimeStamp = "'" + DateTime.Now.ToString("dd-MMM-yyyy hh:mm:ss") + "'";
            //INSERT INTO table
            if (this.qualStatus == Constants.QualStatus.InConstruction)
            {
                return Query = string.Format("INSERT INTO {0} ({1},{2},{3},{4},{5},{6},{7},{8},{9},{10}) VALUES ({11},{12},{13},{14},{15},{16},{17},{18},{19},{20})",
               entity.jobStats_Tablename, entity.jobId, entity.jobStats_logDate, entity.jobStats_noofDesgRecieved, entity.jobStats_noofDesgProcessed,
               entity.jobStats_noofProjsRecieved, entity.jobStats_noofProjsProcessed,
               entity.jobStats_noofPolygonsSuccess, entity.jobStats_noofDesgFailed, entity.jobStats_noofoPolygonsExceptions, entity.jobStats_QualStatus,
               this.jobID, dateTimeStamp, this.constDesgRecieved, this.constDesgProcessed, this.designationLst.Count, this.ProcessedDesignations, this.polygonsSuccess,
               this.polygonsFailed, this.polygonswithExceptions, this.qualStatus);
            }
            else if (this.qualStatus == Constants.QualStatus.NoBuild)
            {
                return Query = string.Format("INSERT INTO {0} ({1},{2},{3},{4},{5}) VALUES ({6},{7},{8},{9},{10})",
                              entity.jobStats_Tablename, entity.jobId, entity.jobStats_logDate, entity.jobStats_QualStatus,
                              entity.jobStats_noofPolygonsSuccess, entity.jobStats_noofDesgFailed,
                              this.jobID, dateTimeStamp, this.qualStatus, this.polygonsSuccess, this.polygonsFailed);
            }
            else
            {
                return Query = string.Format("INSERT INTO {0} ({1},{2},{3},{4},{5},{6},{7},{8}) VALUES ({9},{10},{11},{12},{13},{14},{15},{16})",
                               entity.jobStats_Tablename, entity.jobId, entity.jobStats_logDate, entity.jobStats_noofDesgRecieved, entity.jobStats_noofDesgProcessed,
                               entity.jobStats_noofPolygonsSuccess, entity.jobStats_noofDesgFailed, entity.jobStats_noofoPolygonsExceptions, entity.jobStats_QualStatus,
                               this.jobID, dateTimeStamp, this.constDesgRecieved, this.ProcessedDesignations, this.polygonsSuccess,
                               this.polygonsFailed, this.polygonswithExceptions, this.qualStatus);
            }

        }
        internal void InserIntoReviewNB(List<NoBuild> noBuildFC, JobEntity entity, bool isFailePolys)
        {
            try
            {
                int rowAffected = 0;
                string Query = string.Empty;
                string dateTimeStamp = "'" + DateTime.Now.ToString("dd-MMM-yyyy hh:mm:ss") + "'";
                SqlQueryHelper.LoadServerConnection(true);
                foreach (var item in noBuildFC)
                {
                    if (isFailePolys)
                    {
                        Query = string.Format("INSERT INTO {0} ({1},{2},{3},{4}) VALUES ({5},{6},{7},{8})",
                   entity.NBReviewTableName, entity.NBReview_JobID, entity.NBReview_WireCenterID, entity.NBReview_StatusID, entity.NBReview_RevDate,
                   this.jobID, this.wireCenterID, item.PolygonStatus, dateTimeStamp);

                    }
                    else
                    {
                        Query = string.Format("INSERT INTO {0} ({1},{2},{3},{4},{5}) VALUES ({6},{7},{8},{9},{10})",
                   entity.NBReviewTableName, entity.NBReview_JobID, entity.NBReview_WireCenterID,
                   entity.NBReview_PolygonID, entity.NBReview_StatusID, entity.NBReview_RevDate,
                   this.jobID, this.wireCenterID, item.PolygonID, item.PolygonStatus, dateTimeStamp);

                    }
                    DbWorker dbWorker = new DbWorker();
                    rowAffected = dbWorker.RunDmlQuery(Query);
                }

            }

            catch (Exception ex)
            {
                LogManager.Log(ex); isExecutionResult = false;
            }
        }
        internal void InsertRecordsinCopperAddress(List<CopperPolygonClass> copperpoly)
        {
            int rowAffected;
            try
            {
                DbWorker dbWorker = new DbWorker();
                Exception ex = null;
               
                foreach (var item in copperpoly)
                {
                    foreach (var LuidSTitem in item.dictLUID)
                        rowAffected = dbWorker.RunDmlQuery("insert into " + configKey.Default.Copper_Address + " (APN,FIPS,LUID,SERVING_TERMINAL,WC_ID) VALUES ('" + item.APN + "','" + item.FIPS + "','" + LuidSTitem.Key + "','" + LuidSTitem.Value + "','" + item.WC_CLLI + "')");
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
                isExecutionResult = false;
            }
        }
        internal string Getluid(string lat, string lon, string wcid, string jobid)
        {
            string val = string.Empty;
            try
            {
                string tablename = "LU_" + wcid + "_" + jobid;
                DbWorker dbWorker = new DbWorker();//"swgisloc."+ tablename 
                val = dbWorker.Getstringvalue("select ADDRESS_ID from " + "swgisloc." + tablename + " where GEO_LAT='" + lat + "' and GEO_LON='" + lon + "' and rownum=1");

                // val = dbWorker.Getstringvalue("select luid from " + configKey.Default.MONSTER + " where lat='" + lat + "' and lon='" + lon + "' and wire_center_id='" + wcid + "' and rownum=1");
            }
            catch (Exception ex)
            {

                throw ex;
            }
            return val;
        }
        internal int Getluidcount(string lat, string lon, string wcid, string jobid)
        {
            int val = 0;
            try
            {
                string tablename = "LU_" + wcid + "_" + jobid;
                DbWorker dbWorker = new DbWorker();
                val = dbWorker.Getintvalue("select count(*) from " + "swgisloc." + tablename + " where GEO_LAT='" + lat + "' and GEO_LON='" + lon + "'");

                //val = dbWorker.Getintvalue("select count(*) from " + configKey.Default.MONSTER + " where lat='" + lat + "' and lon='" + lon + "' and wire_center_id='" + wcid + "'");
            }
            catch (Exception ex)
            {

                throw ex;
            }
            return val;
        }
        internal int IsHavingcopperreview(string apn, string fips, string wcid)
        {
            int val = 0;
            try
            {
                DbWorker dbWorker = new DbWorker();
                val = dbWorker.Getintvalue("select count(*) from " + configKey.Default.ReviewCopper + " where APN='" + apn + "' and FIPS='" + fips + "' and WC_ID=" + wcid + " and Status_id not in ('201', '202')");

            }
            catch (Exception ex)
            {

                throw ex;
            }
            return val;

        }
        internal void InsertNoLatlonLUS(string wcid, Jobs jobTask)
        {
            try
            {
                DbWorker db = new DbWorker();
                Exception ex = null;
                DataTable dt = db.ReadTable("select b.address_id from " + configKey.Default.MONSTER + " a right join " + configKey.Default.service_addresses + " b on a.wire_center_id=b.wire_center_clli" +
                     " AND a.luid= b.address_id where b.wire_center_clli= '" + wcid + "' and a.lat is null", out ex);
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    jobTask.designationLst.Add(dt.Rows[i][0].ToString(), new List<string> { JobPolygonStatusProgressEnum.designationFailed.ToString(), Constants.Messages.Copper_LatlonNoMessage });
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        /// <summary>
        /// Update Polygon Exception Logs to DB
        /// </summary>
        /// <param name="jobentity"></param>
        /// <param name="exceptnDesgList"></param>
        internal void UpdatePolygonExceptionLogs(JobEntity jobentity, Dictionary<string, List<string>> exceptnDesgList)
        {
            int rowAffected;
            try
            {
                foreach (var item in exceptnDesgList)
                {
                    string query = Desg_ExceptionLstQuery(jobentity, item.Key, item.Value[1]);
                    DbWorker dbWorker = new DbWorker();
                    rowAffected = dbWorker.RunDmlQuery(query);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
                isExecutionResult = false;
            }

        }

        /// <summary>
        /// Update Polygon Failure Logs to DB
        /// </summary>
        /// <param name="jobentity"></param>
        /// <param name="failedDesgList"></param>
        internal void UpdateDesignationFailures(JobEntity jobentity, Dictionary<string, List<string>> failedDesgList)
        {
            int rowAffected;
            try
            {
                foreach (var item in failedDesgList)
                {
                    string query = Desg_FailedLstQuery(jobentity, item.Key, item.Value[1]);
                    DbWorker dbWorker = new DbWorker();
                    rowAffected = dbWorker.RunDmlQuery(query);
                }
               
            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
                isExecutionResult = false;
            }
        }

        internal void UpdateDesignationFailuresForLocNotFound(JobEntity jobentity, Dictionary<string, List<string>> failedDesgList)
        {
            int rowAffected;
            try
            {
                //if (failedDesgList.ContainsKey("Latlong"))
                //{
                //    foreach (var item in failedDesgList)
                //    {
                //        string query = Desg_FailedLstQuery(jobentity, item.Key, item.Value[0]);
                //        DbWorker dbWorker = new DbWorker();
                //        rowAffected = dbWorker.RunDmlQuery(query);
                //    }
                //}
                //else
                //{

                //}
            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
                isExecutionResult = false;
            }
        }

        /// <summary>
        /// Query for Failed Logs
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="failedDesg"></param>
        /// <param name="failMessage"></param>
        /// <returns>Query</returns>
        private string Desg_FailedLstQuery(JobEntity entity, string failedDesg, string failMessage)
        {
            string Query = string.Empty;
            string dateTimeStamp = "'" + DateTime.Now.ToString("dd-MMM-yyyy hh:mm:ss") + "'";
            //INSERT INTO table
            if (this.qualStatus == Constants.QualStatus.InConstruction)
            {
                string[] desg = failedDesg.Split(':');
                if (desg.Length == 1)
                {
                    return Query = string.Format("INSERT INTO {0} ({1},{2},{3},{4},{5},{6},{7}) VALUES ({8},'{9}',{10},'{11}',{12},'{13}',{14})",
                      entity.failureTablename, entity.jobId, entity.failureDesgID, entity.failureType, entity.failureMessage, entity.failureDateTime, entity.failureNDSJNO, entity.failureQualStatus,
                      this.jobID, desg[0], (int)JobPolygonStatusProgressEnum.jobFailureGeometry, failMessage, dateTimeStamp, failedDesg, this.qualStatus);
                }
                else
                {
                    return Query = string.Format("INSERT INTO {0} ({1},{2},{3},{4},{5},{6},{7}) VALUES ({8},'{9}',{10},'{11}',{12},'{13}',{14})",
                      entity.failureTablename, entity.jobId, entity.failureDesgID, entity.failureType, entity.failureMessage, entity.failureDateTime, entity.failureNDSJNO, entity.failureQualStatus,
                      this.jobID, desg[0], (int)JobPolygonStatusProgressEnum.jobFailureGeometry, failMessage, dateTimeStamp, desg[1], this.qualStatus);
                }
                }
            else
                return Query = string.Format("INSERT INTO {0} ({1},{2},{3},{4},{5},{6}) VALUES ({7},'{8}',{9},'{10}',{11},{12})",
               entity.failureTablename, entity.jobId, entity.failureDesgID, entity.failureType, entity.failureMessage, entity.failureDateTime, entity.failureQualStatus,
               this.jobID, failedDesg, (int)JobPolygonStatusProgressEnum.jobFailureGeometry, failMessage, dateTimeStamp, this.qualStatus);

        }

        /// <summary>
        /// Query for Exception Logs
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="exptnDesg"></param>
        /// <param name="failMessage"></param>
        /// <returns>Query</returns>
        private string Desg_ExceptionLstQuery(JobEntity entity, string exptnDesg, string failMessage)
        {
            string Query = string.Empty;
            string dateTimeStamp = "'" + DateTime.Now.ToString("dd-MMM-yyyy hh:mm:ss") + "'";
            if (this.qualStatus == Constants.QualStatus.InConstruction)
            {
                string[] desg = exptnDesg.Split(':');
                if (desg.Length == 1)
                {
                    return Query = string.Format("INSERT INTO {0} ({1},{2},{3},{4},{5},{6},{7}) VALUES ({8},{9},'{10}',{11},'{12}',{13},{14})",
              entity.exceptionTablename, entity.jobId, entity.exception_polygonID, entity.failureDesgID, entity.exceptionType,
              entity.exceptionMessage, entity.exceptionDateTime, entity.exceptionQualStatus,
              this.jobID, 0, desg[0], (int)JobPolygonStatusProgressEnum.jobExceptionOverlaps, failMessage, dateTimeStamp, this.qualStatus);
                }
                else
                {
                    return Query = string.Format("INSERT INTO {0} ({1},{2},{3},{4},{5},{6},{7},{8}) VALUES ({9},{10},'{11}',{12},'{13}',{14},'{15}',{16})",
                  entity.exceptionTablename, entity.jobId, entity.exception_polygonID, entity.failureDesgID, entity.exceptionType,
                  entity.exceptionMessage, entity.exceptionDateTime, entity.exceptionNDSJNO, entity.exceptionQualStatus,
                  this.jobID, 0, desg[0], (int)JobPolygonStatusProgressEnum.jobExceptionOverlaps, failMessage, dateTimeStamp, desg[1], this.qualStatus);
                }
            }
            else
            {
                return Query = string.Format("INSERT INTO {0} ({1},{2},{3},{4},{5},{6},{7}) VALUES ({8},{9},'{10}',{11},'{12}',{13},{14})",
                              entity.exceptionTablename, entity.jobId, entity.exception_polygonID, entity.failureDesgID, entity.exceptionType,
                              entity.exceptionMessage, entity.exceptionDateTime, entity.exceptionQualStatus,
                              this.jobID, 0, exptnDesg, (int)JobPolygonStatusProgressEnum.jobExceptionOverlaps, failMessage, dateTimeStamp, this.qualStatus);
            }

        }

        /// <summary>
        /// Update JOB log Statitics to DB
        /// </summary>
        /// <param name="job entity"></param>
        internal void InsertJobStats(JobEntity entity)
        {
            string query = JobStatsInsertQuery(entity);
            int rowAffected;
            try
            {
                DbWorker dbWorker = new DbWorker();
                rowAffected = dbWorker.RunDmlQuery(query);
            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
                isExecutionResult = false;
            }
        }

        internal void InsertJobStatsCoper(JobEntity entity)
        {
            string query = JobStatsCoperInsertQuery(entity);
            int rowAffected;
            try
            {
                DbWorker dbWorker = new DbWorker();
                rowAffected = dbWorker.RunDmlQuery(query);
            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
                isExecutionResult = false;
            }
        }
        /// <summary>
        /// Read WireCenterName with WCID
        /// </summary>
        /// <param name="wireCenterID"></param>
        /// <returns>WireCenter Name </returns>
        private static string GetWireCenterNamefromID(string wireCenterID)
        {
            string wcName = string.Empty;
            try
            {
                JobEntity jobentiry = JobEntity.GetInstance();
                string[] wctableCols = new string[1] { jobentiry.WireCenterName };
                DbWorker worker = new DbWorker();
                Exception ex = null;
                DataTable wcTable = worker.ReadTable(worker.GetQuery(wctableCols, configKey.Default.WirecenterBoundaryName, jobentiry.wireCenterID + "=" + wireCenterID), out ex);
                if (ex != null)
                {
                    LogManager.WriteLogandConsole(ex);
                }
                if (wcTable.Rows.Count == 0)
                {
                    LogManager.WriteLogandConsole("ERROR : Unable to get the WireCenterName for - " + wireCenterID);
                    return string.Empty;
                }
                wcName = wcTable.Rows[0][0].ToString();
            }
            catch (Exception ex) { LogManager.WriteLogandConsole(ex); isExecutionResult = false; }
            return wcName;
        }

        /// <summary>
        /// Insert/Update Service POlygons to SQM
        /// </summary>
        /// <param name="spPolygonList"></param>
        /// <param name="job entity"></param>
        internal void LoadServicePolygonsToODS(List<ServicePolygonClass> spPolygonList, JobEntity entity)
        {
            try
            {
                foreach (var spLst in spPolygonList)
                {
                    if (isServicePolygonAlreadyExit(spLst, entity))
                        UpdateSPinSQM(spLst, entity);
                    else
                        InsertSPinSQM(spLst, entity);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
                isExecutionResult = false;
            }

        }

        /// <summary>
        /// Insert Service Polygon in SQM
        /// </summary>
        /// <param name="spLst"></param>
        /// <param name="entity"></param>
        private void InsertSPinSQM(ServicePolygonClass spLst, JobEntity entity)
        {
            try
            {

                IPointCollection pCol = spLst.polyGeometry as IPointCollection;
                StringBuilder sb = new StringBuilder();

                //MDSYS.SDO_GEOMETRY(2003,4326,MDSYS.SDO_POINT_TYPE(NULL,NULL,NULL),
                /// MDSYS.SDO_ELEM_INFO_ARRAY(1, 1003, 1),
                //MDSYS.SDO_ORDINATE_ARRAY(
                //for (int i = 0; i < pCol.PointCount; i++)
                //{
                //    IPoint ppnt = pCol.Point[i];
                //}


                DbWorker dbWorker = new DbWorker();
                string Query = string.Format("INSERT INTO {0} ({1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11})" +
                   " VALUES ({12},{13},{14},'{15}',{16},'{17}','{18}','{19}','{20}','{21}',{22})",
               entity.ServicePolyTableName, entity.ServicePoly_TYPE, entity.ServicePoly_STATUS, entity.ServicePoly_AVAILABILITYDATE, entity.ServicePoly_BANDWIDTH,
               entity.ServicePoly_SERVIING_WIRE_CENTER_CLLI, entity.ServicePoly_SERVING_WIRE_CENTER_NAME, entity.ServicePoly_FIBER_CABLE_ID,
               entity.ServicePoly_FIBER_LINE_OF_COUNT, entity.ServicePoly_NDS_JOB_NO, entity.ServicePoly_OLT_RELATIONSHIP, entity.ServicePoly_SHAPE,
               spLst.TYPE, spLst.STATUS, spLst.AVAILABILITY_DATE, spLst.BANDWIDTH, spLst.SERVIING_WIRE_CENTER_CLLI, spLst.SERVING_WIRE_CENTER_NAME,
               spLst.FIBER_CABLE_ID, spLst.FIBER_LINE_OF_COUNT, spLst.NDS_JOB_NO, spLst.OLT_RELATIONSHIP, spLst.polyGeometry);

                int rowsAffected = dbWorker.RunDmlQuery(Query);

            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
            }
        }

        /// <summary>
        /// Update Service Polygon in SQM
        /// </summary>
        /// <param name="spLst"></param>
        /// <param name="entity"></param>
        private void UpdateSPinSQM(ServicePolygonClass spLst, JobEntity spEntity)
        {
            try
            {
                DbWorker dbWorker = new DbWorker();
                string query = string.Format("UPDATE  {0} SET {1} = {2},{3} = {4},{5} = {6},{7} = '{8}',{9} = {10}," +
                    "{11} = '{12}',{13} = '{14}',{15} = '{16}',{17} = '{18}',{19} = '{20}',{21} = {22} WHERE {23} = {24} AND {25} = '{26}'",
                      spEntity.ServicePolyTableName, spEntity.ServicePoly_TYPE, spLst.TYPE, spEntity.ServicePoly_STATUS, spLst.STATUS, spEntity.ServicePoly_AVAILABILITYDATE, spLst.AVAILABILITY_DATE,
                      spEntity.ServicePoly_BANDWIDTH, spLst.BANDWIDTH, spEntity.ServicePoly_SERVIING_WIRE_CENTER_CLLI, spLst.SERVIING_WIRE_CENTER_CLLI,
                      spEntity.ServicePoly_SERVING_WIRE_CENTER_NAME, spLst.SERVING_WIRE_CENTER_NAME, spEntity.ServicePoly_FIBER_CABLE_ID, spLst.FIBER_CABLE_ID,
                      spEntity.ServicePoly_FIBER_LINE_OF_COUNT, spLst.FIBER_LINE_OF_COUNT, spEntity.ServicePoly_NDS_JOB_NO, spLst.NDS_JOB_NO,
                      spEntity.ServicePoly_OLT_RELATIONSHIP, spLst.OLT_RELATIONSHIP, spEntity.ServicePoly_SHAPE, spLst.polyGeometry,
                      spEntity.ServicePoly_FIBER_CABLE_ID, spLst.FIBER_CABLE_ID, spEntity.ServicePoly_SERVING_WIRE_CENTER_NAME, spLst.SERVING_WIRE_CENTER_NAME);
                int rowsAffected = dbWorker.RunDmlQuery(query);

            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
            }
        }

        /// <summary>
        /// Check for exiting Service Polygon in SQM
        /// </summary>
        /// <param name="spLst"></param>
        /// <param name="entity"></param>
        /// <returns>true/false</returns>
        private bool isServicePolygonAlreadyExit(ServicePolygonClass spLst, JobEntity entity)
        {
            bool result = false;
            try
            {
                DbWorker dbWorker;
                string[] colNames = new string[1] { entity.ServicePoly_FIBER_CABLE_ID };
                //  select FCBL_ID from SERVICE_QUALIFICATION_MODEL where FCBL_ID = 'LDMH26570' and WC_ID = '1572328488445168705'
                string whereClause = entity.ServicePoly_FIBER_CABLE_ID + " = '" + spLst.FIBER_CABLE_ID + "' and " +
                    entity.ServicePoly_SERVIING_WIRE_CENTER_CLLI + " = '" + spLst.SERVIING_WIRE_CENTER_CLLI + "'";
                dbWorker = new DbWorker();
                string query = dbWorker.GetQuery(colNames, entity.ServicePolyTableName, whereClause);
                int roweffected = dbWorker.RunDmlQuery(query);
                if (roweffected > 0)
                    result = true;

            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
                return false;
            }
            return result;
        }

        /// <summary>
        /// Update Polygon creating status in Review Table.
        /// </summary>
        /// <param name="servicePolyList"></param>
        /// <param name="spEntity"></param>
        internal void UpdateReviewTable(List<ServicePolygonClass> servicePolyList, JobEntity spEntity, bool isfailedPolys)
        {
            try
            {
                DbWorker dbWorker;
                foreach (var ServicePolygon in servicePolyList)
                {
                    dbWorker = new DbWorker();
                    string Query = GenerateQueryToUpdateReviewTable(ServicePolygon, spEntity, isfailedPolys);
                    int rowAffected = dbWorker.RunDmlQuery(Query);
                }
            }
            catch (Exception ex) { LogManager.WriteLogandConsole(ex); isExecutionResult = false; }
        }
        internal void updateReviewCoppertable(List<CopperPolygonClass> servicePolyList, Jobs spEntity)
        {
            try
            {
                DbWorker dbWorker;
                foreach (var ServicePolygon in servicePolyList)
                {
                    dbWorker = new DbWorker();
                    string Query = "INSERT INTO " + configKey.Default.ReviewCopper + " (JOB_ID,WC_ID,STATUS_ID,REVIEW_DATE,QUAL_STATUS_ID,LU_LAT,LU_LON,POLYGON_ID) " +
                        " VALUES (" + spEntity.jobID + "," + spEntity.wireCenterID + "," + ServicePolygon.Polygon_Status + ",'" + DateTime.Now.ToString("dd-MMM-yyyy hh:mm:ss") + "'" +
                        " ," + ServicePolygon.QUAL_STATUS + ",'" + ServicePolygon.LU_Lat + "','" + ServicePolygon.LU_Lon + "',9999)";
                    int rowAffected = dbWorker.RunDmlQuery(Query);
                }
            }
            catch (Exception ex) { LogManager.WriteLogandConsole(ex); isExecutionResult = false; }

        }
        /// <summary>
        /// Query to Update the REview Table
        /// </summary>
        /// <param name="servicePolyList"></param>
        /// <param name="spEntity"></param>
        /// <returns>query</returns>
        private string GenerateQueryToUpdateReviewTable(ServicePolygonClass servicePolyList, JobEntity spEntity, bool IsFailedPolys)
        {
            string Query = string.Empty;
            if (IsFailedPolys)
                return Query = string.Format("UPDATE {0} SET {1} = {2} WHERE {3} = {4} AND {5} = '{6}' AND {7} = {8} AND {9} = {10} ",
                     spEntity.ReviewTableName, spEntity.Review_Status, servicePolyList.Polygon_Status,
                     spEntity.Review_JobID, this.jobID, spEntity.Review_DesignationID, servicePolyList.FIBER_CABLE_ID,
                     spEntity.Review_WireCenterID, servicePolyList.SERVIING_WIRE_CENTER_CLLI,
                     spEntity.Review_QualStatusID, servicePolyList.STATUS);
            else
                return Query = string.Format("UPDATE {0} SET {1} = {2}, {3} = {4} WHERE {5} = {6} AND {7} = '{8}' AND {9} = {10} AND {11} = {12} ",
                    spEntity.ReviewTableName, spEntity.Review_Status, servicePolyList.Polygon_Status,
                    spEntity.Review_PolygonID, servicePolyList.POLYGON_ID,
                    spEntity.Review_JobID, this.jobID, spEntity.Review_DesignationID, servicePolyList.FIBER_CABLE_ID,
                    spEntity.Review_WireCenterID, servicePolyList.SERVIING_WIRE_CENTER_CLLI,
                    spEntity.Review_QualStatusID, servicePolyList.STATUS);
        }

        /// <summary>
        /// Update JOB Status in DB
        /// </summary>
        /// <param name="status"></param>
        /// <param name="JOBID"></param>
        /// <returns>rows affected</returns>
        public int UpdateJobStatus(int status, int JOBID)
        {
            int rowAffected = 0;
            string Query = string.Empty;
            string dateTimeStamp = "'" + DateTime.Now.ToString("dd-MMM-yyyy hh:mm:ss") + "'";

            string applicationPath = System.Reflection.Assembly.GetEntryAssembly().Location;
            var versionInfo = FileVersionInfo.GetVersionInfo(applicationPath);
            string version = versionInfo.FileVersion;
            try
            {
                JobEntity jobEntity = JobEntity.GetInstance();
                string machinename = System.Environment.MachineName;
                if (status == (int)JobPolygonStatusProgressEnum.Job_Execution_InProgress)
                
                {
                    string jobquery = "select MACHINE_ID from JOB_DETAILS where job_id=" + "'" + JOBID + "'";
                    DbWorker jobdbWorker = new DbWorker();
                    string machinevalue = jobdbWorker.Getstringvalue(jobquery);
                    if (machinevalue != machinename)
                    {
                        if (machinevalue != "")
                        {
                            LogManager.WriteLogandConsole("Info: Application closed because " + JOBID + " is executing in " + machinevalue);
                            System.Environment.Exit(0);
                        }
                    }
                    Query = string.Format("UPDATE  {0} SET {1} = {2},{3} = {4},{5} = '{6}',{7} = '{8}',{9}='{10}' WHERE {11} = {12}",
                        jobEntity.jobTableName, jobEntity.jobStatus, (int)JobPolygonStatusProgressEnum.Job_Execution_InProgress, jobEntity.jobStartDate, dateTimeStamp,
                        jobEntity.JobSQM_Version, version, jobEntity.JobDaemon_Version, version, jobEntity.machineName, machinename,
                        jobEntity.jobId, JOBID);
                }
                else if (status == (int)JobPolygonStatusProgressEnum.Job_Execution_Completed)  // status is wip and jobexestatus = completed..
                    Query = string.Format("UPDATE  {0} SET {1} = {2},{3} = {4},{5}='{6}' WHERE {7} = {8}",
                        jobEntity.jobTableName, jobEntity.jobStatus, (int)JobPolygonStatusProgressEnum.Job_Execution_Completed,
                        jobEntity.jobEndDate, dateTimeStamp,jobEntity.machineName, machinename, jobEntity.jobId, JOBID);
                DbWorker dbWorker = new DbWorker();
                rowAffected = dbWorker.RunDmlQuery(Query);
            }
            catch (Exception ex) { LogManager.WriteLogandConsole(ex); isExecutionResult = false; }
            return rowAffected;
        }
        internal void InsertIntoCopperReviewTable(List<CopperPolygonClass> copperpoly, Jobs entity, bool isFailePolys)
        {
            try
            {
                string Queryrc = string.Empty;
                string dateTimeStamp = "'" + DateTime.Now.ToString("dd-MMM-yyyy hh:mm:ss") + "'";
                foreach (CopperPolygonClass item in copperpoly)
                {
                    string query = "Select OBJECTID from " + configKey.Default.Copper_SQM + " where " +
                       "APN='" + item.APN + "' AND FIPS='" + item.FIPS + "' AND QUAL_TYPE=" + item.QUAL_TYPE + "";
                    DbWorker dbw = new DbWorker();
                    Exception ex1 = null;
                    DataTable dtS = dbw.ReadTable(query, out ex1);
                    foreach (DataRow row in dtS.Rows)
                    {
                        item.POLYGON_ID = Convert.ToInt32(row[0]);
                        Queryrc = "insert into " + configKey.Default.ReviewCopper + " (JOB_ID, WC_ID,POLYGON_ID,STATUS_ID,REVIEW_DATE,APN,FIPS,QUAL_STATUS_ID,LU_LAT,LU_LON)" +
                           " VALUES (" + entity.jobID + "," + entity.wireCenterID + "," + item.POLYGON_ID + ",203," + dateTimeStamp + "," +
                           "'" + item.APN + "','" + item.FIPS + "',1,'" + item.LU_Lat + "','" + item.LU_Lon + "')";
                        DbWorker dbWorker = new DbWorker();
                        dbWorker.RunDmlQuery(Queryrc);
                    }
                }
               
            }
            catch (Exception ex) { LogManager.WriteLogandConsole(ex); isExecutionResult = false; }
        }

        internal void InsertIntoReviewTable(List<ServicePolygonClass> servicePolyList, JobEntity entity, bool isFailePolys)
        {
            try
            {
                int rowAffected = 0;
                string Query = string.Empty;
                string dateTimeStamp = "'" + DateTime.Now.ToString("dd-MMM-yyyy hh:mm:ss") + "'";
                //get UserID frm Review table for current jobID,wirecenterID

                string UserID = string.Empty;

                string getQuery = string.Format("Select {0} From {1} Where {2} = {3} and {4} = {5}",
                    entity.Review_UserID, entity.ReviewTableName, entity.Review_JobID, this.jobID, entity.Review_WireCenterID, this.wireCenterID);
                DbWorker dbww = new DbWorker();
                Exception ex = null;
                DataTable dt = dbww.ReadTable(getQuery, out ex);
                foreach (DataRow row in dt.Rows)
                {
                    UserID = row[0].ToString();
                    break;
                }
                foreach (var item in servicePolyList)
                {
                    if (isFailePolys)
                        Query = string.Format("INSERT INTO {0} ({1},{2},{3},{4},{5},{6},{7},{8},{9})" +
                       " VALUES ({10},'{11}',{12},'{13}',{14},{15},{16},'{17}',{18})",
                   entity.ReviewTableName, entity.Review_JobID, entity.Review_UserID, entity.Review_WireCenterID, entity.Review_DesignationID,
                   entity.Review_QualTYID, entity.Review_Status, entity.Review_RevDate, entity.Review_NDSJNO, entity.Review_QualStatusID,
                   this.jobID, UserID, this.wireCenterID, item.FIBER_CABLE_ID, item.TYPE, item.Polygon_Status, dateTimeStamp, item.NDS_JOB_NO, item.STATUS);
                    else
                        Query = string.Format("INSERT INTO {0} ({1},{2},{3},{4},{5},{6},{7},{8},{9},{10})" +
                      " VALUES ({11},'{12}',{13},'{14}',{15},{16},{17},{18},'{19}',{20})",
                  entity.ReviewTableName, entity.Review_JobID, entity.Review_UserID, entity.Review_WireCenterID, entity.Review_DesignationID,
                  entity.Review_QualTYID, entity.Review_Status, entity.Review_RevDate, entity.Review_PolygonID, entity.Review_NDSJNO, entity.Review_QualStatusID,
                  this.jobID, UserID, this.wireCenterID, item.FIBER_CABLE_ID, item.TYPE, item.Polygon_Status,
                  dateTimeStamp, item.POLYGON_ID, item.NDS_JOB_NO, item.STATUS);

                    DbWorker dbWorker = new DbWorker();
                    rowAffected = dbWorker.RunDmlQuery(Query);
                }
            }
            catch (Exception ex) { LogManager.WriteLogandConsole(ex); isExecutionResult = false; }
        }

        /// <summary>
        /// QUery to Read Terminals
        /// </summary>
        /// <param name="reqViewName"></param>
        /// <returns>query</returns>
        //        public string GenerateTerminalQueryToRead(string reqViewName) //"Terminal_Data4SQM_" + jobID
        //        {
        //            StringBuilder sb = new StringBuilder();
        //            //    string[] desgnationsArray = designations.Split(',');
        //            int i = 0;
        //            foreach (var item in designationLst)           
        //            {  
        //                if (i == designationLst.Count - 1)
        //                    sb.Append("'" + item.Key+ "'");
        //                else
        //                    sb.Append("'" + item.Key + "',");
        //                i++;
        //            }
        //            string query = "create OR REPLACE view " + reqViewName + " As " +
        //"select swlt.ID,clc.designation,swlt.location,swlt.fk_cl_wire_center " +
        //"from Copper_line_of_count clc join MATR_SHEATH_LOC_TERM_LOC102 Swlt on clc.fk_sheath_with_loc_terminal=TO_NUMBER(swlt.id) " +
        //"where swlt.location is not null AND swlt.fk_cl_wire_center = '" + this.wireCenterID + "' and clc.designation in (" + sb.ToString() + ")";
        //            return query;
        //            //exclude undeignated 
        //        }
        public string GenerateConstructionQuery(string viewname)
        {
            StringBuilder sb = new StringBuilder();
            //    string[] desgnationsArray = designations.Split(',');
            int i = 0;
            foreach (var item in designationLst)
            {
                if (i == designationLst.Count - 1)
                    sb.Append("'" + item.Key + "'");
                else
                    sb.Append("'" + item.Key + "',");
                i++;
            }
            string query = "create OR REPLACE view " + viewname + " As select to_number(swlt.id) as id,clc.designation || ':' || sj.job_num as Designation,swlt.location,sj.FIRST_OCCUPANCY_TGT_DT,sj.job_status" +
            " from " + configKey.Default.CopperLineofCountTable + " clc join " + configKey.Default.MAT_SheatWLOCTerminal + " Swlt" +
            " on clc.fk_sheath_with_loc_terminal = swlt.id  join " + configKey.Default.SitesandJobs + " sj on sj.JOB_NUM = swlt.cl_last_modify_project" +
            " where swlt.cl_last_modify_project is not null AND swlt.fk_cl_wire_center = '" + this.wireCenterID + "' and clc.designation in (" + sb.ToString() + ")" +
             " and sj.job_status in ('Placing','Permit','Splicing','Construction','Power','Turn-up','Turnup','Dev Not Ready','Cst Cmpl') " +
            "union all select to_number(pin.id) as id,clc.designation || ':' || sj.job_num as designation,pin.location,sj.FIRST_OCCUPANCY_TGT_DT,sj.job_status" +
            " from " + configKey.Default.sheath_splice + " spl inner join " + configKey.Default.SHEATH_WITH_LOC_TERMINAL + " ter on spl.id = ter.fk_sheath_splice" +
            " inner join " + configKey.Default.MAT_SHEATH_WITH_LOC_PIN + " pin on pin.sheath_splice_id = spl.id" +
            " inner join " + configKey.Default.CopperLineofCountTable + " clc on clc.fk_sheath_with_loc_terminal = ter.id join " + configKey.Default.SitesandJobs + " sj on  sj.JOB_NUM = ter.cl_last_modify_project where" +
             " sj.job_status in ('Placing','Permit','Splicing','Construction','Power','Turn-up','Turnup','Dev Not Ready','Cst Cmpl') and" +
            " ter.cl_last_modify_project is not null and ter.id in (select distinct swlt.id from " + configKey.Default.CopperLineofCountTable + " clc join " + configKey.Default.SHEATH_WITH_LOC_TERMINAL + " Swlt on" +
            " clc.fk_sheath_with_loc_terminal = TO_NUMBER(swlt.id) where swlt.fk_cl_wire_center = '" + this.wireCenterID + "') and clc.designation in (" + sb.ToString() + ")";

            return query;
        }
        public string GenerateTerminalQueryToRead(string reqViewName, string selPref) //"Terminal_Data4SQM_" + jobID
        {
            StringBuilder sb = new StringBuilder();
            //    string[] desgnationsArray = designations.Split(',');
            int i = 0;
            foreach (var item in designationLst)
            {
                if (i == designationLst.Count - 1)
                    sb.Append("'" + item.Key + "'");
                else
                    sb.Append("'" + item.Key + "',");
                i++;
            }
            //            string query = "create OR REPLACE view " + reqViewName + " As " +
            //"select swlt.ID,clc.designation,swlt.location,swlt.fk_cl_wire_center " +
            //"from Copper_line_of_count clc join MATR_SHEATH_LOC_TERM_LOC102 Swlt on clc.fk_sheath_with_loc_terminal=TO_NUMBER(swlt.id) " +
            //"where swlt.location is not null AND swlt.fk_cl_wire_center = '" + this.wireCenterID + "' and clc.designation in (" + sb.ToString() + ")";

            //            select pin.id pinid, spl.id spliceid, ter.id terminalid, clc.designation designation, pin.location,ter.fk_cl_wire_center from sheath_splice spl
            // inner join SWGISLOC.MATR_SHEATH_LOC_TERM_LOC102 ter on spl.id = ter.fk_sheath_splice
            // inner join SWGISLOC.MAT_SHEATH_WITH_LOC_PIN pin on pin.sheath_splice_id = spl.id
            // inner join Copper_line_of_count clc on clc.fk_sheath_with_loc_terminal = TO_NUMBER(ter.id)
            // where ter.id in (select distinct swlt.id from Copper_line_of_count clc join MATR_SHEATH_LOC_TERM_LOC102 Swlt on clc.fk_sheath_with_loc_terminal = TO_NUMBER(swlt.id)
            //where swlt.location is not null AND swlt.fk_cl_wire_center = '1572328488445171051') and clc.designation in ('LD1530GD','LD2543HH','LD14088HH' ) 
            //--and spl.fk_cl_wire_center = '1572328488445171051'
            //order by designation
            StringBuilder sbb = new StringBuilder();
            sbb.Append("create OR REPLACE view " + reqViewName + " As ");

            if (selPref == "1") //terminal
            {
                sbb.Append("select clc.designation,swlt.location,swlt.fk_cl_wire_center " +
   "from " + configKey.Default.CopperLineofCountTable + " clc join " + configKey.Default.MAT_SheatWLOCTerminal + " Swlt on clc.fk_sheath_with_loc_terminal=TO_NUMBER(swlt.id) " +
   "where swlt.location is not null AND swlt.fk_cl_wire_center = '" + this.wireCenterID + "' and clc.designation in (" + sb.ToString() + ") ");
            }
            if (selPref == "2")//LUs
            {
                sbb.Append("select designation, shape, WC_ID from " + configKey.Default.LU_Latlong + " where WC_ID = '" + this.wireCenterID + "' and designation in (" + sb.ToString() + ") ");
            }

            if (selPref == "3")//terminal & LUs
            {
                sbb.Append("select clc.designation,swlt.location,swlt.fk_cl_wire_center " +
"from " + configKey.Default.CopperLineofCountTable + " clc join " + configKey.Default.MAT_SheatWLOCTerminal + " Swlt on clc.fk_sheath_with_loc_terminal=TO_NUMBER(swlt.id) " +
"where swlt.location is not null AND swlt.fk_cl_wire_center = '" + this.wireCenterID + "' and clc.designation in (" + sb.ToString() + ") " +
" union all select designation, shape, WC_ID from lu_latlon where WC_ID = '" + this.wireCenterID + "' and designation in (" + sb.ToString() + ") ");
            }
            if (selPref == "4")//terms,pins
            {
                sbb.Append("select TO_NUMBER(swlt.id) as id,clc.designation,swlt.location,swlt.fk_cl_wire_center,'Terminal' as type " +
"from " + configKey.Default.CopperLineofCountTable + " clc join " + configKey.Default.MAT_SheatWLOCTerminal + " Swlt on clc.fk_sheath_with_loc_terminal=TO_NUMBER(swlt.id) " +
"where swlt.location is not null AND swlt.fk_cl_wire_center = '" + this.wireCenterID + "' and clc.designation in (" + sb.ToString() + ") " +
"union all select TO_NUMBER(pin.id) as id,clc.designation designation,pin.location,TO_CHAR(ter.fk_cl_wire_center),'TerminalPin' as type from " + configKey.Default.sheath_splice + " spl " +
 "inner join " + configKey.Default.SHEATH_WITH_LOC_TERMINAL + " ter on spl.id = ter.fk_sheath_splice " +
"inner join " + configKey.Default.MAT_SHEATH_WITH_LOC_PIN + " pin on pin.sheath_splice_id = spl.id " +
 "inner join " + configKey.Default.CopperLineofCountTable + " clc on clc.fk_sheath_with_loc_terminal = TO_NUMBER(ter.id) " +
"where ter.id in (select distinct swlt.id from " + configKey.Default.CopperLineofCountTable + " clc join " + configKey.Default.SHEATH_WITH_LOC_TERMINAL +
" Swlt on clc.fk_sheath_with_loc_terminal = TO_NUMBER(swlt.id) " +
"where swlt.fk_cl_wire_center = '" + this.wireCenterID + "') and clc.designation in (" + sb.ToString() + ") ");
            }
            if (selPref == "5")//terms,pins,LUs
            {
                sbb.Append("select TO_NUMBER(swlt.id) as id,clc.designation,swlt.location,swlt.fk_cl_wire_center,'Terminal' as type " +
"from " + configKey.Default.CopperLineofCountTable + " clc join " + configKey.Default.MAT_SheatWLOCTerminal + " Swlt on clc.fk_sheath_with_loc_terminal=TO_NUMBER(swlt.id) " +
"where swlt.location is not null AND swlt.fk_cl_wire_center = '" + this.wireCenterID + "' and clc.designation in (" + sb.ToString() + ") " +
" union all select TO_NUMBER(OBJECTID) as id, designation, shape, WC_ID,'LivingUnits' as type from " + configKey.Default.LU_Latlong + " where WC_ID = '" + this.wireCenterID + "' and designation in (" + sb.ToString() + ") " +
"union all select TO_NUMBER(pin.id) as id,clc.designation designation,pin.location,TO_CHAR(ter.fk_cl_wire_center),'TerminalPin' as type from " + configKey.Default.sheath_splice + " spl " +
 "inner join " + configKey.Default.SHEATH_WITH_LOC_TERMINAL + " ter on spl.id = ter.fk_sheath_splice " +
"inner join " + configKey.Default.MAT_SHEATH_WITH_LOC_PIN + " pin on pin.sheath_splice_id = spl.id " +
 "inner join " + configKey.Default.CopperLineofCountTable + " clc on clc.fk_sheath_with_loc_terminal = TO_NUMBER(ter.id) " +
"where ter.id in (select distinct swlt.id from " + configKey.Default.CopperLineofCountTable + " clc join " + configKey.Default.SHEATH_WITH_LOC_TERMINAL +
" Swlt on clc.fk_sheath_with_loc_terminal = TO_NUMBER(swlt.id) " +
"where swlt.fk_cl_wire_center = '" + this.wireCenterID + "') and clc.designation in (" + sb.ToString() + ") ");
            }

            //             
            //--and spl.fk_cl_wire_center = '1572328488445171051' order by designation"
            return sbb.ToString();
            //exclude undeignated 
        }


        /// <summary>
        /// Get LivingUnits for Deignations
        /// </summary>
        /// <param name="wirecenter"></param>
        /// <param name="designationLst"></param>
        /// <returns></returns>
        public int InsertLUData()
        {
            int LUsCount = 0;
            DbWorker worker = new DbWorker();
            Exception ex = null;
            try
            {
                foreach (var item in designationLst)
                {
                    DataTable dt = worker.ReadTable("select SUBSTR(name,INSTR(name,REGEXP_SUBSTR( name, '\\d[a-zA-Z0-9\\s]+'),1),LENGTH(name)) as name " +
    "from " + configKey.Default.mit_terminal_enclosure + " WHERE fk_mit_structure_point IN (Select DISTINCT MIT_STRUCTURE_POINT_ID from " + configKey.Default.MIT_CABLE + " where sheath_with_loc_id in ( " +
    "SELECT DISTINCT fk_sheath_with_loc  FROM " + configKey.Default.CopperLineofCountTable +
    " where fk_sheath_with_loc in  (Select ID from " + configKey.Default.sheath_with_loc + " where fk_cl_wire_center = '" + this.wireCenterID + "') " +//1572328488445168705
    "AND DESIGNATION in ('" + item.Key + "')) AND MIT_STRUCTURE_POINT_ID IS NOT NULL) AND fk_cl_wire_center = '" + this.wireCenterID + "' AND name LIKE 'FSAI%'", out ex);

                    if (dt.Rows.Count == 1)
                    {
                        string name = dt.Rows[0][0].ToString();
                        int count = 0;
                        if (name != null && name != "")
                        {
                            int rr = worker.RunDmlQuery("delete from " + configKey.Default.LU_Latlong + " where WC_ID='" + this.wireCenterID + "' AND DESIGNATION ='" + item.Key + "'");
                            count = worker.RunDmlQuery("INSERT INTO " + configKey.Default.LU_Latlong + " (CLLI_ST,SERVING_TERMINAL,LAT,LON,FSAI_ADDR,DOWN_STREAM,DESIGNATION,WC_ID) " +
       "select 'N' AS clli_st, a.SERVING_TERMINAL, a.lat, a.lon, b.FSAI_ADDR,a.MOD_SPEED_DOWN, '" + item.Key + "' AS DESIG,'" + this.wireCenterID + "' AS WC_ID from " + configKey.Default.Livingunits + " a inner join " +  //SWGISLOC.livingunits
       "" + configKey.Default.GPON_OC_ONT_SUMMARY + " b on a.SERVING_TERMINAL = b.ont_term_addr where b.FSAI_ADDR LIKE '%" + dt.Rows[0][0].ToString() + "'");
                            LUsCount = LUsCount + count;
                        }
                    }
                }//for end
                SqlQueryHelper.LoadServerConnection(false);
                int re = worker.RunDmlQuery("UPDATE " + configKey.Default.LU_Latlong + " SET shape = SDO_GEOMETRY(2001,4326,SDO_POINT_TYPE(LON, LAT, NULL),NULL,NULL) where WC_ID='" + this.wireCenterID + "'");
            }
            catch (Exception ex1)
            {
                LogManager.Log(ex1); isExecutionResult = false;
            }
            return LUsCount;
        }

        /// <summary>
        /// Create Terminal data view in DB
        /// </summary>
        /// <param name="viewName"></param>
        /// <returns>rows Effected</returns>
        public int CreateViewforFeatureClass(string viewName)
        {
            string Query = GenerateTerminalQueryToRead(viewName, configKey.Default.InputPointTypes); //all LUs,terminals,pins
            int rows = 0;
            try
            {
                DbWorker dbWorker = new DbWorker();
                rows = dbWorker.RunDmlQuery(Query);
            }
            catch (Exception ex) { LogManager.WriteLogandConsole(ex); isExecutionResult = false; }
            return rows;
        }
        public int ConstructionviewforFC(string viewName)
        {
            string Query = GenerateConstructionQuery(viewName); //all LUs,terminals,pins
            int rows = 0;
            try
            {
                DbWorker dbWorker = new DbWorker();
                rows = dbWorker.RunDmlQuery(Query);
            }
            catch (Exception ex) { LogManager.WriteLogandConsole(ex); isExecutionResult = false; }
            return rows;
        }
        #endregion

        #region ArcGIS Methods     

        /// <summary>
        /// Read FeatureClass from DB
        /// </summary>
        /// <param name="fCName"></param>
        /// <param name="propertySet"></param>
        /// <returns>FeatureClass</returns>
        public IFeatureClass GetFCfromSDE(string fCName, IPropertySet propertySet)
        {
            IFeatureClass pTerminalFC = null;
            try
            {

                // propertySet.
                IWorkspaceFactory workspaceFactory = new SdeWorkspaceFactory();
                IWorkspace workspace = workspaceFactory.Open(propertySet, 0);
                IFeatureWorkspace pFeatWsp = workspace as IFeatureWorkspace;
                pTerminalFC = pFeatWsp.OpenFeatureClass(fCName);

            }
            catch (Exception ex) { LogManager.WriteLogandConsole(ex); isExecutionResult = false; }
            return pTerminalFC;
        }

        /// <summary>
        /// Append Method to load the polygon to database table
        /// </summary>
        /// <param name="outputPolygonsFClass"></param>
        /// <param name="inputPolygonsFClass"></param>
        internal void LoadPolygonsToODS(IFeatureClass outputPolygonsFClass, IFeatureClass inputPolygonsFClass)
        {
            try
            {
                //part1 : Dump polygon Data to ODS..
                ESRI.ArcGIS.DataManagementTools.Append pAppend = new ESRI.ArcGIS.DataManagementTools.Append();
                pAppend.inputs = outputPolygonsFClass;// @"D:\SourceData\CenturyLink\Shp\Finalconcavebf125.shp";
                pAppend.target = inputPolygonsFClass;
                pAppend.schema_type = "TEST";
                ESRI.ArcGIS.Geoprocessor.Geoprocessor pGeoProcessor = new ESRI.ArcGIS.Geoprocessor.Geoprocessor();
                ITrackCancel pTrackCancel = new CancelTrackerClass();
                pTrackCancel.CancelOnClick = false;
                pTrackCancel.CancelOnKeyPress = true;
                pGeoProcessor.Execute(pAppend, pTrackCancel);
            }
            catch (Exception ex) { LogManager.WriteLogandConsole(ex); isExecutionResult = false; }
        }

        #endregion

    }
}
