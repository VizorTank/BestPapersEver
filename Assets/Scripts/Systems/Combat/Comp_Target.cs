using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Comp_Target : MonoBehaviour, IHurtResponder
{
    [SerializeField] private List<Comp_Hurtbox> hurtboxes = new List<Comp_Hurtbox>();
    [SerializeField] private CharacterStats characterStats;

    private void Start()
    {
        characterStats = transform.GetComponent<CharacterStats>();
        hurtboxes = new List<Comp_Hurtbox>(GetComponentsInChildren<Comp_Hurtbox>());
        foreach (Comp_Hurtbox box in hurtboxes)
        {
            box.HurtResponder = this;
        }
    }
    public bool CheckHit(HitData data)
    {
        return true;
    }

    public bool Rensponse(HitData data)
    {
        characterStats.TakeDamage(data);
        Debug.Log("Damage");
        return true;
    }
}
