using System;
using System.Diagnostics;
using System.IO;
using System.Collections;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Dashboard.net
{
    public class DataDealer
    {

        public static readonly string ChecklistKey = "checklist";

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
            if (Debugger.IsAttached) DataLocation = Environment.CurrentDirectory;
            else DataLocation = Path.Combine(Environment.
                GetFolderPath(Environment.SpecialFolder.ApplicationData), "Dashboard.net Data");

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
                (Newtonsoft.Json.Linq.JArray)data_hashtable[ChecklistKey];

            List<string> checkListDataFormatted = checklistData.ToObject<List<string>>();

            return new Hashtable
            {
                { ChecklistKey, checkListDataFormatted }
            };
        }


        /// <summary>
        /// Method for writing checklist data to the file.
        /// </summary>
        /// <param name="CheckListData"></param>
        public void WriteChecklistData(List<string> CheckListData)
        {
            Hashtable currentData = ReadData();
            currentData[ChecklistKey] = CheckListData;

            WriteData(currentData);
        }

        /// <summary>
        /// Reads the file and returns the checklist data.
        /// </summary>
        /// <returns>The checklist data.</returns>
        public List<string> GetCheckListData()
        {
            return (List<string>)ReadData()[ChecklistKey];
        }
    }
}
