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

    private bool _isLoading = false;

    public SetupDialogForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        btnOK = new Button();
        btnCancel = new Button();
        comboPorts = new ComboBox();
        comboBaud = new ComboBox();
        chkSafeMode = new CheckBox();
        chkRainSensor = new CheckBox();
        chkAutoClose = new CheckBox();
        chkTraceLogging = new CheckBox();
        txtDeviceId = new TextBox();

        this.SuspendLayout();

        comboPorts.Location = new System.Drawing.Point(12, 12);
        comboPorts.Size = new System.Drawing.Size(80, 21);

        comboBaud.Location = new System.Drawing.Point(170, 12);
        comboBaud.Size = new System.Drawing.Size(80, 21);

        chkSafeMode.Location = new System.Drawing.Point(12, 45);
        chkSafeMode.Text = "Scope Safe";
        chkSafeMode.AutoSize = true;

        chkRainSensor.Location = new System.Drawing.Point(12, 70);
        chkRainSensor.Text = "Rain Sensor";
        chkRainSensor.AutoSize = true;

        chkAutoClose.Location = new System.Drawing.Point(12, 95);
        chkAutoClose.Text = "Auto Close";
        chkAutoClose.AutoSize = true;

        chkTraceLogging.Location = new System.Drawing.Point(120, 45);
        chkTraceLogging.Text = "Trace Logging";
        chkTraceLogging.AutoSize = true;

        txtDeviceId.Location = new System.Drawing.Point(120, 95);
        txtDeviceId.Size = new System.Drawing.Size(130, 20);

        btnOK.Location = new System.Drawing.Point(175, 130);
        btnOK.Text = "OK";
        btnOK.Click += btnOK_Click;

        btnCancel.Location = new System.Drawing.Point(175, 160);
        btnCancel.Text = "Cancel";
        btnCancel.Click += btnCancel_Click;

        this.ClientSize = new System.Drawing.Size(270, 200);
        this.Controls.Add(comboPorts);
        this.Controls.Add(comboBaud);
        this.Controls.Add(chkSafeMode);
        this.Controls.Add(chkRainSensor);
        this.Controls.Add(chkAutoClose);
        this.Controls.Add(chkTraceLogging);
        this.Controls.Add(txtDeviceId);
        this.Controls.Add(btnOK);
        this.Controls.Add(btnCancel);

        this.Text = "RRCI Setup";
        this.StartPosition = FormStartPosition.CenterParent;
        this.Load += SetupDialogForm_Load;

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

            // CORRECT KEY
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

            // CORRECT KEY
            profile.WriteValue(
                driverId,
                "TraceLogger",
                chkTraceLogging.Checked ? "True" : "False"
            );
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

        DialogResult = DialogResult.OK;
        Close();
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }
}
