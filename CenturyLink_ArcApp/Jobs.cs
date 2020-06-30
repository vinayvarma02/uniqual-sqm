using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using DbLibrary;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.Display;
using static CenturyLink_ArcApp.Program;
using configKey = CenturyLink_ArcApp.Properties.Settings;
using System.Linq;
using System.Diagnostics;

namespace CenturyLink_ArcApp
{
    public class Jobs : IJob
    {
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
                currentJob.qualStatus = Convert.ToInt16(dr[4].ToString());
                if (currentJob.qualStatus != 3)
                    currentJob.qualType = Convert.ToInt16(dr[3].ToString());
                currentJob.wireCenterName = GetWireCenterNamefromID(currentJob.wireCenterID);
                currentJob.createmode = Convert.ToInt16(dr[5].ToString());
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
            { //drop view "SWGISLOC"."TERMINAL_DATA4SQM_1686"
                string query = "drop view " + dbViewName;
                DbWorker dbWorker = new DbWorker();
                int rowsAffected = dbWorker.RunDmlQuery(query);
            }
            catch (Exception ex) { LogManager.WriteLogandConsole(ex); isExecutionResult = false; }
        }
        internal void Deleteludata(string tablename,string wirecnetername)
        {
            try
            { //drop view "SWGISLOC"."TERMINAL_DATA4SQM_1686"
                string query = "delete from " + tablename+" where WC_ID='"+wirecnetername+"'";
                DbWorker dbWorker = new DbWorker();
                int rowsAffected = dbWorker.RunDmlQuery(query);
            }
            catch (Exception ex) { LogManager.WriteLogandConsole(ex); isExecutionResult = false; }
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


        /// <summary>
        /// Read db properties to PropertySet 
        /// </summary>
        /// <returns>IPropertySet</returns>
        public IPropertySet LoadSDEswgisProperties()
        {
            IPropertySet propertySet = new ESRI.ArcGIS.esriSystem.PropertySetClass();
            if (configKey.Default.RunMode == "0")
            {
                propertySet.SetProperty(Constants.DBProperties.server, configKey.Default.Qual_Server); //"172.16.8.144"
                propertySet.SetProperty(Constants.DBProperties.instance, configKey.Default.Qual_INSTANCE);//"sde:oracle11g:auroradb");
                propertySet.SetProperty(Constants.DBProperties.authMode, configKey.Default.AUTHENTICATION_MODE);// "DBMS");
                propertySet.SetProperty(Constants.DBProperties.database, configKey.Default.Qual_DATABASE);// "auroradb");
                propertySet.SetProperty(Constants.DBProperties.user, configKey.Default.Qual_USER_SWGISLOC);//"SWGISLOC");
                propertySet.SetProperty(Constants.DBProperties.password, configKey.Default.Qual_PASSWORD_SWGISLOC);//"SWGISLOC");
                propertySet.SetProperty(Constants.DBProperties.version, configKey.Default.VERSION);//"sde.DEFAULT");
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

        internal void InserIntoReviewNB(List<NoBuild> noBuildFC, JobEntity entity, bool isFailePolys)
        {
            try
            {
                int rowAffected = 0;
                string Query = string.Empty;
                string dateTimeStamp = "'" + DateTime.Now.ToString("dd-MMM-yyyy hh:mm:ss") + "'";
                /*
                //get UserID frm Review table for current jobID,wirecenterID
                //string UserID = string.Empty;
                //string getQuery = string.Format("Select {0} From {1} Where {2} = {3} and {4} = {5}",
                //    entity.Review_UserID, entity.ReviewTableName, entity.Review_JobID, this.jobID, entity.Review_WireCenterID, this.wireCenterID);
                //DbWorker dbww = new DbWorker();
                //Exception ex = null;
                //DataTable dt = dbww.ReadTable(getQuery, out ex);
                //foreach (DataRow row in dt.Rows)
                //{
                //    UserID = row[0].ToString();
                //    break;
                //}
                */
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
                return Query = string.Format("INSERT INTO {0} ({1},{2},{3},{4},{5},{6},{7}) VALUES ({8},'{9}',{10},'{11}',{12},'{13}',{14})",
                  entity.failureTablename, entity.jobId, entity.failureDesgID, entity.failureType, entity.failureMessage, entity.failureDateTime, entity.failureNDSJNO, entity.failureQualStatus,
                  this.jobID, desg[0], (int)JobPolygonStatusProgressEnum.jobFailureGeometry, failMessage, dateTimeStamp, desg[1], this.qualStatus);
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
                return Query = string.Format("INSERT INTO {0} ({1},{2},{3},{4},{5},{6},{7},{8}) VALUES ({9},{10},'{11}',{12},'{13}',{14},'{15}',{16})",
              entity.exceptionTablename, entity.jobId, entity.exception_polygonID, entity.failureDesgID, entity.exceptionType,
              entity.exceptionMessage, entity.exceptionDateTime, entity.exceptionNDSJNO, entity.exceptionQualStatus,
              this.jobID, 0, desg[0], (int)JobPolygonStatusProgressEnum.jobExceptionOverlaps, failMessage, dateTimeStamp, desg[1], this.qualStatus);
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
                if (status == (int)JobPolygonStatusProgressEnum.Job_Execution_InProgress)
                    Query = string.Format("UPDATE  {0} SET {1} = {2},{3} = {4},{5} = '{6}',{7} = '{8}' WHERE {9} = {10}",
                        jobEntity.jobTableName, jobEntity.jobStatus, (int)JobPolygonStatusProgressEnum.Job_Execution_InProgress, jobEntity.jobStartDate, dateTimeStamp,
                        jobEntity.JobSQM_Version, version, jobEntity.JobDaemon_Version, version,
                        jobEntity.jobId, JOBID);

                else if (status == (int)JobPolygonStatusProgressEnum.Job_Execution_Completed)  // status is wip and jobexestatus = completed..
                    Query = string.Format("UPDATE  {0} SET {1} = {2},{3} = {4} WHERE {5} = {6}",
                        jobEntity.jobTableName, jobEntity.jobStatus, (int)JobPolygonStatusProgressEnum.Job_Execution_Completed,
                        jobEntity.jobEndDate, dateTimeStamp, jobEntity.jobId, JOBID);
                else if (status == (int)JobPolygonStatusProgressEnum.Job_Execution_Cancelled)  // status is wip and jobexestatus = completed..
                    Query = string.Format("UPDATE  {0} SET {1} = {2},{3} = {4} WHERE {5} = {6}",
                        jobEntity.jobTableName, jobEntity.jobStatus, (int)JobPolygonStatusProgressEnum.Job_Execution_Cancelled,
                        jobEntity.jobEndDate, dateTimeStamp, jobEntity.jobId, JOBID);
                DbWorker dbWorker = new DbWorker();
                rowAffected = dbWorker.RunDmlQuery(Query);
            }
            catch (Exception ex) { LogManager.WriteLogandConsole(ex); isExecutionResult = false; }
            return rowAffected;
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
            " on clc.fk_sheath_with_loc_terminal = swlt.id  join " + configKey.Default.SitesandJobs + " sj on sj.JOB_NUM = clc.cl_project_name" +
            " where clc.cl_project_name is not null AND swlt.fk_cl_wire_center = '" + this.wireCenterID + "' and clc.designation in (" + sb.ToString() + ")" +
             " and sj.job_status in ('​Permit','Placing','​Splicing','​Construction','Power','Turn-up','​​​Dev Not Ready','​Cst Cmpl') " +
            "union all select to_number(pin.id) as id,clc.designation || ':' || sj.job_num as designation,pin.location,sj.FIRST_OCCUPANCY_TGT_DT,sj.job_status" +
            " from " + configKey.Default.sheath_splice + " spl inner join " + configKey.Default.SHEATH_WITH_LOC_TERMINAL + " ter on spl.id = ter.fk_sheath_splice" +
            " inner join " + configKey.Default.MAT_SHEATH_WITH_LOC_PIN + " pin on pin.sheath_splice_id = spl.id" +
            " inner join " + configKey.Default.CopperLineofCountTable + " clc on clc.fk_sheath_with_loc_terminal = ter.id join " + configKey.Default.SitesandJobs + " sj on  sj.JOB_NUM = clc.cl_project_name where" +
             " sj.job_status in ('​Permit','Placing','​Splicing','​Construction','Power','Turn-up','​​​Dev Not Ready','​Cst Cmpl') and" +
            " clc.cl_project_name is not null and ter.id in (select distinct swlt.id from " + configKey.Default.CopperLineofCountTable + " clc join " + configKey.Default.SHEATH_WITH_LOC_TERMINAL + " Swlt on" +
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
            //   SqlQueryHelper.LoadServerConnection(false);

            //  DataTable designations = worker.ReadTable("SELECT DISTINCT DESIGNATION FROM SWGISLOC.TERMINAL_DATA4SQM_2407");
            Exception ex = null;
            try
            {
                foreach (var item in designationLst)
                {
                   // if (item.Key != "LD3011-8") continue;
                    DataTable dt = worker.ReadTable("select SUBSTR(name,INSTR(name,REGEXP_SUBSTR( name, '\\d[a-zA-Z0-9\\s]+'),1),LENGTH(name)) as name " +
    "from " + configKey.Default.mit_terminal_enclosure + " WHERE fk_mit_structure_point IN (Select DISTINCT MIT_STRUCTURE_POINT_ID from " + configKey.Default.MIT_CABLE + " where sheath_with_loc_id in ( " +
    "SELECT DISTINCT fk_sheath_with_loc  FROM " + configKey.Default.CopperLineofCountTable +
    " where fk_sheath_with_loc in  (Select ID from " + configKey.Default.sheath_with_loc + " where fk_cl_wire_center = '" + this.wireCenterID + "') " +//1572328488445168705
    "AND DESIGNATION in ('" + item.Key + "')) AND MIT_STRUCTURE_POINT_ID IS NOT NULL) AND fk_cl_wire_center = '" + this.wireCenterID + "' AND name LIKE 'FSAI%'", out ex);

                    if (dt.Rows.Count == 1)
                    {
                        int count=0;
                        String name = dt.Rows[0][0].ToString();
                        if (name != null && name != "")
                        {
                            int rr = worker.RunDmlQuery("delete from " + configKey.Default.LU_Latlong + " where WC_ID='" + this.wireCenterID + "' AND DESIGNATION ='" + item.Key + "'");
                            count = worker.RunDmlQuery("INSERT INTO " + configKey.Default.LU_Latlong + " (CLLI_ST,SERVING_TERMINAL,LAT,LON,FSAI_ADDR,DOWN_STREAM,DESIGNATION,WC_ID) " +
       "select 'N' AS clli_st, a.SERVING_TERMINAL, a.lat, a.lon, b.FSAI_ADDR,a.MOD_SPEED_DOWN, '" + item.Key + "' AS DESIG,'" + this.wireCenterID + "' AS WC_ID from " + configKey.Default.Livingunits + " a inner join " +  //SWGISLOC.livingunits
       "" + configKey.Default.GPON_OC_ONT_SUMMARY + " b on a.SERVING_TERMINAL = b.ont_term_addr where b.FSAI_ADDR LIKE '%" + dt.Rows[0][0].ToString() + "'");

                            LUsCount = LUsCount + count;
                        }
                    }
                }
                SqlQueryHelper.LoadServerConnection(false);
                int re = worker.RunDmlQuery("UPDATE " + configKey.Default.LU_Latlong + " SET shape = SDO_GEOMETRY(2001,4326,SDO_POINT_TYPE(LON, LAT, NULL),NULL,NULL) where WC_ID='"+this.wireCenterID+"'");
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
