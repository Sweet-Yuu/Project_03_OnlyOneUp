using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;


public class StaminaSystem : MonoBehaviour
{
    [SerializeField] private float maxStamina = 100f;

    public static event Action<float, float> OnStaminaChanged;
    public static event Action OnStaminaCleared;

    private float currentStamina;
    private bool isExhausted;

    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;
    public bool IsExhausted => isExhausted;

    private void Awake()
    {
        currentStamina = maxStamina;
    }

    public void UseStamina(float amount)
    {
        if (isExhausted) return;

        currentStamina -= amount;
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);

        OnStaminaChanged?.Invoke(currentStamina, maxStamina);

        if (currentStamina <= 0f)
        {
            isExhausted = true;
            OnStaminaCleared?.Invoke();
        }  
    }

    public void RegainStamina(float amount)
    {
        currentStamina += amount;
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);

        if (currentStamina > 0f)
        {
            isExhausted = false;
        }

        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }
}

