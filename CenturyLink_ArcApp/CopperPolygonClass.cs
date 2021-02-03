using ESRI.ArcGIS.Geometry;
using System.Collections.Generic;

namespace CenturyLink_ArcApp
{
    public class CopperPolygonClass
    {
        public int ObjectID { get; set; }

        public int POLYGON_ID { get; set; }
        public IGeometry PolyGeometry { get; set; }
        public string LU_Lat { get; set; }
        public string LU_Lon { get; set; }
        public string Bandwidth { get; set; }
        public int QUAL_TYPE { get; set; }
        public int QUAL_STATUS { get; set; }
        public string AVAILABILITY_DATE { get; set; }
        public string WC_CLLI { get; set; }
        public string WC_NAME { get; set; }
        public string Cable_ID { get; set; }
        public int Copper_LOC { get; set; }
        public string NDS_JNO { get; set; }
        public string ONT_REL { get; set; }
        public string Creation_mode { get; set; }
        public string APN { get; set; }
        public string FIPS { get; set; }
        public int Parcel_ID { get; set; }
        public string LUID { get; set; }
        public int Parcel_Type { get; set; }
        public int Polygon_Status { get; set; }

        public Dictionary<string,string> dictLUID;

        public CopperPolygonClass()
        {
            if (string.IsNullOrEmpty(AVAILABILITY_DATE))
                AVAILABILITY_DATE = System.DateTime.Now.ToString("MM/dd/yyyy");
        }

    }
}
