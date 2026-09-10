using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;


public class StaminaUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI staminaText;
    [SerializeField] private StaminaSystem staminaSystem;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color exhaustedColor = Color.red;

    private void Start()
    {
        if (staminaSystem != null && staminaText != null)
        {
            UpdateUI(staminaSystem.CurrentStamina, staminaSystem.MaxStamina);
        }
    }

    private void OnEnable()
    {
        StaminaSystem.OnStaminaChanged += UpdateUI;
        StaminaSystem.OnStaminaCleared += HandleExhausted;
    }

    private void OnDisable()
    {
        StaminaSystem.OnStaminaChanged -= UpdateUI;
        StaminaSystem.OnStaminaCleared -= HandleExhausted;
    }

    private void UpdateUI(float current, float max)
    {
        if (staminaText == null) return;

        int currentInt = Mathf.CeilToInt(current);
        int maxInt = Mathf.CeilToInt(max);

        staminaText.text = $"{currentInt}/{maxInt}";

        if (current > 0f)
        {
            staminaText.color = normalColor;
        }
    }

    private void HandleExhausted()
    {
        if (staminaText != null)
        {
            staminaText.color = exhaustedColor;
        }
    }
}

