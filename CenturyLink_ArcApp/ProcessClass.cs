﻿using DbLibrary;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using static CenturyLink_ArcApp.Program;
using configKey = CenturyLink_ArcApp.Properties.Settings;


namespace CenturyLink_ArcApp
{
    class ProcessClass
    {
        #region Private Varables
        private IFeatureClass teminalFeatureClass = null;
        private IFeatureClass constrFeatureClass = null;
        private IFeatureClass roadLinksFeatureClass = null;
        private IFeatureClass wireCenterBoundFeatureClass = null;
        private IFeatureClass servicePolygonsOutputFC = null;
        private IFeatureClass FcParcels = null;
        private IFeatureClass fcLND_Address = null;

        private bool swgisDBConnection = false;
        private bool qualSchemaDBConnection = true;
        private bool servicePolygons = false;
        private bool constrPolygons = true;

        private bool noBuild = false;
        private bool IsCopper = false;

        public static Dictionary<string, string> constructionDesignations;
        public Jobs jobTask = null;


        #endregion

        /// <summary>
        /// Delete Directory from path
        /// </summary>
        /// <param name="path"></param>
        private void deletefile(string path)
        {
            try
            {
                System.IO.DirectoryInfo di = new DirectoryInfo(path);
                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in di.GetDirectories())
                {
                    dir.Delete(true);
                }
            }
            catch (Exception ex)
            {
                isExecutionResult = false;
                LogManager.WriteLogandConsole(ex);
            }
        }


        /// <summary>
        /// Process Method for JOB
        /// </summary>
        /// <param name="jobID"></param>
        public void Process(string jobID)
        {
            try
            {
                LogManager.WriteLogandConsole("INFO : SQM Execution started for JobID : " + jobID);
                if (Directory.Exists(tempfilesPath))
                    deletefile(tempfilesPath);
                Directory.CreateDirectory(tempfilesPath);
                #region Read JObTable               
                LogManager.WriteLogandConsole("INFO : Reading JobTable to get the wirecenter and designations...");
                JobEntity jobentity = JobEntity.GetInstance();
                string[] requiredcolumnNames = new string[7] { jobentity.jobWireCenterID, jobentity.jobDessignations, jobentity.jobStatus,
                    jobentity.jobQualType, jobentity.jobQualStatus,jobentity.JobCreateMode,jobentity.jobSource};
                //open Connection                 
                DbWorker worker = new DbWorker();
                Exception ex = null;
                DataTable jobtable = worker.ReadTable(worker.GetQuery(requiredcolumnNames, jobentity.jobTableName, jobentity.jobId + "=" + jobID), out ex);
                if (ex != null)
                {
                    LogManager.WriteLogandConsole(ex); isExecutionResult = false;
                    return;
                }
                if (jobtable.Rows.Count == 0)
                {
                    LogManager.WriteLogandConsole("ERROR : No matching JOb found in the JOBTABLE to execute - " + jobID); isExecutionResult = false;
                    return;
                }
                #endregion
                foreach (DataRow jobRow in jobtable.Rows)
                {
                    jobTask = new Jobs();
                    if (IsTaskcancelled(jobTask))                 //code for task cancellation
                    {
                        isTaskCancelled = true; isExecutionResult = false; break;
                    }
                    if (jobTask.UpdateJobStatus((int)JobPolygonStatusProgressEnum.Job_Execution_InProgress, Convert.ToInt16(jobID)) == 0) //running status
                    { LogManager.WriteLogandConsole("Error : Unable to Update the job running status in JOB table"); isExecutionResult = false; return; }

                    SqlQueryHelper.LoadServerConnection(swgisDBConnection); //connect to SWIGISLOC    
                    jobTask = Jobs.CreatInstance(jobRow, jobID); //Load the data to job properties.
                    LogManager.WriteLogandConsole("INFO : Processing wireCenter name : " + jobTask.wireCenterName);
                    //check condition for no buid and make parmeter nobuild s true.
                    if (jobTask.qualStatus == Constants.QualStatus.NoBuild)
                        noBuild = true;
                    else if (jobTask.qualType == Constants.QualType.COPPER)
                        IsCopper = true;
                    else
                        LogManager.WriteLogandConsole(string.Format("INFO : Designations count to be processed for the JOBID {0} : {1}",
                    jobID, jobTask.designationLst.Count));

                    #region Reading Input FeatureClasses
                    LogManager.WriteLogandConsole("INFO : Reading Wirecenter Boundary...");
                    wireCenterBoundFeatureClass = jobTask.GetFCfromSDE(configKey.Default.WirecenterBoundaryName, jobTask.LoadSDEswgisProperties());

                    LogManager.WriteLogandConsole("INFO : Reading Road Features...");
                    if (configKey.Default.RunMode == "0") //CTL DB
                        roadLinksFeatureClass = jobTask.GetFCfromSDE(configKey.Default.RoadsTableName, jobTask.LoadODSDEVProperties());
                    else
                        roadLinksFeatureClass = jobTask.GetFCfromSDE(configKey.Default.RoadsTableName, jobTask.LoadSDEswgisProperties());
                    if (roadLinksFeatureClass == null || wireCenterBoundFeatureClass == null)
                    {
                        LogManager.WriteLogandConsole("ERROR : Unable to read the required Input FeatureClasses... execution will terminate!!!");
                        isExecutionResult = false;
                        return;
                    }
                    IFeatureClass RoadsWithinWCFC = null;
                    IPolygon wcBoundry;
                    IFeatureClass roads_Polygon = null;
                    LogManager.WriteLogandConsole("INFO : Getting Roads within wirecenter...");
                    ServiceBoundary_Process servicePoly = new ServiceBoundary_Process(jobTask);

                    servicePoly.ProcessRoadsTogetWCInterectedRoads(roadLinksFeatureClass, wireCenterBoundFeatureClass,
                        out RoadsWithinWCFC, out roads_Polygon, out wcBoundry);
                    if (RoadsWithinWCFC == null || roads_Polygon == null)
                    {
                        LogManager.WriteLogandConsole("ERROR : Unable to process the Roads within wirecenter... execution will terminate!!!");
                        isExecutionResult = false;
                        return;
                    }
                    #endregion

                    #region Copperpolygon processing
                    if (IsCopper)
                    {
                        //copper polygon test 
                        if (Directory.Exists(tempfilesPath + "\\Copperpoly\\"))
                            deletefile(tempfilesPath + "\\Copperpoly\\");
                        Directory.CreateDirectory(tempfilesPath + "\\Copperpoly\\");
                        string wcid = jobTask.GetWCID(jobTask.wireCenterID);
                        string wcLFaC = jobTask.getWcLfacId(jobTask.wireCenterID);
                        LogManager.WriteLogandConsole("INFO : Reading Lat Lons from service addresses...");
                        jobTask.CreateCopperTables(wcid, jobID, wcLFaC);
                        LogManager.WriteLogandConsole("INFO : Started Parcel Shapefile Creation... ");
                        IFeatureClass FCParcels = servicePoly.ConvertFeatureClassToShapefile(wireCenterBoundFeatureClass);
                        LogManager.WriteLogandConsole("INFO : Creating spatial index on the parcel shape file... ");
                        FcParcels = Arclib.AddSpatialindex(FCParcels);
                        RoadsWithinWCFC = Arclib.AddSpatialindex(RoadsWithinWCFC);
                        jobTask.designationLst = new Dictionary<string, List<string>>();
                        LogManager.WriteLogandConsole("INFO : Started generating Copper Polygons... ");
                        List<CopperPolygonClass> copperlist = servicePoly.CreateCopperPolygonGeneration(FCParcels, wcid, jobID);
                        LogManager.WriteLogandConsole("Info: Inserting into CopperAddress");
                        SqlQueryHelper.LoadServerConnection(qualSchemaDBConnection);
                        jobTask.InsertRecordsinCopperAddress(copperlist);
                        LogManager.WriteLogandConsole("Info: Updating Recordds Into LoopQual");
                        SqlQueryHelper.LoadServerConnection(swgisDBConnection);//for getting lfac value
                        if (copperlist.Count > 0)
                            jobTask.UpdateRecordIntoLoopQual(copperlist);
                        LogManager.WriteLogandConsole("Info: Inserting Records into CopperReviewTable");
                        SqlQueryHelper.LoadServerConnection(qualSchemaDBConnection);
                        jobTask.InsertIntoCopperReviewTable(copperlist, jobTask, false);
                        LogManager.WriteLogandConsole("Info: Deleting Table " + "LU_" + wcid + "_" + jobID);
                        SqlQueryHelper.LoadServerConnection(swgisDBConnection);
                        jobTask.DeleteTable("LU_" + wcid + "_" + jobID);
                        #region Update Polygon Stats,Exceptions and failures      
                        LogManager.WriteLogandConsole("INFO : Writing exceptions,failures and Logs");
                        //exceptions table
                        SqlQueryHelper.LoadServerConnection(qualSchemaDBConnection);
                        if (jobTask.dictexception.Count != 0)
                        {
                            jobTask.UpdatePolygonExceptionLogs(jobentity, jobTask.dictexception);
                            if (jobTask.source == 2)
                            {
                                foreach (var item in jobTask.dictexception)//update in change request lo table if the source is from daemon
                                {
                                    string[] splititem = item.Key.Split('|');
                                    string excepluid = splititem[2];
                                    jobTask.updateChangeRequestlogtbl(jobTask.wireCenterID, excepluid, item.Value[1], "Success with Exceptions", jobTask.qualStatus);

                                }
                            }
                        }
                        jobTask.polygonswithExceptions = jobTask.dictexception.Count;
                        jobTask.polygonsSuccess = copperlist.Count;//successPolyLst1.Count;
                        jobTask.ProcessedDesignations = jobTask.dictexception.Count + copperlist.Count;//ExceptnDesgList1.Count + copperlist.Count;//successPolyLst1.Count; 
                        //update Failures table.
                        List<CopperPolygonClass> faiLst1 = new List<CopperPolygonClass>();
                        if (jobTask.dictfailed.Count != 0)//failedDesgList1.Count != 0)
                        {
                            jobTask.UpdateDesignationFailures(jobentity, jobTask.dictfailed);// failedDesgList1);
                            foreach (var item in jobTask.dictfailed)//failedDesgList1)
                            {
                                if (!item.Key.Contains('|')) continue;
                                string[] split1 = item.Key.Split(':');
                                string[] split = split1[1].Split('|');
                                faiLst1.Add(new CopperPolygonClass
                                {
                                    WC_NAME = jobTask.wireCenterName,
                                    LU_Lat = split[0],
                                    LU_Lon = split[1],
                                    Polygon_Status = (int)JobPolygonStatusProgressEnum.Polygon_Completed_with_Error,
                                    QUAL_STATUS = jobTask.qualStatus
                                });

                                if (jobTask.source == 2)//update in change request lo table if the source is from daemon
                                {
                                    string[] splititem = item.Key.Split('|');
                                    string excepluid = splititem[2];
                                    jobTask.updateChangeRequestlogtbl(jobTask.wireCenterID, excepluid, item.Value[1], "Failed", jobTask.qualStatus);

                                }
                            }

                        }

                        LogManager.WriteLogandConsole("INFO : Updating Review Coper Table");

                        if (faiLst1.Count > 0)
                            jobTask.updateReviewCoppertable(faiLst1, jobTask);
                        jobTask.polygonsFailed = faiLst1.Count;
                        //update failedJObs status in reviewTable
                        //update Stats table.     
                        jobTask.constDesgRecieved = faiLst1.Count + copperlist.Count + jobTask.dictexception.Count;
                        jobTask.InsertJobStatsCoper(jobentity);
                        #endregion
                        continue;
                    }
                    #endregion

                    #region NoBuild Polygons Processing
                    else if (noBuild)
                    {
                        // if input requested to NOBUILD......
                        LogManager.WriteLogandConsole("INFO : Started generating NOBUILD Polygons... ");
                        FcParcels = jobTask.GetFCfromSDE(configKey.Default.Parcels, jobTask.LoadSDEswgisProperties());
                        fcLND_Address = jobTask.GetFCfromSDE(configKey.Default.LNDAddress, jobTask.LoadSDEswgisProperties());
                        if (FcParcels == null || fcLND_Address == null)
                        {
                            LogManager.WriteLogandConsole("ERROR : Unable to read the required Inputs(Parcels/LNDaddress)... execution will terminate!!!");
                            isExecutionResult = false;
                            return;
                        }
                        List<NoBuild> noBuildFC = servicePoly.NoBuild(FcParcels, fcLND_Address, roads_Polygon, jobTask);
                        if (noBuildFC == null || noBuildFC.Count == 0)
                        {
                            LogManager.WriteLogandConsole("WARNING : ZERO NOBUILD POLYGONS CREATED,CHECK LOG FOR DETAILS...");
                            continue;
                            // goto UPDATEJOBSTATUS;
                        }
                        LogManager.WriteLogandConsole("INFO : NOBUILD Polygons generated : " + noBuildFC.Count);
                        IFeatureClass noBuildFCfromODS = jobTask.GetFCfromSDE(configKey.Default.Nobuild, jobTask.LoadSDEswgisProperties());
                        Arclib.InsertNobuildFeaturesFinal(noBuildFCfromODS, noBuildFC, jobTask.createmode);
                        jobTask.polygonsSuccess = noBuildFC.Count;
                        foreach (NoBuild item in noBuildFC)
                        {
                            string query = "Select OBJECTID from " + configKey.Default.Nobuild + " where " +
                                jobentity.NoBuild_WireCenterID + " = " + item.WC_ID + " AND " + jobentity.NoBuild_AddrID + " = " + item.Addr_id;
                            DbWorker dbw = new DbWorker();
                            Exception ex1 = null;
                            DataTable dt = dbw.ReadTable(query, out ex1);
                            foreach (DataRow row in dt.Rows)
                            {
                                item.PolygonID = Convert.ToInt32(row[0]);
                                break;
                            }
                        }
                        //Insert into Review Table..
                        jobTask.InserIntoReviewNB(noBuildFC, jobentity, false);
                        // Update Job stats..
                        jobTask.InsertJobStats(jobentity);
                        continue;
                        //   goto UPDATEJOBSTATUS;        
                    }
                    #endregion

                    #region Service Polygons Processing
                    
                    if (IsTaskcancelled(jobTask))                 //code for task cancellation
                    {
                        isTaskCancelled = true;
                        isExecutionResult = false;
                        break;
                    }
                    if (configKey.Default.InputPointTypes == "2" || configKey.Default.InputPointTypes == "3" || configKey.Default.InputPointTypes == "5")
                    {
                        LogManager.WriteLogandConsole("INFO : Reading Living Units data...");
                        if (jobTask.InsertLUData() == 0)
                            LogManager.WriteLogandConsole("INFO : No Living units found for the selected designation...");
                    }
                    LogManager.WriteLogandConsole("INFO : Reading Terminals FeatureClass..");
                    jobTask.CreateViewforFeatureClass(Constants.TerminalView.viewName + jobID);
                    teminalFeatureClass = jobTask.GetFCfromSDE(Constants.TerminalView.dbName + Constants.TerminalView.viewName + jobID, jobTask.LoadSDEswgisProperties());
                    if (teminalFeatureClass == null)
                    {
                        LogManager.WriteLogandConsole("ERROR : Unable to Read the Terminal Inputs to process further... execution will terminate!!!");
                        isExecutionResult = false;
                        return;
                    }
                    if (TerminalViewCount(Constants.TerminalView.viewName + jobID) == 0)
                    {
                        LogManager.WriteLogandConsole("Warning : No Terminals found for the selected designations... execution will terminate!!!");
                        isExecutionResult = false;
                        // return;
                    }
                    LogManager.WriteLogandConsole("INFO : Started processing Service Polygons...");
                    List<ServicePolygonClass> servicePolyList = servicePoly.GenerateServicePolygons(teminalFeatureClass, roads_Polygon, wcBoundry,
                        out servicePolygonsOutputFC, servicePolygons); //Create service Polygons
                    if (servicePolyList.Count != 0)
                    {
                        LogManager.WriteLogandConsole("INFO : processing Parcels...");
                        servicePolyList = checkingparcelswithfinal(servicePolyList, servicePoly);
                        LogManager.WriteLogandConsole("INFO : Excluding Polygons...");
                        servicePolyList = Excludelanduse(servicePolyList);
                    }

                    if (IsTaskcancelled(jobTask))                 //code for task cancellation
                    {
                        isTaskCancelled = true;
                        isExecutionResult = false;
                        break;
                    }
                    LogManager.WriteLogandConsole("INFO : Service Polygons Generated : " + servicePolyList.Count);
                    if (servicePolyList.Count == 0 || servicePolygonsOutputFC == null)
                        LogManager.WriteLogandConsole("WARNING : No Service polygons are created for the given designations...");
                    else if (!CheckFOWRoad(RoadsWithinWCFC) || GetFow2RoadsCount(RoadsWithinWCFC) == 0)
                    {
                        LogManager.WriteLogandConsole("WARNING : The service boundaries matched road data does not contain FOW(2) DC roads information, " +
                            "Hence the application skipped the validation with DC Boundary");
                        LoadtoSQModelandReviewTable(jobTask, servicePolyList, jobentity);
                    }
                    else
                    {
                        LogManager.WriteLogandConsole("INFO : Validating service Polygons with DC Boundary...");
                        servicePolygonsOutputFC = servicePoly.AddDCpolytoFinalboundary(servicePolyList, servicePolygonsOutputFC, RoadsWithinWCFC);
                        LoadtoSQModelandReviewTable(jobTask, servicePolyList, jobentity);
                    }
                    jobTask.DeleteView(Constants.TerminalView.dbName + Constants.TerminalView.viewName + jobID);
                    jobTask.DeleteRecords(configKey.Default.LU_Latlong, jobTask.wireCenterID);

                    #region Update Polygon Stats,Exceptions and failures      
                    LogManager.WriteLogandConsole("INFO : Writing exceptions,failures and Logs");
                    //exceptions table
                    SqlQueryHelper.LoadServerConnection(qualSchemaDBConnection);
                    Dictionary<string, List<string>> ExceptnDesgList = jobTask.designationLst.Where
                        (kvp => kvp.Value[0] == JobPolygonStatusProgressEnum.deignationException.ToString()).ToDictionary(i => i.Key, i => i.Value);
                    if (ExceptnDesgList.Count != 0)
                    {
                        jobTask.UpdatePolygonExceptionLogs(jobentity, ExceptnDesgList);
                        if (jobTask.source == 2)
                        {
                            foreach (var item in ExceptnDesgList)//update in change request lo table if the source is from daemon
                                jobTask.updateChangeRequestlogtbl(jobTask.wireCenterID, item.Key, item.Value[1], "Success with Exceptions", jobTask.qualStatus);
                        }
                    }
                    jobTask.polygonswithExceptions = ExceptnDesgList.Count;

                    Dictionary<string, List<string>> successPolyLst = jobTask.designationLst.Where
                      (kvp => kvp.Value[0] == JobPolygonStatusProgressEnum.designationSuccess.ToString()).ToDictionary(i => i.Key, i => i.Value);

                    jobTask.polygonsSuccess = successPolyLst.Count;
                    jobTask.ProcessedDesignations = ExceptnDesgList.Count + successPolyLst.Count;

                    //update Failures table.
                    List<ServicePolygonClass> faiLst = new List<ServicePolygonClass>();

                    Dictionary<string, List<string>> failedDesgList = jobTask.designationLst.Where
                        (kvp => kvp.Value[0] == JobPolygonStatusProgressEnum.designationFailed.ToString()).ToDictionary(i => i.Key, i => i.Value);
                    if (failedDesgList.Count != 0)
                    {
                        jobTask.UpdateDesignationFailures(jobentity, failedDesgList);
                        foreach (var item in failedDesgList)
                        {
                            faiLst.Add(new ServicePolygonClass
                            {
                                SERVIING_WIRE_CENTER_CLLI = jobTask.wireCenterID,
                                FIBER_CABLE_ID = item.Key,
                                Polygon_Status = (int)JobPolygonStatusProgressEnum.Polygon_Completed_with_Error,
                                STATUS = jobTask.qualStatus
                            });

                            if (jobTask.source == 2)//update in change request lo table if the source is from daemon
                                jobTask.updateChangeRequestlogtbl(jobTask.wireCenterID, item.Key, item.Value[1], "Failed", jobTask.qualStatus);
                        }
                    }
                    //Check not processed polygons
                    Dictionary<string, List<string>> notProcessedDesgList = jobTask.designationLst.Where
                        (kvp => kvp.Value[0] == JobPolygonStatusProgressEnum.deignationCreated.ToString()).
                        ToDictionary(i => i.Key, i => new List<string> { JobPolygonStatusProgressEnum.deignationCreated.ToString(), Constants.Messages.noGeom4DesignationErrorMessage });

                    if (notProcessedDesgList.Count != 0)
                    {
                        jobTask.UpdateDesignationFailures(jobentity, notProcessedDesgList);
                        foreach (var item in notProcessedDesgList)
                        {
                            faiLst.Add(new ServicePolygonClass
                            {
                                SERVIING_WIRE_CENTER_CLLI = jobTask.wireCenterID,
                                FIBER_CABLE_ID = item.Key,
                                Polygon_Status = (int)JobPolygonStatusProgressEnum.Polygon_Completed_with_Error,
                                STATUS = jobTask.qualStatus
                            });
                            if (jobTask.source == 2)//update in change request lo table if the source is from daemon
                                jobTask.updateChangeRequestlogtbl(jobTask.wireCenterID, item.Key, item.Value[1], "Failed", jobTask.qualStatus);
                        }
                    }
                    if (faiLst.Count > 0)
                        jobTask.UpdateReviewTable(faiLst, jobentity, true);
                    jobTask.polygonsFailed = failedDesgList.Count + notProcessedDesgList.Count;
                    //update failedJObs status in reviewTable
                    //update Stats table.             
                    jobTask.InsertJobStats(jobentity);
                    #endregion
                    #endregion Service Polygons Processing

                    #region Construction Polygons Processing
                    LogManager.WriteLogandConsole("INFO : Reading inputs for construction polygons..");
                    tempfilesPath = configKey.Default.TempFilesPath + jobID + "\\Construction\\";
                    Directory.CreateDirectory(tempfilesPath);
                    jobTask.ConstructionviewforFC(Constants.ConstructionView.dbName + Constants.ConstructionView.viewName + jobID);
                    constrFeatureClass = jobTask.GetFCfromSDE(Constants.ConstructionView.dbName + Constants.ConstructionView.viewName + jobID, jobTask.LoadSDEswgisProperties());
                    if (constrFeatureClass == null)
                    {
                        LogManager.WriteLogandConsole("ERROR : Unable to Read the construction Inputs to process further... execution will terminate!!!");
                        isExecutionResult = false;
                        return;
                    }
                    if (TerminalViewCount(Constants.ConstructionView.dbName + Constants.ConstructionView.viewName + jobID) == 0)
                    {
                        LogManager.WriteLogandConsole("Warning : No Construction found for the selected designations... execution will terminate!!!");
                        isExecutionResult = false;
                        return;
                    }
                    LogManager.WriteLogandConsole("INFO : Started procesing construction Polygons..."); //Construction
                    jobTask.designationLst = new Dictionary<string, List<string>>(); //List of all desig with NDS job concardinated
                    constructionDesignations = new Dictionary<string, string>(); //unique designations
                    jobTask.qualStatus = Constants.QualStatus.InConstruction;

                    List<ServicePolygonClass> constrPolyList = servicePoly.GenerateServicePolygons(constrFeatureClass, roads_Polygon, wcBoundry,
                        out servicePolygonsOutputFC, constrPolygons); //Create construction Polygons
                    //Adding parcels and land use for construction polygon 
                    if (constrPolyList.Count != 0)
                    {
                        LogManager.WriteLogandConsole("INFO : processing Parcels...");
                        constrPolyList = checkingparcelswithfinal(constrPolyList, servicePoly);
                        LogManager.WriteLogandConsole("INFO : Excluding Polygons...");
                        constrPolyList = Excludelanduse(constrPolyList);
                    }
                    LogManager.WriteLogandConsole("INFO : Construction Polygons Generated : " + constrPolyList.Count);

                    LoadConstructionstoSQModeL(jobTask, constrPolyList, jobentity); //insert into review also
                    //create dictonary for constructonpolygons
                    jobTask.DeleteView(Constants.ConstructionView.dbName + Constants.ConstructionView.viewName + jobID);
                    //statistics
                    jobTask.constDesgRecieved = constructionDesignations.Count;
                    Dictionary<string, string> processesCOnstructiondesg = constructionDesignations.Where
                      (kvp => kvp.Value == "Processed").ToDictionary(i => i.Key, i => i.Value);
                    jobTask.constDesgProcessed = processesCOnstructiondesg.Count;

                    #region Update Polygon Stats,Exceptions and failures      
                    LogManager.WriteLogandConsole("INFO : Writing exceptions,failures and Logs");
                    //exceptions table
                    Dictionary<string, List<string>> ExceptnDesgListCnstr = jobTask.designationLst.Where
                        (kvp => kvp.Value[0] == JobPolygonStatusProgressEnum.deignationException.ToString()).ToDictionary(i => i.Key, i => i.Value);
                    if (ExceptnDesgListCnstr.Count != 0)
                    {
                        jobTask.UpdatePolygonExceptionLogs(jobentity, ExceptnDesgListCnstr);
                        foreach (var item in ExceptnDesgListCnstr)
                        {
                            if (jobTask.source == 2)//update in change request lo table if the source is from daemon                  
                                jobTask.updateChangeRequestlogtbl(jobTask.wireCenterID, item.Key, item.Value[1], "Success with Exceptions", jobTask.qualStatus);
                        }
                    }
                    jobTask.polygonswithExceptions = ExceptnDesgListCnstr.Count;

                    Dictionary<string, List<string>> successPolyLstCnstr = jobTask.designationLst.Where
                      (kvp => kvp.Value[0] == JobPolygonStatusProgressEnum.designationSuccess.ToString()).ToDictionary(i => i.Key, i => i.Value);

                    jobTask.polygonsSuccess = successPolyLstCnstr.Count;
                    jobTask.ProcessedDesignations = ExceptnDesgListCnstr.Count + successPolyLstCnstr.Count;
                    //update Failures table.
                    List<ServicePolygonClass> faiLstCnstr = new List<ServicePolygonClass>();
                    Dictionary<string, List<string>> failedDesgListCnstr = jobTask.designationLst.Where
                        (kvp => kvp.Value[0] == JobPolygonStatusProgressEnum.designationFailed.ToString()).ToDictionary(i => i.Key, i => i.Value);
                    if (failedDesgListCnstr.Count != 0)
                    {
                        jobTask.UpdateDesignationFailures(jobentity, failedDesgListCnstr);
                        foreach (var item in failedDesgListCnstr)
                        {
                            faiLstCnstr.Add(new ServicePolygonClass { SERVIING_WIRE_CENTER_CLLI = jobTask.wireCenterID, FIBER_CABLE_ID = item.Key, Polygon_Status = (int)JobPolygonStatusProgressEnum.Polygon_Completed_with_Error });
                            if (jobTask.source == 2)//update in change request lo table if the source is from daemon
                                jobTask.updateChangeRequestlogtbl(jobTask.wireCenterID, item.Key, item.Value[1], "Failed", jobTask.qualStatus);
                        }
                    }
                    //Check not processed polygons
                    Dictionary<string, List<string>> notProcessedDesgListCnstr = jobTask.designationLst.Where
                        (kvp => kvp.Value[0] == JobPolygonStatusProgressEnum.deignationCreated.ToString()).
                        ToDictionary(i => i.Key, i => new List<string> { JobPolygonStatusProgressEnum.deignationCreated.ToString(), Constants.Messages.noGeom4DesignationErrorMessage });
                    if (notProcessedDesgListCnstr.Count != 0)
                    {
                        jobTask.UpdateDesignationFailures(jobentity, notProcessedDesgListCnstr);
                        foreach (var item in notProcessedDesgListCnstr)
                        {
                            faiLstCnstr.Add(new ServicePolygonClass { SERVIING_WIRE_CENTER_CLLI = jobTask.wireCenterID, FIBER_CABLE_ID = item.Key, Polygon_Status = (int)JobPolygonStatusProgressEnum.Polygon_Completed_with_Error });
                            if (jobTask.source == 2)//update in change request lo table if the source is from daemon
                                jobTask.updateChangeRequestlogtbl(jobTask.wireCenterID, item.Key, item.Value[1], "Failed", jobTask.qualStatus);
                        }
                    }
                    if (faiLstCnstr.Count > 0)
                        jobTask.InsertIntoReviewTable(constrPolyList, jobentity, true);

                    jobTask.polygonsFailed = faiLstCnstr.Count + notProcessedDesgListCnstr.Count;
                    //update failedJObs status in reviewTable
                    //update Stats table.   
                    //if (constrPolyList.Count != 0)
                    jobTask.InsertJobStats(jobentity);
                    #endregion
                    #endregion

                    // UPDATEJOBSTATUS:
                    if (jobTask.UpdateJobStatus((int)JobPolygonStatusProgressEnum.Job_Execution_Completed, jobTask.jobID) == 0) //Complete status
                    { LogManager.WriteLogandConsole("Error : Unable to Update the  job Completed status in JOB table"); isExecutionResult = false; }

                }
            }
            catch (Exception ex)
            {
                isExecutionResult = false;
                LogManager.WriteLogandConsole(ex);
            }
            finally
            {
                SqlQueryHelper.LoadServerConnection(qualSchemaDBConnection);
                if (isExecutionResult)
                {
                    if (jobTask.UpdateJobStatus((int)JobPolygonStatusProgressEnum.Job_Execution_Completed, jobTask.jobID) == 0) //Complete status
                    { LogManager.WriteLogandConsole("Error : Unable to Update the  job Completed status in JOB table"); }
                    LogManager.WriteLogandConsole("INFO : SQM Execution successfully completed for JobID : " + jobID);
                    LogManager.WriteLogandConsole("Application Successfully closed..");
                }
                else if (isTaskCancelled)
                {
                    if (jobTask.UpdateJobStatus((int)JobPolygonStatusProgressEnum.Job_Execution_Cancelled, Convert.ToInt16(jobID)) == 0) //Complete status
                    { LogManager.WriteLogandConsole("Error : Unable to Update the  job Cancelled status in JOB table"); }
                    LogManager.WriteLogandConsole("INFO : Job Ececution cancelled for JobID : " + jobID);
                    LogManager.WriteLogandConsole("Application Successfully closed..");
                }
                else
                {
                    if (jobTask.UpdateJobStatus((int)JobPolygonStatusProgressEnum.Job_Execution_Completed, Convert.ToInt16(jobID)) == 0) //Complete status
                    { LogManager.WriteLogandConsole("Error : Unable to Update the  job Completed status in JOB table"); }
                    LogManager.WriteLogandConsole("INFO : SQM Execution failed with Errors for JobID : " + jobID);
                    LogManager.WriteLogandConsole("Application closed..");
                }
            }
        }
        private List<ServicePolygonClass> checkingparcelswithfinal(List<ServicePolygonClass> servicePolyList, ServiceBoundary_Process ss)
        {
            try
            {

                IFeatureClass FCParcels = ss.ConvertFeatureClassToShapefile(wireCenterBoundFeatureClass);
                List<ServicePolygonClass> final = new List<ServicePolygonClass>();
                for (int i = 0; i < servicePolyList.Count; i++)
                {
                    List<IPolygon> geolist = new List<IPolygon>();
                    IGeometry igeo = servicePolyList[i].polyGeometry;
                    ISpatialFilter spatialFilter = new SpatialFilterClass();
                    spatialFilter.Geometry = igeo;
                    spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                    spatialFilter.GeometryField = FCParcels.ShapeFieldName;
                    IFeatureCursor cursor = FCParcels.Search(spatialFilter, false);
                    IFeature ifeature = null;
                    ITopologicalOperator topo = igeo as ITopologicalOperator;
                    int indxlanduse = FCParcels.Fields.FindField("LAND_USE");
                    int featurecount = FCParcels.FeatureCount(spatialFilter);
                    while ((ifeature = cursor.NextFeature()) != null)
                    {
                        int oid = ifeature.OID;
                        if (string.IsNullOrEmpty(ifeature.get_Value(indxlanduse).ToString()) || ifeature.get_Value(indxlanduse).ToString() == " "
                            || ifeature.get_Value(indxlanduse).ToString() == "650")
                            continue;
                        //if landuse is null then continue;
                        IGeometry geo = topo.Intersect(ifeature.Shape, esriGeometryDimension.esriGeometry2Dimension);
                        IArea inersectedGeomArea = geo as IArea;
                        IArea totalRoadPolyGeomArea = ifeature.Shape as IArea;
                        double intesectedAreaPercent = (inersectedGeomArea.Area / totalRoadPolyGeomArea.Area) * 100;
                        int parcelpercentageval1 = Convert.ToInt32(configKey.Default.ParcelProcessingPercentage1);
                        double parcelpercentageval2 = Convert.ToDouble(configKey.Default.ParcelProcessingPercentage2);
                        if (intesectedAreaPercent > parcelpercentageval1 && intesectedAreaPercent < parcelpercentageval2) 
                        {
                            //check if feat is multipart or not.  
                            IPolygon4 pfeatpoly = ifeature.Shape as IPolygon4;
                            int multipart = CheckMultipartPolygon(pfeatpoly);
                            if (multipart > 1)
                                continue;
                            else
                                geolist.Add(ifeature.Shape as IPolygon);
                        }
                        else if (intesectedAreaPercent < parcelpercentageval1)
                        {

                            IGeometry geome = topo.Difference(ifeature.Shape);
                            topo = geome as ITopologicalOperator;
                            //check if topo is multipart or not.
                            IPolygon4 topopoly = topo as IPolygon4;
                            int multipart1 = CheckMultipartPolygon(topopoly);
                            if (multipart1 > 1)
                                continue;
                            else
                                igeo = geome;
                        }
                    }
                    geolist.Add(igeo as IPolygon);
                    IPolygon finalgeo = Arclib.Unionpolygon(geolist);
                    final.Add(new ServicePolygonClass { polyGeometry = finalgeo });
                    servicePolyList[i].polyGeometry = finalgeo;
                }

                Arclib.Createshapefile(final, tempfilesPath + "finalwithparcel.shp", false);
            }
            catch (Exception ex)
            {
                throw;
            }
            return servicePolyList;
        }

        private int CheckMultipartPolygon(IPolygon4 polygon)
        {
            int pgeoRingCount = 0;
            try
            {
                IGeometryBag exteriorRingGeometryBag = polygon.ExteriorRingBag;
                IGeometryCollection exteriorRingGeometryCollection = exteriorRingGeometryBag as IGeometryCollection;
                if (exteriorRingGeometryCollection.GeometryCount > 1)
                        pgeoRingCount = exteriorRingGeometryCollection.GeometryCount;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return pgeoRingCount;
        }

        private List<ServicePolygonClass> Excludelanduse(List<ServicePolygonClass> servicePolyList)
        {
            try
            {

                IFeatureClass Landuse = jobTask.GetFCfromSDE(configKey.Default.LandUseTable, jobTask.LoadLanduseProperties());
                List<string> LandUseExcludeFTLst = new List<string>();
                LandUseExcludeFTLst = ((configKey.Default.LanduseExcludeFTs).Split(',')).ToList();
                int FTIndex = Landuse.FindField("FEATTYP");
                List<ServicePolygonClass> final = new List<ServicePolygonClass>();
                for (int i = 0; i < servicePolyList.Count; i++)
                {
                    List<IPolygon> geolist = new List<IPolygon>();
                    IGeometry igeo = servicePolyList[i].polyGeometry;
                    ISpatialFilter spatialFilter = new SpatialFilterClass();
                    spatialFilter.Geometry = igeo;
                    spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                    spatialFilter.GeometryField = Landuse.ShapeFieldName;

                    IFeatureCursor cursor = Landuse.Search(spatialFilter, false);
                    IFeature ifeature = null;
                    ITopologicalOperator topo = igeo as ITopologicalOperator;
                    while ((ifeature = cursor.NextFeature()) != null)
                    {
                        string FTIValue = ifeature.get_Value(FTIndex).ToString();
                        if (!LandUseExcludeFTLst.Contains(FTIValue))
                            continue;
                        IGeometry geome = topo.Difference(ifeature.Shape);
                        topo = geome as ITopologicalOperator;
                        igeo = geome;

                    }
                    final.Add(new ServicePolygonClass { polyGeometry = igeo });
                    servicePolyList[i].polyGeometry = igeo;
                }
                Arclib.Createshapefile(final, tempfilesPath + "finallanduse.shp", false);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return servicePolyList;
        }
        private int TerminalViewCount(string viewname)
        {
            int val = 0;
            try
            {
                DbWorker DB = new DbWorker();
                val = DB.Getintvalue("SELECT COUNT(*) FROM " + viewname);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return val;
        }
      

        /// <summary>
        /// Get FOW2 Roads count 
        /// </summary>
        /// <param name="clippedRoads"></param>
        /// <returns>Fow raods count</returns>
        private static int GetFow2RoadsCount(IFeatureClass clippedRoads)
        {
            IQueryFilter queryFilter = new QueryFilterClass();
            queryFilter.WhereClause = "PolygonID='2'"; //FOW =2
            int fow2CountinRoads = clippedRoads.FeatureCount(queryFilter);
            return fow2CountinRoads;
        }

        /// <summary>
        /// Loading the Service Polygons to SQM and REVIEW table
        /// </summary>
        /// <param name="jobTask"></param>
        /// <param name="servicePolyList"></param>
        /// <param name=" job entity"></param>
        /// 

        private void LoadConstructionstoSQModeL(Jobs jobTask, List<ServicePolygonClass> servicePolyList, JobEntity entity)
        {
            try
            {
                LogManager.WriteLogandConsole("INFO : Loading Output to ODS...");
                // Conncet to QualSchema
                SqlQueryHelper.LoadServerConnection(qualSchemaDBConnection);
                //LogManager.WriteLogandConsole("INFO : QualShema connected..!");
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
                    propertySet.SetProperty(Constants.DBProperties.user, "qual_schema");//"SWGISLOC");
                    propertySet.SetProperty(Constants.DBProperties.password, "cyient#3");//"SWGISLOC");
                    propertySet.SetProperty(Constants.DBProperties.version, "sde.DEFAULT");//"sde.DEFAULT");
                }
                Arclib.InsertUpdateFeaturestoODS(jobTask.GetFCfromSDE(configKey.Default.ServicePolygonOutput, propertySet), servicePolyList);

                //getPolygon IDS
                foreach (ServicePolygonClass item in servicePolyList)
                {
                    string query = "Select OBJECTID from " + configKey.Default.ServicePolygonOutput + " where " +
                        entity.ServicePoly_FIBER_CABLE_ID + " = '" + item.FIBER_CABLE_ID + "' AND " +
                        entity.ServicePoly_NDS_JOB_NO + " = '" + item.NDS_JOB_NO + "' AND " +
                        entity.ServicePoly_SERVIING_WIRE_CENTER_CLLI + " = " + item.SERVIING_WIRE_CENTER_CLLI + " AND " +
                        entity.ServicePoly_STATUS + " = " + jobTask.qualStatus;
                    DbWorker dbw = new DbWorker();
                    Exception ex = null;
                    DataTable dt = dbw.ReadTable(query, out ex);
                    foreach (DataRow row in dt.Rows)
                    {
                        item.POLYGON_ID = Convert.ToInt32(row[0]);
                        break;
                    }
                }
                //Insert into review table..
                jobTask.InsertIntoReviewTable(servicePolyList, entity, false);

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private void LoadtoSQModelandReviewTable(Jobs jobTask, List<ServicePolygonClass> servicePolyList, JobEntity entity)
        {
            try
            {
                LogManager.WriteLogandConsole("INFO : Loading Output to ODS...");
                // Conncet to QualSchema
                SqlQueryHelper.LoadServerConnection(qualSchemaDBConnection);
                Arclib.InsertUpdateFeaturestoODS(jobTask.GetFCfromSDE(configKey.Default.ServicePolygonOutput, jobTask.LoadSDEswgisProperties()), servicePolyList);
                Arclib.InsertFeaturestoWorkFlowTable(jobTask.GetFCfromSDE(configKey.Default.SQM_WORKFLOW, jobTask.LoadSDEswgisProperties()), servicePolyList, jobTask);
                //UpdateSpeedValue(jobTask);//old bandwidth
                //GetPolygonID fromSQM table..
                foreach (ServicePolygonClass item in servicePolyList)
                {
                    string query = "Select OBJECTID from " + configKey.Default.ServicePolygonOutput + " where " +
                        entity.ServicePoly_FIBER_CABLE_ID + " = '" + item.FIBER_CABLE_ID + "' AND " +
                        entity.ServicePoly_SERVIING_WIRE_CENTER_CLLI + " = " + item.SERVIING_WIRE_CENTER_CLLI + " AND " +
                        entity.ServicePoly_STATUS + " = " + jobTask.qualStatus;
                    DbWorker dbw = new DbWorker();
                    Exception ex = null;
                    DataTable dt = dbw.ReadTable(query, out ex);
                    foreach (DataRow row in dt.Rows)
                    {
                        item.POLYGON_ID = Convert.ToInt32(row[0]);
                        break;
                    }
                }
                LogManager.WriteLogandConsole("INFO : Updating Review Table...");
                jobTask.UpdateReviewTable(servicePolyList, entity, false);

            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex); isExecutionResult = false;
            }
        }
        public bool IsTaskcancelled(Jobs job)
        {
            bool iscancelled = false;

            try
            {
                DbWorker worker = new DbWorker();
                int val = worker.Getintvalue("select job_status from job_details where job_id=" + job.jobID + "");
                if (val == (int)JobPolygonStatusProgressEnum.Job_Execution_Cancellation_inprogress) iscancelled = true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return iscancelled;

        }
        public void UpdateSpeedValue(Jobs job)
        {
            try
            {
                DbWorker worker = new DbWorker();
                Exception ex = null;
                DataTable dt = worker.ReadTable("select designation,down_stream from " + configKey.Default.LU_Latlong + " where wc_id=" + job.wireCenterID + " group by designation,down_stream", out ex);
                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        DataTable desigcount = worker.ReadTable("select distinct designation,down_stream from " + configKey.Default.LU_Latlong + " where designation='" + dt.Rows[i][0] + "'", out ex);
                        if (desigcount.Rows.Count > 1)
                        {
                            LogManager.WriteLogandConsole("Error : Unable to Update the multiple speed values for the designation " + dt.Rows[i][0]);
                        }
                        DbWorker e = new DbWorker();
                        int value = e.RunDmlQuery("update " + configKey.Default.ServicePolygonOutput + " set BAND='" + dt.Rows[i][1] + "' WHERE FCBL_ID='" + dt.Rows[i][0] + "'");
                        int value1 = e.RunDmlQuery("update " + configKey.Default.SQM_WORKFLOW + " set BAND='" + dt.Rows[i][1] + "' WHERE FCBL_ID='" + dt.Rows[i][0] + "' and job_id=" + job.jobID + "");
                    }
                }
                DataTable nospeed = worker.ReadTable("select FCBL_ID FROM " + configKey.Default.ServicePolygonOutput + " WHERE BAND IS NULL and WC_NAME='" + job.wireCenterName + "'", out ex);
                for (int J = 0; J < nospeed.Rows.Count; J++)
                {
                    LogManager.WriteLogandConsole("Error : Speed value not present for the designation " + nospeed.Rows[J][0]);
                }
            }

            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex); isExecutionResult = false;
            }

        }

        /// <summary>
        /// CHeck for FOW Column in FeatureClass
        /// </summary>
        /// <param name="roadLinksFeatureClass"></param>
        /// <returns>true/false</returns>
        private bool CheckFOWRoad(IFeatureClass roadLinksFeatureClass)
        {
            int fowIndex = roadLinksFeatureClass.FindField("PolygonID"); //fow
            if (fowIndex < 0)
                return false;

            return true;
        }

    }
}
