using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.UI;

namespace JUMP
{

    class JUMPStatusOnline : Photon.PunBehaviour
    {
        private Text OnlineStatus = null;

        void Start()
        {
            OnlineStatus = GetComponent<Text>();
        }

        void Update()
        {
            if (OnlineStatus != null)
            {
                OnlineStatus.text = JUMPMultiplayer.IsOffline ? "Offline" : "Online";
            }
        }
    }
}
