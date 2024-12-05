using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Image _healthFrontImage;
    [SerializeField] private UnityEngine.UI.Image _healthBackImage;

    private int _health = 20;

    private float chipSpeed = 1f;
    private float starterHealth;
    private float lerpTimer;

    public bool IsDead { get; private set; } = false;

    public void SetHealth(int value)
    {
        _health = value;
        starterHealth = _health;
    }

    public void TakeDamage(int value)
    {
        if (IsDead) return;

        if (_health - value <= 0)
        {
            _health = 0;
            IsDead = true;
            return;
        }

        _health -= value;

        lerpTimer = 0f;
    }

    private void Update()
    {
        UpdateHealthUI();
    }

    private void UpdateHealthUI()
    {
        float fillB = _healthBackImage.fillAmount;
        float hFraction = _health / starterHealth;

        if (fillB > hFraction)
        {
            _healthFrontImage.fillAmount = hFraction;
            _healthBackImage.color = Color.white;
            lerpTimer += Time.deltaTime;
            float percentComplete = lerpTimer / chipSpeed;
            percentComplete = percentComplete * percentComplete;
            _healthBackImage.fillAmount = Mathf.Lerp(fillB, hFraction, percentComplete);
        }
    }
}
