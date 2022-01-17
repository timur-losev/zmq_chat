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

        client.ClientController m_chatClient = null;

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

        private void OnNewChatMessage(string message)
        {
            // Pass message through the main thread
            if (InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    this.chatBox.Text += message;
                });
            }
            else
            {
                this.chatBox.Text += message;
            }
        }

        private void OnServerDisconnected()
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
                joinServer.Visible = false;
                connectingLabel.Visible = true;
                // Initiate the connection in the background thread
               Task.Run(() => {
                    m_chatClient.Connect(nameTextBox.Text, portTextBox.Text,
                        // onConnected callback
                        chatHistory =>
                        {
                            // Redirect to the main thread
                            if (this.InvokeRequired)
                            {
                                this.Invoke((MethodInvoker)delegate
                                {
                                    connectedView();

                                    this.Text = nameTextBox.Text;
                                    this.chatBox.Text = chatHistory;
                                });
                            }
                        },
                        // onConnectionFailed callback
                        () =>
                        {
                            // Redirect to the main thread
                            if (this.InvokeRequired)
                            {
                                this.Invoke((MethodInvoker)delegate
                                {
                                    var res = MessageBox.Show(String.Format("No server at port: {0} \nWould you like to start it?", portTextBox.Text), "Sorry", MessageBoxButtons.YesNo);

                                    if (res == DialogResult.Yes)
                                    {
                                        // Launch a new server instance
                                        common.Common.StartServer(portTextBox.Text, System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath));
                                        Thread.Sleep(500);

                                        // Try connecting again
                                        joinServer_Click(sender, e);
                                    }
                                    else
                                    {
                                        disconnectedView();
                                    }
                                });
                            }
                        },
                        // onNewChatMessage callback
                        OnNewChatMessage,
                        //onServerShutDown callback
                        () =>
                        {
                            OnNewChatMessage("SERVER HAS BEEN SHUT DOWN");
                            OnServerDisconnected();
                        });
                });
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
                m_chatClient.SendChatMessage(sendTextBox.Text);
                sendTextBox.Text = "";
            }
        }

        private void sendTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r' && sendTextBox.Text.Length > 0)
            {
                m_chatClient.SendChatMessage(sendTextBox.Text);
                sendTextBox.Text = "";
            }
        }

        private void disconnectedView()
        {
            connectionPanel.Visible = true;
            sendPanel.Visible = false;
            joinServer.Visible = true;
            connectingLabel.Visible = false;
        }

        private void connectedView()
        {
            connectionPanel.Visible = false;
            sendPanel.Visible = true;
        }

        private void leaveBtn_Click(object sender, EventArgs e)
        {
            m_chatClient.SendLeaveTheServer();
            disconnectedView();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            m_chatClient.SendLeaveTheServer();
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            disconnectedView();
            m_chatClient = new client.ClientController();
        }

        private void shutDownServer_Click(object sender, EventArgs e)
        {
            m_chatClient.SendServerShutdown();

            disconnectedView();
        }
    }
    
    public class TheTest
    {
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var form = new MainWindow();
            Application.Run(form);
        }
    }
}


