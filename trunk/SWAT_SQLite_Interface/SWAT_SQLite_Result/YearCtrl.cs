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
    public partial class YearCtrl : UserControl
    {
        public event EventHandler onYearChanged;

        public YearCtrl()
        {
            InitializeComponent();

            cmbYear.SelectedIndexChanged += (s, e) =>
            {
                if (onYearChanged != null) onYearChanged(this, new EventArgs());
            };

            rdbEachYear.Checked = true;
            rdbEachYear.CheckedChanged += (ss, e) => { onDisplayTypeChanged(); };
            rdbAllYears.CheckedChanged += (ss, e) => { onDisplayTypeChanged(); };

            timer1.Tick += (ss, e) =>
            {
                if (cmbYear.SelectedIndex < cmbYear.Items.Count - 1)
                {
                    cmbYear.SelectedIndex += 1;
                    if (onYearChanged != null) onYearChanged(this, new EventArgs());

                    if (cmbYear.SelectedIndex == cmbYear.Items.Count - 1)
                    {
                        bPlay.Text = "Start";
                        timer1.Stop();
                    }
                }
            };

            bPlay.Click += (ss, e) =>
                {
                    if (bPlay.Text.ToLower().Equals("start"))
                    {
                        if (cmbYear.SelectedIndex == cmbYear.Items.Count - 1)
                        {
                            cmbYear.SelectedIndex = 0;
                            if (onYearChanged != null) onYearChanged(this, new EventArgs());
                        }

                        bPlay.Text = "Stop";
                        timer1.Start();
                    }
                    else
                    {
                        bPlay.Text = "Start";
                        timer1.Stop();
                    }                  

                };
        }

        private void onDisplayTypeChanged()
        {
            if (cmbYear.Enabled == DisplayByYear) return;
            cmbYear.Enabled = DisplayByYear; 
            bPlay.Enabled = cmbYear.Enabled; 
            if (onYearChanged != null) 
                onYearChanged(this, new EventArgs());
        }

        /// <summary>
        /// The scenario result
        /// </summary>
        public ArcSWAT.ScenarioResult Scenario
        {
            set
            {
                initializeYearList(value.StartYear, value.EndYear);
                rdbEachYear.Checked = true;
            }
        }

        /// <summary>
        /// Add all years to the list
        /// </summary>
        /// <param name="startYear"></param>
        /// <param name="endYear"></param>
        private void initializeYearList(int startYear, int endYear)
        {
            this.cmbYear.Items.Clear();
            for (int i = startYear; i <= endYear; i++)
                this.cmbYear.Items.Add(i);
            this.cmbYear.SelectedIndex = 0;
        }

        private ArcSWAT.SWATUnitColumnYearObservationData _observedData = null;

        /// <summary>
        /// For used in project view
        /// </summary>
        public ArcSWAT.SWATUnitColumnYearObservationData ObservedData
        {
            set
            {
                this.Enabled = value != null;
                if (value == null) return;

                //don't change when same observed data is displayed
                if (_observedData != null && 
                    _observedData.UnitID == value.UnitID && //same unit id
                    _observedData.Column == value.Column && //same column
                    _observedData.UnitType == value.UnitType &&//same unit type
                    _observedData.FirstDay.Year == value.FirstDay.Year && //same start year
                    _observedData.LastDay.Year == value.LastDay.Year) return; //same end year
                
                _observedData = value;
                initializeYearList(value.FirstDay.Year, value.LastDay.Year);
                rdbAllYears.Checked = true;              
            }
        }

        /// <summary>
        /// If year by year option is used
        /// </summary>
        public bool DisplayByYear 
        { 
            get 
            { 
                return rdbEachYear.Checked; 
            } 
        }
        
        /// <summary>
        /// Selected year
        /// </summary>
        public int Year 
        { 
            get 
            {
                if (DisplayByYear)
                    return int.Parse(cmbYear.SelectedItem.ToString()); 
                return -1; 
            } 
        }
    }
}
