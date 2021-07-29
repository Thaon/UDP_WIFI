using UnityEngine;
using LocalNetworking;

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
        public virtual void Init()
        {
            _server = GetComponent<Server>();
            _server.OnConnect += OnConnect;
            _server.OnData += OnData;
        }

        /// <summary>
        /// Unbinds the event listeners
        /// </summary>
        public virtual void Shutdown()
        {
            _server.OnConnect -= OnConnect;
            _server.OnData -= OnData;
        }

        /// <summary>
        /// Called when a connection with the server is established
        /// </summary>
        /// <param name="IP">The IP address that the Server is on</param>
        public virtual void OnConnect(string IP)
        {
        }

        /// <summary>
        /// Called whenever a packet of data is received from the server
        /// </summary>
        /// <param name="opCode">A string that represent the operation to perform</param>
        /// <param name="payload">The data associated with the operation, can be null</param>
        public virtual void OnData(string opCode, string payload = null)
        {
        }

        /// <summary>
        /// Starts the Networking process as a Host
        /// </summary>
        public void Host()
        {
            if (!_started)
            {
                _started = true;
                _server.StartNetworking(true);
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
                _server.StartNetworking(false);
            }
        }
    }
}