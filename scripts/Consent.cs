using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ConsentManager : MonoBehaviour
{
    [Header("Screens")]
    public GameObject[] screens;
    private int currentScreenIndex = 0;

    [Header("Consents")]
    public TMP_InputField phoneLastDigitsInput;
    public Button acceptButton;

    [Header("Scenes")]
    public string scene1Name = "ScenePP1";
    public string scene2Name = "ScenePP3";

    public static int listIndex;

    private void Start()
    {
        for (int i = 0; i < screens.Length; i++)
            screens[i].SetActive(i == 0);

        if (acceptButton != null)
        {
            acceptButton.interactable = false;
            acceptButton.onClick.AddListener(OnConsentButtonClick);
        }

        if (phoneLastDigitsInput != null)
            phoneLastDigitsInput.onValueChanged.AddListener(ValidatePhoneDigits);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            NextScreen();
    }

    public void NextScreen()
    {
        if (currentScreenIndex < screens.Length - 1)
        {
            screens[currentScreenIndex].SetActive(false);
            currentScreenIndex++;
            screens[currentScreenIndex].SetActive(true);
        }
    }

    private void ValidatePhoneDigits(string text)
    {
        bool valid = (text.Length == 2 && IsNumeric(text));
        acceptButton.interactable = valid;

        if (valid)
        {
            listIndex = int.Parse(text);
        }
    }

    private bool IsNumeric(string text)
    {
        foreach (char c in text)
            if (!char.IsDigit(c))
                return false;
        return true;
    }

    public void OnConsentButtonClick()
    {
        int randomSceneIndex = Random.Range(0, 2);
        string sceneToLoad = (randomSceneIndex == 0) ? scene1Name : scene2Name;

        SceneManager.LoadScene(sceneToLoad);
    }
}