using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterController : MonoBehaviour
{
    public WorldClass worldClass;






    Vector3 VerticalMomentum = Vector3.zero;
    Vector3 movement;
    Vector3 velocity;
    float upSpeed = 0;
    bool noClip = false;
    bool isSprinting;
    bool isGrounded;
    Vector3 zero = Vector3.zero;
    bool jumprequest = false;


    public bool Xaxis = false;
    public bool Zaxis = false;

    public float reach = 8f;
    public float checkIncrement = 0.1f;


    [Header("Limiters")]
    public Vector3 DownBlock;
    public Vector3 UpBlock;
    public int LimiterFront;
    public int LimiterBack;
    public int LimiterLeft;
    public int LimiterRight;


    public CharacterStats CharacterStats;
    public Animator animator;
    // Start is called before the first frame update
    void Start()
    {
        animator = transform.Find("Body").GetComponent<Animator>();
        CharacterStats = transform.GetComponent<CharacterStats>();
    }

    // Update is called once per frame
    void Update()
    {
        Move();
    }
    public void GetMovement(Vector3 input , bool isSprinting = false)
    {
        
        movement = (transform.forward * input.z + transform.right * input.x) * CharacterStats.speed * Time.deltaTime;
        if(input.y!=0)
        {
            jumprequest = true;
        }

        this.isSprinting = isSprinting;
        input = Vector3.zero;

        
    }

    private void Jump()
    {
        isGrounded = false;
        jumprequest = false;
        VerticalMomentum.y = -CharacterStats.jumpHeight * CharacterStats.gravity * Time.deltaTime;
    }
    protected void Move()
    {
        
        movement = CalculateMovement(movement);
        if (!isGrounded)
        {
            VerticalMomentum +=new Vector3(0,1f,0) * CharacterStats.gravity * Time.deltaTime / 1;
            jumprequest = false;
            animator.SetBool("InAir", true);
        }
        if (isGrounded)
        {
            animator.SetBool("InAir", false);
            if (jumprequest)
                VerticalMomentum.y = Mathf.Sqrt(CharacterStats.jumpHeight * -CharacterStats.gravity * 2f);// * Time.deltaTime;
            

        }
       
        
        if(movement!=Vector3.zero)
        {
            animator.SetBool("IsWalking", true);
        }
        else
        {
            animator.SetBool("IsWalking", false);
        }
        if(isSprinting)
        {
            if(movement != Vector3.zero && CharacterStats.TryUseStamina(50*Time.deltaTime))
            {
            animator.SetBool("IsRunning", true);
            movement *= CharacterStats.sprintSpeed;
            }
        }
        else animator.SetBool("IsRunning", false);
        
        
        transform.position += movement + VerticalMomentum;
    }

    protected Vector3 CalculateMovement(Vector3 move)
    {
        FindGroundBlock();
        FindFrontBlock();
        FindBackBlock();
        FindLeftBlock();
        FindRightBlock();
        FindUpBlock();

        if (transform.position.y - CharacterStats.height / 2 <= DownBlock.y + 1)
        {
            transform.position = new Vector3(transform.position.x, DownBlock.y + CharacterStats.height / 2 + 1, transform.position.z);
            isGrounded = true;
            VerticalMomentum = Vector3.zero;
        }
        else isGrounded = false;

        if (move.z > 0 && transform.position.z + CharacterStats.width >= LimiterFront)
        {
            move.z = 0f;
            transform.position = new Vector3(transform.position.x, transform.position.y, LimiterFront - CharacterStats.width);
        }
        else if (move.z < 0 && transform.position.z - CharacterStats.width <= LimiterBack)
        {
            move.z = 0f;
            transform.position = new Vector3(transform.position.x, transform.position.y, LimiterBack + CharacterStats.width);
        }
        if (move.x > 0 && transform.position.x + CharacterStats.width >= LimiterRight)
        {
            move.x = 0f;
            transform.position = new Vector3(LimiterRight - CharacterStats.width, transform.position.y, transform.position.z);
        }
        else if (move.x < 0 && transform.position.z - CharacterStats.width <= LimiterLeft)
        {
            move.x = 0f;
            transform.position = new Vector3(LimiterLeft + CharacterStats.width, transform.position.y, transform.position.z);
        }
        if (transform.position.y + CharacterStats.height / 2 >= UpBlock.y)
        {
            transform.position = new Vector3(transform.position.x, UpBlock.y - CharacterStats.height / 2 , transform.position.z);
            VerticalMomentum = Vector3.zero;
        }


        return move;
    }

    //Correct
    private void FindGroundBlock()
    {

            Vector3 pos = transform.position -new Vector3(0f,(CharacterStats.height/2) *1.1f,0f);
            
            if (worldClass.GetBlock(pos) != 0)
            {
                if (worldClass.blockTypesList.areSolid[worldClass.GetBlock(pos)])
                {
                    DownBlock = new Vector3(Mathf.Floor(pos.x), Mathf.Floor(pos.y), Mathf.Floor(pos.z));
                    return;
                }
            }
            
           // step += checkIncrement;
        
        DownBlock = new Vector3(Mathf.Floor(transform.position.x), int.MinValue, transform.position.z);

    }

    private void FindUpBlock()
    {

        Vector3 pos = transform.position + new Vector3(0f, (CharacterStats.height / 2) * 1.1f, 0f);

        if (worldClass.GetBlock(pos) != 0)
        {
            if (worldClass.blockTypesList.areSolid[worldClass.GetBlock(pos)])
            {
                UpBlock = new Vector3(Mathf.Floor(pos.x), Mathf.Floor(pos.y), Mathf.Floor(pos.z));
                return;
            }
        }

        // step += checkIncrement;

        UpBlock = new Vector3(Mathf.Floor(transform.position.x), int.MaxValue, transform.position.z);

    }

    private void FindFrontBlock()
    {
        

        for (int i = 0; i < CharacterStats.height; i++)
        {
            Vector3 pos = transform.position + new Vector3(0,(-CharacterStats.height /4 + i) ,0) + Vector3.forward * CharacterStats.width * 1.1f;

            if (worldClass.GetBlock(pos) != 0)
            {
                if (worldClass.blockTypesList.areSolid[worldClass.GetBlock(pos)])
                {
                    LimiterFront = (int)Mathf.Floor(pos.z);
                    return;
                }
            }
        }
        LimiterFront = int.MaxValue;
    }

    private void FindBackBlock()
    {


        for (int i = 0; i < CharacterStats.height; i++)
        {
            Vector3 pos = transform.position + new Vector3(0, (-CharacterStats.height / 4 + i), 0) - Vector3.forward * CharacterStats.width * 1.1f;

            if (worldClass.GetBlock(pos) != 0)
            {
                if (worldClass.blockTypesList.areSolid[worldClass.GetBlock(pos)])
                {
                    LimiterBack = (int)Mathf.Floor(pos.z) + 1;
                    return;
                }
            }
        }
        LimiterBack = int.MinValue;
    }

    private void FindRightBlock()
    {


        for (int i = 0; i < CharacterStats.height; i++)
        {
            Vector3 pos = transform.position + new Vector3(0, (-CharacterStats.height / 4 + i), 0) + Vector3.right * CharacterStats.width * 1.1f;

            if (worldClass.GetBlock(pos) != 0)
            {
                if (worldClass.blockTypesList.areSolid[worldClass.GetBlock(pos)])
                {
                    LimiterRight = (int)Mathf.Floor(pos.x);
                    return;
                }
            }
        }
        LimiterRight = int.MaxValue;
    }

    private void FindLeftBlock()
    {


        for (int i = 0; i < CharacterStats.height; i++)
        {
            Vector3 pos = transform.position + new Vector3(0, (-CharacterStats.height / 4 + i), 0) - Vector3.right * CharacterStats.width * 1.1f;

            if (worldClass.GetBlock(pos) != 0)
            {
                if (worldClass.blockTypesList.areSolid[worldClass.GetBlock(pos)])
                {
                    LimiterLeft = (int)Mathf.Floor(pos.x) + 1;
                    return;
                }
            }
        }
        LimiterLeft = int.MinValue;
    }
}


