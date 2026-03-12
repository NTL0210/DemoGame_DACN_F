using UnityEngine;
using TMPro;
using System.Collections; // Thêm thư viện để sử dụng Coroutine

/// <summary>
/// Xử lý sự kiện khi nhấn vào nút nâng cấp Sát thương (Damage Up).
/// - Gọi hàm nâng cấp trong PlayerDamage.
/// - Cập nhật màu sắc của các text level.
/// - Đợi animation chạy xong rồi mới tắt giao diện lựa chọn kỹ năng.
/// </summary>
public class DamageUpButtonHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SkillSelectionManager skillSelectionManager;

    [Header("Level Text References")]
    [SerializeField] private TextMeshProUGUI textLv1;
    [SerializeField] private TextMeshProUGUI textLv2;
    [SerializeField] private TextMeshProUGUI textLv3;
    [SerializeField] private TextMeshProUGUI textLv4;

    [Header("Settings")]
    [SerializeField] private Color activeColor = Color.black;
    [SerializeField] private Color inactiveColor = new Color(1f, 0f, 0f, 195f / 255f);
    [SerializeField] private float animationDelay = 0.3f; // Thêm delay cho animation

    private void Awake()
    {
        if (skillSelectionManager == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) skillSelectionManager = player.GetComponent<SkillSelectionManager>();
        }
        UpdateLevelTextColors();
    }

    private void OnEnable()
    {
        UpdateLevelTextColors();
    }

    public void OnButtonClick()
    {
        if (PlayerDamage.Instance != null)
        {
            PlayerDamage.Instance.LevelUpDamage();
            UpdateLevelTextColors();

            // Bắt đầu Coroutine để đợi animation chạy xong
            StartCoroutine(WaitForAnimationThenHideUI());
        }
        else
        {
            Debug.LogError("[DamageUpButtonHandler] PlayerDamage.Instance không tồn tại trong scene!");
        }
    }

    /// <summary>
    /// Coroutine đợi animation chạy xong rồi mới ẩn UI và tiếp tục game
    /// </summary>
    private IEnumerator WaitForAnimationThenHideUI()
    {
        // Đợi animation chạy xong (sử dụng unscaledTime vì game đang pause)
        yield return new WaitForSecondsRealtime(animationDelay);

        if (skillSelectionManager != null)
        {
            // Dùng mapping theo Button để tránh sai ID trong Inspector
            skillSelectionManager.SelectSkillAndUpgradeByButton(gameObject);
        }
    }

    private void UpdateLevelTextColors()
    {
        if (PlayerDamage.Instance == null) return;

        int currentLevel = PlayerDamage.Instance.DamageLevel;

        SetTextColor(textLv1, currentLevel >= 1);
        SetTextColor(textLv2, currentLevel >= 2);
        SetTextColor(textLv3, currentLevel >= 3);
        SetTextColor(textLv4, currentLevel >= 4);
    }

    private void SetTextColor(TextMeshProUGUI text, bool isActive)
    {
        if (text != null)
        {
            text.color = isActive ? activeColor : inactiveColor;
        }
    }
}