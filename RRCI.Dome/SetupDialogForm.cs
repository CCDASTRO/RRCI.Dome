using System;
using System.IO.Ports;
using System.Windows.Forms;
using ASCOM.Utilities;

public partial class SetupDialogForm : Form
{
    private const string driverId = "RRCI.Dome";


    
    
    //private ASCOM.Utilities.Profile profile;

    
    
    
    
    
    private Button btnOK;
    private Button btnCancel;
    private ComboBox comboPorts;
    private ComboBox comboBaud;
    private CheckBox chkSafeMode;
    private CheckBox chkRainSensor;
    private CheckBox chkAutoClose;
    private TextBox txtTimeout;
    private TextBox txtDeviceId;
    private Button btnTestConnection;
    private TextBox textBox1;
    private Button button1;
    private Label lblStatus;
    
    public SetupDialogForm()
    {
        InitializeComponent();
       // profile = new ASCOM.Utilities.Profile();
        //profile.DeviceType = "Dome";
        comboPorts.Items.AddRange(SerialPort.GetPortNames());
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
            this.txtTimeout = new System.Windows.Forms.TextBox();
            this.txtDeviceId = new System.Windows.Forms.TextBox();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnTestConnection = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(24, 102);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 0;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(105, 102);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // comboPorts
            // 
            this.comboPorts.FormattingEnabled = true;
            this.comboPorts.Location = new System.Drawing.Point(12, 12);
            this.comboPorts.Name = "comboPorts";
            this.comboPorts.Size = new System.Drawing.Size(121, 21);
            this.comboPorts.TabIndex = 3;
            this.comboPorts.SelectedIndexChanged += new System.EventHandler(this.comboPorts_SelectedIndexChanged);
            // 
            // comboBaud
            // 
            this.comboBaud.FormattingEnabled = true;
            this.comboBaud.Location = new System.Drawing.Point(140, 12);
            this.comboBaud.Name = "comboBaud";
            this.comboBaud.Size = new System.Drawing.Size(121, 21);
            this.comboBaud.TabIndex = 4;
            this.comboBaud.SelectedIndexChanged += new System.EventHandler(this.comboBaud_SelectedIndexChanged);
            // 
            // chkSafeMode
            // 
            this.chkSafeMode.AutoSize = true;
            this.chkSafeMode.Location = new System.Drawing.Point(28, 33);
            this.chkSafeMode.Name = "chkSafeMode";
            this.chkSafeMode.Size = new System.Drawing.Size(82, 17);
            this.chkSafeMode.TabIndex = 5;
            this.chkSafeMode.Text = "Scope Safe";
            this.chkSafeMode.UseVisualStyleBackColor = true;
            this.chkSafeMode.CheckedChanged += new System.EventHandler(this.chkSafeMode_CheckedChanged);
            // 
            // chkRainSensor
            // 
            this.chkRainSensor.AutoSize = true;
            this.chkRainSensor.Location = new System.Drawing.Point(28, 56);
            this.chkRainSensor.Name = "chkRainSensor";
            this.chkRainSensor.Size = new System.Drawing.Size(84, 17);
            this.chkRainSensor.TabIndex = 6;
            this.chkRainSensor.Text = "Rain Sensor";
            this.chkRainSensor.UseVisualStyleBackColor = true;
            this.chkRainSensor.CheckedChanged += new System.EventHandler(this.chkRainSensor_CheckedChanged);
            // 
            // chkAutoClose
            // 
            this.chkAutoClose.AutoSize = true;
            this.chkAutoClose.Location = new System.Drawing.Point(166, 41);
            this.chkAutoClose.Name = "chkAutoClose";
            this.chkAutoClose.Size = new System.Drawing.Size(77, 17);
            this.chkAutoClose.TabIndex = 8;
            this.chkAutoClose.Text = "Auto Close";
            this.chkAutoClose.UseVisualStyleBackColor = true;
            this.chkAutoClose.CheckedChanged += new System.EventHandler(this.chkAutoClose_CheckedChanged);
            // 
            // txtTimeout
            // 
            this.txtTimeout.Location = new System.Drawing.Point(161, 131);
            this.txtTimeout.Name = "txtTimeout";
            this.txtTimeout.Size = new System.Drawing.Size(100, 20);
            this.txtTimeout.TabIndex = 10;
            this.txtTimeout.TextChanged += new System.EventHandler(this.txtTimeout_TextChanged);
            // 
            // txtDeviceId
            // 
            this.txtDeviceId.Location = new System.Drawing.Point(33, 131);
            this.txtDeviceId.Name = "txtDeviceId";
            this.txtDeviceId.Size = new System.Drawing.Size(100, 20);
            this.txtDeviceId.TabIndex = 11;
            this.txtDeviceId.TextChanged += new System.EventHandler(this.txtDeviceId_TextChanged);
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(183, 218);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(37, 13);
            this.lblStatus.TabIndex = 12;
            this.lblStatus.Text = "Status";
            // 
            // btnTestConnection
            // 
            this.btnTestConnection.Location = new System.Drawing.Point(197, 102);
            this.btnTestConnection.Name = "btnTestConnection";
            this.btnTestConnection.Size = new System.Drawing.Size(75, 23);
            this.btnTestConnection.TabIndex = 13;
            this.btnTestConnection.Text = "Test";
            this.btnTestConnection.UseVisualStyleBackColor = true;
            this.btnTestConnection.Click += new System.EventHandler(this.btnTestConnection_Click);
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(46, 193);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(100, 20);
            this.textBox1.TabIndex = 14;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(169, 157);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 15;
            this.button1.Text = "button1";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // SetupDialogForm
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.btnTestConnection);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.txtDeviceId);
            this.Controls.Add(this.txtTimeout);
            this.Controls.Add(this.chkAutoClose);
            this.Controls.Add(this.chkRainSensor);
            this.Controls.Add(this.chkSafeMode);
            this.Controls.Add(this.comboBaud);
            this.Controls.Add(this.comboPorts);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Name = "SetupDialogForm";
            this.Load += new System.EventHandler(this.SetupDialogForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

    }



    private void SetupDialogForm_Load(object sender, EventArgs e)
    {
        
        EnableControls(true);
        LoadSettings();
    }
    private bool _isLoading = false;

    private void LoadSettings()
    {
        // 1. Lock the gate so UI events don't trigger SaveSettings()
        _isLoading = true;

        try
        {
            using (var p = new ASCOM.Utilities.Profile())
            {
                p.DeviceType = "Dome";

                // 2. Populate UI - these will trigger events, but _isLoading stops the Save
                comboPorts.Text =p.GetValue(driverId,"COM", "");
                comboBaud.Text = p.GetValue(driverId,"Baud", "9600");
                txtTimeout.Text = p.GetValue(driverId,"Timeout", "5000");
                txtDeviceId.Text = p.GetValue(driverId,"DeviceId", driverId);
                textBox1.Text = driverId;
                chkSafeMode.Checked = ReadBool(p,"SafeMode");
                chkAutoClose.Checked = ReadBool(p,"AutoClose");
                chkRainSensor.Checked = ReadBool(p,"RainSensor");
                
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error loading settings: " + ex.Message);
        }
        finally
        {
            // 3. Unlock the gate so user changes will be saved again
            _isLoading = false;
        }
    }
    private void EnableControls(bool enabled)
    {
        comboPorts.Enabled = enabled;
        comboBaud.Enabled = enabled;

        chkSafeMode.Enabled = enabled;
        chkAutoClose.Enabled = enabled;
        chkRainSensor.Enabled = enabled;
        

        txtTimeout.Enabled = enabled;
        txtDeviceId.Enabled = enabled;

        btnTestConnection.Enabled = enabled;
        btnOK.Enabled = enabled;
    }
    private bool GetBool(Profile p, string key)
    {
        string value = p.GetValue(driverId, key, "False");
        return bool.TryParse(value, out bool result) && result;
    }

    private bool ReadBool(Profile p, string key)
    {
        return p.GetValue(driverId, key, "False")
                 .Equals("True", StringComparison.OrdinalIgnoreCase);
    }
    private void SaveSettings()
    {
        using (var p = new ASCOM.Utilities.Profile())
        {
            p.DeviceType = "Dome";

            p.WriteValue(driverId, "COM", comboPorts.Text);
            p.WriteValue(driverId, "Baud", comboBaud.Text);

            p.WriteValue(driverId, "SafeMode", chkSafeMode.Checked ? "True" : "False");
            p.WriteValue(driverId, "AutoClose", chkAutoClose.Checked ? "True" : "False");
            p.WriteValue(driverId, "RainSensor", chkRainSensor.Checked ? "True" : "False");
            

            p.WriteValue(driverId, "Timeout", txtTimeout.Text);
            p.WriteValue(driverId, "DeviceId", txtDeviceId.Text);
        }
    }


    private void checkBox5_CheckedChanged(object sender, EventArgs e)
    {
        if (_isLoading) return;

        
    }

    private void button3_Click(object sender, EventArgs e)
    {

    }

    private void btnOK_Click(object sender, EventArgs e)
    {
        //MessageBox.Show("Saving settings...");
        if (string.IsNullOrWhiteSpace(comboPorts.Text))
        {
            MessageBox.Show("Please select a COM port.");
            return;
        }
        
        // Save settings
        SaveSettings();

        // Close dialog properly
        this.DialogResult = DialogResult.OK;
        this.Close();
    }

    private void btnTestConnection_Click(object sender, EventArgs e)
    {
        lblStatus.Text = "Testing...";

        try
        {
            if (string.IsNullOrWhiteSpace(comboPorts.Text))
            {
                lblStatus.Text = "Select COM port";
                return;
            }

            int baud = 9600;
            int.TryParse(comboBaud.Text, out baud);

            using (SerialPort sp = new SerialPort(comboPorts.Text, baud))
            {
                sp.ReadTimeout = 2000;
                sp.WriteTimeout = 2000;

                sp.Open();

                // OPTIONAL: replace with your real command
                sp.WriteLine("ping");

                lblStatus.Text = "Connected OK";
            }
        }
        catch (TimeoutException)
        {
            lblStatus.Text = "No response (timeout)";
        }
        catch (Exception ex)
        {
            lblStatus.Text = "Error: " + ex.Message;
        }
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        this.DialogResult = DialogResult.Cancel;
        this.Close();
    }

    private void comboPorts_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_isLoading) return;

        
    }

    private void comboBaud_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_isLoading) return;

        
    }

    private void chkSafeMode_CheckedChanged(object sender, EventArgs e)
    {
        if (_isLoading) return;

        
    }

    private void chkAutoClose_CheckedChanged(object sender, EventArgs e)
    {
        if (_isLoading) return;

        
    }

    private void chkRainSensor_CheckedChanged(object sender, EventArgs e)
    {
        if (_isLoading) return;

        
    }

    private void txtDeviceId_TextChanged(object sender, EventArgs e)
    {
        if (_isLoading) return;

       
    }

    private void txtTimeout_TextChanged(object sender, EventArgs e)
    {
        if (_isLoading) return;

        
    }

    private void button1_Click(object sender, EventArgs e)
    {
        LoadSettings();
    }
}