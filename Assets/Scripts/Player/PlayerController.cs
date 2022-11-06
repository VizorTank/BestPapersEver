using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    // World
    public WorldClass worldClass;
    public bool IsConsoleOpenned = false;
    // Player Input
    public PlayerInput playerInput;
    private Vector3 input;
    public float scroll;

    // Player Movement
    public float speed = 10;
    public float sprintSpeed = 10;
    public float jumpHeight = 2;
    public float gravity = -9.8f;
    public float playerHeight = 2f;
    public float playerWidth = 0.3f;
    public Vector3 movement;
    public Vector3 velocity;
    public float upSpeed = 0;
    public bool noClip = true;
    public bool isSprinting;

    // Input Flags
    public bool isGrounded;
    public bool placeBlock;
    public bool destroyBlock;

    // Camera management
    public Transform Camera;
    public float mouseSensitivity = 100f;
    private Vector2 mouseInput;
    private float yRotation = 0;

    // Block Placing/Destroying
    public Transform blockPlacement;
    public Transform HighlightBlock;
    public Transform HighlightPlaceBlock;
    public Text BlockDisplay;
    public float checkIncrement = 0.1f;
    public float reach = 8f;
    public int placingBlockID = 1;

    public Player_HitResponder HitResponder;
    public CharacterController controller;
    public BarSystem barSystem;
    public BuildingBlocked buildingBlocked;
    public GameObject AtackHitbox;

    bool placeStructure;

    int[,] tab;
    //Inventory

    private void Awake()
    {
        BindInput();
        ItemMenager.GetInstance();
        RecipeMenager.GetInstance();
        EnemyMenager.GetInstance();
    }
    private void Start()
    {
        HitResponder = transform.GetComponentInChildren<Player_HitResponder>();
        controller = transform.GetComponent<CharacterController>();
        inventory = InventorySys.GetComponent<Inventory>();
        toolbar = inventory.Toolbar.GetComponent<Toolbar>();
        Backpack = inventory.Backpack;
        cursorSlot = inventory.cursorSlot;
        toolbar.playerController = this;
        barSystem.SetMaxStats(controller.CharacterStats.MaxHealth, controller.CharacterStats.MaxStamina);
    }
    private void BindInput()
    {
        playerInput = new PlayerInput();


        playerInput.Player.PlaceBlock.started += PlaceBlock_started;
        playerInput.Player.DestroyBlock.started += DestroyBlock_started;
        playerInput.Player.NoClip.started += NoClip_started;
        playerInput.Player.Inventory.started += Inventory_started;

        playerInput.Player.PlaceStructure.started += PlaceStructure_started;
        playerInput.Player.EscapeMenu.started += EscapeMenuStarded;

        playerInput.Player.TestKey1.started += TestKey1_started;
        playerInput.Player.TestKey2.started += TestKey2_started;
    }

    private void TestKey2_started(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        testSaving();
    }

    /// <summary>
    /// /
    /// </summary>
    /// <param name="obj"></param>
    private void TestKey1_started(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        testLoading();
    }

    private void NoClip_started(UnityEngine.InputSystem.InputAction.CallbackContext obj) => noClip = !noClip;
    private void Inventory_started(UnityEngine.InputSystem.InputAction.CallbackContext obj) => inInventory = !inInventory;

    private void PlaceBlock_started(UnityEngine.InputSystem.InputAction.CallbackContext obj) => UseItem();
    private void DestroyBlock_started(UnityEngine.InputSystem.InputAction.CallbackContext obj) => HandleLeftClick();


    private void PlaceStructure_started(UnityEngine.InputSystem.InputAction.CallbackContext obj) => placeStructure = true;

    private void EscapeMenuStarded(UnityEngine.InputSystem.InputAction.CallbackContext obj) => EscapeSys();
    private void OnEnable() => playerInput.Enable();
    private void OnDisable() => playerInput.Disable();

    int atackdowdtime;
    void Update()
    {
        if(EscapeMenuGO.IsActive()||IsConsoleOpenned||inInventory)
                {
            Cursor.lockState = CursorLockMode.None;
        }
        else 
        { 
            
            PlaceCursorBlock();
            GetInput();
            SetRotation();
            SelectBlock();
            Cursor.lockState = CursorLockMode.Locked;
        }
        controller.GetMovement(input, isSprinting);
        SetArmor();
        barSystem.SetHealth(controller.CharacterStats.CurrentHealth);
        barSystem.SetStamina(controller.CharacterStats.CurrentStamina);

    }


    private void testSaving()
    {
        tab = inventory.GetItemsForSaving();
        inventory.TempClearInventory();
    }

    private void testLoading()
    {
        inventory.LoadItems(tab);
    }

    private void FixedUpdate()
    {
        if (HitResponder.Atack)
        {
            atackdowdtime++;
        }
        if (atackdowdtime >= 50)
        {
            HitResponder.Atack = false;
        }
    }
    void GetInput()
    {
        Vector2 vector2 = playerInput.Player.Move.ReadValue<Vector2>();
        input.x = vector2.x;
        input.z = vector2.y;
        input.y = playerInput.Player.Jump.ReadValue<float>();

        isSprinting = playerInput.Player.Sprint.ReadValue<float>() != 0;
        scroll = playerInput.Player.SelectHotbarSlot.ReadValue<float>();

        mouseInput = playerInput.Player.Look.ReadValue<Vector2>();
        
    }

    public void OnDestroy()
    {
        EnemyMenager.Destroy();
        ItemMenager.Destroy();
        RecipeMenager.Destroy();
    }
    private void HandleLeftClick()
    {
        if (SelectedItem is Weapon)
        {
            AtackHitbox.SetActive(true);
            HitResponder.Damage = ((Weapon)SelectedItem).AtackDamage;
            HitResponder.Atack = true;
            atackdowdtime = 0;
        }
        else
        {
            DestroyBlock();
            AtackHitbox.SetActive(false);
        }
    }

    private void SelectBlock()
    {
        if (HighlightBlock.gameObject.activeSelf)
        {
            int a = 0;
            if (destroyBlock)
            {
                worldClass.TrySetBlock(HighlightBlock.position, 0, ref a);
                destroyBlock = false;
            }
            if (placeBlock)
            {
                worldClass.TrySetBlock(HighlightPlaceBlock.position, placingBlockID, ref a);
                placeBlock = false;
            }
            if (placeStructure)
            {
                worldClass.CreateStructure(HighlightPlaceBlock.position - new Vector3(2, 0, 2), 0);
                placeStructure = false;
            }
        }
    }

    public void DestroyBlock()
    {
        if (!IsConsoleOpenned)
            if (!inInventory)
                if (HighlightBlock.gameObject.activeSelf)
                {
                    int blockDestroyed = 0;
                    worldClass.TrySetBlock(HighlightBlock.position, 0, ref blockDestroyed);
                }
    }
    public void PlaceBlock(int BlockID)
    {
        worldClass.TryPlaceBlock(HighlightPlaceBlock.position, BlockID);
        placeBlock = false;
    }

    private void PlaceCursorBlock()
    {
        float step = checkIncrement;

        Vector3 lastPos = Camera.position;

        while (step < reach)
        {
            Vector3 pos = Camera.position + Camera.forward * step;
            int blockId = 0;
            if (worldClass.TryGetBlock(pos, ref blockId) && blockId != 0)
            {
                if (worldClass.blockTypesList.areSolid[worldClass.GetBlock(pos)])
                {
                    HighlightBlock.position = new Vector3(Mathf.Floor(pos.x), Mathf.Floor(pos.y), Mathf.Floor(pos.z));
                    HighlightBlock.gameObject.SetActive(true);

                    HighlightPlaceBlock.position = new Vector3(Mathf.Floor(lastPos.x), Mathf.Floor(lastPos.y), Mathf.Floor(lastPos.z));
                    HighlightPlaceBlock.gameObject.SetActive(SelectedItem is Placeable);
                    return;

                }

                
            }
            lastPos = pos;
            step += checkIncrement;
        }
        HighlightBlock.gameObject.SetActive(false); 
        HighlightPlaceBlock.gameObject.SetActive(false);
    }


    void SetRotation()
    {
        transform.Rotate(Vector3.up * mouseInput.x * mouseSensitivity);

        yRotation -= mouseInput.y * mouseSensitivity;
        yRotation = Mathf.Clamp(yRotation, -90f, 90f);
        Camera.localRotation = Quaternion.Euler(yRotation, 0f, 0f);
        //Camera.Rotate(Vector3.right * -mouseInput.y);
    }


    [Header("InventoryUI")]
    public GameObject itempickup;
    public GameObject InventorySys;
    private GameObject Backpack;
    private GameObject cursorSlot;
    private Toolbar toolbar;
    public bool _inInventory = false;
    public bool _inContainer = false;
    public bool inContainer = false;
    private Inventory inventory;
    [SerializeField] private EscapeMenu EscapeMenuGO;


    public void EscapeSys()
    {
        if (EscapeMenuGO.IsActive())
        {
            EscapeMenuGO.ExitMenu();
        }
        else EscapeMenuGO.EnterMenu();
    }
    public bool inInventory
    {
        get { return _inInventory; }

        set
        {
            if (!IsConsoleOpenned)
            {
                _inInventory = value;
                if (_inInventory)
                {
                    Cursor.lockState = CursorLockMode.None;
                    inventory.OpenInventory();
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    inventory.CloseInventory();
                }
            }

        }
    }

    public ItemStack PickUpItem(ItemStack item)
    {
        return inventory.PickUpItem(item);
        
    }
    Item SelectedItem;
    public Item SelectItem { set => SelectedItem = value; }
    public void UseItem()
    {
        if (!IsConsoleOpenned&&!inInventory)
        {
            if(SelectedItem!=null)
            if(SelectedItem.itemtype == Itemtype.Building)
                    if(buildingBlocked.CheckBuilding())
                        toolbar.UseItem(HighlightPlaceBlock.gameObject.activeSelf);
        }
    }
    
    public void SetArmor()
    {
        
        Stats def = inventory.GetItemStats().Find(x => x.StatName == StatEnum.Armor);
        if(def!=null)
        controller.CharacterStats.SetArmor((int)def.Value);
        else
            controller.CharacterStats.SetArmor(0);
    }


}


