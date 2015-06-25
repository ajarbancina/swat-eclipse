using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace SWATPerformanceTest
{
    /// <summary>
    /// Performance of data reading from SWAT outputs
    /// </summary>
    class DataReadingPerformance
    {
        private string _txtinoutPath = "";

        public DataReadingPerformance(string txtinoutPath)
        {
            _txtinoutPath = txtinoutPath;
        }        

        public string Read()
        {
            StringBuilder sb = new StringBuilder();

            //Read(DataReadingMethodType.SWATPlot);
            sb.AppendLine(Read(DataReadingMethodType.SWATPlot));
            //sb.AppendLine(Read(UnitType.SUB));
            //sb.AppendLine(Read(UnitType.RCH));
            //sb.AppendLine(Read(UnitType.HRU));

            return sb.ToString();
        }

        public string Read(DataReadingMethodType method)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(method);
            sb.Append("*** ");

            UnitType source = UnitType.SUB;
            //Console.WriteLine(string.Format("******* {0} *******", source));
            //sb.Append(string.Format(", {0}: {1:F4} ms", source, Read(source, method)));

            //source = UnitType.RCH;
            //Console.WriteLine(string.Format("******* {0} *******", source));
            //sb.Append(string.Format(", {0}: {1:F4} ms", source, Read(source, method)));

            source = UnitType.HRU;
            Console.WriteLine(string.Format("******* {0} *******", source));
            sb.Append(string.Format(", {0}: {1:F4} ms", source, Read(source, method)));

            return sb.ToString();
        }

        public string Read(UnitType source)
        {
           StringBuilder sb = new StringBuilder();
           sb.Append(source);
           sb.Append("*** ");
           for (int i = (int)(DataReadingMethodType.SQLite); i <= (int)(DataReadingMethodType.SWATPlot); i++)
           {
               DataReadingMethodType type = (DataReadingMethodType)(i);
               if (type == DataReadingMethodType.FileDriver) continue;
               if (type == DataReadingMethodType.FileHelper) continue;
               if (type == DataReadingMethodType.SWATPlot) continue;
               Console.WriteLine(string.Format("******* {0} *******", type));
               sb.Append(string.Format(", {0}: {1:F4} ms",type,Read(source, type)));
           }
           return sb.ToString();
        }

        public double Read(UnitType source, DataReadingMethodType method)
        {
            //if (source == UnitType.HRU && method == DataReadingMethodType.SWATPlot)
            //    return 0.0;
            using (ExtractSWAT ex = ExtractSWAT.ExtractFromMethod(method, _txtinoutPath))
            {
                ExtractSWAT_Text ex_text = new ExtractSWAT_Text(_txtinoutPath);
                int numofRecord = ex_text.NumberOfRecordForEachUnit;
                //int num = 2;
                int num = ex_text.NumberofSubbasin;
                if (source == UnitType.HRU) num = 1;// ex_text.NumberofHRU;
                double reading_time = 0.0;
                string[] cols = ExtractSWAT_SQLite.GetSQLiteColumns(source);
                DataTable dt = null;
                for(int colindex = 0;colindex < 20;colindex++)
                {
                    string column = cols[colindex];
                    Console.Write(column.PadRight(20));
                    double col_time = 0.0;//data reading time for current column
                    for (int id = 1; id <= num; id++)
                    {
                        //ouput the id
                        if (id > 1) Console.Write(",");
                        Console.Write(id); 
 
                        //read data
                        dt = ex.Extract(source, id, column, true);

                        //make sure the number of record is same
                        if (dt.Rows.Count != numofRecord)
                            throw new Exception(string.Format("Wrong number of records from {0} {1} on column {2} and id {3}!", source,method,column,id));
                        
                        //add the reading time to the column reading time
                        col_time += ex.ExtractTime;
                    }
                    Console.WriteLine("");

                    //calculate the average column reading time
                    col_time /= num;
                    System.Diagnostics.Debug.WriteLine(string.Format("{0}: {1:F4} ms",column,col_time));
                    
                    //add to total reading time
                    reading_time += col_time;
                }
                return reading_time/20;
            }
        }

    }
}
