using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data.OleDb;
using System.Data;

namespace SWATPerformanceTest
{
    /// <summary>
    /// Read data from SWAT output files using OLE DB File Driver
    /// </summary>
    class ExtractSWAT_Text_FileDriver : ExtractSWAT_Text_Database, IDisposable
    {
        private static string TEXT_FILE_NAME_SUB = "outputsub";
        private static string TEXT_FILE_NAME_HRU = "outputhru";
        private static string TEXT_FILE_NAME_RCH = "outputrch";

        public ExtractSWAT_Text_FileDriver(string txtinoutPath)
            : base(txtinoutPath)
        {
        }

        /// <summary>
        /// Get SQL for data query
        /// </summary>
        /// <param name="requestStartYear"></param>
        /// <param name="requestFinishYear"></param>
        /// <param name="source"></param>
        /// <param name="id"></param>
        /// <param name="var"></param>
        /// <returns></returns>
        /// <remarks>Still have no way to remove the average annual outputs for monthly and yearly HRU, subbain and reach</remarks>
        private string getSQL(int requestStartYear, int requestFinishYear,
            UnitType source, int id, string var)
        {
            //columns            
            string cols = COLUMN_NAME_MON_SWAT + "," + var;
            if (var.Equals("*")) cols = "*";

            //get table
            string table = TEXT_FILE_NAME_SUB;
            if (source == UnitType.HRU)
                table = TEXT_FILE_NAME_HRU;
            else if (source == UnitType.RCH)
                table = TEXT_FILE_NAME_RCH;

            table += "_" + _interval.ToString().ToLower() + ".txt";

            string col_id = source.ToString();

            //id, for wtr is not correct
            string idCondition = "";
            if (id > 0) idCondition = string.Format("{0}={1}", col_id, id);

            //year condition
            if (requestStartYear < _startYear) requestStartYear = _startYear;
            if (requestFinishYear > _endYear || requestFinishYear < _startYear) requestFinishYear = _endYear;
            string yearCondition = "";
            if (requestStartYear != _startYear || requestFinishYear != _endYear)
            {
                if (requestStartYear == requestFinishYear)
                    yearCondition = string.Format("YEAR({0}) = {1}", COLUMN_NAME_TIME, requestStartYear);
                else
                    yearCondition = string.Format("YEAR({2}) >= {0} AND YEAR({2}) <= {1}", requestStartYear, requestFinishYear, COLUMN_NAME_TIME);
            }

            string extraRecordCondition = "";
            if (_interval == OutputIntervalType.DAY || _interval == OutputIntervalType.MON)
                extraRecordCondition = "MON <= 366";

            string where = "";
            if (!string.IsNullOrWhiteSpace(idCondition))
                where = idCondition;
            if (!string.IsNullOrWhiteSpace(yearCondition))
            {
                if (!string.IsNullOrWhiteSpace(where))
                    where += " and ";
                where += yearCondition;
            }
            if (!string.IsNullOrWhiteSpace(extraRecordCondition))
            {
                if (!string.IsNullOrWhiteSpace(where))
                    where += " and ";
                where += extraRecordCondition;
            }

            string query = string.Format("select {0} from {1}", cols, table);
            if (!string.IsNullOrWhiteSpace(where)) query += " where " + where;

            return query;
        }

        private OleDbConnection _connection = null;

        private OleDbConnection Connection
        {
            get
            {
                if (_connection == null)
                {
                    _connection = new OleDbConnection(
                        "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" +
                        _txtInOutPath + ";Extended Properties='text;HDR=No;FMT=Fixed'");

                    _connection.Open();
                }
                
                return _connection;
            }            
        }

        //public override System.Data.DataTable Extract(UnitType source, int id, string column, 
        //    bool addTimeColumn = false, bool forValidation = false)
        //{
        //    if (source != UnitType.SUB && source != UnitType.HRU && source != UnitType.RCH)
        //        throw new Exception("Only support Subasin, Reach and HRU");

        //    //start time
        //    //the time to read data from SWAT output files using file driver includes
        //    //1. SQL construction
        //    //2. Database connection
        //    //3. Execute SQL
        //    //4. Add time column and calcualte date to be comparable with SWAT plot
        //    DateTime startTime = DateTime.Now;
        //    _extractTime = -99.0;

        //    //get sql
        //    string sql = getSQL(-1, -1, source, id, column);

        //    //do the query            
        //    DataTable dt = Extract(sql);

        //    //remove the average annual if necessary
        //    //it's the last record
        //    if (hasAverageAnnual(source)) dt.Rows.RemoveAt(dt.Rows.Count - 1);                

        //    //add datetime column and calculate the date
        //    if (addTimeColumn && dt != null) calculateDate(dt); 
            
        //    //get the data reading time
        //    _extractTime = DateTime.Now.Subtract(startTime).TotalMilliseconds;

        //    return dt;
        //}

        protected override DataTable extractDailyHRU(string column)
        {
            return base.extractDailyHRU(column);
        }
        protected override DataTable getWholeTable(UnitType source)
        {
            if (!_wholeTables.ContainsKey(source)) //read the whole table first
            {
                string sql = getSQL(-1, -1, source, -1, "*");
                DataTable dt = Extract(sql);

                //remove the average annual if necessary
                if (hasAverageAnnual(source)) 
                {
                    int ignorenum = getNumberOfLinesForAverageAnnualOutput(source);
                    for(int i=0;i<ignorenum;i++)
                        dt.Rows.RemoveAt(dt.Rows.Count - 1); 
                }

                dt.TableName = source.ToString();
                _wholeTables.Add(source, dt);
            }
            return _wholeTables[source];
        }

        private DataTable Extract(string query)
        {
            DataTable dt = new DataTable();
            using (OleDbDataAdapter a = new OleDbDataAdapter(query, Connection))
            {
                try
                {
                    a.Fill(dt);
                }
                catch (System.Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("Query: " + query);
                    System.Diagnostics.Debug.WriteLine(e.ToString());
                    throw new Exception("Wrong File Driver query: " + query);
                }
            }
            return dt;
        }

        public override void Dispose()
        {
            if (_connection != null && _connection.State != ConnectionState.Closed)
                _connection.Close();
        }
    }
}
