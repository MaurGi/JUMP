using UnityEngine;
using UnityEngine.UI;

namespace JUMP
{
    class JUMPStatusGameRoom : Photon.PunBehaviour
    {
        private Text GameRoomStatus = null;

        [SerializeField]
        private bool debugMode;

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
