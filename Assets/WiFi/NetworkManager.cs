using UnityEngine;
using LocalNetworking;

public class NetworkManager : MonoBehaviour
{
    #region member variables

    public Server _server;
    public GameObject _UI;

    private bool _started = false;

    #endregion

    void Start()
    {
        _server.OnData += OnData;
    }

    private void OnDestroy()
    {
        _server.OnData -= OnData;
    }

    private void OnData(string msg, string payload)
    {
        Debug.LogError(msg + " - " + payload);
    }

    void Update()
    {
        if (_started)
        {
           if (Input.GetMouseButtonDown(0))
                _server.Send(new Message("PING", "Hello!"));
        }
    }

    public void StartHost()
    {
        if (!_started)
        {
            _started = true;
            _server.StartNetworking(true);
            _UI.SetActive(false);
        }
    }

    public void Join()
    {
        if (!_started)
        {
            _started = true;
            _server.StartNetworking(false);
            _UI.SetActive(false);
        }
    }
}
