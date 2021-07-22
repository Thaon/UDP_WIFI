using UnityEngine;
using LocalNetworking;
using UnityEngine.UI;

public class NetworkManager : MonoBehaviour
{
    #region member variables

    public Server _server;
    public GameObject _UI;
    public Image _dot;

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
        string[] positions = payload.Split(',');
        Vector3 pos = new Vector3(int.Parse(positions[0]), int.Parse(positions[1]), int.Parse(positions[2]));
        _dot.GetComponent<Fader>()._alpha = 1f;
        _dot.transform.position = pos;
    }

    void Update()
    {
        if (_started)
        {
           if (Input.GetMouseButtonDown(0))
            {
                string pos = "";
                pos += Mathf.Round(Input.mousePosition.x).ToString() + ",";
                pos += Mathf.Round(Input.mousePosition.y).ToString() + ",";
                pos += Mathf.Round(Input.mousePosition.z).ToString();
                _server.Send(new Message("DOT", pos));
            }
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
