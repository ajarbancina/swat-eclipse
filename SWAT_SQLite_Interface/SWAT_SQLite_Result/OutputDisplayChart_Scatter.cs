using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms.DataVisualization.Charting;
using System.Data;
using SWAT_SQLite_Result.ArcSWAT;

namespace SWAT_SQLite_Result
{
    class OutputDisplayChart_Scatter : Chart
    {
        public void draw(ArcSWAT.SWATUnitColumnYearResult result, ArcSWAT.SeasonType season)
        {
            setup();
            clear();

            if (result.ColumnDisplay.Equals("Flow")) return; //don't display for flow

            //scatter
            DataTable dt = result.CompareWithObserved.SeasonTableForStatistics(season); //already removed missing value

            string col_observed = result.CompareWithObserved.ChartColumns[1];
            string col_simulated = result.CompareWithObserved.ChartColumns[0];
            _dt = dt.Copy();
            foreach (DataRow r in _dt.Rows)
            {
                double observed = double.Parse(r[col_observed].ToString());
                double simulated = double.Parse(r[col_simulated].ToString());

                if (observed > 0) r[col_observed] = Math.Log(observed);
                if (simulated > 0) r[col_simulated] = Math.Log(simulated);
            }
            _scatter.XValueMember = col_observed;
            _scatter.YValueMembers = col_simulated;
            this.DataSource = _dt.Rows;

            _xColName = col_observed;
            _yColName = col_simulated;

            //1:1 line
            double max = Compute(_dt, "Max(" + col_observed + ")", "");
            max = Math.Max(max,Compute(_dt, "Max(" + col_simulated + ")", ""));
            double round = Math.Round(max);
            if (round < max) round += 1;
            max = round;

            _line.Points.Add(new DataPoint(0, 0));
            _line.Points.Add(new DataPoint(max, max));

            //set x,y max
            _chartArea.AxisX.Maximum = max;
            _chartArea.AxisY.Maximum = max;
            _chartArea.AxisX.Minimum = 0;
            _chartArea.AxisY.Minimum = 0;

            //      
            if (result.IsFlow)
            {
                _chartArea.AxisX.Title = string.Format("Ln({0} observed ({1}))", result.ColumnDisplay, result.Unit);
                _chartArea.AxisY.Title = string.Format("Ln({0} simulated ({1}))", result.ColumnDisplay, result.Unit);
            }
            else //loading
            {
                string interval = "day";
                if (result.UnitResult.Interval == SWATResultIntervalType.MONTHLY) interval = "month";
                else if (result.UnitResult.Interval == SWATResultIntervalType.YEARLY) interval = "year";
                _chartArea.AxisX.Title = string.Format("Ln({0} observed ({1}/{2}))", result.ColumnDisplay, result.Unit, interval);
                _chartArea.AxisY.Title = string.Format("Ln({0} simulated ({1}/{2}))", result.ColumnDisplay, result.Unit, interval);
            }
        }

        private double Compute(DataTable dt, string expression, string filter)
        {
            object result = dt.Compute(expression, filter);
            if (result is System.DBNull)
                return ScenarioResultStructure.EMPTY_VALUE;
            double value = ScenarioResultStructure.EMPTY_VALUE;
            double.TryParse(result.ToString(), out value);
            return value;
        }

        private ChartArea _chartArea = null;   //used to change Y title
        private Series _line = null;
        private Series _scatter = null;
        System.Data.DataTable _dt = null;
        string _xColName = "";
        string _yColName = "";

        private void setup()
        {
            if (_chartArea != null) return;

            this.ChartAreas.Clear();
            this.Series.Clear();
            this.Titles.Clear();

            _chartArea = this.ChartAreas.Add("chart_area");
            _chartArea.AxisY.Title = "y";
            _chartArea.AxisX.MajorGrid.Enabled = false;
            _chartArea.AxisY.MajorGrid.Enabled = false;
            _chartArea.AxisX.IsStartedFromZero = true;
            _chartArea.AxisY.IsStartedFromZero = true;
            _chartArea.AxisX.IsLabelAutoFit = true;
            _chartArea.BorderDashStyle = ChartDashStyle.Solid;
           

            //1:1 line
            _line = this.Series.Add("11");
            _line.ChartType = SeriesChartType.Line;
            _line.ChartArea = "chart_area";
            _line.MarkerStyle = MarkerStyle.None;
            _line.Color = System.Drawing.Color.Blue;
            _line.BorderWidth = 2;
            _line.IsValueShownAsLabel = false;
            _line.IsVisibleInLegend = false;

            //scatter
            _scatter = this.Series.Add("scatter");
            _scatter.ChartType = SeriesChartType.Point;
            _scatter.ChartArea = "chart_area";
            _scatter.Color = System.Drawing.Color.Red;
            _scatter.IsVisibleInLegend = false;
            _scatter.IsValueShownAsLabel = false;

            //context menu
            System.Windows.Forms.ToolStripMenuItem exportMenu =
                new System.Windows.Forms.ToolStripMenuItem("Export current results to CSV");
            exportMenu.Click += (ss, _e) => { export(); };

            System.Windows.Forms.ToolStripMenuItem exportImageMenu =
                new System.Windows.Forms.ToolStripMenuItem("Export chart to pic");
            exportImageMenu.Click += (ss, _e) => { exportImage(); };

            this.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
            this.ContextMenuStrip.Items.Add(exportMenu);
            this.ContextMenuStrip.Items.Add(exportImageMenu);
        }

        private void exportImage()
        {
            string imagePath = SWAT_SQLite.InstallationFolder + @"exports\";
            if (!System.IO.Directory.Exists(imagePath)) System.IO.Directory.CreateDirectory(imagePath);
            imagePath += string.Format("export_image_{0:yyyyMMddhhmmss}.jpg", DateTime.Now);
            this.SaveImage(imagePath, ChartImageFormat.Jpeg);
        }

        private void export()
        {
            string csvPath = SWAT_SQLite.InstallationFolder + @"exports\";
            if (!System.IO.Directory.Exists(csvPath)) System.IO.Directory.CreateDirectory(csvPath);
            csvPath += string.Format("export_{0:yyyyMMddhhmmss}.csv", DateTime.Now);
            using (System.IO.StreamWriter writer = new System.IO.StreamWriter(csvPath))
            {
                string output = _xColName;
                output += "," + _yColName;
                writer.WriteLine(output);

                foreach (DataRow r in _dt.Rows)
                {
                    output = r[_xColName].ToString();
                    output += "," + r[_yColName].ToString();
                    writer.WriteLine(output);
                }
            }
        }

        public void clear()
        {
            foreach (Series line in this.Series)
            {
                line.Points.Clear();
                line.XValueMember = "";
                line.YValueMembers = "";
            }
            if (_chartArea != null)
                _chartArea.AxisY.Title = "";

            this.DataSource = null;
        }
    }
}
