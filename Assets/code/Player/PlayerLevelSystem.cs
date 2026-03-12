using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Hệ thống quản lý Level và EXP cho Player
/// - Tính toán EXP cần để lên level (tăng 1.5x mỗi level)
/// - Cộng EXP và tự động lên level
/// - Phát sự kiện khi lên level hoặc nhận EXP
/// </summary>
public class PlayerLevelSystem : MonoBehaviour
{
    [Header("Level Settings")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private float currentExp = 0f;
    [SerializeField] private float baseExpRequired = 100f; // EXP cần cho Level 2
    [SerializeField] private float expMultiplier = 1.5f; // Hệ số nhân mỗi level
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    // Cache EXP yêu cầu cho mỗi level để tránh tính toán lại
    private Dictionary<int, float> expRequiredCache = new Dictionary<int, float>();
    
    // Events
    public System.Action<int> OnLevelUp; // Event khi lên level (truyền level mới)
    public System.Action<float, float> OnExpGained; // Event khi nhận EXP (currentExp, requiredExp)
    public System.Action<int, float, float> OnLevelChanged; // Event tổng hợp (level, currentExp, requiredExp)
    
    // Properties
    public int CurrentLevel => currentLevel;
    public float CurrentExp => currentExp;
    public float ExpRequired => GetExpRequiredForLevel(currentLevel);
    public float ExpProgress => currentExp / ExpRequired; // Tỷ lệ % hoàn thành (0-1)
    
    private void Awake()
    {
        // Khởi tạo cache
        CalculateExpCache();
        
        if (showDebugInfo)
        {
            Debug.Log($"[PlayerLevelSystem] Initialized at Level {currentLevel}. EXP Required: {ExpRequired}");
        }
    }
    
    /// <summary>
    /// Tính toán và cache EXP yêu cầu cho các level (tối đa 100 level)
    /// </summary>
    private void CalculateExpCache()
    {
        expRequiredCache.Clear();
        
        float currentRequired = baseExpRequired;
        for (int i = 1; i <= 100; i++)
        {
            expRequiredCache[i] = currentRequired;
            
            if (showDebugInfo && i <= 10)
            {
                Debug.Log($"Level {i} → {i + 1}: {currentRequired:F0} EXP");
            }
            
            // Tính EXP cho level tiếp theo
            currentRequired = Mathf.Round(currentRequired * expMultiplier);
        }
    }
    
    /// <summary>
    /// Lấy EXP yêu cầu cho level cụ thể
    /// </summary>
    private float GetExpRequiredForLevel(int level)
    {
        if (expRequiredCache.ContainsKey(level))
        {
            return expRequiredCache[level];
        }
        
        // Fallback nếu level vượt quá cache
        return baseExpRequired * Mathf.Pow(expMultiplier, level - 1);
    }
    
    /// <summary>
    /// Thêm EXP cho Player
    /// </summary>
    /// <param name="amount">Số lượng EXP nhận được</param>
    public void AddExp(float amount)
    {
        if (amount <= 0) return;
        
        currentExp += amount;
        
        if (showDebugInfo)
        {
            Debug.Log($"[PlayerLevelSystem] Gained {amount} EXP. Current: {currentExp}/{ExpRequired}");
        }
        
        // Phát event nhận EXP
        OnExpGained?.Invoke(currentExp, ExpRequired);
        
        // Kiểm tra lên level
        CheckLevelUp();
    }
    
    /// <summary>
    /// Kiểm tra và xử lý lên level (có thể lên nhiều level cùng lúc)
    /// </summary>
    private void CheckLevelUp()
    {
        bool leveledUp = false;
        
        // Lặp để xử lý trường hợp lên nhiều level cùng lúc
        while (currentExp >= ExpRequired)
        {
            // Trừ EXP đã dùng và chuyển phần dư sang level mới
            currentExp -= ExpRequired;
            currentLevel++;
            leveledUp = true;
            
            if (showDebugInfo)
            {
                Debug.Log($"[PlayerLevelSystem] 🎉 LEVEL UP! Now Level {currentLevel}. Next: {ExpRequired} EXP");
            }
            
            // Phát event lên level
            OnLevelUp?.Invoke(currentLevel);
        }
        
        // Phát event cập nhật tổng hợp
        if (leveledUp)
        {
            OnLevelChanged?.Invoke(currentLevel, currentExp, ExpRequired);
        }
    }
    
    /// <summary>
    /// Reset về Level 1
    /// </summary>
    public void ResetLevel()
    {
        currentLevel = 1;
        currentExp = 0f;
        
        OnLevelChanged?.Invoke(currentLevel, currentExp, ExpRequired);
        
        if (showDebugInfo)
        {
            Debug.Log("[PlayerLevelSystem] Reset to Level 1");
        }
    }
    
    /// <summary>
    /// Set level cụ thể (dùng cho testing hoặc save/load)
    /// </summary>
    public void SetLevel(int level, float exp = 0f)
    {
        currentLevel = Mathf.Max(1, level);
        currentExp = Mathf.Max(0f, exp);
        
        // Đảm bảo EXP không vượt quá yêu cầu
        currentExp = Mathf.Min(currentExp, ExpRequired - 1);
        
        OnLevelChanged?.Invoke(currentLevel, currentExp, ExpRequired);
        
        if (showDebugInfo)
        {
            Debug.Log($"[PlayerLevelSystem] Set to Level {currentLevel}, EXP: {currentExp}/{ExpRequired}");
        }
    }
    
    /// <summary>
    /// Lấy tổng EXP đã nhận từ Level 1 đến Level hiện tại + EXP hiện tại
    /// </summary>
    public float GetTotalExp()
    {
        float total = currentExp;
        
        // Cộng EXP của tất cả level trước đó
        for (int i = 1; i < currentLevel; i++)
        {
            total += GetExpRequiredForLevel(i);
        }
        
        return total;
    }
}

