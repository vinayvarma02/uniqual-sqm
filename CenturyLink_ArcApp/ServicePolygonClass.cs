using ESRI.ArcGIS.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CenturyLink_ArcApp
{
   public  class ServicePolygonClass
    {
        public IGeometry polyGeometry;
        public int POLYGON_ID;
        public int TYPE;
        public int STATUS; //"In Service";      
        public string SERVIING_WIRE_CENTER_CLLI = string.Empty;
        public string SERVING_WIRE_CENTER_NAME = string.Empty;
        public string NDS_JOB_NO = string.Empty;
        public string AVAILABILITY_DATE;
        public string BANDWIDTH = string.Empty;
        public string FIBER_CABLE_ID = string.Empty;
        public string FIBER_LINE_OF_COUNT = string.Empty;
        public string OLT_RELATIONSHIP = string.Empty;
        public string CREATEMODE = Constants.SQMCreationMode.AutoCreationMode;
        public string LST_UPD_DT;

        public int Polygon_Status;

        public ServicePolygonClass()
        {
            if(string.IsNullOrEmpty(AVAILABILITY_DATE))
                AVAILABILITY_DATE = System.DateTime.Now.ToString("MM/dd/yyyy");   
            if(string.IsNullOrEmpty(LST_UPD_DT))
                LST_UPD_DT = System.DateTime.Now.ToString("MM/dd/yyyy");
        }

    }

}
