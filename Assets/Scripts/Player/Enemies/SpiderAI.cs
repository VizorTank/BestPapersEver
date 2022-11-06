using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiderAI : EnemyAi
{
    Pathfinding pathfinding;
    public float atacktimer = 0;
    public WanderAi wander;
    public SpiderMindState mindState = SpiderMindState.Wandering;
    [SerializeField] DetectingPlayer detectingplayer;
    public bool Pathfind=false;
    private void Start()
    {
        wander = new WanderAi(0f, 1.5f, 0, 1, 0, 2.5f, 1, 3, 200);
        detectingplayer.enemyAi = this;
        pathfinding = new Pathfinding(MoveController.worldClass);
    }
    protected override void EnemyMovement()
    {

        if(target!=null&&Pathfind)
        {
            pathfinding.FindPath(transform.position, target.position);
            Pathfind = false;
        }
        if (target != null)
        {
            if (mindState == SpiderMindState.Atacking)
            {
                if (atacktimer >= 0.9f && atacktimer <= 1f)
                {
                    transform.GetComponent<Player_HitResponder>().atack = true;
                    transform.Find("SpiderAtack").gameObject.SetActive(true);
                }
                if (atacktimer >= 1f)
                {
                    transform.GetComponent<Player_HitResponder>().atack = false;
                    transform.Find("SpiderAtack").gameObject.SetActive(false);
                    mindState = SpiderMindState.Wandering;
                    atacktimer = 0f;
                }
                atacktimer = atacktimer + 1 * Time.deltaTime;
            }
            else if (Vector3.Distance(transform.position, target.position) < 10)
            {
                if (Vector3.Distance(transform.position, target.position) > 1.5f)
                {
                    LookAtTarget();
                    WalkForward();
                }
                else if (Vector3.Distance(transform.position, target.position) < 2)
                {
                    LookAtTarget();
                    mindState = SpiderMindState.Atacking;
                    atacktimer = 0;
                }

            }
        }
        else
        {
            detectingplayer.CheckField();
            Wandering();
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


    public enum SpiderMindState
    {
        Wandering,
        Atacking,
        FollowEnemy
    }
}
