using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;
using System.Data;

namespace SWATPerformanceTest
{
    class ExtractSWAT_SQLite : ExtractSWAT, System.IDisposable
    {
        private string _db3Path = "";
        private SQLiteConnection _connection = null;
        private int _startYear = -1;
        private int _endYear = -1;
        private int _numofHRU = -1;
        private int _numofSub = -1;
        private int _numofRes = -1;
        private OutputIntervalType _interval = OutputIntervalType.UNKNOWN;

        public ExtractSWAT_SQLite(string db3Path)
        {
            _db3Path = db3Path;
            if (!System.IO.File.Exists(_db3Path)) _db3Path = "";
        }

        public ExtractSWAT_SQLite(string scenariosDir, string scenarioName, OutputIntervalType interval)
        {
            _db3Path = System.IO.Path.Combine( scenariosDir, scenarioName,"txtinout", getDatabaseName(interval));
            if (!System.IO.File.Exists(_db3Path)) _db3Path = "";
        }

        public ExtractSWAT_SQLite(string txtinoutPath, OutputIntervalType interval)
        {
            _db3Path = System.IO.Path.Combine(txtinoutPath, getDatabaseName(interval));
            if (!System.IO.File.Exists(_db3Path)) _db3Path = "";
        }

        public override void Dispose()
        {
            if (_connection != null && _connection.State != ConnectionState.Closed)
                _connection.Close();
        }

        private SQLiteConnection Connection
        {
            get
            {
                if (_connection == null)
                {
                    if (string.IsNullOrEmpty(_db3Path))
                        throw new System.Exception("The SQLite database doesn't exist!");

                    //Build the connection string;
                    SQLiteConnectionStringBuilder s = new SQLiteConnectionStringBuilder();
                    s.DataSource = _db3Path;
                    s.Version = 3;
                    s.FailIfMissing = false;

                    //Open the connection;
                    _connection = new SQLiteConnection(s.ConnectionString);
                    _connection.Open();
                 }
                return _connection;
            }
        }

        /// <summary>
        /// Retrieve data columns for given result interval
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string getDateColumns(OutputIntervalType type)
        {
            switch (type)
            {
                case OutputIntervalType.DAY: return DATE_COLUMNS_DAILY;
                case OutputIntervalType.MON: return DATE_COLUMNS_MONTHLY;
                case OutputIntervalType.YEAR: return DATE_COLUMNS_YEARLY;
                default: return "";
            }
        }

        /// <summary>
        /// Retrieve data table for given unit type
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string getTableName(UnitType source)
        {
            switch (source)
            {
                case UnitType.HRU: return "hru";
                case UnitType.RCH: return "rch";
                case UnitType.RSV: return "rsv";
                case UnitType.SUB: return "sub";
                case UnitType.WATER: return "wtr";
                default: return "";
            }
        }

        public static string getDatabaseName(OutputIntervalType interval)
        {
            switch (interval)
            {
                case OutputIntervalType.DAY: return "result_627_daily.db3";
                case OutputIntervalType.MON: return "result_627_monthly.db3";
                case OutputIntervalType.YEAR: return "result_627_yearly.db3";
                default: return "";
            }
        }

#region SQLite Struction
        public static string COLUMN_NAME_YEAR = "YR";
        public static string COLUMN_NAME_MONTH = "MO";
        public static string COLUMN_NAME_DAY = "DA";

        public static string DATE_COLUMNS_YEARLY = COLUMN_NAME_YEAR;
        public static string DATE_COLUMNS_MONTHLY = DATE_COLUMNS_YEARLY + "," + COLUMN_NAME_MONTH;
        public static string DATE_COLUMNS_DAILY = DATE_COLUMNS_MONTHLY + "," + COLUMN_NAME_DAY;

        public static string[] TEXT_COLUMNS_HRU = new string[] {
                             "  PRECIPmm"," SNOFALLmm"," SNOMELTmm","     IRRmm",     
                             "     PETmm","      ETmm"," SW_INITmm","  SW_ENDmm",     
                             "    PERCmm"," GW_RCHGmm"," DA_RCHGmm","   REVAPmm",     
                             "  SA_IRRmm","  DA_IRRmm","   SA_STmm","   DA_STmm",     
                             "SURQ_GENmm","SURQ_CNTmm","   TLOSSmm"," LATQGENmm",     
                             "    GW_Qmm","    WYLDmm","   DAILYCN"," TMP_AVdgC",     
                             " TMP_MXdgC"," TMP_MNdgC","SOL_TMPdgC","SOLARMJ/m2",     
                             "  SYLDt/ha","  USLEt/ha","N_APPkg/ha","P_APPkg/ha",     
                             "NAUTOkg/ha","PAUTOkg/ha"," NGRZkg/ha"," PGRZkg/ha",     
                             "NCFRTkg/ha","PCFRTkg/ha","NRAINkg/ha"," NFIXkg/ha",     
                             " F-MNkg/ha"," A-MNkg/ha"," A-SNkg/ha"," F-MPkg/ha",     
                             "AO-LPkg/ha"," L-APkg/ha"," A-SPkg/ha"," DNITkg/ha",     
                             "  NUPkg/ha","  PUPkg/ha"," ORGNkg/ha"," ORGPkg/ha",     
                             " SEDPkg/ha","NSURQkg/ha","NLATQkg/ha"," NO3Lkg/ha",     
                             "NO3GWkg/ha"," SOLPkg/ha"," P_GWkg/ha","    W_STRS",     
                             "  TMP_STRS","    N_STRS","    P_STRS","  BIOMt/ha",     
                             "       LAI","   YLDt/ha","   BACTPct","  BACTLPct",     
                             " WTAB CLIm"," WTAB SOLm","     SNOmm"," CMUPkg/ha",     
                             "CMTOTkg/ha","   QTILEmm"," TNO3kg/ha"," LNO3kg/ha",     
                             "  GW_Q_Dmm"," LATQCNTmm"};

        public static string[] TEXT_COLUMNS_SUB = new string[] {
                             "  PRECIPmm"," SNOMELTmm","     PETmm","      ETmm",     
                             "      SWmm","    PERCmm","    SURQmm","    GW_Qmm",     
                             "    WYLDmm","  SYLDt/ha"," ORGNkg/ha"," ORGPkg/ha",     
                             "NSURQkg/ha"," SOLPkg/ha"," SEDPkg/ha"," LAT Q(mm)",     
                             "LATNO3kg/h","GWNO3kg/ha","CHOLAmic/L","CBODU mg/L",     
                             " DOXQ mg/L"," TNO3kg/ha"};

        public static string[] TEXT_COLUMNS_RCH = new string[] {
                             "  FLOW_INcms"," FLOW_OUTcms","     EVAPcms",            
                             "    TLOSScms","  SED_INtons"," SED_OUTtons",            
                             "SEDCONCmg/kg","   ORGN_INkg","  ORGN_OUTkg",            
                             "   ORGP_INkg","  ORGP_OUTkg","    NO3_INkg",            
                             "   NO3_OUTkg","    NH4_INkg","   NH4_OUTkg",            
                             "    NO2_INkg","   NO2_OUTkg","   MINP_INkg",            
                             "  MINP_OUTkg","   CHLA_INkg","  CHLA_OUTkg",            
                             "   CBOD_INkg","  CBOD_OUTkg","  DISOX_INkg",            
                             " DISOX_OUTkg"," SOLPST_INmg","SOLPST_OUTmg",            
                             " SORPST_INmg","SORPST_OUTmg","  REACTPSTmg",          
                             "    VOLPSTmg","  SETTLPSTmg","RESUSP_PSTmg",            
                             "DIFFUSEPSTmg","REACBEDPSTmg","   BURYPSTmg",            
                             "   BED_PSTmg"," BACTP_OUTct","BACTLP_OUTct",            
                             "  CMETAL#1kg","  CMETAL#2kg","  CMETAL#3kg",            
                             "     TOT Nkg","     TOT Pkg"," NO3ConcMg/l",            
                             "    WTMPdegc"};

        public static string[] TEXT_COLUMNS_RES = new string[] {
                             "    VOLUMEm3","  FLOW_INcms"," FLOW_OUTcms",          
                             "    PRECIPm3","      EVAPm3","   SEEPAGEm3",          
                             "  SED_INtons"," SED_OUTtons"," SED_CONCppm",          
                             "   ORGN_INkg","  ORGN_OUTkg"," RES_ORGNppm",          
                             "   ORGP_INkg","  ORGP_OUTkg"," RES_ORGPppm",          
                             "    NO3_INkg","   NO3_OUTkg","  RES_NO3ppm",          
                             "    NO2_INkg","   NO2_OUTkg","  RES_NO2ppm",          
                             "    NH3_INkg","   NH3_OUTkg","  RES_NH3ppm",          
                             "   MINP_INkg","  MINP_OUTkg"," RES_MINPppm",          
                             "   CHLA_INkg","  CHLA_OUTkg","SECCHIDEPTHm",          
                             "   PEST_INmg","  REACTPSTmg","    VOLPSTmg",          
                             "  SETTLPSTmg","RESUSP_PSTmg","DIFFUSEPSTmg",          
                             "REACBEDPSTmg","   BURYPSTmg","  PEST_OUTmg",          
                             "PSTCNCWmg/m3","PSTCNCBmg/m3"};

        public static string[] SQLITE_COLUMNS_HRU = new string[] {
                             "PRECIPmm","SNOFALLmm","SNOMELTmm","IRRmm",     
                             "PETmm","ETmm","SW_INITmm","SW_ENDmm",     
                             "PERCmm","GW_RCHGmm","DA_RCHGmm","REVAPmm",     
                             "SA_IRRmm","DA_IRRmm","SA_STmm","DA_STmm",     
                             "SURQ_GENmm","SURQ_CNTmm","TLOSSmm","LATQGENmm",     
                             "GW_Qmm","WYLDmm","DAILYCN","TMP_AVdgC",     
                             "TMP_MXdgC","TMP_MNdgC","SOL_TMPdgC","SOLARMJ_m2",
                             "SYLDt_ha","USLEt_ha","N_APPkg_ha","P_APPkg_ha",
                             "NAUTOkg_ha","PAUTOkg_ha","NGRZkg_ha","PGRZkg_ha",
                             "NCFRTkg_ha","PCFRTkg_ha","NRAINkg_ha","NFIXkg_ha",
                             "F_MNkg_ha","A_MNkg_ha","A_SNkg_ha","F_MPkg_ha",
                             "AO_LPkg_ha","L_APkg_ha","A_SPkg_ha","DNITkg_ha",
                             "NUPkg_ha","PUPkg_ha","ORGNkg_ha","ORGPkg_ha",
                             "SEDPkg_ha","NSURQkg_ha","NLATQkg_ha","NO3Lkg_ha",
                             "NO3GWkg_ha","SOLPkg_ha","P_GWkg_ha","W_STRS",
                             "TMP_STRS","N_STRS","P_STRS","BIOMt_ha",
                             "LAI","YLDt_ha","BACTPct","BACTLPct",
                             "WTAB_CLIm","WTAB_SOLm","SNOmm","CMUPkg_ha",
                             "CMTOTkg_ha","QTILEmm","TNO3kg_ha","LNO3kg_ha",
                             "GW_Q_Dmm","LATQCNTmm"};

        public static string[] SQLITE_COLUMNS_SUB = new string[] {
                             "PRECIPmm","SNOMELTmm","PETmm","ETmm",     
                             "SWmm","PERCmm","SURQmm","GW_Qmm",     
                             "WYLDmm","SYLDt_ha","ORGNkg_ha","ORGPkg_ha",
                             "NSURQkg_ha","SOLPkg_ha","SEDPkg_ha","LAT_Q_mm",
                             "LATNO3kg_h","GWNO3kg_ha","CHOLAmic_L","CBODU_mg_L",
                             "DOXQ_mg_L","TNO3kg_ha"};

        public static string[] SQLITE_COLUMNS_RCH = new string[] {
                             "FLOW_INcms","FLOW_OUTcms","EVAPcms",            
                             "TLOSScms","SED_INtons","SED_OUTtons",            
                             "SEDCONCmg_kg","ORGN_INkg","ORGN_OUTkg",
                             "ORGP_INkg","ORGP_OUTkg","NO3_INkg",            
                             "NO3_OUTkg","NH4_INkg","NH4_OUTkg",            
                             "NO2_INkg","NO2_OUTkg","MINP_INkg",            
                             "MINP_OUTkg","CHLA_INkg","CHLA_OUTkg",            
                             "CBOD_INkg","CBOD_OUTkg","DISOX_INkg",            
                             "DISOX_OUTkg","SOLPST_INmg","SOLPST_OUTmg",            
                             "SORPST_INmg","SORPST_OUTmg","REACTPSTmg",          
                             "VOLPSTmg","SETTLPSTmg","RESUSP_PSTmg",            
                             "DIFFUSEPSTmg","REACBEDPSTmg","BURYPSTmg",            
                             "BED_PSTmg","BACTP_OUTct","BACTLP_OUTct",            
                             "CMETAL_1kg","CMETAL_2kg","CMETAL_3kg",
                             "TOT_Nkg","TOT_Pkg","NO3ConcMg_l",
                             "WTMPdegc"};

        public static string[] SQLITE_COLUMNS_RES = new string[] {
                               "VOLUMEm3","FLOW_INcms","FLOW_OUTcms",          
                               "PRECIPm3","EVAPm3","SEEPAGEm3",          
                               "SED_INtons"," SED_OUTtons","SED_CONCppm",          
                               "ORGN_INkg","ORGN_OUTkg","RES_ORGNppm",          
                               "ORGP_INkg","ORGP_OUTkg","RES_ORGPppm",          
                               "NO3_INkg","NO3_OUTkg","RES_NO3ppm",          
                               "NO2_INkg","NO2_OUTkg","RES_NO2ppm",          
                               "NH3_INkg","NH3_OUTkg","RES_NH3ppm",          
                               "MINP_INkg","MINP_OUTkg","RES_MINPppm",          
                               "CHLA_INkg","CHLA_OUTkg","SECCHIDEPTHm",          
                               "PEST_INmg","REACTPSTmg","VOLPSTmg",          
                               "SETTLPSTmg","RESUSP_PSTmg","DIFFUSEPSTmg",          
                               "REACBEDPSTmg","BURYPSTmg","PEST_OUTmg",          
                               "PSTCNCWmg_m3","PSTCNCBmg_m3"};

        public static string[] GetSQLiteColumns(UnitType unit)
        {
            switch (unit)
            {
                case UnitType.HRU: return SQLITE_COLUMNS_HRU;
                case UnitType.RCH: return SQLITE_COLUMNS_RCH;
                case UnitType.RSV: return SQLITE_COLUMNS_RES;
                case UnitType.SUB: return SQLITE_COLUMNS_SUB;
                default: return null;
            }
        }

        public static string ColumnSQLite2Text(UnitType unit, string colSQLite)
        {
            int index = -1;
            switch (unit)
            {
                case UnitType.HRU: index = Array.IndexOf(SQLITE_COLUMNS_HRU, colSQLite); break;
                case UnitType.RCH: index = Array.IndexOf(SQLITE_COLUMNS_RCH, colSQLite); break;
                case UnitType.RSV: index = Array.IndexOf(SQLITE_COLUMNS_RES, colSQLite); break;
                case UnitType.SUB: index = Array.IndexOf(SQLITE_COLUMNS_SUB, colSQLite); break;
            }

            if (index == -1)
                throw new System.Exception("Couldn't find column " + colSQLite + " in sqlite tables.");

            switch (unit)
            {
                case UnitType.HRU: return TEXT_COLUMNS_HRU[index];
                case UnitType.RCH: return TEXT_COLUMNS_RCH[index];
                case UnitType.RSV: return TEXT_COLUMNS_RES[index];
                case UnitType.SUB: return TEXT_COLUMNS_SUB[index];
                default: 
                    throw new System.Exception("Couldn't find column " + colSQLite + " in text outputs.");
            }
        }
#endregion

        #region Basic Information

        private void getBasicInformation()
        {            
            DataTable dt = Extract("select * from ave_annual_basin");
            if (dt.Rows.Count == 0) return;

            foreach (DataRow r in dt.Rows)
            {
                RowItem item = new RowItem(r);
                string name = item.getColumnValue_String("NAME");
                if (name.Equals("START_YEAR_OUTPUT"))
                    _startYear = item.getColumnValue_Int("VALUE");
                else if (name.Equals("END_YEAR"))
                    _endYear = item.getColumnValue_Int("VALUE");
                else if (name.Equals("OUTPUT_INTERVAL"))
                    _interval = (OutputIntervalType)(item.getColumnValue_Int("VALUE"));
            }

            dt = Extract("select * from hru_info");
            _numofHRU = dt.Rows.Count;

            dt = Extract("select * from sub_info");
            _numofSub = dt.Rows.Count;

            dt = Extract("select * from sqlite_master where name = 'rsv_info'");
            if (dt.Rows.Count > 0)
            {
                dt = Extract("select * from rsv_info");
                _numofSub = dt.Rows.Count;
            }            
        }

        public int NumberofHRU
        {
            get
            {
                if (_numofHRU == -1) getBasicInformation();
                return _numofHRU;
            }
        }

        public int NumberofSubbasin
        {
            get
            {
                if (_numofSub == -1) getBasicInformation();
                return _numofSub;
            }
        }

        public int NumberofReservor
        {
            get
            {
                if (_numofRes == -1) getBasicInformation();
                return _numofRes;
            }
        }

        public int StartYear
        {
            get
            {
                if (_startYear == -1) getBasicInformation();
                return _startYear;
            }
        }
        
        public int EndYear
        {
            get
            {
                if (_endYear == -1) getBasicInformation();
                return _endYear;
            }
        }

        private OutputIntervalType Interval
        {
            get
            {
                if (_interval == OutputIntervalType.UNKNOWN) getBasicInformation();
                return _interval;
            }
        }                       

        #endregion

        #region Data Extraction

        /// <summary>
        /// Extract data for given unit and column. The time range is not specified.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="id"></param>
        /// <param name="var"></param>
        /// <returns></returns>
        public override DataTable Extract(UnitType source, int id, string var, bool addTimeColumn = false,
            bool adjustAccuracy = false)
        {
            return Extract(StartYear, EndYear, source, id, var, addTimeColumn, adjustAccuracy);
        }

        /// <summary>
        /// Extract data for given unit and column in given year
        /// </summary>
        /// <param name="year"></param>
        /// <param name="source"></param>
        /// <param name="id"></param>
        /// <param name="var"></param>
        /// <returns></returns>
        public DataTable Extract(int year, UnitType source, int id, string var, bool addTimeColumn = false,
            bool adjustAccuracy = false)
        {
            return Extract(year, year, source, id, var, addTimeColumn, adjustAccuracy);
        }

        private string getSQL(int requestStartYear, int requestFinishYear,
            UnitType source, int id, string var)
        {
            //columns
            string cols = getDateColumns(Interval);
            cols += "," + var;

            //get table
            string table = getTableName(source);
            string col_id = table;
            if (source == UnitType.RSV) col_id = "res";

            //id, for wtr is not correct
            string idCondition = "";
            if (id > 0) idCondition = string.Format("{0}={1}", col_id, id);

            //year condition
            if (requestStartYear < StartYear) requestStartYear = StartYear;
            if (requestFinishYear > EndYear) requestFinishYear = EndYear;
            string yearCondition = "";
            if (requestStartYear != StartYear || requestFinishYear != EndYear)
            {
                if (requestStartYear == requestFinishYear)
                    yearCondition = string.Format("YR = {0}", requestStartYear);
                else
                    yearCondition = string.Format("YR >= {0} AND YR <= {1}", requestStartYear, requestFinishYear);
            }

            string query = string.Format("select {0} from {1}",
                cols, table);
            if (!string.IsNullOrWhiteSpace(idCondition))
                query += " where " + idCondition;
            if (!string.IsNullOrWhiteSpace(yearCondition))
            {
                if (!string.IsNullOrWhiteSpace(idCondition))
                    query += " and ";
                query += yearCondition;
            }
            return query;
        }

        /// <summary>
        /// Extract data for given unit and column in given time range
        /// </summary>
        /// <param name="requestStartYear">The starting year for the request</param>
        /// <param name="requestFinishYear">The ending year for the request</param>
        /// <param name="source">The type of the SWAT unit</param>
        /// <param name="id">The id of the SWAT unit</param>
        /// <param name="var">The name of the column</param>
        /// <param name="addTimeColumn">If add time column</param>
        /// <param name="adjustAccuracy">If adjust the accuracy of the data value. As data value is saved as a string in SWAT output files, 
        /// the accuracy is depending on the notation being used. The scientifc notation is accurate enough but the decimal notation is not. 
        /// To be comarable with SWAT, the data value from SQLite should be trimmed using the same notation or else there will be enough difference. 
        /// The most common used decimal notation is f10.3, which is used in HRU and Subbasin output file. Only scientific notiation is used in 
        /// reach and reservoir files.</param>
        /// <returns>Data table including request data</returns>
        public DataTable Extract(
            int requestStartYear, int requestFinishYear,
            UnitType source, int id, string var,
            bool addTimeColumn = false,
            bool adjustAccuracy = false)
        {
            //start time
            //the time to read data from SQLite include
            //1. SQL construction
            //2. Database connection
            //3. Execute SQL
            //4. Add time column and calcualte date to be comparable with SWAT plot
            //System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString("hh:mm:ss.fff"));
            DateTime startTime = DateTime.Now;
            _extractTime = -99.0;

            //SQL construction
            string sql = getSQL(requestStartYear, requestFinishYear, source, id, var);

            //execute SQL
            DataTable dt = Extract(sql);          

            //add datetime column and calculate the date
            if (addTimeColumn) calculateDate(dt);

            //get the data reading time
            _extractTime = DateTime.Now.Subtract(startTime).TotalMilliseconds;
            //System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString("hh:mm:ss.fff"));

            if (adjustAccuracy) adjustDataAccuracy(dt,source,var);
            
            return dt;
        }

        /// <summary>
        /// Get the format used in SWAT
        /// </summary>
        /// <param name="type">The type of SWAT unit type</param>
        /// <returns>format string for double value</returns>
        /// <remarks>
        /// convert to decimal/scientific notation used in SWAT and convert back to number to be comparable with SWAT
        /// Subbasin
        ///      Monthly and yearly use f10.3 for the first 18 variables, and then use one e10.5 and three e10.3
        ///      Daily use e10.3 for the first 18 variables, and then use one e10.5 and three e10.3
        /// HRU
        ///      Use f10.3 for first 66 variables, and then use two e10.5, eight e10.3 and two f10.3
        /// Reach and Reservoir
        ///      All use e12.4
        /// </remarks>
        private string getSWATFormat(UnitType type, string col)
        {
            if (type == UnitType.WATER)
                throw new Exception("Doesn't support " + type.ToString());
            
            string format = "";
            int colindex = -1;
            if (type == UnitType.SUB)
            {
                colindex = Array.IndexOf(SQLITE_COLUMNS_SUB, col);
                if (colindex == -1)
                    throw new Exception(col + " doesn't exist. Forget the space?");
                if (colindex < 18)
                {
                    if (_interval == OutputIntervalType.MON || _interval == OutputIntervalType.YEAR)
                        format = "F3";          //f10.3
                    else
                        format = "0.00e+00";   //e10.3
                }
                else if (colindex == 18)
                    format = "0.0000e+00";     //e10.5
                else
                    format = "0.00e+00";       //e10.3
            }
            else if (type == UnitType.HRU)
            {
                colindex = Array.IndexOf(SQLITE_COLUMNS_HRU, col);
                if (colindex == -1)
                    throw new Exception(col + " doesn't exist. Forget the space?");
                if (colindex < 66)
                    format = "F3";              //f10.3
                else if (colindex < 68)
                    format = "0.0000e+00";     //e10.5
                else if (colindex < 76)
                    format = "0.00e+00";       //e10.3
                else
                    format = format = "F3";     //f10.3
            }
            else
                format = "0.000e+00";          //e12.4

            return format;
        }

        private void adjustDataAccuracy(DataTable dt, UnitType source, string var)
        {
            if (dt != null)
            {
                string format = getSWATFormat(source, var);
                string col = var.Trim();
                foreach (DataRow r in dt.Rows)
                {
                    double v = double.Parse(r[col].ToString()); //get the value
                    r[col] = double.Parse(v.ToString(format));  //trim the value using SWAT format
                }
            }
        }

        /// <summary>
        /// Get time from year, month and day
        /// </summary>
        /// <param name="r"></param>
        private void calculateDate(DataTable dt)
        {
            dt.Columns.Add("TIME", typeof(DateTime));
            foreach (DataRow r in dt.Rows)
            {
                DateTime d = DateTime.Now;
                RowItem item = new RowItem(r);
                int year = item.getColumnValue_Int(COLUMN_NAME_YEAR);
                int month = 1;
                int day = 1;
                if (Interval == OutputIntervalType.MON || Interval == OutputIntervalType.DAY)
                    month = item.getColumnValue_Int(COLUMN_NAME_MONTH);
                if (Interval == OutputIntervalType.DAY)
                    day = item.getColumnValue_Int(COLUMN_NAME_DAY);

                r["TIME"] = new DateTime(year, month, day);
            }
        }

        /// <summary>
        /// Excecute query in database
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        private DataTable Extract(string query)
        {
            DataTable dt = new DataTable();
            using (SQLiteDataAdapter a = new SQLiteDataAdapter(query, Connection))
            {
                try
                {
                    a.Fill(dt);
                }
                catch (System.Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("Query: " + query);
                    System.Diagnostics.Debug.WriteLine(e.ToString());
                    throw new Exception("Wrong SQLite query: " + query);
                }
            }
            return dt;         
        }

        #endregion

    }
}
