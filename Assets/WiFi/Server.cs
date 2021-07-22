using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using System.Linq;
using System.Collections;

namespace LocalNetworking
{
    public class Message
    {
        public string _opCode, _msg;

        public Message(string opCode, string msg)
        {
            _opCode = opCode;
            _msg = msg;
        }
    }

    public class Server : MonoBehaviour
    {
        #region member variables

        public Action<string, string> OnData;

        private bool _host;
        private string _thisIP;
        private Thread _serverThread;
        private UdpClient _client;
        private IPEndPoint _endPoint;

        #endregion

        public void StartNetworking(bool hosting)
        {
            if (hosting)
            {
                _host = true;
            }
            else
            {
                _host = false;
            }

            _thisIP = Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToString();

            _endPoint = new IPEndPoint(IPAddress.Broadcast, 8008);
            _client = new UdpClient(8008);

            _serverThread = new Thread(new ThreadStart(Listen));
            _serverThread.IsBackground = true;
            _serverThread.Start();
        }

        private void Listen()
        {
            while (true)
            {
                try
                {
                    byte[] bytes = _client.Receive(ref _endPoint);

                    //decode message
                    string msg = Encoding.UTF8.GetString(bytes);

                    string cmd = msg.Split('|')[0];
                    string payload = msg.Split('|')[1];
                    string ip = msg.Split('|')[2];

                    //broadcast the message back to all clients
                    if (_host && ip != _thisIP) Send(new Message(cmd, payload));

                    UnityMainThreadDispatcher.Instance().Enqueue(DataReceived(cmd, payload));
                }
                catch (Exception err)
                {
                    print(err.ToString());
                }
            }
        }

        private IEnumerator DataReceived(string cmd, string payload)
        {
            OnData?.Invoke(cmd, payload);
            yield return new WaitForEndOfFrame();
        }

        public void Send(Message msg)
        {
            if (msg._opCode.Length == 0)
                return;

            string message = msg._opCode + "|" + msg._msg;

            try
            {
                if (message.Length == 0 || !message.Contains("|"))
                    return;

                byte[] data = Encoding.UTF8.GetBytes(message + "|" + _thisIP);
                _client.Send(data, data.Length, _endPoint);
            }
            catch (Exception err)
            {
                print(err.ToString());
            }
        }

        public bool IsHost()
        {
            return _host;
        }

        void OnDestroy()
        {
            if (_serverThread != null)
                _serverThread.Abort();
            if (_client != null)
                _client.Close();
        }
    }
}