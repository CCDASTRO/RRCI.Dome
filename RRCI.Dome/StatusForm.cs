using RRCI.DomeDriver;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RRCI.Dome
{
    public partial class StatusForm : Form
    {
        private RRCI.DomeDriver.Dome driver;
        private bool allowClose = false;
        public StatusForm(RRCI.DomeDriver.Dome dome)
        {
            InitializeComponent();

            driver = dome;

            lblState.Text = "Initializing";
            lblPercent.Text = "0%";
            lblPulses.Text = "0";
            lblFault.Text = "None";

            this.TopMost = true;

            timer1.Interval = 250;
            timer1.Start();
        }

        private void StatusForm_Load(object sender, EventArgs e)
        {

        }
        public void ForceClose()
        {
            allowClose = true;
            Close();
        }
        private void timer1_Tick(
    object sender,
    EventArgs e)
        {
            //this.Text = $"P={RoofTelemetry.CurrentPulseCount} O={RoofTelemetry.OpenPulseCount} %={RoofTelemetry.PercentOpen}";
            // Force telemetry refresh from the connected driver
            try
            {
                var state =
                    driver.ShutterStatus;
            }
            catch
            {
            }

            lblState.Text =
                "Roof State: " +
                RoofTelemetry.ShutterState;

            lblPercent.Text =
                "Position: " +
                RoofTelemetry.PercentOpen +
                "%";

            lblPulses.Text =
                "Pulses: " +
                RoofTelemetry.CurrentPulseCount;

            lblFault.Text =
                RoofTelemetry.Faulted
                ? "Fault: " +
                  RoofTelemetry.FaultMessage
                : "Fault: None";

            int value =
                Math.Max(
                    0,
                    Math.Min(
                        100,
                        RoofTelemetry.PercentOpen));

            progressRoof.Value = value;
        }
        protected override void OnFormClosing(
    FormClosingEventArgs e)
        {
            if (!allowClose &&
                e.CloseReason ==
                CloseReason.UserClosing)
            {
                e.Cancel = true;

                this.WindowState =
                    FormWindowState.Minimized;

                return;
            }

            timer1.Enabled = false;

            base.OnFormClosing(e);
        }
        private void btnCalibrate_Click(
    object sender,
    EventArgs e)
        {
            try
            {
                MessageBox.Show(
                    "Calibration starting.\n\n" +
                    "Roof will fully open.");

                string result =
                    driver.StartCalibration();

                MessageBox.Show(result);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
