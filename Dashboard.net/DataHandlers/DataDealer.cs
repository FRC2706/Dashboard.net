using System;
using System.Diagnostics;
using System.IO;
using System.Collections;
using Newtonsoft.Json;
using System.Collections.Generic;
using Dashboard.net.Element_Controllers;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;

namespace Dashboard.net.DataHandlers
{
    public static class DataDealer
    {

        public static readonly string CHECKLISTKEY = "checklist", CAUTIONERKEY = "cautioner", CONSTANTSKEY = "constants";

        private static string dataLocation;
        /// <summary>
        /// The location of the data json file.
        /// </summary>
        public static string DataLocation
        {
            get
            {
                // If the variable for the location is null or empty, fix it.
                if (string.IsNullOrEmpty(dataLocation))
                {
                    dataLocation = Path.Combine(Environment.
                        GetFolderPath(Environment.SpecialFolder.ApplicationData), "Dashboard.net Data");

                    // If we're debugging or the data location doesn't exist, change it to the current directory
                    dataLocation = (Debugger.IsAttached || !Directory.Exists(dataLocation)) ? Environment.CurrentDirectory : dataLocation;
                }
                return dataLocation;
            }
        }
        /// <summary>
        /// The location of the data file that contains the data.
        /// </summary>
        public static string DataFileLocation
        {
            get
            {
                return Path.Combine(DataLocation, "data.json");
            }
        }

        static object key = new object();

        /// <summary>
        /// Returns true if the data file exists, false otherwise.
        /// </summary>
        private static bool DataFileExists
        {
            get
            {
                return File.Exists(DataFileLocation);
            }
        }

        /// <summary>
        /// Creates the data file, overwritting it if it doesn't exist.
        /// </summary>
        private static void CreateDataFile()
        {
            File.Create(DataFileLocation).Close();
        }

        #region Basic read write functions
        /// <summary>
        /// Encodes the data hashtable into json and writes it to the data file.
        /// </summary>
        /// <param name="dataToWrite"></param>
        private static void WriteData(Hashtable dataToWrite)
        {
            // Lock this segment so that other threads don't cause problems.
            lock (key)
            {
                // Make sure the file exists
                if (!DataFileExists) CreateDataFile();

                // Serialize hashtable
                string writable = JsonConvert.SerializeObject(dataToWrite);

                // Make new writer
                StreamWriter writer = new StreamWriter(DataFileLocation);

                // Write data.
                writer.Write(writable);

                // Close writer
                writer.Close();
            }
        }

        private static Hashtable ReadData()
        {
            string fileContents = "";
            // Lock this segment so other threads don't cause problems.
            lock (key)
            {
                if (!DataFileExists)
                {
                    CreateDataFile();
                    return new Hashtable();
                }

                StreamReader reader = new StreamReader(DataFileLocation);
                fileContents = reader.ReadLine();
                reader.Close();
            }
            if (fileContents == "" || fileContents == null) return new Hashtable();

            Hashtable data_hashtable =
                (Hashtable)JsonConvert.DeserializeObject<Hashtable>(fileContents) ;

            // Make hashtable of the data that will be returned at the end.
            Hashtable dataToBeReturned = new Hashtable();
            foreach (string key in data_hashtable.Keys)
            {
                var data = data_hashtable[key];
                if (data is JArray)
                {
                    dataToBeReturned[key] = (JArray)data;
                    Type type = dataToBeReturned[key].GetType();
                }
                else if (data is JObject)
                {
                    dataToBeReturned[key] = (JObject)data;
                }
            }

            return dataToBeReturned;
        }
        #endregion

        /// <summary>
        /// Replaces the current value of the key provided with the new value
        /// </summary>
        /// <param name="key">The key to replace</param>
        /// <param name="newValue">The new value to set that key to</param>
        private static void UpdateAndWrite(string key, object newValue)
        {
            Hashtable currentData = ReadData();
            currentData[key] = newValue;

            WriteData(currentData);
        }

        #region Checklist read write methods
        /// <summary>
        /// Method for writing checklist data to the file.
        /// </summary>
        /// <param name="CheckListData"></param>
        public static void WriteChecklistData(List<string> CheckListData)
        {
            UpdateAndWrite(CHECKLISTKEY, CheckListData);
        }

        /// <summary>
        /// Reads the file and returns the checklist data.
        /// </summary>
        /// <returns>The checklist data.</returns>
        public static List<string> ReadCheckListData()
        {
            // Read and convert then return.
            return ((JArray)ReadData()[CHECKLISTKEY])?.ToObject<List<string>>();
        }

        #endregion

        #region Cautioner read write methods
        /// <summary>
        /// Writes new checklist data to the file
        /// </summary>
        /// <param name="data">The new data to write to the file</param>
        public static void WriteCautionerData(Hashtable data)
        {
            UpdateAndWrite(CAUTIONERKEY, data);
        }
        /// <summary>
        /// Reads the cautioner data and returns it
        /// </summary>
        /// <returns>The cautioner data present on file</returns>
        public static Hashtable ReadCautionerData()
        {
            Hashtable goodData = new Hashtable();
            // Read and convert the data
            Hashtable data = ((JObject)(ReadData()[CAUTIONERKEY]))?.ToObject<Hashtable>();
            if (data == null) return null;

            goodData[Cautioner.ENABLEDKEY] = data[Cautioner.ENABLEDKEY];
            goodData[Cautioner.IGNOREKEY] = ((JArray)data[Cautioner.IGNOREKEY]).ToObject<ObservableCollection<string>>();

            return goodData;
        }
        #endregion

        #region ConstantMaster read write methods
        /// <summary>
        /// Writes the constants data to the file in order to be retrieved later
        /// </summary>
        /// <param name="data">The constants to write to file</param>
        public static void WriteConstants(Hashtable data)
        {
            UpdateAndWrite(CONSTANTSKEY, data);
        }

        public static Hashtable ReadConstants()
        {
            // Read the data and return it.
            Hashtable readData = ((JObject)ReadData()[CONSTANTSKEY])?.ToObject<Hashtable>();
            return readData;
        }
        #endregion
    }
}
