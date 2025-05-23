using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class FinalScreenManager : MonoBehaviour
{
    public Canvas finalCanvas;
    public Button linkButton;
    public string websiteURL;
    private bool finalScreenActive = false;

    void Start()
    {
        if (finalCanvas != null)
            finalCanvas.gameObject.SetActive(false);

        if (linkButton != null)
            linkButton.onClick.AddListener(OpenWebsite);

        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }
    }

    public void ShowFinalScreen()
    {
        finalScreenActive = true;

        if (finalCanvas != null)
            finalCanvas.gameObject.SetActive(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void OpenWebsite()
    {
        Debug.Log("Opening URL: " + websiteURL);
        Application.OpenURL(websiteURL);
    }
}