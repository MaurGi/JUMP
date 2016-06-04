using JUMP;
using UnityEngine;
using UnityEngine.UI;

public class JumpButtonStartMatchmaking : MonoBehaviour
{
    private Button button;

	// Use this for initialization
	void Start()
    {
        button = GetComponent<Button>();
	}
	
	// Update is called once per frame
	void Update()
    {
	    if (button != null)
        {
            button.interactable = (JUMPMultiplayer.IsConnectedToMaster) && (!JUMPMultiplayer.IsOffline);
        }
	}
}