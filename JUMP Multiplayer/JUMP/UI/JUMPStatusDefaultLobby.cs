using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.UI;

namespace JUMP
{

    class JUMPStatusDefaultLobby : Photon.PunBehaviour
    {
        private Text DefaultLobbyStatus = null;

        void Start()
        {
            DefaultLobbyStatus = GetComponent<Text>();
        }

        void Update()
        {
            if (PhotonNetwork.EnableLobbyStatistics)
            {
                List<TypedLobbyInfo> info = PhotonNetwork.LobbyStatistics;
                TypedLobbyInfo lobby = info.Find(l => l.IsDefault == true);
                if (lobby != null)
                {
                    DefaultLobbyStatus.text = "Online players [Default Lobby]: " + lobby.PlayerCount.ToString();
                }
            }
        }
    }
}
