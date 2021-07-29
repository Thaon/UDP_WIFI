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
        
        Debug.Log("Connected to Server!");
        _server.SendBool("Ping", true);
    }

    public override void OnData(string opCode, string payload)
    {
        base.OnData(opCode, payload);

        switch(opCode)
        {
            case "Ping":
                Debug.Log("Ping received");
                break;

            case "Pong":
                Debug.Log("Pong received");
                break;
        }
    }
}
