using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DebugControler : MonoBehaviour
{
    bool ShowConsole = false;
    PlayerInput inputs;
    string input;
    // Start is called before the first frame update
    
    public void OnToggleDebug()
    {
        ShowConsole = !ShowConsole;
        controller.IsConsoleOpenned = ShowConsole;
    }
    GameObject player;
    PlayerController controller;
    private void OnGUI()
    {
        if (!ShowConsole) return;

        float y=0f;
        GUI.Box(new Rect(0, y, Screen.width, 30), "");
        GUI.backgroundColor = new Color(0, 0, 0, 0);
        input = GUI.TextField(new Rect(10f, y + 5f, Screen.width - 20f, 20f), input);
    }
    void Start()
    {
        inputs = new PlayerInput();
        inputs.Player.Console.started += Console_started;
        inputs.Player.Confirm.started += Confirm_Command;
        inputs.Enable();
        player = GameObject.Find("Player");
        controller = player.GetComponent<PlayerController>();
    }
    private void Console_started(UnityEngine.InputSystem.InputAction.CallbackContext obj) => OnToggleDebug();
    private void Confirm_Command(UnityEngine.InputSystem.InputAction.CallbackContext obj) => HandlingInput();
    // Update is called once per frame
    void Update()
    {

    }

    private void HandlingInput()
    {
        string[] Inputs= input.Split(' ');

        if(Inputs[0].CompareTo("SpawnItem")==0)
        {
            if(Inputs.Length==4)
            {
                int amount;
                int Times;
                int.TryParse(Inputs[2], out amount);
                int.TryParse(Inputs[2], out Times);
                ItemSpawn(Inputs[1], amount, Times);
            }
            if (Inputs.Length == 3)
            {
                int amount;
                int.TryParse(Inputs[2], out amount);
                ItemSpawn(Inputs[1], amount);
            }

        }
        if (Inputs[0].CompareTo("SpawnEnemy") == 0)
        {
            if (Inputs.Length == 3)
            {
                int amount;
                int.TryParse(Inputs[2], out amount);
                EnemySpawn(Inputs[1], amount);
            }
            else if (Inputs.Length==2)
            {
                EnemySpawn(Inputs[1]);
            }
        }
        input = "";

    }

    public GameObject itempickup;
    public void ItemSpawn(string itemname, int Amount)
    {
        Item item = ItemMenager.GetItem(itemname);
        if (item != null)
        {
            GameObject a = Instantiate(itempickup) as GameObject;
            a.GetComponent<ItemFizician>().item = item;
            a.GetComponent<ItemFizician>().amount = Amount;
            a.transform.position = player.transform.position + new Vector3(0f, 1f, 0);
            Debug.Log("Spawned " + item.ItemName);
        }
        else
        {
            Debug.Log("Didn't Find " + itemname);
        }
    }

    public void EnemySpawn(string EnemyName)
    {
        Object enemy = EnemyMenager.GetEnemy(EnemyName);
        if(enemy!=null)
        {
            GameObject a = Instantiate(enemy) as GameObject;
            a.transform.position = player.transform.position + new Vector3(Random.Range(-1f,1f), 1f, Random.Range(-1f, 1f));
            Debug.Log("Spawned " + enemy.name);
        }
        else
        {
            Debug.Log("Didn't Find " + enemy.name);
        }
    }

    public void EnemySpawn(string EnemyName, int Amount)
    {
        for (int i = 0; i < Amount; i++)
        {
            EnemySpawn(EnemyName);
        }
    }
    public void ItemSpawn(string itemname, int Amount, int Times)
    {
        for (int i = 0; i < Times; i++)
        {
            ItemSpawn(itemname, Amount);
        }
    }

}
