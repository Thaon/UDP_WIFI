using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Fader : MonoBehaviour
{
    public float _alpha = 1f;

    void Update()
    {
        _alpha = Mathf.Lerp(_alpha, 0, .05f);
        GetComponent<Image>().color = new Color(1, 1, 1, _alpha);
    }
}
