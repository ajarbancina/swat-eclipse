using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FileHelpers;
using System.Data;

namespace SWATPerformanceTest
{
    /// <summary>
    /// Class to read data from SWAT output files using database approach, including File Driver and File Helper
    /// </summary>
    /// <remarks>
    /// 1. The whole table will be loaded first in memory
    /// 2. The following query all from the whole table
    /// </remarks>
    class ExtractSWAT_Text_Database : ExtractSWAT_Text
    {
        public ExtractSWAT_Text_Database(string txtInOutPath)
            : base(txtInOutPath)
        {
        }

        /// <summary>
        /// Only cache the whole tables to save memory
        /// </summary>
        protected Dictionary<UnitType, DataTable> _wholeTables = new Dictionary<UnitType, DataTable>();
        
        protected virtual DataTable getWholeTable(UnitType source)
        {
            throw new Exception("Haven't been implemented!");
        }

        protected virtual DataTable extractDailyHRU(string column)
        {
            throw new Exception("Haven't been implemented!");
        }

        public override System.Data.DataTable Extract(UnitType source, int id, string column, 
            bool addTimeColumn = false, bool forValidation = true)
        {
            if (source == UnitType.WATER)
                throw new Exception("ExtractSWAT_Text_Database doesn't support " + source.ToString());

            DateTime startTime = DateTime.Now;
            _extractTime = -99.0;

            DataTable finalTable = null;
            //if (_interval == OutputIntervalType.DAY && source == UnitType.HRU) //record-by-record reading for daily HRU outputs to avoid out of memory
            //{
            //    finalTable = extractDailyHRU(column);
            //}
            //else
            //{
                //read the whole table if necessary
                DataTable wholeTable = getWholeTable(source);

                //select the request data from the whole table
                //Console.WriteLine(string.Format("Query data for {0}_{1}_{2}", source,id,column));
                DataView view = new DataView(wholeTable);
                string filter = "";
                if (id > 0) filter = string.Format("{0} = {1}", source, id);           //filter for certain id and remove the year summary,this is not good for yearly output
                if (_interval == OutputIntervalType.DAY || _interval == OutputIntervalType.MON)
                {
                    if (!string.IsNullOrWhiteSpace(filter)) filter += " and ";
                    filter += COLUMN_NAME_MON_SWAT + " <= 366";
                }
                view.RowFilter = filter;

                string timeCol = COLUMN_NAME_MON_SWAT;
                DataTable queryTable = null;
                if (id > 0)
                    queryTable = view.ToTable(false, new string[] { timeCol, column });                     //time and value
                else
                    queryTable = view.ToTable(false, new string[] { timeCol, source.ToString(), column }); //output id when all ids is outputs
                finalTable = queryTable;

                //add time if necessary
                //don't do this on the whole dataset any more
                if (addTimeColumn) calculateDate(finalTable);   
            //}
            _extractTime = DateTime.Now.Subtract(startTime).TotalMilliseconds;
            return finalTable;
        }
    }
}
