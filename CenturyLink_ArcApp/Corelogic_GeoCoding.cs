//using CoreLogic.PxPoint4S.JsonDotNet;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace CenturyLink_ArcApp
{
    class Corelogic_GeoCoding
    {
        //public void GeoCoding()
        //{
        //    try
        //    {

        //        var request = new
        //        {

        //            address = "W 51ST AVE"//"1600 Pennsylvania Avenue NW, Washington D.C. 20500"

        //        };

        //        JObject geocodeResult = PxPoint4S.GeocodeAddress(request);

        //        // There are many ways to parse the JObject returned by JSON.NET
        //        // We recommend casting into a Dictionary with key being string and value being dynamic
        //        // to accomodate varying data types. This often will lead to simpler intuitive code
        //        Dictionary<string, dynamic> geocodeResultDict = geocodeResult.ToObject<Dictionary<string, dynamic>>();

        //        // Did we find the address?
        //        // Result object only contains values if address was found.
        //        if (geocodeResultDict.Keys.Count > 0)
        //        {
        //            string bestDataset = geocodeResultDict["bestMatchDataset"];
        //            // Each geocode result is a list of potential matches. Each match is again a  dictionary of key value
        //            // pairs where value is dyanmic type.  
        //            List<Dictionary<string, dynamic>> bestResult = geocodeResultDict[bestDataset].ToObject<List<Dictionary<string, dynamic>>>();
        //            // PxPoint4S can return multiple results if it finds multiple records that
        //            // match the input result. Your application will need to decide how to handle multiple matches.
        //            // You can display them on map, or for batch operations treat them as no match.
        //            if (bestResult.Count > 1)
        //            {
        //                Console.WriteLine("Multiple Results found.");
        //            }
        //            else
        //            {
        //                Console.WriteLine("Latitude = " + bestResult[0]["Latitude"]);
        //                Console.WriteLine("Longitude = " + bestResult[0]["Longittude"]);
        //                Console.WriteLine("AddressLine = " + bestResult[0]["AddressLine"]);
        //                Console.WriteLine("CityLine = " + bestResult[0]["CityLine"]);
        //                Console.WriteLine("Dataset = " + bestResult[0]["Dataset"]);
        //                Console.WriteLine("MatchCode = " + bestResult[0]["MatchCode"]);
        //                Console.WriteLine("MatchDescription = " + bestResult[0]["MatchDescription"]);
        //            }
        //        }

        //        else
        //        {

        //            Console.WriteLine("Address could not be geocoded.");
        //        }

        //    }

        //    catch (PxPoint4S.Error err)
        //    {

        //        Console.WriteLine(err);

        //    }


        //}
    }
}
