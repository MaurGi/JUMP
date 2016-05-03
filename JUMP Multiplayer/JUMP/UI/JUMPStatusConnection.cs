using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.UI;

namespace JUMP
{

    class JUMPStatusConnection : Photon.PunBehaviour
    {
        private Text ConnectionStatus = null;

        void Start()
        {
            ConnectionStatus = GetComponent<Text>();
        }

        void Update()
        {
            if (ConnectionStatus != null)
            {
                ConnectionStatus.text = "Connection status: " + PhotonNetwork.connectionStateDetailed.ToString();
            }
        }
    }
}
