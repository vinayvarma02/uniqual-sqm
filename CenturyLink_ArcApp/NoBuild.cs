using ESRI.ArcGIS.Geometry;

namespace CenturyLink_ArcApp
{
    public class NoBuild
    {
        public int PolygonID { get; set; }
        public string Createmode { get; set; }
        public int Addr_id { get; set; }
        public string Latlon { get; set; }
        public string STATUS { get; set; }
        public string REASON { get; set; }
        public string WC_ID { get; set; }
        public string Datetim { get; set; }
        public int PolygonStatus { get; set; }
        public IGeometry GEOEMTRY { get; set; }
    }
}
