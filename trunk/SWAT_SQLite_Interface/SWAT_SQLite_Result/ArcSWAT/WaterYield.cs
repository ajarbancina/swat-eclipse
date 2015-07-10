using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SWAT_SQLite_Result.ArcSWAT
{
    class WaterYield
    {
        public WaterYield(double wateryield, double surfacerunoff, double lateralflow,
            double tileflow, double groundwaterflow)
        {
            _water_yield = wateryield;
            _surface_runoff = surfacerunoff;
            _lateral_flow = lateralflow;
            _tile_flow = tileflow;
            _groundwater_flow = groundwaterflow;
        }
        private double _water_yield;

        public double TotalWaterYield
        {
            get { return _water_yield; }
            set { _water_yield = value; }
        }
        private double _surface_runoff;

        public double SurfaceRunoff
        {
            get { return _surface_runoff; }
            set { _surface_runoff = value; }
        }
        private double _lateral_flow;

        public double LateralFlow
        {
            get { return _lateral_flow; }
            set { _lateral_flow = value; }
        }
        private double _tile_flow;

        public double TileFlow
        {
            get { return _tile_flow; }
            set { _tile_flow = value; }
        }
        private double _groundwater_flow;

        public double GroundwaterFlow
        {
            get { return _groundwater_flow; }
            set { _groundwater_flow = value; }
        }




    }
}
