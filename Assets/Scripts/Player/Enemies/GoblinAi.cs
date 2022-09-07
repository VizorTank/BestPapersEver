using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoblinAi : EnemyAi
{
    public Vector3 Direction;
    public float angle;
    public Vector3 homePosition;

    //to replace
    public Transform target;
    public bool Frigenned = false;
    public string State1;

    public override void Awake()
    {
        base.Awake();
        homePosition = transform.position;
    }

    protected override void EnemyMovment()
    {
       // if(Vector3.Distance(transform.position,homePosition)>30)
       // {
       //     LookAtHome();
       // }
        if(!Frigenned && Vector3.Distance(transform.position,target.position)<16.5)
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
            }
        }
        else if(Frigenned)
        {
            if(Vector3.Distance(transform.position, target.position) < 15)
            {
                State1 = State.Runing.ToString();
                RunFromTarget();
                WalkForward();
            }
            else { Frigenned = false; }
        }
        else
        {
            if(Vector3.Distance(transform.position, homePosition) > 2.5)
            {
                LookAtHome();
                State1 = State.GoingHome.ToString();
                WalkForward();
            }
        }

    }

    public void LookAtHome()
    {
        Direction = homePosition - transform.position;
        angle = (Mathf.Atan2(Direction.x, Direction.z) * Mathf.Rad2Deg);
        transform.localEulerAngles = new Vector3(0f, angle, 0f);
    }

    public void WalkForward()
    {
        MoveController.GetMovement(new Vector3(0, 0, 1));
        if ((MoveController.movement.x == 0 && MoveController.input.x != 0) || (MoveController.movement.z == 0 && MoveController.input.z != 0))
        { MoveController.jumprequest = true; }
    }
    public void LookAtTarget()
    {
        Direction = target.position - transform.position;
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
