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

        public virtual void Init()
        {
            _server = GetComponent<Server>();
            _server.OnConnect += OnConnect;
            _server.OnData += OnData;
        }


        public virtual void Shutdown()
        {
            _server.OnConnect -= OnConnect;
            _server.OnData -= OnData;
        }

        public virtual void OnConnect(string IP)
        {
        }

        public virtual void OnData(string msg, string payload)
        {
        }

        public void Host()
        {
            if (!_started)
            {
                _started = true;
                _server.StartNetworking(true);
            }
        }

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