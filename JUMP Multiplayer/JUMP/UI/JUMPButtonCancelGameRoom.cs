using UnityEngine;
using UnityEngine.UI;
using JUMP;

public class JUMPButtonCancelGameRoom : MonoBehaviour
{
    private Button button;
    
	void Start()
    {
        button = GetComponent<Button>();
    }
	
	void Update()
    {
        if (button != null)
        {
            button.interactable = (JUMPMultiplayer.IsConnectedToGameRoom) && (!JUMPMultiplayer.IsPlayingGame);
        }
	}
}
