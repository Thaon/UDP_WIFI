using System.Net.Sockets;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace LocalNetworking
{
    [RequireComponent(typeof(Server))]
    public class NetworkManager : MonoBehaviour
    {
        #region member variables

        protected Server _server;
        protected bool _started = false;

        #endregion

        /// <summary>
        /// Starts the server and binds the event listeners
        /// </summary>
        public void Init()
        {
            _server = GetComponent<Server>();
            _server.OnConnection += OnConnect;
            _server.OnData += OnData;
            _server.OnClientDisconnect += OnClientDisconnect;
            _server.OnServerShutdown += OnServerShutdown;
        }

        /// <summary>
        /// Unbinds the event listeners
        /// </summary>
        public void Shutdown()
        {
            _server.OnConnection -= OnConnect;
            _server.OnData -= OnData;
            _server.OnClientDisconnect -= OnClientDisconnect;
            _server.OnServerShutdown -= OnServerShutdown;
        }

        /// <summary>
        /// Starts the Networking process as a Host
        /// </summary>
        public void Host()
        {
            if (!_started)
            {
                _started = true;
                _server.Host();
            }
        }

        /// <summary>
        /// Starts the Networking process as a Client
        /// </summary>
        public void Join()
        {
            if (!_started)
            {
                _started = true;
                _server.Join();
            }
        }

        #region events

        /// <summary>
        /// Called when a client connects to the server
        /// </summary>
        /// <param name="connection"></param>
        public virtual void OnConnect(Socket connection)
        {
        }

        /// <summary>
        /// Called whenever a packet of data is received from the server
        /// </summary>
        /// <param name="message"></param>
        public virtual void OnData(Server.Message message)
        {
        }

        /// <summary>
        /// called whenever a client is disconnected
        /// </summary>
        public virtual void OnClientDisconnect(Socket connection)
        {
        }

        /// <summary>
        /// Called when the server closes the connection with the clients
        /// </summary>
        public virtual void OnServerShutdown()
        {
            Shutdown();
        }

        #endregion
    }
}