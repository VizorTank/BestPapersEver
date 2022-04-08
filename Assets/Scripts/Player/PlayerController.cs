using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public WorldClass world;
    public WorldClassV2 worldClass2;
    public WorldClassV3 worldClass3;
    public CharacterController CharacterController;
    public Transform Camera;
    public Transform blockPlacement;

    public Transform HighlightBlock;
    public Transform HighlightPlaceBlock;
    public float checkIncrement = 0.1f;
    public float reach = 8f;

    public int placingBlockID = 1;

    public float mouseSensitivity = 100f;
    public float speed = 10;
    public float gravity = -9.8f;

    Vector3 input;
    Vector2 mouseInput;
    Vector3 velocity = Vector3.zero;
    Vector3 move;

    void Update()
    {
        PlaceCursorBlock();
        GetInput();
        SetRotation();
        move = (transform.right * input.x + transform.forward * input.z + transform.up * input.y) * Time.deltaTime * speed;
        //velocity += Vector3.up * gravity * Time.deltaTime;

        CharacterController.Move(move + velocity);
    }
    void GetInput()
    {
        input.x = Input.GetAxis("Horizontal");
        input.z = Input.GetAxis("Vertical");
        //input.y = Input.GetButtonDown("Jump") ? 1 : 0;
        input.y = Input.GetAxis("Jump");

        mouseInput.x = Input.GetAxis("Mouse X");
        mouseInput.y = Input.GetAxis("Mouse Y");

        //if (worldClass2 != null && Input.GetAxis("Fire1") != 0)
        //{
        //    int blockID = worldClass2.SetBlock(blockPlacement.position, placingBlockID);
        //    Debug.Log("Replaced " + blockID);
        //}
        //if (worldClass3 != null && Input.GetAxis("Fire1") != 0)
        //{
        //    int blockID = worldClass3.SetBlock(blockPlacement.position, placingBlockID);
        //    Debug.Log("Replaced " + blockID);
        //}

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            placingBlockID += scroll > 0 ? 1 : -1;
            if (placingBlockID >= worldClass3.materials.Count) placingBlockID = 1;
            if (placingBlockID <= 0) placingBlockID = worldClass3.materials.Count - 1;
        }

        if (HighlightBlock.gameObject.activeSelf)
        {
            if (Input.GetMouseButtonDown(0))
                worldClass3.SetBlock(HighlightBlock.position, 0);
            if (Input.GetMouseButtonDown(1))
                worldClass3.SetBlock(HighlightPlaceBlock.position, placingBlockID);
        }
    }

    private void PlaceCursorBlock()
    {
        float step = checkIncrement;

        Vector3 lastPos = Camera.position;

        while (step < reach)
        {
            Vector3 pos = Camera.position + Camera.forward * step;

            if (worldClass3.GetBlock(pos) != 0)
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
        transform.Rotate(Vector3.up * mouseInput.x);
        Camera.Rotate(Vector3.right * -mouseInput.y);
    }
}
