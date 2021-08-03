using System.Net;
using System.Net.Sockets;
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
        if (_started && Input.GetKeyDown(KeyCode.Space))
        {
            print("sending");
            _server.Send("msg", "Message!");
        }
    }

    public override void OnConnect(Socket connection)
    {
        base.OnConnect(connection);

        IPEndPoint endPoint = (IPEndPoint)connection.RemoteEndPoint;
        
        Debug.LogError("Client: "+ endPoint.Address.ToString() +" Connected to the Server!");
    }

    public override void OnData(Server.Message message)
    {
        base.OnData(message);

        Debug.LogError(message._msg);

        switch (message._opCode)
        {
            case "msg":
                Debug.Log(message._msg);
                break;
        }
    }

    public override void OnServerShutdown()
    {
        base.OnServerShutdown();

        Debug.LogError("Server shut down");
    }
}
