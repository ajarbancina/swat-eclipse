using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace SWATPerformanceTest
{
    /// <summary>
    /// Base class for all data reading classes
    /// </summary>
    class ExtractSWAT : IDisposable
    {
        protected double _extractTime = -99.0;

        /// <summary>
        /// Time used to extract given data in millisecond
        /// </summary>
        public double ExtractTime { get { return _extractTime; } }

        /// <summary>
        /// Extract data from given source for given id and column
        /// </summary>
        /// <param name="source">Type of SWAT Unit, including Subbasin, Reach and HRU</param>
        /// <param name="id">The unit id for subbasin, Reach and HRU</param>
        /// <param name="column">The name of column in result data table</param>
        /// <param name="addTimeColumn">If need to calcualte the date for each data record</param>
        /// <param name="forValidation">If this is for validation. The daily HRU table won't be cached to avoid memory problem.</param>
        /// <returns>Data Table</returns>
        public virtual DataTable Extract(UnitType source, int id, string column, 
            bool addTimeColumn = false, bool forValidation = false)
        {
            return null;
        }


        public static ExtractSWAT ExtractFromMethod(DataReadingMethodType method, string txtinoutPath)
        {
            switch (method)
            {
                case DataReadingMethodType.SQLite:
                    ExtractSWAT_Text ex_text = new ExtractSWAT_Text(txtinoutPath);
                    return new ExtractSWAT_SQLite(txtinoutPath, ex_text.OutputInterval);
                case DataReadingMethodType.FileDriver:
                    return new ExtractSWAT_Text_FileDriver(txtinoutPath);
                case DataReadingMethodType.FileHelper:
                    return new ExtractSWAT_Text_FileHelperEngine(txtinoutPath);
                case DataReadingMethodType.SWATPlot:
                    return new ExtractSWAT_Text_SWATPlot(txtinoutPath);
                default:
                    throw new Exception("Not support " + method.ToString());
            }
        }

        public virtual void Dispose()
        {
            //do nothing here
        }
    }
}
