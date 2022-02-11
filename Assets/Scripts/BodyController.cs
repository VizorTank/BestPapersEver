using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodyController : MonoBehaviour
{
    public CharacterController characterController;
    public Vector3 input;
    public Vector3 move;
    public float speed = 10;

    void Update()
    {
        GetInput();
        move = transform.right * input.x + transform.forward * input.z + transform.up * input.y;
        move *= speed;

        characterController.Move((move) * Time.deltaTime);
    }
    void GetInput()
    {
        input.x = Input.GetAxis("Horizontal");
        input.z = Input.GetAxis("Vertical");
        //input.y = Input.GetButtonDown("Jump") ? 1 : 0;
        input.y = Input.GetAxis("Jump");
    }
}
