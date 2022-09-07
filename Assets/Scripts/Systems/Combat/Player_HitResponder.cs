using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_HitResponder : MonoBehaviour, IHitResponder
{

    public bool atack;
    public int damage = 10;
    public Comp_Hitbox _Hitbox;
    public int Damage { get => damage; set => damage = value; }
    public bool Atack { get => atack; set => atack = value; }

    private void Start()
    {
        _Hitbox.HitResponder = this;

    }

    private void Update()
    {
        if(atack)
        {
            _Hitbox.CheckHit();
        }
    }

    public bool CheckHit(HitData data)
    {
        return true;
    }

    public bool Rensponse(HitData data)
    {
        return true;
    }
}
