using UnityEngine;
using UnityEngine.UI;

namespace JUMP
{
    class JUMPStatusConnection : Photon.PunBehaviour
    {
        private Text ConnectionStatus = null;

#pragma warning disable 0649
        [SerializeField]
        private bool debugMode;
#pragma warning restore 0649

        void Start()
        {
            ConnectionStatus = GetComponent<Text>();

            // Do not show informations when not in editor or debug mode
            if ((!Application.isEditor) && (!debugMode))
            {
                gameObject.SetActive(false);
            }
        }

        void Update()
        {
            if (ConnectionStatus != null)
            {
                ConnectionStatus.text = "Photon connection status: " + PhotonNetwork.connectionStateDetailed.ToString();
            }
        }
    }
}
