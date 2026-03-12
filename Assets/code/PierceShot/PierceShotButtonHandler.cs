using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Button nâng cấp skill Pierce Shot
/// - Theo đúng pattern các Button Handler hiện có
/// - Tăng level nội bộ (1->4) thông qua PierceShotManager
/// - Cập nhật UI level text, đợi animationDelay rồi báo SkillSelectionManager bằng SelectSkillAndUpgradeByButton
/// </summary>
public class PierceShotButtonHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PierceShotManager pierceShotManager;
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
        if (pierceShotManager == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) pierceShotManager = player.GetComponentInChildren<PierceShotManager>(true);
            if (pierceShotManager == null) Debug.LogError("[PierceShotButtonHandler] Không tìm thấy PierceShotManager!");
        }

        if (skillSelectionManager == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) skillSelectionManager = player.GetComponent<SkillSelectionManager>();
            if (skillSelectionManager == null) Debug.LogError("[PierceShotButtonHandler] Không tìm thấy SkillSelectionManager!");
        }
    }

    private void FindLevelTexts()
    {
        Transform buttonTransform = transform.Find("Skill button");
        if (buttonTransform == null) buttonTransform = transform;

        if (textLv1 == null) textLv1 = buttonTransform.Find("Text lv1")?.GetComponent<TextMeshProUGUI>();
        if (textLv2 == null) textLv2 = buttonTransform.Find("Text lv2")?.GetComponent<TextMeshProUGUI>();
        if (textLv3 == null) textLv3 = buttonTransform.Find("Text lv3")?.GetComponent<TextMeshProUGUI>();
        if (textLv4 == null) textLv4 = buttonTransform.Find("Text lv4")?.GetComponent<TextMeshProUGUI>();
    }

    public void OnButtonClick()
    {
        if (pierceShotManager == null) { Debug.LogError("[PierceShotButtonHandler] pierceShotManager null!"); return; }

        int lv = pierceShotManager.CurrentLevel;
        if (lv >= 4)
        {
            if (showDebugInfo) Debug.Log("[PierceShotButtonHandler] Pierce Shot đã đạt max level.");
            return;
        }

        pierceShotManager.LevelUp();
        if (showDebugInfo) Debug.Log($"[PierceShotButtonHandler] Pierce Shot nâng lên Lv{pierceShotManager.CurrentLevel}");

        UpdateLevelTextColors();
        StartCoroutine(WaitAndNotify());
    }

    private IEnumerator WaitAndNotify()
    {
        yield return new WaitForSecondsRealtime(animationDelay);
        if (skillSelectionManager != null)
            skillSelectionManager.SelectSkillAndUpgradeByButton(gameObject);
    }

    private void UpdateLevelTextColors()
    {
        if (pierceShotManager == null) return;
        int lv = pierceShotManager.CurrentLevel;
        SetTextColor(textLv1, lv >= 1);
        SetTextColor(textLv2, lv >= 2);
        SetTextColor(textLv3, lv >= 3);
        SetTextColor(textLv4, lv >= 4);
    }

    private void SetTextColor(TextMeshProUGUI t, bool active)
    {
        if (t == null) return;
        var c = active ? activeColor : inactiveColor;
        if (active) c.a = 1f;
        t.color = c;
    }
}

