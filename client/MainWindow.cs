using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace voicemod_test
{
    public partial class MainWindow : Form
    {
        const string YOUR_NAME = "YOUR_NAME";
        const string PORT = "PORT";

        Client chatClient = null;

        public MainWindow()
        {
            InitializeComponent();
            nameTextBox.Text = YOUR_NAME;
            portTextBox.Text = PORT;
        }

        private void textBox1_Enter(object sender, EventArgs e)
        {
            if (nameTextBox.Text == YOUR_NAME)
                nameTextBox.Text = "";
        }

        private void textBox1_Leave(object sender, EventArgs e)
        {
            if (nameTextBox.Text.Length == 0)
            {
                nameTextBox.Text = YOUR_NAME;
            }
        }

        private void portTextBox_Enter(object sender, EventArgs e)
        {
            if (portTextBox.Text == PORT)
            {
                portTextBox.Text = "";
            }
        }

        private void portTextBox_Leave(object sender, EventArgs e)
        {
            if (portTextBox.Text.Length == 0)
            {
                portTextBox.Text = PORT;
            }
        }

        private void OnNewMessage(object o, ClientEventArgs args)
        {
            // Pass message through the main thread
            this.Invoke((MethodInvoker)delegate {
                this.chatBox.Text += args.MessageText;
            });
        }

        private void OnServerDisconnected(object o, ClientEventArgs args)
        {
            this.Invoke((MethodInvoker)delegate
            {
                disconnectedView();
            });
        }

        private void joinServer_Click(object sender, EventArgs e)
        {
            int numericValue = 0;
            bool isNumber = int.TryParse(portTextBox.Text, out numericValue);

            if (!isNumber || numericValue <= 1024 || numericValue > 65535)
            {
                MessageBox.Show("Wrong port number", "Sorry", MessageBoxButtons.OK);
                return;
            }

            if (nameTextBox.Text != YOUR_NAME && nameTextBox.Text.Length <= 20 && nameTextBox.Text.Length > 0)
            {
                chatBox.Text = "";
                if (chatClient.Connect(nameTextBox.Text, portTextBox.Text))
                {
                    connectedView();

                    // Set the capture to user's name
                    this.Text = nameTextBox.Text;
                }
                else
                {
                    var res = MessageBox.Show(String.Format("No server at port: {0} \nWould you like to start it?", portTextBox.Text), "Sorry", MessageBoxButtons.YesNo);
                    
                    if (res == DialogResult.Yes)
                    {
                        // Establish a new server instance
                        string path = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
#if DEBUG
                        System.Diagnostics.Process.Start(path + "/../../../../server/bin/Debug/net5.0/server.exe", "-" + portTextBox.Text);
#else
                        System.Diagnostics.Process.Start(path + "/server.exe", "-" + portTextBox.Text);
#endif
                        Thread.Sleep(500);

                        // Try connecting again
                        joinServer_Click(sender, e);
                    }
                }

            }
            else
            {
                MessageBox.Show("Incorrect value in YOUR NAME field", "Sorry", MessageBoxButtons.OK);
            }
        }

        private void sendBtn_Click(object sender, EventArgs e)
        {
            if (sendTextBox.Text.Length > 0)
            {
                chatClient.SendChatMessage(sendTextBox.Text);
                sendTextBox.Text = "";
            }
        }

        private void sendTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r' && sendTextBox.Text.Length > 0)
            {
                chatClient.SendChatMessage(sendTextBox.Text);
                sendTextBox.Text = "";
            }
        }

        private void disconnectedView()
        {
            connectionPanel.Visible = true;
            sendPanel.Visible = false;
        }

        private void connectedView()
        {
            connectionPanel.Visible = false;
            sendPanel.Visible = true;
        }

        private void leaveBtn_Click(object sender, EventArgs e)
        {
            chatClient.SendLeaveTheServer();
            disconnectedView();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Client.LeaveTheServer();
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            disconnectedView();
            chatClient = new Client();
            chatClient.OnNewMessage += new EventHandler<ClientEventArgs>(OnNewMessage);
            chatClient.OnServerDisconnected += new EventHandler<ClientEventArgs>(OnServerDisconnected);
        }

        private void shutDownServer_Click(object sender, EventArgs e)
        {
            chatClient.SendServerShutdown();

            disconnectedView();
        }
    }
}
