using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WanderAi 
{
    public  float rotSpeed;
           
    public  bool isWandering = false;
    public  bool isRotatingLeft = false;
    public  bool isRotatingRight = false;
    public  bool isWalking = false;

    // Update is called once per frame
    public  void Wandering()
    {

    }

    float MinRottime = 0, MaxRotTime = 0, MinRotateWait = 0, MaxRotateWait = 0, MinWalkWait = 0, MaxWalkWait = 0, MinWalkTime = 0, MaxWalkTime = 0;

    public WanderAi(float MinRottime, float MaxRotTime, float MinRotateWait, float MaxRotateWait, float MinWalkWait, float MaxWalkWait, float MinWalkTime, float MaxWalkTime, float rotSpeed = 100f)
    {
        this.MinRottime = MinRottime;
        this.MaxRotTime = MaxRotTime;
        this.MinRotateWait = MinRotateWait;
        this.MaxRotateWait = MaxRotateWait;
        this.MinWalkTime = MinWalkTime;
        this.MaxWalkTime = MaxWalkTime;
        this.MinWalkWait = MinWalkWait;
        this.MaxWalkWait = MaxWalkWait;
        this.rotSpeed = rotSpeed;
    }



    public  IEnumerator Wander()
    {
        float rotTime = Random.Range(MinRottime, MaxRotTime);
        float rotateWait = Random.Range(MinRotateWait, MaxRotateWait);
        float rotateLorR = Random.Range(1, 2);
        float walkWait = Random.Range(MinWalkWait, MaxWalkWait);
        float walkTime = Random.Range(MinWalkTime, MaxWalkTime);

        isWandering = true;

        yield return new WaitForSeconds(walkWait);
        isWalking = true;
        yield return new WaitForSeconds(walkTime);
        isWalking = false;
        yield return new WaitForSeconds(rotateWait);
        if (rotateLorR == 1)
        {
            isRotatingRight = true;
            yield return new WaitForSeconds(rotTime);
            isRotatingRight = false;
        }
        if (rotateLorR == 2)
        {
            isRotatingLeft = true;
            yield return new WaitForSeconds(rotTime);
            isRotatingLeft = false;
        }
        isWandering = false;
    }
}