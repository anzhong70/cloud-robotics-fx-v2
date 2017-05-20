using System;
using System.Text;
using System.Windows.Forms;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CloudRoboticsDeviceSimulator
{
    public partial class DeviceSimulatorForm : Form
    {
        private static DeviceClient deviceClient = null;
        private static string iotHubHostName = string.Empty;
        private static string iotHubConnectionString = string.Empty;
        private static string deviceId = string.Empty;
        private static string deviceKey = string.Empty;
        private static string deviceType = string.Empty;
        private static string deviceMessageType = string.Empty;

        public static string jsonMessages;
        private static int sendCount = 0;

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
                pictureBox1.Image = CloudRoboticsDeviceSimulator.Properties.Resources.pepperS;
                deviceMessageType = "PEPPER";
            }
            else if (deviceType.ToUpper() == "SURFACE HUB")
            {
                pictureBox1.Image = CloudRoboticsDeviceSimulator.Properties.Resources.surface_hub1;
                deviceMessageType = "SURFACE";
            }
            else if (deviceType.ToUpper() == "SURFACE")
            {
                pictureBox1.Image = CloudRoboticsDeviceSimulator.Properties.Resources.Surface;
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
            SendDeviceToCloudMessagesAsync(msg);
        }
        private async void SendDeviceToCloudMessagesAsync(string msg)
        {
            if (deviceClient == null)
            {
                deviceClient = DeviceClient.Create(textBoxIotHubHostName.Text,
                    new DeviceAuthenticationWithRegistrySymmetricKey(comboBoxDeviceId.Text, textBoxDeviceKey.Text));
            }

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
            if (deviceClient == null)
            {
                deviceClient = DeviceClient.Create(textBoxIotHubHostName.Text,
                    new DeviceAuthenticationWithRegistrySymmetricKey(comboBoxDeviceId.Text, textBoxDeviceKey.Text));
            }

            while (true)
            {
                Microsoft.Azure.Devices.Client.Message receivedMessage = await deviceClient.ReceiveAsync();
                if (receivedMessage == null) continue;

                textBoxOutput.Text += string.Format("Received message: {0}", Encoding.UTF8.GetString(receivedMessage.GetBytes()));
                textBoxOutput.Text += Environment.NewLine;

                await deviceClient.CompleteAsync(receivedMessage);
            }
        }

        private void comboBoxDeviceId_SelectedIndexChanged(object sender, EventArgs e)
        {

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

    }
}
