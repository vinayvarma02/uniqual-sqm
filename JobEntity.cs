namespace CenturyLink_ArcApp
{
    public class JobEntity
    {

        #region JOBTABLE
        public string jobTableName { set; get; }

        public string jobId { set; get; }
        //  public string jobDescription { set; get; }
        public string jobCreationDate { set; get; }
        public string jobStatus { set; get; }
        public string machineName { set; get; }
        public string jobWireCenterID { set; get; }
        public string jobDessignations { set; get; }
        // public string jobExecutionStatus { set; get; }
        public string jobEndDate { set; get; }
        public string jobStartDate { set; get; }
        public string jobQualType { set; get; }

        public string jobQualStatus { set; get; }
        public string jobSource { set; get; }

        public string JobSQM_Version { set; get; }
        public string JobDaemon_Version { set; get; }
        public string JobCreateMode { get; set; }


        #endregion

        #region REVIEWTABLE        
        public string ReviewTableName { set; get; }
        public string Review_DesignationID { set; get; }
        public string Review_WireCenterID { set; get; }
        // public string Review_Polygon_ID { set; get; }
        public string Review_Status { set; get; }
        public string Review_JobID { set; get; }
        public string Review_UserID { set; get; }
        public string Review_PolygonID { set; get; }
        public string Review_QualTYID { set; get; }
        public string Review_QualStatusID { set; get; }
        public string Review_RevDate { set; get; }
        public string Review_NDSJNO { set; get; }

        #endregion

        #region SQM_MODEL       
        public string ServicePolyTableName { set; get; }
        public string ServicePoly_POLYGON_ID { set; get; }
        public string ServicePoly_TYPE { set; get; }
        public string ServicePoly_STATUS { set; get; }
        public string ServicePoly_AVAILABILITYDATE { set; get; }
        public string ServicePoly_BANDWIDTH { set; get; }
        public string ServicePoly_SERVIING_WIRE_CENTER_CLLI { set; get; }
        public string ServicePoly_SERVING_WIRE_CENTER_NAME { set; get; }
        public string ServicePoly_FIBER_CABLE_ID { set; get; }
        public string ServicePoly_FIBER_LINE_OF_COUNT { set; get; }
        public string ServicePoly_NDS_JOB_NO { set; get; }
        public string ServicePoly_OLT_RELATIONSHIP { set; get; }
        public string ServicePoly_SHAPE { get; set; }
        #endregion

        #region WIRECENTERTABLE
        public string WirecenterTableName { set; get; }
        public string wireCenterID { set; get; }
        public string WireCenterName { set; get; }
        #endregion

        #region JOBEXCEPTION
        internal string exceptionTablename { set; get; }
        public string exception_polygonID { set; get; }
        public string exception_designationID { set; get; }
        public string exceptionType { set; get; }
        public string exceptionMessage { set; get; }
        public string exceptionDateTime { set; get; }

        public string exceptionNDSJNO { set; get; }

        public string exceptionQualStatus { set; get; }
        #endregion

        #region JOBFAILURES
        internal string failureTablename { set; get; }
        public string failureType { set; get; }
        public string failureMessage { set; get; }
        public string failureDateTime { set; get; }
        public string failureDesgID { set; get; }

        public string failureNDSJNO { set; get; }
        public string failureQualStatus { set; get; }
        #endregion

        #region JOBSTATSLOG
        public string jobStats_Tablename { set; get; }
        public string jobStats_logDate { set; get; }
        public string jobStats_QualStatus { set; get; }
        public string jobStats_noofDesgRecieved { set; get; }
        public string jobStats_noofDesgProcessed { set; get; }
        public string jobStats_noofPolygonsSuccess { set; get; }
        public string jobStats_noofDesgFailed { set; get; }
        public string jobStats_noofoPolygonsExceptions { set; get; }
        public string jobStats_noofProjsRecieved { set; get; }
        public string jobStats_noofProjsProcessed { set; get; }

        #endregion

        #region REVIEWTABLE_Nobuild        
        public string NBReviewTableName { set; get; }
        public string NBReview_JobID { set; get; }
        public string NBReview_WireCenterID { set; get; }
        // public string Review_Polygon_ID { set; get; }
        public string NBReview_StatusID { set; get; }
        public string NBReview_PolygonID { set; get; }
        public string NBReview_RevDate { set; get; }

        #endregion

        #region Nobuild        
        public string NoBuildTableName { set; get; }
        public string NoBuild_WireCenterID { set; get; }
        public string NoBuild_StatusID { set; get; }
        public string NoBuild_Reason { set; get; }


        public string NoBuild_AddrID { set; get; }


        #endregion

        private static JobEntity EntityTable { set; get; }
        private JobEntity() { }
        public static JobEntity GetInstance()
        {
            if (EntityTable == null)
                EntityTable = new JobEntity()
                {
                    //jobTable
                    jobTableName = "JOB_DETAILS",
                    jobId = "JOB_ID",
                    // jobDescription = "JOB_DISCRIPTION",
                    jobCreationDate = "JOB_CREATED_DATE",
                    jobStatus = "JOB_STATUS",
                    machineName= "MACHINE_ID",
                    // jobWireCenterID = "WIRE_CENTER_ID",
                    jobWireCenterID = "WC_ID",
                    jobDessignations = "JOB_DESIGNATION",
                    // jobExecutionStatus = "JOB_EXECUTE_STATUS",
                    jobEndDate = "JOB_END_DATETIME",
                    jobStartDate = "JOB_START_DATETIME",
                    //  jobQualType = "QUAL_TYPE",
                    jobQualType = "QUAL_TYPE_ID",
                    jobQualStatus = "QUAL_STATUS",
                    jobSource = "SOURCE",
                    JobSQM_Version = "SQM_VERSION",
                    JobDaemon_Version = "DAEMON_VERSION",

                    JobCreateMode = "CREATE_MODE",
                    //review table
                    ReviewTableName = "REVIEW_DETAILS",
                    Review_DesignationID = "DESIGNATION_ID",
                    Review_WireCenterID = "WC_ID",
                    Review_Status = "STATUS_ID",
                    Review_JobID = "JOB_ID",
                    Review_UserID = "USER_ID",
                    Review_PolygonID = "POLYGON_ID",
                    Review_QualTYID = "QUAL_TYPE_ID",
                    Review_RevDate = "REVIEW_DATE",
                    Review_NDSJNO = "NDS_JNO",
                    Review_QualStatusID = "QUAL_STATUS_ID",

                    //NBReview
                    NBReviewTableName = "REVIEW_NOBUILD",
                    NBReview_JobID = "JOB_ID",
                    NBReview_WireCenterID = "WC_ID",
                    NBReview_PolygonID = "POLYGON_ID",
                    NBReview_StatusID = "STATUS_ID",
                    NBReview_RevDate = "REVIEW_DATE",

                    //NoBuild
                    NoBuildTableName = "NOBUILD",
                    NoBuild_StatusID = "STATUS",
                    NoBuild_Reason = "REASON",
                    NoBuild_WireCenterID = "WC_ID",
                    NoBuild_AddrID = "ADDR_ID",


                    //service polygons
                    ServicePolyTableName = "SERVICE_QUALIFICATION_MODEL",
                    ServicePoly_POLYGON_ID = "OBJECTID",
                    ServicePoly_TYPE = "QU_TYPE",
                    ServicePoly_STATUS = "QU_TY_STAT",
                    ServicePoly_AVAILABILITYDATE = "AVAIL_DT",
                    ServicePoly_BANDWIDTH = "BAND",
                    ServicePoly_SERVIING_WIRE_CENTER_CLLI = "WC_ID",
                    ServicePoly_SERVING_WIRE_CENTER_NAME = "WC_NAME",
                    ServicePoly_FIBER_CABLE_ID = "FCBL_ID",
                    ServicePoly_FIBER_LINE_OF_COUNT = "FLOC",
                    ServicePoly_NDS_JOB_NO = "NDS_JNO",
                    ServicePoly_OLT_RELATIONSHIP = "OLT_REL",
                    ServicePoly_SHAPE = "SHAPE",

                    //WireCenter Table
                    WirecenterTableName = "CL_WIRE_CENTER",
                    wireCenterID = "ID",
                    WireCenterName = "WIRE_CENTER_NAME",

                    exceptionTablename = "JOB_EXCEPTIONS",
                    exception_polygonID = "POLYGON_ID",
                    exceptionMessage = "EXCEPTION_MESSAGE",
                    exceptionType = "EXCEPTION_TYPE",
                    exception_designationID = "DESIGNATION_ID",
                    exceptionDateTime = "EXCEPTION_DATETIME",
                    exceptionNDSJNO = "NDS_JNO",
                    exceptionQualStatus = "QUAL_STATUS",

                    failureTablename = "JOB_FAILURES",
                    failureMessage = "FAILURE_MESSAGE",
                    failureDateTime = "FAILURE_DATETIME",
                    failureType = "FAILURE_TYPE",
                    failureDesgID = "DESIGNATION_ID",
                    failureNDSJNO = "NDS_JNO",
                    failureQualStatus = "QUAL_STATUS",

                    //StatsTable
                    jobStats_Tablename = "JOB_STATISTICS_LOGS",
                    jobStats_logDate = "LOG_DATE_TIME",
                    jobStats_QualStatus = "QUAL_STATUS",
                    jobStats_noofDesgRecieved = "NO_OF_DESIGNATIONS_RECEIVED",
                    jobStats_noofDesgProcessed = "NO_OF_DESIGNATIONS_PROCESSED",

                    jobStats_noofProjsRecieved = "NO_OF_PROJECTS_RECEIVED",
                    jobStats_noofProjsProcessed = "NO_OF_PROJECTS_PROCESSED",

                    jobStats_noofPolygonsSuccess = "NO_OF_SUCCESS",//"NO_OF_POLYGONS_SUCCESS",
                    jobStats_noofDesgFailed = "NO_OF_FAILED",//"NO_OF_DESIGNATION_FAILED",
                    jobStats_noofoPolygonsExceptions = "NO_OF_EXCEPTION",//"NO_OF_POLYGONS_EXCEPTION",


                };
            return EntityTable;
        }

    }
}
