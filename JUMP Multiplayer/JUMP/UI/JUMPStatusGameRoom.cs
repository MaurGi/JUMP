using UnityEngine;
using UnityEngine.UI;

namespace JUMP
{
    class JUMPStatusGameRoom : Photon.PunBehaviour
    {
        private Text GameRoomStatus = null;

#pragma warning disable 0649
        [SerializeField]
        private bool debugMode;
#pragma warning restore 0649

        void Start()
        {
            GameRoomStatus = GetComponent<Text>();

            // Do not show informations when not in editor or debug mode
            if ((!Application.isEditor) && (!debugMode))
            {
                gameObject.SetActive(false);
            }
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
