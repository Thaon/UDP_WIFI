using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ConnectionUI : MonoBehaviour
{
    public UnityEvent OnHost, OnJoin;

    private void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 50, 30), "HOST"))
        {
            OnHost?.Invoke();
            Destroy(this);
        }

        if (GUI.Button(new Rect(70, 10, 50, 30), "JOIN"))
        {
            OnJoin?.Invoke();
            Destroy(this);
        }
    }
}
