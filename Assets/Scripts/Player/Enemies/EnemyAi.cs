using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EnemyAi : MonoBehaviour
{

    public Vector3 Direction;
    public float angle;
    Rigidbody m_Rigidbody;
    public Animator animator;
    public Transform target;

    [SerializeField]
    protected CharacterController MoveController;
    public virtual void Awake()
    {
        MoveController = this.GetComponent(typeof(CharacterController)) as CharacterController;
        if(target==null)
        {
            
        }


    }

    public void Update()
    {
        EnemyMovement();
    }

    protected abstract void EnemyMovement();

    public void LookAtTarget()
    {
        if (target != null)
        {
            Direction = target.position - transform.position;
            angle = (Mathf.Atan2(Direction.x, Direction.z) * Mathf.Rad2Deg);
            transform.localEulerAngles = new Vector3(0f, angle, 0f);
        }
    }
    public void WalkForward()
    {
        MoveController.GetMovement(new Vector3(0, 0, 1));
        //if ((MoveController.movement.x == 0 && MoveController.input.x != 0) || (MoveController.movement.z == 0 && MoveController.input.z != 0))
        if(MoveController.MovementBlocked)
        {
            MoveController.jumprequest = true;
            MoveController.MovementBlocked = false;
        }
    }

    public void RunForward()
    {
        MoveController.GetMovement(new Vector3(0, 0, 1),true);
        //if ((MoveController.movement.x == 0 && MoveController.input.x != 0) || (MoveController.movement.z == 0 && MoveController.input.z != 0))
        if (MoveController.MovementBlocked)
        {
            MoveController.jumprequest = true;
            MoveController.MovementBlocked = false;
        }
    }



}
