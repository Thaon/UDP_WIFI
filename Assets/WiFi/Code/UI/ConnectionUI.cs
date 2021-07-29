using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConnectionUI : MonoBehaviour
{
    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 50, 30), "HOST"))
        {
            FindObjectOfType<LocalNetworking.NetworkManager>().Host();
            Destroy(this);
        }

        if (GUI.Button(new Rect(70, 10, 50, 30), "JOIN"))
        {
            FindObjectOfType<LocalNetworking.NetworkManager>().Join();
            Destroy(this);
        }
    }
}
