using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    // World
    public WorldClass worldClass;

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
    public bool placeStructure;
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

    private void Awake()
    {
        BindInput();
    }

    private void BindInput()
    {
        playerInput = new PlayerInput();
        //playerInput.Player.Move.performed += Move_performed;
        //playerInput.Player.Move.started += Move_performed;
        //playerInput.Player.Move.canceled += Move_performed;

        playerInput.Player.PlaceBlock.started += PlaceBlock_started;
        playerInput.Player.PlaceStructure.started += PlaceStructure_started;
        playerInput.Player.DestroyBlock.started += DestroyBlock_started;
        playerInput.Player.NoClip.started += NoClip_started;

        playerInput.Enable();
    }

    private void NoClip_started(UnityEngine.InputSystem.InputAction.CallbackContext obj) => noClip = !noClip;

    private void PlaceBlock_started(UnityEngine.InputSystem.InputAction.CallbackContext obj) => placeBlock = true;
    private void PlaceStructure_started(UnityEngine.InputSystem.InputAction.CallbackContext obj) => placeStructure = true;
    private void DestroyBlock_started(UnityEngine.InputSystem.InputAction.CallbackContext obj) => destroyBlock = true;

    private void OnEnable() => playerInput.Enable();
    private void OnDisable() => playerInput.Disable();

    void Update()
    {
        PlaceCursorBlock();
        GetInput();
        SetRotation();
        SelectBlock();

        movement = (transform.forward * input.z + transform.right * input.x) * speed * Time.deltaTime;
        if (isSprinting)
            movement *= sprintSpeed;
        // if (false)
        // {
        //     movement = CheckCollisionSides(movement);
        //     velocity.y = CheckGround(velocity.y);
        //     if (!isGrounded)
        //     {
        //         velocity += Vector3.up * gravity * Time.deltaTime / 1;
        //     }
        //     if (isGrounded)
        //     {
        //         velocity.y = Mathf.Clamp(input.y, 0, 1) * jumpHeight;
        //     }
        // }
        // else
        // {
            movement += transform.up * input.y * speed * Time.deltaTime;
            velocity.y = 0;
        // }

        transform.position += (velocity + movement);
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

    private Vector3 CheckCollisionSides(Vector3 move)
    {
        for (int i = -(int)(playerHeight / 2); i < (int)(playerHeight / 2); i++)
        {
            Vector3 block = new Vector3(transform.position.x, 
                transform.position.y + i + 1, 
                transform.position.z) + move;

            for (int j = 0; j < 4; j++)
            {
                int blockId = 0;
                if (worldClass.TryGetBlock(block + fourCorners[j] * playerWidth, ref blockId) 
                    && blockId >= 0 
                    && worldClass.blockTypesList.areSolid[blockId])
                {
                    return Vector3.zero;
                }
            }
        }
        return move;
    }
    Vector3[] fourCorners = new Vector3[] {
        new Vector3(-1, 0, -1),
        new Vector3(1,  0, -1),
        new Vector3(1,  0,  1),
        new Vector3(-1, 0,  1)
    };
    private float CheckGround(float upSpeed)
    {
        Vector3 downBlock = new Vector3(transform.position.x, transform.position.y + upSpeed - playerHeight / 2, transform.position.z);
        
        for (int i = 0; i < 4; i++)
        {
            if (worldClass.blockTypesList.areSolid[worldClass.GetBlock(downBlock + fourCorners[i] * playerWidth)])
            {
                isGrounded = true;
                return 0;
            }
        }
        isGrounded = false;
        return upSpeed;
    }

    private void SelectBlock()
    {
        if (scroll != 0)
        {
            placingBlockID += scroll > 0 ? 1 : -1;
            if (placingBlockID >= worldClass.Materials.Count) placingBlockID = 1;
            if (placingBlockID <= 0) placingBlockID = worldClass.Materials.Count - 1;

            BlockDisplay.text = "Selected: " + worldClass.blockTypesList.Names[placingBlockID];
        }

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
                HighlightBlock.position = new Vector3(Mathf.Floor(pos.x), Mathf.Floor(pos.y), Mathf.Floor(pos.z));
                HighlightBlock.gameObject.SetActive(true);

                HighlightPlaceBlock.position = new Vector3(Mathf.Floor(lastPos.x), Mathf.Floor(lastPos.y), Mathf.Floor(lastPos.z));
                HighlightPlaceBlock.gameObject.SetActive(true);

                return;
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
}
