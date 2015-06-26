using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;

namespace SWATPerformanceTest
{
    /// <summary>
    /// Performance of data reading from SWAT outputs
    /// </summary>
    class DataReadingPerformance
    {
        /// <summary>
        /// Maximum number of ids to be tested
        /// </summary>
        /// <remarks>The maximum number of test cases will be MAX_NUM_TESTED_IDS * MAX_NUM_TESTED_COLUMNS</remarks>
        private static int MAX_NUM_TESTED_IDS = 10;

        /// <summary>
        /// Maximum number of columns to be tested
        /// </summary>
        /// <remarks>The maximum number of test cases will be MAX_NUM_TESTED_IDS * MAX_NUM_TESTED_COLUMNS</remarks>
        private static int MAX_NUM_TESTED_COLUMNS = 20;

        /// <summary>
        /// Maximum number of years to be tested
        /// </summary>
        /// <remarks>The maximum number of test cases will be MAX_NUM_TESTED_IDS * MAX_NUM_TESTED_COLUMNS</remarks>
        private static int MAX_NUM_TESTED_YEARS = 10;

        private string _txtinoutPath = "";
        private ExtractSWAT_Text _modelInfo = null;

        public DataReadingPerformance(string txtinoutPath)
        {
            _txtinoutPath = txtinoutPath;
            _modelInfo = new ExtractSWAT_Text(_txtinoutPath);
        }

        /// <summary>
        /// Read data from SWAT and SWAT-SQLite output files
        /// </summary>
        /// <param name="isQueryEachYear">If read data for each year</param>
        /// <returns></returns>
        public string Read(bool isQueryEachYear)
        {
            string finalOutputFile = Path.Combine(getOutputFolder(isQueryEachYear,_modelInfo.OutputInterval), 
                string.Format("{0}_final_{1}.csv",
                _modelInfo.OutputInterval.ToString().ToLower(),isQueryEachYear ? "each_year" : "all_year"));

            using (StreamWriter file = new StreamWriter(finalOutputFile))
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("SWAT Unit,Method,Prepare Time(ms),Average Exraction Time(ms)");

                //sb.AppendLine(Read(DataReadingMethodType.SQLite, isQueryEachYear));
                sb.Append(Read(UnitType.SUB, isQueryEachYear));
                sb.Append(Read(UnitType.RCH, isQueryEachYear));
                sb.Append(Read(UnitType.HRU, isQueryEachYear));

                file.WriteLine(sb);
                return sb.ToString();
            }
        }


        /// <summary>
        /// Read data with given method
        /// </summary>
        /// <param name="method">Data reading method</param>
        /// <returns>Output message including the data reading time</returns>
        /// <remarks>Support subbasin, reach and HRU data</remarks>
        public string Read(DataReadingMethodType method, bool isQueryEachYear)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = (int)(UnitType.SUB); i <= (int)(UnitType.HRU); i++)
            {
                UnitType source = (UnitType)i;
                Console.WriteLine(string.Format("******* {0} *******", source));
                sb.AppendLine(string.Format("{0},{1},{2}", source,method, Read(source, method, isQueryEachYear)));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Read data from given SWAT unit
        /// </summary>
        /// <param name="source">SWAT unit type, support subbasin, reach and HRU</param>
        /// <returns>Output message including the data reading time</returns>
        /// <remarks>Support four methods: file driver, file helper, SWAT Plot and SQLite</remarks>
        public string Read(UnitType source, bool isQueryEachYear)
        {
           StringBuilder sb = new StringBuilder();
           for (int i = (int)(DataReadingMethodType.SQLite); i <= (int)(DataReadingMethodType.SWATPlot); i++)
           {
               DataReadingMethodType type = (DataReadingMethodType)(i);
               Console.WriteLine(string.Format("******* {0} *******", type));               
               sb.AppendLine(string.Format("{0},{1},{2}", source, type, Read(source, type, isQueryEachYear)));
           }
           return sb.ToString();
        }

        private string getOutputFolder(bool isQueryEachYear, OutputIntervalType interval)
        {
            string workingpath = Path.Combine(Directory.GetCurrentDirectory(), "extract_test");
            if (!Directory.Exists(workingpath)) Directory.CreateDirectory(workingpath);

            string yearOption = isQueryEachYear ? "each_year" : "all_year";
            workingpath = Path.Combine(workingpath, yearOption);
            if (!Directory.Exists(workingpath)) Directory.CreateDirectory(workingpath);

            workingpath = Path.Combine(workingpath, interval.ToString());
            if (!Directory.Exists(workingpath)) Directory.CreateDirectory(workingpath);

            return workingpath;
        }

        private string getOutputFile(bool isQueryEachYear, OutputIntervalType interval, UnitType source, DataReadingMethodType method)
        {
            return Path.Combine(getOutputFolder(isQueryEachYear,interval),
                string.Format("{0}_{1}_{2}_{3}.csv", source, method, 
                isQueryEachYear ? "each_year" : "all_year",
                interval));
        }

        /// <summary>
        /// Read data from given SWAT unit using given method
        /// </summary>
        /// <param name="source">SWAT unit type</param>
        /// <param name="method">Data reading method</param>
        /// <returns>Average reading time</returns>
        /// <remarks>Read data of all columns and all ids and then calcuate the average data reading time for one column and one id</remarks>
        public string Read(UnitType source, DataReadingMethodType method, bool isQueryEachYear)
        {
            //if (source == UnitType.HRU && method == DataReadingMethodType.SWATPlot)
            //    return 0.0;
            System.Diagnostics.Debug.WriteLine(string.Format("******* {0} {1} *******",source,method));
            using (ExtractSWAT ex = ExtractSWAT.ExtractFromMethod(method, _txtinoutPath))
            {
                //get number of record and SWAT unit
                int numofRecord = _modelInfo.NumberOfRecordForEachUnit;
                int numUnits = _modelInfo.NumberofSubbasin;
                if (source == UnitType.HRU) numUnits = _modelInfo.NumberofHRU;
                if (numUnits > MAX_NUM_TESTED_IDS) numUnits = MAX_NUM_TESTED_IDS; //limit the number of ids to be tested to reduce the running time, especially for HRU
                
                //all columns
                //this should be dynamic
                string[] cols = ExtractSWAT_SQLite.GetSQLiteColumns(source);
                int numCols = Math.Min(cols.Length, MAX_NUM_TESTED_COLUMNS); //limit the number of ids to be tested to reduce the running time, especially for HRU

                //get number of year
                int numYears = 1; 
                if(isQueryEachYear)
                    numYears = Math.Min(_modelInfo.EndYear - _modelInfo.StartYear + 1, MAX_NUM_TESTED_YEARS);

                //initialize
                DataTable dt = null;
                double reading_time = 0.0;

                //get output file
                string outputFile = getOutputFile(isQueryEachYear, _modelInfo.OutputInterval, source, method);

                using (StreamWriter file = new StreamWriter(outputFile))
                {
                    //add header
                    file.WriteLine("Column,Time");

                    //start to read
                    for(int colIndex = 0;colIndex < numCols; colIndex ++)
                    {
                        string column = cols[colIndex];
                        Console.Write(column.PadRight(20));
                        double col_time = 0.0;//data reading time for current column
                        for (int id = 1; id <= numUnits; id++)
                        {
                            //ouput the id
                            if (id > 1) Console.Write(",");
                            Console.Write(id);


                            if (!isQueryEachYear)   //all years
                            {
                                //read data
                                dt = ex.Extract(source, -1, id, column, true);

                                //make sure the number of record is same
                                if (dt.Rows.Count != numofRecord)
                                    throw new Exception(string.Format("Wrong number of records from {0} {1} on column {2} and id {3}!", source, method, column, id));

                                //add the reading time to the column reading time
                                col_time += ex.ExtractTime;
                            }
                            else //each year
                            {
                                for (int yearIndex = 0; yearIndex <= numYears; yearIndex++)
                                {
                                    int year = _modelInfo.StartYear + yearIndex;

                                    //read data
                                    dt = ex.Extract(source, year, id, column, true);

                                    //add the reading time to the column reading time
                                    col_time += ex.ExtractTime;
                                }
                            }

                        }
                        Console.WriteLine("");

                        //calculate the average column reading time
                        //and output in debug window to make sure all column have similar reading time
                        col_time /= numUnits * numYears;
                        file.WriteLine(string.Format("{0},{1:F4}", column, col_time));
                        System.Diagnostics.Debug.WriteLine(string.Format("{0}: {1:F4}",column,col_time));
                    
                        //add to total reading time
                        reading_time += col_time;
                    }
                }
                return string.Format("{0},{1}",ex.PrepareTime, reading_time / numCols);
             }
        }

    }
}
