using UnityEngine;
using TMPro;
using System.Collections;
using TMPro; // Thêm thư viện cho TextMeshPro

/// <summary>
/// Xử lý sự kiện khi nhấn vào nút nâng cấp Máu (Heart Up).
/// </summary>
public class HeartUpButtonHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SkillSelectionManager skillSelectionManager;
    [SerializeField] private HealthSystem healthSystem;

    [Header("Level Text References")]
    [SerializeField] private TextMeshProUGUI textLv1;
    [SerializeField] private TextMeshProUGUI textLv2;
    [SerializeField] private TextMeshProUGUI textLv3;
    [SerializeField] private TextMeshProUGUI textLv4;

    [Header("Settings")]
    [SerializeField] private Color activeColor = Color.black;
    [SerializeField] private Color inactiveColor = new Color(1f, 0f, 0f, 195f / 255f);
    [SerializeField] private float animationDelay = 0.3f;

    private void Awake()
    {
        if (skillSelectionManager == null) skillSelectionManager = FindObjectOfType<SkillSelectionManager>();
        if (healthSystem == null) healthSystem = FindObjectOfType<HealthSystem>();
        UpdateLevelTextColors();
    }

    private void OnEnable()
    {
        UpdateLevelTextColors();
    }

    public void OnButtonClick()
    {
        if (healthSystem != null)
        {
            healthSystem.LevelUp();
            UpdateLevelTextColors();
            StartCoroutine(WaitForAnimationThenHideUI());
        }
        else
        {
            Debug.LogError("[HeartUpButtonHandler] HealthSystem not found!");
        }
    }

    private IEnumerator WaitForAnimationThenHideUI()
    {
        yield return new WaitForSecondsRealtime(animationDelay);
        if (skillSelectionManager != null) skillSelectionManager.SelectSkillAndUpgradeByButton(gameObject);
    }

    private void UpdateLevelTextColors()
    {
        if (healthSystem == null) return;
        int currentLevel = healthSystem.GetHeartLevel();
        SetTextColor(textLv1, currentLevel >= 1);
        SetTextColor(textLv2, currentLevel >= 2);
        SetTextColor(textLv3, currentLevel >= 3);
        SetTextColor(textLv4, currentLevel >= 4);
    }

    private void SetTextColor(TextMeshProUGUI text, bool isActive)
    {
        if (text != null) text.color = isActive ? activeColor : inactiveColor;
    }
}
