using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoblinAi : EnemyAi
{

    public Vector3 homePosition;
    public WanderAi wander;
    //to replace

    public bool Frigenned = false;
    public string State1;

    public override void Awake()
    {
        base.Awake();

    }
    private void Start()
    {
        homePosition = transform.position;
        MoveController.CharacterStats.OnDamageTaken += SetTarget;
        wander = new WanderAi(0f, 1.5f, 0, 1, 0, 2.5f, 0, 1, 200);
    }

    protected override void EnemyMovement()
    {

   
        if (target != null && !Frigenned && Vector3.Distance(transform.position, target.position) < 16.5)
        {
            LookAtTarget();
            State1 = State.LookingTarget.ToString();
            if (Vector3.Distance(transform.position, target.position) > 6)
            {
                State1 = State.GoingToTraget.ToString();
                WalkForward();
            }
            if (Vector3.Distance(transform.position, target.position) < 2.5)
            {
                Frigenned = true;
                animator.SetBool("IsScared", true);
                MoveController.jumprequest = true;
            }
        }
        else if (target != null && Frigenned)
        {
            if (Vector3.Distance(transform.position, target.position) < 15)
            {
                State1 = State.Runing.ToString();
                RunFromTarget();
                RunForward();
            }
            else { Frigenned = false;  }
        }
        
        else
        {
            if (Vector2.Distance(new Vector2(transform.position.x,transform.position.z), new Vector2(homePosition.x,homePosition.z)) > 2.5)
            {
                LookAtHome();
                animator.SetBool("IsScared", false);
                State1 = State.GoingHome.ToString();
                WalkForward();
            }
            else
            {
                Wandering();
            }
        }

    }

    void Wandering()
    {
        if (wander.isWandering == false)
        {
            StartCoroutine(wander.Wander());
        }
        if (wander.isRotatingRight == true)
        {
            //gameObject.GetComponent<Animator>().Play("idle");
            transform.Rotate(transform.up * Time.deltaTime * wander.rotSpeed);
        }
        if (wander.isRotatingLeft == true)
        {
            //gameObject.GetComponent<Animator>().Play("idle");
            transform.Rotate(transform.up * Time.deltaTime * -wander.rotSpeed);
        }
        if (wander.isWalking == true)
        {
            //gameObject.GetComponent<Animator>().Play("waalk");
            WalkForward();
        }
    }

    public void SetTarget(object sender,System.EventArgs e)
    {
        List<Transform> keys = new List<Transform>(MoveController.CharacterStats.LataDamageTable.Keys);
        
        target = keys[keys.Count-1];

    }


    public void LookAtHome()
    {
        Direction = homePosition - transform.position;
        angle = (Mathf.Atan2(Direction.x, Direction.z) * Mathf.Rad2Deg);
        transform.localEulerAngles = new Vector3(0f, angle, 0f);
    }



    public void RunFromTarget()
    {
        Direction = target.position - transform.position;
        angle = (Mathf.Atan2(Direction.x, Direction.z) * Mathf.Rad2Deg);
        transform.localEulerAngles = new Vector3(0f, angle + 180f, 0f);
    }
}

public enum State
{
    LookingTarget,
    GoingToTraget,
    Runing,
    GoingHome
};
