using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml;
using DbLibrary;
using ESRI.ArcGIS.CatalogUI;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.GeoDatabaseUI;
using ESRI.ArcGIS.Geometry;
using System.Threading;
using ESRI.ArcGIS.Geoprocessing;
using static CenturyLink_ArcApp.Arclib;
using static CenturyLink_ArcApp.Program;
using configKey = CenturyLink_ArcApp.Properties.Settings;
using System.IO;

namespace CenturyLink_ArcApp
{
    public class pointlist
    {

        public int id { get; set; }
        public IPoint point { get; set; }
        public IPolyline line { get; set; }
        public double distance { get; set; }
    }
    public class GeomList
    {
        public ISegment geom { get; set; }
        public double val { get; set; }

    }
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
                pWorkspaceName.WorkspaceFactoryProgID = "esriDataSourcesFile.ShapefileWorkspaceFactory";
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

            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
            }
            return ifc;
        }

        public IFeatureClass GetClippedParcels(IPolygon WCBoundary, IFeatureClass parcels)
        {
            IFeatureClass ifc = null;
            try
            {
                QueryFilter queryfilter = new QueryFilterClass();
                queryfilter.WhereClause = "COUNTY='Denver'";
                //This function creates various SelectionSets on the table and modifies them using the ISelectionSet methods.
                IDataset dataset = (IDataset)parcels;
                //use the query filter to select features
                ISelectionSet selectionSet = parcels.Select(queryfilter, esriSelectionType.esriSelectionTypeHybrid, esriSelectionOption.esriSelectionOptionNormal, dataset.Workspace);
                int shapeFieldPosition = parcels.FindField(parcels.ShapeFieldName);
                IFields inputFields = parcels.Fields;
                IField shapeField = inputFields.get_Field(shapeFieldPosition);
                IGeometryDef geometryDef = shapeField.GeometryDef;
                ExportFeatureClass(dataset, geometryDef, selectionSet, "ClippedParcels.shp", tempfilesPath);
                ifc = Getfeatureclass(tempfilesPath + "ClippedParcels.shp");
            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
            }
            return ifc;
        }
        public IFeatureClass ConvertFeatureClassToShapefile(IFeatureClass wirecenterfc)
        {
            IFeatureClass fcparcel = null;
            try
            {
                IWorkspaceFactory sourceWorkspaceFactory = new SdeWorkspaceFactory();
                IWorkspace sourceWorkspace = sourceWorkspaceFactory.Open(jobTask.LoadSDEswgisProperties(), 0);
                IFeatureWorkspace pFeatWsp = sourceWorkspace as IFeatureWorkspace;
                IFeatureClass sourceFeatureClass = pFeatWsp.OpenFeatureClass(configKey.Default.Parcels);
                string name = sourceFeatureClass.AliasName;
                String targetWorkspacePath = tempfilesPath;
                IWorkspaceFactory targetWorkspaceFactory = new ShapefileWorkspaceFactoryClass();
                IWorkspace targetWorkspace = targetWorkspaceFactory.OpenFromFile(targetWorkspacePath, 0);
                // Cast the workspaces to the IDataset interface and get name objects.
                IDataset sourceWorkspaceDataset = (IDataset)sourceWorkspace;
                IDataset targetWorkspaceDataset = (IDataset)targetWorkspace;
                IName sourceWorkspaceDatasetName = sourceWorkspaceDataset.FullName;
                IName targetWorkspaceDatasetName = targetWorkspaceDataset.FullName;
                IWorkspaceName sourceWorkspaceName = (IWorkspaceName)sourceWorkspaceDatasetName;
                IWorkspaceName targetWorkspaceName = (IWorkspaceName)
                  targetWorkspaceDatasetName;
                // Create a name object for the shapefile and cast it to the IDatasetName interface.
                IFeatureClassName sourceFeatureClassName = new FeatureClassNameClass();
                IDatasetName sourceDatasetName = (IDatasetName)sourceFeatureClassName;
                string[] sourcedatasename = name.Split('.');
                sourceDatasetName.Name = sourcedatasename[1];//CL_PARCELS
                sourceDatasetName.WorkspaceName = sourceWorkspaceName;
                // Create a name object for the FGDB feature class and cast it to the IDatasetName interface.
                IFeatureClassName targetFeatureClassName = new FeatureClassNameClass();
                IDatasetName targetDatasetName = (IDatasetName)targetFeatureClassName;
                targetDatasetName.Name = "Parcels";
                targetDatasetName.WorkspaceName = targetWorkspaceName;
                // Create the objects and references necessary for field validation.
                IFieldChecker fieldChecker = new FieldCheckerClass();
                IFields sourceFields = sourceFeatureClass.Fields;
                IFields targetFields = null;
                IEnumFieldError enumFieldError = null;
                // Set the required properties for the IFieldChecker interface.
                fieldChecker.InputWorkspace = sourceWorkspace;
                fieldChecker.ValidateWorkspace = targetWorkspace;
                // Validate the fields and check for errors.
                fieldChecker.Validate(sourceFields, out enumFieldError, out targetFields);
                if (enumFieldError != null)
                {
                    // Handle the errors in a way appropriate to your application.
                    // Console.WriteLine("Errors were encountered during field validation.");
                }
                // Find the shape field.
                String shapeFieldName = sourceFeatureClass.ShapeFieldName;
                int shapeFieldIndex = sourceFeatureClass.FindField(shapeFieldName);
                IField shapeField = sourceFields.get_Field(shapeFieldIndex);
                // Get the geometry definition from the shape field and clone it.
                IGeometryDef geometryDef = shapeField.GeometryDef;
                IClone geometryDefClone = (IClone)geometryDef;
                IClone targetGeometryDefClone = geometryDefClone.Clone();
                IGeometryDef targetGeometryDef = (IGeometryDef)targetGeometryDefClone;
                IPolygon wcBounday = GetWCPoly(wirecenterfc, jobTask.wireCenterID);
                ITopologicalOperator itopo = wcBounday as ITopologicalOperator;
                IGeometry buffergeo = itopo.Buffer(0.0025); //250 mt
                ISpatialFilter spatialFilter1 = new SpatialFilterClass();
                spatialFilter1.Geometry = buffergeo;
                spatialFilter1.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                spatialFilter1.GeometryField = sourceFeatureClass.ShapeFieldName;
                // Create the converter and run the conversion.
                IFeatureDataConverter featureDataConverter = new FeatureDataConverterClass();
                IEnumInvalidObject enumInvalidObject =
                  featureDataConverter.ConvertFeatureClass(sourceFeatureClassName,
                  spatialFilter1, null, targetFeatureClassName, targetGeometryDef, targetFields,
                  "", 1000, 0);
                // Check for errors.
                IInvalidObjectInfo invalidObjectInfo = null;
                enumInvalidObject.Reset();
                while ((invalidObjectInfo = enumInvalidObject.Next()) != null)
                {
                    // Handle the errors in a way appropriate to the application.
                    Console.WriteLine("Errors occurred for the following feature: {0}",
                      invalidObjectInfo.InvalidObjectID);
                }
                fcparcel = Getfeatureclass(tempfilesPath + "Parcels.shp");
            }
            catch (Exception ex)
            {

                throw ex;
            }
            return fcparcel;
        }
        public void Addpointcol(IFeatureClass point, IFeatureClass roads, List<ServicePolygonClass> li)
        {
            IFeatureCursor featureCursor = point.Search(null, false);
            IFeature feature = null;
            int index = point.FindField("PolygonID");
            while ((feature = featureCursor.NextFeature()) != null)
            {
                IBufferConstruction pbf = new BufferConstructionClass();
                double BufferVal = Math.Round(Convert.ToDouble(configKey.Default.ServiceBufferValue) / (3280.8 * 100), 5);
                IGeometry _300BufferPoint = pbf.Buffer(feature.Shape, BufferVal);

                string desigid = feature.get_Value(index).ToString();
                ISpatialFilter spatialFilter = new SpatialFilterClass();
                spatialFilter.Geometry = feature.Shape;
                spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                spatialFilter.GeometryField = point.ShapeFieldName;
                IFeatureCursor roadcursor = roads.Search(spatialFilter, false);
                IFeature feat = null;
                while ((feat = roadcursor.NextFeature()) != null)
                {                   
                    ITopologicalOperator ptopo = feat.Shape as ITopologicalOperator;
                    IGeometry pinterGeom = ptopo.Intersect(_300BufferPoint, esriGeometryDimension.esriGeometry2Dimension);

                    IArea parea = feat.Shape as IArea;
                    IArea pgeomarea = pinterGeom as IArea;
                    double intesectedAreaPercent = (pgeomarea.Area / parea.Area) * 100;
                    if (intesectedAreaPercent > 60)
                        li.Add(new ServicePolygonClass
                        {
                            SERVING_WIRE_CENTER_NAME = jobTask.wireCenterName,
                            TYPE = jobTask.qualType,
                            FIBER_CABLE_ID = desigid,
                            polyGeometry = feat.Shape,
                            SERVIING_WIRE_CENTER_CLLI = jobTask.wireCenterID,
                            Polygon_Status = (int)JobPolygonStatusProgressEnum.Polygon_Creation_Completed
                        });
                    else
                        li.Add(new ServicePolygonClass
                        {
                            SERVING_WIRE_CENTER_NAME = jobTask.wireCenterName,
                            TYPE = jobTask.qualType,
                            FIBER_CABLE_ID = desigid,
                            polyGeometry = pinterGeom, 
                            SERVIING_WIRE_CENTER_CLLI = jobTask.wireCenterID,
                            Polygon_Status = (int)JobPolygonStatusProgressEnum.Polygon_Creation_Completed
                        });
                }
                jobTask.designationLst[desigid] = new List<string> { JobPolygonStatusProgressEnum.designationSuccess.ToString(), "" };
            }
            Marshal.ReleaseComObject(featureCursor);
        }
        public IFeatureClass ExtendParcels(IFeatureClass wirecenterfc)
        {
            IFeatureClass merged = null;
            try
            {
                //need to take the clipped network
                IFeatureClass Fcroads = Arclib.Getfeatureclass(tempfilesPath + "ClippedRNbf.shp");
                IFeatureClass FCPoints = Getfeatureclass(@"C:\SQM_Logs\ppp.shp");
                // extending parcel boundary to road
                IFeatureCursor featureCursor = Fcroads.Search(null, true);
                IFeature feature = null;
                List<ServicePolygonClass> li = new List<ServicePolygonClass>();
                while ((feature = featureCursor.NextFeature()) != null)
                {
                    IFeature ifc = null; 
                    if (ifc == null)
                    {
                        continue;
                    }
                    IProximityOperator iproxy = ifc.Shape as IProximityOperator;
                    ISpatialFilter spatialFilter = new SpatialFilterClass();
                    spatialFilter.Geometry = feature.Shape;
                    spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                    spatialFilter.GeometryField = Fcroads.ShapeFieldName;
                    IFeatureCursor cursor1 = FCPoints.Search(spatialFilter, true);
                    IFeature pfeature = cursor1.NextFeature();
                    while ((pfeature = cursor1.NextFeature()) != null)
                    {
                        IPoint ipo = iproxy.ReturnNearestPoint(pfeature.Shape as IPoint, esriSegmentExtension.esriNoExtension);
                        double distance = iproxy.ReturnDistance(ipo);
                        if (distance > 0.00019)
                        {
                            continue;
                        }
                        object gMissing = System.Type.Missing;
                        IPointCollection pOutPntColl = new PolylineClass(); // a new path
                        pOutPntColl.AddPoint(pfeature.Shape as IPoint, ref gMissing, ref gMissing); // add first point
                        pOutPntColl.AddPoint(ipo, ref gMissing, ref gMissing); // add 2nd point
                                                                               //IGeometryCollection pOutGeomColl = new PolylineClass(); // a new polyline
                                                                               // pOutGeomColl.AddGeometry((IGeometry)pOutPntColl, ref gMissing, ref gMissing); // add this path
                        IGeometry pOutGeom = (IGeometry)pOutPntColl; // cast the geometry collection to a geometry
                        li.Add(new ServicePolygonClass { polyGeometry = pOutGeom });

                    }
                    cursor1.Flush(); Marshal.ReleaseComObject(cursor1);
                }
                featureCursor.Flush(); Marshal.ReleaseComObject(featureCursor);
                Arclib.Createshapefile(li, tempfilesPath + "Conectinglines.shp", false);
                merged = Arclib.Merge(tempfilesPath + "ClippedRN.shp"
                            , tempfilesPath + "Parcelpolylines.shp"
                            , tempfilesPath + "Conectinglines.shp");
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return merged;
        }
        public void DeleteBorderedges()
        {
            IFeatureClass Fcroads = Getfeatureclass(@"C:\SQM_Logs\CLT_tempFiles\3675\ClippedRN.shp");
            IWorkspaceFactory WorkspaceFactory = new ShapefileWorkspaceFactoryClass();
            IWorkspace Workspace = WorkspaceFactory.OpenFromFile(@"C:\SQM_Logs\", 0);
            IFeatureWorkspace featureWorkspace = (IFeatureWorkspace)Workspace;
            IFeatureClass featureClass = featureWorkspace.OpenFeatureClass("sss.shp");
            IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)Workspace;
            //start editing with undo redo functionality
            workspaceEdit.StartEditing(true);
            workspaceEdit.StartEditOperation();
            IFeatureCursor featureCursor = Fcroads.Search(null, false);
            IFeature feature = null;
            while ((feature = featureCursor.NextFeature()) != null)
            {
                IGeometry buffergeo = GetBuffer(feature.Shape, 0.00015);
                ISpatialFilter spatialFilter = new SpatialFilterClass();
                spatialFilter.Geometry = buffergeo;
                spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                spatialFilter.GeometryField = Fcroads.ShapeFieldName;
                IFeatureCursor cursor1 = featureClass.Search(spatialFilter, false);
                IFeature pfeature = null;
                int index = featureClass.FindField("LEFT_FID");
                while ((pfeature = cursor1.NextFeature()) != null)
                {
                    try
                    {
                        int val = Convert.ToInt32(pfeature.get_Value(index));
                        if (val == -1)
                        {
                            pfeature.Delete();
                        }
                    }
                    catch (Exception ex)
                    {

                        // throw ex;
                    }

                }
                cursor1.Flush(); //Marshal.ReleaseComObject(cursor1);

            }
            featureCursor.Flush();
            workspaceEdit.StopEditing(true);
            workspaceEdit.StopEditOperation();
        }

        IGeometry GetBuffer(IGeometry feature, double buffer)
        {
            ITopologicalOperator topologicalOperator = feature as ITopologicalOperator;

            IGeometry buffergeo = topologicalOperator.Buffer(buffer);
            return buffergeo;
        }
        public bool GetCustomParcel(IFeatureClass Customparcel, IFeature feature, ref IGeometry geom)
        {
            bool isparcel = false;
            try
            {
                ISpatialFilter sf = new SpatialFilterClass();
                sf.Geometry = feature.Shape;
                sf.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                IFeatureCursor cursor = Customparcel.Search(sf, false);
                IFeature feat = null;
                while ((feat = cursor.NextFeature()) != null)
                {
                    geom = feat.Shape;
                    isparcel = true;
                }
                cursor.Flush();
                Marshal.ReleaseComObject(cursor);
            }
            catch (Exception ex)
            {

                throw ex;
            }
            return isparcel;
        }

        public IFeatureCursor GetLivingunitCursor(IFeatureClass FCLivingunits, string boundingbox)
        {
            IEnvelope envelope = null;
            IFeatureCursor LUcursor = null;
            try
            {
                //boundingbox = string.Empty;
                if (boundingbox != string.Empty && boundingbox.Contains("Xmin"))
                {
                    string[] list = boundingbox.Split(','); string[] coor = new string[4];
                    for (int i = 0; i < list.Length; i++)
                    {
                        string[] jd = list[i].Split(':');
                        coor[i] = jd[1];
                    }
                    IPointCollection ip = new PolygonClass();
                    double xmax = Convert.ToDouble(coor[1]); double xmin = Convert.ToDouble(coor[0]);
                    double ymax = Convert.ToDouble(coor[3]); double ymin = Convert.ToDouble(coor[2]);
                    envelope = new EnvelopeClass();
                    envelope.PutCoords(xmin, ymin, xmax, ymax);
                    ISpatialFilter spatialfilter = new SpatialFilterClass();
                    spatialfilter.Geometry = envelope;
                    spatialfilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                    LUcursor = FCLivingunits.Search(spatialfilter, false);
                    int cnt = FCLivingunits.FeatureCount(spatialfilter);
                }
                else
                {
                    
                    LUcursor = FCLivingunits.Search(null, false);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return LUcursor;
        }
        public IGeometry GetparcelwithRoad(IFeatureClass RoadpolygonFC, IFeature parcelfeat, IFeatureClass FcParcels)
        {
            IGeometry Finalgeo = null;
            try
            {
                List<IPolyline> Roadpolylines = ReturnPoly(RoadpolygonFC, parcelfeat.Shape);
                ISegmentCollection Parcelsegments = parcelfeat.Shape as ISegmentCollection;
                //Totalparcelpoints = new List<IPoint>();
                List<ServicePolygonClass> sl = new List<ServicePolygonClass>();
                List<ServicePolygonClass> sl1 = new List<ServicePolygonClass>();
                object gMissing = System.Type.Missing;
                List<IPolygon> geolist = new List<IPolygon>(); //ReturngeomlistAroundParcel(Parcelsegments, FcParcels, parcelid, Roadpolylines);
                IPointCollection col = null;
                for (int i = 0; i < Parcelsegments.SegmentCount; i++)
                {
                    List<pointlist> pl1 = new List<pointlist>();
                    List<pointlist> pl2 = new List<pointlist>();
                    bool Isadjacent = Edgeadjacent(FcParcels, Parcelsegments.Segment[i], parcelfeat.OID);
                    if (Isadjacent) continue;
                    col = new PolygonClass() as IPointCollection;
                    col.AddPoint(Parcelsegments.Segment[i].FromPoint, ref gMissing, ref gMissing);
                    col.AddPoint(Parcelsegments.Segment[i].ToPoint, ref gMissing, ref gMissing);
                    for (int k = 0; k < Roadpolylines.Count; k++)
                    {
                        IProximityOperator iproxy = Roadpolylines[k] as IProximityOperator;
                        double distance = iproxy.ReturnDistance(Parcelsegments.Segment[i].FromPoint);
                        double distance2 = iproxy.ReturnDistance(Parcelsegments.Segment[i].ToPoint);
                        if (distance < 0.00019)
                        {
                            IPoint po = iproxy.ReturnNearestPoint(Parcelsegments.Segment[i].FromPoint, esriSegmentExtension.esriNoExtension);
                            col.AddPoint(po, ref gMissing, ref gMissing);
                            // col.AddPoint(ip1, ref gMissing, ref gMissing);
                            pl1.Add(new pointlist { id = k, point = po, line = Roadpolylines[k], distance = distance });
                        }
                        if (distance2 < 0.00019)
                        {
                            IPoint po = iproxy.ReturnNearestPoint(Parcelsegments.Segment[i].ToPoint, esriSegmentExtension.esriNoExtension);
                            col.AddPoint(po, ref gMissing, ref gMissing);
                            // col.AddPoint(ip2, ref gMissing, ref gMissing);
                            pl2.Add(new pointlist { id = k, point = po, line = Roadpolylines[k], distance = distance2 });

                        }

                    }
                    // Marshal.ReleaseComObject(RoadCursor);
                    //adding intersection lines to the hull pointcollection
                    if (pl2.Count > 0)
                    {
                        AddIntersectpoints(pl2, col);
                    }
                    if (pl1.Count > 0)
                    {
                        AddIntersectpoints(pl1, col);
                    }
                    if (col.PointCount > 0)
                    {
                        IGeometry geom = GetHull(col);
                        if (geom == null) continue;
                        geom.SpatialReference = parcelfeat.Shape.SpatialReference;
                        geolist.Add(geom as IPolygon);
                    }

                } //parceledges
                geolist.Add(parcelfeat.Shape as IPolygon);
                Finalgeo = Unionpolygon(geolist);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return Finalgeo;
        }
        /// <summary>
        /// create basic copper polygon
        /// </summary>
        public List<CopperPolygonClass> CreateCopperPolygonGeneration(IFeatureClass FcParcels, string wcid, string jobid)
        {
            List<CopperPolygonClass> copperpoly = new List<CopperPolygonClass>();
            jobTask.dictfailed = new Dictionary<string, List<string>>();
            jobTask.dictsuccess = new Dictionary<string, List<string>>();
            jobTask.dictexception = new Dictionary<string, List<string>>();
            try
            {
                Dictionary<string, string> dictLuidST;
                Jobs jobs = new Jobs();
                IFeatureClass FCLivingunits = jobs.GetFCfromSDE("LU_" + wcid + "_" + jobid, jobs.LoadSDEswgisProperties());
                IFeatureClass CustomParcel = jobs.GetFCfromSDE(configKey.Default.CustomParcelLayer, jobs.LoadSDEswgisProperties());
                IFeatureClass FcRoads = Arclib.Getfeatureclass(tempfilesPath + "ClippedRN.shp");
                IFeatureClass RoadpolygonFC = Arclib.DoConvertlinetopolygon(FcRoads, "RoadPoly");
                Arclib.AddSpatialindex(RoadpolygonFC);
                IFeatureCursor LUcursor = GetLivingunitCursor(FCLivingunits, jobTask.designations);
                IFeature feat = null;
                int latindex = FCLivingunits.FindField("GEO_LAT");
                int lonindex = FCLivingunits.FindField("GEO_LON");
                int speedindex = FCLivingunits.FindField("MOD_SPEED_DOWN");
                int luidindex = FCLivingunits.FindField("ADDRESS_ID");
                int servterindex = FCLivingunits.FindField("SERVING_TERMINAL");
                int parcelindex = FcParcels.FindField("OBJECTID");
                IFeatureCursor parcelcursor = null;
                //Living units loop
                int count = 0;
                while ((feat = LUcursor.NextFeature()) != null)
                {
                    try
                    {
                        count++;
                        LogManager.WriteLogandConsole("Info: Count of points while creating Polygons : " + count);
                        string luid = feat.get_Value(luidindex).ToString();
                        string lat = feat.get_Value(latindex).ToString();
                        string lon = feat.get_Value(lonindex).ToString();
                        string servingTerminal = feat.get_Value(servterindex).ToString();
                        string speed = feat.get_Value(speedindex).ToString();
                        if (lat == "" || lon == "")
                        {
                            LogManager.WriteLogandConsole("Info: Location Not Found for " + luid);
                            continue;
                        }
                        if (speed == "0" || speed == "")
                            speed = OracleClientClass.GetNearestSpeed(wcid, Convert.ToDouble(lat), Convert.ToDouble(lon), luid);
                        if (speed == "0")
                        {
                            //if (!jobTask.dictexception.ContainsKey("LatlongLuid:" + lat + "|" + lon + "|" + luid))
                            //    jobTask.dictexception.Add("LatlongLuid:" + lat + "|" + lon + "|" + luid, new List<string> { "", Constants.Messages.Copper_Nospeedmessage + ",LUID is :" + lu_id + " and luid count " + lucount });
                        }
                        if (speed == "")
                        {
                            string lu_id = jobTask.Getluid(lat, lon, wcid, jobid); int lucount = jobTask.Getluidcount(lat, lon, wcid, jobid);
                            if (!jobTask.dictfailed.ContainsKey("LatlongLuid:" + lat + "|" + lon + "|" + luid))
                                jobTask.dictfailed.Add("LatlongLuid:" + lat + "|" + lon + "|" + luid, new List<string> { "", Constants.Messages.Copper_Emptyspeedmessage + ",LUID is :" + lu_id + " and luid count " + lucount });
                            continue;
                        }
                        ISpatialFilter spatialFilter1 = new SpatialFilterClass();
                        spatialFilter1.Geometry = feat.Shape;
                        spatialFilter1.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                        spatialFilter1.GeometryField = FcParcels.ShapeFieldName;
                        parcelcursor = FcParcels.Search(spatialFilter1, false);
                        int fcparcelcount = FcParcels.FeatureCount(spatialFilter1);
                        IFeature parcelfeat = null;
                        bool ishavingparcel = false;
                        int apnindex = FcParcels.FindField("APN");
                        int fipsindex = FcParcels.FindField("FIPS_CODE");//"FIPS");
                        #region customparcel checking loop
                        //customparcel loop
                        int cus_apnindex = CustomParcel.FindField("APN");
                        int cus_fipsindex = CustomParcel.FindField("FIPS");
                        IFeatureCursor cursor = CustomParcel.Search(spatialFilter1, false);
                        IFeature feature = null;
                        bool iscustomparcelavail = false;
                        while ((feature = cursor.NextFeature()) != null)
                        {
                            int parcelid = feature.OID;
                            string apn = feature.get_Value(cus_apnindex).ToString();
                            string fips = feature.get_Value(cus_fipsindex).ToString();
                            if (jobs.IsHavingcopperreview(apn, fips, jobTask.wireCenterID) > 0)
                            {
                                if (!jobTask.dictexception.ContainsKey("LatlongLuid:" + lat + "|" + lon + "|" + luid))
                                    jobTask.dictexception.Add("LatlongLuid:" + lat + "|" + lon + "|" + luid, new List<string> { "", Constants.Messages.Copper_PolygonExecutionProgress });
                                continue;
                            }
                            IGeometry finalgeo = GetparcelwithRoad(RoadpolygonFC, feature, FcParcels);
                            dictLuidST = new Dictionary<string, string>();
                            dictLuidST.Add(luid, servingTerminal);
                            if (!jobTask.dictsuccess.ContainsKey("LatlongLuid:" + lat + "|" + lon + "|" + luid))
                            {
                                jobTask.dictsuccess.Add("LatlongLuid:" + lat + "|" + lon + "|" + luid, new List<string> { "", JobPolygonStatusProgressEnum.designationSuccess.ToString() });
                                copperpoly.Add(new CopperPolygonClass { PolyGeometry = finalgeo, dictLUID = dictLuidST, APN = apn, QUAL_TYPE = Constants.QualType.COPPER, Parcel_Type = (int)Parceltype.custom, FIPS = fips, Bandwidth = speed, WC_NAME = jobTask.wireCenterName, LU_Lat = lat, LU_Lon = lon, WC_CLLI = wcid, Parcel_ID = parcelid });
                            }
                            iscustomparcelavail = true; ishavingparcel = true;
                        }
                        Marshal.ReleaseComObject(cursor);
                        // parcel loop
                        if (!iscustomparcelavail)
                        {
                            #endregion
                            while ((parcelfeat = parcelcursor.NextFeature()) != null)
                            {
                                int parcelid = parcelfeat.OID; ishavingparcel = true;
                                string apn = parcelfeat.get_Value(apnindex).ToString();
                                string fips = parcelfeat.get_Value(fipsindex).ToString();
                                if (jobs.IsHavingcopperreview(apn, fips, jobTask.wireCenterID) > 0)
                                {
                                    if (!jobTask.dictexception.ContainsKey("LatlongLuid:" + lat + "|" + lon + "|" + luid))
                                        jobTask.dictexception.Add("LatlongLuid:" + lat + "|" + lon + "|" + luid, new List<string> { "", Constants.Messages.Copper_PolygonExecutionProgress });
                                    continue;
                                }
                                CopperPolygonClass co = copperpoly.Where(x => x.APN == apn && x.FIPS == fips).FirstOrDefault();
                                if (copperpoly.Contains(co))
                                {
                                    co.dictLUID.Add(luid, servingTerminal);
                                    continue;
                                }
                                IGeometry Finalgeo = GetparcelwithRoad(RoadpolygonFC, parcelfeat, FcParcels);
                                IArea parea = Finalgeo as IArea;
                                IArea parcelarea = parcelfeat.Shape as IArea;
                                if (parea.Area > parcelarea.Area * 3)
                                {
                                    string lu_id = jobTask.Getluid(lat, lon, wcid, jobid); int lucount = jobTask.Getluidcount(lat, lon, wcid, jobid);
                                    if (!jobTask.dictfailed.ContainsKey("LatlongLuid:" + lat + "|" + lon + "|" + luid))
                                        jobTask.dictfailed.Add("LatlongLuid:" + lat + "|" + lon + "|" + luid, new List<string> { "", Constants.Messages.Copper_IncorrectGeometryMessage + ",LUID is :" + lu_id + " and luid count " + lucount });
                                    continue;
                                }
                                dictLuidST = new Dictionary<string, string>();
                                dictLuidST.Add(luid, servingTerminal);
                                if (!jobTask.dictsuccess.ContainsKey("LatlongLuid:" + lat + "|" + lon + "|" + luid))
                                {
                                    jobTask.dictsuccess.Add("LatlongLuid:" + lat + "|" + lon + "|" + luid, new List<string> { "", JobPolygonStatusProgressEnum.designationSuccess.ToString() });
                                    copperpoly.Add(new CopperPolygonClass { PolyGeometry = Finalgeo, dictLUID = dictLuidST, APN = apn, QUAL_TYPE = Constants.QualType.COPPER, Parcel_Type = (int)Parceltype.custom, FIPS = fips, Bandwidth = speed, WC_NAME = jobTask.wireCenterName, LU_Lat = lat, LU_Lon = lon, WC_CLLI = wcid, Parcel_ID = parcelid });
                                }
                                break;
                            }
                        }
                        if (!ishavingparcel)
                        {
                            string lu_id = jobTask.Getluid(lat, lon, wcid, jobid); int lucount = jobTask.Getluidcount(lat, lon, wcid, jobid);
                            if (!jobTask.dictfailed.ContainsKey("LatlongLuid:" + lat + "|" + lon + "|" + luid))
                                jobTask.dictfailed.Add("LatlongLuid:" + lat + "|" + lon + "|" + luid, new List<string> { "", Constants.Messages.Copper_NoParcelfoundMessage + ",LUID is :" + lu_id + " and luid count " + lucount });
                            continue;
                        }
                        Marshal.ReleaseComObject(parcelcursor);
                    }

                    catch (Exception ex)
                    {
                        //throw ex;
                        LogManager.WriteLogandConsole(ex);
                    }
                }
                Marshal.ReleaseComObject(LUcursor);
                if (copperpoly.Count != 0)
                {
                    #region check overlap forcopper polygon
                    //Arclib.Createshapefile(lis, tempfilesPath + "\\Copperpoly\\Copperpoly.shp", false);
                    ////copperpoly = Findoverlaps(tempfilesPath + "\\Copperpoly\\Copperpoly.shp", copperpoly, tempfilesPath + "\\Copperpoly\\NoOverlaps.shp");
                    //List<ServicePolygonClass> spl = new List<ServicePolygonClass>();
                    //for (int i = 0; i < copperpoly.Count; i++)
                    //{
                    //    spl.Add(new ServicePolygonClass { polyGeometry = copperpoly[i].PolyGeometry, FIBER_CABLE_ID = copperpoly[i].Bandwidth });
                    //}
                    //Arclib.Createshapefile(spl, tempfilesPath + "Copperpoly\\FinalCopper.shp", false);
                    #endregion
                    IPropertySet propertySet = new ESRI.ArcGIS.esriSystem.PropertySetClass();
                    propertySet.SetProperty(Constants.DBProperties.server, configKey.Default.Qual_Server);
                    propertySet.SetProperty(Constants.DBProperties.instance, configKey.Default.Qual_INSTANCE);
                    propertySet.SetProperty(Constants.DBProperties.authMode, configKey.Default.AUTHENTICATION_MODE);
                    propertySet.SetProperty(Constants.DBProperties.database, configKey.Default.Qual_DATABASE);
                    propertySet.SetProperty(Constants.DBProperties.user, configKey.Default.Qual_USER_SWGISLOC);
                    propertySet.SetProperty(Constants.DBProperties.password, configKey.Default.Qual_PASSWORD_SWGISLOC);
                    propertySet.SetProperty(Constants.DBProperties.version, configKey.Default.VERSION);
                    IFeatureClass copperSQM = jobs.GetFCfromSDE(configKey.Default.Copper_SQM, propertySet);
                    LogManager.WriteLogandConsole("INFO: Inserting data into copper SQM... ");
                    Arclib.InsertCopperintoODS(copperSQM, copperpoly);

                }

            }
            catch (Exception ex)
            {

                throw ex;
            }
            return copperpoly;
        }
        public List<IPolygon> ReturngeomlistAroundParcel(ISegmentCollection Parcelsegments, IFeatureClass FcParcels, int parcelid, List<IPolyline> Roadpolylines)
        {
            List<IPolygon> geolist = new List<IPolygon>();
            IPointCollection ipointcol = null;
            List<pointlist> pl1 = new List<pointlist>();
            List<pointlist> pl2 = new List<pointlist>();
            try
            {
                object gMissing = System.Type.Missing;
                for (int i = 0; i < Parcelsegments.SegmentCount; i++)
                {
                    if (pl1.Count > 0) pl1.Clear(); if (pl2.Count > 0) pl2.Clear();
                    bool Isadjacent = Edgeadjacent(FcParcels, Parcelsegments.Segment[i], parcelid);
                    if (Isadjacent) continue;
                    ipointcol = new PolygonClass();
                    ipointcol.AddPoint(Parcelsegments.Segment[i].FromPoint, ref gMissing, ref gMissing);
                    ipointcol.AddPoint(Parcelsegments.Segment[i].ToPoint, ref gMissing, ref gMissing);
                    for (int k = 0; k < Roadpolylines.Count; k++)
                    {
                        IProximityOperator iproxy = Roadpolylines[k] as IProximityOperator;
                        double distance = iproxy.ReturnDistance(Parcelsegments.Segment[i].FromPoint);
                        double distance2 = iproxy.ReturnDistance(Parcelsegments.Segment[i].ToPoint);
                        if (distance < 0.00019)
                        {
                            IPoint po = iproxy.ReturnNearestPoint(Parcelsegments.Segment[i].FromPoint, esriSegmentExtension.esriNoExtension);
                            ipointcol.AddPoint(po, ref gMissing, ref gMissing);
                            // col.AddPoint(ip1, ref gMissing, ref gMissing);
                            pl1.Add(new pointlist { id = k, point = po, line = Roadpolylines[k], distance = distance });
                        }
                        if (distance2 < 0.00019)
                        {
                            IPoint po = iproxy.ReturnNearestPoint(Parcelsegments.Segment[i].ToPoint, esriSegmentExtension.esriNoExtension);
                            ipointcol.AddPoint(po, ref gMissing, ref gMissing);
                            // col.AddPoint(ip2, ref gMissing, ref gMissing);
                            pl2.Add(new pointlist { id = k, point = po, line = Roadpolylines[k], distance = distance2 });

                        }

                    }
                    //adding intersection lines to the hull pointcollection
                    if (pl2.Count > 0)
                    {
                        AddIntersectpoints(pl2, ipointcol);
                    }
                    if (pl1.Count > 0)
                    {
                        AddIntersectpoints(pl1, ipointcol);
                    }
                    if (ipointcol.PointCount > 0)
                    {
                        IPolygon geom = GetHull(ipointcol);
                        if (geom == null) continue;
                        geolist.Add(geom);
                    }

                } //parceledges

            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Marshal.ReleaseComObject(ipointcol);
            }
            return geolist;
        }
        public void AddIntersectpoints(List<pointlist> pointlist, IPointCollection pointcollection)
        {
            try
            {
                object gmissing = System.Type.Missing;
                for (int j = 0; j < pointlist.Count; j++)
                {
                    ITopologicalOperator topo = pointlist[j].line as ITopologicalOperator;
                    for (int k = 0; k < pointlist.Count; k++)
                    {
                        if (j == k) continue;
                        IPointCollection geomtry = null;
                        try
                        {
                            geomtry = topo.Intersect(pointlist[k].line, esriGeometryDimension.esriGeometry0Dimension) as IPointCollection;
                        }
                        catch (Exception ex)
                        {
                            continue;
                        }
                        if (geomtry.PointCount > 0)
                        {
                            pointcollection.AddPoint(geomtry.Point[0], ref gmissing, ref gmissing);
                        }

                    }

                }
            }
            catch (Exception ex)
            {

            }
        }
        public bool Edgeadjacent(IFeatureClass FcParcels, ISegment seg, int id)
        {
            bool isval = false;
            try
            {
                IPolyline polyline = new PolylineClass();
                polyline.FromPoint = seg.FromPoint; polyline.ToPoint = seg.ToPoint;
                ISpatialFilter spatialFilter1 = new SpatialFilterClass();
                spatialFilter1.Geometry = polyline;
                spatialFilter1.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                spatialFilter1.GeometryField = FcParcels.ShapeFieldName;
                IFeatureCursor parcelcursor = FcParcels.Search(spatialFilter1, false);
                IFeature ifc = null;
                List<ISegment> seglist = new List<ISegment>();
                while ((ifc = parcelcursor.NextFeature()) != null)
                {
                    if (ifc.OID == id) continue;

                    ISegmentCollection segcol = ifc.Shape as ISegmentCollection;
                    for (int i = 0; i < segcol.SegmentCount; i++)
                    {
                        if (segcol.Segment[i].FromPoint == seg.FromPoint && segcol.Segment[i].ToPoint == seg.ToPoint)
                        {
                            isval = true;
                        }
                        if (segcol.Segment[i].ToPoint.X == seg.FromPoint.X && segcol.Segment[i].FromPoint.X == seg.ToPoint.X)
                        {
                            if (segcol.Segment[i].ToPoint.Y == seg.FromPoint.Y && segcol.Segment[i].FromPoint.Y == seg.ToPoint.Y)
                            {
                                isval = true;
                            }
                        }
                    }
                }
                Marshal.ReleaseComObject(parcelcursor);
                Marshal.ReleaseComObject(spatialFilter1);
            }
            catch (Exception ex)
            {
                /// throw ex;
            }
            return isval;

        }
        public int GetAngle(IPoint p1, IPoint p2)
        {
            double x1 = p1.X, y1 = p1.Y, x2 = p2.X, y2 = p2.Y;
            var deltax = Math.Pow((x2 - x1), 2);
            var deltay = Math.Pow((y2 - y1), 2);
            var distance = Math.Sqrt(deltay + deltax);
            var radiance = Math.Atan2((y2 - y1), (x2 - x1));
            var angle = radiance * (180 / Math.PI);
            return (int)angle;
        }
        public IPolygon GetHull(IPointCollection Parcelpoints)
        {
            IGeometry geo = null;
            try
            {
                ITopologicalOperator topologicalOperator = Parcelpoints as ITopologicalOperator;
                geo = topologicalOperator.ConvexHull();
                if (geo.GeometryType == esriGeometryType.esriGeometryPolyline) { return null; }
                if (geo.GeometryType == esriGeometryType.esriGeometryPoint) { return null; }
            }
            catch (Exception ex)
            {

                throw ex;
            }
            return (IPolygon)geo;
        }
        public IPolygon ReturnroadPoly(IFeatureClass Roadpoly, IGeometry geo)
        {
            IPolygon geom = null;
            List<IPolygon> li = new List<IPolygon>();
            try
            {
                ISpatialFilter spatialFil = new SpatialFilterClass();
                spatialFil.Geometry = geo;
                spatialFil.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                spatialFil.GeometryField = Roadpoly.ShapeFieldName;
                IFeatureCursor RoadCursor = Roadpoly.Search(spatialFil, false);
                IFeature roadfeature = null;
                while ((roadfeature = RoadCursor.NextFeature()) != null)
                {
                    geom = roadfeature.Shape as IPolygon; li.Add(geom);
                }
                IPolygon unionpoly = Unionpolygon(li);
            }
            catch (Exception ex) { }
            return geom;
        }
        public List<IPolyline> ReturnPoly(IFeatureClass Roadpoly, IGeometry geo)
        {
            List<IPolyline> seg = new List<IPolyline>();
            try
            {
                ISpatialFilter spatialFil = new SpatialFilterClass();
                spatialFil.Geometry = geo;
                spatialFil.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                spatialFil.GeometryField = Roadpoly.ShapeFieldName;
                IFeatureCursor RoadCursor = Roadpoly.Search(spatialFil, false);
                IFeature roadfeature = null;
                ISegmentCollection tsegmentCollection = new PolylineClass();

                while ((roadfeature = RoadCursor.NextFeature()) != null)
                {

                    ISegmentCollection segmentCollection = roadfeature.Shape as ISegmentCollection;
                    tsegmentCollection.AddSegmentCollection(segmentCollection);

                }
                for (int i = 0; i < tsegmentCollection.SegmentCount; i++)
                {
                    IPolyline line = new PolylineClass();
                    line.FromPoint = tsegmentCollection.Segment[i].FromPoint;
                    line.ToPoint = tsegmentCollection.Segment[i].ToPoint;
                    seg.Add(line);
                }
                Marshal.ReleaseComObject(RoadCursor);
            }
            catch (Exception ex)
            {

                throw ex;
            }
            return seg;
        }
        public void processparcels(IFeatureClass FcParcels)
        {
            try
            {
                IFeatureCursor parcelcursor = FcParcels.Search(null, true);
                IFeatureClass FcRoads = Arclib.Getfeatureclass(tempfilesPath + "ClippedRN.shp");
                IFeature parcelfeat = null;
                //parcel loop
                List<ServicePolygonClass> li = new List<ServicePolygonClass>();
                IPointCollection Parcelpoints = null;
                ITopologicalOperator topologicalOperator = null;
                while ((parcelfeat = parcelcursor.NextFeature()) != null)
                {
                    int parcelid = parcelfeat.OID;
                    Parcelpoints = parcelfeat.Shape as IPointCollection;
                    // ITopologicalOperator topologicalOperator = parcelfeat.Shape as ITopologicalOperator;
                    IGeometry buffergeo = GetBuffer(parcelfeat.Shape, 0.00015);
                    ISpatialFilter spatialFil = new SpatialFilterClass();
                    spatialFil.Geometry = buffergeo;
                    spatialFil.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                    spatialFil.GeometryField = FcRoads.ShapeFieldName;
                    IFeatureCursor RoadCursor = FcRoads.Search(spatialFil, true);
                    IFeature roadpoly = null;
                    List<IPoint> pointcollection = new List<IPoint>();
                    object gMissing = System.Type.Missing;
                    //road loop
                    while ((roadpoly = RoadCursor.NextFeature()) != null)
                    {
                        IProximityOperator iproxy = roadpoly.Shape as IProximityOperator;
                        for (int i = 0; i < Parcelpoints.PointCount; i++)
                        {
                            double distance = iproxy.ReturnDistance(Parcelpoints.Point[i]);
                            if (distance < 0.00015)
                            {
                                IPoint po = iproxy.ReturnNearestPoint(Parcelpoints.Point[i], esriSegmentExtension.esriNoExtension);
                                pointcollection.Add(po);
                            }

                        }
                    }
                    RoadCursor.Flush(); Marshal.ReleaseComObject(RoadCursor);
                    for (int i = 0; i < pointcollection.Count; i++)
                    {
                        Parcelpoints.AddPoint(pointcollection[i], ref gMissing, ref gMissing);
                    }
                    topologicalOperator = Parcelpoints as ITopologicalOperator;
                    IGeometry parcelpoly = topologicalOperator.ConvexHull();
                    IArea parea = parcelpoly as IArea;
                    if (parea.Area > 0.000005) continue;
                    li.Add(new ServicePolygonClass { polyGeometry = parcelpoly });
                }
                parcelcursor.Flush(); Marshal.ReleaseComObject(parcelcursor);
                Arclib.Createshapefile(li, tempfilesPath + "Copperpoly.shp", false);
            }
            catch (Exception)
            {

                throw;
            }

        }
        /// <summary>
        /// create basic copper polygon
        /// </summary>

        /// <summary>
        /// create basic copper polygon
        /// </summary>
        public void CreateCopperPolygon(IFeatureClass FcParcels)
        {
            try
            {
                Jobs jobs = new Jobs();
                IFeatureClass ifc = jobs.GetFCfromSDE(configKey.Default.Livingunits, jobs.LoadSDEswgisProperties());
                FcParcels = Arclib.Getfeatureclass(tempfilesPath + "Mergedroadpolygons.shp");
                IFeatureClass actualparcels = Arclib.Getfeatureclass(tempfilesPath + "ClippedParcels.shp");
                IFeatureClass FCRoads = Arclib.Getfeatureclass(tempfilesPath + "sderoadstopolygon.shp");

                IQueryFilter pqf = new QueryFilterClass();
                pqf.WhereClause = "wire_center_id = 'DNVRCONO' and serving_terminal not like 'ONT%'";
                IFeatureCursor ifccursor = ifc.Search(pqf, false);
                IFeature feat = null;
                int latindex = ifc.FindField("LAT");
                int lonindex = ifc.FindField("LON");
                int speedindex = ifc.FindField("MOD_SPEED_DOWN");
                //int objectindex = ifc.FindField("LUID");
                //int parcelindex = actualparcels.FindField("OBJECTID");
                List<CopperPolygonClass> lis = new List<CopperPolygonClass>();
                IGeometry buffergeo = null;
                IRelationalOperator reationoperator = null;
                //living unit cursor
                while ((feat = ifccursor.NextFeature()) != null)
                {
                    string lat = feat.get_Value(latindex).ToString();
                    string lon = feat.get_Value(lonindex).ToString();
                    string speed = feat.get_Value(speedindex).ToString();
                    // string objectid = feat.get_Value(objectindex).ToString();
                    ISpatialFilter spatialFilter1 = new SpatialFilterClass();
                    spatialFilter1.Geometry = feat.Shape;
                    spatialFilter1.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                    spatialFilter1.GeometryField = actualparcels.ShapeFieldName;
                    IFeatureCursor parcelcursor = actualparcels.Search(spatialFilter1, false);
                    IFeature parcelfeat = null;
                    while ((parcelfeat = parcelcursor.NextFeature()) != null)//
                    {
                        //int parcelid = Convert.ToInt32(feat.get_Value(parcelindex));
                        //to get the intersected road of selected parcel
                        ISpatialFilter spatialRoad = new SpatialFilterClass();
                        spatialRoad.Geometry = parcelfeat.Shape;
                        spatialRoad.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                        spatialRoad.GeometryField = FCRoads.ShapeFieldName;
                        IFeatureCursor FCroadCursor = FCRoads.Search(spatialRoad, false);
                        IFeature proadfeature = null;
                        int sum = 0;
                        while ((proadfeature = FCroadCursor.NextFeature()) != null)
                        {
                            sum++;
                            if (sum > 1) break;
                            reationoperator = proadfeature.Shape as IRelationalOperator;
                        }

                        if (sum == 1)
                        {
                            //getting the polygons with in the buffer
                            buffergeo = Getbuffergeom(parcelfeat.Shape as IGeometry);
                            ISpatialFilter spatialFilter = new SpatialFilterClass();
                            spatialFilter.Geometry = buffergeo;
                            spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelContains;
                            spatialFilter.GeometryField = FcParcels.ShapeFieldName;
                            IFeatureCursor cursor1 = FcParcels.Search(spatialFilter, false);

                            List<IPolygon> list = new List<IPolygon>();
                            IFeature pfeature = null;
                            while ((pfeature = cursor1.NextFeature()) != null)
                            {
                                if (reationoperator.Contains(pfeature.Shape))
                                    list.Add(pfeature.Shape as IPolygon);
                            }
                            IPolygon ipo = Arclib.Unionpolygon(list);
                            lis.Add(new CopperPolygonClass { PolyGeometry = ipo, Bandwidth = speed, LU_Lat = lat, LU_Lon = lon });
                        }
                        FCroadCursor.Flush(); Marshal.ReleaseComObject(FCroadCursor);
                    }
                    parcelcursor.Flush(); Marshal.ReleaseComObject(parcelcursor);//dd
                }
                ifccursor.Flush(); Marshal.ReleaseComObject(ifccursor);
                // Arclib.Createshapefile(lis, tempfilesPath + "outputparcels4.shp", false);
            }
            catch (Exception)
            {

                throw;
            }
        }
        public List<CopperPolygonClass> CreateBasicCopperPolygon()
        {
            List<CopperPolygonClass> copperpoly = new List<CopperPolygonClass>();
            Jobs jobs = null;
            try
            {
                jobs = new Jobs();
                IFeatureClass FCLivingunits = jobs.GetFCfromSDE(configKey.Default.Livingunits, jobs.LoadSDEswgisProperties());
                IFeatureClass FcParcels = Getfeatureclass(@"C:\SQM_Logs\Shape\Parcels.shp");
                IQueryFilter pqf = new QueryFilterClass();
                pqf.WhereClause = "wire_center_id = 'DNVRCONO' and serving_terminal not like 'ONT%'";
                IFeatureCursor ifccursor = FCLivingunits.Search(pqf, false);
                IFeature feat = null;
                int latindex = FCLivingunits.FindField("LAT");
                int lonindex = FCLivingunits.FindField("LON");
                int speedindex = FCLivingunits.FindField("MOD_SPEED_DOWN");
                int parcelindex = FcParcels.FindField("OBJECTID");
                //living unit cursor
                while ((feat = ifccursor.NextFeature()) != null)
                {
                    string lat = feat.get_Value(latindex).ToString();
                    string lon = feat.get_Value(lonindex).ToString();
                    string speed = feat.get_Value(speedindex).ToString();

                    ISpatialFilter spatialFilter1 = new SpatialFilterClass();
                    spatialFilter1.Geometry = feat.Shape;
                    spatialFilter1.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                    spatialFilter1.GeometryField = FcParcels.ShapeFieldName;
                    IFeatureCursor parcelcursor = FcParcels.Search(spatialFilter1, false);
                    IFeature parcelfeat = null;
                    while ((parcelfeat = parcelcursor.NextFeature()) != null)//
                    {
                        int parcelid = parcelfeat.OID;
                        copperpoly.Add(new CopperPolygonClass { PolyGeometry = parcelfeat.Shape, Bandwidth = speed, LU_Lat = lat, LU_Lon = lon, Parcel_ID = parcelid });
                    }
                    parcelcursor.Flush(); Marshal.ReleaseComObject(parcelcursor);
                }
                //ifccursor.Flush(); 
                Marshal.ReleaseComObject(ifccursor);
                //  Arclib.Createshapefile(lis, tempfilesPath + "outputparcels3.shp", false);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            //IPropertySet propertySet = new ESRI.ArcGIS.esriSystem.PropertySetClass();

            //propertySet.SetProperty(Constants.DBProperties.server, configKey.Default.Qual_Server); //"172.16.8.144"
            //propertySet.SetProperty(Constants.DBProperties.instance, configKey.Default.Qual_INSTANCE);//"sde:oracle11g:auroradb");
            //propertySet.SetProperty(Constants.DBProperties.authMode, configKey.Default.AUTHENTICATION_MODE);// "DBMS");
            //propertySet.SetProperty(Constants.DBProperties.database, configKey.Default.Qual_DATABASE);// "auroradb");
            //propertySet.SetProperty(Constants.DBProperties.user, configKey.Default.Qual_USER_SWGISLOC);//"SWGISLOC");
            //propertySet.SetProperty(Constants.DBProperties.password, configKey.Default.Qual_PASSWORD_SWGISLOC);//"SWGISLOC");
            //propertySet.SetProperty(Constants.DBProperties.version, configKey.Default.VERSION);//"sde.DEFAULT");

            //IFeatureClass copperSQM = jobs.GetFCfromSDE("QUAL_SCHEMA.COPPER_SQM", propertySet);
            //Arclib.InsertCopperintoODS(copperSQM, copperpoly);

            return copperpoly;
        }
        public List<IPoint> Getbufferpoints(List<CopperPolygonClass> cooperlist)
        {
            IFeatureClass fcpoints = Getfeatureclass(@"C:\SQM_Logs\points.shp");
            List<IPoint> points = new List<IPoint>();
            try
            {


                foreach (var item in cooperlist)
                {
                    ISpatialFilter spatialFilter = new SpatialFilterClass();
                    spatialFilter.Geometry = item.PolyGeometry;
                    spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                    spatialFilter.GeometryField = fcpoints.ShapeFieldName;
                    IFeatureCursor cursor1 = fcpoints.Search(spatialFilter, false);
                    IFeature feature = null;
                    while ((feature = cursor1.NextFeature()) != null)
                    {
                        points.Add((IPoint)feature.Shape);
                    }
                    cursor1.Flush();
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
            return points;
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
                int polygoncount = convexHullFC.FeatureCount(null);
                while ((feature = featureCursor.NextFeature()) != null)
                {
                    try
                    {
                        int oid = feature.OID;
                        string designationid = feature.get_Value(polygonindex).ToString(); 
                        IPointCollection igeocillection = new PolygonClass();
                        igeocillection.AddPointCollection(feature.Shape as IPointCollection);
                        IArea pa = (igeocillection as IPolygon) as IArea;
                        double aaaa = pa.Area;

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
                            //code for avg approach
                            double avg = 0.0;
                            for (int i = 0; i < segCollection.Count; i++)
                            {
                                IArea parea = segCollection[i] as IArea;
                                double area = avg + parea.Area;
                                avg = area;
                            }
                            avg = avg / segCollection.Count;
                            for (int i = 0; i < segCollection.Count; i++)
                            {
                                IArea parea = segCollection[i] as IArea;
                                double totalRoadPolyGeomArea = parea.Area;
                                //AvgRoadAreaMultiFactor
                                double AvgRoadAreaMultiFactor = Convert.ToDouble(configKey.Default.AvgRoadAreaMultiFactor);
                                if (totalRoadPolyGeomArea < AvgRoadAreaMultiFactor * avg) 
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
                                double BufferVal = Math.Round(Convert.ToDouble(configKey.Default.ServiceBufferValue) / (3280.8 * 100), 5);
                                IGeometry _300BuffconHull = (ConvexHUllPolygon as ITopologicalOperator).Buffer(BufferVal);
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
                        IPolygon unionpoly = Unionpolygon(newPolylist); 

                        if (unionpoly.IsEmpty)
                        {
                            jobTask.designationLst[designationid] = new List<string> { JobPolygonStatusProgressEnum.designationFailed.ToString(), Constants.Messages.PolygonFailureErrorMessage };
                            continue;
                        }
                        IGeometry igeo = GetPolygonExteriorring((IPolygon4)unionpoly);
                        if (isConstr)
                        {
                            string[] splitconsdesi = designationid.Split(':');
                            string query = "select DISTINCT SVC_DELIVERY_TYPE from " + configKey.Default.SitesandJobs + " where JOB_NUM IN(Select cl_last_modify_project from " +
                               configKey.Default.CopperLineofCountTable + " where designation = '" + splitconsdesi[0] + "' and cl_last_modify_project is Not Null)";
                            Exception ex = null;
                            DbWorker worker = new DbWorker();
                            DataTable QualTypefrmGpon = worker.ReadTable(query, out ex);
                            bool isbiw = false;
                            string oltbandwidth = string.Empty;
                            oltbandwidth = CalculateBandwidth(splitconsdesi[0], oltbandwidth); //calculate bancdwidth...
                            string oltValue = string.Empty;
                            string bandwidthvalue = string.Empty;
                            if (oltbandwidth != string.Empty)
                            {
                                string[] splitoltbandwith = oltbandwidth.Split(',');
                                oltValue = splitoltbandwith[0];
                                bandwidthvalue = splitoltbandwith[1];
                            }
                            string availdate = DateTime.Now.ToString("dd-MM-yyyy"); 
                            int qulType = 0; //jobTask.qualType;
                            if (QualTypefrmGpon.Rows.Count == 0)
                                jobTask.designationLst[designationid] = new List<string> { JobPolygonStatusProgressEnum.designationFailed.ToString(), "Not able to featch the Qualtype from GPON" };
                            else
                            {
                                foreach (DataRow item in QualTypefrmGpon.Rows)
                                {
                                    if (item[0].ToString() == "BIW")
                                    {
                                        isbiw = true;
                                        qulType = Constants.QualType.BIW;
                                        break;
                                    }
                                }
                                if (isbiw == false)
                                    qulType = Constants.QualType.GPON;
                                li.Add(new ServicePolygonClass
                                {
                                    BANDWIDTH = bandwidthvalue,
                                    OLT_RELATIONSHIP = oltValue,
                                    SERVING_WIRE_CENTER_NAME = jobTask.wireCenterName,
                                    STATUS = Constants.QualStatus.InConstruction,
                                    TYPE = qulType,
                                    FIBER_CABLE_ID = splitconsdesi[0],
                                    polyGeometry = igeo,
                                    AVAILABILITY_DATE = availdate,
                                    NDS_JOB_NO = splitconsdesi[1],
                                    SERVIING_WIRE_CENTER_CLLI = jobTask.wireCenterID,
                                    Polygon_Status = (int)JobPolygonStatusProgressEnum.Polygon_Creation_Completed
                                });
                                ProcessClass.constructionDesignations[splitconsdesi[0]] = "Processed";
                                if (oltbandwidth != string.Empty)
                                    jobTask.designationLst[designationid] = new List<string> { JobPolygonStatusProgressEnum.designationSuccess.ToString(), "" };
                                else
                                    jobTask.designationLst[designationid] = new List<string> { JobPolygonStatusProgressEnum.deignationException.ToString(), "Not able to find bandwidth" };
                            }
                        }
                        else
                        {
                            string query = "select DISTINCT SVC_DELIVERY_TYPE from " + configKey.Default.SitesandJobs + " where JOB_NUM IN(Select cl_last_modify_project from " +
                               configKey.Default.CopperLineofCountTable + " where designation = '" + designationid + "' and cl_last_modify_project is Not Null)";
                            Exception ex = null;
                            DbWorker worker = new DbWorker();
                            DataTable QualTypefrmGpon = worker.ReadTable(query, out ex);
                            bool isbiw = false;
                            if (QualTypefrmGpon.Rows.Count == 0)
                                jobTask.designationLst[designationid] = new List<string> { JobPolygonStatusProgressEnum.designationFailed.ToString(), "Not able to featch the Qualtype from GPON" };
                            else
                            {
                                string oltbandwidth = string.Empty;
                                oltbandwidth = CalculateBandwidth(designationid, oltbandwidth); //calculate bancdwidth...
                                string oltValue = string.Empty;
                                string bandwidthvalue = string.Empty;
                                if (oltbandwidth != string.Empty)
                                {
                                    string[] splitoltbandwith = oltbandwidth.Split(',');
                                    oltValue = splitoltbandwith[0];
                                    bandwidthvalue = splitoltbandwith[1];
                                }
                                foreach (DataRow item in QualTypefrmGpon.Rows)
                                {
                                    if (item[0].ToString() == "BIW")
                                    {
                                        isbiw = true;
                                        li.Add(new ServicePolygonClass
                                        {
                                            BANDWIDTH = bandwidthvalue,
                                            OLT_RELATIONSHIP = oltValue,
                                            SERVING_WIRE_CENTER_NAME = jobTask.wireCenterName,
                                            TYPE = Constants.QualType.BIW,// jobTask.qualType,
                                            STATUS = Constants.QualStatus.InService,
                                            FIBER_CABLE_ID = designationid,
                                            polyGeometry = igeo,
                                            SERVIING_WIRE_CENTER_CLLI = jobTask.wireCenterID,
                                            Polygon_Status = (int)JobPolygonStatusProgressEnum.Polygon_Creation_Completed,
                                        });
                                        break;
                                    }
                                }
                                if (isbiw == false)
                                {

                                    li.Add(new ServicePolygonClass
                                    {
                                        BANDWIDTH = bandwidthvalue,
                                        OLT_RELATIONSHIP = oltValue,
                                        SERVING_WIRE_CENTER_NAME = jobTask.wireCenterName,
                                        TYPE = Constants.QualType.GPON,
                                        STATUS = Constants.QualStatus.InService,
                                        FIBER_CABLE_ID = designationid,
                                        polyGeometry = igeo,
                                        SERVIING_WIRE_CENTER_CLLI = jobTask.wireCenterID,
                                        Polygon_Status = (int)JobPolygonStatusProgressEnum.Polygon_Creation_Completed,

                                    });
                                }
                                //Mark polygon as Success
                                if (oltbandwidth != string.Empty)
                                {
                                    jobTask.designationLst[designationid] = new List<string> { JobPolygonStatusProgressEnum.designationSuccess.ToString(), "" };

                                }
                                else
                                {
                                    jobTask.designationLst[designationid] = new List<string> { JobPolygonStatusProgressEnum.deignationException.ToString(), "Not able to find bandwidth" };

                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }
                Marshal.ReleaseComObject(featureCursor);
                if (System.IO.Directory.GetFiles(tempfilesPath, "Point.shp").Length != 0)
                {
                    IFeatureClass ipointco = Getfeatureclass(tempfilesPath + "point.shp");
                    Addpointcol(ipointco, roads_Polygon, li);
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

        private string CalculateBandwidth(string designationid, string oltbandwidth)
        {
            try
            {
                Exception ex = null;
                DbWorker worker = new DbWorker();
                DataTable dt = worker.ReadTable("select distinct SUBSTR(name,INSTR(name,REGEXP_SUBSTR( name, '\\d[a-zA-Z0-9\\s]+'),1),LENGTH(name)) as name " +
    "from " + configKey.Default.mit_terminal_enclosure + " WHERE fk_mit_structure_point IN (Select DISTINCT MIT_STRUCTURE_POINT_ID from " + configKey.Default.MIT_CABLE + " where sheath_with_loc_id in ( " +
    "SELECT DISTINCT fk_sheath_with_loc  FROM " + configKey.Default.CopperLineofCountTable +
    " where fk_sheath_with_loc in  (Select ID from " + configKey.Default.sheath_with_loc + " where fk_cl_wire_center = '" + jobTask.wireCenterID + "') " +//1572328488445168705
    "AND DESIGNATION in ('" + designationid + "')) AND MIT_STRUCTURE_POINT_ID IS NOT NULL) AND fk_cl_wire_center = '" + jobTask.wireCenterID + "' AND name LIKE 'FSAI%'", out ex);
                //bool isFSAIToOLTQueryAvailable = false;
                if (dt.Rows.Count == 1)
                {
                    string name = dt.Rows[0][0].ToString();
                    //int count = 0;
                    if (name != null && name != "")
                    {
                        //assuming Querytrace is not available../ Returns no records.
                        worker = new DbWorker();
                        DataTable dtFSAIBandwidth = worker.ReadTable("select distinct f1_term,olt_clli,download_Speed from gpon.gpon_ont_term_info@GPON_PROD_LINK where f1_term like '%" + name + "'", out ex);
                        for (int i = 0; i < dtFSAIBandwidth.Rows.Count; i++)
                        {
                            if (dtFSAIBandwidth.Rows[i][2].ToString() != "" && dtFSAIBandwidth.Rows[i][1].ToString() != "")
                            {
                                string fsaivalue = dtFSAIBandwidth.Rows[i][0].ToString();
                                string OltValue = dtFSAIBandwidth.Rows[i][1].ToString();
                                string bandwidthvalue = dtFSAIBandwidth.Rows[i][2].ToString();
                                oltbandwidth = OltValue + "," + bandwidthvalue;
                                return oltbandwidth;
                            }
                        }
                        if (dtFSAIBandwidth.Rows.Count == 0)
                            jobTask.designationLst[designationid] = new List<string> { JobPolygonStatusProgressEnum.deignationException.ToString(), "not able to calc the band with multiple FSAIs..." };
                        if (dtFSAIBandwidth.Rows.Count != 0 && string.IsNullOrEmpty(oltbandwidth))
                        {
                            string bandwidthvalue = string.Empty;
                            string OltValue = string.Empty;
                            string fsaivalue = string.Empty;
                            for (int i = 0; i < dtFSAIBandwidth.Rows.Count; i++)
                            {
                                if (dtFSAIBandwidth.Rows[i][2].ToString() != "")
                                {
                                     fsaivalue = dtFSAIBandwidth.Rows[i][0].ToString();
                                    bandwidthvalue = dtFSAIBandwidth.Rows[i][2].ToString();
                                    oltbandwidth =","+ bandwidthvalue;
                                }
                                if (dtFSAIBandwidth.Rows[i][1].ToString() != "")
                                {
                                    fsaivalue = dtFSAIBandwidth.Rows[i][0].ToString();
                                    OltValue = dtFSAIBandwidth.Rows[i][1].ToString();
                                    oltbandwidth = OltValue+",";
                                }
                            }
                            if (string.IsNullOrEmpty(bandwidthvalue))
                                jobTask.designationLst[designationid] = new List<string> { JobPolygonStatusProgressEnum.deignationException.ToString(), "unable to find the bandwidth for FSAI :"+ fsaivalue };
                            if (string.IsNullOrEmpty(OltValue))
                                jobTask.designationLst[designationid] = new List<string> { JobPolygonStatusProgressEnum.deignationException.ToString(), "unable to find the olt for FSAI :" + fsaivalue };
                            return oltbandwidth;
                        }
                    }

                    else
                    {
                        //obtain tne bandwidth info from trace query.
                        //isFSAIToOLTQueryAvailable = true;
                    }
                }
                else if (dt.Rows.Count > 1)
                {
                    //exception not able to calc the band with multiple FSAIs...
                    jobTask.designationLst[designationid] = new List<string> { JobPolygonStatusProgressEnum.deignationException.ToString(), "not able to calc the band with multiple FSAIs..." };

                }
                else if (dt.Rows.Count == 0)
                {
                    worker = new DbWorker();
                    DataTable dtCA = worker.ReadTable("select distinct ca from gpon.gpon_ont_term_info@GPON_PROD_LINK where ca='" + designationid + "'", out ex);
                    if (dtCA.Rows.Count == 1)
                    {
                        string caName = dtCA.Rows[0][0].ToString();
                        worker = new DbWorker();
                        DataTable dtCAIBandwidth = worker.ReadTable("select distinct f1_term,olt_clli,download_Speed from gpon.gpon_ont_term_info@GPON_PROD_LINK where ca ='" + caName + "'", out ex);
                        for (int i = 0; i < dtCAIBandwidth.Rows.Count; i++)
                        {
                            if (dtCAIBandwidth.Rows[i][2].ToString() != "" && dtCAIBandwidth.Rows[i][1].ToString() != "")
                            {
                                string fsaivalue = dtCAIBandwidth.Rows[i][0].ToString();
                                string OltValue = dtCAIBandwidth.Rows[i][1].ToString();
                                string bandwidthvalue = dtCAIBandwidth.Rows[i][2].ToString();
                                oltbandwidth = OltValue + "," + bandwidthvalue;
                                return oltbandwidth;
                            }
                        }

                    }
                    else if (dtCA.Rows.Count == 0)
                    {
                        //cannot obtain the bandwidth information
                        jobTask.designationLst[designationid] = new List<string> { JobPolygonStatusProgressEnum.deignationException.ToString(), "cannot obtain the bandwidth information" };
                    }
                    else if (dtCA.Rows.Count > 1)
                    {

                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
            return oltbandwidth;
        }

        private static void ProcessPolygonWith300ftRule(IFeature feature, List<IPolygon> segCollection, List<IPolygon> newPolylist)
        {
            ITopologicalOperator ptopConvex = feature.Shape as ITopologicalOperator;
            IGeometry ConvexHUllPolygon = ptopConvex.ConvexHull();
            IBufferConstruction pbf = new BufferConstructionClass();
            double BufferVal = Math.Round(Convert.ToDouble(configKey.Default.ServiceBufferValue) / (3280.8 * 100), 5);
            IGeometry pbufGeom = pbf.Buffer(ConvexHUllPolygon, BufferVal);
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
                    IGeometry buffergeo = itopo.Buffer(0.01); //Expanding WC Boundary to 500 mt
                    IPointCollection pcol = new PolylineClass();
                    pcol.AddPointCollection(buffergeo as IPointCollection);
                    IPolyline line = pcol as IPolyline;
                    li.Add(new ServicePolygonClass { polyGeometry = line as IGeometry });
                }

                Createshapefile(li, tempfilesPath + "WCBoundaryLine.shp", false);
                ifc = Getfeatureclass(tempfilesPath + "WCBoundaryLine.shp");
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
                List<IPolygon> li = new List<IPolygon>();
                foreach (var item in servicePolyList)
                {
                    if (li.Count > 0) li.Clear();
                    ITopologicalOperator topologicalOperator = item.polyGeometry as ITopologicalOperator;
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
            
            List<string> uniqueids = DataStatistics(featureClass, "DESIGNATION");
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
            List<string> largedesigsConvex = new List<string>();
            try
            {
                foreach (var item in uniqueids) 
                {
                    currentJob.designationLst.Add(item, new List<string> { JobPolygonStatusProgressEnum.deignationCreated.ToString(), "" });
                    string[] desigSplit = item.Split(':');
                    if (!ProcessClass.constructionDesignations.ContainsKey(desigSplit[0]))
                        ProcessClass.constructionDesignations.Add(desigSplit[0], "Recieved");
                    try
                    {
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
                            {
                                largedesigsConvex.Add(desigSplit[0]);
                                continue;
                            }
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
                        if (are.Area > 0.000100)
                        {
                            largedesigsConvex.Add(desigSplit[0]);
                            continue;
                        }
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
                if (largedesigsConvex.Count > 0)
                {
                    foreach (var largedesiitem in largedesigsConvex)
                        jobTask.designationLst[largedesiitem] = new List<string> { JobPolygonStatusProgressEnum.designationFailed.ToString(), "larger Area" };

                }
                //creating shapefiles
                if (li.Count > 0)
                    Createshapefile(li, tempfilesPath + "convex.shp", false);
                if (lipoint.Count > 0)
                    Createshapefile(lipoint, tempfilesPath + "point.shp", false);
                if (File.Exists(tempfilesPath + "convex.shp"))
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
            LogManager.WriteLogandConsole("INFO : Generating minimal possible outer boundaries with inputs...");
            List<ServicePolygonClass> li = new List<ServicePolygonClass>();
            List<ServicePolygonClass> lipoint = new List<ServicePolygonClass>();
            int skippedLSt = 0;
            IFeatureClass concavehullpoly = null;
            try
            {
                List<string> largerdesig = new List<string>();
                foreach (var item in currentJob.designationLst)  
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
                        if (pointCollection.PointCount == 0)
                        {
                            skippedLSt++;
                            continue;
                        }
                        if (pointCollection.PointCount == 2)
                        {
                            pointCollection.AddPoint(pointCollection.get_Point(0));
                            IPolygon ipoly = pointCollection as IPolygon;
                            IEnvelope envlope = ipoly.Envelope;
                            IArea area = envlope as IArea;
                            if (area.Area > 0.000150)
                            {
                                //skippedLSt++;
                                largerdesig.Add(item.Key);
                                continue;
                            }
                            li.Add(new ServicePolygonClass { FIBER_CABLE_ID = item.Key.ToString(), polyGeometry = ipoly });
                            continue;
                        }
                        if (pointCollection.PointCount == 1)
                        {
                            //lipoint.Add(new ServicePolygonClass { FIBER_CABLE_ID = item.Key.ToString(), polyGeometry = pointCollection.get_Point(0) });
                            continue;
                        }
                        ITopologicalOperator topologicalOperator = pointCollection as ITopologicalOperator;
                        //Outside buffer
                        IGeometry polygon = topologicalOperator.ConvexHull();
                        IEnvelope env = polygon.Envelope;
                        IArea are = env as IArea;
                        if (are.Area > 0.000150)
                        {
                            // skippedLSt++;
                            //Add failure comment for larger area exception
                            largerdesig.Add(item.Key);
                            continue;
                        }
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
                if (largerdesig.Count > 0)
                {
                    foreach (var largedesiitem in largerdesig)
                        jobTask.designationLst[largedesiitem] = new List<string> { JobPolygonStatusProgressEnum.designationFailed.ToString(), "larger Area" };

                }
                //creating shapefiles
                if (li.Count > 0)
                    Createshapefile(li, tempfilesPath + "convex.shp", false);
                if (lipoint.Count > 0)
                    Createshapefile(lipoint, tempfilesPath + "point.shp", false);
                if (File.Exists(tempfilesPath + "convex.shp"))
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

                //Repairgeom(tempfilesPath + "concav1655.shp");
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
        public List<CopperPolygonClass> Findoverlaps(string inputshapefilepath, List<CopperPolygonClass> copperpolylist, string outputpath)
        {

            try
            {
                List<ServicePolygonClass> union = new List<ServicePolygonClass>();
                IFeatureClass featureClass = Getfeatureclass(inputshapefilepath);
                // IFeatureClass featureClass = Getfeatureclass(@"C:\SQM_Logs\CLT_tempFiles\3675 - Copy (13)\Copperpoly.shp");
                IFeatureCursor featureCursor = featureClass.Search(null, false);
                IFeature feature = null;
                for (int i = 0; i < copperpolylist.Count; i++)
                // while ((feature = featureCursor.NextFeature()) != null)
                {
                    //if (feature.OID != 3559) continue;
                    ISpatialFilter spatialFilter = new SpatialFilterClass();
                    // spatialFilter.Geometry = feature.Shape;
                    spatialFilter.Geometry = copperpolylist[i].PolyGeometry;
                    spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                    spatialFilter.GeometryField = featureClass.ShapeFieldName;
                    IFeatureCursor cursor1 = featureClass.Search(spatialFilter, false);
                    IFeature pfeature = null;
                    // ITopologicalOperator topologicalOperator = feature.Shape as ITopologicalOperator;
                    ITopologicalOperator topologicalOperator = copperpolylist[i].PolyGeometry as ITopologicalOperator;
                    IRelationalOperator rp = copperpolylist[i].PolyGeometry as IRelationalOperator;
                    IGeometry igeo = null;
                    bool isfeature = false;// bool ispfeature = false;
                    while ((pfeature = cursor1.NextFeature()) != null)
                    {
                        // isfeature = false;
                        if (!rp.Equals(pfeature.Shape))
                        {
                            IGeometry geom = topologicalOperator.Intersect(pfeature.Shape, esriGeometryDimension.esriGeometry2Dimension);
                            ISegmentCollection segs = geom as ISegmentCollection;
                            if (segs.SegmentCount == 0) continue;
                            List<GeomList> list = new List<GeomList>();
                            for (int m = 0; m < segs.SegmentCount; m++)
                            {
                                list.Add(new GeomList { val = segs.Segment[m].Length, geom = segs.Segment[m] });//segs.Segment[i].Length
                            }
                            GeomList ge = list.Where(j => j.val == list.Max(m => m.val)).First();
                            isfeature = false;
                            //ISegmentCollection seg1 = pfeature.Shape as ISegmentCollection;
                            //for (int j = 0; j < seg1.SegmentCount; j++)
                            //{
                            //    if (seg1.Segment[j].Length == ge.geom.Length)
                            //    {
                            //        ispfeature = true;
                            //    }
                            //}
                            ISegmentCollection sesg2 = copperpolylist[i].PolyGeometry as ISegmentCollection;
                            for (int k = 0; k < sesg2.SegmentCount; k++)
                            {
                                if (Math.Round(sesg2.Segment[k].Length, 9) == Math.Round(ge.geom.Length, 9))
                                {
                                    isfeature = true;
                                }
                            }
                            if (isfeature)
                            {
                                igeo = topologicalOperator.Difference(pfeature.Shape);
                                topologicalOperator = igeo as ITopologicalOperator;
                            }
                            //else if (ispfeature)
                            //{
                            //    topologicalOperator = pfeature.Shape as ITopologicalOperator;
                            //    igeo = topologicalOperator.Difference(copperpolylist[i].PolyGeometry);
                            //    topologicalOperator = igeo as ITopologicalOperator;
                            //}
                        }
                    }
                    if (igeo == null) continue;
                    if (isfeature)
                    {
                        ServicePolygonClass igeos = new ServicePolygonClass { polyGeometry = igeo as IPolygon };
                        union.Add(igeos);
                        copperpolylist[i].PolyGeometry = igeo;
                    }
                    cursor1.Flush();
                    Marshal.ReleaseComObject(cursor1);
                    // Marshal.ReleaseComObject(topologicalOperator);
                    // Marshal.ReleaseComObject(rp);
                }
                // featureCursor.Flush();
                // Marshal.ReleaseComObject(featureCursor);
                // Createshapefile(union, @"C:\Centurylink\27\checkoverlap.shp", false);
                if (union.Count != 0)
                {
                    Createshapefile(union, outputpath, false);

                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return copperpolylist;
        }
        public void Findoverlapss()
        {

            try
            {
                List<ServicePolygonClass> union = new List<ServicePolygonClass>();
                IFeatureClass featureClass = Getfeatureclass(@"C:\SQM_Logs\CLT_tempFiles\3675 - Copy (14)\Copperpoly\Copperpoly.shp");
                IFeatureCursor featureCursor = featureClass.Search(null, false);
                IFeature feature = null;
                while ((feature = featureCursor.NextFeature()) != null)
                {
                    if (feature.OID != 2708) continue;
                    ISpatialFilter spatialFilter = new SpatialFilterClass();
                    spatialFilter.Geometry = feature.Shape;
                    spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                    spatialFilter.GeometryField = featureClass.ShapeFieldName;
                    IFeatureCursor cursor1 = featureClass.Search(spatialFilter, false);
                    IFeature pfeature = null;
                    ITopologicalOperator topologicalOperator = feature.Shape as ITopologicalOperator;
                    IGeometry igeo = null;
                    while ((pfeature = cursor1.NextFeature()) != null)
                    {
                        if (pfeature.OID == feature.OID) continue;
                        IGeometry geom = topologicalOperator.Intersect(pfeature.Shape, esriGeometryDimension.esriGeometry2Dimension);
                        ISegmentCollection segs = geom as ISegmentCollection;
                        if (segs.SegmentCount == 0) continue;
                        List<GeomList> list = new List<GeomList>();
                        for (int i = 0; i < segs.SegmentCount; i++)
                        {
                            list.Add(new GeomList { val = segs.Segment[i].Length, geom = segs.Segment[i] });//segs.Segment[i].Length
                        }
                        GeomList ge = list.Where(j => j.val == list.Max(i => i.val)).First();
                        bool isfeature = false; bool ispfeature = false;
                        //ISegmentCollection seg1 = pfeature.Shape as ISegmentCollection;
                        //for (int j = 0; j < seg1.SegmentCount; j++)
                        //{
                        //    if (seg1.Segment[j].Length == ge.geom.Length)
                        //    {
                        //        ispfeature = true;
                        //    }
                        //}
                        ISegmentCollection sesg2 = feature.Shape as ISegmentCollection;
                        for (int k = 0; k < sesg2.SegmentCount; k++)
                        {
                            double va = Math.Round(sesg2.Segment[k].Length, 10);
                            double val = Math.Round(ge.geom.Length);
                            if (Math.Round(sesg2.Segment[k].Length, 12) == Math.Round(ge.geom.Length, 12))
                            {
                                isfeature = true;
                            }
                        }
                        if (isfeature)
                        {
                            igeo = topologicalOperator.Difference(pfeature.Shape);
                            topologicalOperator = igeo as ITopologicalOperator;
                        }
                        //else if (ispfeature)
                        //{
                        //    topologicalOperator = pfeature.Shape as ITopologicalOperator;
                        //    igeo = topologicalOperator.Difference(feature.Shape);
                        //    topologicalOperator = igeo as ITopologicalOperator;
                        //}

                    }
                    ServicePolygonClass igeos = new ServicePolygonClass { polyGeometry = igeo as IPolygon, FIBER_CABLE_ID = feature.OID.ToString() }; union.Add(igeos);
                    cursor1.Flush();
                    Marshal.ReleaseComObject(cursor1);
                }
                featureCursor.Flush();
                Marshal.ReleaseComObject(featureCursor);
                Createshapefile(union, @"C:\Centurylink\27\checkoverlap.shp", false);

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
               // fcLND_Address.AliasName;
                IQueryFilter queryfilter = new QueryFilterClass();
                queryfilter.WhereClause = "CL_HOUSE_NAME ='No Build' and (CITY_NAME = '" + currentJob.wireCenterName.ToUpper() + "' or CITY_NAME = '" + currentJob.wireCenterName + "')";
                IFeatureCursor pntcursor = fcLND_Address.Search(queryfilter, false);
                IFeatureCursor parcelcursor = null;
                int featcount = fcLND_Address.FeatureCount(queryfilter);
                IFeature feature = null;
                int ind = fcLND_Address.FindField("ID");
                List<NoBuild> noBuildGeomList = new List<NoBuild>();
                ITopologicalOperator itopo = null;
                while ((feature = pntcursor.NextFeature()) != null)
                {
                    //int oid = feature.OID;
                    //if (oid==67095)
                    //{

                    //}
                    int valu = Convert.ToInt32(feature.get_Value(ind));
                    ISpatialFilter spatialFilter = new SpatialFilterClass();
                    spatialFilter.Geometry = feature.Shape;
                    spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                    spatialFilter.GeometryField = fcParcel.ShapeFieldName;
                    parcelcursor = fcParcel.Search(spatialFilter, false);
                    IFeature ift = null;
                    int sum = 0;
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
                        IGeometry nobuildBuffPolygon = itopo.Buffer(0.0001); //36ft 
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

                        //check nobuildBuffPolygon intersects with the parcel 
                        ISpatialFilter pParcelsp = new SpatialFilterClass();
                        pParcelsp.Geometry = nobuildBuffPolygon;
                        pParcelsp.GeometryField = RoadsPolygons.ShapeFieldName;
                        pParcelsp.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;

                        IFeatureCursor pParcelcur = RoadsPolygons.Search(psp, false);
                        IFeature pparcelFeat = null;
                        while ((pparcelFeat = pParcelcur.NextFeature()) != null)
                        {
                            ITopologicalOperator pParceltopo = pparcelFeat.Shape as ITopologicalOperator;
                            IGeometry ParcelNobuildPoly = pParceltopo.Intersect(nobuildBuffPolygon, esriGeometryDimension.esriGeometry2Dimension);
                            noBuildGeomList.Add(new NoBuild
                            {
                                WC_ID = jobTask.wireCenterID,
                                Addr_id = ind,
                                // TYPE = currentJob.qualType.ToString(),
                                Createmode = "Automatic",
                                STATUS = currentJob.qualStatus.ToString(),
                                REASON = "",
                                GEOEMTRY = ParcelNobuildPoly
                            });

                        }
                        Marshal.ReleaseComObject(pParcelcur);
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
