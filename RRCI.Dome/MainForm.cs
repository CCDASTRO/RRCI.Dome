using ASCOM.DeviceInterface;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace RoofDomeUI
{
    public partial class MainForm : Form
    {
        private readonly IDomeV2 driver;
        private Button btnConnect;
        private Label lblConnected;
        private Label lblSafeState;
        private Label lblShutterState;
        private TextBox txtLog;
        private Button btnSetup;
        private Button btnOpen;
        private Button btnClose;
        private Button btnAbort;
        private Timer timer1;

        // =========================
        // CONSTRUCTOR
        // =========================
        public MainForm(IDomeV2 domeDriver)
        {
            driver = domeDriver;

            InitializeComponent();   // <-- Designer ONLY

            InitTimer();
        }

        // =========================
        // TIMER SETUP (UI ONLY)
        // =========================
        private void InitTimer()
        {
            timer1.Interval = 500;
            timer1.Tick += Timer1_Tick;
            timer1.Enabled = false;
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                lblConnected.Text = driver.Connected ? "Connected" : "Disconnected";
                lblConnected.ForeColor = driver.Connected ? Color.Green : Color.Red;

                lblShutterState.Text = "Shutter: " + driver.ShutterStatus.ToString();

                // Placeholder safety indicator (extend later with real interlocks)
                lblSafeState.Text = "Safe: OK";
            }
            catch (Exception ex)
            {
                Log("Timer error: " + ex.Message);
            }
        }

        // =========================
        // CONNECTION
        // =========================
        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                driver.Connected = !driver.Connected;

                btnConnect.Text = driver.Connected ? "Disconnect" : "Connect";

                if (driver.Connected)
                    timer1.Start();
                else
                    timer1.Stop();

                Log("Connection toggled");
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
        }

        // =========================
        // SETUP DIALOG
        // =========================
        private void btnSetup_Click(object sender, EventArgs e)
        {
            try
            {
                driver.SetupDialog();
                Log("Setup opened");
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
        }

        // =========================
        // ROOF CONTROL
        // =========================
        private void btnOpen_Click(object sender, EventArgs e)
        {
            try
            {
                driver.OpenShutter();
                Log("OPEN sent");
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            try
            {
                driver.CloseShutter();
                Log("CLOSE sent");
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
        }

        private void btnAbort_Click(object sender, EventArgs e)
        {
            try
            {
                driver.AbortSlew();
                Log("ABORT sent");
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
        }

        // =========================
        // LOGGING
        // =========================
        private void Log(string msg)
        {
            txtLog.AppendText(
                $"{DateTime.Now:HH:mm:ss} - {msg}{Environment.NewLine}"
            );
        }

        private void InitializeComponent()
        {
            this.btnConnect = new System.Windows.Forms.Button();
            this.lblConnected = new System.Windows.Forms.Label();
            this.lblSafeState = new System.Windows.Forms.Label();
            this.lblShutterState = new System.Windows.Forms.Label();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.btnSetup = new System.Windows.Forms.Button();
            this.btnOpen = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.btnAbort = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnConnect
            // 
            this.btnConnect.Location = new System.Drawing.Point(34, 5);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(75, 23);
            this.btnConnect.TabIndex = 0;
            this.btnConnect.Text = "Connect";
            this.btnConnect.UseVisualStyleBackColor = true;
            // 
            // lblConnected
            // 
            this.lblConnected.AutoSize = true;
            this.lblConnected.Location = new System.Drawing.Point(151, 10);
            this.lblConnected.Name = "lblConnected";
            this.lblConnected.Size = new System.Drawing.Size(65, 13);
            this.lblConnected.TabIndex = 1;
            this.lblConnected.Text = "Connected?";
            // 
            // lblSafeState
            // 
            this.lblSafeState.AutoSize = true;
            this.lblSafeState.Location = new System.Drawing.Point(40, 44);
            this.lblSafeState.Name = "lblSafeState";
            this.lblSafeState.Size = new System.Drawing.Size(69, 13);
            this.lblSafeState.TabIndex = 2;
            this.lblSafeState.Text = "Scope Safe?";
            // 
            // lblShutterState
            // 
            this.lblShutterState.AutoSize = true;
            this.lblShutterState.Location = new System.Drawing.Point(151, 44);
            this.lblShutterState.Name = "lblShutterState";
            this.lblShutterState.Size = new System.Drawing.Size(75, 13);
            this.lblShutterState.TabIndex = 3;
            this.lblShutterState.Text = "Shutter State?";
            // 
            // txtLog
            // 
            this.txtLog.Location = new System.Drawing.Point(9, 113);
            this.txtLog.Name = "txtLog";
            this.txtLog.Size = new System.Drawing.Size(240, 20);
            this.txtLog.TabIndex = 4;
            // 
            // btnSetup
            // 
            this.btnSetup.Location = new System.Drawing.Point(93, 139);
            this.btnSetup.Name = "btnSetup";
            this.btnSetup.Size = new System.Drawing.Size(75, 23);
            this.btnSetup.TabIndex = 5;
            this.btnSetup.Text = "Setup";
            this.btnSetup.UseVisualStyleBackColor = true;
            // 
            // btnOpen
            // 
            this.btnOpen.Location = new System.Drawing.Point(12, 84);
            this.btnOpen.Name = "btnOpen";
            this.btnOpen.Size = new System.Drawing.Size(75, 23);
            this.btnOpen.TabIndex = 6;
            this.btnOpen.Text = "Open";
            this.btnOpen.UseVisualStyleBackColor = true;
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(93, 84);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 23);
            this.btnClose.TabIndex = 7;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.button2_Click);
            // 
            // btnAbort
            // 
            this.btnAbort.Location = new System.Drawing.Point(174, 84);
            this.btnAbort.Name = "btnAbort";
            this.btnAbort.Size = new System.Drawing.Size(75, 23);
            this.btnAbort.TabIndex = 8;
            this.btnAbort.Text = "Abort";
            this.btnAbort.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(264, 176);
            this.Controls.Add(this.btnAbort);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnOpen);
            this.Controls.Add(this.btnSetup);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.lblShutterState);
            this.Controls.Add(this.lblSafeState);
            this.Controls.Add(this.lblConnected);
            this.Controls.Add(this.btnConnect);
            this.Name = "MainForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void button2_Click(object sender, EventArgs e)
        {

        }
    }
}