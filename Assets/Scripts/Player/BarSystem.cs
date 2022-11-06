using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BarSystem : MonoBehaviour
{
    public Slider HealthBar;
    public Slider StaminaBar;

    public void SetMaxStats(int health, float stamina)
    {
        HealthBar.maxValue = health;
        HealthBar.value = health;
        StaminaBar.maxValue = stamina;
        StaminaBar.value = stamina;
    }
    public void SetHealth(float health)
    {
        HealthBar.value = health;
    }
    public void SetStamina(float stamina)
    {
        StaminaBar.value = stamina;
    }
}
