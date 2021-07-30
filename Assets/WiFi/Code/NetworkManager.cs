using UnityEngine;
using System.Collections.Generic;

namespace LocalNetworking
{
    [RequireComponent(typeof(Server))]
    public class NetworkManager : MonoBehaviour
    {
        #region member variables

        public List<GameObject> _prefabs;

        protected Server _server;
        protected bool _started = false;
        protected uint _spawnedObjectsTotal;

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
        /// Called when a client connects to the server
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
            if (opCode == "#SPAWN#")
            {
                //if (_server.IsHost()) return;

                int objectID = int.Parse(payload.Split('~')[0]);
                Vector3 pos = _server.ReadVector3(payload.Split('~')[1]);
                Quaternion rot = Quaternion.Euler(_server.ReadVector3(payload.Split('~')[2]));

                //instantiate and configure on clients
                GameObject spawned = Instantiate(_prefabs[objectID], pos, rot);
                NetBehaviour snb = spawned.GetComponent<NetBehaviour>();
                snb.ID = _spawnedObjectsTotal;
                snb.hasAuthority = true;
                _spawnedObjectsTotal++;
            }
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

        public NetBehaviour Spawn(GameObject go, Vector3 position, Quaternion rotation)
        {
            NetBehaviour nb = go.GetComponentInChildren<NetBehaviour>();
            if (nb == null)
            {
                Debug.LogWarning("You need a NetBehaviour to spawn this object!");
                return null;
            }

            //instantiate locally
            GameObject spawned = Instantiate(go, position, rotation);
            //configure locally
            NetBehaviour snb = spawned.GetComponent<NetBehaviour>();
            snb.ID = _spawnedObjectsTotal;
            snb.hasAuthority = true;
            _spawnedObjectsTotal++;

            //instantiate on the network
            int obj = _prefabs.FindIndex(o => o == go);

            string pos = "";
            pos += Mathf.Round(position.x).ToString() + ",";
            pos += Mathf.Round(position.y).ToString() + ",";
            pos += Mathf.Round(position.z).ToString();

            string rot = "";
            rot += Mathf.Round(rotation.eulerAngles.x).ToString() + ",";
            rot += Mathf.Round(rotation.eulerAngles.y).ToString() + ",";
            rot += Mathf.Round(rotation.eulerAngles.z).ToString();

            _server.Send(new Message("#SPAWN#", obj.ToString() + "~" + pos + "~" + rot));
            return nb;
        }
    }
}