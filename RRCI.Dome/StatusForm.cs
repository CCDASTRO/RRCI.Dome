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
        public StatusForm()
        {
            InitializeComponent();

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

        private void timer1_Tick(
    object sender,
    EventArgs e)
        {
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
    }
}
