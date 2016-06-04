using UnityEngine;
using UnityEngine.UI;

namespace JUMP
{
    class JUMPStatusOnline : Photon.PunBehaviour
    {
        private Text OnlineStatus = null;

        [SerializeField]
        private bool debugMode;

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
                OnlineStatus.text = JUMPMultiplayer.IsOffline ? "Offline" : "Online";
            }
        }
    }
}
