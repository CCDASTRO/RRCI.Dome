using ASCOM.Utilities;
using Microsoft.Win32;
using System;
using System.IO.Ports;
using System.Runtime;
using System.Windows.Forms;

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
    private TextBox txtDeviceId;
    private Button btnTestConnection;
    private Button button1;
    private Label label1;
    private Label lblStatus;
    private bool _isLoading = false;
    public SetupDialogForm()
    {
        InitializeComponent();
        // profile = new ASCOM.Utilities.Profile();
        //profile.DeviceType = "Dome";
        
        //comboPorts.Items.AddRange(SerialPort.GetPortNames());
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
            this.txtDeviceId = new System.Windows.Forms.TextBox();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnTestConnection = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(179, 39);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 0;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(179, 68);
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
            this.comboPorts.Location = new System.Drawing.Point(13, 12);
            this.comboPorts.Name = "comboPorts";
            this.comboPorts.Size = new System.Drawing.Size(75, 21);
            this.comboPorts.TabIndex = 3;
            this.comboPorts.SelectedIndexChanged += new System.EventHandler(this.comboPorts_SelectedIndexChanged);
            // 
            // comboBaud
            // 
            this.comboBaud.FormattingEnabled = true;
            this.comboBaud.Location = new System.Drawing.Point(179, 12);
            this.comboBaud.Name = "comboBaud";
            this.comboBaud.Size = new System.Drawing.Size(75, 21);
            this.comboBaud.TabIndex = 4;
            this.comboBaud.SelectedIndexChanged += new System.EventHandler(this.comboBaud_SelectedIndexChanged);
            // 
            // chkSafeMode
            // 
            this.chkSafeMode.AutoSize = true;
            this.chkSafeMode.Location = new System.Drawing.Point(12, 51);
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
            this.chkRainSensor.Location = new System.Drawing.Point(13, 74);
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
            this.chkAutoClose.Location = new System.Drawing.Point(13, 97);
            this.chkAutoClose.Name = "chkAutoClose";
            this.chkAutoClose.Size = new System.Drawing.Size(77, 17);
            this.chkAutoClose.TabIndex = 8;
            this.chkAutoClose.Text = "Auto Close";
            this.chkAutoClose.UseVisualStyleBackColor = true;
            this.chkAutoClose.CheckedChanged += new System.EventHandler(this.chkAutoClose_CheckedChanged);
            // 
            // txtDeviceId
            // 
            this.txtDeviceId.Location = new System.Drawing.Point(13, 126);
            this.txtDeviceId.Name = "txtDeviceId";
            this.txtDeviceId.Size = new System.Drawing.Size(121, 20);
            this.txtDeviceId.TabIndex = 11;
            this.txtDeviceId.TextChanged += new System.EventHandler(this.txtDeviceId_TextChanged);
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(112, 168);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(37, 13);
            this.lblStatus.TabIndex = 12;
            this.lblStatus.Text = "Status";
            // 
            // btnTestConnection
            // 
            this.btnTestConnection.Location = new System.Drawing.Point(179, 97);
            this.btnTestConnection.Name = "btnTestConnection";
            this.btnTestConnection.Size = new System.Drawing.Size(75, 23);
            this.btnTestConnection.TabIndex = 13;
            this.btnTestConnection.Text = "Test";
            this.btnTestConnection.UseVisualStyleBackColor = true;
            this.btnTestConnection.Click += new System.EventHandler(this.btnTestConnection_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(179, 126);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 15;
            this.button1.Text = "Fetch";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(95, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(78, 13);
            this.label1.TabIndex = 16;
            this.label1.Text = "Port    -    Baud";
            // 
            // SetupDialogForm
            // 
            this.ClientSize = new System.Drawing.Size(268, 190);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.btnTestConnection);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.txtDeviceId);
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
        _isLoading = true;

        try
        {
            // 1. Populate COM ports FIRST
            comboPorts.Items.Clear();
            comboPorts.Items.AddRange(System.IO.Ports.SerialPort.GetPortNames());

            // 2. Populate baud rates
            comboBaud.Items.Clear();
            comboBaud.Items.AddRange(new object[]
            {
            "9600", "19200", "38400", "57600", "115200"
            });

            // 3. THEN load registry
            LoadSettings2();
        }
        finally
        {
            //LoadSettings2();
            _isLoading = false;
        }
    }
    

   
    
    private void SetComboValue(ComboBox combo, string value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        if (combo.Items.Contains(value))
            combo.SelectedItem = value;
        else
            combo.Text = value; // fallback if not in list
    }
    private void LoadSettings()
    {
        _isLoading = true;

        try
        {
            using (var p = new ASCOM.Utilities.Profile())
            {
                p.DeviceType = "Dome";

                string port = p.GetValue(driverId, "COM", "");
                string baud = p.GetValue(driverId, "Baud", "9600");

                // Populate FIRST
                comboPorts.Items.Clear();
                comboPorts.Items.AddRange(SerialPort.GetPortNames());

                comboBaud.Items.Clear();
                comboBaud.Items.AddRange(new object[] { "9600", "19200", "38400", "57600", "115200" });

                // THEN apply values
                SetComboValue(comboPorts, port);
                SetComboValue(comboBaud, baud);

                //txtTimeout.Text = p.GetValue(driverId, "Timeout", "5000");
                txtDeviceId.Text = p.GetValue(driverId, "DeviceId", driverId);

                chkSafeMode.Checked = ReadBool(p, "SafeMode");
                chkAutoClose.Checked = ReadBool(p, "AutoClose");
                chkRainSensor.Checked = ReadBool(p, "RainSensor");
            }
        }
        finally
        {
            _isLoading = false;
        }
    }
    private void LoadSettings2()
    {
        _isLoading = true;
        try
        {
            string keyPath = @"SOFTWARE\WOW6432Node\ASCOM\Dome Drivers\RRCI.Dome";

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(keyPath))

            {
                if (key == null)
                {
                    MessageBox.Show("Key not found in HKLM");
                    return;
                }
                //MessageBox.Show("Load: ");
                string baud = key.GetValue("Baud")?.ToString();
                string com = key.GetValue("COM")?.ToString();
                string id = key.GetValue("Description")?.ToString();
                string timeout = key.GetValue("Timeout")?.ToString();
                string safe = key.GetValue("SafeMode")?.ToString();
                string auto = key.GetValue("AutoClose")?.ToString();
                string rain = key.GetValue("RainSensor")?.ToString();

                chkSafeMode.Checked = safe != null && (safe.Equals("True", StringComparison.OrdinalIgnoreCase) || safe.Equals("1", StringComparison.OrdinalIgnoreCase));
                chkRainSensor.Checked = rain != null && (rain.Equals("True", StringComparison.OrdinalIgnoreCase) || rain.Equals("1", StringComparison.OrdinalIgnoreCase));
                chkAutoClose.Checked = auto != null && (auto.Equals("True", StringComparison.OrdinalIgnoreCase) || auto.Equals("1", StringComparison.OrdinalIgnoreCase));
                comboBaud.Text = baud;
                txtDeviceId.Text = id;
                if (!string.IsNullOrWhiteSpace(com))
                {
                    if (comboPorts.Items.Contains(com))
                        comboPorts.SelectedItem = com;
                    else
                        comboPorts.Text = com; // fallback only if not in list
                }

                if (!string.IsNullOrWhiteSpace(baud))
                {
                    if (comboBaud.Items.Contains(baud))
                        comboBaud.SelectedItem = baud;
                    else
                        comboBaud.Text = baud;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error loading settings: " + ex.Message);
        }
        finally
        {
            _isLoading = false;
        }
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


            //p.WriteValue(driverId, "Timeout", txtTimeout.Text);
            p.WriteValue(driverId, "DeviceId", txtDeviceId.Text);
            //MessageBox.Show("Profile sub-key: " + driverId);
            //string regPath = $@"HKEY_CURRENT_USER\Software\ASCOM\{p.DeviceType}\{driverId}";
            //MessageBox.Show("ASCOM Profile path:\n" + regPath);
            //string key = $@"Software\ASCOM\Dome\{driverId}";
            //MessageBox.Show("About to write to HKCU:\n" + key);

            //p.WriteValue(driverId, "TestWrite", DateTime.Now.ToString());
        }
    }


    
    
    private bool ReadBool(Profile p, string key)
    {
        string val = p.GetValue(driverId, key, "False");
        return val.Equals("True", StringComparison.OrdinalIgnoreCase) ||
               val.Equals("1", StringComparison.OrdinalIgnoreCase);
    }
    


    private void checkBox5_CheckedChanged(object sender, EventArgs e)
    {
        if (_isLoading) return;

        
    }

    private void button3_Click(object sender, EventArgs e)
    {
        if (_isLoading) return;
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

    

    private void button1_Click(object sender, EventArgs e)
    {
        //LoadSettings();
        

        string keyPath = @"SOFTWARE\WOW6432Node\ASCOM\Dome Drivers\RRCI.Dome";

        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(keyPath))
        {
            if (key == null)
            {
                MessageBox.Show("Key not found in HKLM");
                return;
            }

            string baud = key.GetValue("Baud")?.ToString();
            string com = key.GetValue("COM")?.ToString();
            string id = key.GetValue("DeviceId")?.ToString();
            string timeout = key.GetValue("Timeout")?.ToString();
            string safe = key.GetValue("SafeMode")?.ToString();
            string auto = key.GetValue("AutoClose")?.ToString();
            string rain = key.GetValue("RainSensor")?.ToString();
            
            chkSafeMode.Checked = safe != null && (safe.Equals("True", StringComparison.OrdinalIgnoreCase) || safe.Equals("1", StringComparison.OrdinalIgnoreCase));
            chkRainSensor.Checked = rain != null && (rain.Equals("True", StringComparison.OrdinalIgnoreCase) || rain.Equals("1", StringComparison.OrdinalIgnoreCase));
            chkAutoClose.Checked = auto != null && (auto.Equals("True", StringComparison.OrdinalIgnoreCase) || auto.Equals("1", StringComparison.OrdinalIgnoreCase));
            comboBaud.Text = baud;
            comboPorts.Text = com;
            txtDeviceId.Text = id;
            //txtTimeout.Text = timeout;
            

            //MessageBox.Show($"Baud={baud}\nCOM={com}");
        }
    }
}