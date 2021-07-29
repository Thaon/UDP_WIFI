using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LocalNetworking;

public class MyNetworkManager : NetworkManager
{
    void Awake()
    {
        Init();
    }

    void OnDestroy()
    {
        Shutdown();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && _server.IsStarted()) _server.SendBool("Pong", true);
    }

    public override void OnConnect(string IP)
    {
        base.OnConnect(IP);
        _server.SendBool("Ping", true);
    }

    public override void OnData(string msg, string payload)
    {
        base.OnData(msg, payload);
    }
}
