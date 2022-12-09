using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EscapeMenu : MonoBehaviour
{
    public WorldClass World;
    public GameObject Main;
    public GameObject Settings;

    [Header("Resolution")]
    public Slider Resolutin_slider;
    public Text Resolution;

    private int chosenResolution = 3;
    private int tempResolution = 4;

    [Header("GUI")]
    public Slider GUI_slider;
    public Text GUI;

    private int chosenGUI = 2;
    private int tempGUI = 2;

    [Header("Fullscreen")]
    public Toggle Fullscreen_toggle;

    private int chosenFullscreen = 1;
    private int tempFullscreen = 1;


    [Header("RenderType")]
    public Toggle Render_type_toggle;

    private RenderType chosenRenderType = RenderType.GreedyMeshing;
    private RenderType tempRenderType = RenderType.GreedyMeshing;


    [Header("RenderDistance")]
    public Slider Render_distance_slider;
    public Text Render_distance;

    private int chosenRenderDistance = 8;
    private int tempRenderDistance = 8;


    [Header("Gamma")]
    public Slider Gamma_slider;
    public Text Gamma;
    private int chosenGamma = 100;
    private int tempGamma = 100;

 

    List<Vector2Int> possibleResolutions = new List<Vector2Int>
    {
        new Vector2Int(640, 360),
        new Vector2Int(1280, 720),
        new Vector2Int(1536, 864),
        new Vector2Int(1600, 900),
        new Vector2Int(1920, 1080),
        new Vector2Int(2048, 1152),
        new Vector2Int(2560, 1440),
        new Vector2Int(3840, 2160)
    };

    List<FullScreenMode> possibleFullscreen = new List<FullScreenMode>
    {
        FullScreenMode.ExclusiveFullScreen,
        FullScreenMode.FullScreenWindow,
        FullScreenMode.MaximizedWindow,
        FullScreenMode.Windowed
    };

    


    void Start()
    {
        SetResolution(possibleResolutions[chosenResolution]);
        SetFullscreen(possibleFullscreen[chosenFullscreen]);
        Resolutin_slider.onValueChanged.AddListener( (v) =>
        {
            ChangeRes((int)System.Math.Round(v));
        });
        
        Fullscreen_toggle.onValueChanged.AddListener((v) =>
       {
           ChangeSceen(v);
       });

        GUI_slider.onValueChanged.AddListener((v) =>
        {
            ChangeGUI((int)System.Math.Round(v));
        });

        Render_type_toggle.onValueChanged.AddListener((v) =>
        {
            ChangeRenderType(v);
        });

        Render_distance_slider.onValueChanged.AddListener((v) =>
        {
            ChangeRender((int)System.Math.Round(v));
        });

        Gamma_slider.onValueChanged.AddListener((v) =>
        {
            ChangeGamma((int)System.Math.Round(v));
        });

    }

    public void ChangeRes(int number)
    {
        tempResolution = number;
       
        Resolution.text =
             possibleResolutions[tempResolution].x
             + "x"
             + possibleResolutions[tempResolution].y;

    }
    public void ChangeSceen(bool Scream)
    {
        if (Scream) tempFullscreen = 1;

        else tempFullscreen = 3;
    
        //SetFullscreen(possibleFullscreen[tempFullscreen]);
    }
    public void ChangeGUI(int number)
    {
        tempGUI = number;
        GUI.text = tempGUI.ToString();
    }

    public void ChangeRender(int number)
    {
        tempRenderDistance = number;
        Render_distance.text = tempRenderDistance.ToString();
    }

    public void ChangeRenderType(bool type)
    {
        if (type) tempRenderType = RenderType.Instancing;
        else tempRenderType = RenderType.GreedyMeshing;
    }

    public void ChangeGamma(int number)
    {
        tempGamma = number;
        int temp = number - 100;
        Gamma.text = temp + "%";
    }

    public void ApplyGamma()
    {
        chosenGamma = tempGamma;
    }
    public void  ApplyGUI()
    {
        chosenGUI = tempGUI;
    }
    public void ApplyRender()
    {
        chosenRenderDistance = tempRenderDistance;
        World.SetRenderDistance(chosenRenderDistance);
    }

    public void ApplyRenderType()
    {
        chosenRenderType = tempRenderType;
        World.SetRenderType(chosenRenderType);
    }

    public void ApplyChanges()
    {
        chosenFullscreen = tempFullscreen;
        chosenResolution = tempResolution;
        SetResolution(possibleResolutions[chosenResolution]);
        SetFullscreen(possibleFullscreen[tempFullscreen]);
        ApplyGamma();
        ApplyGUI();
        ApplyRender();
        ApplyRenderType();
        ExitSettings();
    }
    public void SetFullscreen(FullScreenMode fullScreenMode)
    {
        Screen.fullScreenMode = fullScreenMode;
    }

    public void SetResolution(Vector2Int size)
    {
        Screen.SetResolution(size.x, size.y, Screen.fullScreen);

    }

    public void EnterSettings()
    {
        Main.SetActive(false);
        Settings.SetActive(true);
        Resolutin_slider.value = chosenResolution;
        Fullscreen_toggle.isOn = chosenFullscreen == 1 ? true : false;
        GUI_slider.value = chosenGUI;
        Render_distance_slider.value = chosenRenderDistance;
        Gamma_slider.value = chosenGamma;
    }

    public void ExitSettings()
    {
        Main.SetActive(true);
        Settings.SetActive(false);

    }

    public void ExitMenu()
    {
        this.gameObject.SetActive(false);
    }

    public void EnterMenu()
    {
        this.gameObject.SetActive(true);
        Main.SetActive(true);
        Settings.SetActive(false);
    }
    public bool IsActive()
    {
        return this.gameObject.activeSelf;
    }

    public void MainMenu()
    {
        Loader.Load(Loader.Scene.MainMenu);
    }
    public void ExitGame()
    {
        Application.Quit();
    }
}
