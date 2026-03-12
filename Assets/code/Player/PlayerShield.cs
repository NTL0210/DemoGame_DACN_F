using UnityEngine;
using System.Collections;

/// <summary>
/// Quản lý logic của khiên: chặn sát thương và hồi chiêu.
/// </summary>
public class PlayerShield : MonoBehaviour
{
    [Header("Shield Settings")]
    [SerializeField] private float cooldownTime = 30f;
    [SerializeField] private GameObject shieldVisual; // GameObject hiển thị khiên (trên người player)
    [SerializeField] private GameObject shieldIconUI; // Icon khiên trong thanh UI

    private bool hasShield = false;
    private bool isCooldown = false;

    private void Start()
    {
        // Ban đầu, khiên và icon đều tắt
        SetShieldActive(false);
    }

    /// <summary>
    /// Kích hoạt khiên lần đầu khi đạt Heart Up Lv4.
    /// </summary>
    public void ActivateShieldSystem()
    {
        if (!hasShield && !isCooldown)
        {
            SetShieldActive(true);
        }
    }

    /// <summary>
    /// Kiểm tra xem người chơi có khiên không.
    /// </summary>
    public bool HasShield()
    {
        return hasShield;
    }

    /// <summary>
    /// Sử dụng khiên để chặn sát thương.
    /// </summary>
    public void UseShield()
    {
        if (!hasShield) return;

        Debug.Log("Shield blocked an attack!");
        SetShieldActive(false);
        StartCoroutine(CooldownCoroutine());
    }

    private IEnumerator CooldownCoroutine()
    {
        isCooldown = true;
        Debug.Log($"Shield cooldown started: {cooldownTime} seconds.");
        yield return new WaitForSeconds(cooldownTime);
        isCooldown = false;
        SetShieldActive(true);
        Debug.Log("Shield is back online!");
    }

    private void SetShieldActive(bool active)
    {
        hasShield = active;
        if (shieldVisual != null) shieldVisual.SetActive(active);
        if (shieldIconUI != null) shieldIconUI.SetActive(active);
    }
}
