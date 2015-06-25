using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Data;
using System.IO;
using System.Data.OleDb;
using System.Data.SQLite;
using FileHelpers;

namespace SWATPerformanceTest
{
    class Program
    {
        static void Main(string[] args)
        {
            //TestSQLiteComparedToText();
            //TestExtractFromText_OleDB();
            //TestExtractFromSQLite();
            //TestExtractFromText();
            //TestRunSWAT();
            TestOutputReadingSub();
            Console.WriteLine("DONE");
            Console.ReadLine();
            return;
        }

        #region Model Run Performance Test

        /// <summary>
        /// Test SWAT running time to compare SWAT and SWAT-SQLite. The two executables
        /// would be put in the same model folder. One executable will be run five times
        /// and then the average running time will be calculated.
        /// </summary>
        static void TestRunSWAT()
        {
            string workingpath = System.IO.Directory.GetCurrentDirectory();//use working directory
            System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(workingpath);
            System.IO.FileInfo[] files = dir.GetFiles("*.exe");
            Dictionary<string, double> runTimes = new Dictionary<string, double>();
            foreach (System.IO.FileInfo f in files)
            {
                //run all swat exes and give the execution time
                for (int i = 0; i <= 2; i++)
                {
                    runTimes.Add(string.Format("{0}_{1}",f.Name,i), RunSWAT(f.FullName, 5,i));
                }               
            }
            foreach (string n in runTimes.Keys)
            {
                Console.WriteLine(string.Format("{0}:{1} seconds",n,runTimes[n]));
            }
        }

        /// <summary>
        /// Run given SWAT executable and return the running time
        /// </summary>
        /// <param name="swatexe">The path of the executable</param>
        /// <param name="num">Number of times the executable will be run</param>
        /// <param name="iprint">The iprint that will be set to file.cio</param>
        /// <returns></returns>
        static double RunSWAT(string swatexe, int num, int iprint)
        {
            Console.WriteLine(string.Format("Runing {0} with iprint = {1}",swatexe,iprint));

            //change the iprint first
            //find file.cio
            System.IO.FileInfo info = new FileInfo(swatexe);
            string cioFile = info.DirectoryName + @"\file.cio";
            if (!System.IO.File.Exists(cioFile))
                throw new Exception("Couldn't find " + cioFile);

            //modify file.cio with given output interval, which is located in line 59
            string cio = null;
            using (System.IO.StreamReader reader = new StreamReader(cioFile))
            {
                cio = reader.ReadToEnd();
            }
            using (System.IO.StreamWriter writer = new StreamWriter(cioFile))
            {
                using (System.IO.StringReader reader = new StringReader(cio))
                {
                    string oneline = reader.ReadLine();
                    while (oneline != null)
                    {
                        if (oneline.Contains("IPRINT"))
                            oneline = string.Format("{0}    | IPRINT: print code (month, day, year)", iprint);
                        writer.WriteLine(oneline);
                        oneline = reader.ReadLine();
                    }
                }
            }

            //start to run
            return RunSWAT(swatexe, num);
        }

        /// <summary>
        /// Run given SWAT executable and return the running time
        /// </summary>
        /// <param name="swatexe">The path of the executable</param>
        /// <param name="num">Number of times the executable will be run</param>
        /// <returns></returns>
        static double RunSWAT(string swatexe, int num)
        {
            double timeUsed = 0.0;
            for (int i = 1; i <= num; i++)
            {
                Console.WriteLine(string.Format("Times: {0}", i));
                timeUsed += RunSWAT(swatexe) / num;
            }
            return timeUsed;
        }

        /// <summary>
        /// Run given SWAT executable and return the running time
        /// </summary>
        /// <param name="swatexe">The path of the executable</param>
        /// <returns></returns>
        static double RunSWAT(string swatexe)
        {
            using (Process myProcess = new Process())
            {
                try
                {
                    myProcess.StartInfo.UseShellExecute = false;
                    myProcess.StartInfo.FileName = swatexe;
                    myProcess.StartInfo.CreateNoWindow = true;
                    myProcess.StartInfo.RedirectStandardError = true;
                    myProcess.StartInfo.RedirectStandardOutput = true;
                    myProcess.OutputDataReceived += (sender, agrs) =>
                    {
                        if (agrs.Data != null) Console.WriteLine(agrs.Data);
                    };
                    myProcess.ErrorDataReceived += (sender, agrs) =>
                    {
                        if (agrs.Data != null) Console.WriteLine(agrs.Data);
                    };
                    DateTime before = DateTime.Now;
                    myProcess.Start();
                    myProcess.BeginOutputReadLine();
                    myProcess.BeginErrorReadLine();
                    myProcess.WaitForExit();
                    DateTime after = DateTime.Now;
                    return after.Subtract(before).TotalSeconds;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return -99.0;
                }
            }
        }

        #endregion

        #region Output Reading Performance Test

        static void outputDataTable(DataTable dt)
        {
            System.Diagnostics.Debug.WriteLine("**********");
            StringBuilder sb = new StringBuilder();
            foreach (DataRow r in dt.Rows)
            {
                sb.Clear();
                foreach (DataColumn col in dt.Columns)
                {
                    if (sb.Length > 0) sb.Append(",");
                    sb.Append(r[col]);                    
                }
                System.Diagnostics.Debug.WriteLine(sb);
            }
        }

        static void TestOutputReadingSub()
        {
            string workingpath = System.IO.Directory.GetCurrentDirectory();//use working directory
            string filePath = Path.Combine(workingpath, "outputsub.txt");
            string subPath = Path.Combine(workingpath, "output.sub");
            string sqlitePath = Path.Combine(workingpath, "result_627_monthly.db3");

            DataReadingPerformance per = new DataReadingPerformance(workingpath);
            string info = per.Read();
            Console.WriteLine(info);
            Debug.WriteLine(info);
            

            //TestExtractFromText_FileDriver(workingpath);

            //Console.WriteLine("** Text File FileHelperAsyncEngine **");
            //ExtractSWAT_Text_FileHelperEngine ex = new ExtractSWAT_Text_FileHelperEngine(workingpath);
            //dt = ex.Extract(UnitType.HRU, 1, "ETmm");
            //Console.WriteLine(ex.ExtractTime);
            //outputDataTable(dt);
            //dt = ex.Extract(UnitType.HRU, 2, "ETmm");
            //Console.WriteLine(ex.ExtractTime);
            //outputDataTable(dt);
            //dt = ex.Extract(UnitType.HRU, 3, "ETmm");
            //Console.WriteLine(ex.ExtractTime);
            //outputDataTable(dt);

            //Console.WriteLine("** Text File SWATPlot **");
            //ExtractSWAT_Text_SWATPlot ex_plot = new ExtractSWAT_Text_SWATPlot(
            //    @"E:\SWAT\Elie\Elie\Scenarios", "default");
            //dt = ex_plot.Extract(UnitType.HRU, 1, "ETmm");
            //Console.WriteLine(ex_plot.ExtractTime);
            //outputDataTable(dt);
            //dt = ex_plot.Extract(UnitType.HRU, 2, "ETmm");
            //Console.WriteLine(ex_plot.ExtractTime);
            //outputDataTable(dt);
            //dt = ex_plot.Extract(UnitType.HRU, 3, "ETmm");
            //Console.WriteLine(ex_plot.ExtractTime);
            //outputDataTable(dt);
            

            //Console.WriteLine("** SQLite **");
            //using (ExtractSWAT_SQLite extract = new ExtractSWAT_SQLite(sqlitePath))
            //{
            //    dt = extract.Extract(UnitType.HRU, 1, "ETmm");
            //    Console.WriteLine(extract.ExtractTime);
            //    outputDataTable(dt);
            //    dt = extract.Extract(UnitType.HRU, 2, "ETmm");
            //    Console.WriteLine(extract.ExtractTime);
            //    outputDataTable(dt);
            //    dt = extract.Extract(UnitType.HRU, 3, "ETmm");
            //    Console.WriteLine(extract.ExtractTime);
            //    outputDataTable(dt);
            //}


            //TestExtractFromText_FileDriver(filePath);
            //TestExtractFromText_TextFieldParser(subPath);
            //TestExtractFromText_FileHelperAsyncEngine(subPath);
            //TestExtractFromSQLite_WholeTable(sqlitePath);
        }

        /// <summary>
        /// Read data from SWAT output file using TextFieldParser
        /// </summary>
        /// <remarks>
        /// 1. Need to know the number and width of columns
        /// 2. Doesn't support select partial data
        /// </remarks>
        static void TestExtractFromText_TextFieldParser(string filePah)
        {
            Console.WriteLine("TextFieldParser");
            DateTime before = DateTime.Now;
            using (Microsoft.VisualBasic.FileIO.TextFieldParser reader = 
                new Microsoft.VisualBasic.FileIO.TextFieldParser(filePah))
            {
                reader.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.FixedWidth;
                reader.SetFieldWidths(6,4,10,4,
                    10,10,10,10,10,10,10,10,10,10,
                    10,10,10,10,10,10,10,10,10,11,
                    10,10,10,6);
                string[] currentRow = null;
                int i = 0;
                //ignore the first 9 lines
                while (!reader.EndOfData && i<9)
                {
                    reader.ReadLine(); 
                    i++;
                }
                while(!reader.EndOfData)
                   {
                       currentRow = reader.ReadFields();
                   }
            }
            DateTime after = DateTime.Now;
            Console.WriteLine(string.Format("******\nTime Used: {0} seconds\n******", after.Subtract(before).TotalSeconds));
        }

        /// <summary>
        /// Read data from SWAT output file using FileHelperAsyncEngine
        /// </summary>
        /// <remarks>
        /// 1. Need to define classes correponding to each data table
        /// 2. Get the data table directly
        /// </remarks>
        static void TestExtractFromText_FileHelperAsyncEngine(string filePah)
        {
            Console.WriteLine("FileHelperAsyncEngine");
            DateTime before = DateTime.Now;            
            FileHelperEngine engine = new FileHelperEngine(typeof(SWATSub));
            using (DataTable dt = engine.ReadFileAsDT(filePah))
            {
                DataRow[] rows = dt.Select("SUB=1");
                //foreach (DataRow r in rows)
                //{
                //    Console.WriteLine(string.Format("{0},{1},{2}", r["SUB"], r["MON"], r["PETmm"]));
                //}            
            }
            DateTime after = DateTime.Now;
            Console.WriteLine(string.Format("******\nTime Used: {0} seconds\n******", after.Subtract(before).TotalSeconds));

        }

        /// <summary>
        /// Read data from SWAT output file using OLEDB File Driver 
        /// </summary>
        /// <remarks>
        /// 1. This is the method used in ArcSWAT/SWAT_Editor to import data into mdb database 
        /// 2. Only support file with .txt extension
        /// </remarks>
        static void TestExtractFromText_FileDriver(string txtinoutPath)
        {
            ExtractSWAT_Text_FileDriver ex = new ExtractSWAT_Text_FileDriver(txtinoutPath);
            ex.Extract(UnitType.SUB, 1, "ETmm");
            ex.Extract(UnitType.SUB, 4, "ETmm");
            ex.Extract(UnitType.SUB, 10, "ETmm");

            ex.Extract(UnitType.HRU, 1, "ETmm");
            ex.Extract(UnitType.HRU, 4, "ETmm");
            ex.Extract(UnitType.HRU, 10, "ETmm");

            ex.Extract(UnitType.RCH, 1, "FLOW_OUTcms");
            ex.Extract(UnitType.RCH, 4, "FLOW_OUTcms");
            ex.Extract(UnitType.RCH, 10, "FLOW_OUTcms");
        }

        /// <summary>
        /// Read data from SQLite database
        /// </summary>
        /// <param name="sqlitePath"></param>
        static void TestExtractFromSQLite_WholeTable(string sqlitePath)
        {
            Console.WriteLine("SQLite"); 

            DateTime before = DateTime.Now;
            SQLiteConnectionStringBuilder s = new SQLiteConnectionStringBuilder();
            s.DataSource = sqlitePath;
            s.Version = 3;
            s.FailIfMissing = false;

            using (SQLiteConnection connection = new SQLiteConnection(
                s.ConnectionString))
            {
                connection.Open();
                DataTable dt = new DataTable();
                using (SQLiteDataAdapter a = new SQLiteDataAdapter("select * from sub where sub=1", connection))
                {
                    try
                    {
                        a.Fill(dt);
                    }
                    catch (System.Exception)
                    {
                        dt = null;
                    }
                }
                connection.Close();
            }
            DateTime after = DateTime.Now;
            Console.WriteLine(string.Format("******\nTime Used: {0} seconds\n******", after.Subtract(before).TotalSeconds));
        }

        /// <summary>
        /// Extract data from regular text file as datatable
        /// </summary>
        static void TestExtractFromText_SWATPlot(string txtinoutPath)
        {
            Console.WriteLine("******************** Text SWATPlot ********************");
            ExtractSWAT_Text_SWATPlot extract = new ExtractSWAT_Text_SWATPlot(txtinoutPath);

            Console.WriteLine("******************** First Try ********************");
            extract.Extract(UnitType.RCH, 1, "FLOW_OUTcms");//case sensitive
            extract.Extract(1993, UnitType.RCH, 1, "FLOW_OUTcms");
            extract.Extract(2000, UnitType.RCH, 1, "FLOW_OUTcms");
            extract.Extract(2007, UnitType.RCH, 1, "FLOW_OUTcms");
            extract.Extract(UnitType.HRU, 1, "ETmm");
            extract.Extract(1993, UnitType.HRU, 1, "ETmm");
            extract.Extract(2000, UnitType.HRU, 1, "ETmm");
            extract.Extract(2007, UnitType.HRU, 1, "ETmm");

            Console.WriteLine("******************** Second Try ********************");
            extract.Extract(UnitType.RCH, 1, "FLOW_OUTcms");//case sensitive
            extract.Extract(1993, UnitType.RCH, 1, "FLOW_OUTcms");
            extract.Extract(2000, UnitType.RCH, 1, "FLOW_OUTcms");
            extract.Extract(2007, UnitType.RCH, 1, "FLOW_OUTcms");
            extract.Extract(UnitType.HRU, 1, "ETmm");
            extract.Extract(1993, UnitType.HRU, 1, "ETmm");
            extract.Extract(2000, UnitType.HRU, 1, "ETmm");
            extract.Extract(2007, UnitType.HRU, 1, "ETmm");
        }

        /// <summary>
        /// Extract data from SQLite as datatable
        /// </summary>
        static void TestExtractFromSQLite()
        {
            Console.WriteLine("********************SQLite********************");
            using(ExtractSWAT_SQLite extract =
                new ExtractSWAT_SQLite(@"E:\SWAT\Elie\Elie\Scenarios\default\TxtInOut\result_627_daily.db3"))
            {
                Console.WriteLine("******************** First Try ********************");
                extract.Extract(UnitType.RCH, 1, "FLOW_OUTcms");//not case sensitive
                extract.Extract(1993, UnitType.RCH, 1, "FLOW_OUTcms");
                extract.Extract(2000, UnitType.RCH, 1, "FLOW_OUTcms");
                extract.Extract(2007, UnitType.RCH, 1, "FLOW_OUTcms");
                extract.Extract(UnitType.HRU, 1, "ETmm");
                extract.Extract(1993, UnitType.HRU, 1, "ETmm");
                extract.Extract(2000, UnitType.HRU, 1, "ETmm");
                extract.Extract(2007, UnitType.HRU, 1, "ETmm");

                Console.WriteLine("******************** Second Try ********************");
                extract.Extract(UnitType.RCH, 1, "FLOW_OUTcms");//not case sensitive
                extract.Extract(1993, UnitType.RCH, 1, "FLOW_OUTcms");
                extract.Extract(2000, UnitType.RCH, 1, "FLOW_OUTcms");
                extract.Extract(2007, UnitType.RCH, 1, "FLOW_OUTcms");
                extract.Extract(UnitType.HRU, 1, "ETmm");
                extract.Extract(1993, UnitType.HRU, 1, "ETmm");
                extract.Extract(2000, UnitType.HRU, 1, "ETmm");
                extract.Extract(2007, UnitType.HRU, 1, "ETmm");
            }            
        }

        #endregion

        #region Validation

        /// <summary>
        /// Compare resuts in SQLite database and regular text files. R2 is calculated to make sure
        /// the results are identical
        /// </summary>
        static void TestSQLiteComparedToText()
        {
            Console.WriteLine("********************SQLite vs Text********************");

            string workingpath = System.IO.Directory.GetCurrentDirectory();//use working directory
            SQLiteValidation2 test = new SQLiteValidation2(workingpath);
            try
            {
                test.Compare(UnitType.HRU);
            }
            catch (OutOfMemoryException)
            {
                Console.WriteLine("Out of Memory!");
            }
        }

        #endregion


    }
}
