using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public float mouseSensitivity = 100f;
    public Transform playerbody;
    public float xRotation = 0f;

    //PlayerMovement player;
    // Start is called before the first frame update
    void Start()
    {
        //player = playerbody.GetComponent<PlayerMovement>();
        xRotation = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        if (false)
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * 2 * mouseSensitivity * Time.deltaTime;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            playerbody.Rotate(Vector3.up * mouseX);
            float mouseScroll = Input.GetAxis("Mouse ScrollWheel");
            mouseScroll = Mathf.Clamp(mouseScroll, 1f, 2.46f);
            //transform.localScale=new Vector3(1f, 1f, mouseScroll);
        }
    }
}
