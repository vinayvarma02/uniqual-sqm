using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.Geoprocessor;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.DataManagementTools;
using ESRI.ArcGIS.Geoprocessing;
using ESRI.ArcGIS.AnalysisTools;
using System.Diagnostics;
using static CenturyLink_ArcApp.Program;
using System.Linq;
using System.IO;
using System.Text;
using DbLibrary;
using configKey = CenturyLink_ArcApp.Properties.Settings;

namespace CenturyLink_ArcApp
{
    public static class Arclib
    {
        public static Geoprocessor gp = null;

        /// <summary>
        /// Checking the point is in polygon or not.
        /// </summary>
        /// <param name="segCollection"></param>
        /// <param name="ipo"></param>
        /// <returns></returns>
        static Arclib()
        {
            gp = new Geoprocessor();

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="segCollection"></param>
        /// <param name="ipo"></param>
        /// <returns></returns>
        public static List<IPolygon> Getintersectingpolygon(List<IPolygon> segCollection, IPointCollection ipo)
        {
            List<IPolygon> _List = new List<IPolygon>();
            bool value = false;
            try
            {
                for (int i = 0; i < segCollection.Count; i++)
                {
                    IRelationalOperator relationalOperator = segCollection[i] as IRelationalOperator;
                    for (int j = 0; j < ipo.PointCount; j++)
                    {
                        value = relationalOperator.Contains(ipo.Point[j]);
                        if (value)
                        {
                            _List.Add(segCollection[i]);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return _List;
        }
        public static List<IPolygon> Getintersectingpolygon(List<IPolygon> segCollection, List<ServicePolygonClass> ipo, string id)
        {
            List<ServicePolygonClass> intersected = new List<ServicePolygonClass>();
            List<IPolygon> lis = new List<IPolygon>();
            bool value = false;
            try
            {
                for (int i = 0; i < segCollection.Count; i++)
                {
                    IRelationalOperator relationalOperator = segCollection[i] as IRelationalOperator;
                    for (int j = 0; j < ipo.Count; j++)
                    {
                        value = relationalOperator.Contains(ipo[j].polyGeometry as IPoint);
                        if (value)
                        {
                            intersected.Add(new ServicePolygonClass { polyGeometry = segCollection[i], FIBER_CABLE_ID = ipo[j].FIBER_CABLE_ID });
                        }
                    }
                    List<string> li = intersected.Select(x => x.FIBER_CABLE_ID).Distinct().ToList();

                    if (!li.Contains(id) && li.Count > 0)
                    {
                        // LogManager.WriteLogandConsole("Overlap solved in : "+id);
                    }
                    else
                    {
                        lis.Add(segCollection[i]);
                    }
                    intersected.Clear();
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
                isExecutionResult = false;
            }
            return lis;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="segCollection"></param>
        /// <param name="ipo"></param>
        /// <returns></returns>
        public static bool IsPolygonIntersected(List<IPolygon> segCollection, IPoint ipo)
        {

            bool value = false;
            try
            {
                for (int i = 0; i < segCollection.Count; i++)
                {
                    IRelationalOperator relationalOperator = segCollection[i] as IRelationalOperator;

                    value = relationalOperator.Contains(ipo);
                    if (value) break;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return value;
        }
        public static void GetConnection()
        {
            try
            {
                ESRI.ArcGIS.esriSystem.IPropertySet propertySet = new ESRI.ArcGIS.esriSystem.PropertySetClass();
                propertySet.SetProperty("SERVER", "172.16.8.144");
                propertySet.SetProperty("INSTANCE", "sde:oracle11g:auroradb");
                propertySet.SetProperty("AUTHENTICATION_MODE", "DBMS");
                propertySet.SetProperty("DATABASE", "auroradb");
                propertySet.SetProperty("USER", "SWGISLOC");
                propertySet.SetProperty("PASSWORD", "SWGISLOC");
                // propertySet.SetProperty("VERSION", "sde.DEFAUALT");
                IWorkspaceFactory workspaceFactory = new SdeWorkspaceFactory();
                IWorkspace workspace = workspaceFactory.Open(propertySet, 0);
                IFeatureWorkspace destinationWorkspace = workspace as IFeatureWorkspace;//(IFeatureWorkspace)workspaceFactory.OpenFromFile("gettterminal", 0);
                IFeatureClass featureClass = destinationWorkspace.OpenFeatureClass("SWGISLOC.GETTTERMINAL");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static IFeatureClass GetSdeFeatureClass(string featureClassNae)
        {

            IFeatureClass featureClass = null;
            IWorkspaceFactory workspaceFactory = null;
            IFeatureWorkspace destinationWorkspace = null;

            try
            {
                workspaceFactory = new SdeWorkspaceFactory();
                destinationWorkspace = (IFeatureWorkspace)workspaceFactory.OpenFromFile("gettterminal", 0);
                featureClass = destinationWorkspace.OpenFeatureClass(featureClassNae);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Marshal.ReleaseComObject(workspaceFactory);
                Marshal.ReleaseComObject(destinationWorkspace);
            }

            return featureClass;

        }
        public static IGeometry Getbuffergeom(IGeometry geom)
        {
            IGeometry buffergeo = null;
            try
            {
                ITopologicalOperator topologicalOperator = geom as ITopologicalOperator;
                buffergeo = topologicalOperator.Buffer(0.0001);
                if (buffergeo.IsEmpty)
                    buffergeo = geom;
            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
                isExecutionResult = false;
            }
            return buffergeo;
        }
        public static IPolygon Unionpolygon(List<IPolygon> geomcoll)
        {
            ITopologicalOperator pTopo = new PolygonClass();
            IPolygon polygon1 = new PolygonClass();
            IPolygon polygon2 = new PolygonClass();
            IPolygon UnionPolygon = new PolygonClass();
            try
            {
                if (geomcoll.Count > 1)
                {
                    for (int g = 0; g < geomcoll.Count; g++)
                    {
                        if (g == 0)
                        {
                            polygon1 = geomcoll[g] as IPolygon;
                            pTopo = polygon1 as ITopologicalOperator;
                        }
                        else
                        {
                            polygon2 = geomcoll[g] as IPolygon;
                            UnionPolygon = pTopo.Union(polygon2) as IPolygon;
                            pTopo = UnionPolygon as ITopologicalOperator;
                        }
                    }
                }
                else
                {
                    UnionPolygon = geomcoll[0] as IPolygon;
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
                isExecutionResult = false;
            }
            finally
            {
                //Marshal.ReleaseComObject(pTopo);
            }
            return UnionPolygon;

        }

        /// <summary>
        /// http://help.arcgis.com/en/sdk/10.0/arcobjects_net/componenthelp/index.html#/ExteriorRingBag_Property/002m000002n0000000/
        /// </summary>
        /// <param name="polygon"></param>
        //public static IGeometry GetPolygonExteriorring(IPolygon4 polygon)
        //{
        //    IGeometryCollection exteriorRingGeometryCollection = new PolygonClass();
        //    IGeometry exteriorRingGeometry = null;
        //    IGeometry pReturnGeometry = null;
        //    object obj = Type.Missing;
        //    try
        //    {
        //        IGeometryBag exteriorRingGeometryBag = polygon.ExteriorRingBag;
        //        exteriorRingGeometryCollection = exteriorRingGeometryBag as IGeometryCollection;
        //        exteriorRingGeometry = exteriorRingGeometryCollection.get_Geometry(0);
        //        if (exteriorRingGeometryCollection.GeometryCount > 1) { }
        //        // you've got this mostly right
        //        IGeometryCollection pPolyGeoCollection = new PolygonClass();
        //        pPolyGeoCollection.AddGeometry(exteriorRingGeometry, ref obj, ref obj);

        //        // now make the geometry collection into a geometry
        //        pReturnGeometry = pPolyGeoCollection as IGeometry;

        //    }
        //    catch (Exception ex)
        //    {
        //        // throw ex;
        //    }
        //    return pReturnGeometry;
        //}


        /// <summary>
        /// GetPolygonExteriorring
        /// http://help.arcgis.com/en/sdk/10.0/arcobjects_net/componenthelp/index.html#/ExteriorRingBag_Property/002m000002n0000000/
        /// </summary>
        /// <param name="polygon"></param>
        public static IGeometry GetPolygonExteriorring(IPolygon4 polygon)
        {
            IGeometryCollection exteriorRingGeometryCollection = new PolygonClass();
            // IGeometry exteriorRingGeometry = null;
            IGeometry pReturnGeometry = null;
            object obj = Type.Missing;
            try
            {
                IGeometryBag exteriorRingGeometryBag = polygon.ExteriorRingBag;
                exteriorRingGeometryCollection = exteriorRingGeometryBag as IGeometryCollection;
                // exteriorRingGeometry = exteriorRingGeometryCollection.get_Geometry(0);
                // if (exteriorRingGeometryCollection.GeometryCount > 1) { }
                // you've got this mostly right
                IGeometryCollection pPolyGeoCollection = new PolygonClass();
                for (int i = 0; i < exteriorRingGeometryCollection.GeometryCount; i++)
                {
                    pPolyGeoCollection.AddGeometry(exteriorRingGeometryCollection.get_Geometry(i), ref obj, ref obj);
                }
                // now make the geometry collection into a geometry
                pReturnGeometry = pPolyGeoCollection as IGeometry;

            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
                isExecutionResult = false;
            }
            return pReturnGeometry;
        }

        private static string PointToString(IPoint point)
        {
            return (point.X + ", " + point.Y + ", " + point.Z);
        }
        public static void PolygonToString(IPolygon4 polygon)
        {
            try
            {
                IGeometryBag exteriorRingGeometryBag = polygon.ExteriorRingBag;
                IGeometryCollection exteriorRingGeometryCollection = exteriorRingGeometryBag as IGeometryCollection;
                Trace.WriteLine("polygon.ExteriorRingCount = " + exteriorRingGeometryCollection.GeometryCount);
                for (int i = 0; i < exteriorRingGeometryCollection.GeometryCount; i++)
                {
                    Trace.WriteLine("polygon.ExteriorRing[" + i + "]");
                    IGeometry exteriorRingGeometry = exteriorRingGeometryCollection.get_Geometry(i);
                    IPointCollection exteriorRingPointCollection = exteriorRingGeometry as IPointCollection;
                    for (int j = 0; j < exteriorRingPointCollection.PointCount; j++)
                    {
                        Trace.WriteLine("Point[" + j + "] = " + PointToString(exteriorRingPointCollection.get_Point(j)));
                    }
                    IGeometryBag interiorRingGeometryBag = polygon.get_InteriorRingBag(exteriorRingGeometry as IRing);
                    IGeometryCollection interiorRingGeometryCollection = interiorRingGeometryBag as IGeometryCollection;
                    Trace.WriteLine("polygon.InteriorRingCount[exteriorRing" + i + "] = " + interiorRingGeometryCollection.GeometryCount);
                    for (int k = 0; k < interiorRingGeometryCollection.GeometryCount; k++)
                    {
                        Trace.WriteLine("polygon.InteriorRing[" + k + "]");
                        IGeometry interiorRingGeometry = interiorRingGeometryCollection.get_Geometry(k);
                        IPointCollection interiorRingPointCollection = interiorRingGeometry as IPointCollection;
                        for (int m = 0; m < interiorRingPointCollection.PointCount; m++)
                        {
                            Trace.WriteLine("Point[" + m + "] = " + PointToString(interiorRingPointCollection.get_Point(m)));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {

            }
        }
        public static IFeatureClass DoConvertlinetopolygon(IFeatureClass inputShape, string name)
        {
            IFeatureClass featClass = null;

            try
            {
                IDataset pset = inputShape as IDataset;
                string path = pset.Workspace.PathName;
                gp.OverwriteOutput = true;
                FeatureToPolygon featureToPolygon = new FeatureToPolygon();
                featureToPolygon.in_features = path + "\\" + inputShape.AliasName + ".shp";
                featureToPolygon.out_feature_class = tempfilesPath + name + ".shp";
                featureToPolygon.attributes = "NO_ATTRIBUTES";
                IGeoProcessorResult result4 = (IGeoProcessorResult)gp.Execute(featureToPolygon, null);
                featClass = Getfeatureclass(tempfilesPath + name + ".shp");

            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
                isExecutionResult = false;
            }
            return featClass;
        }

        public static IFeatureClass FeatureClassToshapeFile(IFeatureClass InputFC)
        {
            IFeatureClass OPFC = null;
            if (!Directory.Exists(tempfilesPath + "\\Roads"))
                Directory.CreateDirectory(tempfilesPath + "\\Roads");
            try
            {
                ESRI.ArcGIS.ConversionTools.FeatureClassToShapefile FCtoShp = new ESRI.ArcGIS.ConversionTools.FeatureClassToShapefile();
                FCtoShp.Input_Features = InputFC;
                FCtoShp.Output_Folder = tempfilesPath + "\\Roads\\";
                gp.OverwriteOutput = true;
                IGeoProcessorResult result4 = (IGeoProcessorResult)gp.Execute(FCtoShp, null);

                string[] filename = Directory.GetFiles(tempfilesPath + "\\Roads\\", "*.shp");

                // string path = tempfilesPath + "\\" + InputFC.AliasName + ".shp";
                OPFC = Getfeatureclass(filename[0]);

                // opPath = tempfilesPath + "\\" + InputFC.AliasName + ".shp";


            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
                isExecutionResult = false;
            }
            return OPFC;

        }
        public static void DeleteduplicateGeom(string shapePath)
        {
            try
            {

                //  gp = new Geoprocessor();
                // Geoprocessor pgeoP = new Geoprocessor();
                gp.OverwriteOutput = true;
                DeleteIdentical delete = new DeleteIdentical();
                delete.in_dataset = shapePath;//plyr;
                // delete.out_dataset = outfc;
                delete.fields = "Shape";
                IGeoProcessorResult result4 = (IGeoProcessorResult)gp.Execute(delete, null);
            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
                isExecutionResult = false;
            }
        }
        public static IFeatureClass DoMerge(IFeatureClass roads, IFeatureClass wcBound)
        {

            IFeatureClass outFC = null;
            string output = string.Empty;
            StringBuilder sb = new StringBuilder();
            sb.Append("'" + tempfilesPath + "ClippedRN.shp" + "'");
            sb.Append(";");
            sb.Append("'" + tempfilesPath + "WCBoundaryLine.shp" + "'");
            try
            {
                // Initialize the geoprocessor.
                gp.OverwriteOutput = true;
                gp.ToolExecuted += new EventHandler<ToolExecutedEventArgs>(Gp_ToolboxChanged);
                gp.MessagesCreated += new EventHandler<MessagesCreatedEventArgs>(Gp_MessagesCreated);
                Merge merge = new Merge();
                merge.inputs = sb.ToString();//"'D:\\Temp\\CLT_tempFiles\\2880\\WCBoundaryLine.shp';'D:\\Temp\\CLT_tempFiles\\2880\\ClippedRN1.shp'";
                merge.output = tempfilesPath + "RoadWCMergeLines.shp";
                IGeoProcessorResult result4 = (IGeoProcessorResult)gp.Execute(merge, null);
                outFC = Getfeatureclass(merge.output.ToString());
            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex); isExecutionResult = false; return null;
            }
            return outFC;
        }
        #region mergecommented
        //public static void merge1()
        //{
        //    Geoprocessor gp = new Geoprocessor();
        //    gp.OverwriteOutput = true;
        //    Merge merge = new Merge();
        //    merge.inputs = "D:\\Roads\\CO_mh.shp;D:\\Roads\\CO_ih.shp";
        //    merge.output = @"D:\Roads\output1.shp";
        //    IGeoProcessorResult result2 = (IGeoProcessorResult)gp.Execute(merge, null);

        //}
        //public static void merge2()
        //{
        //    Geoprocessor gp = new Geoprocessor();
        //    gp.OverwriteOutput = true;
        //    Merge merge = new Merge();
        //    merge.inputs = "D:\\Roads\\CO_sh_major.shp;D:\\Roads\\CO_rp.shp";
        //    merge.output = @"D:\Roads\output2.shp";
        //    IGeoProcessorResult result5 = (IGeoProcessorResult)gp.Execute(merge, null);

        //}
        //public static void merge3()
        //{
        //    Geoprocessor gp = new Geoprocessor();
        //    gp.OverwriteOutput = true;
        //    Merge merge = new Merge();
        //    merge.inputs = "'D:\\Roads\\CO_uh.shp';'D:\\Roads\\CO_sh_minor.shp'";
        //    merge.output = @"D:\Roads\output3.shp";
        //    IGeoProcessorResult result7 = (IGeoProcessorResult)gp.Execute(merge, null);

        //}
        //public static void merge4()
        //{
        //    Geoprocessor gp = new Geoprocessor();
        //    gp.OverwriteOutput = true;
        //    Merge merge = new Merge();
        //    merge.inputs = "D:\\Roads\\output3.shp;D:\\Roads\\output2.shp;D:\\Roads\\output1.shp";
        //    merge.output = @"D:\Roads\outputf.shp";
        //    IGeoProcessorResult result7 = (IGeoProcessorResult)gp.Execute(merge, null);

        //}
        #endregion
        public static void Union()
        {
            // Initialize the geoprocessor.
            Geoprocessor gp = new Geoprocessor();
            gp.OverwriteOutput = true;
            gp.ToolExecuted += new EventHandler<ToolExecutedEventArgs>(Gp_ToolboxChanged);
            gp.MessagesCreated += new EventHandler<MessagesCreatedEventArgs>(Gp_MessagesCreated);
            Union union = new Union();
            union.in_features = @"C:\Users\sp15226\Documents\My Received Files\final.shp";
            union.out_feature_class = @"D:\Roads\ddsfsfs.shp";
            union.gaps = "NO_GAPS";
            IGeoProcessorResult result4 = (IGeoProcessorResult)gp.Execute(union, null);
        }

        public static void Intersecting()
        {
            // Initialize the geoprocessor.
            Geoprocessor gp = new Geoprocessor();
            gp.OverwriteOutput = true;

            Intersect intersect = new Intersect();
            intersect.in_features = @"D:\CenturyOutput\FinalBoundarybuff.shp";
            intersect.out_feature_class = @"D:\CenturyOutput\intersecdata.shp";
            IGeoProcessorResult result4 = (IGeoProcessorResult)gp.Execute(intersect, null);
        }

        public static void Dissolvepolygon(string inputShape, string outputShape)
        {
            try
            {
                // Initialize the geoprocessor.
                Geoprocessor gp = new Geoprocessor();
                gp.OverwriteOutput = true;
                Dissolve featureToPolygon = new Dissolve();
                featureToPolygon.in_features = @"C:\Users\sp15226\Documents\My Received Files\final.shp";
                featureToPolygon.out_feature_class = @"C:\Users\sp15226\Documents\My Received Files\dissolve.shp";
                featureToPolygon.dissolve_field = "NextField";
                IGeoProcessorResult result4 = (IGeoProcessorResult)gp.Execute(featureToPolygon, null);

            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        private static void Gp_ToolboxChanged(object sender, EventArgs e)
        {

        }
        private static void Gp_MessagesCreated(object sender, EventArgs e)
        {
            //  MessagesCreatedEventArgs Events = e as MessagesCreatedEventArgs;
            // IGeoProcessorResult igpms = Events.GPResult;
            // MessageBox.Show(igpms.GetMessage(0));
        }
        public static void updatefieldsWithCursor(IFeatureClass featureClass)
        {
            //using (ComReleaser comReleaser = new ComReleaser())
            //{
            IDataset dataset = (IDataset)featureClass;
            IWorkspace workspace = dataset.Workspace;
            IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
            // Start an edit session
            workspaceEdit.StartEditing(false);
            // Start an edit operation
            workspaceEdit.StartEditOperation();
            // Use IFeatureClass.Search to create a search cursor.
            IFeatureCursor searchCursor = featureClass.Search(null, false);
            //  comReleaser.ManageLifetime(searchCursor);
            // Find the positions of the fields used to get and set values.
            int idIndex = featureClass.FindField("DESIGNATIO");
            IFeature feature = null;
            while ((feature = searchCursor.NextFeature()) != null)
            {
                feature.set_Value(idIndex, "ABC");
                feature.Store();
            }
            searchCursor.Flush();
            workspaceEdit.StopEditOperation();
            // Stop the edit session
            workspaceEdit.StopEditing(true);
            // }
        }
        /// <summary>
        /// returns segment collection
        /// </summary>
        /// <param name="ifc"></param>
        /// <param name="spatialFilter"></param>
        /// <returns>ISegmentCollection</returns>
        public static ISegmentCollection Getsegmentcollection(IFeatureClass ifc, ISpatialFilter spatialFilter)
        {
            IFeatureCursor cursor1 = ifc.Search(spatialFilter, false);
            IFeature pfeature = cursor1.NextFeature();
            // ISegmentCollection segCollection = new PolylineClass() as ISegmentCollection;
            ISegmentCollection segCollection = new PolylineClass() as ISegmentCollection;
            object missin = Type.Missing;
            while (pfeature != null)
            {
                segCollection.AddSegmentCollection(pfeature.Shape as ISegmentCollection);
                pfeature = cursor1.NextFeature();
            }
            cursor1.Flush();
            return segCollection;

        }
        public static void alertenvolope(IGeometry geom)
        {
            IEnvelope envelope = geom.Envelope as IEnvelope;
            IPoint upl = envelope.UpperLeft;
            IPoint upr = envelope.UpperRight;
            IPoint ll = envelope.LowerLeft;
            IPoint lr = envelope.LowerRight;
            MessageBox.Show(upl.X.ToString() + "," + upl.Y.ToString() + ",\n" + upr.X.ToString() + "," + upr.Y.ToString() + ",\n" + ll.X.ToString() + "," + ll.Y.ToString() + ",\n" + lr.X.ToString() + "," + lr.Y.ToString());
        }

        public static int getFeatureCount(ISpatialFilter pSFilter, IFeatureClass pClass, bool pCycling)
        {
            IFeatureCursor lCursor = pClass.Search(pSFilter, pCycling);
            IFeature feature = null;
            int featureCount = 0;

            while ((feature = lCursor.NextFeature()) != null)
            {
                featureCount++;
            }
            return featureCount;
        }

        public static IGeometryCollection Getgeomcollection(IFeatureClass ifc, ISpatialFilter spatialFilter)
        {
            IFeatureCursor cursor1 = ifc.Search(spatialFilter, false);
            IFeature pfeature = cursor1.NextFeature();
            // ISegmentCollection segCollection = new PolylineClass() as ISegmentCollection;
            IGeometryCollection segCollection = new PolygonClass();
            object missin = Type.Missing;
            while (pfeature != null)
            {
                segCollection.AddGeometryCollection(pfeature.Shape as IGeometryCollection);
                pfeature = cursor1.NextFeature();
            }
            cursor1.Flush();
            return segCollection;

        }
        public static List<IPolygon> GetListpolygons(IFeatureClass ifc, ISpatialFilter spatialFilter)
        {
            List<IPolygon> NewCollection = new List<IPolygon>();
            IFeatureCursor cursor1 = null;
            try
            {

                cursor1 = ifc.Search(spatialFilter, false);

                IFeature pfeature = cursor1.NextFeature();
                while (pfeature != null)
                {


                    NewCollection.Add(pfeature.Shape as IPolygon);
                    pfeature = cursor1.NextFeature();
                }

                //AShok added 








                // cursor1.Flush();
            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
                isExecutionResult = false;
            }
            // Marshal.ReleaseComObject(cursor1);
            return NewCollection;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="featureClass"></param>
        /// <param name="spatialFilter"></param>
        /// <returns></returns>
        public static IPointCollection GetpointcollectionwithSpatialfilter(IFeatureClass featureClass, ISpatialFilter spatialFilter)
        {
            IFeatureCursor featureCursor = featureClass.Search(spatialFilter, true);
            IFeature feature = null;
            IPointCollection pointCollection = new MultipointClass();
            object missing = Type.Missing;
            while ((feature = featureCursor.NextFeature()) != null)
            {
                pointCollection.AddPoint(feature.Shape as IPoint, ref missing, ref missing);
            }
            featureCursor.Flush();
            return pointCollection;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="featureClass"></param>
        /// <param name="wherec"></param>
        /// <returns></returns>
        public static IPointCollection GetpointcollectionwithQueryfilter(IFeatureClass featureClass, string wherec)
        {
            IPointCollection pointCollection = new MultipointClass();
            try
            {
                IQueryFilter queryFilter = new QueryFilterClass();
                queryFilter.WhereClause = wherec;
                IFeatureCursor featureCursor = featureClass.Search(queryFilter, true);
                IFeature feature = null;
                object missing = Type.Missing;
                while ((feature = featureCursor.NextFeature()) != null)
                {
                    pointCollection.AddPoint(feature.Shape as IPoint, ref missing, ref missing);
                }
                featureCursor.Flush();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return pointCollection;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pList"></param>
        /// <returns></returns>
        private static IPolygon BoundingPolygon(IPointCollection pList)
        {
            try
            {
                //MessageBox.Show("7");
                IGeometryBridge2 pGeoBrg = new GeometryEnvironment() as IGeometryBridge2;
                IPointCollection4 pPointColl = new MultipointClass(); // edited here
                int numPoints = pList.PointCount;
                WKSPoint[] aWKSPointBuffer = new WKSPoint[numPoints];
                for (int i = 0; i < pList.PointCount; i++)
                {
                    WKSPoint A = new WKSPoint();
                    A.X = pList.get_Point(i).X;
                    A.Y = pList.get_Point(i).Y;
                    aWKSPointBuffer[i] = A;
                }
                //MessageBox.Show("8");
                pGeoBrg.SetWKSPoints(pPointColl, ref aWKSPointBuffer);
                // edits here
                IGeometry pGeom = (IMultipoint)pPointColl;
                // pGeom.SpatialReference = map.SpatialReference;
                ITopologicalOperator pTopOp = (ITopologicalOperator)pGeom;
                IPolygon pPointColl2 = (IPolygon)pTopOp.ConvexHull();

                // pPointColl2.SpatialReference = map.SpatialReference;
                // OutputPolygon = pPointColl2; maybe you don't need this line as the object is not used
                //status = true;
                // MessageBox.Show("9");
                return pPointColl2;
            }
            catch (Exception ex)
            {
                //MessageBox.Show("PolygonFromPoint: " + e.Message);
                //status = false;
                return null;
            }
        }
        public static List<string> DataStatistics(IFeatureClass Iflayer, string fieldname)
        {
            List<string> _uniqueids = new List<string>();
            try
            {
                IDataStatistics dataStats = null;
                ICursor cursor = null;
                // IStatisticsResults statResults = null;
                cursor = (ICursor)(Iflayer.Search(null, false));
                dataStats = new DataStatisticsClass();
                dataStats.Field = fieldname;
                dataStats.Cursor = cursor;
                //IEnumVariantSimple enumVar = null;
                object value = null;
                System.Collections.IEnumerator myEnum = dataStats.UniqueValues;
                value = myEnum.MoveNext();
                while (myEnum.Current != null)
                {
                    _uniqueids.Add(myEnum.Current.ToString());
                    value = myEnum.MoveNext();
                }
                // cursor = (ICursor)(Iflayer.Search(null, false));
                // dataStats.Cursor = cursor;
                //statResults = dataStats.Statistics;

            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
                isExecutionResult = false;

            }
            return _uniqueids;
        }

        public static void IFeatureClass_Select_Example(IFeatureClass featureClass)
        {
            //create the query filter and give it a where clause
            IQueryFilter queryFilter = new QueryFilterClass();
            queryFilter.WhereClause = "DESIGNATIO LIKE '%Cath%'";
            //use the query filter to select features
            //When calling Select the selectionContainer parameter is no longer required. Null (C#, VB.Net) should be supplied it its place. 
            ISelectionSet selectionSet = featureClass.Select(queryFilter, esriSelectionType.esriSelectionTypeIDSet, esriSelectionOption.esriSelectionOptionNormal, null);
            //count the number of selected features
            Console.WriteLine("{0} features where selected from {1} with the where clause {2}", selectionSet.Count, featureClass.AliasName, queryFilter.WhereClause);

        }

        // This example shows how to use a query filter to display the names of all

        // international airports from a feature class of airports.

        public static void DisplayInternationalAirports(IFeatureClass featureClass)
        {
            // Create a new query filter.
            IQueryFilter queryFilter = new QueryFilterClass();
            // Use the WhereClause property to restrict the airports returned to those
            // with a TYPE field value of "International".
            queryFilter.WhereClause = "TYPE = 'International'";
            // Use the SubFields property to restrict the fields returned to the NAME
            // field - it's the only one we need.
            queryFilter.SubFields = "NAME";
            // Find the position of the NAME field in the feature class.
            int fieldPosition = featureClass.FindField("NAME");
            // Get a recycling search cursor and iterate through its results.
            IFeatureCursor featureCursor = featureClass.Search(queryFilter, true);
            IFeature feature = null;
            while ((feature = featureCursor.NextFeature()) != null)
            {
                // LogManager.WriteLogandConsole(feature.get_Value(fieldPosition));
            }

        }

        public static IFeatureClass GetfeatureclassfromGDB(string gdbpath)
        {
            IFeatureClass featureClassWCE = null;
            try
            {
                IWorkspaceFactory pWorkspaceFactory = new ESRI.ArcGIS.DataSourcesGDB.FileGDBWorkspaceFactory();
                IFeatureWorkspace pWorkSpace = pWorkspaceFactory.OpenFromFile(gdbpath, 0) as IFeatureWorkspace;// txtinput
                featureClassWCE = pWorkSpace.OpenFeatureClass("CO_lr_FeatureToPolygon");

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return featureClassWCE;
        }

        public static void Createshapefile(List<ServicePolygonClass> igeom, string shapefilepath, bool isFinalOp)
        {
            try
            {
                // create a spatial reference object from EPSG Code
                ISpatialReferenceFactory3 pSRgen = new SpatialReferenceEnvironmentClass();
                ISpatialReference pSR = pSRgen.CreateSpatialReference((int)esriSRGeoCSType.esriSRGeoCS_WGS1984);
                IFields pOutFields = new FieldsClass();
                IFieldsEdit pFieldsEd = (IFieldsEdit)pOutFields;
                IField pNewField;
                IFieldEdit pNewFieldEd;
                IGeometryDef pGeomDef = new GeometryDefClass();
                IGeometryDefEdit pGDefEd = (IGeometryDefEdit)pGeomDef;
                // create the fields object from scratch
                // start with objectid / fid
                pNewField = new FieldClass();
                pNewFieldEd = (IFieldEdit)pNewField;
                pNewFieldEd.Name_2 = "FID";
                pNewFieldEd.Type_2 = esriFieldType.esriFieldTypeOID;
                pFieldsEd.AddField(pNewField);
                // next shape
                pNewField = new FieldClass();
                pNewFieldEd = (IFieldEdit)pNewField;
                pNewFieldEd.Name_2 = "SHAPE";
                pNewFieldEd.Type_2 = esriFieldType.esriFieldTypeGeometry;
                // geometry fields need a geometry def: geometry type, grid, spatial reference ...
                if (igeom[0].polyGeometry.GeometryType == esriGeometryType.esriGeometryPolygon)
                    pGDefEd.GeometryType_2 = esriGeometryType.esriGeometryPolygon;
                else if (igeom[0].polyGeometry.GeometryType == esriGeometryType.esriGeometryPolyline)
                    pGDefEd.GeometryType_2 = esriGeometryType.esriGeometryPolyline;
                else if (igeom[0].polyGeometry.GeometryType == esriGeometryType.esriGeometryPoint)
                    pGDefEd.GeometryType_2 = esriGeometryType.esriGeometryPoint;
                pGDefEd.GridCount_2 = 1;
                // grid and spatial reference valid for geographic coordinates
                // change the values for projected coordinate systems
                pGDefEd.set_GridSize(0, 0.1);
                pSR.SetFalseOriginAndUnits(-180, -180, 1000000000);
                // Associate the spatial reference with the GeometryDef
                pGDefEd.SpatialReference_2 = pSR;
                pNewFieldEd.GeometryDef_2 = pGeomDef;
                pFieldsEd.AddField(pNewField);

                // shapefiles must have at least one other field
                // you can use this as a template to add new fields
                if (isFinalOp)
                {
                    //pNewField = new FieldClass();
                    //pNewFieldEd = (IFieldEdit)pNewField;
                    //pNewFieldEd.Name_2 = "POLYGON_ID";
                    //pNewFieldEd.Type_2 = esriFieldType.esriFieldTypeInteger;
                    //pFieldsEd.AddField(pNewField);

                    pNewField = new FieldClass();
                    pNewFieldEd = (IFieldEdit)pNewField;
                    pNewFieldEd.Name_2 = "QU_TYPE";
                    pNewFieldEd.Type_2 = esriFieldType.esriFieldTypeSmallInteger;
                    pFieldsEd.AddField(pNewField);

                    pNewField = new FieldClass();
                    pNewFieldEd = (IFieldEdit)pNewField;
                    pNewFieldEd.Name_2 = "QU_TY_STAT";
                    pNewFieldEd.Type_2 = esriFieldType.esriFieldTypeSmallInteger;
                    pFieldsEd.AddField(pNewField);

                    pNewField = new FieldClass();
                    pNewFieldEd = (IFieldEdit)pNewField;
                    pNewFieldEd.Name_2 = "AVAIL_DT";
                    pNewFieldEd.Type_2 = esriFieldType.esriFieldTypeDate;
                    pFieldsEd.AddField(pNewField);

                    pNewField = new FieldClass();
                    pNewFieldEd = (IFieldEdit)pNewField;
                    pNewFieldEd.Name_2 = "BAND";
                    pNewFieldEd.Type_2 = esriFieldType.esriFieldTypeString;
                    pFieldsEd.AddField(pNewField);

                    pNewField = new FieldClass();
                    pNewFieldEd = (IFieldEdit)pNewField;
                    pNewFieldEd.Name_2 = "WC_ID";
                    pNewFieldEd.Type_2 = esriFieldType.esriFieldTypeString;
                    pFieldsEd.AddField(pNewField);

                    pNewField = new FieldClass();
                    pNewFieldEd = (IFieldEdit)pNewField;
                    pNewFieldEd.Name_2 = "WC_NAME";
                    pNewFieldEd.Type_2 = esriFieldType.esriFieldTypeString;
                    pFieldsEd.AddField(pNewField);

                    pNewField = new FieldClass();
                    pNewFieldEd = (IFieldEdit)pNewField;
                    pNewFieldEd.Name_2 = "FCBL_ID";
                    pNewFieldEd.Type_2 = esriFieldType.esriFieldTypeString;
                    pFieldsEd.AddField(pNewField);

                    pNewField = new FieldClass();
                    pNewFieldEd = (IFieldEdit)pNewField;
                    pNewFieldEd.Name_2 = "FLOC";
                    pNewFieldEd.Type_2 = esriFieldType.esriFieldTypeString;
                    pFieldsEd.AddField(pNewField);

                    pNewField = new FieldClass();
                    pNewFieldEd = (IFieldEdit)pNewField;
                    pNewFieldEd.Name_2 = "NDS_JNO";
                    pNewFieldEd.Type_2 = esriFieldType.esriFieldTypeString;
                    pFieldsEd.AddField(pNewField);

                    pNewField = new FieldClass();
                    pNewFieldEd = (IFieldEdit)pNewField;
                    pNewFieldEd.Name_2 = "OLT_REL";
                    pNewFieldEd.Type_2 = esriFieldType.esriFieldTypeString;
                    pFieldsEd.AddField(pNewField);

                    pNewField = new FieldClass();
                    pNewFieldEd = (IFieldEdit)pNewField;
                    pNewFieldEd.Name_2 = "CREATEMODE";
                    pNewFieldEd.Type_2 = esriFieldType.esriFieldTypeString;
                    pFieldsEd.AddField(pNewField);

                    pNewField = new FieldClass();
                    pNewFieldEd = (IFieldEdit)pNewField;
                    pNewFieldEd.Name_2 = "LST_UPD_DT";
                    pNewFieldEd.Type_2 = esriFieldType.esriFieldTypeDate;
                    pFieldsEd.AddField(pNewField);
                }
                else
                {
                    pNewField = new FieldClass();
                    pNewFieldEd = (IFieldEdit)pNewField;
                    pNewFieldEd.Name_2 = "NextField";
                    pNewFieldEd.Type_2 = esriFieldType.esriFieldTypeString;
                    pFieldsEd.AddField(pNewField);

                    pNewField = new FieldClass();
                    pNewFieldEd = (IFieldEdit)pNewField;
                    pNewFieldEd.Name_2 = "PolygonID";
                    pNewFieldEd.Type_2 = esriFieldType.esriFieldTypeString;
                    pFieldsEd.AddField(pNewField);
                }

                // now there's enough information to create the shapefile... sort of
                // insert your own values here
                string PutShapeHere = System.IO.Path.GetDirectoryName(shapefilepath);
                string ShapeName = System.IO.Path.GetFileName(shapefilepath);

                // open the workspace and make it a feature workspace
                IWorkspaceFactory pWSF = new ESRI.ArcGIS.DataSourcesFile.ShapefileWorkspaceFactory();
                IWorkspace pWS = pWSF.OpenFromFile(PutShapeHere, 0);
                IFeatureWorkspace pFeatWS = (IFeatureWorkspace)pWS;
                // now to create the shapefile
                IFeatureClass Ifc = pFeatWS.CreateFeatureClass(ShapeName, pOutFields, null, null, esriFeatureType.esriFTSimple, "Shape", "");
                InsertFeaturesForGeometryList(Ifc, igeom, isFinalOp);

            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
                isExecutionResult = false;
            }
        }


        public static void InsertFeaturesForGeometryList(IFeatureClass featureClass, List<ServicePolygonClass> geometryList, bool isFinalOp)
        {
            try
            {
                // Get the Workspace from the IDataset interface on the feature class.

                IDataset dataset = (IDataset)featureClass;
                IWorkspace workspace = dataset.Workspace;
                // Cast the workspace to the IWorkspaceEdit interface.

                IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
                // Start an edit session and edit operation.

                workspaceEdit.StartEditing(true);
                workspaceEdit.StartEditOperation();
                // Create a feature buffer.

                IFeatureBuffer featureBuffer = featureClass.CreateFeatureBuffer();
                // Set the common value(s) of the new features (the "Contractor" field, in this case).

                int installedByFieldIndex = featureClass.FindField("NextField");
                int PolygonIndex = featureClass.FindField("PolygonID");

                //  int polygonIDIndex = featureClass.FindField("POLYGON_ID");
                int typeIndex = featureClass.FindField("QU_TYPE");
                int statusindex = featureClass.FindField("QU_TY_STAT");
                int availDateIndex = featureClass.FindField("AVAIL_DT");
                int bandWidthIndex = featureClass.FindField("BAND");
                int wireIDIndex = featureClass.FindField("WC_ID");
                int wcname_index = featureClass.FindField("WC_NAME");
                int fbclIDindex = featureClass.FindField("FCBL_ID");
                int fLOCIndex = featureClass.FindField("FLOC");
                int ndJobIndex = featureClass.FindField("NDS_JNO");
                int oltIndex = featureClass.FindField("OLT_REL");
                int crmodeIndex = featureClass.FindField("CREATEMODE");
                int lastUpdDtIndex = featureClass.FindField("LST_UPD_DT");

                // featureBuffer.set_Value(installedByFieldIndex, "K Johnston");
                // Create an insert cursor with buffering enabled.
                IFeatureCursor featureCursor = featureClass.Insert(true);

                // Insert a feature for each parameter-supplied geometry.
                int i = 0;
                foreach (ServicePolygonClass sqmModel in geometryList)
                {
                    try
                    {
                        // Assign a geometry to the feature buffer.

                        if (isFinalOp)
                        {
                            // featureBuffer.set_Value(polygon_Status_Index, geometry.Polygon_Status.ToString());
                            featureBuffer.set_Value(fbclIDindex, sqmModel.FIBER_CABLE_ID);
                            featureBuffer.set_Value(wireIDIndex, sqmModel.SERVIING_WIRE_CENTER_CLLI);
                            featureBuffer.set_Value(wcname_index, sqmModel.SERVING_WIRE_CENTER_NAME);
                            featureBuffer.set_Value(statusindex, sqmModel.STATUS);
                            featureBuffer.set_Value(typeIndex, sqmModel.TYPE);
                            featureBuffer.set_Value(availDateIndex, sqmModel.AVAILABILITY_DATE);
                            featureBuffer.set_Value(bandWidthIndex, sqmModel.BANDWIDTH);
                            featureBuffer.set_Value(fLOCIndex, sqmModel.FIBER_LINE_OF_COUNT);
                            featureBuffer.set_Value(ndJobIndex, sqmModel.NDS_JOB_NO);
                            featureBuffer.set_Value(oltIndex, sqmModel.OLT_RELATIONSHIP);
                            featureBuffer.set_Value(crmodeIndex, sqmModel.CREATEMODE);
                            featureBuffer.set_Value(lastUpdDtIndex, sqmModel.LST_UPD_DT);
                        }
                        else
                        {
                            featureBuffer.set_Value(installedByFieldIndex, i);
                            featureBuffer.set_Value(PolygonIndex, sqmModel.FIBER_CABLE_ID);
                        }


                        featureBuffer.Shape = sqmModel.polyGeometry;
                        i++;
                        // Insert the feature into the feature class.
                        featureCursor.InsertFeature(featureBuffer);
                    }
                    catch (Exception ex)
                    {
                        LogManager.WriteLogandConsole(ex);
                        isExecutionResult = false;
                    }
                }
                // Calling flush allows you to handle any errors at a known time rather then on the cursor destruction.
                // featureCursor.Flush();
                // Explicitly release the cursor.
                Marshal.ReleaseComObject(featureCursor);
                // Stop editing.
                workspaceEdit.StopEditOperation();
                workspaceEdit.StopEditing(true);
            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
                isExecutionResult = false;
            }

        }


        /// <summary>
        /// InsertUpdateFeaturestoODS - SQM table
        /// </summary>
        /// <param name="featureClass"></param>
        /// <param name="geometryList"></param>
        public static void InsertUpdateFeaturestoODS(IFeatureClass featureClass, List<ServicePolygonClass> geometryList)
        {

            try
            {
                #region ListOutExisting DEsignations
                List<int> objIds = new List<int>();
                foreach (var sqmModel in geometryList)
                {
                    IQueryFilter PFq = new QueryFilterClass();
                    PFq.WhereClause = "FCBL_ID = '" + sqmModel.FIBER_CABLE_ID + "' AND QU_TY_STAT = " + sqmModel.STATUS; //Status = Inconstruction/Inservice
                    IFeatureCursor pfCur = featureClass.Search(PFq, false);
                    IFeature pfet = null;
                    while ((pfet = pfCur.NextFeature()) != null)
                    {
                        objIds.Add(pfet.OID);
                    }
                    Marshal.ReleaseComObject(pfCur);
                }
                #endregion ListOutExisting DEsignations

                #region Update polygons to OP table             

                IDataset dataset = (IDataset)featureClass;
                IWorkspace workspace = dataset.Workspace;
                IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
                // Start an edit session and edit operation.
                workspaceEdit.StartEditing(true);
                workspaceEdit.StartEditOperation();

                int typeIndex = featureClass.FindField("QU_TYPE");
                int statusindex = featureClass.FindField("QU_TY_STAT");
                int availDateIndex = featureClass.FindField("AVAIL_DT");
                int bandWidthIndex = featureClass.FindField("BAND");
                int wireIDIndex = featureClass.FindField("WC_ID");
                int wcname_index = featureClass.FindField("WC_NAME");
                int fbclIDindex = featureClass.FindField("FCBL_ID");
                int fLOCIndex = featureClass.FindField("FLOC");
                int ndJobIndex = featureClass.FindField("NDS_JNO");
                int oltIndex = featureClass.FindField("OLT_REL");
                int crmodeIndex = featureClass.FindField("CREATEMODE");
                int lastUpdDtIndex = featureClass.FindField("LST_UPD_DT");
                // Create a feature buffer.
                IFeatureBuffer featureBuffer = featureClass.CreateFeatureBuffer();
                IFeatureCursor featureCursor = featureClass.Insert(true);
                foreach (ServicePolygonClass sqmModel in geometryList)
                {
                    try
                    {
                        featureBuffer.set_Value(fbclIDindex, sqmModel.FIBER_CABLE_ID);
                        featureBuffer.set_Value(wireIDIndex, sqmModel.SERVIING_WIRE_CENTER_CLLI);
                        featureBuffer.set_Value(wcname_index, sqmModel.SERVING_WIRE_CENTER_NAME);
                        featureBuffer.set_Value(statusindex, sqmModel.STATUS);
                        featureBuffer.set_Value(typeIndex, sqmModel.TYPE);
                        featureBuffer.set_Value(availDateIndex, sqmModel.AVAILABILITY_DATE);
                        featureBuffer.set_Value(bandWidthIndex, sqmModel.BANDWIDTH);
                        featureBuffer.set_Value(fLOCIndex, sqmModel.FIBER_LINE_OF_COUNT);
                        featureBuffer.set_Value(ndJobIndex, sqmModel.NDS_JOB_NO);
                        featureBuffer.set_Value(oltIndex, sqmModel.OLT_RELATIONSHIP);
                        featureBuffer.set_Value(crmodeIndex, sqmModel.CREATEMODE);
                        featureBuffer.set_Value(lastUpdDtIndex, sqmModel.LST_UPD_DT);
                        featureBuffer.Shape = sqmModel.polyGeometry;

                        // Insert the feature into the feature class.
                        featureCursor.InsertFeature(featureBuffer);
                    }
                    catch (Exception ex)
                    {
                        LogManager.WriteLogandConsole(ex);
                        isExecutionResult = false;
                    }
                }

                Marshal.ReleaseComObject(featureCursor);
                // Stop editing.
                workspaceEdit.StopEditOperation();
                workspaceEdit.StopEditing(true);
                #endregion

                #region delexistin designations

                if (objIds.Count > 0)
                {
                    try
                    {
                        DbWorker dbWorker = new DbWorker();
                        foreach (var item in objIds)
                        {
                            string delquery = "delete from " + featureClass.AliasName + " where objectid = " + item; //and WC_ID = '" + sqmModel.SERVIING_WIRE_CENTER_CLLI + "'"
                            int rows = dbWorker.RunDmlQuery(delquery);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogManager.WriteLogandConsole(ex); isExecutionResult = false;
                    }

                }
                Marshal.FinalReleaseComObject(featureClass);
                Marshal.FinalReleaseComObject(workspace);
                Marshal.FinalReleaseComObject(workspaceEdit);
                Marshal.FinalReleaseComObject(dataset);
                workspace = null;

                #endregion
            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
                isExecutionResult = false;
            }
            finally
            {

            }
        }

        /// <summary>
        /// InsertFeaturestoWorkFlowTable - SQM WorkFlow Table
        /// </summary>
        /// <param name="featureClass"></param>
        /// <param name="geometryList"></param>
        public static void InsertFeaturestoWorkFlowTable(IFeatureClass featureClass, List<ServicePolygonClass> geometryList, Jobs currentJOb)
        {
            try
            {
                IDataset dataset = (IDataset)featureClass;
                IWorkspace workspace = dataset.Workspace;
                IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
                // Start an edit session and edit operation.
                workspaceEdit.StartEditing(true);
                workspaceEdit.StartEditOperation();

                int typeIndex = featureClass.FindField("QU_TYPE");
                int statusindex = featureClass.FindField("QU_TY_STAT");
                int availDateIndex = featureClass.FindField("AVAIL_DT");
                int bandWidthIndex = featureClass.FindField("BAND");
                int wireIDIndex = featureClass.FindField("WC_ID");
                int wcname_index = featureClass.FindField("WC_NAME");
                int fbclIDindex = featureClass.FindField("FCBL_ID");
                int fLOCIndex = featureClass.FindField("FLOC");
                int ndJobIndex = featureClass.FindField("NDS_JNO");
                int oltIndex = featureClass.FindField("OLT_REL");
                int crmodeIndex = featureClass.FindField("CREATEMODE");
                int JOBINDEX = featureClass.FindField("JOB_ID");
                int lastUpdDtIndex = featureClass.FindField("LST_UPD_DT");
                // Create a feature buffer.
                IFeatureBuffer featureBuffer = featureClass.CreateFeatureBuffer();
                IFeatureCursor featureCursor = featureClass.Insert(true);
                foreach (ServicePolygonClass sqmModel in geometryList)
                {
                    try
                    {
                        featureBuffer.set_Value(fbclIDindex, sqmModel.FIBER_CABLE_ID);
                        featureBuffer.set_Value(wireIDIndex, sqmModel.SERVIING_WIRE_CENTER_CLLI);
                        featureBuffer.set_Value(wcname_index, sqmModel.SERVING_WIRE_CENTER_NAME);
                        featureBuffer.set_Value(statusindex, sqmModel.STATUS);
                        featureBuffer.set_Value(typeIndex, sqmModel.TYPE);
                        featureBuffer.set_Value(availDateIndex, sqmModel.AVAILABILITY_DATE);
                        featureBuffer.set_Value(bandWidthIndex, sqmModel.BANDWIDTH);
                        featureBuffer.set_Value(fLOCIndex, sqmModel.FIBER_LINE_OF_COUNT);
                        featureBuffer.set_Value(ndJobIndex, sqmModel.NDS_JOB_NO);
                        featureBuffer.set_Value(oltIndex, sqmModel.OLT_RELATIONSHIP);
                        featureBuffer.set_Value(crmodeIndex, sqmModel.CREATEMODE);
                        featureBuffer.set_Value(JOBINDEX, currentJOb.jobID);
                        featureBuffer.set_Value(lastUpdDtIndex, sqmModel.LST_UPD_DT);
                        featureBuffer.Shape = sqmModel.polyGeometry;

                        // Insert the feature into the feature class.
                        featureCursor.InsertFeature(featureBuffer);
                    }
                    catch (Exception ex)
                    {
                        LogManager.WriteLogandConsole(ex);
                        isExecutionResult = false;
                    }
                }
                Marshal.ReleaseComObject(featureCursor);
                // Stop editing.
                workspaceEdit.StopEditOperation();
                workspaceEdit.StopEditing(true);
            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
                isExecutionResult = false;
            }
        }

        public static IFeatureClass Getfeatureclass(string path)
        {
            IWorkspaceFactory wf = new ESRI.ArcGIS.DataSourcesFile.ShapefileWorkspaceFactory();
            IFeatureWorkspace featureWorkspace = (IFeatureWorkspace)wf.OpenFromFile(System.IO.Path.GetDirectoryName(path), 0);
            IFeatureClass featureClass = featureWorkspace.OpenFeatureClass(System.IO.Path.GetFileName(path));
            return featureClass;
        }
        public static IFeatureClass CreateInMemoryshapefile(List<ServicePolygonClass> igeom, bool Load)
        {
            IFeatureClass pOutFeatClass = null;
            try
            {
                UID CLSID = null;
                String featureClassName = "TestFC";
                // assign the class id value if not assigned  
                if (CLSID == null)
                {
                    CLSID = new UIDClass();
                    CLSID.Value = "esriGeoDatabase.Feature";
                }
                IObjectClassDescription objectClassDescription = new FeatureClassDescriptionClass();
                ISpatialReferenceFactory3 pSRgen = new SpatialReferenceEnvironmentClass();
                ISpatialReference pSR = pSRgen.CreateSpatialReference((int)esriSRGeoCSType.esriSRGeoCS_WGS1984);
                IFields pOutFields = new FieldsClass();
                // IFields pOutFields = objectClassDescription.RequiredFields;
                IFieldsEdit pFieldsEd = (IFieldsEdit)pOutFields;
                IField pNewField;
                IFieldEdit pNewFieldEd;
                IGeometryDef pGeomDef = new GeometryDefClass();
                IGeometryDefEdit pGDefEd = (IGeometryDefEdit)pGeomDef;
                // create the fields object from scratch
                // start with objectid / fid
                pNewField = new FieldClass();
                pNewFieldEd = (IFieldEdit)pNewField;
                pNewFieldEd.Name_2 = "FID";
                pNewFieldEd.Type_2 = esriFieldType.esriFieldTypeOID;
                pFieldsEd.AddField(pNewField);
                // next shape
                pNewField = new FieldClass();
                pNewFieldEd = (IFieldEdit)pNewField;
                pNewFieldEd.Name_2 = "Shape";

                pNewFieldEd.Type_2 = esriFieldType.esriFieldTypeGeometry;

                // geometry fields need a geometry def: geometry type, grid, spatial reference ...
                if (igeom[0].polyGeometry.GeometryType == esriGeometryType.esriGeometryPolygon)
                    pGDefEd.GeometryType_2 = esriGeometryType.esriGeometryPolygon;
                else if (igeom[0].polyGeometry.GeometryType == esriGeometryType.esriGeometryPolyline)
                    pGDefEd.GeometryType_2 = esriGeometryType.esriGeometryPolyline;

                pGDefEd.GridCount_2 = 1;
                // grid and spatial reference valid for geographic coordinates
                // change the values for projected coordinate systems
                pGDefEd.set_GridSize(0, 0.1);
                pSR.SetFalseOriginAndUnits(-180, -180, 1000000000);
                // Associate the spatial reference with the GeometryDef
                pGDefEd.SpatialReference_2 = pSR;
                pNewFieldEd.GeometryDef_2 = pGeomDef;
                pFieldsEd.AddField(pNewField);

                // shapefiles must have at least one other field
                // you can use this as a template to add new fields
                pNewField = new FieldClass();
                pNewFieldEd = (IFieldEdit)pNewField;
                pNewFieldEd.Name_2 = "NextField";
                pNewFieldEd.Type_2 = esriFieldType.esriFieldTypeString;
                pFieldsEd.AddField(pNewField);

                pNewField = new FieldClass();
                pNewFieldEd = (IFieldEdit)pNewField;
                pNewFieldEd.Name_2 = "PolygonID";
                pNewFieldEd.Type_2 = esriFieldType.esriFieldTypeString;
                pFieldsEd.AddField(pNewField);



                // open the workspace and make it a feature workspace
                IWorkspaceFactory pWSF = new InMemoryWorkspaceFactory();
                IName name = pWSF.Create("", "MyWorkspace", null, 0) as IName;
                IWorkspace inMemWorkspace = (IWorkspace)name.Open();
                IFeatureWorkspace featureWorkspace = (IFeatureWorkspace)inMemWorkspace;


                //IWorkspace pWS = pWSF.OpenFromFile(PutShapeHere, 0);
                // IFeatureWorkspace pFeatWS = (IFeatureWorkspace)pWS;
                // now to create the shapefile
                // IFeatureClass Ifc = pFeatWS.CreateFeatureClass(ShapeName, pOutFields, null, null, esriFeatureType.esriFTSimple, "Shape", "");
                // IFeatureClass Ifc = featureWorkspace.CreateFeatureClass(ShapeName, pOutFields, null, null, esriFeatureType.esriFTSimple, "Shape", "");
                pOutFeatClass = featureWorkspace.CreateFeatureClass(featureClassName, pOutFields, CLSID, null, esriFeatureType.esriFTSimple, "Shape", null);
                if (Load)
                    InsertFeaturesForGeometryList(pOutFeatClass, igeom, false);
            }
            catch (Exception)
            {

                throw;
            }
            return pOutFeatClass;
        }

        public static void CreateNoBuildshapefile(List<NoBuild> igeom, string shapefilepath)
        {
            try
            {
                // create a spatial reference object from EPSG Code
                ISpatialReferenceFactory3 pSRgen = new SpatialReferenceEnvironmentClass();
                ISpatialReference pSR = pSRgen.CreateSpatialReference((int)esriSRGeoCSType.esriSRGeoCS_WGS1984);
                IFields pOutFields = new FieldsClass();
                IFieldsEdit pFieldsEd = (IFieldsEdit)pOutFields;
                IField pNewField;
                IFieldEdit pNewFieldEd;
                IGeometryDef pGeomDef = new GeometryDefClass();
                IGeometryDefEdit pGDefEd = (IGeometryDefEdit)pGeomDef;
                // create the fields object from scratch
                // start with objectid / fid
                pNewField = new FieldClass();
                pNewFieldEd = (IFieldEdit)pNewField;
                pNewFieldEd.Name_2 = "OBJECTID";
                pNewFieldEd.Type_2 = esriFieldType.esriFieldTypeOID;
                pFieldsEd.AddField(pNewField);
                // next shape
                pNewField = new FieldClass();
                pNewFieldEd = (IFieldEdit)pNewField;
                pNewFieldEd.Name_2 = "Shape";
                pNewFieldEd.Type_2 = esriFieldType.esriFieldTypeGeometry;
                // geometry fields need a geometry def: geometry type, grid, spatial reference ...
                // pGDefEd.GeometryType_2 = esriGeometryType.esriGeometryPolygon;
                pGDefEd.GeometryType_2 = esriGeometryType.esriGeometryPolygon;
                pGDefEd.GridCount_2 = 1;
                // grid and spatial reference valid for geographic coordinates
                // change the values for projected coordinate systems
                pGDefEd.set_GridSize(0, 0.1);
                pSR.SetFalseOriginAndUnits(-180, -180, 1000000000);
                // Associate the spatial reference with the GeometryDef
                pGDefEd.SpatialReference_2 = pSR;
                pNewFieldEd.GeometryDef_2 = pGeomDef;
                pFieldsEd.AddField(pNewField);

                // shapefiles must have at least one other field
                // you can use this as a template to add new fields
                pNewField = new FieldClass();
                pNewFieldEd = (IFieldEdit)pNewField;
                pNewFieldEd.Name_2 = "TYPE";
                pNewFieldEd.Type_2 = esriFieldType.esriFieldTypeString;
                pFieldsEd.AddField(pNewField);

                pNewField = new FieldClass();
                pNewFieldEd = (IFieldEdit)pNewField;
                pNewFieldEd.Name_2 = "STATUS";
                pNewFieldEd.Type_2 = esriFieldType.esriFieldTypeString;
                pFieldsEd.AddField(pNewField);

                pNewField = new FieldClass();
                pNewFieldEd = (IFieldEdit)pNewField;
                pNewFieldEd.Name_2 = "REASON";
                pNewFieldEd.Type_2 = esriFieldType.esriFieldTypeString;
                pFieldsEd.AddField(pNewField);

                pNewField = new FieldClass();
                pNewFieldEd = (IFieldEdit)pNewField;
                pNewFieldEd.Name_2 = "WC_ID";
                pNewFieldEd.Type_2 = esriFieldType.esriFieldTypeString;
                pFieldsEd.AddField(pNewField);

                pNewField = new FieldClass();
                pNewFieldEd = (IFieldEdit)pNewField;
                pNewFieldEd.Name_2 = "CREATEMODE";
                pNewFieldEd.Type_2 = esriFieldType.esriFieldTypeString;
                pFieldsEd.AddField(pNewField);

                pNewField = new FieldClass();
                pNewFieldEd = (IFieldEdit)pNewField;
                pNewFieldEd.Name_2 = "LST_UPD_DT";
                pNewFieldEd.Type_2 = esriFieldType.esriFieldTypeString;
                pFieldsEd.AddField(pNewField);

                pNewField = new FieldClass();
                pNewFieldEd = (IFieldEdit)pNewField;
                pNewFieldEd.Name_2 = "ADDR_ID";
                pNewFieldEd.Type_2 = esriFieldType.esriFieldTypeInteger;
                pFieldsEd.AddField(pNewField);

                pNewField = new FieldClass();
                pNewFieldEd = (IFieldEdit)pNewField;
                pNewFieldEd.Name_2 = "LATLON";
                pNewFieldEd.Type_2 = esriFieldType.esriFieldTypeString;
                pFieldsEd.AddField(pNewField);

                // now there's enough information to create the shapefile... sort of
                // insert your own values here
                string PutShapeHere = System.IO.Path.GetDirectoryName(shapefilepath);
                string ShapeName = System.IO.Path.GetFileName(shapefilepath);

                // open the workspace and make it a feature workspace
                IWorkspaceFactory pWSF = new ESRI.ArcGIS.DataSourcesFile.ShapefileWorkspaceFactory();
                IWorkspace pWS = pWSF.OpenFromFile(PutShapeHere, 0);
                IFeatureWorkspace pFeatWS = (IFeatureWorkspace)pWS;
                // now to create the shapefile
                IFeatureClass Ifc = pFeatWS.CreateFeatureClass(ShapeName, pOutFields, null, null, esriFeatureType.esriFTSimple, "Shape", "");
                InsertNobuildFeatures(Ifc, igeom);

            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
                isExecutionResult = false;
            }
        }
        public static void InsertNobuildFeatures(IFeatureClass featureClass, List<NoBuild> geometryList)
        {
            try
            {
                // Get the Workspace from the IDataset interface on the feature class.

                IDataset dataset = (IDataset)featureClass;
                IWorkspace workspace = dataset.Workspace;
                // Cast the workspace to the IWorkspaceEdit interface.
                IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
                // Start an edit session and edit operation.
                workspaceEdit.StartEditing(true);
                workspaceEdit.StartEditOperation();
                // Create a feature buffer.
                IFeatureBuffer featureBuffer = featureClass.CreateFeatureBuffer();
                // Set the common value(s) of the new features (the "Contractor" field, in this case).
                // int type = featureClass.FindField("TYPE");
                int status = featureClass.FindField("STATUS");
                int reason = featureClass.FindField("REASON");
                int wcid = featureClass.FindField("WC_ID");
                int addr_id = featureClass.FindField("ADDR_ID");
                int creationmode = featureClass.FindField("CREATEMODE");
                int latlon = featureClass.FindField("LATLON");
                int dateti = featureClass.FindField("LST_UPD_DT");
                // Create an insert cursor with buffering enabled.
                IFeatureCursor featureCursor = featureClass.Insert(true);
                foreach (NoBuild objNobuild in geometryList)
                {
                    try
                    {
                        featureBuffer.set_Value(status, Convert.ToInt16(objNobuild.STATUS));
                        featureBuffer.set_Value(reason, objNobuild.REASON);
                        featureBuffer.set_Value(wcid, objNobuild.WC_ID);
                        featureBuffer.set_Value(addr_id, objNobuild.Addr_id);
                        featureBuffer.set_Value(creationmode, objNobuild.Createmode);
                        featureBuffer.set_Value(latlon, objNobuild.Latlon);
                        featureBuffer.set_Value(dateti, DateTime.Now.ToString("dd-MMM-yyyy hh:mm:ss"));
                        featureBuffer.Shape = objNobuild.GEOEMTRY;
                        // Insert the feature into the feature class.
                        featureCursor.InsertFeature(featureBuffer);
                    }
                    catch (Exception ex)
                    {
                        LogManager.WriteLogandConsole(ex);
                        isExecutionResult = false;
                    }
                }
                // Calling flush allows you to handle any errors at a known time rather then on the cursor destruction.
                featureCursor.Flush();
                // Explicitly release the cursor.
                Marshal.ReleaseComObject(featureCursor);
                // Stop editing.
                workspaceEdit.StopEditOperation();
                workspaceEdit.StopEditing(true);
            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
                isExecutionResult = false;
            }

        }
        public static void InsertNobuildFeaturesFinal(IFeatureClass featureClass, List<NoBuild> geometryList,int createmode)
        {
            try
            {
                // Get the Workspace from the IDataset interface on the feature class.

                IDataset dataset = (IDataset)featureClass;
                IWorkspace workspace = dataset.Workspace;
                // Cast the workspace to the IWorkspaceEdit interface.
                IWorkspaceEdit workspaceEdit = (IWorkspaceEdit)workspace;
                // Start an edit session and edit operation.
                workspaceEdit.StartEditing(true);
                workspaceEdit.StartEditOperation();
                // Create a feature buffer.
                IFeatureBuffer featureBuffer = featureClass.CreateFeatureBuffer();
                // Set the common value(s) of the new features (the "Contractor" field, in this case).
                // int type = featureClass.FindField("TYPE");
                int status = featureClass.FindField("STATUS");
                int reason = featureClass.FindField("REASON");
                int wcid = featureClass.FindField("WC_ID");
                int addr_id = featureClass.FindField("ADDR_ID");
                int creationmode = featureClass.FindField("CREATEMODE");
                int latlon = featureClass.FindField("LATLON");
                int dateti = featureClass.FindField("LST_UPD_DT");
                // Create an insert cursor with buffering enabled.
                IFeatureCursor featureCursor = featureClass.Insert(true);
                foreach (NoBuild objNobuild in geometryList)
                {
                    try
                    {
                        bool value = false;
                        if (createmode == 1)
                        {
                            IQueryFilter queryFilter = new QueryFilterClass();
                            queryFilter.WhereClause = "ADDR_ID=" + objNobuild.Addr_id + " AND CREATEMODE='Manual'";
                            IFeatureCursor FC = featureClass.Search(queryFilter, false);
                            IFeature feature = null;
                            while ((feature = FC.NextFeature()) != null)
                            {
                                value = true;
                            }
                        }
                        else if (createmode == 2)
                        {
                            IQueryFilter queryFilter = new QueryFilterClass();
                            queryFilter.WhereClause = "ADDR_ID=" + objNobuild.Addr_id + " AND CREATEMODE='Automatic'";
                            IFeatureCursor FC = featureClass.Search(queryFilter, false);
                            IFeature feature = null;
                            while ((feature = FC.NextFeature()) != null)
                            {
                                value = true;
                            }
                        }
                        if (value) continue;
                        // Assign a geometry to the feature buffer.
                        // featureBuffer.set_Value(type, Convert.ToInt16(objNobuild.TYPE));
                        DbWorker db = new DbWorker();
                        db.RunDmlQuery("delete from " + configKey.Default.Qual_Server + " where ADDR_ID=" + objNobuild.Addr_id + "");

                        featureBuffer.set_Value(status, Convert.ToInt16(objNobuild.STATUS));
                        featureBuffer.set_Value(reason, objNobuild.REASON);
                        featureBuffer.set_Value(wcid, objNobuild.WC_ID);
                        featureBuffer.set_Value(addr_id, objNobuild.Addr_id);
                        featureBuffer.set_Value(creationmode, objNobuild.Createmode);
                        featureBuffer.set_Value(latlon, objNobuild.Latlon);
                        featureBuffer.set_Value(dateti, DateTime.Now.ToString("dd-MMM-yyyy hh:mm:ss"));
                        featureBuffer.Shape = objNobuild.GEOEMTRY;
                        // Insert the feature into the feature class.
                        featureCursor.InsertFeature(featureBuffer);
                    }
                    catch (Exception ex)
                    {
                        LogManager.WriteLogandConsole(ex);
                        isExecutionResult = false;
                    }
                }
                // Calling flush allows you to handle any errors at a known time rather then on the cursor destruction.
                featureCursor.Flush();
                // Explicitly release the cursor.
                Marshal.ReleaseComObject(featureCursor);
                // Stop editing.
                workspaceEdit.StopEditOperation();
                workspaceEdit.StopEditing(true);
            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
                isExecutionResult = false;
            }

        }

        /// <summary>
        /// Check Overlaps Geometry 
        /// </summary>
        /// <param name="spPolygonLst"></param>
        /// <returns></returns>
        public static bool CheckOverlaps(List<ServicePolygonClass> spPolygonLst, Jobs jobtask, bool isconstr)
        {
            //List<string> overlapDesigations = new List<string>();
            bool chkResult = true;
            for (int i = 0; i < spPolygonLst.Count; i++)
            {
                List<ServicePolygonClass> overlapErrFeatures = spPolygonLst.FindAll(delegate (ServicePolygonClass sp)
                { return sp.FIBER_CABLE_ID != spPolygonLst[i].FIBER_CABLE_ID && FindIntersection(sp.polyGeometry, spPolygonLst[i].polyGeometry) > 0; });
                if (overlapErrFeatures.Count > 0)
                {
                    for (int j = 0; j < overlapErrFeatures.Count; j++)
                    {
                        if (isconstr)
                        {
                            string desgNDSi = spPolygonLst[i].FIBER_CABLE_ID + ":" + spPolygonLst[i].NDS_JOB_NO;
                            string desgNDSj = overlapErrFeatures[j].FIBER_CABLE_ID + ":" + overlapErrFeatures[j].NDS_JOB_NO;
                            jobtask.designationLst[desgNDSj] = new List<string> {
                                JobPolygonStatusProgressEnum.deignationException.ToString(), Constants.Messages.OverlapExceptionMsg(desgNDSi,desgNDSj)};
                            overlapErrFeatures[j].Polygon_Status = (int)JobPolygonStatusProgressEnum.Polygon_Completed_with_Exception;
                        }
                        else
                        {
                            jobtask.designationLst[overlapErrFeatures[j].FIBER_CABLE_ID] = new List<string>{
                                JobPolygonStatusProgressEnum.deignationException.ToString(), Constants.Messages.OverlapExceptionMsg(spPolygonLst[i].FIBER_CABLE_ID,overlapErrFeatures[j].FIBER_CABLE_ID)};
                            overlapErrFeatures[j].Polygon_Status = (int)JobPolygonStatusProgressEnum.Polygon_Completed_with_Exception;
                        }

                    }
                }

                //check MultiPart
                //if(IsGeometryMultiPart(spPolygonLst[i].polyGeometry))
                //{
                //    jobtask.designationLst[spPolygonLst[i].FIBER_CABLE_ID] = new List<string>
                //    { JobPolygonStatusProgressEnum.deignationException.ToString(), Constants.Messages.MultiPartExceptionMessage(spPolygonLst[i].FIBER_CABLE_ID)};
                //    spPolygonLst[i].Polygon_Status = (int)JobPolygonStatusProgressEnum.Polygon_Completed_with_Exception;
                //}

            }
            return chkResult;
        }

        /// <summary>
        /// Finding intersection between two features...
        /// </summary>
        /// <param name="f1"></param>
        /// <param name="f2"></param>
        /// <returns></returns>
        public static double FindIntersection(IGeometry f1, IGeometry f2)
        {
            ITopologicalOperator ptopo2 = f2 as ITopologicalOperator;
            ptopo2.Simplify();

            double area = 0.0;
            ITopologicalOperator ptopo = f1 as ITopologicalOperator;
            ptopo.Simplify();

            IGeometry ppGon = ptopo.Intersect(f2, esriGeometryDimension.esriGeometry2Dimension);
            IArea pAr = ppGon as IArea;
            area = pAr.Area;
            return area;
        }

        internal static bool IsGeometryMultiPart(IGeometry geom)
        {
            bool Multipart = false;
            IPointCollection pcol = new PolygonClass();
            try
            {
                pcol.AddPointCollection(geom as IPointCollection);
                IPolygon polygon = pcol as IPolygon;
                if (polygon.ExteriorRingCount > 1)
                    Multipart = true;
            }
            catch (Exception ex)
            {
                LogManager.WriteLogandConsole(ex);
            }
            return Multipart;
        }

    }
}
