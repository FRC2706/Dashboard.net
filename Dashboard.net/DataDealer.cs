using System;
using System.Diagnostics;
using System.IO;
using System.Collections;
using Newtonsoft.Json;
using System.Collections.Generic;
using Dashboard.net.Element_Controllers;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;

namespace Dashboard.net
{
    public class DataDealer
    {

        public static readonly string CHECKLISTKEY = "checklist";
        public static readonly string CAUTIONERKEY = "cautioner";

        /// <summary>
        /// The location of the data json file.
        /// </summary>
        public static string DataLocation { get; private set; }
        public static string DataFileLocation { get; private set; }

        static object key = new object();

        /// <summary>
        /// Returns true if the data file exists, false otherwise.
        /// </summary>
        private bool DataFileExists
        {
            get
            {
                return File.Exists(DataFileLocation);
            }
        }

        public DataDealer()
        {
            DataLocation = Path.Combine(Environment.
                GetFolderPath(Environment.SpecialFolder.ApplicationData), "Dashboard.net Data");
            if (Debugger.IsAttached || !Directory.Exists(DataLocation)) DataLocation = Environment.CurrentDirectory;

            // Set the data file location.
            DataFileLocation = Path.Combine(DataLocation, "data.json");
        }

        /// <summary>
        /// Creates the data file, overwritting it if it doesn't exist.
        /// </summary>
        private void CreateDataFile()
        {
            File.Create(DataFileLocation).Close();
        }

        #region Basic read write functions
        /// <summary>
        /// Encodes the data hashtable into json and writes it to the data file.
        /// </summary>
        /// <param name="dataToWrite"></param>
        private void WriteData(Hashtable dataToWrite)
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

        private Hashtable ReadData()
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
                (Hashtable)JsonConvert.DeserializeObject<Hashtable>(fileContents);

            Newtonsoft.Json.Linq.JArray checklistData =
                (Newtonsoft.Json.Linq.JArray)data_hashtable[CHECKLISTKEY];
            Newtonsoft.Json.Linq.JObject cautionerData =
                (Newtonsoft.Json.Linq.JObject)data_hashtable[CAUTIONERKEY];

            List<string> checkListDataFormatted = (checklistData != null) ?checklistData.ToObject<List<string>>() : null;
            Hashtable cautionerDataFormatted = (cautionerData != null) ? cautionerData.ToObject<Hashtable>() : null;

            return new Hashtable
            {
                { CHECKLISTKEY, checkListDataFormatted },
                {CAUTIONERKEY, cautionerDataFormatted }
            };
        }
        #endregion

        /// <summary>
        /// Replaces the current value of the key provided with the new value
        /// </summary>
        /// <param name="key">The key to replace</param>
        /// <param name="newValue">The new value to set that key to</param>
        private void UpdateAndWrite(string key, object newValue)
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
        public void WriteChecklistData(List<string> CheckListData)
        {
            UpdateAndWrite(CHECKLISTKEY, CheckListData);
        }

        /// <summary>
        /// Reads the file and returns the checklist data.
        /// </summary>
        /// <returns>The checklist data.</returns>
        public List<string> ReadCheckListData()
        {
            return (List<string>)ReadData()[CHECKLISTKEY];
        }

        #endregion

        #region Cautioner read write methods
        /// <summary>
        /// Writes new checklist data to the file
        /// </summary>
        /// <param name="data">The new data to write to the file</param>
        public void WriteCautionerData(Hashtable data)
        {
            UpdateAndWrite(CAUTIONERKEY, data);
        }
        /// <summary>
        /// Reads the cautioner data and returns it
        /// </summary>
        /// <returns>The cautioner data present on file</returns>
        public Hashtable ReadCautionerData()
        {
            Hashtable goodData = new Hashtable();
            Hashtable data = (Hashtable)ReadData()[CAUTIONERKEY];

            goodData[Cautioner.ENABLEDKEY] = data[Cautioner.ENABLEDKEY];
            goodData[Cautioner.IGNOREKEY] = ((JArray)data[Cautioner.IGNOREKEY]).ToObject<ObservableCollection<string>>();

            return goodData;
        }
        #endregion
    }
}
