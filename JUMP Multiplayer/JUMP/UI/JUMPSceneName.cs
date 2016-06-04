using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class JUMPSceneName : MonoBehaviour
{
    private Text text;

    [SerializeField]
    private bool debugMode;

    private void Awake()
    {
        text = GetComponent<Text>();
        text.text = SceneManager.GetActiveScene().name;

        // Do not show informations when not in editor or debug mode
        if ((!Application.isEditor) && (!debugMode))
        {
            gameObject.SetActive(false);

            return;
        }
    }
}