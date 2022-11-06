using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterStats : MonoBehaviour
{
    [Header("Size")]
    public float height = 0f;
    public float width = 0f;


    [Header("Movement")]
    public float speed = 6;
    public float sprintSpeed = 2;
    public float jumpHeight = 0.015f;
    public float gravity = -0.5f;

    [Header("Maximum")]
    public int MaxHealth= 200;
    public int MaxStamina = 200;
    public float StaminaRegeneration = 0.5f;
    public float HealthRegeneration = 0f;

    [Header("Current")]
    public float CurrentHealth;
    public float CurrentStamina;
    public bool IsTired;

    [Header("Defences")]
    public int Armour=0;



    public Dictionary<Transform, int> LataDamageTable = new Dictionary<Transform, int>();
    // Start is called before the first frame update
    void Start()
    {
        CurrentHealth = MaxHealth;
        CurrentStamina = MaxStamina;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        UpdateStats();
        UpdateDamageTable();
    }

    private void UpdateDamageTable()
    {
        if(LataDamageTable.Count>0)
        {
            List<Transform> Array = new List<Transform>(LataDamageTable.Keys);
            foreach(Transform damageints in Array)
            {
                LataDamageTable[damageints]--;
                if(LataDamageTable[damageints]<=0)
                {
                    LataDamageTable.Remove(damageints);
                }
            }
        }
    }

    public void TakeHeal(float heal)
    {
        heal = Mathf.Clamp(heal, 0, int.MaxValue);

        CurrentHealth += heal;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);
    }


    public event EventHandler OnDamageTaken;
    public void TakeDamage(HitData data)
    {
        float damage = Mathf.Clamp(data.damage, 0, int.MaxValue);
        if (data.Sourse == transform || data.damageTarget.ToString()!=transform.tag)
            return;
        if (!LataDamageTable.ContainsKey(data.Sourse))
        {
            LataDamageTable.Add(data.Sourse, 10);
            if (OnDamageTaken != null) OnDamageTaken(this, EventArgs.Empty);
            CurrentHealth -= damage;
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);
            if (CurrentHealth == 0)
            { Die(); }
        }
    }
    public void SetArmor(int armor)
    {
        Armour = armor;
    }
    private void Die()
    {
       // Destroy(transform.gameObject);
    }

    public bool TryUseStamina(float useStamina)
    {
        if (!IsTired && CurrentStamina >= useStamina)
        {
            CurrentStamina -= useStamina;
            return true;
        }
        else
        {
            IsTired = true;
            return false; 
        }

    }

    public void UpdateStats()
    {
        CurrentStamina = Mathf.Clamp(CurrentStamina += StaminaRegeneration * Time.fixedDeltaTime, 0, MaxStamina);
        CurrentHealth = Mathf.Clamp(CurrentHealth += HealthRegeneration * Time.fixedDeltaTime, 0, MaxHealth);
        if(CurrentStamina >= 0.7f * MaxStamina)
        {
            IsTired = false;
        }
    }

}
