using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;

namespace SWATPerformanceTest
{
    class ExtractSWAT_Text : ExtractSWAT
    {
        protected string _txtInOutPath;

        public ExtractSWAT_Text(string txtInOutPath)
        {
            _txtInOutPath = txtInOutPath;
            getModelSettings();
        }
 
        /// <summary>
        /// Get output file path corresponding to given type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        protected string getOutputFileFromType(UnitType type)
        {
            return getFilePath(string.Format("output.{0}",type));
        }

        /// <summary>
        /// Get file path corresponding to the file name
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        protected string getFilePath(string fileName)
        {
            return System.IO.Path.Combine(_txtInOutPath, fileName);
        }

        public int StartYear { get { return _startYear; } }
        public int EndYear { get { return _endYear; } }
        public OutputIntervalType OutputInterval { get { return _interval; } }

        public int NumberOfRecordForEachUnit
        {
            get
            {
                if (_interval == OutputIntervalType.YEAR) return _endYear - _startYear + 1;
                else if (_interval == OutputIntervalType.MON) return (_endYear - _startYear + 1) * 12;
                else
                {
                    DateTime d1 = new DateTime(_startYear, 1, 1);
                    DateTime d2 = new DateTime(_endYear + 1, 1, 1);
                    return (int)(d2.Subtract(d1).TotalDays);
                }
            }
            
        }

        /// <summary>
        /// Get the number of lines used for average annual output
        /// </summary>
        /// <param name="type">SWAT Unit Type</param>
        /// <returns></returns>
        /// <remarks>
        /// The average annual output is added to the end of the output file of HRU, Subbasin and Reach when output interval is month and year. 
        /// They needs to be ignored when read output data.
        /// </remarks>
        protected int getNumberOfLinesForAverageAnnualOutput(UnitType type)
        {
            if (type == UnitType.RSV || type == UnitType.WATER) return 0;

            if (_interval == OutputIntervalType.MON || _interval == OutputIntervalType.YEAR)
            {
                if (type == UnitType.HRU) return _numofHRU;
                if (type == UnitType.SUB || type == UnitType.RCH) return _numofSub;
            }
            return 0;
        }

        protected bool hasAverageAnnual(UnitType type)
        {
            return getNumberOfLinesForAverageAnnualOutput(type) > 0;
        }

        protected int _startYear = -1;
        protected int _endYear = -1;
        protected OutputIntervalType _interval = OutputIntervalType.UNKNOWN;
        protected int _numofHRU = -1;
        protected int _numofSub = -1;
        protected int _numofRes = -1;
        
        public int NumberofHRU
        {
            get
            {
                return _numofHRU;
            }
        }

        public int NumberofSubbasin
        {
            get
            {
                return _numofSub;
            }
        }

        public int NumberofReservor
        {
            get
            {
                return _numofRes;
            }
        }

        protected void getModelSettings()
        {
            //get start year and output interval
            string cioFile = this.getFilePath("file.cio");

            string line;
            // get number of records
            int numYears;
            
            using (StreamReader sr = new StreamReader(cioFile))
            {
                // skip 7 lines
                for (int i = 1; i <= 7; i++) sr.ReadLine();
                line = sr.ReadLine();
                numYears = Int32.Parse(line.Substring(0, 16));
                line = sr.ReadLine();
                _startYear = Int32.Parse(line.Substring(0, 16));
                _endYear = _startYear + numYears - 1;
                line = sr.ReadLine();
                line = sr.ReadLine();
                // skip 47 lines, so next is line 59
                for (int i = 1; i <= 47; i++) sr.ReadLine();
                line = sr.ReadLine();
                _interval = (OutputIntervalType)(Int32.Parse(line.Substring(0, 16)));
                line = sr.ReadLine();
                int NYSKIP = Int32.Parse(line.Substring(0, 16));
                _startYear += NYSKIP;
            }

            //get number of hru, subbasin and reservoir from the output file
            _numofHRU = getNumberOfUnit(UnitType.HRU);
            _numofSub = getNumberOfUnit(UnitType.SUB);
            _numofRes = getNumberOfUnit(UnitType.RSV);
        }

        private int getNumberOfUnit(UnitType type)
        {
            string outputFileName = string.Format("output.{0}",type.ToString().ToLower());
            string outputFile = getFilePath(outputFileName);
            if (!File.Exists(outputFile)) return 0;
            
            using (StreamReader sr = new StreamReader(outputFile))
            {
                for (int i = 1; i <= 9; i++) sr.ReadLine(); //ignore first 9 lines
                int previousID = -1;
                string line = "";
                    
                //get the start index and length of id in output file
                int startIndex = 4; //for hru
                int idLength = 5;   //for hru
                if (type == UnitType.SUB)
                {
                    startIndex = 6;
                    idLength = 4;
                }
                else if (type == UnitType.RSV)
                {
                    startIndex = 3;
                    idLength = 11;
                }

                //start to read
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    int currentID = int.Parse(line.Substring(startIndex, idLength).Trim());
                    if (currentID < previousID) break;
                    else previousID = currentID;
                }
                return previousID;
            }
        }

        protected static string COLUMN_NAME_MON_SWAT = "MON";
        protected static string COLUMN_NAME_TIME = "TIME";

        /// <summary>
        /// Add time column to the datatable and calculate the time using start year and the Julian day. 
        /// </summary>
        /// <param name="dt"></param>
        /// <remarks>This is useful when display in a table or chart</remarks>
        protected void calculateDate(DataTable dt)
        {
            //add date column and caculate time from Julian day            
            dt.Columns.Add(COLUMN_NAME_TIME, typeof(DateTime));

            //for daily
            if (_interval == OutputIntervalType.DAY)
            {
                int year = _startYear - 1;
                DateTime firstDayOfYear = DateTime.Now;
                double previousDay = -1;
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    double JulianDay = double.Parse(dt.Rows[i][COLUMN_NAME_MON_SWAT].ToString());
                    if (JulianDay > 366) continue;
                    if (i == 0 || (JulianDay == 1 && JulianDay < previousDay))
                    {
                        year += 1;
                        firstDayOfYear = new DateTime(year, 1, 1);
                    }
                    previousDay = JulianDay;

                    dt.Rows[i][COLUMN_NAME_TIME] = firstDayOfYear.AddDays(JulianDay - 1);
                }
            }
            else if (_interval == OutputIntervalType.MON)
            {
                int year = _startYear - 1;
                int previousMonth = -1;
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    int month = int.Parse(dt.Rows[i][COLUMN_NAME_MON_SWAT].ToString());
                    if (month > 12) continue;
                    if (i == 0 || (month == 1 && previousMonth > month)) year += 1;
                    previousMonth = month;

                    dt.Rows[i][COLUMN_NAME_TIME] = new DateTime(year, month, 1);
                }
            }
            else
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    int year = int.Parse(dt.Rows[i][COLUMN_NAME_MON_SWAT].ToString());
                    dt.Rows[i][COLUMN_NAME_TIME] = new DateTime(year, 1, 1);
                }
            }
        }


    }
}
