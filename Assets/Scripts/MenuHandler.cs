using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuHandler : MonoBehaviour
{
    public TMP_InputField text;

    public static string username = "balls420";
    
    public Button playButton;
    public Button guideButton;
    public Button quitButton;

    private void Start()
    {
        playButton.onClick.AddListener(OnPlayClick);
        guideButton.onClick.AddListener(OnGuideClick);
        quitButton.onClick.AddListener(OnQuitClick);
    }

    private void OnPlayClick()
    {
        if (text.text != "")
        {
            username = text.text;
        }
        SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
    }
    
    private void OnGuideClick()
    {
        print("guide");
    }

    private void OnQuitClick()
    {
        print("quit");
        Application.Quit();
    }
}
