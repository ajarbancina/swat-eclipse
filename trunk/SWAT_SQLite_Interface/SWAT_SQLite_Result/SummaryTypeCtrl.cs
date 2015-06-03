using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SWAT_SQLite_Result
{
    public partial class SummaryTypeCtrl : UserControl
    {
        public SummaryTypeCtrl()
        {
            InitializeComponent();

            //default is annual for the map display
            this.rdbAnnual.Checked = true;
            this._summaryType = ArcSWAT.ResultSummaryType.ANNUAL;

            this.rdbAnnual.CheckedChanged += (ss, e) => { whenClickHappens(); };
            this.rdbAverageAnnual.CheckedChanged += (ss, e) => { whenClickHappens(); };
            this.rdbTimeStep.CheckedChanged += (ss, e) => { whenClickHappens(); };
        }

        /// <summary>
        /// Showing the current time step for Time step display
        /// </summary>
        public DateTime TimeForTimeStep
        {
            set
            {
                string[] tt = rdbTimeStep.Text.Split(',');
                rdbTimeStep.Text = string.Format("{0},{1:yyyy-MM-dd}",tt[0],value);
            }
        }

        /// <summary>
        /// Corresponding scenario result
        /// </summary>
        /// <remarks>Change the text based on output interval</remarks>
        public ArcSWAT.ScenarioResult ScenarioResult
        {
            set
            {
                switch (value.Interval)
                {
                    case ArcSWAT.SWATResultIntervalType.DAILY:
                        rdbTimeStep.Text = "Each Day";                        
                        break;
                    case ArcSWAT.SWATResultIntervalType.MONTHLY:
                        rdbTimeStep.Text = "Each Month";
                        break;
                    case ArcSWAT.SWATResultIntervalType.YEARLY:
                        rdbTimeStep.Text = "Each Year";
                        break;
                    default:
                        rdbTimeStep.Text = "Unknown";
                        break;
                }
                rdbAverageAnnual.Text = string.Format("Average Annual, {0}-{1}, {2} years", value.StartYear, value.EndYear,
                    value.EndYear - value.StartYear + 1);
                CurrentYear = value.StartYear;
                TimeForTimeStep = new DateTime(value.StartYear, 1, 1);
            }
        }

        /// <summary>
        /// Current year selection for result display
        /// </summary>
        /// <remarks>Change the text for the annual option to show the current year</remarks>
        public int CurrentYear
        {
            set
            {
                rdbAnnual.Text = string.Format("Annual,{0}", value);
            }
        }

        /// <summary>
        /// Handler for change
        /// </summary>
        /// <remarks>Use variable to avoid duplicated response</remarks>
        private void whenClickHappens()
        {
            if (_summaryType != SummaryType)
            {
                _summaryType = SummaryType;
                if (onSummaryTypeChanged != null) onSummaryTypeChanged(this, new EventArgs());               
            }
        }

        /// <summary>
        /// Event for summary type change
        /// </summary>
        public event EventHandler onSummaryTypeChanged;

        /// <summary>
        /// Variable to avoid duplicated response
        /// </summary>
        private ArcSWAT.ResultSummaryType _summaryType = ArcSWAT.ResultSummaryType.AVERAGE_ANNUAL;

        /// <summary>
        /// Summary Type
        /// </summary>
        public ArcSWAT.ResultSummaryType SummaryType
        {
            get
            {
                if (rdbAnnual.Checked) return ArcSWAT.ResultSummaryType.ANNUAL;
                if (rdbAverageAnnual.Checked) return ArcSWAT.ResultSummaryType.AVERAGE_ANNUAL;
                else return ArcSWAT.ResultSummaryType.TIMESTEP;
            }
        }
    }
}
