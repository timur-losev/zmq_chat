using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace voicelab_test
{
    public partial class Form1 : Form
    {
        const string YOUR_NAME = "YOUR_NAME";
        const string PORT = "PORT";
        public Form1()
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
                this.chatBox.Text = args.MessageText;
            });
        }

        private void joinServer_Click(object sender, EventArgs e)
        {
            if (portTextBox.Text != PORT)
            {
                int numericValue = 0;
                bool isNumber = int.TryParse(portTextBox.Text, out numericValue);

                if (!isNumber || numericValue <= 1024)
                {
                    return;
                }
            }

            if (nameTextBox.Text != YOUR_NAME && nameTextBox.Text.Length <= 20 && nameTextBox.Text.Length > 0)
            {
                Client.OnNewMessage += new EventHandler<ClientEventArgs>(OnNewMessage);

                if (Client.Connect(nameTextBox.Text, portTextBox.Text))
                {
                    connectionPanel.Visible = false;

                    // Set the capture to user's name
                    this.Text = nameTextBox.Text;
                }
                else
                {
                    var res = MessageBox.Show(String.Format("No server at port: {0} \nWould you like to open it?", portTextBox.Text), "Sorry", MessageBoxButtons.YesNo);
                    
                    if (res == DialogResult.Yes)
                    {
                        // Establish a new server instance
                    }
                }
            }
        }

        private void sendBtn_Click(object sender, EventArgs e)
        {
            if (sendTextBox.Text.Length > 0)
            {
                Client.SendMessage(sendTextBox.Text);
                sendTextBox.Text = "";
            }
        }

        private void sendTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r' && sendTextBox.Text.Length > 0)
            {
                Client.SendMessage(sendTextBox.Text);
                sendTextBox.Text = "";
            }
        }

        private void leaveBtn_Click(object sender, EventArgs e)
        {
            Client.LeaveTheServer();
            connectionPanel.Visible = true;
        }
    }
}
