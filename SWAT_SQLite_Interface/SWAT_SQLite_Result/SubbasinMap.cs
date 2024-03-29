﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotSpatial.Controls;
using DotSpatial.Symbology;
using DotSpatial.Data;
using System.Diagnostics;
using System.Data;
using System.Drawing;
using DotSpatial.Topology;

namespace SWAT_SQLite_Result
{
    public delegate void LayerSelectionChangedEventHandler(ArcSWAT.SWATUnitType unitType, int selectedid);

    /// <summary>
    /// Display subbasin results
    /// </summary>
    class SubbasinMap : Map
    {
        public static string RESULT_COLUMN = "RESULT";
        public static string OBSERVED_COLUMN = "OBSERVED";
        public static string ID_COLUMN_NAME = "subbasin";

        private ArcSWAT.Project _project = null;
        private ArcSWAT.ScenarioResult _scenario = null;
        private IFeatureLayer _workingLayer = null;
        public event LayerSelectionChangedEventHandler onLayerSelectionChanged = null;
        private ArcSWAT.SWATUnitType _type = ArcSWAT.SWATUnitType.UNKNOWN;
        private string _resultColumn = "";
        private ArcSWAT.ResultSummaryType _summaryType = ArcSWAT.ResultSummaryType.AVERAGE_ANNUAL;
        private DateTime _resultDate = DateTime.Now;
        private Dictionary<int, ArcSWAT.SWATUnit> _unitList = null;
        private ArcSWAT.SWATResultIntervalType _interval = ArcSWAT.SWATResultIntervalType.UNKNOWN;

        public SubbasinMap()
        {
            //context menu
            System.Windows.Forms.ToolStripMenuItem exportMenu =
                new System.Windows.Forms.ToolStripMenuItem("Export current results to CSV");
            exportMenu.Click += (ss, _e) => { export(); };


            this.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
            this.ContextMenuStrip.Items.Add(exportMenu);
        }

        /// <summary>
        /// For project view
        /// </summary>
        /// <param name="project"></param>
        public void setProject(ArcSWAT.Project project)
        {
            _project = project;

            this.MapFrame.ProjectionModeDefine = DotSpatial.Controls.ActionMode.Always;
            this.MapFrame.ProjectionModeReproject = DotSpatial.Controls.ActionMode.Never;
            this.Resized += (ss, ee) => { this.ZoomToMaxExtent(); };

            //add layers
            drawObservationLayers();
            this.FunctionMode = DotSpatial.Controls.FunctionMode.Select;
        }

        private void export()
        {
            if (_workingLayer == null) return;
            DataTable dt = _workingLayer.DataSet.DataTable;
            int resultIndex = dt.Columns.IndexOf(RESULT_COLUMN);
            if (resultIndex == -1) return;
            int idIndex = dt.Columns.IndexOf(ID_COLUMN_NAME);

            string csvPath = SWAT_SQLite.InstallationFolder + @"exports\";
            if (!System.IO.Directory.Exists(csvPath)) System.IO.Directory.CreateDirectory(csvPath);
            csvPath += string.Format("export_map_{0:yyyyMMddhhmmss}.csv", DateTime.Now);
            using (System.IO.StreamWriter writer = new System.IO.StreamWriter(csvPath))
            {
                //write summary type
                writer.WriteLine(_summaryType);

                //write time range
                if (_summaryType == ArcSWAT.ResultSummaryType.ANNUAL)
                    writer.WriteLine(_resultDate.Year);
                else if (_summaryType == ArcSWAT.ResultSummaryType.TIMESTEP)
                    writer.WriteLine("yyyy-MM-dd", _resultDate);
                else
                    writer.WriteLine(string.Format("{0} - {1}", _scenario.StartYear, _scenario.EndYear));

                //write header
                writer.WriteLine(string.Format("{0},{1}", _type, _resultColumn));

                //write the real data
                foreach (DataRow r in dt.Rows)
                {
                    writer.WriteLine(string.Format("{0},{1}", r[idIndex], r[resultIndex]));
                }
            }
        }

        private void drawObservationLayers()
        {
            if (_scenario != null) return;
            if (_project == null) return;

            //add layers
            this.Layers.Clear();
            this.addLayer(_project.Spatial.SubbasinShapefile, "Subbasin", true, false);
            this.addLayer(_project.Spatial.ReachShapefile, "Reach", true, true);
            this.addLayer(_project.Spatial.MonitoringShapefile, "Reservoir", true, true);
        }

        /// <summary>
        /// For result display view
        /// </summary>
        /// <param name="project"></param>
        /// <param name="scenario"></param>
        /// <param name="type"></param>
        public void setProjectScenario(ArcSWAT.Project project, ArcSWAT.ScenarioResult scenario, ArcSWAT.SWATUnitType type)
        {
            if (type != ArcSWAT.SWATUnitType.SUB && type != ArcSWAT.SWATUnitType.RCH && 
                type != ArcSWAT.SWATUnitType.HRU && type != ArcSWAT.SWATUnitType.RES) return;

            _project = project;
            _scenario = scenario;
            _type = type;

            this.MapFrame.ProjectionModeDefine = DotSpatial.Controls.ActionMode.Always;
            this.MapFrame.ProjectionModeReproject = DotSpatial.Controls.ActionMode.Never;
            this.Resized += (ss, ee) => { this.ZoomToMaxExtent(); };

            this.Layers.Clear();

            if (type == ArcSWAT.SWATUnitType.SUB)
            {
                _workingLayer = addLayer(project.Spatial.SubbasinShapefile, "Subbasin", false, true);
                addLayer(project.Spatial.ReachShapefile, "Reach", false, false);
            }
            else if (type == ArcSWAT.SWATUnitType.RCH)
            {
                addLayer(project.Spatial.SubbasinShapefile, "Subbasin", false, false);
                _workingLayer = addLayer(project.Spatial.ReachShapefile, "Reach", false, true);               
            }
            else if (type == ArcSWAT.SWATUnitType.HRU)
            {
                _workingLayer = addLayer(project.Spatial.SubbasinShapefile, "Subbasin", false, true);
                addLayer(project.Spatial.ReachShapefile, "Reach", false, false);
            }
            else if (type == ArcSWAT.SWATUnitType.RES)
            {
                addLayer(project.Spatial.SubbasinShapefile, "Subbasin", false, false);
                addLayer(project.Spatial.ReachShapefile, "Reach", false, false);
                _workingLayer = addLayer(project.Spatial.MonitoringShapefile, "Reservoir", false, true);
            }
            this.FunctionMode = DotSpatial.Controls.FunctionMode.Select;
        }

        public ArcSWAT.SWATResultIntervalType Interval
        {
            get
            {
                if (_scenario != null) return _scenario.Interval;
                return _interval;
            }
            set
            {
                _interval = value;

                //layers needs to be re-draw when time interval for observation data is changed
                //needs to be improved in the future, consider just update reach and reservoir layer
                drawObservationLayers();
            }
        }

        /// <summary>
        /// Update the observed status when the data is loaded or deleted.
        /// </summary>
        /// <param name="unitType"></param>
        /// <param name="id"></param>
        public void updateObservedStatus(ArcSWAT.SWATUnitType unitType, int id)
        {
            FeatureType feaType = FeatureType.Line;
            if (unitType == ArcSWAT.SWATUnitType.RES)
                feaType = FeatureType.Point;

            foreach (IFeatureLayer layer in Layers)
            {
                if (layer.DataSet.FeatureType == feaType)
                {
                    DataRow[] rows = layer.DataSet.DataTable.Select(string.Format("{0}={1}", ID_COLUMN_NAME,id));
                    if (rows == null || rows.Length == 0) continue;

                    DataRow r = rows[0];
                    if (_project.Observation(Interval).getObservedData(unitType, id).Count > 0)
                        r[OBSERVED_COLUMN] = 1;
                    else
                        r[OBSERVED_COLUMN] = 0;

                    //re-draw all feature using defined categories
                    layer.ApplyScheme(layer.Symbology);
                }              
            }
        }

        private ArcSWAT.HRU _crrentHRU = null;

        public ArcSWAT.HRU HRU
        {
            set
            {
                if (_type != ArcSWAT.SWATUnitType.HRU) return;
                if (_workingLayer == null) return;
                if(value == null)return;

                if (_crrentHRU != null && _crrentHRU.Subbasin.ID == value.ID) return;

                _crrentHRU = value;

                //hilight the belonging subbasin
                _workingLayer.SelectByAttribute(string.Format("{0}={1}", ID_COLUMN_NAME, _crrentHRU.Subbasin.ID));
            }
        }        

        private IFeatureLayer addLayer(string path, string name,bool isForObserved, bool isWorkingLayer)
        {
            Debug.WriteLine(DateTime.Now);
            Debug.WriteLine("Adding Layer..., " + name);

            if (!System.IO.File.Exists(path))
            {
                Debug.WriteLine(path + " doesn't exist!");
                return null;
            }

            IFeatureLayer layer = this.Layers.Add(path) as IFeatureLayer;
            layer.SelectionEnabled = isWorkingLayer;
            layer.LegendText = name;

            foreach (DataColumn col in layer.DataSet.DataTable.Columns)
                col.ColumnName = col.ColumnName.ToLower();

            //working layer and result display
            if (isWorkingLayer && !isForObserved)
            {
                //add result column
                DataTable dt = layer.DataSet.DataTable;
                dt.Columns.Add(RESULT_COLUMN, typeof(double));

                //create schema for result display
                layer.Symbology.EditorSettings.ClassificationType = ClassificationType.Quantities;
                layer.Symbology.EditorSettings.FieldName = RESULT_COLUMN;
                layer.Symbology.EditorSettings.IntervalMethod = IntervalMethod.Quantile;
                layer.Symbology.EditorSettings.IntervalSnapMethod = IntervalSnapMethod.SignificantFigures;
                layer.Symbology.EditorSettings.IntervalRoundingDigits = 3; //3 significant number
                layer.Symbology.EditorSettings.StartSize = 5;
                layer.Symbology.EditorSettings.EndSize = 25;
                layer.Symbology.EditorSettings.NumBreaks = 5;
                layer.Symbology.EditorSettings.UseSizeRange = true;

                //start and end color
                layer.Symbology.EditorSettings.StartColor = Color.Green;
                layer.Symbology.EditorSettings.EndColor = Color.Red;                
            }

            //set normal symbol
            //for result display, this is just the initial symbol. The symbol would be updated based on result
            //after the result is retrieved.
            if (layer.DataSet.FeatureType == DotSpatial.Topology.FeatureType.Polygon) //subbasin
            {
                layer.Symbolizer = new PolygonSymbolizer(System.Drawing.Color.LightGray, System.Drawing.Color.Black, 0.5);

                //show label for subbasin
                MapLabelLayer label = new MapLabelLayer();
                label.Symbology.Categories[0].Expression = "[" + ID_COLUMN_NAME + "]";
                layer.LabelLayer = label;
                layer.ShowLabels = true;                
            }
            else if (layer.DataSet.FeatureType == DotSpatial.Topology.FeatureType.Line) //reach
            {
                layer.Symbolizer = new LineSymbolizer(System.Drawing.Color.Blue, 3.0);

                //set selection sysmbol for reach as wider red to make it more obvious
                layer.SelectionSymbolizer = new LineSymbolizer(System.Drawing.Color.Red, 3.0);
            }
            else if (layer.DataSet.FeatureType == DotSpatial.Topology.FeatureType.Point) //reservoir
            {
                //set the symbol color,shape and size
                layer.Symbolizer = new PointSymbolizer(Color.Green, DotSpatial.Symbology.PointShape.Hexagon, 20.0);
                layer.SelectionSymbolizer = new PointSymbolizer(Color.Cyan, DotSpatial.Symbology.PointShape.Hexagon, 20.0);


                //also set to just show reservoir
                //first to see if there are some reservoir there Type = R
                if (layer.DataSet.DataTable.Rows.Count == 0) { Layers.Remove(layer as IMapLayer); return null; }

                int reservoirNum = int.Parse(layer.DataSet.DataTable.Compute("count(" + ID_COLUMN_NAME+")", "type = 'R' or type = 'r'").ToString());
                if (reservoirNum <= 0) { Layers.Remove(layer as IMapLayer); return null; }

                //only show reservoir
                List<int> hiddenMoniterPoints = new List<int>();
                for (int i = 0; i < layer.DataSet.DataTable.Rows.Count; i++)
                {
                    ArcSWAT.RowItem item = new ArcSWAT.RowItem(layer.DataSet.DataTable.Rows[i]);
                    string type = item.getColumnValue_String("type");

                    if (!type.Equals("R") && !type.Equals("r"))
                        hiddenMoniterPoints.Add(i);
                }
                layer.RemoveFeaturesAt(hiddenMoniterPoints);
            }

            //add a column to show if the feature has observed data
            if (isForObserved)
            {
                //add observed column
                DataTable dt = layer.DataSet.DataTable;
                dt.Columns.Add(OBSERVED_COLUMN, typeof(int));             
                

                //create schema observed column to make feature with observed data more obvious
                if (layer.DataSet.FeatureType != DotSpatial.Topology.FeatureType.Polygon)
                {
                    layer.Symbology.ClearCategories();

                    //get the observed data status
                    ArcSWAT.SWATUnitType unitType = ArcSWAT.SWATUnitType.RCH;
                    if (layer.DataSet.FeatureType == DotSpatial.Topology.FeatureType.Point)
                        unitType = ArcSWAT.SWATUnitType.RES;

                    foreach (DataRow r in layer.DataSet.DataTable.Rows)
                    {
                        int id = getIDFromFeatureRow(r);
                        if (_project.Observation(Interval).getObservedData(unitType, id).Count > 0)
                            r[OBSERVED_COLUMN] = 1;
                        else
                            r[OBSERVED_COLUMN] = 0;
                    }

                    //set the category
                    IFeatureCategory cat_observed = layer.Symbology.CreateNewCategory(Color.Blue, 3.0) as IFeatureCategory;
                    cat_observed.FilterExpression = string.Format("[{0}]=0", OBSERVED_COLUMN.ToUpper());

                    IFeatureCategory cat_no_observed = layer.Symbology.CreateNewCategory(Color.Red, 3.0) as IFeatureCategory;
                    cat_no_observed.FilterExpression = string.Format("[{0}]=1", OBSERVED_COLUMN.ToUpper());
                    
                    //for reservoir, change default size and shape
                    if (layer.DataSet.FeatureType == DotSpatial.Topology.FeatureType.Point)
                    {
                        cat_observed.SelectionSymbolizer = new PointSymbolizer(Color.Cyan, DotSpatial.Symbology.PointShape.Hexagon, 20.0);
                        cat_no_observed.SelectionSymbolizer = new PointSymbolizer(Color.Cyan, DotSpatial.Symbology.PointShape.Hexagon, 20.0);

                        ((cat_observed.Symbolizer as PointSymbolizer).Symbols[0] as SimpleSymbol).Size = new Size2D(20.0, 20.0);
                        ((cat_no_observed.Symbolizer as PointSymbolizer).Symbols[0] as SimpleSymbol).Size = new Size2D(20.0, 20.0);

                        ((cat_observed.Symbolizer as PointSymbolizer).Symbols[0] as SimpleSymbol).PointShape = DotSpatial.Symbology.PointShape.Hexagon;
                        ((cat_no_observed.Symbolizer as PointSymbolizer).Symbols[0] as SimpleSymbol).PointShape = DotSpatial.Symbology.PointShape.Hexagon;
                    }

                    layer.Symbology.AddCategory(cat_observed);
                    layer.Symbology.AddCategory(cat_no_observed);

                    layer.ApplyScheme(layer.Symbology);

                }              
                
            }

            if (isWorkingLayer)
            {
                layer.SelectionEnabled = true;

                //for selection changed event
                layer.SelectionChanged += (ss, _e) =>
                {
                    if (onLayerSelectionChanged == null) return;

                    ArcSWAT.SWATUnitType unitType = ArcSWAT.SWATUnitType.SUB;
                    if (layer.DataSet.FeatureType == DotSpatial.Topology.FeatureType.Point)
                        unitType = ArcSWAT.SWATUnitType.RES;
                    else if (layer.DataSet.FeatureType == DotSpatial.Topology.FeatureType.Line)
                        unitType = ArcSWAT.SWATUnitType.RCH;

                    int id = -1;
                    if (layer.Selection.NumRows() > 0)
                    {
                        IFeature fea = layer.Selection.ToFeatureList()[0];
                        id = getIDFromFeatureRow(fea.DataRow);
                    }
                    onLayerSelectionChanged(unitType,id);
                    _id = id;
                };
            }           

            return layer;
        }

        private int _id = -1;
        public int ID
        {
            get { return _id; }
            set 
            { 
                if (value <= 0 || value == _id || _workingLayer == null) return;

                LayerSelectionChangedEventHandler handler = onLayerSelectionChanged;
                onLayerSelectionChanged = null;
                _workingLayer.SelectByAttribute(string.Format("{0}={1}", ID_COLUMN_NAME,value));
                _id = value;
                onLayerSelectionChanged = handler;
            }
        }

        private int getIDFromFeatureRow(DataRow r)
        {
            ArcSWAT.RowItem item = new ArcSWAT.RowItem(r);
            return item.getColumnValue_Int(ID_COLUMN_NAME);
        }

        /// <summary>
        /// update corresponding layer 
        /// </summary>
        /// <param name="type"></param>
        public void drawLayer(string resultType, string col, 
            DateTime date, ArcSWAT.ResultSummaryType summaryType)
        {
            if (_type != ArcSWAT.SWATUnitType.SUB && _type != ArcSWAT.SWATUnitType.RCH) return;
            if (_workingLayer == null) return;
            if (_unitList == null && _scenario != null)
            {
                if (_type == ArcSWAT.SWATUnitType.SUB) _unitList = _scenario.Subbasins;
                if (_type == ArcSWAT.SWATUnitType.RCH) _unitList = _scenario.Reaches;
            }
            if (_unitList == null) return;

            _resultColumn = col;
            _summaryType = summaryType;
            _resultDate = date;

            Debug.WriteLine("Draw Layer, " + _workingLayer.LegendText);
            Debug.WriteLine("Getting results...");
            DateTime d = DateTime.Now;

            DataTable dt = _workingLayer.DataSet.DataTable;
            int resultIndex = dt.Columns.IndexOf(RESULT_COLUMN);
            int idIndex = dt.Columns.IndexOf(ID_COLUMN_NAME);
            if (summaryType == ArcSWAT.ResultSummaryType.AVERAGE_ANNUAL ||
                summaryType == ArcSWAT.ResultSummaryType.ANNUAL)
            {
                int year = -1;
                if (summaryType == ArcSWAT.ResultSummaryType.ANNUAL)
                    year = date.Year;

                Dictionary<int, double> ave_annual = _scenario.getAverageAnnualResults(_type, col, year);
                foreach (DataRow r in dt.Rows)
                {
                    r[resultIndex] = ArcSWAT.ScenarioResultStructure.EMPTY_VALUE;

                    int id = int.Parse(r[idIndex].ToString());
                    r[resultIndex] = ave_annual[id];
                }
            }
            else
            {
                foreach (DataRow r in dt.Rows)
                {
                    r[resultIndex] = ArcSWAT.ScenarioResultStructure.EMPTY_VALUE;

                    int id = int.Parse(r[idIndex].ToString());
                    if (!_unitList.ContainsKey(id)) continue;

                    ArcSWAT.SWATUnit unit = _unitList[id];
                    if (!unit.Results.ContainsKey(resultType)) continue;

                    ArcSWAT.SWATUnitResult result = unit.Results[resultType];
                    if (!result.Columns.Contains(col)) continue;


                    if (summaryType == ArcSWAT.ResultSummaryType.TIMESTEP)
                        r[resultIndex] = result.getData(col, date);
                    else if (summaryType == ArcSWAT.ResultSummaryType.ANNUAL)   //annual 
                        r[resultIndex] = result.getData(col, date.Year);
                 }
            }

            Debug.WriteLine(DateTime.Now.Subtract(d).TotalMilliseconds);
            Debug.WriteLine("setLayerSchema");
            d = DateTime.Now;

            //update symbol
            setLayerSchema(_workingLayer);

            Debug.WriteLine(DateTime.Now.Subtract(d).TotalMilliseconds);

            ////update chart
            //onLayerSelectionChanged(type);
            if (onMapUpdated != null) onMapUpdated(this, new EventArgs());
        }

        public DataTable DataTable
        {
            get
            {
                DataView view = new DataView(_workingLayer.DataSet.DataTable);
                return view.ToTable(false,ID_COLUMN_NAME, RESULT_COLUMN);
            }
        }

        public event EventHandler onMapUpdated;

        private void setLayerSchema(IFeatureLayer layer)
        {
            int selectFeaIndex = -1;
            for (int i = 0; i < layer.DrawnStates.Count(); i++)
                if (layer.DrawnStates[i].Selected)
                {
                    selectFeaIndex = i;
                    break;
                }

            layer.Symbology.CreateCategories(layer.DataSet.DataTable);

            foreach (IFeatureCategory fc in layer.Symbology.GetCategories())
                fc.ContextMenuItems.Clear();

            try
            {
                layer.ApplyScheme(layer.Symbology); //must have this to make changes work
            }
            catch
            {
            }
            

            //must after updateLayerResultColumnName to get column name correct
            if (selectFeaIndex > -1) layer.Select(selectFeaIndex);
        }
    }
}
