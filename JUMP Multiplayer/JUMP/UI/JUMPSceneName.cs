using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class JUMPSceneName : MonoBehaviour
{
    private Text text;

#pragma warning disable 0649
    [SerializeField]
    private bool debugMode;
#pragma warning restore 0649

    private void Awake()
    {
        text = GetComponent<Text>();
        text.text = SceneManagerHelper.ActiveSceneName;

        // Do not show informations when not in editor or debug mode
        if ((!Application.isEditor) && (!debugMode))
        {
            gameObject.SetActive(false);

            return;
        }
    }
}