using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using System;
using System.Collections.Generic;
using System.Data;

namespace CenturyLink_ArcApp
{
    public interface IJob
    {
        int jobID { set; get; }
        string wireCenterID { set; get; }
        string wireCenterName { set; get; }
        string designations { set; get; }
        int jobStatus { set; get; } // Running,Completed
        string jobOwner { set; get; }
        string jobCreateDate { set; get; }
        DateTime jobDate { set; get; }
        IFeatureClass OutFeatureClass { set; get; }        
        List<IPolygon> NeedtoModifyPolygons { set; get; }
        int jobExecutionStatus { set; get; }
        int jobStartDate { set; get;}
        int jobEndDate { set; get;}

        int qualType { set; get; }

        int UpdateJobStatus(int status,int jobID);
        string GenerateTerminalQueryToRead(string viewName,string selPref);        
        int CreateViewforFeatureClass(string Query);        
        IFeatureClass GetFCfromSDE(string fCName, IPropertySet propertySet);

    }
    

    //}
}
