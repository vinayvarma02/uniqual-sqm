using System;
using System.Collections.Generic;
using System.IO;

namespace CenturyLink_ArcApp
{
    class LogManager
    {
        public static string FilePath { set; get; }
        public static void IntializeLog(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(FilePath))
                    return;
                FileInfo fi = new FileInfo(path);
                if (!Directory.Exists(fi.DirectoryName))
                    Directory.CreateDirectory(path);
                FilePath = path;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static void IntializeLog(string path, string name, List<string> headers, string errorMessage, string title)
        {
            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                FilePath = path + "\\" + name + ".log";
                WriteHead(FilePath, headers);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(errorMessage + "\n" + ex.Message, title,
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }
        public static void WriteHead(string fileName, List<string> headers)
        {
            if (string.IsNullOrEmpty(FilePath))
                return;

            using (StreamWriter writer = new StreamWriter(FilePath, true))
            {
                foreach (var headLine in headers)
                    writer.WriteLine(headLine);
                writer.WriteLine("******************************************************************************");
                writer.Close();
            }
        }
        public static void Log(string text)
        {
            if (string.IsNullOrEmpty(FilePath))
                return;
            using (StreamWriter writer = new StreamWriter(FilePath, true))
            {
                writer.WriteLine(string.Format(DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt") + " - {0}", text.ToUpper().Trim()));
                writer.Close();
            }
        }
        public static void Log(Exception ex)
        {
            if (string.IsNullOrEmpty(FilePath))
                return;

            using (StreamWriter writer = new StreamWriter(FilePath, true))
            {
                writer.WriteLine(string.Format(DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt") + " - EXCEPTION : {0}", ex.Message.ToUpper().Trim()));
                writer.WriteLine(string.Format(DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt") + " - EXCEPTION : {0}", ex.StackTrace.ToUpper().Trim()));
                writer.Close();
            }
        }
        public static void WriteLogandConsole(string LogMsg)
        {
            LogManager.Log(LogMsg);
            Console.WriteLine(string.Format(DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss tt") + " - {0}", LogMsg.ToUpper().Trim()));
        }
        public static void WriteLogandConsole(Exception LogMsg)
        {
            LogManager.Log(LogMsg);
            Console.WriteLine(string.Format(DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt") + " - EXCEPTION : {0}", 
                LogMsg.Message.ToUpper().Trim()));
        }
    }
}
