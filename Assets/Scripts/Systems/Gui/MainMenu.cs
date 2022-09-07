using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenu : MonoBehaviour
{

    [SerializeField] GameObject EntryScreen; 
    [SerializeField] GameObject LoadWorld; 
    [SerializeField] GameObject Settings; 
    [SerializeField] GameObject Authors; 
    public void StartGame()
    {
        Loader.Load(Loader.Scene.GameScene);
    }


    public void LeaveGame()
    {
        Application.Quit();
    }


    public void Start()
    {
        EnterEntry();
    }
    public void EnterSettings()
    {
        EntryScreen.SetActive(false);
        LoadWorld.SetActive(false);
        Settings.SetActive(true);
        Authors.SetActive(false);
    }
    public void EnterEntry()
    {
        EntryScreen.SetActive(true);
        LoadWorld.SetActive(false);
        Settings.SetActive(false);
        Authors.SetActive(false);
    }    
    public void EnterAuthors()
    {
        EntryScreen.SetActive(false);
        LoadWorld.SetActive(false);
        Settings.SetActive(false);
        Authors.SetActive(true);
    }
    public void EnterWorlds()
    {
        EntryScreen.SetActive(false);
        LoadWorld.SetActive(true);
        Settings.SetActive(false);
        Authors.SetActive(false);
    }


}

