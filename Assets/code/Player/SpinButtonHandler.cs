using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Xử lý sự kiện khi nhấn vào nút nâng cấp Spin Attack.
/// - Gọi hàm nâng cấp trong SpinAttackManager.
/// - Cập nhật màu sắc của các text level.
/// - Đợi animation chạy xong rồi mới tắt giao diện lựa chọn kỹ năng.
/// 
/// CÁCH SỬ DỤNG:
/// 1. Attach script này vào GameObject có Button component với tag "Spin Button"
/// 2. Trong Inspector của Button, kéo GameObject này vào On Click ()
/// 3. Chọn function: SpinButtonHandler → OnButtonClick()
/// </summary>
public class SpinButtonHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpinAttackManager spinAttackManager;
    [SerializeField] private SkillSelectionManager skillSelectionManager;

    [Header("Level Text References")]
    [SerializeField] private TextMeshProUGUI textLv1;
    [SerializeField] private TextMeshProUGUI textLv2;
    [SerializeField] private TextMeshProUGUI textLv3;
    [SerializeField] private TextMeshProUGUI textLv4;

    [Header("Settings")]
    [SerializeField] private Color activeColor = Color.black;
    [SerializeField] private Color inactiveColor = new Color(1f, 0f, 0f, 195f / 255f);
    [SerializeField] private float animationDelay = 0.3f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    private void Awake()
    {
        FindReferences();
        FindLevelTexts();
        UpdateLevelTextColors();
    }

    private void OnEnable()
    {
        UpdateLevelTextColors();
    }

    private void FindReferences()
    {
        // Tìm SpinAttackManager
        if (spinAttackManager == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                spinAttackManager = player.GetComponentInChildren<SpinAttackManager>();
            }

            if (spinAttackManager == null)
            {
                Debug.LogError("[SpinButtonHandler] Không tìm thấy SpinAttackManager!");
            }
        }

        // Tìm SkillSelectionManager
        if (skillSelectionManager == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                skillSelectionManager = player.GetComponent<SkillSelectionManager>();
            }

            if (skillSelectionManager == null)
            {
                Debug.LogError("[SpinButtonHandler] Không tìm thấy SkillSelectionManager!");
            }
        }
    }

    private void FindLevelTexts()
    {
        // Tìm các Text level trong button (children của button này)
        Transform buttonTransform = transform.Find("Skill button");
        if (buttonTransform == null)
        {
            buttonTransform = transform;
        }

        // Tìm Text lv1
        if (textLv1 == null)
        {
            Transform t = buttonTransform.Find("Text lv1");
            if (t != null) textLv1 = t.GetComponent<TextMeshProUGUI>();
        }

        // Tìm Text lv2
        if (textLv2 == null)
        {
            Transform t = buttonTransform.Find("Text lv2");
            if (t != null) textLv2 = t.GetComponent<TextMeshProUGUI>();
        }

        // Tìm Text lv3
        if (textLv3 == null)
        {
            Transform t = buttonTransform.Find("Text lv3");
            if (t != null) textLv3 = t.GetComponent<TextMeshProUGUI>();
        }

        // Tìm Text lv4
        if (textLv4 == null)
        {
            Transform t = buttonTransform.Find("Text lv4");
            if (t != null) textLv4 = t.GetComponent<TextMeshProUGUI>();
        }

        if (showDebugInfo)
        {
            int foundCount = 0;
            if (textLv1 != null) foundCount++;
            if (textLv2 != null) foundCount++;
            if (textLv3 != null) foundCount++;
            if (textLv4 != null) foundCount++;

            Debug.Log($"[SpinButtonHandler] Tìm thấy {foundCount}/4 level texts.");
        }
    }

    /// <summary>
    /// Được gọi khi button được click
    /// PUBLIC để có thể gán vào Button.OnClick() trong Inspector
    /// </summary>
    public void OnButtonClick()
    {
        if (spinAttackManager == null)
        {
            Debug.LogError("[SpinButtonHandler] SpinAttackManager is null!");
            return;
        }

        // Lấy level hiện tại
        int currentLevel = spinAttackManager.CurrentLevel;

        // Kiểm tra xem có thể nâng cấp không (max level 4)
        if (currentLevel >= 4)
        {
            if (showDebugInfo)
            {
                Debug.Log("[SpinButtonHandler] Spin Attack đã đạt max level (4)!");
            }
            return;
        }

        // Tăng level
        int newLevel = currentLevel + 1;
        spinAttackManager.LevelUp();

        if (showDebugInfo)
        {
            Debug.Log($"[SpinButtonHandler] Spin Attack nâng cấp lên Level {newLevel}");
        }

        // Cập nhật màu text
        UpdateLevelTextColors();

        // Bắt đầu Coroutine để đợi animation chạy xong
        StartCoroutine(WaitForAnimationThenHideUI());
    }

    /// <summary>
    /// Coroutine đợi animation chạy xong rồi mới ẩn UI và tiếp tục game
    /// </summary>
    private IEnumerator WaitForAnimationThenHideUI()
    {
        if (showDebugInfo)
        {
            Debug.Log($"[SpinButtonHandler] Đợi {animationDelay}s để animation chạy xong...");
        }

        // Đợi animation chạy xong (sử dụng unscaledTime vì game đang pause)
        yield return new WaitForSecondsRealtime(animationDelay);

        if (showDebugInfo)
        {
            Debug.Log("[SpinButtonHandler] Animation xong, ẩn UI và tiếp tục game.");
        }

        // Thông báo cho SkillSelectionManager để ẩn UI
        if (skillSelectionManager != null)
        {
            // Dùng mapping theo Button để tránh sai ID trong Inspector
            skillSelectionManager.SelectSkillAndUpgradeByButton(gameObject);
        }
    }

    /// <summary>
    /// Cập nhật màu sắc của các text level dựa trên level hiện tại
    /// </summary>
    private void UpdateLevelTextColors()
    {
        if (spinAttackManager == null) return;

        int currentLevel = spinAttackManager.CurrentLevel;

        SetTextColor(textLv1, currentLevel >= 1);
        SetTextColor(textLv2, currentLevel >= 2);
        SetTextColor(textLv3, currentLevel >= 3);
        SetTextColor(textLv4, currentLevel >= 4);

        if (showDebugInfo)
        {
            Debug.Log($"[SpinButtonHandler] Cập nhật màu text: Level {currentLevel}");
        }
    }

    /// <summary>
    /// Set màu cho text
    /// </summary>
    private void SetTextColor(TextMeshProUGUI text, bool isActive)
    {
        if (text == null) return;

        Color finalColor = isActive ? activeColor : inactiveColor;
        text.color = finalColor;
    }

    /// <summary>
    /// Gọi từ bên ngoài để cập nhật UI (ví dụ khi load game)
    /// </summary>
    public void RefreshUI()
    {
        UpdateLevelTextColors();
    }
}

