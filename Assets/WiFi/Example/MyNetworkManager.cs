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

    public override void OnConnect(string IP)
    {
        base.OnConnect(IP);
    }

    public override void OnData(string msg, string payload)
    {
        base.OnData(msg, payload);
    }
}
