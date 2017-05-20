namespace CloudRoboticsDeviceSimulator
{
    partial class DeviceSimulatorForm
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.textBoxOutput = new System.Windows.Forms.TextBox();
            this.textBoxDeviceKey = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.comboBoxDeviceId = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.textBoxIotHubHostName = new System.Windows.Forms.TextBox();
            this.buttonReceive = new System.Windows.Forms.Button();
            this.buttonSend = new System.Windows.Forms.Button();
            this.buttonMessageEditForm = new System.Windows.Forms.Button();
            this.comboBoxJson = new System.Windows.Forms.ComboBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.textBoxInput = new System.Windows.Forms.TextBox();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI Symbol", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(3, 328);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(61, 15);
            this.label1.TabIndex = 10;
            this.label1.Text = "DeviceId : ";
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.Controls.Add(this.textBoxOutput);
            this.panel1.Controls.Add(this.textBoxDeviceKey);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.comboBoxDeviceId);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.textBoxIotHubHostName);
            this.panel1.Controls.Add(this.buttonReceive);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.buttonSend);
            this.panel1.Controls.Add(this.buttonMessageEditForm);
            this.panel1.Controls.Add(this.comboBoxJson);
            this.panel1.Controls.Add(this.pictureBox1);
            this.panel1.Controls.Add(this.textBoxInput);
            this.panel1.Location = new System.Drawing.Point(6, 9);
            this.panel1.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(626, 464);
            this.panel1.TabIndex = 21;
            // 
            // textBoxOutput
            // 
            this.textBoxOutput.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxOutput.Location = new System.Drawing.Point(2, 361);
            this.textBoxOutput.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            this.textBoxOutput.Multiline = true;
            this.textBoxOutput.Name = "textBoxOutput";
            this.textBoxOutput.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxOutput.Size = new System.Drawing.Size(617, 101);
            this.textBoxOutput.TabIndex = 26;
            // 
            // textBoxDeviceKey
            // 
            this.textBoxDeviceKey.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxDeviceKey.Location = new System.Drawing.Point(311, 330);
            this.textBoxDeviceKey.Name = "textBoxDeviceKey";
            this.textBoxDeviceKey.Size = new System.Drawing.Size(308, 20);
            this.textBoxDeviceKey.TabIndex = 25;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI Symbol", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(238, 329);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(70, 15);
            this.label2.TabIndex = 24;
            this.label2.Text = "DeviceKey : ";
            // 
            // comboBoxDeviceId
            // 
            this.comboBoxDeviceId.FormattingEnabled = true;
            this.comboBoxDeviceId.Location = new System.Drawing.Point(66, 329);
            this.comboBoxDeviceId.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            this.comboBoxDeviceId.Name = "comboBoxDeviceId";
            this.comboBoxDeviceId.Size = new System.Drawing.Size(160, 21);
            this.comboBoxDeviceId.TabIndex = 23;
            this.comboBoxDeviceId.SelectedIndexChanged += new System.EventHandler(this.comboBoxDeviceId_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Segoe UI Symbol", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(3, 298);
            this.label3.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(84, 15);
            this.label3.TabIndex = 22;
            this.label3.Text = "IoT Hub Host :";
            // 
            // textBoxIotHubHostName
            // 
            this.textBoxIotHubHostName.Location = new System.Drawing.Point(90, 299);
            this.textBoxIotHubHostName.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            this.textBoxIotHubHostName.Name = "textBoxIotHubHostName";
            this.textBoxIotHubHostName.Size = new System.Drawing.Size(282, 20);
            this.textBoxIotHubHostName.TabIndex = 21;
            // 
            // buttonReceive
            // 
            this.buttonReceive.Font = new System.Drawing.Font("Segoe UI Symbol", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonReceive.Location = new System.Drawing.Point(182, 254);
            this.buttonReceive.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.buttonReceive.Name = "buttonReceive";
            this.buttonReceive.Size = new System.Drawing.Size(188, 31);
            this.buttonReceive.TabIndex = 19;
            this.buttonReceive.Text = "Receive from IoT Hub (Waiting)";
            this.buttonReceive.UseVisualStyleBackColor = true;
            this.buttonReceive.Click += new System.EventHandler(this.buttonReceive_Click);
            // 
            // buttonSend
            // 
            this.buttonSend.Font = new System.Drawing.Font("Segoe UI Symbol", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonSend.Location = new System.Drawing.Point(0, 254);
            this.buttonSend.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.buttonSend.Name = "buttonSend";
            this.buttonSend.Size = new System.Drawing.Size(169, 31);
            this.buttonSend.TabIndex = 18;
            this.buttonSend.Text = "Send to IoT Hub";
            this.buttonSend.UseVisualStyleBackColor = true;
            this.buttonSend.Click += new System.EventHandler(this.buttonSend_Click);
            // 
            // buttonMessageEditForm
            // 
            this.buttonMessageEditForm.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonMessageEditForm.Font = new System.Drawing.Font("Segoe UI Symbol", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonMessageEditForm.Location = new System.Drawing.Point(456, 253);
            this.buttonMessageEditForm.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            this.buttonMessageEditForm.Name = "buttonMessageEditForm";
            this.buttonMessageEditForm.Size = new System.Drawing.Size(162, 34);
            this.buttonMessageEditForm.TabIndex = 17;
            this.buttonMessageEditForm.Text = "Launch Message Edit Form";
            this.buttonMessageEditForm.UseVisualStyleBackColor = true;
            this.buttonMessageEditForm.Click += new System.EventHandler(this.buttonMessageEditForm_Click);
            // 
            // comboBoxJson
            // 
            this.comboBoxJson.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxJson.FormattingEnabled = true;
            this.comboBoxJson.Location = new System.Drawing.Point(387, 226);
            this.comboBoxJson.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            this.comboBoxJson.Name = "comboBoxJson";
            this.comboBoxJson.Size = new System.Drawing.Size(232, 21);
            this.comboBoxJson.TabIndex = 16;
            this.comboBoxJson.SelectedIndexChanged += new System.EventHandler(this.comboBoxJson_SelectedIndexChanged);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox1.ErrorImage = null;
            this.pictureBox1.Image = global::CloudRoboticsDeviceSimulator.Properties.Resources.pepperS;
            this.pictureBox1.InitialImage = null;
            this.pictureBox1.Location = new System.Drawing.Point(387, 2);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(1, 2, 1, 2);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(230, 205);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 15;
            this.pictureBox1.TabStop = false;
            // 
            // textBoxInput
            // 
            this.textBoxInput.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxInput.Location = new System.Drawing.Point(2, 2);
            this.textBoxInput.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.textBoxInput.Multiline = true;
            this.textBoxInput.Name = "textBoxInput";
            this.textBoxInput.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxInput.Size = new System.Drawing.Size(370, 243);
            this.textBoxInput.TabIndex = 4;
            // 
            // DeviceSimulatorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(629, 484);
            this.Controls.Add(this.panel1);
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "DeviceSimulatorForm";
            this.Text = "Cloud Robotics Device Simulator";
            this.Load += new System.EventHandler(this.DeviceSimulatorForm_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.TextBox textBoxOutput;
        private System.Windows.Forms.TextBox textBoxDeviceKey;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboBoxDeviceId;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBoxIotHubHostName;
        private System.Windows.Forms.Button buttonReceive;
        private System.Windows.Forms.Button buttonSend;
        private System.Windows.Forms.Button buttonMessageEditForm;
        private System.Windows.Forms.ComboBox comboBoxJson;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.TextBox textBoxInput;
    }
}

