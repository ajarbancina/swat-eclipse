using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace SWATPerformanceTest
{
    /// <summary>
    /// Vaidate SQLite results are identical with regular text results
    /// </summary>
    class SQLiteValidation
    {
        private ExtractSWAT_SQLite _extractSQLite;
        private ExtractSWAT_Text_SWATPlot _extractText;
        private int _startYear;
        private int _endYear;

        public SQLiteValidation(string scenariosDir, string scenarioName, OutputIntervalType interval)
        {
            _extractSQLite = new ExtractSWAT_SQLite(scenariosDir, scenarioName,interval);
            _extractText = new ExtractSWAT_Text_SWATPlot(scenariosDir,scenarioName);

            _startYear = _extractText.StartYear;
            _endYear = _extractText.EndYear;
        }

        private int getNumberofUnit(UnitType source)
        {
            if (source == UnitType.HRU || source == UnitType.WATER) return _extractText.NumberofHRU;
            if (source == UnitType.RCH || source == UnitType.SUB) return _extractText.NumberofSubbasin;
            if (source == UnitType.RSV) return _extractText.NumberofReservor;
            return -1;
        }

        public void Compare()
        {            
            Compare(UnitType.RCH);
            Compare(UnitType.RSV);
            Compare(UnitType.SUB);
            Compare(UnitType.HRU);
        }

        public double Compare(UnitType source)
        {
            //System.Diagnostics.Debug.WriteLine("------------------" + source.ToString() + "------------------");

            using(System.IO.StreamWriter file = new System.IO.StreamWriter(
                System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop), source.ToString() + "_validation.txt")))
                {
                    string[] cols = ExtractSWAT_SQLite.GetSQLiteColumns(source);
                    if (cols == null) return -99.0;

                    double mean_R2 = 0;
                    int num_R2 = 0;
                    foreach (string col in cols)
                    {
                        double R2 = Compare(source, col,file);
                        if (R2 > -99)
                        {
                            mean_R2 += R2;
                            num_R2 += 1;
                        }
                    }
                    
                    if (num_R2 > 0)
                        return mean_R2 / num_R2;
                    return -99.0;
                }
        }

        /// <summary>
        /// Validate SQLite results for given unit type and column. Here same column name are assumed for bother
        /// SQLite and regular text file. But some column names have been changed in SQLite results. For these 
        /// columns, a lookup table should be used.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="var"></param>
        /// <returns></returns>
        public double Compare(UnitType source, string var, System.IO.StreamWriter file)
        {
            //System.Diagnostics.Debug.WriteLine("***************" + var.Trim() + "***************");

            int num = getNumberofUnit(source);
            if (source == UnitType.RSV) num = getNumberofUnit(UnitType.SUB);
            if (num == -1) return -99.0;

            double mean_R2 = 0;
            int num_R2 = 0;

            for (int i = 1; i <= num; i++)
            {
                double R2 = Compare(_startYear, _endYear, source, i, var);
                if (R2 > -99)
                {
                    mean_R2 += R2;
                    num_R2 += 1;
                    System.Diagnostics.Debug.WriteLine(string.Format("{0}-{1}-{2},{3:F4}", source, i, var.Trim(), R2));
                    file.WriteLine(string.Format("{0},{1},{2},{3:F4}", source, i, var.Trim(), R2));
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(string.Format("{0}-{1}-{2},No Record", source, i, var.Trim()));
                    file.WriteLine(string.Format("{0},{1},{2},NoRecord", source, i, var.Trim()));
                }
            }

            if (num_R2 > 0)
                return mean_R2 / num_R2;
            return -99.0;
                
        }

        private double Compare(int startYear, int endYear, UnitType source, int id, string var)
        {
            string col_sqlite = var.Trim();
            string col_text = ExtractSWAT_SQLite.ColumnSQLite2Text(source,var).Trim();
            DataTable dtSQLite = _extractSQLite.Extract(startYear, endYear, source, id, col_sqlite);
            DataTable dtText = _extractText.Extract(startYear, endYear, source, id, col_text);

            if (dtSQLite == null || dtText == null) return -99.0;
            if (dtSQLite.Rows.Count == 0 || dtText.Rows.Count == 0) return -99.0;

            //Console.WriteLine(string.Format("Extract time for {0}-{1}-{2}-{3}-{4}: SQLite = {5:F4} ms, Text = {6:F4} ms",
            //    startYear, endYear, source, id, var, _extractSQLite.ExtractTime,_extractText.ExtractTime));

            //the join table structure
            DataTable dt = new DataTable();
            dt.Columns.Add("TIME", typeof(DateTime));
            dt.Columns.Add("SQLite", typeof(double));
            dt.Columns.Add("Text", typeof(double));

            //join these two tables using Linq
            var results = from table1 in dtSQLite.AsEnumerable()
                          join table2 in dtText.AsEnumerable() on table1["TIME"] equals table2["TIME"]
                          select dt.LoadDataRow(new object[]
                              {
                                  table1["TIME"],
                                  table1[col_sqlite],
                                  table2[col_text]
                              }, false);

            results.CopyToDataTable();

            return CalculateR2(dt, "SQLite", "Text", "");
        }

        #region R2 Calculation

        public static double EMPTY_VALUE = -99.0;

        private static double Compute(DataTable dt, string expression, string filter)
        {
            object result = dt.Compute(expression, filter);
            if (result is System.DBNull)
                return EMPTY_VALUE;
            double value = EMPTY_VALUE;
            double.TryParse(result.ToString(), out value);
            return value;
        }

        private static double Average(DataTable dt, string col, string filter)
        {
            return Compute(dt, string.Format("Avg({0})", col), filter);
        }

        private static double Sum(DataTable dt, string col, string filter)
        {
            return Compute(dt, string.Format("Sum({0})", col), filter);
        }

        private static double Variance(DataTable dt, string col, string filter)
        {
            double var = Compute(dt, string.Format("Var({0})", col), filter);
            if (var == EMPTY_VALUE) return var;

            return var * (dt.Select(filter).Length - 1);
        }

        static double CalculateR2(DataTable dt, string col_observed, string col_simulated, string filter)
        {
            //consider missing value in observed data
            //some year just doesn't have data
            if (dt == null || dt.Rows.Count == 0)
                return EMPTY_VALUE;

            double ave_observed = Average(dt, col_observed, filter);
            double ave_simulated = Average(dt, col_simulated, filter);

            //see if all values in the time series are 0
            if (ave_observed == 0 || ave_simulated == 0)
                return 1.0; //all zero, identical

            //add a new colum R2_TOP for [(Oi-Oave) * (Pi-Pave)]
            string col_top = "R2_TOP";
            if (dt.Columns.Contains(col_top))
                dt.Columns.Remove(col_top);

            DataColumn col = new DataColumn(col_top, typeof(double));
            col.Expression = string.Format("({0} - {1}) * ({2} - {3})",
                col_observed, ave_observed, col_simulated, ave_simulated);
            dt.Columns.Add(col);

            //get top value
            double top = Sum(dt, col_top, filter);
            top *= top;

            double var_observed = Variance(dt, col_observed, filter);
            double var_simulated = Variance(dt, col_simulated, filter);

            double r2 = EMPTY_VALUE;
            if (var_observed >= 0.000001 && var_simulated >= 0.000001)
                r2 = top / var_observed / var_simulated;
            return r2;
        }

        #endregion
    }
}
