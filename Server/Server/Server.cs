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

namespace Server
{
    public partial class frmServer : Form
    {
        private Thread listenerThread;
        const int PORTNUM = 8000;
        private IPEndPoint localIP;
        private Socket server;
        private List<ClientInfo> clientList;


        public frmServer()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            this.AcceptButton = btnSend;
            
        }

        
        private void frmServer_Load(object sender, EventArgs e)
        {
            Connect();
            AddMessage("San sang ket noi");
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            foreach (ClientInfo client in clientList)
            {
               Send(txtMessage.Text, client.ClientSocket);
            }
            AddMessage("Server: " + txtMessage.Text);
            txtMessage.Clear();
        }

        /// <summary>
        /// mo ket noi
        /// </summary>
        public void Connect()
        {
            clientList = new List<ClientInfo>();
            localIP = new IPEndPoint(IPAddress.Any, PORTNUM);
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(localIP);

            listenerThread = new Thread(() =>
            {
                try
                {
                    while(true)
                    {
                        server.Listen(100);
                        Socket client = server.Accept();
                        clientList.Add(new ClientInfo(client));

                        Thread receive = new Thread(Receive);
                        receive.IsBackground = true;
                        receive.Start(client);
                    }
                }
                catch
                {
                    localIP = new IPEndPoint(IPAddress.Any, PORTNUM);
                    server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                }
            });
            listenerThread.IsBackground = true;
            listenerThread.Start();
        }

        /// <summary>
        /// dong ket noi
        /// </summary>
        public void Disconnect()
        {
            server.Close();
        }

        /// <summary>
        /// gui tin
        /// </summary>
        public void Send(string msg, Socket client)
        {
            if (client != null && msg != string.Empty)
                client.Send(Serialize("Server: " + msg));
        }

        /// <summary>
        /// nhan tin
        /// </summary>
        public void Receive(object obj)
        {
            Socket client = obj as Socket;
            try
            {
                while (true)
                {
                    byte[] data = new byte[1024 * 5000];
                    client.Receive(data);

                    string message = (string)Deserialize(data);
                    foreach(ClientInfo item in clientList)
                    {
                        if (item.ClientSocket != null && item.ClientSocket != client)
                            item.ClientSocket.Send(Serialize(message));
                    }
                    AddMessage(message);
                }
            }
            catch
            {
                clientList.Remove(new ClientInfo(client));
                client.Close();
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

       /// <summary>
       /// dang nhap
       /// </summary>
       /// <param name="Name"></param>
       /// <param name="client"></param>
        private void ConnectUser(string Name, ClientInfo client)
        {
            client.Name = Name;
            clientList.Add(client);
            AddMessage(client.Name + " just logged in");
            foreach (ClientInfo item in clientList)
            {
                Send(client.Name + " just logged in", client.ClientSocket);
            }

        }




        private void frmServer_FormClosed(object sender, FormClosedEventArgs e)
        {
            Disconnect();
        }
    }
}