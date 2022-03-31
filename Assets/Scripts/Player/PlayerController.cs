using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public WorldClass world;
    public WorldClassV2 WorldClassV2;
    public CharacterController CharacterController;
    public Transform Camera;
    public Transform blockPlacement;

    public float mouseSensitivity = 100f;
    public float speed = 10;
    public float gravity = -9.8f;

    Vector3 input;
    Vector2 mouseInput;
    Vector3 velocity = Vector3.zero;
    Vector3 move;

    void Update()
    {
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

        if (WorldClassV2 != null && Input.GetAxis("Fire1") != 0)
        {
            int blockID = WorldClassV2.SetBlock(blockPlacement.position, 1);
            Debug.Log("Replaced " + blockID);
        }

    }

    void SetRotation()
    {
        transform.Rotate(Vector3.up * mouseInput.x);
        Camera.Rotate(Vector3.right * -mouseInput.y);
    }
}
