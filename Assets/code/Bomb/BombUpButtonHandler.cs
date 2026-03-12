using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Xử lý sự kiện khi nhấn vào nút nâng cấp Bomb.
/// - Pattern giống hệt SpinButtonHandler
/// </summary>
public class BombUpButtonHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BombSkillManager bombSkillManager;
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
        if (bombSkillManager == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                bombSkillManager = player.GetComponentInChildren<BombSkillManager>(true);
            }
            if (bombSkillManager == null)
            {
                Debug.LogError("[BombUpButtonHandler] Không tìm thấy BombSkillManager!");
            }
        }

        if (skillSelectionManager == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                skillSelectionManager = player.GetComponent<SkillSelectionManager>();
            }
            if (skillSelectionManager == null)
            {
                Debug.LogError("[BombUpButtonHandler] Không tìm thấy SkillSelectionManager!");
            }
        }
    }

    private void FindLevelTexts()
    {
        Transform buttonTransform = transform.Find("Skill button");
        if (buttonTransform == null)
        {
            buttonTransform = transform;
        }

        if (textLv1 == null)
        {
            Transform t = buttonTransform.Find("Text lv1");
            if (t != null) textLv1 = t.GetComponent<TextMeshProUGUI>();
        }
        if (textLv2 == null)
        {
            Transform t = buttonTransform.Find("Text lv2");
            if (t != null) textLv2 = t.GetComponent<TextMeshProUGUI>();
        }
        if (textLv3 == null)
        {
            Transform t = buttonTransform.Find("Text lv3");
            if (t != null) textLv3 = t.GetComponent<TextMeshProUGUI>();
        }
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
            Debug.Log($"[BombUpButtonHandler] Tìm thấy {foundCount}/4 level texts.");
        }
    }

    public void OnButtonClick()
    {
        if (bombSkillManager == null)
        {
            Debug.LogError("[BombUpButtonHandler] BombSkillManager is null!");
            return;
        }

        int currentLevel = bombSkillManager.CurrentLevel;
        if (currentLevel >= 4)
        {
            if (showDebugInfo)
            {
                Debug.Log("[BombUpButtonHandler] Bomb đã đạt max level (4)!");
            }
            return;
        }

        bombSkillManager.LevelUp();
        int newLevel = bombSkillManager.CurrentLevel;
        if (showDebugInfo)
        {
            Debug.Log($"[BombUpButtonHandler] Bomb nâng cấp lên Level {newLevel}");
        }

        UpdateLevelTextColors();
        StartCoroutine(WaitForAnimationThenHideUI());
    }

    private IEnumerator WaitForAnimationThenHideUI()
    {
        if (showDebugInfo)
        {
            Debug.Log($"[BombUpButtonHandler] Đợi {animationDelay}s để animation chạy xong...");
        }
        yield return new WaitForSecondsRealtime(animationDelay);

        if (showDebugInfo)
        {
            Debug.Log("[BombUpButtonHandler] Animation xong, ẩn UI và tiếp tục game.");
        }

        if (skillSelectionManager != null)
        {
            skillSelectionManager.SelectSkillAndUpgradeByButton(gameObject);
        }
    }

    private void UpdateLevelTextColors()
    {
        if (bombSkillManager == null) return;
        int currentLevel = bombSkillManager.CurrentLevel;
        SetTextColor(textLv1, currentLevel >= 1);
        SetTextColor(textLv2, currentLevel >= 2);
        SetTextColor(textLv3, currentLevel >= 3);
        SetTextColor(textLv4, currentLevel >= 4);

        if (showDebugInfo)
        {
            Debug.Log($"[BombUpButtonHandler] Cập nhật màu text: Level {currentLevel}");
        }
    }

    private void SetTextColor(TextMeshProUGUI text, bool isActive)
    {
        if (text == null) return;
        Color finalColor = isActive ? activeColor : inactiveColor;
        text.color = finalColor;
    }

    public void RefreshUI()
    {
        UpdateLevelTextColors();
    }
}


