using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EnemyAi : MonoBehaviour
{
    [SerializeField]
    protected CharacterController MoveController;
    public virtual void Awake()
    {
        MoveController = this.GetComponent(typeof(CharacterController)) as CharacterController;
    }

    public void Update()
    {
        EnemyMovment();
    }

    protected abstract void EnemyMovment();
}
