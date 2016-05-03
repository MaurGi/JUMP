using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using JUMP;

public class JUMPButtonCancelGameRoom : MonoBehaviour {

    private Button button;
	// Use this for initialization
	void Start () {
        button = this.GetComponent<Button>();
    }
	
	// Update is called once per frame
	void Update () {
        if (button != null)
        {
            button.interactable = (JUMPMultiplayer.IsConnectedToGameRoom) && (!JUMPMultiplayer.IsPlayingGame);
        }
	}
}
