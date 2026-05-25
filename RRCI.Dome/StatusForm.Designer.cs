namespace RRCI.Dome
{
    partial class StatusForm
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.lblState = new System.Windows.Forms.Label();
            this.lblPercent = new System.Windows.Forms.Label();
            this.lblPulses = new System.Windows.Forms.Label();
            this.lblFault = new System.Windows.Forms.Label();
            this.progressRoof = new System.Windows.Forms.ProgressBar();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // lblState
            // 
            this.lblState.AutoSize = true;
            this.lblState.Location = new System.Drawing.Point(12, 9);
            this.lblState.Name = "lblState";
            this.lblState.Size = new System.Drawing.Size(32, 13);
            this.lblState.TabIndex = 0;
            this.lblState.Text = "State";
            // 
            // lblPercent
            // 
            this.lblPercent.AutoSize = true;
            this.lblPercent.Location = new System.Drawing.Point(11, 94);
            this.lblPercent.Name = "lblPercent";
            this.lblPercent.Size = new System.Drawing.Size(44, 13);
            this.lblPercent.TabIndex = 1;
            this.lblPercent.Text = "Percent";
            // 
            // lblPulses
            // 
            this.lblPulses.AutoSize = true;
            this.lblPulses.Location = new System.Drawing.Point(12, 34);
            this.lblPulses.Name = "lblPulses";
            this.lblPulses.Size = new System.Drawing.Size(38, 13);
            this.lblPulses.TabIndex = 2;
            this.lblPulses.Text = "Pulses";
            // 
            // lblFault
            // 
            this.lblFault.AutoSize = true;
            this.lblFault.Location = new System.Drawing.Point(12, 62);
            this.lblFault.Name = "lblFault";
            this.lblFault.Size = new System.Drawing.Size(30, 13);
            this.lblFault.TabIndex = 3;
            this.lblFault.Text = "Fault";
            // 
            // progressRoof
            // 
            this.progressRoof.Location = new System.Drawing.Point(78, 94);
            this.progressRoof.Name = "progressRoof";
            this.progressRoof.Size = new System.Drawing.Size(100, 23);
            this.progressRoof.TabIndex = 4;
            // 
            // timer1
            // 
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // StatusForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(211, 137);
            this.ControlBox = false;
            this.Controls.Add(this.progressRoof);
            this.Controls.Add(this.lblFault);
            this.Controls.Add(this.lblPulses);
            this.Controls.Add(this.lblPercent);
            this.Controls.Add(this.lblState);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.Name = "StatusForm";
            this.Text = "Roof Status";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.StatusForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblState;
        private System.Windows.Forms.Label lblPercent;
        private System.Windows.Forms.Label lblPulses;
        private System.Windows.Forms.Label lblFault;
        private System.Windows.Forms.ProgressBar progressRoof;
        private System.Windows.Forms.Timer timer1;
    }
}