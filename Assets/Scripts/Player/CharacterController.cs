using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterController : MonoBehaviour
{
    public WorldClass worldClass;


    public Vector3 VerticalMomentum = Vector3.zero;
    public  Vector3 movement;
    public bool isSprinting;
    public bool isGrounded;
    public bool jumprequest = false;

    public float reach = 8f;
    public float checkIncrement = 0.1f;


    [Header("Limiters")]
    public Vector3 DownBlock;
    public Vector3 UpBlock;
    public int LimiterFront;
    public int LimiterBack;
    public int LimiterLeft;
    public int LimiterRight;
    public Vector3 input;
    public bool MovementBlocked;
    public bool _InLiquid;
    public bool inLiquid
    {
        set
        {
            bool oldValue = _InLiquid;
            if(value&&value!=oldValue)
            EnterWater();
            _InLiquid = value;
        }
        get { return _InLiquid; }
    }

    public CharacterStats CharacterStats;
    public Animator animator;
    private void Awake()
    {
        if (animator == null)
            animator = transform.Find("Body").GetComponent<Animator>();
        CharacterStats = transform.GetComponent<CharacterStats>();
        if (worldClass == null)
        {
            worldClass = GameObject.Find("World").transform.GetComponent<WorldClass>();
        }
    }

    void Update()
    {
        Move();
    }

    public void GetMovement(Vector3 input , bool isSprinting = false)
    {
        
        this.input = (transform.forward * input.z + transform.right * input.x) * CharacterStats.speed * Time.deltaTime;
        if(input.y!=0)
        {
            jumprequest = true;
        }
        if(input.y==0)
        {
            jumprequest = false;
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
        
        movement = CalculateMovement(input);
        input = Vector3.zero;
        if (!isGrounded&&!inLiquid)
        {
            VerticalMomentum +=new Vector3(0,1f,0) * CharacterStats.gravity * Time.deltaTime / 1;
            jumprequest = false;
            animator.SetBool("InAir", true);
        }
        if (isGrounded&&!inLiquid)
        {
            animator.SetBool("InAir", false);
            if (jumprequest)
                VerticalMomentum.y = Mathf.Sqrt(CharacterStats.jumpHeight * -CharacterStats.gravity * 2f);// * Time.deltaTime;
            

        }
        if(inLiquid)
        {
            if(!jumprequest)
                VerticalMomentum += new Vector3(0, .1f, 0) * CharacterStats.gravity * Time.deltaTime / 1;
            if(jumprequest)
                VerticalMomentum -= new Vector3(0, .1f, 0) * CharacterStats.gravity * Time.deltaTime / 1;
        }
       
        
        if(movement!=Vector3.zero)
        {
            animator.SetBool("IsWalking", true);
        }
        else
        {
            animator.SetBool("IsWalking", false);
        }
        if(isSprinting && movement != Vector3.zero && CharacterStats.TryUseStamina(50 * Time.deltaTime))
        {
            animator.SetBool("IsRunning", true);
            movement *= CharacterStats.sprintSpeed;
        }
        else animator.SetBool("IsRunning", false);
        
        
        
        transform.position += movement + VerticalMomentum;
    }


    public Vector3 EnterWater()
    {

        Vector3 oldVertical = VerticalMomentum;
        if(VerticalMomentum.y>=.11f)
        VerticalMomentum = new Vector3(0, .1f, 0) * CharacterStats.gravity * Time.deltaTime / 1;
        return oldVertical - VerticalMomentum;
    }
    protected Vector3 CalculateMovement(Vector3 move)
    {
        Findlegblock();
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
            MovementBlocked = true;
        }

        if (move.z < 0 && transform.position.z - CharacterStats.width <= LimiterBack)
        {
            move.z = 0f;
            MovementBlocked = true;
        }

        if (move.x < 0 && transform.position.x - CharacterStats.width <= LimiterLeft)
        {
            move.x = 0f;
            MovementBlocked = true;
        }

        if (move.x > 0 && transform.position.x + CharacterStats.width >= LimiterRight)
        {
            move.x = 0f;
            MovementBlocked = true;
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
        int type = 0;
        worldClass.TryGetBlock(pos, ref type);
            if (type != 0)
            {
                if (worldClass.blockTypesList.areSolid[type])
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
        int type = 0;
        worldClass.TryGetBlock(pos, ref type);
        if (type != 0)
        {
            if (worldClass.blockTypesList.areSolid[type])
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
            int type = 0;
            worldClass.TryGetBlock(pos, ref type);
            if (type != 0)
            {
                if (worldClass.blockTypesList.areSolid[type])
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
            int type = 0;
            worldClass.TryGetBlock(pos, ref type);
            if (type != 0)
            {
                if (worldClass.blockTypesList.areSolid[type])
                {
                    LimiterBack = (int)Mathf.Floor(pos.z) + 1;
                    return;
                }
            }
        }
        LimiterBack = int.MinValue;
    }

    private void Findlegblock()
    {
        Vector3 pos = transform.position + new Vector3(0, (-CharacterStats.height / 4 ), 0);
        int type = 0;
        worldClass.TryGetBlock(pos, ref type);
        if (type != 0)
        {
            if (worldClass.blockTypesList.areLiquid[type])
            {
                inLiquid = true;
                return;
            }
        }
        inLiquid = false;
    }

    private void FindRightBlock()
    {
        for (int i = 0; i < CharacterStats.height; i++)
        {
            Vector3 pos = transform.position + new Vector3(0, (-CharacterStats.height / 4 + i), 0) + Vector3.right * CharacterStats.width * 1.1f;

            int type = 0;
            worldClass.TryGetBlock(pos, ref type);
            if (type != 0)
            {
                if (worldClass.blockTypesList.areSolid[type])
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
            int type = 0;
            worldClass.TryGetBlock(pos, ref type);
            if (type != 0)
            {
                if (worldClass.blockTypesList.areSolid[type])
                {
                    LimiterLeft = (int)Mathf.Floor(pos.x) + 1;
                    return;
                }
            }
        }
        LimiterLeft = int.MinValue;
    }
}


