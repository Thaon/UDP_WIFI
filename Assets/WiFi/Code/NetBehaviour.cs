using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LocalNetworking
{
    public class NetBehaviour : MonoBehaviour
    {
        #region member variables

        [HideInInspector]
        public uint ID;
        [HideInInspector]
        public bool hasAuthority;

        #endregion

        public virtual void OnStartAuthority() { }

    }
}