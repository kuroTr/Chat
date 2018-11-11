﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Collections;
using System.Threading;
using System.Net;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Client
{
    public partial class Client : Form
    {
        private Thread listenerThread;
        const int PORTNUM = 8910;
        private IPEndPoint localIP;
        private Socket client;
        private string user;
        public Client()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            this.AcceptButton = btnSend;
        }

        private void Client_Load(object sender, EventArgs e)
        {
            frmLogin login = new frmLogin();
            this.Show();
            Connect();
            AttemptLogin();
        }

        private void AttemptLogin()
        {
            
            frmLogin login = new frmLogin();
            login.ShowDialog(this);
            if(login.DialogResult == DialogResult.OK)
            {
                user = login.txtlogin.Text;
                client.Send(Serialize("CONNECT|" + user));
                lbName.Text += user;
            }
            if(login.DialogResult == DialogResult.Cancel)
            {
                Close();
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if(txtMessage.Text != string.Empty)
            {
                Send();
                AddMessage(user + ": " + txtMessage.Text);
            }
        }

        //connect, disconnect, gui tin, nhan tin tu server
        #region Cac ham xu ly co ban
        /// <summary>
        /// mo ket noi
        /// </summary>
        public void Connect()
        {
            
            localIP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), PORTNUM);
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                client.Connect(localIP);
            }
            catch
            {
                MessageBox.Show("Không thể kết nối đến server!", "Lỗi!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Disconnect();
                Close();
            }


            listenerThread = new Thread(Receive);
            listenerThread.IsBackground = true;
            listenerThread.Start();

        }

        /// <summary>
        /// dong ket noi
        /// </summary>
        public void Disconnect()
        {

           client.Close();
        }

        /// <summary>
        /// gui tin
        /// </summary>
        public void Send()
        {
            if (txtMessage.Text != string.Empty)
                client.Send(Serialize("CHAT|" + user + ": " + txtMessage.Text));
        }

        /// <summary>
        /// nhan tin
        /// </summary>
        public void Receive()
        {
            try
            {
                while (true)
                {
                    byte[] data = new byte[1024 * 5000];
                    client.Receive(data);

                    string message = (string)Deserialize(data);
                    ReceivedCmd(message);
                }
            }
            catch
            {
                Disconnect();
            }

        }

        /// <summary>
        /// them message
        /// </summary>
        /// <param name="s"></param>
        public void AddMessage(string s)
        {
            listBoxStatus.Items.Add(s);
            txtMessage.Clear();
        }

        /// <summary>
        /// phan manh
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private byte[] Serialize(object obj)
        {
            MemoryStream stream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();

            formatter.Serialize(stream, obj);

            return stream.ToArray();
        }

        /// <summary>
        /// gom manh
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private object Deserialize(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            BinaryFormatter formatter = new BinaryFormatter();

            return formatter.Deserialize(stream);
        }
        #endregion

        //tin nhan duoc tu server se dua vao day xu ly
        #region Ham xu ly lenh nhan dc tu server
        private void ReceivedCmd(string data)
        {
            string[] dataArray = data.Split('|');
            switch (dataArray[0])
            {
               case "CHAT":            //gui tin nhan
                    AddMessage(dataArray[1]);
                    break;
                case "REQUESTUSERS":    // yeu cau gui list user dang online
                    ListUsers(dataArray[1]);
                    break;
                default:
                    AddMessage("Unknown message:" + data);
                    break;
            }
        }

        private void ListUsers(string data)
        {
            string[] Users = data.Split(',');
            listBoxUsers.Items.Clear();
            foreach (string user in Users)
                listBoxUsers.Items.Add(user);
        }


        #endregion

        private void frmServer_FormClosed(object sender, FormClosedEventArgs e)
        {
            if(user != null)
                client.Send(Serialize("DISCONNECT|" + user));
            Disconnect();
        }

        private void btnonlineusers_Click(object sender, EventArgs e)
        {
            client.Send(Serialize("REQUESTUSERS"));
           
        }
    }
}
