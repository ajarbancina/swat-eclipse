using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;

namespace SWAT_SQLite_Result.ArcSWAT
{
    /// <summary>
    /// One type result of a SWAT Unit
    /// </summary>
    public class SWATUnitResult
    {
        public static string COLUMN_NAME_DATE = "DATE1";

        private SWATUnit _unit = null;
        private string _tableName = null;
        private SWATResultIntervalType _interval = SWATResultIntervalType.UNKNOWN;
        private Dictionary<string, SWATUnitColumnYearResult> _results = new Dictionary<string, SWATUnitColumnYearResult>();
        
        private StringCollection _columns = null;

        public SWATUnitResult(string tableName, SWATUnit parentUnit)
        {
            _tableName = tableName;
            _unit = parentUnit;
        }

        /// <summary>
        /// format string for get data function to get date for a specific day.
        /// Only used for daily data to speed up
        /// </summary>
        private static string STRING_FORMAT_GET_DATA_SPECIFIC_DAY_DAILY =
            "select {0} from {1} where " +
            ScenarioResultStructure.COLUMN_NAME_YEAR + "={2} and "+
            ScenarioResultStructure.COLUMN_NAME_MONTH +"={3} and "+
            ScenarioResultStructure.COLUMN_NAME_DAY +  "={4} and {5}={6}";

        /// <summary>
        /// Calculate average annual results for given column
        /// </summary>
        /// <param name="col"></param>
        /// <returns></returns>
        public double getData(string col)
        {
            DataTable dt = getDataTable(col);

            //determine right summary method based on result type 
            //string summary = "sum";
            //if (_unit.Type == SWATUnitType.RCH && System.Array.IndexOf(ScenarioResultStructure.REACH_UNIT_COLUMNS, col.ToLower()) > -1)
            //    summary = "avg";

            var query = from oneresult in dt.AsEnumerable()
                        group oneresult by "YR" into g
                        select new 
                        {
                            Year = g.Key,
                            Total = g.Sum(oneresult => oneresult.Field<double>(col)),
                        };

            double avg = 0.0;
            double num = 0;
            foreach (var oneyear in query)
            {
                avg += oneyear.Total;
                num += 1;
            }
            return avg / num;

            //int startYear = _unit.Scenario.StartYear;
            //int endYear = _unit.Scenario.EndYear;

            //double avg = 0.0;
            //for (int i = startYear; i <= endYear; i++)
            //{
            //    double v = getData(col, i);
            //    if (v == ScenarioResultStructure.EMPTY_VALUE)
            //    {
            //        System.Diagnostics.Debug.WriteLine(string.Format("No annual value for year {0}",i));
            //        continue;
            //    }
            //    avg += v;
            //}
            //return avg / (endYear - startYear + 1);
        }

        /// <summary>
        /// Calculate annual result for given column in given year. If the output interval is yearly, it's same as getData(string col, DateTime date). 
        /// For monthly and daily output, a table needs to be read first.
        /// 
        /// How to get the yearly value will be based on output. For flow, it will be average. For load, it will be total.
        /// </summary>
        /// <param name="col"></param>
        /// <param name="year"></param>
        /// <returns></returns>
        public double getData(string col, int year)
        {
            if (_interval == SWATResultIntervalType.YEARLY) return getData(col, new DateTime(year, 1, 1));

            DataTable dt = getDataTable(col, year);

            //determine right summary method based on result type 
            string summary = "sum";
            if (_unit.Type == SWATUnitType.RCH && System.Array.IndexOf(ScenarioResultStructure.REACH_UNIT_COLUMNS, col.ToLower()) > -1)
                summary = "avg";

            object value = dt.Compute(string.Format("{0}({1})", summary, col), "");
            if (value == null || value.ToString().Trim().Length == 0) return ScenarioResultStructure.EMPTY_VALUE;
            return double.Parse(value.ToString());            
        }
    
        /// <summary>
        /// read result for given column and date
        /// </summary>
        /// <param name="col"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public double getData(string col, DateTime date)
        {
            if (Interval == SWATResultIntervalType.DAILY)
            {
                //specially method for daily
                string sql = string.Format(STRING_FORMAT_GET_DATA_SPECIFIC_DAY_DAILY,
                    col, Name,
                    date.Year, date.Month, date.Day,
                    Unit.Type, Unit.ID);

                DataTable dt = Unit.Scenario.GetDataTable(sql);
                if(dt.Rows.Count == 0) return ScenarioResultStructure.EMPTY_VALUE;

                RowItem item = new RowItem(dt.Rows[0]);
                return item.getColumnValue_Double(0);
            }
            else
            {            
                DataTable dt = getDataTable(col,date.Year);
                if (dt.Rows.Count == 0) return ScenarioResultStructure.EMPTY_VALUE;

                string filter = string.Format("{0}='{1:yyyy-MM-dd}'",COLUMN_NAME_DATE,date);
                DataRow[] rows = dt.Select(filter);
                if (rows.Length == 0) return ScenarioResultStructure.EMPTY_VALUE;

                RowItem item = new RowItem(rows[0]);
                return item.getColumnValue_Double(col);
            }
        }

        /// <summary>
        /// read result for given column and year
        /// </summary>
        /// <param name="col"></param>
        /// <param name="year"></param>
        /// <returns></returns>
        public DataTable getDataTable(string col,int year)
        {
            return getResult(col, year).Table;
        }

        /// <summary>
        /// Read result for given column
        /// </summary>
        /// <param name="col"></param>
        /// <returns></returns>
        public DataTable getDataTable(string col)
        {
            return getDataTable(col, -1);          
        }

        public Statistics getStatistics(string col, int year)
        {
            return getResult(col, year).Statistics;
        }

        public SWATUnitColumnYearResult getResult(string col, int year)
        {
            if (!Columns.Contains(col)) return null;

            //see if the result is already there
            string id = SWATUnitColumnYearResult.getUniqueResultID(col, year);
            if (!_results.ContainsKey(id)) _results.Add(id, new SWATUnitColumnYearResult(col, year, this));
            return _results[id];
        }

        /// <summary>
        /// Name of the result, also the table name
        /// </summary>
        public string Name { get { return _tableName; } }

        /// <summary>
        /// Result interval
        /// </summary>
        public SWATResultIntervalType Interval
        {
            get
            {
                if(_interval == SWATResultIntervalType.UNKNOWN)
                    _interval = _unit.Scenario.Structure.getInterval(Name);
                return _interval;
            }
        }

        /// <summary>
        /// Data Columns
        /// </summary>
        public StringCollection Columns { get { if (_columns == null) _columns = _unit.Scenario.Structure.getDataColumns(Name); return _columns; } }


        public SWATUnit Unit { get { return _unit; } }

        #region Performace Table

        private Dictionary<string, DataTable> _performanceTableYearly = new Dictionary<string, DataTable>();

        /// <summary>
        /// get the performace table for given column
        /// </summary>
        /// <param name="col"></param>
        /// <returns></returns>
        public DataTable getYearlyPerformanceTable(string col,ArcSWAT.StatisticCompareType statisticType)
        {
            string tableName = string.Format("performance_{0}_{1}", col, statisticType);
            if (!_performanceTableYearly.ContainsKey(tableName))
            {
                //create the table
                DataTable dt = new DataTable(tableName);
                dt.Columns.Add("Year", typeof(Int32));
                for (int j = (int)(SeasonType.WholeYear); j <= (int)(SeasonType.HydrologicalYear); j++)
                    dt.Columns.Add(((SeasonType)j).ToString(), typeof(double));

                for (int i = this.Unit.Scenario.StartYear; i <= this.Unit.Scenario.EndYear; i++)
                {
                    ArcSWAT.SWATUnitColumnYearResult r = getResult(col, i);
                    if (r != null)
                    {
                        DataRow newRow = dt.NewRow();
                        newRow[0] = i;

                        for(int j=(int)(SeasonType.WholeYear);j<=(int)(SeasonType.HydrologicalYear);j++)
                            newRow[j] = Math.Round(r.CompareWithObserved.SeasonStatistics((SeasonType)j).Statistic
                                ("",statisticType),4);
                        dt.Rows.Add(newRow);
                    }
                }
                _performanceTableYearly[tableName] = dt;                
            }
            return _performanceTableYearly[tableName];
        }

        

        #endregion
    }
}
