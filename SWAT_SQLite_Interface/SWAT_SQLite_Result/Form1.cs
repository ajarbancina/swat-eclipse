using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SWAT_SQLite_Result
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            testSQLiteClear();
            return;
            ArcSWAT.Project p = new ArcSWAT.Project(@"C:\Swat\ArcSWAT\Databases\Example1_model"); //University
            //ArcSWAT.Project p = new ArcSWAT.Project(@"C:\Users\yuz\Downloads\Example1_model");   //AAFC
            //richTextBox1.Text = p.ToString();
            //System.Diagnostics.Debug.WriteLine(richTextBox1.Text);

            //projectTree1.Project = p;

            //DataTable dt = ArcSWAT.Query.GetDataTable("select date(printf('%d-%02d-%02d',yr,mo,da)) from sub where sub=1",
            //    @"C:\Swat\ArcSWAT\Databases\Example1_model\Scenarios\Default\TxtInOut\result.db3");
            //foreach (DataRow r in dt.Rows)
            //    System.Diagnostics.Debug.WriteLine(r[0]);

            //tableResultsCtrl1.SWATUnits = p.Scenarios["Default"].getModelResult(ArcSWAT.SWATModelType.SWAT_488).Subbasins;
            p.Scenarios["Default"].modifyOutputInterval(ArcSWAT.SWATResultIntervalType.MONTHLY);
        }

        private void testSQLiteClear()
        {
            string dbPath = @"C:\Users\yuz\Downloads\result_canswat_monthly.db3";

            //get all the table names
            //DataTable dt = ArcSWAT.Query.GetDataTable("select name from sqlite_master where type = 'table'", dbPath);
            //foreach (DataRow r in dt.Rows)
            //{
            //    string tblName = r[0].ToString();
            //    System.Diagnostics.Debug.WriteLine(tblName);
            //    ArcSWAT.SQLite.insert(dbPath, "DROP TABLE IF EXISTS " + tblName);
            //}
            ArcSWAT.SQLite.exeCmdWithoutTransaction(dbPath, "ANALYZE");
            //ArcSWAT.SQLite.exeCmdWithoutTransaction(dbPath, "VACUUM");
            ArcSWAT.Query.CloseConnection(dbPath);
        }
    }
}
