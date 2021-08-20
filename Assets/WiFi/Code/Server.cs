using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LocalNetworking
{
    public class Server : MonoBehaviour
    {
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


        #region member variables
        
        /// <summary>
        /// The size of the buffer used to send and receive data
        /// </summary>
        private const int BUFFER_SIZE = 100 * 1024;

        /// <summary>
        /// should we debug the communications?
        /// </summary>
        public bool _debug = false;

        /// <summary>
        /// is this instance a host?
        /// </summary>
        [HideInInspector]
        public bool host;
        /// <summary>
        /// the port used for communications, MUST BE ABOVE 3K
        /// </summary>
        public int _port = 8008;
        /// <summary>
        /// the socket we will use for communications
        /// </summary>
        public Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        /// <summary>
        /// Events handling
        /// </summary>
        public Action<Socket> OnConnection;
        public Action<Message> OnData;
        public Action<Socket> OnClientDisconnect;
        public Action OnServerShutdown;

        /// <summary>
        /// Using a thread so we don't block the program's execution while listening
        /// </summary>
        private Thread _clientThread;
        /// <summary>
        /// list of the connected clients
        /// </summary>
        private List<Socket> _clients = new List<Socket>();
        /// <summary>
        /// buffer used for sending/receiving data
        /// </summary>
        private byte[] _buffer = new byte[BUFFER_SIZE];

        #endregion


        /// <summary>
        /// clean up on disable
        /// </summary>
        private void OnDisable()
        {
            if (host) CloseAllSockets(); else CloseClientConnection();
        }

        /// <summary>
        /// We initiate a server session
        /// </summary>
        public void Host()
        {
            if (_debug) Debug.LogError("Started Host");
            host = true;
            _socket.Bind(new IPEndPoint(IPAddress.Any, _port));
            _socket.Listen(100);
            _socket.BeginAccept(ListenForConnections, null);
        }

        /// <summary>
        /// we initiate a client session
        /// </summary>
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
                    Debug.LogError("An error occurred whilst trying to join a game.");
                }
            }

            _clientThread = new Thread(new ThreadStart(ListenForData));
            _clientThread.IsBackground = true;
            _clientThread.Start();
        }

        /// <summary>
        /// thread used to listen for incoming connections
        /// </summary>
        private void ListenForConnections(IAsyncResult AR)
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
            _socket.BeginAccept(ListenForConnections, null);

        }

        /// <summary>
        /// thread used to listen for incoming data
        /// </summary>
        private void ListenForData()
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

        /// <summary>
        /// fire up events when data is received, this happens on the main thread
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private IEnumerator OnDataCO(Message msg)
        {
            OnData?.Invoke(msg);
            yield return new WaitForEndOfFrame();
        }

        /// <summary>
        /// simpler version of the Send method used to pack Messages across
        /// </summary>
        /// <param name="opCode"></param>
        /// <param name="text"></param>
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

        /// <summary>
        /// both the server and the client will use this to process received data
        /// </summary>
        /// <param name="AR"></param>
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
                //close the connection and clean up if there's an issue
                if (_debug) Debug.LogWarning("Client disconnected");
                current.Close();
                _clients.Remove(current);
                return;
            }

            //get the data
            byte[] recBuf = new byte[received];
            Array.Copy(_buffer, recBuf, received);

            //decode the message
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
                    OnClientDisconnect?.Invoke(current);
                    if (_debug) Debug.Log("Client disconnected");
                    break;

                case "SRV_SHUTDOWN":
                    OnServerShutdown?.Invoke();
                    CloseAllSockets();
                    return;
            }

            //broadcast message back on the server
            if (host) _clients.ForEach(socket =>
            {
                socket.Send(recBuf);
            });

            //restart the cycle
            current.BeginReceive(_buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveData, current);
        }

        /// <summary>
        /// clean exit for the client
        /// </summary>
        private void CloseClientConnection()
        {
            Send("CLIENT_EXIT", ""); // Tell the server we are exiting
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }

        /// <summary>
        /// complete server shutdown
        /// </summary>
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

            //close the socket
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