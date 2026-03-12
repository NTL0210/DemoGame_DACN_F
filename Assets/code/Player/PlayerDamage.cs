using UnityEngine;
using TMPro; // Sử dụng lại TextMeshPro vì FlameButtonHandler cũng dùng

public class PlayerDamage : MonoBehaviour
{
    // Singleton pattern để các script khác (như FlameAttackDamage) có thể truy cập dễ dàng
    public static PlayerDamage Instance { get; private set; }

    [Header("Damage Settings")]
    [SerializeField] private float baseDamage = 20f;
    private float currentDamage;
    private int damageLevel = 0;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI damageText;

    // Hệ số nhân sát thương CƠ BẢN theo từng cấp
    // Cấp 0: 100% (sát thương gốc)
    // Cấp 1: Tăng 25% -> 1.25
    // Cấp 2: Tăng 40% -> 1.40
    // Cấp 3: Tăng 55% -> 1.55
    // Cấp 4: Tăng 75% -> 1.75
    private readonly float[] damageMultipliers = { 1.0f, 1.25f, 1.40f, 1.55f, 1.75f };

    public float CurrentDamage => currentDamage;
    public int DamageLevel => damageLevel;

    private void Awake()
    {
        // Khởi tạo Singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        currentDamage = baseDamage;
        UpdateDamageText();
    }

    public void LevelUpDamage()
    {
        if (damageLevel < damageMultipliers.Length - 1)
        {
            damageLevel++;
            // Luôn tính toán dựa trên sát thương gốc để tránh sai số cộng dồn
            currentDamage = baseDamage * damageMultipliers[damageLevel];
            UpdateDamageText();
            Debug.Log($"[PlayerDamage] Nâng cấp sát thương lên Cấp {damageLevel}. Sát thương mới: {currentDamage}");
        }
    }

    private void UpdateDamageText()
    {
        if (damageText != null)
        {
            damageText.text = currentDamage.ToString("F0");
        }
    }

    public void ResetDamage()
    {
        damageLevel = 0;
        currentDamage = baseDamage;
        UpdateDamageText();
    }
}