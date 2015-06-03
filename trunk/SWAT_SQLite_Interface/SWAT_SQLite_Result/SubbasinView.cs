using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SWAT_SQLite_Result
{
    public delegate void SwitchFromSubbasin2HRUEventHandler(ArcSWAT.HRU hru);

    public partial class SubbasinView : UserControl
    {
        public SubbasinView()
        {
            InitializeComponent();
        }

        private string _resultType = null;
        private string _col = null;
        private ArcSWAT.SWATUnit _unit = null;
        private DateTime _date = DateTime.Now;
        private string _statistics = "No Statistics Data Available";

        private ArcSWAT.Project _project = null;
        private ArcSWAT.ScenarioResult _scenario = null;
        private ArcSWAT.SWATUnitType _type = ArcSWAT.SWATUnitType.UNKNOWN;
        private ArcSWAT.ResultSummaryType _summaryType = ArcSWAT.ResultSummaryType.ANNUAL;
        private Dictionary<int, ArcSWAT.SWATUnit> _unitList = null;

        /// <summary>
        /// Happens when go button is clicked
        /// </summary>
        public event SwitchFromSubbasin2HRUEventHandler onSwitch2HRU = null;

        /// <summary>
        /// Happens when new feature is selected
        /// </summary>
        public event EventHandler onMapSelectionChanged = null;

        /// <summary>
        /// Happens when time is changed
        /// </summary>
        public event EventHandler onMapTimeChanged = null;

        /// <summary>
        /// Happens when statistic information is changed
        /// </summary>
        public event EventHandler onDataStatisticsChanged = null;

        public DateTime MapTime { get { return _date; } }
        public ArcSWAT.ResultSummaryType SummaryType { get { return _summaryType; } }
        public ArcSWAT.SWATUnit MapSelection { get { return _unit; } }
        public string Statistics { get { return _statistics; } }
        public ArcSWAT.ScenarioResult ScenarioResult { get { return _scenario; } }

        public void setProjectScenario(ArcSWAT.Project project, ArcSWAT.ScenarioResult scenario,ArcSWAT.SWATUnitType type)
        {
            _project = project;
            _scenario = scenario;            
            _type = type;
            _date = new DateTime(scenario.StartYear, 1, 1);
            if (onMapTimeChanged != null)
                onMapTimeChanged(this, new EventArgs());
            
            if (type == ArcSWAT.SWATUnitType.SUB)
                _unitList = _scenario.Subbasins;
            else if (type == ArcSWAT.SWATUnitType.RCH)
                _unitList = _scenario.Reaches;
            else if (type == ArcSWAT.SWATUnitType.HRU)
                _unitList = _scenario.HRUs;
            else if (type == ArcSWAT.SWATUnitType.RES)
                _unitList = _scenario.Reservoirs;

            this.Resize += (ss, ee) => { splitContainer3.SplitterDistance = 72; };

            //swat input files extension list
            swatFileList1.SWATUnitType = _type;
            swatFileList1.onSWATInputFileExtensionChanged += (s, e) =>
                {
                    if (_unit == null) return;
                    string fileName = _unit.getInputFileName(swatFileList1.Extension);
                    if (!System.IO.File.Exists(fileName))
                    {
                        SWAT_SQLite.showInformationWindow(fileName + " doesn't exist!");
                        return;
                    }

                    string notePad = System.Environment.SystemDirectory + @"\notepad.exe";
                    if (System.IO.File.Exists(notePad))
                        System.Diagnostics.Process.Start(notePad, fileName);
                };

            //id list
            if(type == ArcSWAT.SWATUnitType.HRU)
                idList1.IDs = scenario.getSWATUnitIDs(ArcSWAT.SWATUnitType.SUB);
            else
                idList1.IDs = scenario.getSWATUnitIDs(type);

            idList1.onIDChanged += (s, e) => { onIDChanged(idList1.ID); subbasinMap1.ID = idList1.ID; setMapTalbeIDSelection(idList1.ID); };
            
            //season control
            seasonCtrl1.onSeasonTypeChanged += (s, e) => { tableView1.Season = seasonCtrl1.Season; outputDisplayChart1.Season = seasonCtrl1.Season; updateTableAndChart(); };

            //year control
            yearCtrl1.Scenario = scenario;
            yearCtrl1.onYearChanged += (s, e) => 
            { 
                //update the summary type control
                summaryTypeCtrl1.CurrentYear = yearCtrl1.Year;

                //update the time step map view and summary control
                if (yearCtrl1.Year != -1)
                {
                    _date = new DateTime(yearCtrl1.Year, 1, 1);
                    summaryTypeCtrl1.TimeForTimeStep = _date;

                    //update the status bar
                    if (onMapTimeChanged != null)
                        onMapTimeChanged(this, new EventArgs());
                }

                //update map               
                if(_summaryType != ArcSWAT.ResultSummaryType.AVERAGE_ANNUAL) //only update map when it's not average annual
                    this.updateMap(); 

                updateTableAndChart(); 
            };

            //summary type control for map
            summaryTypeCtrl1.ScenarioResult = scenario;
            summaryTypeCtrl1.onSummaryTypeChanged += (s, e) =>
            {
                _summaryType = summaryTypeCtrl1.SummaryType;
                this.updateMap();                     //update the status bar
                if (onMapTimeChanged != null)
                    onMapTimeChanged(this, new EventArgs());
            };

            //only for subbasin to show hru list
            hruList1.Visible = (type == ArcSWAT.SWATUnitType.SUB || type == ArcSWAT.SWATUnitType.HRU);
            hruList1.IsChangeWhenSelect = (type == ArcSWAT.SWATUnitType.HRU);
            hruList1.onSwitch2HRU += (hru) =>
                {
                    if (_type == ArcSWAT.SWATUnitType.HRU) 
                    {
                        if (_unit != null && _unit.ID == hruList1.HRU.ID) return;

                        _unit = hruList1.HRU;

                        //show basic information
                        if (onMapSelectionChanged != null)
                            onMapSelectionChanged(this, new EventArgs());

                        //update table and chart
                        updateTableAndChart(); 
                    }
                    if (_type == ArcSWAT.SWATUnitType.SUB)
                    {
                        if (onSwitch2HRU != null) onSwitch2HRU(hru);
                    }
                };

            //columns
            resultColumnTree1.onResultTypeAndColumnChanged += (resultType, col) =>
            {
                _resultType = resultType;
                _col = col;

                //only for daily and monthly
                this.yearCtrl1.Visible = _scenario.Structure.getInterval(_resultType) == ArcSWAT.SWATResultIntervalType.DAILY ||
                    _scenario.Structure.getInterval(_resultType) == ArcSWAT.SWATResultIntervalType.MONTHLY;

                updateMap();
                updateTableAndChart();
            };
            resultColumnTree1.setScenarioAndUnit(scenario, type);

            //the id selection changed
            tblMapData.RowHeadersVisible = false;
            tblMapData.ReadOnly = true;
            tblMapData.RowEnter += (s, e) =>
                {
                    if (e.RowIndex < 0 || tblMapData.Rows[e.RowIndex].Cells[0].Value == null) return;
                    int id = int.Parse(tblMapData.Rows[e.RowIndex].Cells[0].Value.ToString());
                    
                    onIDChanged(id); 
                    idList1.ID = id;
                    subbasinMap1.ID = id;
                };

            //map            
            subbasinMap1.onLayerSelectionChanged += (unitType, id) => { onIDChanged(id); idList1.ID = id; setMapTalbeIDSelection(id); };
            subbasinMap1.setProjectScenario(project, scenario, type);
            subbasinMap1.onMapUpdated += (s, e) => 
            {
                this.tblMapData.DataSource = subbasinMap1.DataTable;
                foreach (DataGridViewColumn col in tblMapData.Columns)
                {
                     col.Visible = col.Name.Equals(SubbasinMap.ID_COLUMN_NAME) ||
                        col.Name.Equals(SubbasinMap.RESULT_COLUMN);
                     if (col.Name.Equals(SubbasinMap.RESULT_COLUMN))
                     {
                         col.DefaultCellStyle.Format = "F4";
                         col.HeaderText = _col;
                     }
                     else if (col.Name.Equals(SubbasinMap.ID_COLUMN_NAME))
                     {
                         col.HeaderText = _resultType.ToString().ToLower();
                     }
                }
            };

            //chart export
            outputDisplayChart1.onExport += (s, e) =>
                {

                };

            //table view
            tableView1.onDateChanged += (d) => 
            { 
                if (_type == ArcSWAT.SWATUnitType.HRU) return; 
                _date = d;
                summaryTypeCtrl1.TimeForTimeStep = d;

                if (onMapTimeChanged != null)
                    onMapTimeChanged(this, new EventArgs());
                if(_summaryType == ArcSWAT.ResultSummaryType.TIMESTEP)
                    updateMap(); 
            };

            //compare control
            compareCtrl1.ScenarioResult = scenario;
            compareCtrl1.onCompareResultChanged += (ss, ee) => 
            {
                updateTableAndChart(); 
            };
           

            //update
            updateMap();
            updateTableAndChart();

            //update the status bar
            if (onMapTimeChanged != null)
                onMapTimeChanged(this, new EventArgs());
        }

        /// <summary>
        /// Select the row correponding to given id
        /// </summary>
        /// <param name="id"></param>
        /// <remarks>The ID selection in IDList, Map and TableMap is now connected.</remarks>
        private void setMapTalbeIDSelection(int id)
        {
            foreach (DataGridViewRow r in tblMapData.Rows)
            {
                if (r.Cells[0].Value == null) continue;
                int currentId = int.Parse(r.Cells[0].Value.ToString());
                r.Selected = currentId == id;
            }          
        }

        public void onIDChanged(int id)
        {
            if (_type != ArcSWAT.SWATUnitType.SUB &&
                _type != ArcSWAT.SWATUnitType.RCH &&
                _type != ArcSWAT.SWATUnitType.HRU && 
                _type != ArcSWAT.SWATUnitType.RES && 
                _unitList != null) return;

            if (id <= 0)
                _unit = null;
            else
            {
                if (_type == ArcSWAT.SWATUnitType.HRU)
                    _unit = (_scenario.Subbasins[id] as ArcSWAT.Subbasin).HRUs.First().Value;
                else
                    _unit = _unitList[id];
            }

            //show basic information
            if (onMapSelectionChanged != null)
                onMapSelectionChanged(this, new EventArgs());

            if (_unit != null)
            {
                //get hrus
                if (_type == ArcSWAT.SWATUnitType.SUB)
                    hruList1.Subbasin = _unit as ArcSWAT.Subbasin;
                if (_type == ArcSWAT.SWATUnitType.HRU)
                    hruList1.Subbasin = (_unit as ArcSWAT.HRU).Subbasin;
            }

            updateTableAndChart();
        }

        public ArcSWAT.HRU HRU
        {
            set
            {
                if (_type == ArcSWAT.SWATUnitType.HRU)
                    subbasinMap1.HRU = value;
            }
        }

        private void updateMap()
        {
            if (_type == ArcSWAT.SWATUnitType.HRU) return;
            if (_resultType == null || _col == null) return;

            //consider the result summary type in map
            int year = yearCtrl1.Year;
            _summaryType = summaryTypeCtrl1.SummaryType;
            if (year == -1 && _summaryType == ArcSWAT.ResultSummaryType.ANNUAL)
                _summaryType = ArcSWAT.ResultSummaryType.AVERAGE_ANNUAL;

            if (_summaryType == ArcSWAT.ResultSummaryType.ANNUAL)
                subbasinMap1.drawLayer(_resultType, _col, new DateTime(year, 1, 1), _summaryType);
             else
                subbasinMap1.drawLayer(_resultType, _col, _date, _summaryType);
        }

        private void updateTableAndChart()
        {
            tableView1.DataTable = null;
            outputDisplayChart1.clear();
            _statistics = "No Statistics Data Available";
            if (onDataStatisticsChanged != null)
                onDataStatisticsChanged(this, new EventArgs());

            if (_resultType == null || _col == null || _unit == null) return;

            if (!_unit.Results.ContainsKey(_resultType)) return;

            ArcSWAT.SWATUnitResult result = _unit.Results[_resultType];
            if (!result.Columns.Contains(_col)) return;

            //consider year selection
            int year = -1;
            if ((result.Interval == ArcSWAT.SWATResultIntervalType.DAILY || result.Interval == ArcSWAT.SWATResultIntervalType.MONTHLY) && yearCtrl1.DisplayByYear)
                year = yearCtrl1.Year;
            
            //current working result
            ArcSWAT.SWATUnitColumnYearResult oneResult = result.getResult(_col, year);

            //set compare control
            //compareCtrl1.HasObervedData = (oneResult.ObservedData != null);

            //do the update
            if (compareCtrl1.CompareResult == null) //don't compare
            {              
                if (oneResult.Table.Rows.Count == 0 && _type == ArcSWAT.SWATUnitType.HRU)
                    MessageBox.Show("No results for HRU " + _unit.ID.ToString() + ". For more results, please modify file.cio.");

                //remove temporarily to improve performance
                //this.tableView1.Result = oneResult; 
                this.outputDisplayChart1.Result = oneResult;
                this._statistics = oneResult.SeasonStatistics(seasonCtrl1.Season).ToString();
                if(oneResult.ObservedData != null)
                    this._statistics += " || Compare to Observed: " + oneResult.CompareWithObserved.SeasonStatistics(seasonCtrl1.Season).ToString() + ")";
                if (onDataStatisticsChanged != null)
                    onDataStatisticsChanged(this, new EventArgs());
            }
            else //compare
            {
                try
                {
                    ArcSWAT.SWATUnitColumnYearCompareResult compare = null;
                    if (compareCtrl1.CompareResult != null)
                    {
                        compare = oneResult.Compare(compareCtrl1.CompareResult);

                        //compare to scenario
                        this._statistics = string.Format("{0} vs {1}: {2}",
                            result.Unit.Scenario.ModelType,
                            compareCtrl1.CompareResult.ModelType,
                            compare.SeasonStatistics(seasonCtrl1.Season));

                        if (oneResult.ObservedData != null)
                        {
                            //compare to observed
                            this._statistics += " || ";
                            this._statistics += string.Format("{0} vs Observed: {1}",
                                result.Unit.Scenario.ModelType,
                                oneResult.CompareWithObserved.SeasonStatistics(seasonCtrl1.Season));

                            ArcSWAT.SWATUnitColumnYearResult comparedData = compare.ComparedData as ArcSWAT.SWATUnitColumnYearResult;
                            this._statistics += " || ";
                            this._statistics += string.Format("{0} vs Observed: {1}",
                                compareCtrl1.CompareResult.ModelType,
                                comparedData.CompareWithObserved.SeasonStatistics(seasonCtrl1.Season));
                        }
                     }
                    else
                    {
                        compare = oneResult.CompareWithObserved;
                        this._statistics = compare.SeasonStatistics(seasonCtrl1.Season).ToString();
                    }
                    //remove temporarily to improve performance
                    //this.tableView1.CompareResult = compare;
                    this.outputDisplayChart1.CompareResult = compare;                    
                    if (onDataStatisticsChanged != null)
                        onDataStatisticsChanged(this, new EventArgs());
                }
                catch (System.Exception e)
                {
                    SWAT_SQLite.showInformationWindow(e.ToString());
                }
            }

            
        }

        public DotSpatial.Controls.Map Map { get { return subbasinMap1; } }
    }
}
