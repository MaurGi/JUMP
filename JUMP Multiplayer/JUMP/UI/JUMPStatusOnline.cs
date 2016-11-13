using UnityEngine;
using UnityEngine.UI;

namespace JUMP
{
    class JUMPStatusOnline : Photon.PunBehaviour
    {
        private Text OnlineStatus = null;

#pragma warning disable 0649
        [SerializeField]
        private bool debugMode;
#pragma warning restore 0649

        void Start()
        {
            OnlineStatus = GetComponent<Text>();

            // Do not show informations when not in editor or debug mode
            if ((!Application.isEditor) && (!debugMode))
            {
                gameObject.SetActive(false);
            }
        }

        void Update()
        {
            if (OnlineStatus != null)
            {
                OnlineStatus.text = JUMPMultiplayer.IsPhotonOffline ? "Offline" : "Online";
            }
        }
    }
}
