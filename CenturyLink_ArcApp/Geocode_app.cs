/**
 * $Id$
 * 
 * Copyright (c) 2012 CoreLogic. All rights reserved.
 */
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

using CoreLogic.Commons;
using CoreLogic.PxPointSC;
using CoreLogic.PxPointSC.Constants;
using CoreLogic.PxPointSC.Spatial;
//using CoreLogic.Sample;

namespace CenturyLink_ArcApp
{
    /**
     * Convenience/demo program to do simple geocoding on the command line. User
     * passes in the following input values:
     * 
     * SingleCallGeocode <path to PxPoint data> -l <license file> -c <license code>
     * "address" "address"
     * 
     * 
     * Program uses PxPointSC to do the geocoding and outputs geocode results for
     * each address passed in
     * 
     */
    public class SingleCallGeocode
    {
        const string ID_COLUMN = "_ID_";          // Default primary-kay column name
        const string GEOM_COLUMN = "InputGeometry"; // name for column which contains lat/long geometries

        string pxDataPath_, pxLicenseFile_, pxLicenseCode_;
        static string[] inputVals_;
        string[] outColumns_ = Geocoder.DEFAULT_GEOCODE_COLUMNS;

        public SingleCallGeocode(string pxDataPath, string pxLicenseFile,
                string pxLicenseCode, string[] inputs)
        {
            pxDataPath_ = StripQuotes(pxDataPath);
            pxLicenseFile_ = StripQuotes(pxLicenseFile);
            pxLicenseCode_ = StripQuotes(pxLicenseCode);
            inputVals_ = new string[inputs.Length];
            for (int i = 0; i < inputs.Length; i++)
            {
                inputVals_[i] = StripQuotes(inputs[i]);
            }
            inputVals_ = inputs;
            // now verify that all the addresses passed in are in
            // address-line,city-line format
            for (int i = 0; i < inputVals_.Length; i++)
            {
                if (inputVals_[i].IndexOf(",") < 1)
                {
                    Console.WriteLine("Address " + inputVals_[i]
                            + " not in property address-line,city-line format");
                    Environment.Exit(-1);
                }
            }

        }

        public string[] DoGeocode()
        {
            string[] opLatlon = new string[2];
            if (inputVals_.Length == 1)
            {
                string[] pieces = inputVals_[0].Split(',');
                if (pieces.Length != 2)
                {
                    LogManager.WriteLogandConsole("Unable to parse address " + inputVals_[0]);
                    return opLatlon;
                }
                // do single address to geocode with convenience method
                try
                {
                    DataSet ds = Geocoder.BestGeocode(pieces[0], pieces[1]);
                    DataTable outTable = ds.Tables[TableNames.Output];
                    if (outTable.Rows.Count > 0)
                    {
                        for (int i = 0; i < outTable.Rows.Count; i++)
                        {
                            opLatlon[0] = outTable.Rows[i][0].ToString();
                            opLatlon[1] = outTable.Rows[i][1].ToString();
                        }


                            //Console.WriteLine(OutputAddress(outTable.Rows[i]));
                    }
                    else
                    {
                        //DataTable errs = ds.Tables[TableNames.Error];
                        //Console.WriteLine("Error geocoding input address "
                        //        + inputVals_[0]);
                        //Console.WriteLine(errs.ToString());
                    }
                }
                catch (Exception ex)
                {
                    LogManager.WriteLogandConsole("Error geocoding value " + inputVals_[0]);
                  ////  Console.WriteLine(ex.StackTrace);
                   // Environment.Exit(-1);
                }
            }
            else
            {
                // multiple addresses being passed on command line.
                // need to manually construct an input data table model with ID for
                // each input value and
                // address line and city line columns
                DataTable inModel = new DataTable();
                inModel.Columns.Add(ID_COLUMN, typeof(Int32));
                inModel.Columns.Add(ResultColumns.AddressLine);
                inModel.Columns.Add(ResultColumns.CityLine);
                for (int i = 0; i < inputVals_.Length; i++)
                {
                    string[] pieces = inputVals_[i].Split(',');
                    if (pieces.Length != 2)
                    {
                        Console.WriteLine("Unable to parse address "
                                + inputVals_[i]);
                        Environment.Exit(-1);
                    }
                    inModel.Rows.Add(new Object[] { i, pieces[0], pieces[1] });
                }
                // assume user just wants best match
                GeocodeCallSpec spec = new GeocodeCallSpec();
                spec.BestResultOnly = true;
                // specify output columns - this will include input line# and a
                // bunch of default columns
                // check output to see which ones these are
                spec.OutputColumns.Add(ID_COLUMN, true);
                spec.OutputColumns.AddRange(Geocoder.DEFAULT_GEOCODE_COLUMNS);
                spec.ErrorColumns.Add(ID_COLUMN, true);
                try
                {
                    DataSet ds = Geocoder.Geocode(inModel, spec);
                    // for every input line that successfully geocodes, there will
                    // be an line in the "Output" table
                    DataTable outTable = ds.Tables[TableNames.Output];
                    if (outTable.Rows.Count > 0)
                    {
                        Console.WriteLine("Results from input addresses: ");
                        for (int i = 0; i < outTable.Rows.Count; i++)
                        {
                            Console.WriteLine("\tAddress " + (i + 1) + ": " + OutputAddress(outTable.Rows[i]));
                        }
                    }
                    // there will only be one or more lines in the error table if
                    // PxPointSC had problems
                    // trying to geocode the input line
                    DataTable errTable = ds.Tables[TableNames.Error];
                    if (errTable.Rows.Count > 0)
                    {
                        Console.WriteLine("Addresses that resulted in errors: ");
                        for (int i = 0; i < errTable.Rows.Count; i++)
                            Console.WriteLine("\t"
                                    + ErrorAddress(outTable.Rows[i]));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error geocoding input data");
                    Console.WriteLine(ex.StackTrace);
                    Environment.Exit(-1);
                }
            }
            return opLatlon;
        }

        /**
         * Takes a row from the output table of a geocode call and formats it into a
         * readable tab-delimited output string
         * 
         * @param tableRow
         * @return
         */
        private string OutputAddress(DataRow tableRow)
        {
            StringBuilder outBuff = new StringBuilder();

            outBuff.Append("Address: ");
            outBuff.Append(tableRow[ResultColumns.AddressLine]);
            outBuff.Append("\tCity: ");
            outBuff.Append(tableRow[ResultColumns.City]);
            outBuff.Append("\tState: ");
            outBuff.Append(tableRow[ResultColumns.State]);
            outBuff.Append("\tZip: ");
            outBuff.Append(tableRow[ResultColumns.Postcode]);
            outBuff.Append("\tMatch Code: ");
            outBuff.Append(tableRow[ResultColumns.MatchCode]);
            outBuff.Append("\tLat/Long: ");
            outBuff.Append(tableRow[ResultColumns.Latitude]);
            outBuff.Append("/");
            outBuff.Append(tableRow[ResultColumns.Longitude]);
            return outBuff.ToString();
        }

        /**
         * Takes a row from the error table of a geocode call and formats it into a
         * readable tab-delimited output string
         * 
         * @param errorTableRow
         * @return
         */
        private string ErrorAddress(DataRow errorTableRow)
        {
            StringBuilder outBuff = new StringBuilder();

            outBuff.Append("Address ");
            int aNum = int.Parse((string)(errorTableRow[ID_COLUMN]));
            outBuff.Append((aNum + 1).ToString());
            outBuff.Append(": \tError Code: ");
            outBuff.Append(errorTableRow[ErrorColumns.ErrorCode]);
            outBuff.Append("\tError Message: ");
            outBuff.Append(errorTableRow[ErrorColumns.ErrorMessage]);
            return outBuff.ToString();
        }

        /**
         * Starts up PxPointSC with the passed in values from user. Exits program if
         * initialization is not successful for any reason
         */
        public void InitPxPoint()
        {
            int licCode = 0;
            // check to be sure user gave us a valid integer license code (we need
            // string --> int anyway)
            try
            {
                licCode = int.Parse(pxLicenseCode_);
            }
            catch (Exception ex)
            {
                Console.WriteLine("License code value must be an integer - found " + pxLicenseCode_);
                Environment.Exit(-1);
            }
            try
            {
                Geocoder.Init(pxDataPath_, DatasetNames.All, pxLicenseFile_, licCode);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error initializing PxPointSC");
                Console.WriteLine(ex.StackTrace);
                Environment.Exit(-1);
            }
        }

        /**
         * Exits PxPointSC, closing datasets etc.
         */
        public void ClosePxPoint()
        {
            Geocoder.Close();
        }

        /**
         * Gets rid of enclosing quotes on input token (if present)
         * 
         * @param instring
         * @return
         */
        private string StripQuotes(string instring)
        {
            int startChar = 0;
            int length = instring.Length;
            if (instring[0] == '"')
            {
                startChar = 1;
                length -= 2;
            }
            return instring.Substring(startChar, length);
        }

        //public static void GeoCodeMain(string[] args)
        //{
        //    // did user at least pass in enough parameters?
        //    if (args.Length < 6)
        //    {
        //        Console.WriteLine("Usage: SingleCallGeocode <path to PxPoint data-dir> -l <license file> -c <license code> \"address1\" ... \"address n\"");
        //        Environment.Exit(-1);
        //    }
        //    string pxLicenseFile = null;
        //    string pxLicenseCode = null;

        //    for (int i = 1; i < 5; i += 2)
        //    {
        //        if (args[i].Equals("-l"))
        //            pxLicenseFile = args[i + 1];
        //        else if (args[i].Equals("-c"))
        //            pxLicenseCode = args[i + 1];
        //    }
        //    string[] dataStuff = new string[args.Length - 5];
        //    for (int i = 5; i < args.Length; i++)
        //        dataStuff[i - 5] = args[i];
        //    // check to be sure user specified license file and license code params
        //    if ((null != pxLicenseFile) && (null != pxLicenseCode))
        //    {
        //        SingleCallGeocode scg = new SingleCallGeocode(args[0], pxLicenseFile, pxLicenseCode, dataStuff);
        //        // initialize single call - note this will exit the program if any
        //        // problems encountered
        //        scg.InitPxPoint();
        //        scg.DoGeocode();
        //        scg.ClosePxPoint();
        //    }
        //    else
        //    {
        //        Console.WriteLine("Usage: SingleCallGeocode <path to PxPoint data-dir> -l <license file> -c <license code> \"address1\" ... \"address n\"");
        //        Environment.Exit(-1);
        //    }
        //}
    }
}
