using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JUMP
{

    class JUMPStatusPlayers : Photon.PunBehaviour
    {
        private Text PlayerStatus = null;

#pragma warning disable 0649
        [SerializeField]
        private bool debugMode;
#pragma warning restore 0649

        void Start()
        {
            PlayerStatus = GetComponent<Text>();

            // Do not show informations when not in editor or debug mode
            if ((!Application.isEditor) && (!debugMode))
            {
                gameObject.SetActive(false);
            }
        }

        void Update()
        {
            PlayerStatus.text = string.Format("Players: {0} Playing, {1} Total", PhotonNetwork.countOfPlayersInRooms, PhotonNetwork.countOfPlayers);
        }
    }
}
