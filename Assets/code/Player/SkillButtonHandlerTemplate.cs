using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// TEMPLATE: Script mẫu cho các skill button khác
/// Copy file này và chỉnh sửa để tạo handler cho skill mới
/// 
/// HƯỚNG DẪN SỬ DỤNG:
/// 1. Copy file này và đổi tên (ví dụ: IceButtonHandler.cs)
/// 2. Đổi tên class (ví dụ: IceButtonHandler)
/// 3. Thay đổi logic nâng cấp skill trong OnButtonClick()
/// 4. Attach vào skill button tương ứng
/// </summary>
[RequireComponent(typeof(Button))]
public class SkillButtonHandlerTemplate : MonoBehaviour
{
    [Header("References")]
        [Header("Skill Settings")]
    [SerializeField] private string skillId; // ID của kỹ năng, ví dụ: "spin_attack"

    [Header("References")]
    [SerializeField] private SkillSelectionManager skillSelectionManager;
    // TODO: Thêm reference đến skill manager của bạn (ví dụ: IceAttackManager)
    // [SerializeField] private YourSkillManager yourSkillManager;
    
    [Header("Level Text References")]
    [SerializeField] private TextMeshProUGUI textLv1;
    [SerializeField] private TextMeshProUGUI textLv2;
    [SerializeField] private TextMeshProUGUI textLv3;
    [SerializeField] private TextMeshProUGUI textLv4;
    
    [Header("Settings")]
    [SerializeField] private Color activeColor = Color.black;
    [SerializeField] private Color inactiveColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private int maxLevel = 4;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    private Button button;
    private int currentLevel = 1;
    
    private void Awake()
    {
        button = GetComponent<Button>();
        
        // Tìm references
        FindReferences();
        FindLevelTexts();
        
        // Đăng ký sự kiện click
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
        }
        
        // Cập nhật UI ban đầu
        UpdateLevelTextColors();
    }
    
    private void FindReferences()
    {
        // Tìm SkillSelectionManager
        if (skillSelectionManager == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                skillSelectionManager = player.GetComponent<SkillSelectionManager>();
            }
        }
        
        // TODO: Tìm skill manager của bạn
        // if (yourSkillManager == null)
        // {
        //     GameObject player = GameObject.FindGameObjectWithTag("Player");
        //     if (player != null)
        //     {
        //         yourSkillManager = player.GetComponentInChildren<YourSkillManager>();
        //     }
        // }
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
    }
    
    /// <summary>
    /// Được gọi khi button được click
    /// </summary>
    private void OnButtonClick()
    {
        // Kiểm tra max level
        if (currentLevel >= maxLevel)
        {
            if (showDebugInfo)
            {
                Debug.Log($"[{GetType().Name}] Skill đã đạt max level ({maxLevel})!");
            }
            return;
        }
        
        // TODO: Thay đổi logic nâng cấp skill của bạn ở đây
        // Ví dụ:
        // if (yourSkillManager != null)
        // {
        //     int newLevel = yourSkillManager.CurrentLevel + 1;
        //     yourSkillManager.SetLevel(newLevel);
        //     currentLevel = newLevel;
        // }
        
        // DEMO: Tăng level local (xóa dòng này khi implement thật)
        currentLevel++;
        
        if (showDebugInfo)
        {
            Debug.Log($"[{GetType().Name}] Skill nâng cấp lên Level {currentLevel}");
        }
        
        // Cập nhật UI
        UpdateLevelTextColors();
        
        // Thông báo SkillSelectionManager để ẩn UI
        if (skillSelectionManager != null)
        {
            skillSelectionManager.SelectSkillAndUpgrade(skillId);
        }
    }
    
    /// <summary>
    /// Cập nhật màu sắc của các text level
    /// </summary>
    private void UpdateLevelTextColors()
    {
        // Reset tất cả về màu inactive
        SetTextColor(textLv1, inactiveColor);
        SetTextColor(textLv2, inactiveColor);
        SetTextColor(textLv3, inactiveColor);
        SetTextColor(textLv4, inactiveColor);
        
        // Set màu active cho level hiện tại
        switch (currentLevel)
        {
            case 1:
                SetTextColor(textLv1, activeColor);
                break;
            case 2:
                SetTextColor(textLv2, activeColor);
                break;
            case 3:
                SetTextColor(textLv3, activeColor);
                break;
            case 4:
                SetTextColor(textLv4, activeColor);
                break;
        }
    }
    
    /// <summary>
    /// Set màu cho text
    /// </summary>
    private void SetTextColor(TextMeshProUGUI text, Color color)
    {
        if (text == null) return;
        
        Color finalColor = color;
        if (color == activeColor)
        {
            finalColor.a = 1f; // Alpha = 255
        }
        
        text.color = finalColor;
    }
    
    /// <summary>
    /// Refresh UI từ bên ngoài
    /// </summary>
    public void RefreshUI()
    {
        UpdateLevelTextColors();
    }
    
    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClick);
        }
    }
}

