using DbLibrary;
using ESRI.ArcGIS.DataManagementTools;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.GeoDatabaseUI;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geoprocessing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static CenturyLink_ArcApp.Arclib;
using static CenturyLink_ArcApp.Program;
using configKey = CenturyLink_ArcApp.Properties.Settings;


namespace CenturyLink_ArcApp
{

    class ServiceBoundary_Process
    {
        public Jobs jobTask;

        public ServiceBoundary_Process(Jobs job)
        {
            jobTask = job;

        }


        /// <summary>
        /// Creates polygon from envolope
        /// </summary>
        /// <param name="envolop"></param>
        /// <returns></returns>
        private IPolygon EnvolopetoPolygon(IEnvelope envolop)
        {
            IPolygon envoloppoly = null;
            try
            {
                IPointCollection ipointc = new PolygonClass();
                object missin = Type.Missing;

                ipointc.AddPoint(envolop.LowerLeft as IPoint, ref missin, ref missin);
                ipointc.AddPoint(envolop.UpperLeft as IPoint, ref missin, ref missin);
                ipointc.AddPoint(envolop.UpperRight as IPoint, ref missin, ref missin);
                ipointc.AddPoint(envolop.LowerRight as IPoint, ref missin, ref missin);
                ipointc.AddPoint(envolop.LowerLeft as IPoint, ref missin, ref missin);
                envoloppoly = ipointc as IPolygon;
            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
            }
            return envoloppoly;
        }
        /// <summary>
        /// exports the feature class from the iselectionset
        /// </summary>
        /// <param name="pDataset"></param>
        /// <param name="pGeometryDef"></param>
        /// <param name="selSet"></param>
        /// <param name="datasetNAme"></param>
        /// <param name="outpath"></param>
        public void ExportFeatureClass(IDataset pDataset, IGeometryDef pGeometryDef, ISelectionSet selSet, string datasetNAme, string outpath)
        {
            try
            {
                IDatasetName pDatasetName = pDataset.FullName as IDatasetName;
                IWorkspaceName pWorkspaceName = new WorkspaceNameClass();
                pWorkspaceName.PathName = outpath;

                // pWorkspaceName.PathName = @"D:\tmp\output.gdb";
                pWorkspaceName.WorkspaceFactoryProgID = "esriDataSourcesFile.ShapefileWorkspaceFactory";
                // pWorkspaceName.WorkspaceFactoryProgID = "esriDataSourcesGDB.FileGDBWorkspaceFactory";
                IDatasetName dsName = new FeatureClassNameClass();
                dsName.Name = datasetNAme;
                dsName.WorkspaceName = pWorkspaceName;
                IFeatureClassName outfcName = dsName as IFeatureClassName;
                IExportOperation exportOp = new ExportOperationClass();
                exportOp.ExportFeatureClass(pDatasetName, null, selSet, pGeometryDef, outfcName, 0);
            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
            }
        }
        /// <summary>
        /// clips the roadnetwork with buffered envolope geoemtry
        /// </summary>
        /// <param name="concavehullFC"></param>
        /// <param name="roads"></param>
        /// <returns></returns>
        public IFeatureClass GetClippedRoandnetwk(IPolygon WCBoundary, IFeatureClass roads)
        {
            IFeatureClass ifc = null;
            try
            {
                // IGeometry polygeometry = Geocollection(concavehullFC);
                //IPolygon envolope = EnvolopetoPolygon(polygeometry.Envelope);
                ITopologicalOperator itopo = WCBoundary as ITopologicalOperator;
                IGeometry buffergeo = itopo.Buffer(0.0025); //250 mt
                ISpatialFilter spatialFilter1 = new SpatialFilterClass();
                spatialFilter1.Geometry = buffergeo;
                spatialFilter1.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                spatialFilter1.GeometryField = roads.ShapeFieldName;

                List<ServicePolygonClass> li = new List<ServicePolygonClass>();
                IFeatureCursor pfCur = roads.Search(spatialFilter1, false);
                IFeature proad = null;
                int fowIndex = roads.FindField("FOW");
                while ((proad = pfCur.NextFeature()) != null)
                {
                    if (fowIndex > 0)
                    {
                        string fowVl = proad.Value[fowIndex].ToString();
                        li.Add(new ServicePolygonClass { polyGeometry = proad.Shape, FIBER_CABLE_ID = fowVl });
                    }
                    else
                        li.Add(new ServicePolygonClass { polyGeometry = proad.Shape });


                }
                Marshal.ReleaseComObject(pfCur);
                if (li.Count > 0)
                {
                    Createshapefile(li, tempfilesPath + "ClippedRN.shp", false);
                    ifc = Getfeatureclass(tempfilesPath + "ClippedRN.shp");
                }



                ////This function creates various SelectionSets on the table and modifies them using the ISelectionSet methods.
                //IDataset dataset = (IDataset)roads;
                ////use the query filter to select features
                //ISelectionSet selectionSet = roads.Select(spatialFilter1, esriSelectionType.esriSelectionTypeHybrid, esriSelectionOption.esriSelectionOptionNormal, dataset.Workspace);
                //int shapeFieldPosition = roads.FindField(roads.ShapeFieldName);
                //IFields inputFields = roads.Fields;
                //IField shapeField = inputFields.get_Field(shapeFieldPosition);
                //IGeometryDef geometryDef = shapeField.GeometryDef;
                //ExportFeatureClass(dataset, geometryDef, selectionSet, "ClippedRN.shp", tempfilesPath);
                //ifc = Getfeatureclass(tempfilesPath + "ClippedRN.shp");

            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
            }
            return ifc;
        }
        public void Addpointcol(IFeatureClass point, IFeatureClass roads, List<ServicePolygonClass> li, bool isconstr)
        {
            IFeatureCursor featureCursor = point.Search(null, false);
            IFeature feature = null;
            int index = point.FindField("PolygonID");
            while ((feature = featureCursor.NextFeature()) != null)
            {
                // if (feature.OID != 27) continue;

                IBufferConstruction pbf = new BufferConstructionClass();
                IGeometry _300BufferPoint = pbf.Buffer(feature.Shape, 0.00091); //300ft

                string desigid = feature.get_Value(index).ToString();
                ISpatialFilter spatialFilter = new SpatialFilterClass();
                spatialFilter.Geometry = feature.Shape;
                spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                spatialFilter.GeometryField = point.ShapeFieldName;
                IFeatureCursor roadcursor = roads.Search(spatialFilter, false);
                IFeature feat = null;
                while ((feat = roadcursor.NextFeature()) != null)
                {                   // li.Add(new CenturyLink_ArcApp.ServicePolygonClass { polyGeometry = feat.Shape, FIBER_CABLE_ID = desigid });
                    ITopologicalOperator ptopo = feat.Shape as ITopologicalOperator;
                    IGeometry pinterGeom = ptopo.Intersect(_300BufferPoint, esriGeometryDimension.esriGeometry2Dimension);

                    IArea parea = feat.Shape as IArea;
                    IArea pgeomarea = pinterGeom as IArea;
                    double intesectedAreaPercent = (pgeomarea.Area / parea.Area) * 100;

                    if (isconstr)
                    {
                        string availdate = string.Empty;
                        string[] Nnum = desigid.Split(':');
                        string type = GettypewithDate(desigid, out availdate);
                        int qulType = jobTask.qualType;
                        if (type == Constants.QualType.strBIW)
                            qulType = Constants.QualType.BIW;
                        if (type == Constants.QualType.strGPON)
                            qulType = Constants.QualType.GPON;
                        if (type == Constants.QualType.strCOPPER)
                            qulType = Constants.QualType.COPPER;
                        if (intesectedAreaPercent > 60)
                            li.Add(new ServicePolygonClass
                            {
                                SERVING_WIRE_CENTER_NAME = jobTask.wireCenterName,
                                TYPE = qulType,
                                FIBER_CABLE_ID = Nnum[0],
                                NDS_JOB_NO = Nnum[1],
                                AVAILABILITY_DATE = availdate,
                                STATUS = Constants.QualStatus.InConstruction,
                                polyGeometry = feat.Shape, //ashok
                                SERVIING_WIRE_CENTER_CLLI = jobTask.wireCenterID,
                                Polygon_Status = (int)JobPolygonStatusProgressEnum.Polygon_Creation_Completed
                            });
                        else
                            li.Add(new ServicePolygonClass
                            {
                                SERVING_WIRE_CENTER_NAME = jobTask.wireCenterName,
                                TYPE = qulType,
                                FIBER_CABLE_ID = Nnum[0],
                                NDS_JOB_NO = Nnum[1],
                                AVAILABILITY_DATE = availdate,
                                STATUS = Constants.QualStatus.InConstruction,
                                polyGeometry = pinterGeom, //ashok
                                SERVIING_WIRE_CENTER_CLLI = jobTask.wireCenterID,
                                Polygon_Status = (int)JobPolygonStatusProgressEnum.Polygon_Creation_Completed
                            });
                    }
                    else
                    {
                        if (intesectedAreaPercent > 60)
                            li.Add(new ServicePolygonClass
                            {
                                SERVING_WIRE_CENTER_NAME = jobTask.wireCenterName,
                                TYPE = jobTask.qualType,
                                FIBER_CABLE_ID = desigid,
                                STATUS = Constants.QualStatus.InService,
                                polyGeometry = feat.Shape, //ashok
                                SERVIING_WIRE_CENTER_CLLI = jobTask.wireCenterID,
                                Polygon_Status = (int)JobPolygonStatusProgressEnum.Polygon_Creation_Completed
                            });
                        else
                            li.Add(new ServicePolygonClass
                            {
                                SERVING_WIRE_CENTER_NAME = jobTask.wireCenterName,
                                TYPE = jobTask.qualType,
                                FIBER_CABLE_ID = desigid,
                                STATUS = Constants.QualStatus.InService,
                                polyGeometry = pinterGeom, //ashok
                                SERVIING_WIRE_CENTER_CLLI = jobTask.wireCenterID,
                                Polygon_Status = (int)JobPolygonStatusProgressEnum.Polygon_Creation_Completed
                            });
                    }
                }
                jobTask.designationLst[desigid] = new List<string> { JobPolygonStatusProgressEnum.designationSuccess.ToString(), "" };
            }
            Marshal.ReleaseComObject(featureCursor);
        }

        /// <summary>
        /// Method to generate Service Polygons
        /// </summary>
        /// <param name="terminals"></param>
        /// <param name="roads"></param>
        /// <param name="servicePolygons"></param>
        /// <param name="RoadswithInWCFC"></param>
        /// <param name="currentJob"></param>
        /// <returns>List of Service Polygons,SP FeatureClas</returns>
        public List<ServicePolygonClass> GenerateServicePolygons(IFeatureClass terminals, IFeatureClass roads_Polygon, IPolygon wcBoundary,
            out IFeatureClass servicePolygons, bool isConstr)
        {
            // jobTask = currentJob;
            List<ServicePolygonClass> li = new List<ServicePolygonClass>();
            servicePolygons = null;
            try
            {
                LogManager.WriteLogandConsole("INFO : Reading Designation wise Inputs...");
                IFeatureClass convexHullFC = null;
                if (isConstr)
                    convexHullFC = CnstrConvex(terminals, jobTask);
                else convexHullFC = Convex(terminals, jobTask);
                if (convexHullFC == null)
                {
                    LogManager.WriteLogandConsole("Warning : Failed to create minimal outer boundaries for the designations..");
                    return li;
                }
                IFeatureCursor featureCursor = convexHullFC.Search(null, false);
                IFeature feature = null;
                LogManager.WriteLogandConsole("INFO : Generating Polygons...");
                int polygonindex = convexHullFC.FindField("PolygonID");
                while ((feature = featureCursor.NextFeature()) != null)
                {
                    string designationid = feature.get_Value(polygonindex).ToString(); //ashok
                    if (designationid == "5095S")
                    {

                    }
                    IPointCollection igeocillection = new PolygonClass();
                    igeocillection.AddPointCollection(feature.Shape as IPointCollection);
                    IGeometry geom = Getbuffergeom(igeocillection as IGeometry);
                    ISpatialFilter spatialFilter = new SpatialFilterClass();
                    spatialFilter.Geometry = geom;
                    spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                    spatialFilter.GeometryField = roads_Polygon.ShapeFieldName;
                    List<IPolygon> segCollection = GetListpolygons(roads_Polygon, spatialFilter);
                    List<IPolygon> newPolylist = new List<IPolygon>();
                    if (segCollection.Count == 0)
                    {
                        jobTask.designationLst[designationid] = new List<string> { JobPolygonStatusProgressEnum.designationFailed.ToString(), Constants.Messages.noRoadsFailureMesage };
                        continue;
                    }
                    if (segCollection.Count == 1) //300 feet rule..
                    {
                        ProcessPolygonWith300ftRule(feature, segCollection, newPolylist);
                    }
                    else
                    {
                        //Ashok Added code for avg approach
                        double avg = 0.0;
                        for (int i = 0; i < segCollection.Count; i++)
                        {
                            IArea parea = segCollection[i] as IArea;
                            double area = avg + parea.Area;
                            avg = area;
                        }
                        avg = avg / segCollection.Count;
                        // int terDegIndex = terminals.FindField("DESIGNATION");
                        for (int i = 0; i < segCollection.Count; i++)
                        {
                            //eliminate road if other terminals are in the road and not current terminals  
                            //if (configKey.Default.Overlap_TerminalsonRoads == "1")
                            //{
                            //    int selfTerminalCnt, otherTerminalCnt;
                            //    GetSelfandOtherTeminalCountOnRoads(terminals, designationid, segCollection[i], out selfTerminalCnt, out otherTerminalCnt);
                            //    if (selfTerminalCnt == 0 && otherTerminalCnt > 0)
                            //        continue;
                            //}
                            IArea parea = segCollection[i] as IArea;
                            double totalRoadPolyGeomArea = parea.Area;
                            if (totalRoadPolyGeomArea < 1.5 * avg)
                            {
                                newPolylist.Add(segCollection[i]);
                                continue;
                            }
                            ITopologicalOperator ptopConvex = feature.Shape as ITopologicalOperator;
                            IGeometry ConvexHUllPolygon = ptopConvex.ConvexHull();
                            IRelationalOperator prelOP = ConvexHUllPolygon as IRelationalOperator;
                            if (prelOP.Contains(segCollection[i]))
                            {
                                newPolylist.Add(segCollection[i]);
                                continue;
                            }
                            IGeometry _300BuffconHull = (ConvexHUllPolygon as ITopologicalOperator).Buffer(0.00091);
                            ITopologicalOperator ptopo = _300BuffconHull as ITopologicalOperator;
                            ptopo.Simplify();
                            try
                            {
                                IGeometry pintersectedGeom = ptopo.Intersect(segCollection[i], esriGeometryDimension.esriGeometry2Dimension);
                                IArea inersectedGeomArea = pintersectedGeom as IArea;
                                double intesectedAreaPercent = (inersectedGeomArea.Area / totalRoadPolyGeomArea) * 100;
                                if (intesectedAreaPercent > 60)
                                    newPolylist.Add(segCollection[i]);
                                else
                                    newPolylist.Add(pintersectedGeom as IPolygon);
                            }
                            catch (Exception)
                            {
                                newPolylist.Add(segCollection[i]);
                                continue;
                            }
                        }
                    }
                    IPolygon unionpoly = Unionpolygon(newPolylist); //ashok
                    if (unionpoly.IsEmpty)
                    {
                        jobTask.designationLst[designationid] = new List<string> { JobPolygonStatusProgressEnum.designationFailed.ToString(), Constants.Messages.PolygonFailureErrorMessage };
                        continue;
                    }
                    //  string desigid = string.Empty;
                    //  List<IPolygon> polycoll = GetintersectedPolygons(newPolylist, unionpoly, terminals, designationid);
                    //  IPolygon unionpoly1 = Unionpolygon(polycoll);
                    //removing innerpolygons 
                    IGeometry igeo = GetPolygonExteriorring((IPolygon4)unionpoly);
                    // withouthole.Add(igeo);                 
                    // li.Add(new GoemList { UniqueID = feature.OID.ToString(), Geometry = igeo });
                    //
                    if (isConstr)
                    {
                        string availdate = string.Empty;
                        string[] Nnum = designationid.Split(':');
                        string type = GettypewithDate(designationid, out availdate);
                        int qulType = jobTask.qualType;
                        if (type == Constants.QualType.strBIW)
                            qulType = Constants.QualType.BIW;
                        if (type == Constants.QualType.strGPON)
                            qulType = Constants.QualType.GPON;
                        if (type == Constants.QualType.strCOPPER)
                            qulType = Constants.QualType.COPPER;
                        li.Add(new ServicePolygonClass
                        {
                            SERVING_WIRE_CENTER_NAME = jobTask.wireCenterName,
                            STATUS = Constants.QualStatus.InConstruction,
                            TYPE = qulType,
                            FIBER_CABLE_ID = Nnum[0],
                            polyGeometry = igeo,
                            AVAILABILITY_DATE = availdate,
                            NDS_JOB_NO = Nnum[1],
                            SERVIING_WIRE_CENTER_CLLI = jobTask.wireCenterID,
                            Polygon_Status = (int)JobPolygonStatusProgressEnum.Polygon_Creation_Completed
                        });
                        ProcessClass.constructionDesignations[Nnum[0]] = "Processed";

                    }
                    else
                    {
                        li.Add(new ServicePolygonClass
                        {
                            SERVING_WIRE_CENTER_NAME = jobTask.wireCenterName,
                            TYPE = jobTask.qualType,
                            STATUS = Constants.QualStatus.InService,
                            FIBER_CABLE_ID = designationid,
                            polyGeometry = igeo,
                            SERVIING_WIRE_CENTER_CLLI = jobTask.wireCenterID,
                            Polygon_Status = (int)JobPolygonStatusProgressEnum.Polygon_Creation_Completed
                        });
                    }
                    //Mark polygon as Success
                    jobTask.designationLst[designationid] = new List<string> { JobPolygonStatusProgressEnum.designationSuccess.ToString(), "" };

                }
                Marshal.ReleaseComObject(featureCursor);
                if (System.IO.Directory.GetFiles(tempfilesPath, "Point.shp").Length != 0)
                {
                    IFeatureClass ipointco = Getfeatureclass(tempfilesPath + "point.shp");
                    Addpointcol(ipointco, roads_Polygon, li, isConstr);
                }
                if (li.Count > 0)
                {
                    Createshapefile(li, tempfilesPath + "FinalOPPoly.shp", true);
                    servicePolygons = Getfeatureclass(tempfilesPath + "FinalOPPoly.shp");
                }
                //geometry Check...
                CheckOverlaps(li, jobTask, isConstr);


            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
                isExecutionResult = false;
                return null;
            }
            return li;
        }

        private static void ProcessPolygonWith300ftRule(IFeature feature, List<IPolygon> segCollection, List<IPolygon> newPolylist)
        {
            ITopologicalOperator ptopConvex = feature.Shape as ITopologicalOperator;
            IGeometry ConvexHUllPolygon = ptopConvex.ConvexHull();
            IBufferConstruction pbf = new BufferConstructionClass();
            IGeometry pbufGeom = pbf.Buffer(ConvexHUllPolygon, 0.00091); //300ft
            ITopologicalOperator ptopCnve = segCollection[0] as ITopologicalOperator;
            ptopCnve.Simplify();
            try
            {
                IGeometry pgeom = ptopCnve.Intersect(pbufGeom, esriGeometryDimension.esriGeometry2Dimension);
                IArea parea = segCollection[0] as IArea;
                IArea pgeomarea = pgeom as IArea;
                double intesectedAreaPercent = (pgeomarea.Area / parea.Area) * 100;
                if (intesectedAreaPercent > 60)
                    newPolylist.Add(segCollection[0]);
                else
                    newPolylist.Add(pgeom as IPolygon);

                //  newPolylist.Add(pgeom as IPolygon);
            }
            catch (Exception)
            {
                newPolylist.Add(segCollection[0] as IPolygon);
            }
        }

        public void ProcessRoadsTogetWCInterectedRoads(IFeatureClass roads, IFeatureClass WCfc,
            out IFeatureClass RoadswithInWCFC, out IFeatureClass roads_Polygon, out IPolygon wcBounday)
        {

            RoadswithInWCFC = null;
            roads_Polygon = null;
            wcBounday = null;
            try
            {
                IFeatureClass requiredWCboundaryFC = GetFilteredWCIDFC(WCfc, jobTask.wireCenterID);
                wcBounday = GetWCPoly(WCfc, jobTask.wireCenterID);
                RoadswithInWCFC = GetClippedRoandnetwk(wcBounday, roads);
                IFeatureClass MergedroadsandWCbound = DoMerge(RoadswithInWCFC, requiredWCboundaryFC);
                roads_Polygon = DoConvertlinetopolygon(MergedroadsandWCbound, "sderoadstopolygon");
            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex); isExecutionResult = false;
            }

        }

        private static void GetSelfandOtherTeminalCountOnRoads(IFeatureClass terminals, string designationid, IPolygon roadPolygon,
            out int selfTerminalCnt, out int otherTerminalCnt)
        {
            ISpatialFilter psppp = new SpatialFilterClass();
            psppp.Geometry = roadPolygon;
            psppp.GeometryField = terminals.ShapeFieldName;
            psppp.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
            psppp.WhereClause = "DESIGNATION = '" + designationid + "'";
            selfTerminalCnt = terminals.FeatureCount(psppp);
            psppp.WhereClause = "DESIGNATION <> '" + designationid + "'";
            otherTerminalCnt = terminals.FeatureCount(psppp);
        }

        private IFeatureClass MergeGp(IFeatureClass roads_Polygon, IFeatureClass requiredWCboundaryFC)
        {
            IFeatureClass opFC = null;
            try
            {
                ESRI.ArcGIS.DataManagementTools.Merge MergeTool = new ESRI.ArcGIS.DataManagementTools.Merge();
                MergeTool.inputs = roads_Polygon + ";" + requiredWCboundaryFC;
                MergeTool.output = tempfilesPath + "mergedWC_Roads.shp";
                gp.Execute(MergeTool, null);
                opFC = Getfeatureclass(tempfilesPath + "mergedWC_Roads.shp");

            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex); isExecutionResult = false;
            }
            return opFC;
        }

        private IFeatureClass GetFilteredWCIDFC(IFeatureClass wCfc, string wireCenterID)
        {
            IFeatureClass ifc = null;
            try
            {
                List<ServicePolygonClass> li = new List<ServicePolygonClass>();
                IQueryFilter pqf = new QueryFilterClass();
                pqf.WhereClause = "ID = " + wireCenterID;
                IFeatureCursor pfCur = wCfc.Search(pqf, false);
                IFeature pf = null;
                while ((pf = pfCur.NextFeature()) != null)
                {
                    ITopologicalOperator itopo = pf.Shape as ITopologicalOperator;
                    IGeometry buffergeo = itopo.Buffer(0.005); //Expanding WC Boundary to 500 mt
                    IPointCollection pcol = new PolylineClass();
                    pcol.AddPointCollection(buffergeo as IPointCollection);
                    IPolyline line = pcol as IPolyline;
                    li.Add(new ServicePolygonClass { polyGeometry = line as IGeometry });
                }

                Createshapefile(li, tempfilesPath + "WCBoundaryLine.shp", false);
                ifc = Getfeatureclass(tempfilesPath + "WCBoundaryLine.shp");
                //  ifc = Getfeatureclass(tempfilesPath + "WCBoundary.shp");

            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
            }
            return ifc;
        }
        private IPolygon GetWCPoly(IFeatureClass wCfc, string wireCenterID)
        {
            IPolygon wcBoundPoly = null;
            try
            {

                IQueryFilter pqf = new QueryFilterClass();
                pqf.WhereClause = "ID = " + wireCenterID;

                IFeatureCursor pfCur = wCfc.Search(pqf, false);
                IFeature pfeat = null;
                while ((pfeat = pfCur.NextFeature()) != null)
                {
                    wcBoundPoly = pfeat.Shape as IPolygon;

                }

            }
            catch (Exception ex)
            {

                LogManager.WriteLogandConsole(ex); isExecutionResult = false; return null;
            }
            return wcBoundPoly;
        }

        /// <summary>
        /// seperating dc from road fc and creating polygons of the dc part only
        /// road fc is the input here
        /// </summary>
        private IFeatureClass Getdcbounds(IFeatureClass roadshape)
        {
            IFeatureClass pOutClass = null;

            try
            {
                //  string dcpolyshapepath = "d://dcpolyshape.shp";
                // IFeatureClass roadshape = Getfeatureclass(@"D:\Dontdel_Suvarnaraju\CentureLink\Streets_AURRCOMA\Streets_AURRCOMA.shp");
                IQueryFilter queryFilter = new QueryFilterClass();
                queryFilter.WhereClause = "PolygonID='2'"; //FOW = 2

                IFeatureCursor cursor = roadshape.Search(queryFilter, false);
                IFeature feat = null;
                object missing = Type.Missing;
                IGeometryCollection coll = new PolylineClass();
                while ((feat = cursor.NextFeature()) != null)
                {
                    coll.AddGeometryCollection(feat.Shape as IGeometryCollection);
                }
                IGeometry igeo = coll as IGeometry;
                ISpatialFilter spatialFilter = new SpatialFilterClass();
                spatialFilter.Geometry = igeo;
                spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                spatialFilter.GeometryField = roadshape.ShapeFieldName;
                IFeatureCursor DCB = roadshape.Search(spatialFilter, false);
                IFeature ft = null;
                List<ServicePolygonClass> li = new List<ServicePolygonClass>();
                while ((ft = DCB.NextFeature()) != null)
                {
                    li.Add(new ServicePolygonClass { polyGeometry = ft.Shape });
                }
                //  IFeatureClass dcLinetoPoly = CreateInMemoryshapefile(li, true);
                //creating line shapefile 
                Createshapefile(li, tempfilesPath + "dcline.shp", false);
                IFeatureClass dcLinetoPoly = Getfeatureclass(tempfilesPath + "dcline.shp");

                pOutClass = DoConvertlinetopolygon(dcLinetoPoly, "dclinetopoly");
            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
                isExecutionResult = false;
            }
            return pOutClass;


        }
        /// <summary>
        /// need to process this method on the final polygons to merge DC with final polygons
        /// 
        /// </summary>
        public IFeatureClass AddDCpolytoFinalboundary(List<ServicePolygonClass> servicePolyList, IFeatureClass pOutputPolygonClass, IFeatureClass RoadsFC)
        {
            IFeatureClass outFC = null;
            try
            {
                //DC Converted shape file
                IFeatureClass Dcboundary = Getdcbounds(RoadsFC);
                //deleting status field to delete updated status
                IFields2 fields = Dcboundary.Fields as IFields2;
                IField2 nameField = null;
                int fieldIndex = Dcboundary.FindField("STATUS");
                if (fieldIndex != -1)
                {
                    nameField = fields.Field[fieldIndex] as IField2;
                    Dcboundary.DeleteField(nameField);
                }
                //adding field
                AddField(Dcboundary);
                //getting cursor
                //IFeatureCursor ifc = pOutputPolygonClass.Search(null, false);
                //IFeature ifeature = null;
                List<IPolygon> li = new List<IPolygon>();
                foreach (var item in servicePolyList)
                {


                    //}
                    //// int installedByFieldIndex = pOutputPolygonClass.FindField("NextField");
                    //// featureBuffer.set_Value(installedByFieldIndex, "K Johnston");
                    //while ((ifeature = ifc.NextFeature()) != null)
                    //{
                    if (li.Count > 0) li.Clear();
                    // if (ifeature.OID== 38&&ifeature.OID==49) continue;
                    ITopologicalOperator topologicalOperator = item.polyGeometry as ITopologicalOperator;
                    //  string val =ifeature.get_Value(installedByFieldIndex).ToString();
                    // if (val == "109") continue;
                    // ifeature.set_Value(installedByFieldIndex, "109");
                    // IGeometry igeometry = topologicalOperator.Buffer(0.0001);
                    ISpatialFilter spatialFilter = new SpatialFilterClass();
                    spatialFilter.Geometry = item.polyGeometry;
                    spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                    spatialFilter.GeometryField = Dcboundary.ShapeFieldName;
                    IFeatureCursor DCB = Dcboundary.Search(spatialFilter, false);
                    IFeature feat = null;
                    int PolygonIndex = Dcboundary.FindField("STATUS");
                    while ((feat = DCB.NextFeature()) != null)
                    {
                        string kal = feat.get_Value(PolygonIndex).ToString();
                        IArea pArea = feat.Shape as IArea;
                        if (pArea.Area > 0.00001)
                            continue;

                        if (kal == "109") continue;
                        IGeometry ig = topologicalOperator.Intersect(feat.Shape as IGeometry, esriGeometryDimension.esriGeometry1Dimension);
                        if (!ig.IsEmpty)
                        {
                            li.Add(feat.Shape as IPolygon);
                            feat.set_Value(PolygonIndex, "109");
                            feat.Store();
                        }
                    }
                    if (li.Count > 0)
                    {
                        li.Add(item.polyGeometry as IPolygon);
                        IPolygon unionpoly = Unionpolygon(li);
                        item.polyGeometry = unionpoly;
                        //ifeature.Shape = unionpoly;
                        //ifeature.Store();
                    }
                }
                outFC = pOutputPolygonClass;
            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
                isExecutionResult = false;
            }
            return outFC;
        }

        private void AddField(IFeatureClass featureclass)
        {
            // Try to acquire an exclusive schema lock.
            //   ISchemaLock schemaLock = (ISchemaLock)featureclass;
            try
            {
                //  schemaLock.ChangeSchemaLock(esriSchemaLock.esriExclusiveSchemaLock);

                // Add your field.
                IFieldEdit2 field = new FieldClass() as IFieldEdit2;
                field.Name_2 = "STATUS";
                field.Type_2 = esriFieldType.esriFieldTypeString;
                field.Length_2 = 50;
                // field.DefaultValue_2 = "Parcel";
                featureclass.AddField(field);

            }
            catch (COMException comExc)
            {
                LogManager.WriteLogandConsole(comExc.Message);
                throw comExc;
                // Handle the exception appropriately for the application.
            }
            finally
            {
                // Demote the exclusive lock to a shared lock.
                // schemaLock.ChangeSchemaLock(esriSchemaLock.esriSharedSchemaLock);
            }

        }

        public void GetBoundaryfromConvex(IFeatureClass featureClass, IFeatureClass Ifc1)
        {
            //IFeatureClass featureClass = Getfeatureclass(@"D:\Dontdel_Suvarnaraju\convexhull\toconvex.shp");
            // updatefieldsWithCursor(featureClass);
            List<string> uniqueids = DataStatistics(featureClass, "DESIGNATION");
            // IFeatureClass Ifc1 = Getfeatureclass(@"D:\Roads\Roads_FeatureToPolygon.shp");
            List<ServicePolygonClass> li = new List<ServicePolygonClass>();

            for (int i = 0; i < uniqueids.Count; i++)
            {
                IQueryFilter queryFilter = new QueryFilterClass();
                queryFilter.WhereClause = "DESIGNATION='" + uniqueids[i] + "'";
                IFeatureCursor featureCursor = featureClass.Search(queryFilter, false);
                IFeature feature = null;
                IPointCollection pointCollection = new PolygonClass();
                object missing = Type.Missing;
                while ((feature = featureCursor.NextFeature()) != null)
                {
                    pointCollection.AddPoint(feature.Shape as IPoint, ref missing, ref missing);
                }
                featureCursor.Flush();
                if (pointCollection.PointCount < 3) continue;
                ITopologicalOperator topologicalOperator = pointCollection as ITopologicalOperator;
                //Outside buffer
                IGeometry polygon = topologicalOperator.ConvexHull();
                ISpatialFilter spatialFilter = new SpatialFilterClass();
                spatialFilter.Geometry = polygon;
                spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                spatialFilter.GeometryField = Ifc1.ShapeFieldName;
                //ISegmentCollection segCollection = Getsegmentcollection(Ifc1, spatialFilter);
                //IGeometryCollection segCollection1 = Getgeomcollection(Ifc1, spatialFilter);
                List<IPolygon> segCollection = GetListpolygons(Ifc1, spatialFilter);
                //BufferMakeBoundarywithConcave
                //topologicalOperator = polygon as ITopologicalOperator;
                //IGeometry buffergeo = topologicalOperator.Buffer(0.00095);
                //Removing overlapped polygons
                // List<IPolygon> touchedpoly = Getintersectingpolygon(segCollection, pointCollection);
                IPolygon unionpoly = Unionpolygon(segCollection);
                //removing innerpolygons 
                IGeometry igeo = GetPolygonExteriorring((IPolygon4)unionpoly);
                // withouthole.Add(igeo);                 
                li.Add(new ServicePolygonClass { FIBER_CABLE_ID = uniqueids[i].ToString(), polyGeometry = (IPolygon)igeo });
            }
            //creating shapefiles
            Createshapefile(li, @"D:\CenturyOutput\Finalboundary.shp", false);


        }
        /// <summary>
        /// careating concave
        /// 
        /// </summary>
        //public IFeatureClass Concave(IFeatureClass terminals)
        //{
        //    //  string concavehullpoly = @"D:\CenturyOutput\concavepoly123.shp";
        //    IFeatureClass concavehullpoly = null;
        //    List<ServicePolygonClass> li = new List<ServicePolygonClass>();
        //    List<ServicePolygonClass> lipoint = new List<ServicePolygonClass>();           
        //    try
        //    {
        //       // IFeatureClass featureClass = Getfeatureclass(@"D:\Terminal_ODC\TerminalPointShapeFile_ODC.shp");
        //        List<string> uniqueids = DataStatistics(terminals, "DESIGNATION");
        //        jobTask.uniqueDeignationsFromTerminals = uniqueids.Count;
        //        LogManager.WriteLogandConsole("INFO : Designations Identified from Teminals to process : " + uniqueids.Count);
        //     //   IFeatureClass Ifc1 = Getfeatureclass(@"D:\Roads\Roads_FeatureToPolygon.shp");
        //        for (int i = 0; i < uniqueids.Count; i++)
        //        {
        //            IQueryFilter queryFilter = new QueryFilterClass();
        //            queryFilter.WhereClause = "DESIGNATION='" + uniqueids[i] + "'";
        //            IFeatureCursor featureCursor = terminals.Search(queryFilter, false);
        //            IFeature feature = null;
        //            IPointCollection pointCollection = new PolygonClass();
        //            object missing = Type.Missing;
        //            List<PointF> ips = new List<PointF>();
        //            IPoint ip = null;
        //            while ((feature = featureCursor.NextFeature()) != null)
        //            {
        //                pointCollection.AddPoint(feature.Shape as IPoint, ref missing, ref missing);
        //                ip = feature.Shape as IPoint;
        //                PointF pf = new PointF();
        //                pf.X = (float)ip.X; pf.Y = (float)ip.Y;
        //                ips.Add(pf);
        //            }
        //            //featureCursor.Flush();
        //            if (pointCollection.PointCount < 2)
        //            {
        //                lipoint.Add(new ServicePolygonClass { polyGeometry = ip, FIBER_CABLE_ID = uniqueids[i] });
        //                continue;
        //            }


        //            AlphaShape css = new AlphaShape(ips, 0.00165f);
        //            List<Edge> edges = css.BorderEdges;
        //            if (edges.Count == 0)
        //            {
        //                li.Add(new ServicePolygonClass { polyGeometry = (IPolyline)pointCollection, FIBER_CABLE_ID = uniqueids[i] });
        //                continue;
        //            }
        //            IGeometryCollection igeocillection = new PolylineClass();
        //            for (int m = 0; m < edges.Count; m++)
        //            {
        //                IPointCollection ipo = new PolylineClass();
        //                ipo.AddPoint(edges[m].A, ref missing, ref missing);
        //                ipo.AddPoint(edges[m].B, ref missing, ref missing);
        //                // ipo.AddPoint(edges[m].A, ref missing, ref missing);
        //                igeocillection.AddGeometryCollection(ipo as IGeometryCollection);
        //            }

        //            li.Add(new ServicePolygonClass { polyGeometry = (IPolyline)igeocillection, FIBER_CABLE_ID = uniqueids[i] });
        //        }

        //      //  IFeatureClass cCave = CreateInMemoryshapefile(li,true);

        //        Createshapefile(li, tempfilesPath+"concav1655.shp",false);
        //        Createshapefile(lipoint, tempfilesPath + "concav1655.shp", false);

        //        // DeleteduplicateGeom(tempfilesPath + "concav1655.shp");
        //        concavehullpoly = Getfeatureclass(tempfilesPath+"concav1655.shp");              
        //       // concavehullpoly = DoConvertlinetopolygon(cCave,"cctopolygon");
        //    }
        //    catch (Exception ex)
        //    {
        //        LogManager.WriteLogandConsole(ex);
        //        isExecutionResult = false;
        //        return null;
        //    }
        //    return concavehullpoly;
        //}
        public IFeatureClass CnstrConvex(IFeatureClass featureClass, Jobs currentJob)
        {
            List<string> uniqueids = DataStatistics(featureClass, "DESIGNATION");
            LogManager.WriteLogandConsole("INFO : Generating minimal possible outer boundaries with inputs...");
            List<ServicePolygonClass> li = new List<ServicePolygonClass>();
            List<ServicePolygonClass> lipoint = new List<ServicePolygonClass>();
            IFeatureClass concavehullpoly = null;
            try
            {
                foreach (var item in uniqueids)  // for (int i = 0; i < uniqueids.Count; i++)
                {
                    currentJob.designationLst.Add(item, new List<string> { JobPolygonStatusProgressEnum.deignationCreated.ToString(), "" });
                    string[] desigSplit = item.Split(':');
                    if (!ProcessClass.constructionDesignations.ContainsKey(desigSplit[0]))
                        ProcessClass.constructionDesignations.Add(desigSplit[0], "Recieved");
                    try
                    {
                        //  if (item.ToString() != "LD1101K") continue;                       
                        IQueryFilter queryFilter = new QueryFilterClass();
                        queryFilter.WhereClause = "DESIGNATION='" + item + "'";
                        if (item.StartsWith(":")) continue;
                        IFeatureCursor featureCursor = featureClass.Search(queryFilter, false);
                        IFeature feature = null;
                        IPointCollection pointCollection = new PolygonClass();
                        object missing = Type.Missing;
                        while ((feature = featureCursor.NextFeature()) != null)
                        {
                            pointCollection.AddPoint(feature.Shape as IPoint, ref missing, ref missing);
                        }
                        // featureCursor.Flush();
                        if (pointCollection.PointCount == 0)
                        {
                            continue;
                        }
                        if (pointCollection.PointCount == 2)
                        {
                            pointCollection.AddPoint(pointCollection.get_Point(0));
                            IPolygon ipoly = pointCollection as IPolygon;
                            IEnvelope envlope = ipoly.Envelope;
                            IArea area = envlope as IArea;
                            if (area.Area > 0.000100)
                                continue;
                            li.Add(new ServicePolygonClass { FIBER_CABLE_ID = item.ToString(), polyGeometry = ipoly });
                            continue;
                        }
                        if (pointCollection.PointCount == 1)
                        {

                            lipoint.Add(new ServicePolygonClass { FIBER_CABLE_ID = item.ToString(), polyGeometry = pointCollection.get_Point(0) });
                            continue;
                        }
                        ITopologicalOperator topologicalOperator = pointCollection as ITopologicalOperator;
                        //Outside buffer
                        IGeometry polygon = topologicalOperator.ConvexHull();
                        IEnvelope env = polygon.Envelope;
                        IArea are = env as IArea;
                        if (are.Area > 0.000100) continue;
                        if (polygon.GeometryType == esriGeometryType.esriGeometryPoint)
                        {
                            polygon = pointCollection as IPolygon;
                        }
                        if (polygon.GeometryType == esriGeometryType.esriGeometryPolyline)
                        {
                            polygon = pointCollection as IPolygon;
                        }

                        li.Add(new ServicePolygonClass { FIBER_CABLE_ID = item.ToString(), polyGeometry = (IGeometry)polygon });
                    }
                    catch (Exception ex)
                    {
                        LogManager.WriteLogandConsole(ex);
                        isExecutionResult = false;
                    }
                }
                //creating shapefiles
                Createshapefile(li, tempfilesPath + "convex.shp", false);
                if (lipoint.Count > 0)
                    Createshapefile(lipoint, tempfilesPath + "point.shp", false);
                concavehullpoly = Getfeatureclass(tempfilesPath + "convex.shp");
            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
                isExecutionResult = false;
                return null;
            }
            return concavehullpoly;

        }
        public string GettypewithDate(string designation, out string availdate)
        {
            string type = string.Empty;
            try
            {
                DbWorker worker = new DbWorker();
                availdate = worker.Getstringvalue("select first_occupancy_tgt_dt from SWGISLOC.ConstructionView_" + jobTask.jobID + " where designation='" + designation + "' and rownum<2");
                string defaultdate = "08-01-2020";
                DateTime d1 = DateTime.Parse(availdate);
                DateTime d2 = DateTime.Parse(defaultdate);
                if (availdate != "")
                {
                    if (d1 <= d2)
                    {
                        type = "GPON";
                    }
                    else { type = "BIW"; }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return type;
        }

        public IFeatureClass Convex(IFeatureClass featureClass, Jobs currentJob)
        {
            // List<string> uniqueids = DataStatistics(featureClass, "DESIGNATION");
            LogManager.WriteLogandConsole("INFO : Generating minimal possible outer boundaries with inputs...");
            List<ServicePolygonClass> li = new List<ServicePolygonClass>();
            List<ServicePolygonClass> lipoint = new List<ServicePolygonClass>();
            IFeatureClass concavehullpoly = null;
            try
            {
                foreach (var item in currentJob.designationLst)  // for (int i = 0; i < uniqueids.Count; i++)
                {
                    try
                    {

                        IQueryFilter queryFilter = new QueryFilterClass();
                        queryFilter.WhereClause = "DESIGNATION='" + item.Key + "'";
                        IFeatureCursor featureCursor = featureClass.Search(queryFilter, false);
                        IFeature feature = null;
                        IPointCollection pointCollection = new PolygonClass();
                        object missing = Type.Missing;
                        while ((feature = featureCursor.NextFeature()) != null)
                        {
                            pointCollection.AddPoint(feature.Shape as IPoint, ref missing, ref missing);
                        }
                        // featureCursor.Flush();
                        if (pointCollection.PointCount == 0)
                        {
                            continue;
                        }
                        if (pointCollection.PointCount == 2)
                        {
                            pointCollection.AddPoint(pointCollection.get_Point(0));
                            IPolygon ipoly = pointCollection as IPolygon;
                            IEnvelope envlope = ipoly.Envelope;
                            IArea area = envlope as IArea;
                            if (area.Area > 0.000100)
                                continue;
                            li.Add(new ServicePolygonClass { FIBER_CABLE_ID = item.Key.ToString(), polyGeometry = ipoly });
                            continue;
                        }
                        if (pointCollection.PointCount == 1)
                        {

                            lipoint.Add(new ServicePolygonClass { FIBER_CABLE_ID = item.Key.ToString(), polyGeometry = pointCollection.get_Point(0) });
                            continue;
                        }
                        ITopologicalOperator topologicalOperator = pointCollection as ITopologicalOperator;
                        //Outside buffer
                        IGeometry polygon = topologicalOperator.ConvexHull();
                        IEnvelope env = polygon.Envelope;
                        IArea are = env as IArea;
                        if (are.Area > 0.000100) continue;
                        if (polygon.GeometryType == esriGeometryType.esriGeometryPoint)
                        {
                            polygon = pointCollection as IPolygon;
                        }
                        if (polygon.GeometryType == esriGeometryType.esriGeometryPolyline)
                        {
                            polygon = pointCollection as IPolygon;
                        }

                        li.Add(new ServicePolygonClass { FIBER_CABLE_ID = item.Key.ToString(), polyGeometry = (IGeometry)polygon });
                    }
                    catch (Exception ex)
                    {
                        LogManager.WriteLogandConsole(ex);
                        isExecutionResult = false;
                    }
                }
                //creating shapefiles
                if (li.Count > 0)
                    Createshapefile(li, tempfilesPath + "convex.shp", false);
                if (lipoint.Count > 0)
                    Createshapefile(lipoint, tempfilesPath + "point.shp", false);
                concavehullpoly = Getfeatureclass(tempfilesPath + "convex.shp");
            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
                isExecutionResult = false;
                return null;
            }
            return concavehullpoly;

        }


        public IFeatureClass Concave(IFeatureClass terminals, Jobs currentJob, IPolygon wcBoundary)
        {
            IFeatureClass concavehullpoly = null;
            List<ServicePolygonClass> li = new List<ServicePolygonClass>();
            List<ServicePolygonClass> lipoint = new List<ServicePolygonClass>();
            try
            {
                // IFeatureClass featureClass = Getfeatureclass(@"D:\Terminal_ODC\TerminalPointShapeFile_ODC.shp");
                //List<string> uniqueids = currentJob.designationLst // DataStatistics(terminals, "DESIGNATION");               
                LogManager.WriteLogandConsole("INFO : Generating mimimal possible outer boundaries with inputs...");


                //List<string> excludeDesg = new List<string>();                    
                //ISpatialFilter ppfilter = new SpatialFilterClass();
                //ppfilter.Geometry = wcBoundary;
                //ppfilter.GeometryField = terminals.ShapeFieldName;
                //ppfilter.SpatialRelDescription = "FF*FF****";
                //IFeatureCursor pfccc = terminals.Search(ppfilter, false);
                //IFeature pexFeat = null;
                //int desgIndex = terminals.FindField("DESIGNATION");
                //while ((pexFeat = pfccc.NextFeature())!=null)
                //{
                //    string desgVal = pexFeat.Value[desgIndex].ToString();
                //    if(!excludeDesg.Contains(desgVal))
                //        excludeDesg.Add(desgVal);
                //}
                //    Marshal.ReleaseComObject(pfccc);


                foreach (var item in currentJob.designationLst)
                {
                    //if (excludeDesg.Contains(item.Key)) // mark designation as failed in log..
                    //    continue;

                    //if (item.Key == "LD10201W26")
                    // continue; //ASHOK
                    IQueryFilter queryFilter = new QueryFilterClass();
                    queryFilter.WhereClause = "DESIGNATION='" + item.Key + "'";
                    IFeatureCursor featureCursor = terminals.Search(queryFilter, false);
                    IFeature feature = null;
                    IPointCollection pointCollection = new PolylineClass();
                    object missing = Type.Missing;
                    List<PointF> ips = new List<PointF>();
                    IPoint ip = null;
                    while ((feature = featureCursor.NextFeature()) != null)
                    {
                        pointCollection.AddPoint(feature.Shape as IPoint, ref missing, ref missing);
                        ip = feature.Shape as IPoint;
                        PointF pf = new PointF();
                        pf.X = (float)ip.X; pf.Y = (float)ip.Y;
                        ips.Add(pf);
                    }
                    if (pointCollection.PointCount == 0)
                    {
                        // currentJob.designationLst[item.Key] = new List<string> { JobPolygonStatusProgressEnum.designationFailed.ToString(), Constants.Messages.noGeom4DesignationErrorMessage };
                        continue;
                    }
                    if (pointCollection.PointCount == 1)
                    {
                        lipoint.Add(new ServicePolygonClass { polyGeometry = ip, FIBER_CABLE_ID = item.Key });
                        continue;
                    }
                    AlphaShape css = new AlphaShape(ips, 0.00165f);// AlphaShape css = new AlphaShape(ips, 0.00165f);
                    List<Edge> edges = css.BorderEdges;
                    if (edges.Count == 0)
                    {
                        IPolyline hull1 = (IPolyline)pointCollection;
                        IEnvelope env1 = hull1.Envelope; IArea are1 = env1 as IArea;
                        if (are1.Area > Convert.ToDouble(configKey.Default.FiberBufferArea)) continue;
                        li.Add(new ServicePolygonClass { polyGeometry = (IPolyline)pointCollection, FIBER_CABLE_ID = item.Key });
                        continue;
                    }
                    IGeometryCollection igeocillection = new PolylineClass();
                    for (int m = 0; m < edges.Count; m++)
                    {
                        IPointCollection ipo = new PolylineClass();
                        ipo.AddPoint(edges[m].A, ref missing, ref missing);
                        ipo.AddPoint(edges[m].B, ref missing, ref missing);
                        // ipo.AddPoint(edges[m].A, ref missing, ref missing);
                        igeocillection.AddGeometryCollection(ipo as IGeometryCollection);
                    }

                    IPolyline hull = (IPolyline)igeocillection;
                    IEnvelope env = hull.Envelope;
                    IArea are = env as IArea;
                    if (are.Area > Convert.ToDouble(configKey.Default.FiberBufferArea)) continue; //100 sqDec
                    li.Add(new ServicePolygonClass { polyGeometry = (IPolyline)igeocillection, FIBER_CABLE_ID = item.Key });
                }

                Createshapefile(li, tempfilesPath + "concav1655.shp", false);
                if (lipoint.Count > 0)
                    Createshapefile(lipoint, tempfilesPath + "point.shp", false);

                //  Repairgeom(tempfilesPath + "concav1655.shp");
                // DeleteduplicateGeom(tempfilesPath + "concav1655.shp");
                //IFeatureClass cCave = Getfeatureclass(tempfilesPath + "concav1655.shp");
                //concavehullpoly = DoConvertlinetopolygon(cCave, "cctopolygon");
                concavehullpoly = Getfeatureclass(tempfilesPath + "concav1655.shp");

            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
                isExecutionResult = false;
                return null;
            }
            return concavehullpoly;
        }

        /// <summary>
        /// finding overlaps
        /// </summary>
        private void Findoverlaps()
        {
            try
            {
                List<ServicePolygonClass> ext = new List<ServicePolygonClass>(); List<ServicePolygonClass> union = new List<ServicePolygonClass>();
                // DeleteduplicateGeom(@"D:\CenturyOutput\Finalconcavebf.shp");
                IFeatureClass featureClass = Getfeatureclass(@"D:\CenturyOutput\Finalconcavebf.shp");
                IFeatureClass parcelpoly = Getfeatureclass(@"D:\CenturyOutput\parcel\parcel.shp");
                //INTERSECTED DATA
                //IFeatureClass bufferpoly = Getfeatureclass(@"D:\CenturyOutput\intersecdata.shp");
                IQueryFilter queryFilter = new QueryFilterClass();
                // queryFilter.WhereClause = null;
                IFeatureCursor featureCursor = featureClass.Search(queryFilter, false);
                IFeature feature = null;
                while ((feature = featureCursor.NextFeature()) != null)
                {
                    // if (feature.OID != 27) continue;
                    ISpatialFilter spatialFilter = new SpatialFilterClass();
                    spatialFilter.Geometry = feature.Shape;
                    spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                    spatialFilter.GeometryField = featureClass.ShapeFieldName;
                    IFeatureCursor cursor1 = featureClass.Search(spatialFilter, false);
                    IFeature pfeature = null;
                    ITopologicalOperator topologicalOperator = feature.Shape as ITopologicalOperator;
                    //IGeometry buffergeo = topologicalOperator.Buffer(0.00095);
                    while ((pfeature = cursor1.NextFeature()) != null)
                    {
                        IGeometry igeo = topologicalOperator.Intersect(pfeature.Shape, esriGeometryDimension.esriGeometry2Dimension);
                        IArea ia = igeo as IArea;
                        double value = ia.Area * 100000;
                        if (value > 0)
                        {
                            if (feature.OID != pfeature.OID)
                            {
                                //GoemList geo = new GoemList { Geometry = pfeature.Shape, UniqueID = pfeature.OID.ToString() };
                                //GoemList geo1 = new GoemList { Geometry = feature.Shape, UniqueID = feature.OID.ToString() };
                                //if (!ext.Contains(geo))
                                //    ext.Add(geo);
                                //if (!ext.Contains(geo1))
                                //    ext.Add(geo1);
                                //finding overlaps
                                ServicePolygonClass igeos = new ServicePolygonClass { polyGeometry = igeo as IPolygon, FIBER_CABLE_ID = feature.OID.ToString() + "," + pfeature.OID.ToString() }; union.Add(igeos);
                                //ISpatialFilter sp1 = new SpatialFilterClass();
                                //sp1.Geometry = igeo;
                                //sp1.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                                //sp1.GeometryField = bufferpoly.ShapeFieldName;
                                //IFeatureCursor cursor2 = bufferpoly.Search(sp1, false);
                                //object missing = Type.Missing;
                                //IFeature pf = null;
                                //while ((pf = cursor2.NextFeature()) != null)
                                //{
                                //    // ext.Add(new GoemList { Geometry = pf.Shape, UniqueID = pf.OID.ToString() });
                                //    List<IGeometry> ss = GetParcels(parcelpoly, igeo);
                                //    IGeometry ss1 = GetIntersectedParcels(ss, pf.Shape);
                                //    if (!ss1.IsEmpty)
                                //    {
                                //        IEnvelope envelope = ss1.Envelope as IEnvelope;
                                //        IPoint ip = new PointClass(); IPoint ip1 = new PointClass();
                                //        ip.X = (envelope.UpperLeft.X + envelope.UpperRight.X) / 2;
                                //        ip1.X = (envelope.LowerLeft.X + envelope.LowerRight.X) / 2;
                                //        ip.Y = (envelope.UpperLeft.Y + envelope.UpperRight.Y) / 2;
                                //        ip1.Y = (envelope.LowerLeft.Y + envelope.LowerRight.Y) / 2;

                                //        IPointCollection ipo = new PolygonClass();
                                //        ipo.AddPoint(ip, ref missing, ref missing);
                                //        ipo.AddPoint(ip1, ref missing, ref missing);
                                //        ipo.AddPoint(ip, ref missing, ref missing);
                                //        ext.Add(new GoemList { Geometry = (IGeometry)ipo, UniqueID = "raju" });
                                //    }
                                //}
                            }
                        }
                    }
                    cursor1.Flush();
                    Marshal.ReleaseComObject(cursor1);
                }
                featureCursor.Flush();
                Marshal.ReleaseComObject(featureCursor);
                // Createshapefile(ext, @"D:\CenturyOutput\Overlappedline.shp");
                Createshapefile(union, @"D:\CenturyOutput\ovelapconcave1.shp", false);
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        /// <summary>
        /// generating boundary with concavehull
        /// </summary>
        /// <param name="Concavehullpath"></param>
        /// <param name="Roadpoly"></param>
        /// <param name="outputpath"></param>

        private List<IPolygon> GetintersectedPolygons(List<IPolygon> polygonlist, IPolygon unionpoly, IFeatureClass fcterminal, string designationid)
        {
            List<IPolygon> list = new List<IPolygon>();
            // designationid = string.Empty;
            try
            {
                // IFeatureClass fcterminal = Getfeatureclass(@"D:\Dontdel_Suvarnaraju\convexhull\toconvex.shp");
                ISpatialFilter spatialFilter = new SpatialFilterClass();
                spatialFilter.Geometry = unionpoly;
                spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                spatialFilter.GeometryField = fcterminal.ShapeFieldName;
                IFeatureCursor ifc = fcterminal.Search(spatialFilter, false);
                IFeature feature = null;
                List<ServicePolygonClass> pointcollec = new List<ServicePolygonClass>();
                int index = fcterminal.FindField("DESIGNATION");
                string id = string.Empty;
                while ((feature = ifc.NextFeature()) != null)
                {
                    id = feature.get_Value(index).ToString();
                    // IsPolygonIntersected(polygonlist, feature as IPoint);
                    pointcollec.Add(new ServicePolygonClass { polyGeometry = feature.Shape as IGeometry, FIBER_CABLE_ID = id });
                }
                //  designationid = GetmajorTerminal(pointcollec);
                if (pointcollec.Count != 0)
                    list = Getintersectingpolygon(polygonlist, pointcollec, designationid);


            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
                isExecutionResult = false;
                return null;
            }
            return list;
        }

        /// <summary>
        /// creataing hull for the fiber cables
        /// </summary>
        public void CreatefiberHull()
        {

            IFeatureClass featureClass = Getfeatureclass(@"D:\Dontdel_Suvarnaraju\2020-01-29 - Data set\Fiber_Aerial_Route_Gather.shp");

            List<string> uniqueids = DataStatistics(featureClass, "CABLE_ID_S");

            IFeatureClass Ifc1 = Getfeatureclass(@"D:\Roads\Roads_FeatureToPolygon.shp");

            List<ServicePolygonClass> li = new List<ServicePolygonClass>();
            for (int i = 0; i < uniqueids.Count; i++)
            {

                IQueryFilter queryFilter = new QueryFilterClass();
                queryFilter.WhereClause = "CABLE_ID_S='" + uniqueids[i] + "'";
                IFeatureCursor featureCursor = featureClass.Search(queryFilter, false);
                IFeature feature = null;
                ISegmentCollection segCollection = new PolygonClass() as ISegmentCollection;
                object missing = Type.Missing;
                while ((feature = featureCursor.NextFeature()) != null)
                {
                    //ISegmentCollection pseg = feature.Shape as ISegmentCollection;
                    segCollection.AddSegmentCollection(feature.Shape as ISegmentCollection);
                }
                featureCursor.Flush();
                IGeometry geom = segCollection as IGeometry;
                ITopologicalOperator topologicalOperator = geom as ITopologicalOperator;
                //Outside buffer
                IGeometry polygon = topologicalOperator.ConvexHull();
                li.Add(new ServicePolygonClass { FIBER_CABLE_ID = uniqueids[i].ToString(), polyGeometry = (IPolygon)polygon });

            }
            //creating shapefiles
            Createshapefile(li, @"D:\CenturyOutput\asddsasad.shp", false);

        }
        /// <summary>
        /// getting parcels with in the intersecting part
        /// </summary>
        /// <param name="fc"></param>
        /// <param name="overlap"></param>
        /// <returns></returns>
        private List<IGeometry> GetParcels(IFeatureClass fc, IGeometry overlap)
        {
            List<IGeometry> igeolist = new List<IGeometry>();
            ISpatialFilter sp1 = new SpatialFilterClass();
            sp1.Geometry = overlap;
            sp1.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
            sp1.GeometryField = fc.ShapeFieldName;
            IFeatureCursor cursor2 = fc.Search(sp1, false);
            IFeature pf = null;

            while ((pf = cursor2.NextFeature()) != null)
            {
                igeolist.Add(pf.Shape);
            }
            cursor2.Flush();
            Marshal.ReleaseComObject(cursor2);

            return igeolist;
        }
        /// <summary>
        /// getting parcels at the intersecting location
        /// </summary>
        /// <param name="Parcels"></param>
        /// <param name="geo"></param>
        /// <returns></returns>
        private IGeometry GetIntersectedParcels(List<IGeometry> Parcels, IGeometry geo)
        {
            IGeometryCollection geomBag = new GeometryBagClass();
            object missing = Type.Missing;
            ITopologicalOperator itopo = geo as ITopologicalOperator;
            for (int i = 0; i < Parcels.Count; i++)
            {
                IGeometry igeo = itopo.Intersect(Parcels[i], esriGeometryDimension.esriGeometry2Dimension);
                IArea ia = igeo as IArea;
                double value = ia.Area * 100000;
                if (value > 0)
                {
                    geomBag.AddGeometry(igeo, ref missing, ref missing);
                }
            }
            return (IGeometry)geomBag;

        }

        private string GetmajorTerminal(List<ServicePolygonClass> collec)
        {
            string value = string.Empty;
            try
            {

                var distinct = collec.GroupBy(l => l.FIBER_CABLE_ID).Select(g =>
                new { Date = g.Key, name = g.Select(l => l.FIBER_CABLE_ID).Count() });
                value = distinct.OrderByDescending(s => s.name).First().Date;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return value;

        }


        /// <summary>
        /// getting feature class from SDE
        /// </summary>
        /// <param name="fCName"></param>
        /// <returns></returns>

        /// <summary>
        /// Merging the feature polygons into one geometry
        /// </summary>
        /// <param name="fcfeaturepoly"></param>
        /// <returns></returns>
        private IGeometry Geocollection(IFeatureClass fcfeaturepoly)
        {
            IGeometry igeo = null;
            try
            {
                IFeatureCursor cursor = fcfeaturepoly.Search(null, false);
                IFeature feat = null;
                IGeometryCollection coll = new PolylineClass();
                while ((feat = cursor.NextFeature()) != null)
                {
                    coll.AddGeometryCollection(feat.Shape as IGeometryCollection);
                }
                igeo = coll as IGeometry;
                cursor.Flush();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return igeo;
        }
        /// <summary>
        /// processing no build and inserting
        /// </summary>
        public List<NoBuild> NoBuild(IFeatureClass fcParcel, IFeatureClass fcLND_Address, IFeatureClass RoadsPolygons, Jobs currentJob)
        {
            IFeatureClass noBuildFC = null;
            List<NoBuild> resultNobuildLst = null;
            try
            {
                //IFeatureClass FcParcels = jobTask.GetFCfromSDE("SWGISLOC.PARCELS", jobTask.LoadSDEswgisProperties()); ///GetFC("SWGISLOC.PARCELS");
                // IFeatureClass FcPoint =  jobTask.GetFCfromSDE("SWGISLOC.GIS_LOCAL_LND_ADDRESS_COPY", jobTask.LoadSDEswgisProperties()); //GetFC("SWGISLOC.GIS_LOCAL_LND_ADDRESS_COPY");
                //  IFeatureClass FcPolyout = jobTask.GetFCfromSDE(configKey.Default.ServicePolygonOutput, jobTask.LoadSDEswgisProperties());// GetFC("QUAL_SCHEMA.SERVICE_POLYGON_OUTPUT");
                // IGeometry polygeometry = Geocollection(fcSQM_Polygons);
                IQueryFilter queryfilter = new QueryFilterClass();
                queryfilter.WhereClause = "CL_HOUSE_NAME ='No Build' and CITY_NAME = '" + currentJob.wireCenterName.ToUpper() + "'";
                IFeatureCursor pntcursor = fcLND_Address.Search(queryfilter, false);
                IFeatureCursor parcelcursor = null;
                IFeature feature = null;
                int ind = fcLND_Address.FindField("ID");
                List<NoBuild> noBuildGeomList = new List<NoBuild>();
                ITopologicalOperator itopo = null;
                while ((feature = pntcursor.NextFeature()) != null)
                {
                    int valu = Convert.ToInt32(feature.get_Value(ind));
                    //if (valu != "506329847") continue;
                    ISpatialFilter spatialFilter = new SpatialFilterClass();
                    spatialFilter.Geometry = feature.Shape;
                    spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                    spatialFilter.GeometryField = fcParcel.ShapeFieldName;
                    parcelcursor = fcParcel.Search(spatialFilter, false);
                    IFeature ift = null;
                    int sum = 0;
                    //vinay
                    int co = parcelcursor.Fields.FieldCount;
                    while ((ift = parcelcursor.NextFeature()) != null)
                    {
                        sum++;
                        noBuildGeomList.Add(new NoBuild
                        {
                            WC_ID = jobTask.wireCenterID,
                            Addr_id = valu,
                            // TYPE = currentJob.qualType.ToString(),
                            Createmode = "Automatic",
                            STATUS = currentJob.qualStatus.ToString(),
                            REASON = "",
                            GEOEMTRY = ift.Shape
                        });
                    }
                    if (sum == 0)
                    {
                        itopo = feature.Shape as ITopologicalOperator;
                        IGeometry nobuildBuffPolygon = itopo.Buffer(0.0001); //36ft Buffer
                                                                             //get roads interecting with lndaddress
                        ISpatialFilter psp = new SpatialFilterClass();
                        psp.Geometry = feature.Shape;
                        psp.GeometryField = RoadsPolygons.ShapeFieldName;
                        psp.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;

                        IFeatureCursor proadCur = RoadsPolygons.Search(psp, false);
                        IFeature pRoadFeat = null;
                        while ((pRoadFeat = proadCur.NextFeature()) != null)
                        {
                            ITopologicalOperator proadtopo = pRoadFeat.Shape as ITopologicalOperator;
                            IGeometry NobuildPoly = proadtopo.Intersect(nobuildBuffPolygon, esriGeometryDimension.esriGeometry2Dimension);
                            noBuildGeomList.Add(new NoBuild
                            {
                                WC_ID = jobTask.wireCenterID,
                                Addr_id = ind,
                                // TYPE = currentJob.qualType.ToString(),
                                Createmode = "Automatic",
                                STATUS = currentJob.qualStatus.ToString(),
                                REASON = "",
                                GEOEMTRY = NobuildPoly
                            });
                        }
                        Marshal.ReleaseComObject(proadCur);

                        // geo.Add(new CentureLink.NoBuild { Geometry = igeo, UniqueID = "bufer" });
                        //itopo = igeo as ITopologicalOperator;
                        //IGeometry igeodiff = itopo.Difference(polygeometry);                       
                    }
                }

                if (noBuildGeomList.Count == 0)
                    return null;
                CreateNoBuildshapefile(noBuildGeomList, tempfilesPath + "NobuildShape.shp");
                noBuildFC = Getfeatureclass(tempfilesPath + "NobuildShape.shp");

                #region check for overlap  and solve


                ISpatialFilter pspf = new SpatialFilterClass();
                IFeatureCursor pnbCursor = noBuildFC.Search(null, false);
                IFeature pnBFeat = null;
                while ((pnBFeat = pnbCursor.NextFeature()) != null)
                {
                    //  lstPolys = new List<IPolygon>();

                    IPoint nbCenterPnt = (pnBFeat.Shape as IArea).Centroid;
                    ITopologicalOperator ptopo = pnBFeat.Shape as ITopologicalOperator;
                    pspf.Geometry = ptopo.Buffer(-0.00001);
                    pspf.GeometryField = noBuildFC.ShapeFieldName;
                    pspf.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                    IFeatureCursor pCurs1 = noBuildFC.Search(pspf, false);
                    IFeature pInterSectFeat = null;
                    ITopologicalOperator ptopol = pnBFeat.Shape as ITopologicalOperator;
                    while ((pInterSectFeat = pCurs1.NextFeature()) != null)
                    {
                        if (pInterSectFeat.OID == pnBFeat.OID)
                            continue;
                        IPoint pInterPolyCenterPnt = (pInterSectFeat.Shape as IArea).Centroid;
                        if (pInterPolyCenterPnt.X == nbCenterPnt.X && pInterPolyCenterPnt.Y == nbCenterPnt.Y)
                            continue;
                        IGeometry interSecPntsGEom = ptopol.Intersect(pInterSectFeat.Shape, esriGeometryDimension.esriGeometry0Dimension);
                        IPointCollection pcol = new PolylineClass();
                        pcol.AddPointCollection(interSecPntsGEom as IPointCollection);
                        IPolyline cutLIne = pcol as IPolyline;

                        IGeometry unionGeom = ptopol.Union(pInterSectFeat.Shape);
                        ITopologicalOperator pttt = unionGeom as ITopologicalOperator;
                        IGeometry geom1; IGeometry geom2;
                        try { pttt.Cut(cutLIne, out geom1, out geom2); }
                        catch (Exception) { continue; }
                        pInterSectFeat.Shape = geom1;
                        pInterSectFeat.Store();
                        pnBFeat.Shape = geom2;
                        pnBFeat.Store();

                    }
                    Marshal.ReleaseComObject(pCurs1);
                }
                Marshal.ReleaseComObject(pnbCursor);
                #endregion check for overlap  and solve

                #region Load final nobuild List              

                resultNobuildLst = new List<CenturyLink_ArcApp.NoBuild>();
                IFeatureCursor pnbCursor1 = noBuildFC.Search(null, false);
                IFeature pnBFeat1 = null;
                int statusIndex = noBuildFC.FindField("STATUS");
                int typeIndex = noBuildFC.FindField("TYPE");
                int reasonIndex = noBuildFC.FindField("REASON");
                int wcIDIndex = noBuildFC.FindField("WC_ID");
                int addr_id = noBuildFC.FindField("ADDR_ID");
                int creationmode = noBuildFC.FindField("CREATEMODE");
                int latlon = noBuildFC.FindField("LATLON");
                while ((pnBFeat1 = pnbCursor1.NextFeature()) != null)
                {
                    resultNobuildLst.Add(new NoBuild
                    {
                        GEOEMTRY = pnBFeat1.Shape,
                        REASON = pnBFeat1.Value[reasonIndex].ToString(),
                        //TYPE = pnBFeat1.Value[typeIndex].ToString(),
                        STATUS = pnBFeat1.Value[statusIndex].ToString(),
                        WC_ID = pnBFeat1.Value[wcIDIndex].ToString(),
                        Addr_id = Convert.ToInt32(pnBFeat1.Value[addr_id]),
                        Createmode = pnBFeat1.Value[creationmode].ToString(),
                        PolygonStatus = (int)JobPolygonStatusProgressEnum.Polygon_Creation_Completed
                    });
                }
                Marshal.ReleaseComObject(pnbCursor1);

                #endregion Load final nobuild List
            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
                isExecutionResult = false;
                return resultNobuildLst;
            }
            return resultNobuildLst;
        }
    }
}
