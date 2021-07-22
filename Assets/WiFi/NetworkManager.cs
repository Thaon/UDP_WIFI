﻿using UnityEngine;
using LocalNetworking;
using UnityEngine.UI;
using System;

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
        _server.OnConnect += OnConnect;
        _server.OnData += OnData;
    }

    private void OnConnect(string IP)
    {
        print(IP + " connected");
    }

    private void OnDestroy()
    {
        _server.OnConnect -= OnConnect;
        _server.OnData -= OnData;
    }

    private void OnData(string msg, string payload)
    {
        Vector3 pos = _server.ReadVector3(payload);
        _dot.GetComponent<Fader>()._alpha = 1f;
        _dot.transform.position = pos;
    }

    void Update()
    {
        if (_started)
        {
           if (Input.GetMouseButtonDown(0))
            {
                _server.SendVector3("DOT", Input.mousePosition);
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
