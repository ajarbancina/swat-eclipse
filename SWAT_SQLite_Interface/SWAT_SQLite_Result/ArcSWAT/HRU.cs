﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Diagnostics;

namespace SWAT_SQLite_Result.ArcSWAT
{
    class HRU : SWATUnit
    {
        public HRU(DataRow hruInfoRow, ScenarioResult scenario) : base(hruInfoRow,scenario)
        {
            RowItem item = new RowItem(hruInfoRow);
            _id = item.getColumnValue_Int(ScenarioResultStructure.COLUMN_NAME_HRU);
            _area = item.getColumnValue_Double(ScenarioResultStructure.COLUMN_NAME_AREA_KM2);
            _area_fr_sub = item.getColumnValue_Double(ScenarioResultStructure.COLUMN_NAME_AREA_FR_SUB);
            _area_fr_wshd = item.getColumnValue_Double(ScenarioResultStructure.COLUMN_NAME_AREA_FR_WSHD);

            //connect hru and subbasin
            int subid = item.getColumnValue_Int(ScenarioResultStructure.COLUMN_NAME_SUB);
            if (scenario.Subbasins.ContainsKey(subid))
            {
                _sub = scenario.Subbasins[subid] as Subbasin;
                if (_sub != null) _sub.addHRU(this);                    
            }
        }

        public override string BasicInfoTableName
        {
            get { return ScenarioResultStructure.INFO_TABLE_NAME_HRU; }
        }

        public override SWATUnitType Type
        {
            get { return SWATUnitType.HRU; }
        }

        public override string ToString()
        {
            return base.ToString() +
                string.Format("Sub : {0}\tArea : {1:F4} km2\tArea Fraction in Subbasin : {2:P2}\tArea Fraction in Watershed : {3:P2}",
                _sub == null ? -1 : _sub.ID, _area, _area_fr_sub, _area_fr_wshd);
        }

        private Subbasin _sub = null;
        private double _area = ScenarioResultStructure.EMPTY_VALUE;
        private double _area_fr_sub = ScenarioResultStructure.EMPTY_VALUE;
        private double _area_fr_wshd = ScenarioResultStructure.EMPTY_VALUE;           

    }
}
