using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitData
{
    public int damage;
    public Vector3 hitPoint;
    public Vector3 hitNormal;
    public IHurtBox hurtbox;
    public IHitDetector hitDetector;
    public Transform Sourse;
    public DamageTarget damageTarget;

    public bool Validate()
    {
        if (hurtbox != null)
            if (hurtbox.CheckHit(this))
                if (hurtbox.HurtResponder == null || hurtbox.HurtResponder.CheckHit(this))
                    if (hitDetector.HitResponder == null || hitDetector.HitResponder.CheckHit(this))
                        return true;
        return false;
    }

}

public interface IHitDetector
{
    public IHitResponder HitResponder { get; set; }
    public void CheckHit();
}

public interface IHitResponder
{

    int Damage { get; }

    public bool CheckHit(HitData data);

    public bool Rensponse(HitData data);
}

public interface IHurtBox
{
    public bool Active { get; }
    public GameObject Owner { get; }
    public Transform Transform { get; }
    public IHurtResponder HurtResponder { get; set; }
    public bool CheckHit(HitData data);
}

public interface IHurtResponder
{
    
    public bool CheckHit(HitData data);
    public bool Rensponse(HitData data);
}

public enum DamageTarget
{
    Enemy,
    Player
}
