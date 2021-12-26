
namespace voicemod_test
{
    partial class MainWindow
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.chatBox = new System.Windows.Forms.RichTextBox();
            this.connectionPanel = new System.Windows.Forms.Panel();
            this.portTextBox = new System.Windows.Forms.TextBox();
            this.nameTextBox = new System.Windows.Forms.TextBox();
            this.joinServer = new System.Windows.Forms.Button();
            this.sendPanel = new System.Windows.Forms.Panel();
            this.shutDownServer = new System.Windows.Forms.Button();
            this.leaveBtn = new System.Windows.Forms.Button();
            this.sendBtn = new System.Windows.Forms.Button();
            this.sendTextBox = new System.Windows.Forms.TextBox();
            this.connectingLabel = new System.Windows.Forms.Label();
            this.connectionPanel.SuspendLayout();
            this.sendPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // chatBox
            // 
            this.chatBox.AccessibleName = "";
            this.chatBox.Location = new System.Drawing.Point(12, 47);
            this.chatBox.Name = "chatBox";
            this.chatBox.ReadOnly = true;
            this.chatBox.Size = new System.Drawing.Size(776, 290);
            this.chatBox.TabIndex = 1;
            this.chatBox.Text = "";
            // 
            // connectionPanel
            // 
            this.connectionPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(255)))));
            this.connectionPanel.Controls.Add(this.connectingLabel);
            this.connectionPanel.Controls.Add(this.portTextBox);
            this.connectionPanel.Controls.Add(this.joinServer);
            this.connectionPanel.Controls.Add(this.nameTextBox);
            this.connectionPanel.Location = new System.Drawing.Point(238, 175);
            this.connectionPanel.Name = "connectionPanel";
            this.connectionPanel.Size = new System.Drawing.Size(317, 83);
            this.connectionPanel.TabIndex = 2;
            // 
            // portTextBox
            // 
            this.portTextBox.Location = new System.Drawing.Point(17, 50);
            this.portTextBox.Name = "portTextBox";
            this.portTextBox.Size = new System.Drawing.Size(178, 23);
            this.portTextBox.TabIndex = 2;
            this.portTextBox.Text = "PORT";
            this.portTextBox.Enter += new System.EventHandler(this.portTextBox_Enter);
            this.portTextBox.Leave += new System.EventHandler(this.portTextBox_Leave);
            // 
            // nameTextBox
            // 
            this.nameTextBox.Location = new System.Drawing.Point(17, 12);
            this.nameTextBox.Name = "nameTextBox";
            this.nameTextBox.Size = new System.Drawing.Size(178, 23);
            this.nameTextBox.TabIndex = 1;
            this.nameTextBox.Text = "YOUR NAME";
            this.nameTextBox.WordWrap = false;
            this.nameTextBox.Enter += new System.EventHandler(this.textBox1_Enter);
            this.nameTextBox.Leave += new System.EventHandler(this.textBox1_Leave);
            // 
            // joinServer
            // 
            this.joinServer.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.joinServer.ForeColor = System.Drawing.Color.Blue;
            this.joinServer.Location = new System.Drawing.Point(207, 12);
            this.joinServer.Name = "joinServer";
            this.joinServer.Size = new System.Drawing.Size(93, 61);
            this.joinServer.TabIndex = 0;
            this.joinServer.Text = "JOIN";
            this.joinServer.UseVisualStyleBackColor = true;
            this.joinServer.Click += new System.EventHandler(this.joinServer_Click);
            // 
            // sendPanel
            // 
            this.sendPanel.BackColor = System.Drawing.SystemColors.Control;
            this.sendPanel.Controls.Add(this.shutDownServer);
            this.sendPanel.Controls.Add(this.leaveBtn);
            this.sendPanel.Controls.Add(this.sendBtn);
            this.sendPanel.Controls.Add(this.sendTextBox);
            this.sendPanel.Location = new System.Drawing.Point(12, 340);
            this.sendPanel.Name = "sendPanel";
            this.sendPanel.Size = new System.Drawing.Size(776, 95);
            this.sendPanel.TabIndex = 3;
            // 
            // shutDownServer
            // 
            this.shutDownServer.Location = new System.Drawing.Point(628, 37);
            this.shutDownServer.Name = "shutDownServer";
            this.shutDownServer.Size = new System.Drawing.Size(130, 23);
            this.shutDownServer.TabIndex = 3;
            this.shutDownServer.Text = "SHUT DOWN SERVER ";
            this.shutDownServer.UseVisualStyleBackColor = true;
            this.shutDownServer.Click += new System.EventHandler(this.shutDownServer_Click);
            // 
            // leaveBtn
            // 
            this.leaveBtn.Location = new System.Drawing.Point(523, 37);
            this.leaveBtn.Name = "leaveBtn";
            this.leaveBtn.Size = new System.Drawing.Size(99, 23);
            this.leaveBtn.TabIndex = 2;
            this.leaveBtn.Text = "LEAVE";
            this.leaveBtn.UseVisualStyleBackColor = true;
            this.leaveBtn.Click += new System.EventHandler(this.leaveBtn_Click);
            // 
            // sendBtn
            // 
            this.sendBtn.Location = new System.Drawing.Point(418, 37);
            this.sendBtn.Name = "sendBtn";
            this.sendBtn.Size = new System.Drawing.Size(99, 23);
            this.sendBtn.TabIndex = 1;
            this.sendBtn.Text = "SEND";
            this.sendBtn.UseVisualStyleBackColor = true;
            this.sendBtn.Click += new System.EventHandler(this.sendBtn_Click);
            // 
            // sendTextBox
            // 
            this.sendTextBox.Location = new System.Drawing.Point(4, 37);
            this.sendTextBox.Name = "sendTextBox";
            this.sendTextBox.Size = new System.Drawing.Size(408, 23);
            this.sendTextBox.TabIndex = 0;
            this.sendTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.sendTextBox_KeyPress);
            // 
            // connectingLabel
            // 
            this.connectingLabel.AutoSize = true;
            this.connectingLabel.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.connectingLabel.ForeColor = System.Drawing.Color.DarkRed;
            this.connectingLabel.Location = new System.Drawing.Point(201, 34);
            this.connectingLabel.Name = "connectingLabel";
            this.connectingLabel.Size = new System.Drawing.Size(110, 20);
            this.connectingLabel.TabIndex = 3;
            this.connectingLabel.Text = "CONNECTING...";
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.sendPanel);
            this.Controls.Add(this.connectionPanel);
            this.Controls.Add(this.chatBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "MainWindow";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Form1";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Shown += new System.EventHandler(this.Form1_Shown);
            this.connectionPanel.ResumeLayout(false);
            this.connectionPanel.PerformLayout();
            this.sendPanel.ResumeLayout(false);
            this.sendPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox chatBox;
        private System.Windows.Forms.Panel connectionPanel;
        private System.Windows.Forms.TextBox portTextBox;
        private System.Windows.Forms.TextBox nameTextBox;
        private System.Windows.Forms.Button joinServer;
        private System.Windows.Forms.Panel sendPanel;
        private System.Windows.Forms.Button sendBtn;
        private System.Windows.Forms.TextBox sendTextBox;
        private System.Windows.Forms.Button shutDownServer;
        private System.Windows.Forms.Button leaveBtn;
        private System.Windows.Forms.Label connectingLabel;
    }
}

