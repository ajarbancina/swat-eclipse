using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FileHelpers;
using System.Data;

namespace SWATPerformanceTest
{
    /// <summary>
    /// Class to read data from SWAT output files
    /// </summary>
    /// <remarks>
    /// 1. FileHelperEngine is used to load the whole datatable first and then query request data.
    /// 2. Support HRU, Subbasin, Reach and Reservoir results right now.
    /// </remarks>
    class ExtractSWAT_Text_FileHelperEngine : ExtractSWAT_Text_Database
    {
        public ExtractSWAT_Text_FileHelperEngine(string txtInOutPath)
            : base(txtInOutPath)
        {
        }

        /// <summary>
        /// Get the record type based on given SAWT unit type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private Type getRecordType(UnitType type)
        {
            switch (type)
            {
                case UnitType.HRU: return typeof(SWATHRU);
                case UnitType.RCH: return typeof(SWATReach);
                case UnitType.RSV: return typeof(SWATReservoir);
                case UnitType.SUB: return typeof(SWATSub);
                default:
                    throw new Exception("Doesn't support " + type.ToString());
            }
        }

        protected override DataTable extractDailyHRU(string column)
        {
            //create the data table mannually just for the colume
            DataTable dt = new DataTable();
            dt.Columns.Add("MON", typeof(Int32));
            dt.Columns.Add("HRU", typeof(Int32));
            dt.Columns.Add(column, typeof(double));

            DataRow newRow = null;
            SWATHRU hru = null;
            bool reachLastYear = false;

            FileHelperAsyncEngine asyEngine = new FileHelperAsyncEngine(typeof(SWATHRU));
            asyEngine.BeginReadFile(getOutputFileFromType(UnitType.HRU));
            while (asyEngine.ReadNext() != null)
            {
                hru = (SWATHRU)asyEngine.LastRecord;

                if (_interval == OutputIntervalType.DAY || _interval == OutputIntervalType.MON)
                {
                    if (hru.MON == _endYear) reachLastYear = true;
                    if (reachLastYear && hru.MON < _endYear) break; //reached to the average annual part
                    if (hru.MON > 366) continue;                    //don't consider the annual results
                }

                newRow = dt.NewRow();
                newRow[0] = hru.MON;
                newRow[1] = hru.HRU;
                newRow[2] = typeof(SWATHRU).GetField(column).GetValue(hru);

                dt.Rows.Add(newRow);
            }
            return dt;
        }

        protected override DataTable getWholeTable(UnitType source)
        {
            if (!_wholeTables.ContainsKey(source)) //read the whole table first
            {
                //initialize the engine
                FileHelperEngine engine = new FileHelperEngine(getRecordType(source));

                //ignore the average annual outputs
                engine.Options.IgnoreLastLines = getNumberOfLinesForAverageAnnualOutput(source);

                Console.WriteLine("Reading whole table for " + source.ToString());
                DataTable dt = engine.ReadFileAsDT(getOutputFileFromType(source));

                dt.TableName = source.ToString();
                _wholeTables.Add(source, dt);
            }
            return _wholeTables[source];
        }

        ///// <summary>
        ///// Add time column to the datatable and calculate the time using start year and the Julian day. 
        ///// </summary>
        ///// <param name="dt"></param>
        ///// <remarks>This is useful when display in a table or chart</remarks>
        //private void calculateDate(DataTable dt)
        //{
        //    //add date column and caculate time from Julian day            
        //    dt.Columns.Add("TIME", typeof(DateTime));

        //    //for daily
        //    if (_interval == OutputIntervalType.DAILY)
        //    {
        //        int year = _startYear - 1;
        //        DateTime firstDayOfYear = DateTime.Now;
        //        double previousDay = -1;
        //        for (int i = 0; i < dt.Rows.Count; i++)
        //        {
        //            double JulianDay = double.Parse(dt.Rows[i]["MON"].ToString());
        //            if (JulianDay > 366) continue;
        //            if (i == 0 || (JulianDay == 1 && JulianDay < previousDay))
        //            {
        //                year += 1;
        //                firstDayOfYear = new DateTime(year, 1, 1);
        //            }
        //            previousDay = JulianDay;

        //            dt.Rows[i]["TIME"] = firstDayOfYear.AddDays(JulianDay - 1);
        //        }
        //    }
        //    else if (_interval == OutputIntervalType.MONTHLY)
        //    {
        //        int year = _startYear - 1;
        //        int previousMonth = -1;
        //        for (int i = 0; i < dt.Rows.Count; i++)
        //        {
        //            int month = int.Parse(dt.Rows[i]["MON"].ToString());
        //            if (month > 12) continue;
        //            if (i == 0 || (month == 1 && previousMonth > month)) year += 1;
        //            previousMonth = month;

        //            dt.Rows[i]["TIME"] = new DateTime(year, month, 1);
        //        }
        //    }
        //    else
        //    {
        //        for (int i = 0; i < dt.Rows.Count; i++)
        //        {
        //            int year = int.Parse(dt.Rows[i]["MON"].ToString());
        //            dt.Rows[i]["TIME"] = new DateTime(year, 1, 1);
        //        }
        //    } 
        //}
    }

    /// <summary>
    /// SWAT HRU Data Record
    /// </summary>
    /// <remarks>
    /// 1. Only for ICALEN = 0
    /// 2. The column name is same as the one used in SWAT-SQLie for easiy comparison
    /// </remarks>
    [IgnoreFirst(9)]
    [FixedLengthRecord(FixedMode.ExactLength)]
    public sealed class SWATHRU
    {
        [FieldFixedLength(4)]
        public String LULC;

        [FieldFixedLength(5)]
        public Int32 HRU;

        [FieldFixedLength(10)]
        public string HRUGIS;

        [FieldFixedLength(5)]
        public Int16 SUB;

        [FieldFixedLength(5)]
        public Int16 MGT;

        [FieldFixedLength(5)]
        public Int16 MON;

        [FieldFixedLength(10)]
        [FieldConverter(typeof(AreaConverter))]
        public Double AREAkm2;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double PRECIPmm;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double SNOFALLmm;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double SNOMELTmm;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double IRRmm;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double PETmm;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double ETmm;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double SW_INITmm;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double SW_ENDmm;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double PERCmm;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double GW_RCHGmm;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double DA_RCHGmm;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double REVAPmm;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double SA_IRRmm;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double DA_IRRmm;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double SA_STmm;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double DA_STmm;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double SURQ_GENmm;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double SURQ_CNTmm;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double TLOSSmm;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double LATQGENmm;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double GW_Qmm;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double WYLDmm;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double DAILYCN;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double TMP_AVdgC;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double TMP_MXdgC;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double TMP_MNdgC;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double SOL_TMPdgC;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double SOLARMJ_m2;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double SYLDt_ha;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double USLEt_ha;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double N_APPkg_ha;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double P_APPkg_ha;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double NAUTOkg_ha;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double PAUTOkg_ha;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double NGRZkg_ha;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double PGRZkg_ha;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double NCFRTkg_ha;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double PCFRTkg_ha;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double NRAINkg_ha;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double NFIXkg_ha;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double F_MNkg_ha;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double A_MNkg_ha;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double A_SNkg_ha;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double F_MPkg_ha;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double AO_LPkg_ha;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double L_APkg_ha;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double A_SPkg_ha;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double DNITkg_ha;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double NUPkg_ha;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double PUPkg_ha;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double ORGNkg_ha;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double ORGPkg_ha;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double SEDPkg_ha;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double NSURQkg_ha;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double NLATQkg_ha;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double NO3Lkg_ha;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double NO3GWkg_ha;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double SOLPkg_ha;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double P_GWkg_ha;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double W_STRS;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double TMP_STRS;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double N_STRS;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double P_STRS;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double BIOMt_ha;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double LAI;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double YLDt_ha;

        [FieldOptional]
        [FieldFixedLength(11)]
        [FieldConverter(typeof(AreaConverter))]
        public Double BACTPct;

        [FieldOptional]
        [FieldFixedLength(11)]
        [FieldConverter(typeof(AreaConverter))]
        public Double BACTLPct;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double WTAB_CLIm;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double WTAB_SOLm;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double SNOmm;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double CMUPkg_ha;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double CMTOTkg_ha;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double QTILEmm;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double TNO3kg_ha;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double LNO3kg_ha;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double GW_Q_Dmm;

        [FieldOptional]
        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double LATQCNTmm;

        internal class ValueConverter : ConverterBase
        {
            public override object StringToField(string from)
            {
                //System.Diagnostics.Debug.WriteLine(from);
                if (from.Trim().Equals("0.000E+00")) return 0.0;
                else return Convert.ToDouble(from.Trim());
            }

            public override string FieldToString(object from)
            {
                decimal d = (decimal)from;
                return Math.Round(d * 100).ToString();
            }

        }

        internal class AreaConverter : ConverterBase
        {
            public override object StringToField(string from)
            {
                //System.Diagnostics.Debug.WriteLine(from);
                return Convert.ToDouble("0" + from.Trim());
            }

            public override string FieldToString(object from)
            {
                decimal d = (decimal)from;
                return Math.Round(d * 100).ToString();
            }

        }
    }

    /// <summary>
    /// SWAT Subbasin Data Record
    /// </summary>
    /// <remarks>
    /// 1. Only for ICALEN = 0
    /// 2. The column name is same as the one used in SWAT-SQLie for easiy comparison
    /// </remarks>
    [IgnoreFirst(9)]
    [FixedLengthRecord(FixedMode.ExactLength)]
    public sealed class SWATSub
    {
        [FieldFixedLength(6)]
        public String NOUSE;

        [FieldFixedLength(4)]
        public Int32 SUB;

        [FieldFixedLength(10)]
        public Double GIS;

        [FieldFixedLength(4)]
        public Int16 MON;

        [FieldFixedLength(10)]
        [FieldConverter(typeof(AreaConverter))]
        public Double AREAkm2;

        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double PRECIPmm;

        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double SNOMELTmm;

        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double PETmm;

        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double ETmm;

        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double SWmm;

        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double PERCmm;

        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double SURQmm;

        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double GW_Qmm;

        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double WYLDmm;

        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double SYLDt_ha;

        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double ORGNkg_ha;

        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double ORGPkg_ha;

        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double NSURQkg_ha;

        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double SOLPkg_ha;

        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double SEDPkg_ha;

        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double LAT_Q_mm;

        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double LATNO3kg_h;

        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double GWNO3kg_ha;

        [FieldFixedLength(11)]
        [FieldConverter(typeof(ValueConverter))]
        public Double CHOLAmic_L;

        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double CBODU_mg_L;

        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double DOXQ_mg_L;

        [FieldFixedLength(10)]
        [FieldConverter(typeof(ValueConverter))]
        public Double TNO3kg_ha;

        [FieldFixedLength(6)]
        public Int32 SUB2;

        internal class ValueConverter : ConverterBase
        {
            public override object StringToField(string from)
            {
                if (from.Equals("0.000E+00")) return 0.0;
                else return Convert.ToDouble(from);
            }

            public override string FieldToString(object from)
            {
                decimal d = (decimal)from;
                return Math.Round(d * 100).ToString();
            }

        }

        internal class AreaConverter : ConverterBase
        {
            public override object StringToField(string from)
            {
                return Convert.ToDouble("0" + from);
            }

            public override string FieldToString(object from)
            {
                decimal d = (decimal)from;
                return Math.Round(d * 100).ToString();
            }

        }
    }

    /// <summary>
    /// SWAT Reach Data Record
    /// </summary>
    /// <remarks>
    /// 1. Only for ICALEN = 0
    /// 2. The column name is same as the one used in SWAT-SQLie for easiy comparison
    /// </remarks>
    [IgnoreFirst(9)]
    [FixedLengthRecord(FixedMode.ExactLength)]
    public sealed class SWATReach
    {
        [FieldFixedLength(5)]
        public String NOUSE;

        [FieldFixedLength(5)]
        public Int32 RCH;

        [FieldFixedLength(9)]
        public string GIS;

        [FieldFixedLength(6)]
        public Int16 MON;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double AREAkm2;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double FLOW_INcms;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double FLOW_OUTcms;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double EVAPcms;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double TLOSScms;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double SED_INtons;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double SED_OUTtons;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double SEDCONCmg_kg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double ORGN_INkg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double ORGN_OUTkg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double ORGP_INkg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double ORGP_OUTkg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double NO3_INkg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double NO3_OUTkg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double NH4_INkg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double NH4_OUTkg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double NO2_INkg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double NO2_OUTkg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double MINP_INkg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double MINP_OUTkg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double CHLA_INkg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double CHLA_OUTkg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double CBOD_INkg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double CBOD_OUTkg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double DISOX_INkg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double DISOX_OUTkg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double SOLPST_INmg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double SOLPST_OUTmg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double SORPST_INmg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double SORPST_OUTmg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double REACTPSTmg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double VOLPSTmg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double SETTLPSTmg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double RESUSP_PSTmg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double DIFFUSEPSTmg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double REACBEDPSTmg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double BURYPSTmg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double BED_PSTmg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double BACTP_OUTct;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double BACTLP_OUTct;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double CMETAL_1kg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double CMETAL_2kg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double CMETAL_3kg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double TOT_Nkg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double TOT_Pkg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double NO3ConcMg_l;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double WTMPdegc;

        [FieldFixedLength(6)]
        public Int32 RCH2;

        internal class ValueConverter : ConverterBase
        {
            public override object StringToField(string from)
            {
                //System.Diagnostics.Debug.WriteLine(from);
                if (from.Equals("0.000E+00")) return 0.0;
                else return Convert.ToDouble(from.Trim());
            }

            public override string FieldToString(object from)
            {
                decimal d = (decimal)from;
                return Math.Round(d * 100).ToString();
            }

        }

        internal class AreaConverter : ConverterBase
        {
            public override object StringToField(string from)
            {
                System.Diagnostics.Debug.WriteLine(from);
                return Convert.ToDouble("0" + from);
            }

            public override string FieldToString(object from)
            {
                decimal d = (decimal)from;
                return Math.Round(d * 100).ToString();
            }

        }
    }

    /// <summary>
    /// SWAT Reservoir Data Record
    /// </summary>
    /// <remarks>
    /// 1. Only for ICALEN = 0
    /// 2. The column name is same as the one used in SWAT-SQLie for easiy comparison
    /// </remarks>
    [IgnoreFirst(9)]
    [FixedLengthRecord(FixedMode.ExactLength)]
    public sealed class SWATReservoir
    {
        [FieldFixedLength(3)]
        public String NOUSE;

        [FieldFixedLength(11)]
        public Int32 RSV;

        [FieldFixedLength(5)]
        public Int16 MON;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double VOLUMEm3;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double FLOW_INcms;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double FLOW_OUTcms;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double PRECIPm3;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double EVAPm3;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double SEEPAGEm3;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double SED_INtons;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double SED_OUTtons;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double SED_CONCppm;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double ORGN_INkg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double ORGN_OUTkg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double RES_ORGNppm;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double ORGP_INkg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double ORGP_OUTkg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double RES_ORGPppm;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double NO3_INkg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double NO3_OUTkg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double RES_NO3ppm;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double NO2_INkg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double NO2_OUTkg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double RES_NO2ppm;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double NH3_INkg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double NH3_OUTkg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double RES_NH3ppm;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double MINP_INkg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double MINP_OUTkg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double RES_MINPppm;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double CHLA_INkg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double CHLA_OUTkg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double SECCHIDEPTHm;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double PEST_INmg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double REACTPSTmg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double VOLPSTmg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double SETTLPSTmg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double RESUSP_PSTmg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double DIFFUSEPSTmg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double REACBEDPSTmg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double BURYPSTmg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double PEST_OUTmg;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double PSTCNCWmg_m3;

        [FieldFixedLength(12)]
        [FieldConverter(typeof(ValueConverter))]
        public Double PSTCNCBmg_m3;

        [FieldOptional]
        [FieldFixedLength(5)]
        public Int32 YEAR;

        internal class ValueConverter : ConverterBase
        {
            public override object StringToField(string from)
            {
                //System.Diagnostics.Debug.WriteLine(from);
                if (from.Equals("0.000E+00")) return 0.0;
                else return Convert.ToDouble(from.Trim());
            }

            public override string FieldToString(object from)
            {
                decimal d = (decimal)from;
                return Math.Round(d * 100).ToString();
            }

        }

        internal class AreaConverter : ConverterBase
        {
            public override object StringToField(string from)
            {
                System.Diagnostics.Debug.WriteLine(from);
                return Convert.ToDouble("0" + from);
            }

            public override string FieldToString(object from)
            {
                decimal d = (decimal)from;
                return Math.Round(d * 100).ToString();
            }

        }
    }
}
