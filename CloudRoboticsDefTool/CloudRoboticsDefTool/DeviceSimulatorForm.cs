using System;
using System.Windows.Forms;
using System.Text;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace CloudRoboticsDefTool
{
    public partial class DeviceSimulatorForm : Form
    {
        private DeviceClient deviceClient = null;
        private string iotHubHostName = string.Empty;
        private string iotHubConnectionString = string.Empty;
        private string deviceId = string.Empty;
        private string deviceKey = string.Empty;
        private string deviceType = string.Empty;
        private string deviceMessageType = string.Empty;

        public static string jsonMessages;
        private int sendCount = 0;

        public DeviceSimulatorForm()
        {
            InitializeComponent();
        }

        public DeviceSimulatorForm(string p_iotHubConnectionString, string p_iotHubHostName, string p_deviceId, string p_deviceKey, string p_deviceType)
        {
            InitializeComponent();

            iotHubConnectionString = p_iotHubConnectionString;
            iotHubHostName = p_iotHubHostName;
            deviceId = p_deviceId;
            deviceKey = p_deviceKey;
            deviceType = p_deviceType;
        }

        private void DeviceSimulatorForm_Load(object sender, EventArgs e)
        {
            this.Size = new System.Drawing.Size(640, 480);
            
            comboBoxDeviceId.Text = deviceId;
            textBoxDeviceKey.Text = deviceKey;
            textBoxIotHubHostName.Text = iotHubHostName;

            if (deviceType.ToUpper() == "PEPPER")
            {
                pictureBox1.Image = CloudRoboticsDefTool.Properties.Resources.pepperS;
                deviceMessageType = "PEPPER";
            }
            else if (deviceType.ToUpper() == "SURFACE HUB")
            {
                pictureBox1.Image = CloudRoboticsDefTool.Properties.Resources.surface_hub1;
                deviceMessageType = "SURFACE";
            }
            else if (deviceType.ToUpper() == "SURFACE")
            {
                pictureBox1.Image = CloudRoboticsDefTool.Properties.Resources.Surface;
                deviceMessageType = "SURFACE";
            }

            try
            {
                Properties.Settings.Default.Reload();
                jsonMessages = (string)Properties.Settings.Default["CloudRobotics_JsonMessages"];
            }
            catch
            {
                jsonMessages = string.Empty;
            }

            if (jsonMessages != string.Empty)
            {
                updateJsonComboBox();
            }

            deviceClient = DeviceClient.Create(textBoxIotHubHostName.Text,
                new DeviceAuthenticationWithRegistrySymmetricKey(comboBoxDeviceId.Text, textBoxDeviceKey.Text));

        }

        private void buttonMessageEditForm_Click(object sender, EventArgs e)
        {
            EditMessageForm editMessageForm = new EditMessageForm(jsonMessages);
            editMessageForm.ShowDialog();
            updateJsonComboBox();
        }

        private void buttonSend_Click(object sender, EventArgs e)
        {
            if (textBoxInput.Text == "")
            {
                MessageBox.Show("Send message is nothing !!", "** Error **", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string msg = textBoxInput.Text;
            // Set deviceId to RbHeader
            JObject jo_message = JsonConvert.DeserializeObject<JObject>(msg);
            jo_message["RbHeader"]["SourceDeviceId"] = comboBoxDeviceId.Text;
            msg = JsonConvert.SerializeObject(jo_message);

            SendDeviceToCloudMessagesAsync(msg);
        }

        private async void SendDeviceToCloudMessagesAsync(string msg)
        {
            var message = new Microsoft.Azure.Devices.Client.Message(Encoding.UTF8.GetBytes(msg));

            await deviceClient.SendEventAsync(message);
        }

        private void buttonReceive_Click(object sender, EventArgs e)
        {
            if (comboBoxDeviceId.Text == "")
            {
                MessageBox.Show("Device information is nothing !!", "** Error **", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            textBoxOutput.Text = string.Empty;
            ReceiveC2dAsync();
        }

        private async void ReceiveC2dAsync()
        {
            while (deviceClient != null)
            {
                try
                {
                    Microsoft.Azure.Devices.Client.Message receivedMessage = await deviceClient.ReceiveAsync();
                    if (receivedMessage == null) continue;

                    textBoxOutput.Text += string.Format("Received message: {0}", Encoding.UTF8.GetString(receivedMessage.GetBytes()));
                    textBoxOutput.Text += Environment.NewLine;

                    await deviceClient.CompleteAsync(receivedMessage);
                }
                catch(Exception ex)
                {
                    if (deviceClient != null)
                        throw ex;
                }
            }
        }

        private void comboBoxJson_SelectedIndexChanged(object sender, EventArgs e)
        {
            string searchWord = comboBoxJson.SelectedItem.ToString();

            try
            {
                JObject joMessage = JsonConvert.DeserializeObject<JObject>(jsonMessages);
                JArray ja = (JArray)joMessage["Messages"];

                foreach (JObject jo in ja)
                {
                    string tytle = jo["Tytle"].ToString();

                    if (tytle == searchWord)
                    {
                        JObject joRbMessage = (JObject)jo["RbMessage"];
                        sendCount += 1;
                        joRbMessage["RbHeader"]["MessageSeqno"] = sendCount.ToString();
                        joRbMessage["RbHeader"]["SendDateTime"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

                        textBoxInput.Text = joRbMessage.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Saved JSON Message is invalid !! \n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        private void updateJsonComboBox()
        {
            try
            {
                if (jsonMessages == string.Empty)
                    return;

                JObject joMessage = JsonConvert.DeserializeObject<JObject>(jsonMessages);
                JArray ja = (JArray)joMessage["Messages"];

                comboBoxJson.Items.Clear();

                foreach (JObject jo in ja)
                {
                    string tytle = jo["Tytle"].ToString();
                    string messageType = jo["MessageType"].ToString();

                    if (messageType == deviceMessageType)
                    {
                        comboBoxJson.Items.Add(tytle);
                    }
                    else if (messageType == "CONTROL" || messageType == "CALL")
                    {
                        comboBoxJson.Items.Add(tytle);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Saved JSON Message is invalid !! \n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        private void DeviceSimulatorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (deviceClient != null)
            {
                deviceClient.Dispose();
                deviceClient = null;
            }
        }
    }
}
