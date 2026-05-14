using ASCOM.Utilities;
using System;
using System.IO.Ports;
using System.Windows.Forms;

public partial class SetupDialogForm : Form
{
    private const string driverId = "RRCI.Dome";

    private Button btnOK;
    private Button btnCancel;
    private ComboBox comboPorts;
    private ComboBox comboBaud;
    private CheckBox chkSafeMode;
    private CheckBox chkRainSensor;
    private CheckBox chkAutoClose;
    private CheckBox chkTraceLogging;
    private TextBox txtDeviceId;
    private Label label1;
    private CheckBox chkMotionSensor;
    private bool _isLoading = false;

    public SetupDialogForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.comboPorts = new System.Windows.Forms.ComboBox();
            this.comboBaud = new System.Windows.Forms.ComboBox();
            this.chkSafeMode = new System.Windows.Forms.CheckBox();
            this.chkRainSensor = new System.Windows.Forms.CheckBox();
            this.chkAutoClose = new System.Windows.Forms.CheckBox();
            this.chkTraceLogging = new System.Windows.Forms.CheckBox();
            this.txtDeviceId = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.chkMotionSensor = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(98, 120);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 7;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(179, 120);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 8;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // comboPorts
            // 
            this.comboPorts.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboPorts.Location = new System.Drawing.Point(12, 12);
            this.comboPorts.Name = "comboPorts";
            this.comboPorts.Size = new System.Drawing.Size(82, 21);
            this.comboPorts.TabIndex = 0;
            // 
            // comboBaud
            // 
            this.comboBaud.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBaud.Location = new System.Drawing.Point(166, 12);
            this.comboBaud.Name = "comboBaud";
            this.comboBaud.Size = new System.Drawing.Size(76, 21);
            this.comboBaud.TabIndex = 1;
            // 
            // chkSafeMode
            // 
            this.chkSafeMode.AutoSize = true;
            this.chkSafeMode.Location = new System.Drawing.Point(12, 45);
            this.chkSafeMode.Name = "chkSafeMode";
            this.chkSafeMode.Size = new System.Drawing.Size(82, 17);
            this.chkSafeMode.TabIndex = 2;
            this.chkSafeMode.Text = "Scope Safe";
            this.chkSafeMode.UseVisualStyleBackColor = true;
            // 
            // chkRainSensor
            // 
            this.chkRainSensor.AutoSize = true;
            this.chkRainSensor.Location = new System.Drawing.Point(12, 70);
            this.chkRainSensor.Name = "chkRainSensor";
            this.chkRainSensor.Size = new System.Drawing.Size(84, 17);
            this.chkRainSensor.TabIndex = 3;
            this.chkRainSensor.Text = "Rain Sensor";
            this.chkRainSensor.UseVisualStyleBackColor = true;
            // 
            // chkAutoClose
            // 
            this.chkAutoClose.AutoSize = true;
            this.chkAutoClose.Location = new System.Drawing.Point(12, 95);
            this.chkAutoClose.Name = "chkAutoClose";
            this.chkAutoClose.Size = new System.Drawing.Size(77, 17);
            this.chkAutoClose.TabIndex = 4;
            this.chkAutoClose.Text = "Auto Close";
            this.chkAutoClose.UseVisualStyleBackColor = true;
            // 
            // chkTraceLogging
            // 
            this.chkTraceLogging.AutoSize = true;
            this.chkTraceLogging.Location = new System.Drawing.Point(137, 45);
            this.chkTraceLogging.Name = "chkTraceLogging";
            this.chkTraceLogging.Size = new System.Drawing.Size(95, 17);
            this.chkTraceLogging.TabIndex = 5;
            this.chkTraceLogging.Text = "Trace Logging";
            this.chkTraceLogging.UseVisualStyleBackColor = true;
            // 
            // txtDeviceId
            // 
            this.txtDeviceId.Location = new System.Drawing.Point(113, 92);
            this.txtDeviceId.Name = "txtDeviceId";
            this.txtDeviceId.Size = new System.Drawing.Size(141, 20);
            this.txtDeviceId.TabIndex = 6;
            this.txtDeviceId.TextChanged += new System.EventHandler(this.txtDeviceId_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(95, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(69, 13);
            this.label1.TabIndex = 9;
            this.label1.Text = "Port      Baud";
            // 
            // chkMotionSensor
            // 
            this.chkMotionSensor.AutoSize = true;
            this.chkMotionSensor.Location = new System.Drawing.Point(137, 68);
            this.chkMotionSensor.Name = "chkMotionSensor";
            this.chkMotionSensor.Size = new System.Drawing.Size(120, 17);
            this.chkMotionSensor.TabIndex = 10;
            this.chkMotionSensor.Text = "Roof Motion Sensor";
            this.chkMotionSensor.UseVisualStyleBackColor = true;
            // 
            // SetupDialogForm
            // 
            this.ClientSize = new System.Drawing.Size(266, 147);
            this.Controls.Add(this.chkMotionSensor);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.comboPorts);
            this.Controls.Add(this.comboBaud);
            this.Controls.Add(this.chkSafeMode);
            this.Controls.Add(this.chkRainSensor);
            this.Controls.Add(this.chkAutoClose);
            this.Controls.Add(this.chkTraceLogging);
            this.Controls.Add(this.txtDeviceId);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.Name = "SetupDialogForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "RRCI Dome Setup";
            this.Load += new System.EventHandler(this.SetupDialogForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

    }

    private void SetupDialogForm_Load(object sender, EventArgs e)
    {
        _isLoading = true;

        try
        {
            comboPorts.Items.Clear();
            comboPorts.Items.AddRange(SerialPort.GetPortNames());

            comboBaud.Items.Clear();
            comboBaud.Items.AddRange(new object[]
            {
                "9600",
                "19200",
                "38400",
                "57600",
                "115200"
            });

            LoadSettings();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void LoadSettings()
    {
        using (Profile profile = new Profile())
        {
            profile.DeviceType = "Dome";

            comboPorts.Text = profile.GetValue(driverId, "COM", "", "");
            comboBaud.Text = profile.GetValue(driverId, "Baud", "", "9600");
            txtDeviceId.Text = profile.GetValue(driverId, "DeviceId", "", driverId);

            chkSafeMode.Checked = ReadBool(profile, "SafeMode");
            chkRainSensor.Checked = ReadBool(profile, "RainSensor");
            chkAutoClose.Checked = ReadBool(profile, "AutoClose");

            // New motion sensor option
            chkMotionSensor.Checked = ReadBool(profile, "MotionSensor");

            // Existing trace logging checkbox
            chkTraceLogging.Checked = profile.GetValue(
                driverId,
                "TraceLogger",
                "",
                "False"
            ).Equals("True", StringComparison.OrdinalIgnoreCase);
        }
    }

    private void SaveSettings()
    {
        using (Profile profile = new Profile())
        {
            profile.DeviceType = "Dome";

            profile.WriteValue(driverId, "COM", comboPorts.Text);
            profile.WriteValue(driverId, "Baud", comboBaud.Text);
            profile.WriteValue(driverId, "DeviceId", txtDeviceId.Text);

            profile.WriteValue(driverId, "SafeMode",
                chkSafeMode.Checked ? "True" : "False");

            profile.WriteValue(driverId, "RainSensor",
                chkRainSensor.Checked ? "True" : "False");

            profile.WriteValue(driverId, "AutoClose",
                chkAutoClose.Checked ? "True" : "False");

            // New motion sensor option
            profile.WriteValue(driverId, "MotionSensor",
                chkMotionSensor.Checked ? "True" : "False");

            // Existing trace logging option
            profile.WriteValue(driverId, "TraceLogger",
                chkTraceLogging.Checked ? "True" : "False");
        }
    }

    private bool ReadBool(Profile profile, string key)
    {
        string value = profile.GetValue(driverId, key, "", "False");

        return value.Equals("True", StringComparison.OrdinalIgnoreCase)
            || value.Equals("1", StringComparison.OrdinalIgnoreCase);
    }

    private void btnOK_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(comboPorts.Text))
        {
            MessageBox.Show("Please select a COM port.");
            return;
        }

        SaveSettings();

        this.DialogResult = DialogResult.OK;
        this.Close();
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        this.DialogResult = DialogResult.Cancel;
        this.Close();
    }

    private void txtDeviceId_TextChanged(object sender, EventArgs e)
    {

    }
}