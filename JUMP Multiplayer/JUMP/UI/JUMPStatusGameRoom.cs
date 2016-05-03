using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.UI;

namespace JUMP
{

    class JUMPStatusGameRoom : Photon.PunBehaviour
    {
        private Text GameRoomStatus = null;

        void Start()
        {
            GameRoomStatus = GetComponent<Text>();
        }

        void Update()
        {
            if (PhotonNetwork.inRoom)
            {
                GameRoomStatus.text = "Players in room : " + PhotonNetwork.room.playerCount;
            }
        }
    }
}
