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
    public bool noClip = false;
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
    
    
    public CharacterController controller;
    public CharacterStats characterStats;


    bool placeStructure;
    //Inventory

    private void Awake()
    {
        BindInput();
    }
    private void Start()
    {
        controller = transform.GetComponent<CharacterController>();
        inventory = InventorySys.GetComponent<Inventory>();
        toolbar = inventory.Toolbar.GetComponent<Toolbar>();
        Backpack = inventory.Backpack;
        cursorSlot = inventory.cursorSlot;
        toolbar.playerController = this;
    }
    private void BindInput()
    {
        playerInput = new PlayerInput();
        //playerInput.Player.Move.performed += Move_performed;
        //playerInput.Player.Move.started += Move_performed;
        //playerInput.Player.Move.canceled += Move_performed;

        playerInput.Player.PlaceBlock.started += PlaceBlock_started;
        playerInput.Player.DestroyBlock.started += DestroyBlock_started;
        playerInput.Player.NoClip.started += NoClip_started;
        playerInput.Player.Inventory.started += Inventory_started;

        playerInput.Player.PlaceStructure.started += PlaceStructure_started;
    }

    private void NoClip_started(UnityEngine.InputSystem.InputAction.CallbackContext obj) => noClip = !noClip;
    private void Inventory_started(UnityEngine.InputSystem.InputAction.CallbackContext obj) => inInventory = !inInventory;

    private void PlaceBlock_started(UnityEngine.InputSystem.InputAction.CallbackContext obj) => UseItem();
    private void DestroyBlock_started(UnityEngine.InputSystem.InputAction.CallbackContext obj) => DestroyBlock();


    private void PlaceStructure_started(UnityEngine.InputSystem.InputAction.CallbackContext obj) => placeStructure = true;

    private void OnEnable() => playerInput.Enable();
    private void OnDisable() => playerInput.Disable();

    void Update()
    {
        if (!IsConsoleOpenned)
            if (!inInventory)
                {
                    PlaceCursorBlock();
                    GetInput();
                    SetRotation();
                    SelectBlock();
                }
        controller.GetMovement(input, isSprinting);

        
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




    private void SelectBlock()
    {
        if (HighlightBlock.gameObject.activeSelf)
        {
            if (destroyBlock)
            {
                worldClass.SetBlock(HighlightBlock.position, 0);
                destroyBlock = false;
            }
            if (placeBlock)
            {
                worldClass.SetBlock(HighlightPlaceBlock.position, placingBlockID);
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
                    worldClass.SetBlock(HighlightBlock.position, 0);
    }
    public void PlaceBlock(int BlockID)
    {
        worldClass.SetBlock(HighlightPlaceBlock.position, BlockID);
        placeBlock = false;
    }

    private void PlaceCursorBlock()
    {
        float step = checkIncrement;

        Vector3 lastPos = Camera.position;

        while (step < reach)
        {
            Vector3 pos = Camera.position + Camera.forward * step;

            if (worldClass.GetBlock(pos) != 0)
            {
                if (worldClass.blockTypesList.areSolid[worldClass.GetBlock(pos)])
                {
                    HighlightBlock.position = new Vector3(Mathf.Floor(pos.x), Mathf.Floor(pos.y), Mathf.Floor(pos.z));
                    HighlightBlock.gameObject.SetActive(true);

                    HighlightPlaceBlock.position = new Vector3(Mathf.Floor(lastPos.x), Mathf.Floor(lastPos.y), Mathf.Floor(lastPos.z));
                    HighlightPlaceBlock.gameObject.SetActive(toolbar.IsPlaceable());
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
                    Backpack.SetActive(true);
                    cursorSlot.SetActive(true);
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Backpack.SetActive(false);
                    cursorSlot.SetActive(false);

                }
            }

        }
    }

    public ItemStack PickUpItem(ItemStack item)
    {
        return inventory.PickUpItem(item);
    }

    public void UseItem()
    {
        if(!IsConsoleOpenned)

        toolbar.UseItem(HighlightPlaceBlock.gameObject.activeSelf);
    }
}
