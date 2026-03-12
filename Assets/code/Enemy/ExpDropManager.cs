using UnityEngine;

/// <summary>
/// Quản lý rớt EXP từ Enemy dựa theo thời gian trận đấu
/// - 0-4 phút: chỉ rớt Small EXP (100%)
/// - 4-10 phút: Small 80%, Large 20%
/// - 10-15 phút: Small 70%, Large 30%
/// - 15+ phút: Small 60%, Large 40%
/// </summary>
public class ExpDropManager : MonoBehaviour
{
    [Header("Prefab References")]
    [SerializeField] private GameObject smallExpPrefab;
    [SerializeField] private GameObject largeExpPrefab;
    
    [Header("Drop Settings")]
    [SerializeField] private float dropChance = 100f; // % khả năng rớt EXP (100 = luôn rớt)
    [SerializeField] private int minDropCount = 1; // Số lượng EXP tối thiểu rớt
    [SerializeField] private int maxDropCount = 3; // Số lượng EXP tối đa rớt
    [SerializeField] private float dropRadius = 0.5f; // Bán kính rớt EXP xung quanh vị trí
    
    [Header("Time-based Drop Rates")]
    [SerializeField] private float phase1Time = 240f; // 0-4 phút (giây)
    [SerializeField] private float phase2Time = 600f; // 4-10 phút (giây)
    [SerializeField] private float phase3Time = 900f; // 10-15 phút (giây)
    
    [Header("Phase 1 (0-4 min): 100% Small")]
    [SerializeField] private float phase1SmallChance = 100f;
    
    [Header("Phase 2 (4-10 min): 80% Small, 20% Large")]
    [SerializeField] private float phase2SmallChance = 80f;
    
    [Header("Phase 3 (10-15 min): 70% Small, 30% Large")]
    [SerializeField] private float phase3SmallChance = 70f;
    
    [Header("Phase 4 (15+ min): 60% Small, 40% Large")]
    [SerializeField] private float phase4SmallChance = 60f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    // Singleton
    private static ExpDropManager _instance;
    public static ExpDropManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<ExpDropManager>();
            }
            return _instance;
        }
    }
    
    // References
    private TimerManager _timerManager;
    
    private void Awake()
    {
        // Singleton pattern
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }
    
    private void Start()
    {
        // Tìm TimerManager
        _timerManager = FindFirstObjectByType<TimerManager>();
        
        if (_timerManager == null && showDebugInfo)
        {
            Debug.LogWarning("[ExpDropManager] Không tìm thấy TimerManager! Sẽ dùng phase 1 mặc định.");
        }
        
        // Validate prefabs
        if (smallExpPrefab == null)
        {
            Debug.LogError("[ExpDropManager] Thiếu Small EXP Prefab! Hãy gán trong Inspector.");
        }
        
        if (largeExpPrefab == null)
        {
            Debug.LogError("[ExpDropManager] Thiếu Large EXP Prefab! Hãy gán trong Inspector.");
        }
    }
    
    /// <summary>
    /// Rớt EXP tại vị trí cụ thể (gọi từ Enemy khi chết)
    /// </summary>
    /// <param name="position">Vị trí rớt EXP</param>
    public void DropExp(Vector3 position)
    {
        // Kiểm tra khả năng rớt
        if (Random.Range(0f, 100f) > dropChance)
        {
            return;
        }
        
        // Số lượng EXP rớt
        int dropCount = Random.Range(minDropCount, maxDropCount + 1);
        
        for (int i = 0; i < dropCount; i++)
        {
            // Tính loại EXP dựa theo thời gian
            ExpType expType = GetExpTypeBasedOnTime();
            
            // Spawn EXP
            SpawnExp(expType, position);
        }
        
        if (showDebugInfo)
        {
            float currentTime = _timerManager != null ? _timerManager.GetCurrentTimeInSeconds() : 0f;
            Debug.Log($"[ExpDropManager] Dropped {dropCount} EXP at {position} (Time: {currentTime:F1}s)");
        }
    }
    
    /// <summary>
    /// Xác định loại EXP dựa theo thời gian trận đấu
    /// </summary>
    private ExpType GetExpTypeBasedOnTime()
    {
        float currentTime = _timerManager != null ? _timerManager.GetCurrentTimeInSeconds() : 0f;
        float smallChance;
        
        // Xác định phase và tỉ lệ Small EXP
        if (currentTime < phase1Time)
        {
            // Phase 1: 0-4 phút
            smallChance = phase1SmallChance;
        }
        else if (currentTime < phase2Time)
        {
            // Phase 2: 4-10 phút
            smallChance = phase2SmallChance;
        }
        else if (currentTime < phase3Time)
        {
            // Phase 3: 10-15 phút
            smallChance = phase3SmallChance;
        }
        else
        {
            // Phase 4: 15+ phút
            smallChance = phase4SmallChance;
        }
        
        // Random loại EXP
        float randomValue = Random.Range(0f, 100f);
        return randomValue <= smallChance ? ExpType.Small : ExpType.Large;
    }
    
    /// <summary>
    /// Spawn viên EXP tại vị trí cụ thể
    /// </summary>
    private void SpawnExp(ExpType expType, Vector3 position)
    {
        GameObject prefab = expType == ExpType.Small ? smallExpPrefab : largeExpPrefab;
        
        if (prefab == null)
        {
            Debug.LogError($"[ExpDropManager] Thiếu prefab cho {expType} EXP!");
            return;
        }
        
        // Random vị trí trong bán kính
        Vector2 randomOffset = Random.insideUnitCircle * dropRadius;
        Vector3 spawnPosition = position + new Vector3(randomOffset.x, randomOffset.y, 0f);
        
        // Spawn EXP
        GameObject expObj = Instantiate(prefab, spawnPosition, Quaternion.identity);
        
        // Set loại EXP
        ExpItem expItem = expObj.GetComponent<ExpItem>();
        if (expItem != null)
        {
            expItem.SetExpType(expType);
        }
    }
    
    /// <summary>
    /// Spawn EXP cụ thể (dùng cho testing hoặc trường hợp đặc biệt)
    /// </summary>
    public void SpawnSpecificExp(ExpType expType, Vector3 position, int count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnExp(expType, position);
        }
    }
    
    /// <summary>
    /// Lấy tỉ lệ rớt Small EXP hiện tại (dựa theo thời gian)
    /// </summary>
    public float GetCurrentSmallExpChance()
    {
        float currentTime = _timerManager != null ? _timerManager.GetCurrentTimeInSeconds() : 0f;
        
        if (currentTime < phase1Time)
            return phase1SmallChance;
        else if (currentTime < phase2Time)
            return phase2SmallChance;
        else if (currentTime < phase3Time)
            return phase3SmallChance;
        else
            return phase4SmallChance;
    }
    
    /// <summary>
    /// Lấy phase hiện tại (1-4)
    /// </summary>
    public int GetCurrentPhase()
    {
        float currentTime = _timerManager != null ? _timerManager.GetCurrentTimeInSeconds() : 0f;
        
        if (currentTime < phase1Time)
            return 1;
        else if (currentTime < phase2Time)
            return 2;
        else if (currentTime < phase3Time)
            return 3;
        else
            return 4;
    }
}

