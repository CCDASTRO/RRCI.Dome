using ASCOM.Utilities;
using RRCI.DomeDriver;
using System;
using System.IO.Ports;
using System.Windows.Forms;
using System.Threading.Tasks;
public partial class SetupDialogForm : Form
{
    private const string driverId = "RRCI.Dome";

    private Button btnOK;
    private Button btnCancel;
    private ComboBox comboPorts;
    private ComboBox comboBaud;
    private CheckBox chkSafeMode;
    private CheckBox chkAutoClose;
    private CheckBox chkTraceLogging;
    private TextBox txtDeviceId;
    private Label label1;
    private CheckBox chkMotionSensor;
    private Label lblOpenPulseCount;
    private TextBox txtOpenPulseCount;
    private GroupBox groupBox1;
    private RadioButton radioOpenCloseStop;
    private RadioButton radioAleko;
    private RadioButton radioOpenClose;
    private GroupBox groupBox2;
    private TextBox txtPushoverUserKey;
    private TextBox txtPushoverToken;
    private Button cmdTestPushover;
    private CheckBox chkNotifyConnectionRestored;
    private CheckBox chkNotifyConnectionLost;
    private CheckBox chkNotifyRoofFault;
    private CheckBox chkNotifyRoofClosed;
    private CheckBox chkNotifyRoofOpened;
    private Label lblPushoverUserKey;
    private Label lblPushoverToken;
    private CheckBox chkEnablePushover;
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
            this.chkAutoClose = new System.Windows.Forms.CheckBox();
            this.chkTraceLogging = new System.Windows.Forms.CheckBox();
            this.txtDeviceId = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.chkMotionSensor = new System.Windows.Forms.CheckBox();
            this.lblOpenPulseCount = new System.Windows.Forms.Label();
            this.txtOpenPulseCount = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.radioOpenClose = new System.Windows.Forms.RadioButton();
            this.radioOpenCloseStop = new System.Windows.Forms.RadioButton();
            this.radioAleko = new System.Windows.Forms.RadioButton();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.txtPushoverUserKey = new System.Windows.Forms.TextBox();
            this.txtPushoverToken = new System.Windows.Forms.TextBox();
            this.cmdTestPushover = new System.Windows.Forms.Button();
            this.chkNotifyConnectionRestored = new System.Windows.Forms.CheckBox();
            this.chkNotifyConnectionLost = new System.Windows.Forms.CheckBox();
            this.chkNotifyRoofFault = new System.Windows.Forms.CheckBox();
            this.chkNotifyRoofClosed = new System.Windows.Forms.CheckBox();
            this.chkNotifyRoofOpened = new System.Windows.Forms.CheckBox();
            this.lblPushoverUserKey = new System.Windows.Forms.Label();
            this.lblPushoverToken = new System.Windows.Forms.Label();
            this.chkEnablePushover = new System.Windows.Forms.CheckBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(39, 221);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 7;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(147, 221);
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
            // chkAutoClose
            // 
            this.chkAutoClose.AutoSize = true;
            this.chkAutoClose.Location = new System.Drawing.Point(12, 68);
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
            this.txtDeviceId.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtDeviceId.Location = new System.Drawing.Point(12, 195);
            this.txtDeviceId.Name = "txtDeviceId";
            this.txtDeviceId.Size = new System.Drawing.Size(268, 20);
            this.txtDeviceId.TabIndex = 6;
            this.txtDeviceId.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
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
            // lblOpenPulseCount
            // 
            this.lblOpenPulseCount.AutoSize = true;
            this.lblOpenPulseCount.Location = new System.Drawing.Point(12, 101);
            this.lblOpenPulseCount.Name = "lblOpenPulseCount";
            this.lblOpenPulseCount.Size = new System.Drawing.Size(93, 13);
            this.lblOpenPulseCount.TabIndex = 11;
            this.lblOpenPulseCount.Text = "Open Pulse Count";
            // 
            // txtOpenPulseCount
            // 
            this.txtOpenPulseCount.Location = new System.Drawing.Point(137, 101);
            this.txtOpenPulseCount.Name = "txtOpenPulseCount";
            this.txtOpenPulseCount.Size = new System.Drawing.Size(100, 20);
            this.txtOpenPulseCount.TabIndex = 12;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.radioOpenClose);
            this.groupBox1.Controls.Add(this.radioOpenCloseStop);
            this.groupBox1.Controls.Add(this.radioAleko);
            this.groupBox1.Location = new System.Drawing.Point(15, 127);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(227, 62);
            this.groupBox1.TabIndex = 13;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Controller Type";
            // 
            // radioOpenClose
            // 
            this.radioOpenClose.AutoSize = true;
            this.radioOpenClose.Location = new System.Drawing.Point(15, 42);
            this.radioOpenClose.Name = "radioOpenClose";
            this.radioOpenClose.Size = new System.Drawing.Size(65, 17);
            this.radioOpenClose.TabIndex = 2;
            this.radioOpenClose.TabStop = true;
            this.radioOpenClose.Text = "2-Button";
            this.radioOpenClose.UseVisualStyleBackColor = true;
            // 
            // radioOpenCloseStop
            // 
            this.radioOpenCloseStop.AutoSize = true;
            this.radioOpenCloseStop.Location = new System.Drawing.Point(112, 19);
            this.radioOpenCloseStop.Name = "radioOpenCloseStop";
            this.radioOpenCloseStop.Size = new System.Drawing.Size(65, 17);
            this.radioOpenCloseStop.TabIndex = 1;
            this.radioOpenCloseStop.TabStop = true;
            this.radioOpenCloseStop.Text = "3-Button";
            this.radioOpenCloseStop.UseVisualStyleBackColor = true;
            // 
            // radioAleko
            // 
            this.radioAleko.AutoSize = true;
            this.radioAleko.Location = new System.Drawing.Point(15, 19);
            this.radioAleko.Name = "radioAleko";
            this.radioAleko.Size = new System.Drawing.Size(65, 17);
            this.radioAleko.TabIndex = 0;
            this.radioAleko.TabStop = true;
            this.radioAleko.Text = "1-Button";
            this.radioAleko.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.txtPushoverUserKey);
            this.groupBox2.Controls.Add(this.txtPushoverToken);
            this.groupBox2.Controls.Add(this.cmdTestPushover);
            this.groupBox2.Controls.Add(this.chkNotifyConnectionRestored);
            this.groupBox2.Controls.Add(this.chkNotifyConnectionLost);
            this.groupBox2.Controls.Add(this.chkNotifyRoofFault);
            this.groupBox2.Controls.Add(this.chkNotifyRoofClosed);
            this.groupBox2.Controls.Add(this.chkNotifyRoofOpened);
            this.groupBox2.Controls.Add(this.lblPushoverUserKey);
            this.groupBox2.Controls.Add(this.lblPushoverToken);
            this.groupBox2.Controls.Add(this.chkEnablePushover);
            this.groupBox2.Location = new System.Drawing.Point(12, 267);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(268, 218);
            this.groupBox2.TabIndex = 14;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Pushover";
            // 
            // txtPushoverUserKey
            // 
            this.txtPushoverUserKey.Location = new System.Drawing.Point(71, 80);
            this.txtPushoverUserKey.Name = "txtPushoverUserKey";
            this.txtPushoverUserKey.Size = new System.Drawing.Size(188, 20);
            this.txtPushoverUserKey.TabIndex = 10;
            // 
            // txtPushoverToken
            // 
            this.txtPushoverToken.Location = new System.Drawing.Point(71, 50);
            this.txtPushoverToken.Name = "txtPushoverToken";
            this.txtPushoverToken.Size = new System.Drawing.Size(188, 20);
            this.txtPushoverToken.TabIndex = 9;
            // 
            // cmdTestPushover
            // 
            this.cmdTestPushover.Location = new System.Drawing.Point(92, 179);
            this.cmdTestPushover.Name = "cmdTestPushover";
            this.cmdTestPushover.Size = new System.Drawing.Size(102, 23);
            this.cmdTestPushover.TabIndex = 8;
            this.cmdTestPushover.Text = "Test Notification";
            this.cmdTestPushover.UseVisualStyleBackColor = true;
            this.cmdTestPushover.Click += new System.EventHandler(this.cmdTestPushover_Click);
            // 
            // chkNotifyConnectionRestored
            // 
            this.chkNotifyConnectionRestored.AutoSize = true;
            this.chkNotifyConnectionRestored.Location = new System.Drawing.Point(15, 156);
            this.chkNotifyConnectionRestored.Name = "chkNotifyConnectionRestored";
            this.chkNotifyConnectionRestored.Size = new System.Drawing.Size(156, 17);
            this.chkNotifyConnectionRestored.TabIndex = 7;
            this.chkNotifyConnectionRestored.Text = "Notify Connection Restored";
            this.chkNotifyConnectionRestored.UseVisualStyleBackColor = true;
            // 
            // chkNotifyConnectionLost
            // 
            this.chkNotifyConnectionLost.AutoSize = true;
            this.chkNotifyConnectionLost.Location = new System.Drawing.Point(15, 133);
            this.chkNotifyConnectionLost.Name = "chkNotifyConnectionLost";
            this.chkNotifyConnectionLost.Size = new System.Drawing.Size(133, 17);
            this.chkNotifyConnectionLost.TabIndex = 6;
            this.chkNotifyConnectionLost.Text = "Notify Connection Lost";
            this.chkNotifyConnectionLost.UseVisualStyleBackColor = true;
            // 
            // chkNotifyRoofFault
            // 
            this.chkNotifyRoofFault.AutoSize = true;
            this.chkNotifyRoofFault.Location = new System.Drawing.Point(154, 133);
            this.chkNotifyRoofFault.Name = "chkNotifyRoofFault";
            this.chkNotifyRoofFault.Size = new System.Drawing.Size(105, 17);
            this.chkNotifyRoofFault.TabIndex = 5;
            this.chkNotifyRoofFault.Text = "Notify Roof Fault";
            this.chkNotifyRoofFault.UseVisualStyleBackColor = true;
            // 
            // chkNotifyRoofClosed
            // 
            this.chkNotifyRoofClosed.AutoSize = true;
            this.chkNotifyRoofClosed.Location = new System.Drawing.Point(154, 110);
            this.chkNotifyRoofClosed.Name = "chkNotifyRoofClosed";
            this.chkNotifyRoofClosed.Size = new System.Drawing.Size(114, 17);
            this.chkNotifyRoofClosed.TabIndex = 4;
            this.chkNotifyRoofClosed.Text = "Notify Roof Closed";
            this.chkNotifyRoofClosed.UseVisualStyleBackColor = true;
            // 
            // chkNotifyRoofOpened
            // 
            this.chkNotifyRoofOpened.AutoSize = true;
            this.chkNotifyRoofOpened.Location = new System.Drawing.Point(15, 110);
            this.chkNotifyRoofOpened.Name = "chkNotifyRoofOpened";
            this.chkNotifyRoofOpened.Size = new System.Drawing.Size(120, 17);
            this.chkNotifyRoofOpened.TabIndex = 3;
            this.chkNotifyRoofOpened.Text = "Notify Roof Opened";
            this.chkNotifyRoofOpened.UseVisualStyleBackColor = true;
            // 
            // lblPushoverUserKey
            // 
            this.lblPushoverUserKey.AutoSize = true;
            this.lblPushoverUserKey.Location = new System.Drawing.Point(12, 83);
            this.lblPushoverUserKey.Name = "lblPushoverUserKey";
            this.lblPushoverUserKey.Size = new System.Drawing.Size(50, 13);
            this.lblPushoverUserKey.TabIndex = 2;
            this.lblPushoverUserKey.Text = "User Key";
            // 
            // lblPushoverToken
            // 
            this.lblPushoverToken.AutoSize = true;
            this.lblPushoverToken.Location = new System.Drawing.Point(12, 53);
            this.lblPushoverToken.Name = "lblPushoverToken";
            this.lblPushoverToken.Size = new System.Drawing.Size(38, 13);
            this.lblPushoverToken.TabIndex = 1;
            this.lblPushoverToken.Text = "Token";
            // 
            // chkEnablePushover
            // 
            this.chkEnablePushover.AutoSize = true;
            this.chkEnablePushover.Location = new System.Drawing.Point(12, 19);
            this.chkEnablePushover.Name = "chkEnablePushover";
            this.chkEnablePushover.Size = new System.Drawing.Size(168, 17);
            this.chkEnablePushover.TabIndex = 0;
            this.chkEnablePushover.Text = "Enable Pushover Notifications";
            this.chkEnablePushover.UseVisualStyleBackColor = true;
            // 
            // SetupDialogForm
            // 
            this.ClientSize = new System.Drawing.Size(292, 493);
            this.ControlBox = false;
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.txtOpenPulseCount);
            this.Controls.Add(this.lblOpenPulseCount);
            this.Controls.Add(this.chkMotionSensor);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.comboPorts);
            this.Controls.Add(this.comboBaud);
            this.Controls.Add(this.chkSafeMode);
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
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
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
            
            string controllerType = profile.GetValue(driverId, "ControllerType", "", "1");

            switch (controllerType)
            {
                case "1":
                    radioAleko.Checked = true;
                    break;

                case "2":
                    radioOpenClose.Checked = true;
                    break;

                case "3":
                    radioOpenCloseStop.Checked = true;
                    break;
            }
            comboPorts.Text = profile.GetValue(driverId, "COM", "", "");
            comboBaud.Text = profile.GetValue(driverId, "Baud", "", "9600");
            txtDeviceId.Text = profile.GetValue(driverId, "DeviceId", "", driverId);

            chkSafeMode.Checked = ReadBool(profile, "SafeMode");
            chkAutoClose.Checked = ReadBool(profile, "AutoClose");

            // New motion sensor option
            chkMotionSensor.Checked = ReadBool(profile, "MotionSensor");
            txtOpenPulseCount.Text = profile.GetValue(driverId,"OpenPulseCount","","5000");

            // Existing trace logging checkbox
            chkTraceLogging.Checked = profile.GetValue(
                driverId,
                "TraceLogger",
                "",
                "False"
            ).Equals("True", StringComparison.OrdinalIgnoreCase);
            chkEnablePushover.Checked =
                ReadBool(profile, "EnablePushover");

            txtPushoverToken.Text =
                profile.GetValue(
                    driverId,
                    "PushoverToken",
                    "",
                    "");

            txtPushoverUserKey.Text =
                profile.GetValue(
                    driverId,
                    "PushoverUserKey",
                    "",
                    "");

            chkNotifyRoofOpened.Checked =
                ReadBool(profile, "NotifyRoofOpened");

            chkNotifyRoofClosed.Checked =
                ReadBool(profile, "NotifyRoofClosed");

            chkNotifyRoofFault.Checked =
                ReadBool(profile, "NotifyRoofFault");

            chkNotifyConnectionLost.Checked =
                ReadBool(profile, "NotifyConnectionLost");

            chkNotifyConnectionRestored.Checked =
                ReadBool(profile, "NotifyConnectionRestored");
        }
    }

    private void SaveSettings()
    {
        using (Profile profile = new Profile())
        {
                       
            profile.DeviceType = "Dome";

            string controllerType = "1";

            if (radioOpenClose.Checked)
                controllerType = "2";

            if (radioOpenCloseStop.Checked)
                controllerType = "3";

            profile.WriteValue(driverId, "ControllerType", controllerType);

            profile.WriteValue(driverId, "COM", comboPorts.Text);
            profile.WriteValue(driverId, "Baud", comboBaud.Text);
            profile.WriteValue(driverId, "DeviceId", txtDeviceId.Text);

            profile.WriteValue(driverId, "SafeMode",
                chkSafeMode.Checked ? "True" : "False");
            profile.WriteValue(driverId,"OpenPulseCount",txtOpenPulseCount.Text);
            profile.WriteValue(driverId, "AutoClose",
                chkAutoClose.Checked ? "True" : "False");

            // New motion sensor option
            profile.WriteValue(driverId, "MotionSensor",
                chkMotionSensor.Checked ? "True" : "False");

            // Existing trace logging option
            profile.WriteValue(driverId, "TraceLogger",
                chkTraceLogging.Checked ? "True" : "False");
            // Pushover settings
            profile.WriteValue(
                driverId,
                "EnablePushover",
                chkEnablePushover.Checked
                ? "True"
                : "False");

            profile.WriteValue(
                driverId,
                "PushoverToken",
                txtPushoverToken.Text.Trim());

            profile.WriteValue(
                driverId,
                "PushoverUserKey",
                txtPushoverUserKey.Text.Trim());

            profile.WriteValue(
                driverId,
                "NotifyRoofOpened",
                chkNotifyRoofOpened.Checked
                    ? "True"
                    : "False");

            profile.WriteValue(
                driverId,
                "NotifyRoofClosed",
                chkNotifyRoofClosed.Checked
                    ? "True"
                    : "False");

            profile.WriteValue(
                driverId,
                "NotifyRoofFault",
                chkNotifyRoofFault.Checked
                    ? "True"
                    : "False");

            profile.WriteValue(
                driverId,
                "NotifyConnectionLost",
                chkNotifyConnectionLost.Checked
                    ? "True"
                    : "False");

            profile.WriteValue(
                driverId,
                "NotifyConnectionRestored",
                chkNotifyConnectionRestored.Checked
                    ? "True"
                    : "False");
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

    private void chkRainSensor_CheckedChanged(object sender, EventArgs e)
    {

    }

    private async void cmdTestPushover_Click(
    object sender,
    EventArgs e)
    {
        bool success =
            await PushoverNotifier.SendAsync(
                txtPushoverToken.Text.Trim(),
                txtPushoverUserKey.Text.Trim(),
                "RRCI Test Notification");

        MessageBox.Show(
            success
                ? "Notification sent successfully."
                : "Notification failed.");
    }
}