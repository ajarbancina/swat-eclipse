namespace SWAT_SQLite_Result
{
    partial class SummaryTypeCtrl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.rdbAverageAnnual = new System.Windows.Forms.RadioButton();
            this.rdbAnnual = new System.Windows.Forms.RadioButton();
            this.rdbTimeStep = new System.Windows.Forms.RadioButton();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.rdbAverageAnnual);
            this.groupBox1.Controls.Add(this.rdbAnnual);
            this.groupBox1.Controls.Add(this.rdbTimeStep);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(194, 97);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Summary";
            // 
            // rdbAverageAnnual
            // 
            this.rdbAverageAnnual.AutoSize = true;
            this.rdbAverageAnnual.Location = new System.Drawing.Point(7, 68);
            this.rdbAverageAnnual.Name = "rdbAverageAnnual";
            this.rdbAverageAnnual.Size = new System.Drawing.Size(101, 17);
            this.rdbAverageAnnual.TabIndex = 2;
            this.rdbAverageAnnual.TabStop = true;
            this.rdbAverageAnnual.Text = "Average Annual";
            this.rdbAverageAnnual.UseVisualStyleBackColor = true;
            // 
            // rdbAnnual
            // 
            this.rdbAnnual.AutoSize = true;
            this.rdbAnnual.Location = new System.Drawing.Point(7, 44);
            this.rdbAnnual.Name = "rdbAnnual";
            this.rdbAnnual.Size = new System.Drawing.Size(123, 17);
            this.rdbAnnual.TabIndex = 1;
            this.rdbAnnual.TabStop = true;
            this.rdbAnnual.Text = "Annual, Current Year";
            this.rdbAnnual.UseVisualStyleBackColor = true;
            // 
            // rdbTimeStep
            // 
            this.rdbTimeStep.AutoSize = true;
            this.rdbTimeStep.Location = new System.Drawing.Point(7, 20);
            this.rdbTimeStep.Name = "rdbTimeStep";
            this.rdbTimeStep.Size = new System.Drawing.Size(73, 17);
            this.rdbTimeStep.TabIndex = 0;
            this.rdbTimeStep.TabStop = true;
            this.rdbTimeStep.Text = "Time Step";
            this.rdbTimeStep.UseVisualStyleBackColor = true;
            // 
            // SummaryTypeCtrl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox1);
            this.Name = "SummaryTypeCtrl";
            this.Size = new System.Drawing.Size(194, 97);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton rdbAverageAnnual;
        private System.Windows.Forms.RadioButton rdbAnnual;
        private System.Windows.Forms.RadioButton rdbTimeStep;
    }
}
