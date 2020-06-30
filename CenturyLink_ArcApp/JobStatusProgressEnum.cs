namespace CenturyLink_ArcApp
{
    public enum JobPolygonStatusProgressEnum
    {
        Job_Created = 101,
        Job_Execution_InProgress = 102,
        Job_Execution_Cancellation_inprogress = 103,
        Job_Execution_Cancelled = 104,
        Job_Execution_Completed = 105,

        Polygon_Creation_In_Progress = 202,
        Polygon_Creation_Completed =203,
        Polygon_Completed_with_Exception = 204,
        Polygon_Completed_with_Error = 205,     

        //for failure and exceptions
        deignationCreated =0,designationSuccess = 1,deignationException = 2,designationFailed = 3,

        jobExceptionOverlaps = 0,
        jobFailureGeometry = 0            
       
    }
}
