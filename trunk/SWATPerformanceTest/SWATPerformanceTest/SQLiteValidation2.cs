using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;

namespace SWATPerformanceTest
{
    /// <summary>
    /// Vaidate SQLite results are identical with regular text results
    /// </summary>
    /// <remarks> 
    /// Use ExtractSWAT_Text_FileHelperAsyncEngine to read data from text files. It load the whole dataset
    /// into a datatable first and then given the opportunity to select partial data based on year, id and column. 
    /// This approach would avoid load the output file repeatedly, which is better than SWATPlot. But it would fail
    /// when the output file is huge, like daily HRU output file. It will throw the out of memory exception.
    /// </remarks>
    class SQLiteValidation2
    {
        private ExtractSWAT_SQLite _extractSQLite;
        private ExtractSWAT_Text_FileHelperEngine _extractText;

        /// <summary>
        /// Initialize the data extraction class
        /// </summary>
        /// <param name="txtinoutPath"></param>
        public SQLiteValidation2(string txtinoutPath)
        {            
            _extractText = new ExtractSWAT_Text_FileHelperEngine(txtinoutPath);
            _extractSQLite = new ExtractSWAT_SQLite(txtinoutPath, _extractText.OutputInterval);
        }

        /// <summary>
        /// Compare outputs in text files and SQLite database.
        /// </summary>
        /// <remarks>It supports Reach, Subbasin, Reservoir and HRU. It may run of memory for daily HRU outputs.</remarks>
        public void Compare()
        {            
            Compare(UnitType.RCH);
            Compare(UnitType.SUB);
            Compare(UnitType.RSV);
            Compare(UnitType.HRU);
        }

        /// <summary>
        /// Compare outputs in text files and SQLite database for given SWAT unit
        /// </summary>
        /// <param name="source">SWAT unit type</param>
        /// <returns>Average R2</returns>
        /// <remarks>
        /// 1. R2 is calculated for each column
        /// 2. A text file would be created on desktop to record R2 for all columns
        /// </remarks>
        public double Compare(UnitType source)
        {
            using(StreamWriter file = new StreamWriter(
                Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop), source.ToString() + "_" + _extractText.OutputInterval.ToString() + "_validation.txt")))
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
        /// Compare outputs in text files and SQLite database for given SWAT unit and column
        /// </summary>
        /// <param name="source">SWAT Unit Type</param>
        /// <param name="var">Name of column</param>
        /// <returns>R2</returns>
        public double Compare(UnitType source, string var, System.IO.StreamWriter file)
        {
            //System.Diagnostics.Debug.WriteLine("***************" + var.Trim() + "***************");

            double R2 = Compare(source, -1, var);
            if (R2 > -99)
            {
                Console.WriteLine(string.Format("R2 {0}-{1}, {2:F4}", source, var.Trim(), R2));
                file.WriteLine(string.Format("{0},{1},{2:F4}", source, var.Trim(), R2));
            }
            else
            {
                Console.WriteLine(string.Format("R2 {0}-{1},No Record", source, var.Trim()));
                file.WriteLine(string.Format("{0},{1},NoRecord", source, var.Trim()));
            }
            return R2;    
        }

        /// <summary>
        /// Compare outputs in text files and SQLite database for given SWAT unit, column and id
        /// </summary>
        /// <param name="source">SWAT unit type</param>
        /// <param name="id">SWAT unit id, -1 means all ids</param>
        /// <param name="var">Name of column</param>
        /// <returns>R2</returns>
        private double Compare(UnitType source, int id, string var)
        {
            //Read the data first from SQLite and Text files
            string col_sqlite = var;
            DataTable dtSQLite = _extractSQLite.Extract(source, -1, id, col_sqlite,false,true);
            col_sqlite = col_sqlite.Trim();
            DataTable dtText = _extractText.Extract(source, id, -1, col_sqlite);

            if (dtSQLite == null || dtText == null) return -99.0;
            if (dtSQLite.Rows.Count == 0 || dtText.Rows.Count == 0) return -99.0;
            if (dtSQLite.Rows.Count != dtText.Rows.Count)
                throw new Exception("The number of rows are different from SQLite and Text.");

            //calculate R2
            //SQLite is the modeled value f, and text is the real value y
            //add two columns for R2 calculation
            if (!dtText.Columns.Contains("SUM_SQUARES"))
                dtText.Columns.Add("SUM_SQUARES", typeof(double));
            if (!dtText.Columns.Contains("SUM_SQUARES_RESIDUAL"))
                dtText.Columns.Add("SUM_SQUARES_RESIDUAL", typeof(double));

            double ave_y = Average(dtText, col_sqlite, "");
            double ave_sqlite = Average(dtSQLite, col_sqlite, "");
            if (ave_y == 0 || ave_sqlite == 0)
                return 1.0; //all zero, identical

            double value = EMPTY_VALUE;
            double value_sqlite = EMPTY_VALUE;
            for (int i = 0; i < dtText.Rows.Count; i++)
            {
                value = double.Parse(dtText.Rows[i][col_sqlite].ToString());
                dtText.Rows[i]["SUM_SQUARES"] = Math.Pow(value - ave_y, 2.0);

                value_sqlite = double.Parse(dtSQLite.Rows[i][col_sqlite].ToString());
                dtText.Rows[i]["SUM_SQUARES_RESIDUAL"] = Math.Pow(value - value_sqlite, 2.0);
            }

            double sum_square = Sum(dtText, "SUM_SQUARES", "");
            double sum_square_residual = Sum(dtText, "SUM_SQUARES_RESIDUAL", "");
            if (sum_square == 0)
            {
                //all values are same, so it's 0
                if (sum_square_residual == 0) return 1.0;
                else return sum_square_residual;
            }            
            else
                return 1 - sum_square_residual / sum_square;
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

        #endregion
    }
}
