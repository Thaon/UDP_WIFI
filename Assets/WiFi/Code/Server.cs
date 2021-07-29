using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

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

    [RequireComponent(typeof(UnityMainThreadDispatcher))]
    public class Server : MonoBehaviour
    {
        #region member variables

        public int _port = 8008;
        public bool _debug = false;

        public Action<string> OnConnect;
        public Action<string, string> OnData;

        private bool _host;
        private string _thisIP;
        private Thread _serverThread;
        private UdpClient _client;
        private IPEndPoint _endPoint;
        private bool _started = false;

        private Queue<Message> _messagesToBroadcast = new Queue<Message>();

        #endregion

        #region monobehavior

        void OnDestroy()
        {
            if (_serverThread != null)
                _serverThread.Abort();
            if (_client != null)
                _client.Close();
        }

        #endregion

        #region networking core

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

            _endPoint = new IPEndPoint(_host ? IPAddress.Any : IPAddress.Broadcast, _port);
            _client = new UdpClient();
            _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, false);
            _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            

            if(_host) _client.Client.Bind(_endPoint);

            _serverThread = new Thread(new ThreadStart(Listen));
            _serverThread.IsBackground = true;
            _serverThread.Start();

            if (!_host) Send(new Message("CONN", _thisIP));

            _started = true;
            if (_debug) Debug.Log("Started Networking");
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

                    if (_debug) Debug.Log(msg);

                    string cmd = msg.Split('|')[0];
                    string payload = msg.Split('|')[1];
                    string ip = msg.Split('|')[2];

                    Message incoming = new Message(cmd, payload);

                    //broadcast the message back to all clients
                    if (_host && ip != _thisIP) _messagesToBroadcast.Enqueue(incoming);

                    //check for new connections
                    if (cmd == "CONN")
                    {
                        if (_debug) Debug.Log("Connection from: " + payload);
                        UnityMainThreadDispatcher.Instance().Enqueue(Connection(payload));
                    }
                    else
                    {
                        _messagesToBroadcast.Enqueue(incoming);
                    }
                }
                catch (Exception err)
                {
                    if (_debug) Debug.Log(err.ToString());
                }

                while (_messagesToBroadcast.Count > 0)
                {
                    if (_debug) Debug.Log("Messages to broadcast: " + _messagesToBroadcast.Count);
                    Message msg = _messagesToBroadcast.Dequeue();
                    UnityMainThreadDispatcher.Instance().Enqueue(DataReceived(msg._opCode, msg._msg));
                }
            }

        }

        private IEnumerator Connection(string IP)
        {
            OnConnect?.Invoke(IP);
            yield return new WaitForEndOfFrame();
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
                Debug.Log(err.ToString());
            }
        }

        public bool IsHost()
        {
            return _host;
        }

        public bool IsStarted()
        {
            return _started;
        }

        #endregion

        #region utility methods

        public void SendBool(string opCode, bool val)
        {
            string toSend = val ? "1" : "0";

            Send(new Message(opCode, toSend));
        }

        public bool ReadBool(string message)
        {
            bool toRead = message == "1" ? true : false;
            return toRead;
        }

        public void SendNumber(string opCode, float val)
        {
            string toSend = val.ToString();

            Send(new Message(opCode, toSend));
        }

        public float ReadNumber(string message)
        {
            float toRead = float.Parse(message);
            return toRead;
        }

        public void SendVector2(string opCode, Vector2 vector)
        {
            string toSend = "";
            toSend += Mathf.Round(vector.x).ToString() + ",";
            toSend += Mathf.Round(vector.y).ToString();

            Send(new Message(opCode, toSend));
        }

        public Vector3 ReadVector2(string message)
        {
            string[] positions = message.Split(',');
            Vector2 toRead = new Vector2(int.Parse(positions[0]), int.Parse(positions[1]));
            return toRead;
        }

        public void SendVector3(string opCode, Vector3 vector)
        {
            string toSend = "";
            toSend += Mathf.Round(vector.x).ToString() + ",";
            toSend += Mathf.Round(vector.y).ToString() + ",";
            toSend += Mathf.Round(vector.z).ToString();

            Send(new Message(opCode, toSend));
        }

        public Vector3 ReadVector3(string message)
        {
            string[] positions = message.Split(',');
            Vector3 toRead = new Vector3(int.Parse(positions[0]), int.Parse(positions[1]), int.Parse(positions[2]));
            return toRead;
        }

        public void SendJSON<T>(string opCode, T json)
        {
            string toSend = JsonUtility.ToJson(json);

            Send(new Message(opCode, toSend));
        }

        public T ReadJSON<T>(string message)
        {
            T toRead = JsonUtility.FromJson<T>(message);
            return toRead;
        }

        #endregion
    }
}