using ESRI.ArcGIS.esriSystem;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using configKey = CenturyLink_ArcApp.Properties.Settings;

namespace CenturyLink_ArcApp
{
    class Program
    {
        public static string tempfilesPath = configKey.Default.TempFilesPath;
        public static bool isExecutionResult = true;
        public static bool isTaskCancelled = false;

        private static string progressLogPath = configKey.Default.LogPath;
        private static LicenseInitializer m_AOLicenseInitializer = new CenturyLink_ArcApp.LicenseInitializer();

        [STAThread()]
        static void Main(string[] args)
        {

           // args = new string[] { "3932" }; //2908
            if (args.Length == 0)
            {
                string ss = configKey.Default.TempFilesPath;
                Console.WriteLine("Error: Invalid Arguments...");
                return;
            }
            LogManager.IntializeLog(progressLogPath + "UNIQUAL_SQM_Job" + args[0] + "_Log.txt");
            LogManager.WriteLogandConsole("Application Started..");
            tempfilesPath = tempfilesPath + args[0] + "\\";
            //ESRI License Initializer generated code.
            try
            {
                LogManager.WriteLogandConsole("INFO : Initializing ESRI Advanced License..");
                m_AOLicenseInitializer.InitializeApplication(new esriLicenseProductCode[] { esriLicenseProductCode.esriLicenseProductCodeAdvanced },
                new esriLicenseExtensionCode[] { });
            }
            catch (Exception ex) { LogManager.WriteLogandConsole(ex); }
            ProcessClass start = new ProcessClass();
            start.Process(args[0]);
            //ESRI License Initializer generated code.
            //Do not make any call to ArcObjects after ShutDownApplication()
            m_AOLicenseInitializer.ShutdownApplication();
            // Environment.Exit(0);
            Application.Exit();
        }
    }
}
