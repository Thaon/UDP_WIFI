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
    public class Server : MonoBehaviour
    {
        private const int BUFFER_SIZE = 100 * 1024;

        #region nested classes

        [System.Serializable]
        public class Message
        {
            public string _opCode, _msg;

            public Message(string opCode, string msg)
            {
                _opCode = opCode;
                _msg = msg;
            }
        }

        #endregion

        [HideInInspector]
        public bool host;
        public int _port = 8008;
        public bool _debug = false;
        public Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        /// <summary>
        /// Events handling
        /// </summary>
        public Action<Socket> OnConnection;
        public Action<Message> OnData;
        public Action OnServerShutdown;

        /// <summary>
        /// Networking handlers
        /// </summary>
        private Thread _clientThread;
        private List<Socket> _clients = new List<Socket>();
        private byte[] _buffer = new byte[BUFFER_SIZE];


        private void OnDisable()
        {
            if (host) CloseAllSockets(); else CloseClientConnection();
        }

        public void Host()
        {
            if (_debug) Debug.LogError("Started Host");
            host = true;
            _socket.Bind(new IPEndPoint(IPAddress.Any, _port));
            _socket.Listen(100);
            _socket.BeginAccept(Listen, null);
        }

        public void Join()
        {
            if (_debug) Debug.LogError("Started Client");
            host = false;
            int attempts = 0;

            while (!_socket.Connected || attempts < 100)
            {
                try
                {
                    attempts++;
                    Debug.Log("Connection attempt " + attempts);
                    _socket.Connect(IPAddress.Loopback, _port);
                }
                catch (SocketException)
                {
                    Console.Clear();
                }
            }

            _clientThread = new Thread(new ThreadStart(ClientListen));
            _clientThread.IsBackground = true;
            _clientThread.Start();
        }

        private void ClientListen()
        {
            while (true)
            {
                //unpacke the stream of data
                var buffer = new byte[BUFFER_SIZE];
                int received = _socket.Receive(buffer, SocketFlags.None);
                if (received == 0) return;
                var data = new byte[received];
                Array.Copy(buffer, data, received);

                //decode message and invoke events
                string messageJson = Encoding.ASCII.GetString(data);
                Message msg = JsonUtility.FromJson<Message>(UnpackJson(messageJson));
                UnityMainThreadDispatcher.Instance().Enqueue(OnDataCO(msg));
            }
        }

        private IEnumerator OnDataCO(Message msg)
        {
            OnData?.Invoke(msg);
            yield return new WaitForEndOfFrame();
        }

        private void CloseClientConnection()
        {
            Send("CLIENT_EXIT", ""); // Tell the server we are exiting
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }

        public void Send(string opCode, string text)
        {
            Message msg = new Message(opCode, text);

            string toSend = PackJson(JsonUtility.ToJson(msg).ToString());
            if (_debug) Debug.LogError("Sending Message: " + toSend);

            byte[] bytes = Encoding.ASCII.GetBytes(toSend);
            if (host)
            {
                _clients.ForEach(socket =>
                {
                    socket.Send(bytes);
                });
            }
            else
            {
                _socket.Send(bytes);
            }
        }

        private void Listen(IAsyncResult AR)
        {
            Socket socket;

            try
            {
                socket = _socket.EndAccept(AR);
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            _clients.Add(socket);
            OnConnection?.Invoke(socket);
            socket.BeginReceive(_buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveData, socket);
            _socket.BeginAccept(Listen, null);

        }

        private void ReceiveData(IAsyncResult AR)
        {
            Socket current = (Socket)AR.AsyncState;
            int received;

            try
            {
                received = current.EndReceive(AR);
            }
            catch (SocketException)
            {
                Debug.LogWarning("Client forcefully disconnected");
                // Don't shutdown because the socket may be disposed and its disconnected anyway.
                current.Close();
                _clients.Remove(current);
                return;
            }

            byte[] recBuf = new byte[received];
            Array.Copy(_buffer, recBuf, received);
            string messageJson = Encoding.ASCII.GetString(recBuf);

            Message msg = JsonUtility.FromJson<Message>(UnpackJson(messageJson));

            if (_debug) Debug.LogError(msg._opCode + " - " + msg._msg);

            //invoke events
            OnData?.Invoke(msg);

            //check for special messages
            switch (msg._opCode)
            {
                case "CLIENT_EXIT":
                    // Always Shutdown before closing
                    current.Shutdown(SocketShutdown.Both);
                    current.Close();
                    _clients.Remove(current);
                    if (_debug) Debug.Log("Client disconnected");
                    break;

                case "SRV_SHUTDOWN":
                    OnServerShutdown?.Invoke();
                    CloseAllSockets();
                    return;
            }

            //broadcast message back
            if (host) _clients.ForEach(socket =>
            {
                socket.Send(recBuf);
            });

            current.BeginReceive(_buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveData, current);
        }

        private void CloseAllSockets()
        {
            //notify clients of the imminent server shutdown
            Message msg = new Message("SRV_SHUTDOWN", "");

            string toSend = PackJson(JsonUtility.ToJson(msg).ToString());
            byte[] bytes = Encoding.ASCII.GetBytes(toSend);

            _clients.ForEach(socket =>
            {
                socket.Send(bytes);
            });

            foreach (Socket socket in _clients)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }

            _socket.Close();
        }

        #region utility

        public string PackJson(string json)
        {
            string newJson = json.Trim('"');
            newJson = json.Replace('"', '\'');
            return newJson;
        }

        public string UnpackJson(string data)
        {
            string newJson = data.Trim('"');
            newJson = newJson.Replace('\'', '"');
            return newJson;
        }

        public void SendJSON<T>(string opCode, T json)
        {
            string toSend = JsonUtility.ToJson(json);

            Send(opCode, toSend);
        }

        public T ReadJSON<T>(string message)
        {
            T toRead = JsonUtility.FromJson<T>(message);
            return toRead;
        }

        #endregion
    }
}