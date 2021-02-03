namespace CenturyLink_ArcApp
{
    public static class Constants
    {
        public static class Messages
        {
            public const string PolygonFailureErrorMessage = "Service polygon Creation Failed: Verify Geometry at designation location";
          //  public const string PolygonOverlapExceptionMessage = "Polygon Created with Overlaps, Check the geometry";
            public const string PolygonDuplicateExceptionMessage = "Polygon Created with Duplicate Designations, Check the geometry";

            public const string noGeom4DesignationErrorMessage = "Service Polygon creation Failed : No Point Geometry found to process this Designation";

            public const string noRoadsFailureMesage = "Service Polygon creation Failed : No Roads Data found to create the polygon";

            public const string Copper_NoParcelfoundMessage = "Copper Polygon creation Failed : No Parcel Data found to create the polygon";
            public const string Copper_IncorrectGeometryMessage = "Copper Polygon creation Failed : Copper polygon size is far bigger than Parcel Geometry";
            public const string Copper_LatlonNoMessage = "Copper Polygon creation Failed : No lat long's found for the Livingunit ";
            public const string Copper_Nospeedmessage = "No Speed : No speed found for the Livingunit ";
            public const string Copper_Emptyspeedmessage = " Missing bandwidth";
            public const string Copper_PolygonExecutionProgress = "Polygon execution is in progress";
            public static string OverlapExceptionMsg(string desg1,string desg2)
            {
                return "Overlapping service qualification polygon : [ " + desg1 + " - " + desg2 + " ]";
            }
            public static string MultiPartExceptionMessage(string desg1)
            {
                return "Service Qualification Polygon Multi-Geometry : [ " + desg1 + " ]";
            }

        }
        public static class DBProperties
        {

            public const string server = "SERVER";
            public const string instance = "INSTANCE";
            public const string authMode = "AUTHENTICATION_MODE";
            public const string database = "DATABASE";
            public const string user = "USER";
            public const string password = "PASSWORD";
            public const string version = "VERSION";
           // public const string port = "Port";

           
        }
        public static class TerminalView
        {
            public const string dbName = "SWGISLOC.";
            public const string viewName = "Terminal_Data4SQM_";
        }
        public static class ConstructionView
        {
            public const string dbName = "SWGISLOC.";
            public const string viewName = "ConstructionView_";
        }
        public static class QualStatus
        {
            public const int InService = 1;
            public const int InConstruction = 2;
            public const int NoBuild = 3;

        }
        public static class QualType
        {
            public const int BIW = 1;
            public const int GPON = 2;
            public const int COPPER = 3;

            public const string strBIW = "BIW";
            public const string strGPON = "GPON";
            public const string strCOPPER = "COPPER";
        }
        public static class SQMCreationMode
        {
            public const string AutoCreationMode = "Automatic";
        }

    }
}
