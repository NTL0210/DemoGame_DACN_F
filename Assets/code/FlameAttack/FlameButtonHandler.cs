using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Xử lý nâng cấp Flame Attack khi click vào button
/// - Tăng level của FlameAttack
/// - Cập nhật màu text level tương ứng
/// - Đợi animation chạy xong
/// - Thông báo cho SkillSelectionManager để ẩn UI
/// 
/// CÁCH SỬ DỤNG:
/// 1. Attach script này vào GameObject có Button component
/// 2. Trong Inspector của Button, kéo GameObject này vào On Click ()
/// 3. Chọn function: FlameButtonHandler → OnButtonClick()
/// </summary>
public class FlameButtonHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FlameAttackManager flameAttackManager;
    [SerializeField] private SkillSelectionManager skillSelectionManager;
    
    [Header("Level Text References")]
    [SerializeField] private TextMeshProUGUI textLv1;
    [SerializeField] private TextMeshProUGUI textLv2;
    [SerializeField] private TextMeshProUGUI textLv3;
    [SerializeField] private TextMeshProUGUI textLv4;
    
    [Header("Settings")]
    [SerializeField] private Color activeColor = Color.black; // Màu đen cho level đã nâng cấp
    [SerializeField] private Color inactiveColor = new Color(1f, 0f, 0f, 195f/255f); // Màu đỏ mờ (R:255, G:0, B:0, A:195)
    [SerializeField] private float animationDelay = 0.3f; // Thời gian đợi animation chạy xong (giây)
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    private void Awake()
    {
        // Tìm references nếu chưa được gán
        FindReferences();
        
        // Tìm các Text level trong button
        FindLevelTexts();
        
        // Cập nhật màu text ban đầu
        UpdateLevelTextColors();
    }
    
    private void FindReferences()
    {
        // Tìm FlameAttackManager
        if (flameAttackManager == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                flameAttackManager = player.GetComponentInChildren<FlameAttackManager>();
            }
            
            if (flameAttackManager == null)
            {
                Debug.LogError("[FlameButtonHandler] Không tìm thấy FlameAttackManager!");
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
                Debug.LogError("[FlameButtonHandler] Không tìm thấy SkillSelectionManager!");
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
            
            Debug.Log($"[FlameButtonHandler] Tìm thấy {foundCount}/4 level texts.");
        }
    }
    
    /// <summary>
    /// Được gọi khi button được click
    /// PUBLIC để có thể gán vào Button.OnClick() trong Inspector
    /// </summary>
    public void OnButtonClick()
    {
        if (flameAttackManager == null)
        {
            Debug.LogError("[FlameButtonHandler] FlameAttackManager is null!");
            return;
        }
        
        // Lấy level hiện tại
        int currentLevel = flameAttackManager.CurrentLevel;
        
        // Kiểm tra xem có thể nâng cấp không (max level 4)
        if (currentLevel >= 4)
        {
            if (showDebugInfo)
            {
                Debug.Log("[FlameButtonHandler] Flame Attack đã đạt max level (4)!");
            }
            return;
        }
        
        // Tăng level
        int newLevel = currentLevel + 1;
        flameAttackManager.SetLevel(newLevel);
        
        if (showDebugInfo)
        {
            Debug.Log($"[FlameButtonHandler] Flame Attack nâng cấp lên Level {newLevel}");
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
            Debug.Log($"[FlameButtonHandler] Đợi {animationDelay}s để animation chạy xong...");
        }
        
        // Đợi animation chạy xong (sử dụng unscaledTime vì game đang pause)
        yield return new WaitForSecondsRealtime(animationDelay);
        
        if (showDebugInfo)
        {
            Debug.Log("[FlameButtonHandler] Animation xong, ẩn UI và tiếp tục game.");
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
    /// - Level từ 1 đến currentLevel: Màu đen (active)
    /// - Level > currentLevel: Màu đỏ mờ (inactive)
    /// </summary>
    private void UpdateLevelTextColors()
    {
        if (flameAttackManager == null) return;
        
        int currentLevel = flameAttackManager.CurrentLevel;
        
        // Set màu cho từng level
        // Level 1: Active nếu currentLevel >= 1, ngược lại Inactive
        SetTextColor(textLv1, currentLevel >= 1 ? activeColor : inactiveColor);
        
        // Level 2: Active nếu currentLevel >= 2, ngược lại Inactive
        SetTextColor(textLv2, currentLevel >= 2 ? activeColor : inactiveColor);
        
        // Level 3: Active nếu currentLevel >= 3, ngược lại Inactive
        SetTextColor(textLv3, currentLevel >= 3 ? activeColor : inactiveColor);
        
        // Level 4: Active nếu currentLevel >= 4, ngược lại Inactive
        SetTextColor(textLv4, currentLevel >= 4 ? activeColor : inactiveColor);
        
        if (showDebugInfo)
        {
            Debug.Log($"[FlameButtonHandler] Cập nhật màu text: Level {currentLevel} - Lv1-{currentLevel} = đen, Lv{currentLevel+1}-4 = đỏ mờ");
        }
    }
    
    /// <summary>
    /// Set màu cho text (bao gồm cả alpha)
    /// </summary>
    private void SetTextColor(TextMeshProUGUI text, Color color)
    {
        if (text == null) return;
        
        // Set màu với alpha = 1 (255) cho active, hoặc giá trị custom cho inactive
        Color finalColor = color;
        if (color == activeColor)
        {
            finalColor.a = 1f; // Alpha = 255
        }
        
        text.color = finalColor;
    }
    
    /// <summary>
    /// Gọi từ bên ngoài để cập nhật UI (ví dụ khi load game)
    /// </summary>
    public void RefreshUI()
    {
        UpdateLevelTextColors();
    }
    
    // OnDestroy đã xóa - không cần nữa vì không tự động đăng ký sự kiện
    // Sự kiện được gán thủ công trong Inspector của Button component
}

