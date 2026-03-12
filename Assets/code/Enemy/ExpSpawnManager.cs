using UnityEngine;

/// <summary>
/// Quản lý spawn EXP khi Enemy chết (Instantiate từ prefab)
/// - Enemy chết → Spawn (clone) EXP từ prefab gốc
/// - Giống như cách spawn Enemy (SpawnEnemy.cs)
/// - Dựa theo thời gian game để quyết định Small/Large
/// </summary>
public class ExpSpawnManager : MonoBehaviour
{
    [Header("Prefab References")]
    [SerializeField] private GameObject smallExpPrefab; // Prefab gốc Small EXP
    [SerializeField] private GameObject largeExpPrefab; // Prefab gốc Large EXP
    
    [Header("Drop Settings")]
    [SerializeField] private float dropChance = 100f; // % khả năng rớt EXP
    [SerializeField] private int minDropCount = 1; // Số lượng EXP tối thiểu rớt
    [SerializeField] private int maxDropCount = 2; // Số lượng EXP tối đa rớt (1-2 cục)
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
    
    [Header("Spawn Parent")]
    [SerializeField] private Transform spawnParent; // Parent để chứa các EXP đã spawn (tùy chọn)
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    // Singleton
    private static ExpSpawnManager _instance;
    public static ExpSpawnManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<ExpSpawnManager>();
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
        
        // Nếu không có spawnParent, dùng chính object này
        if (spawnParent == null)
        {
            spawnParent = transform;
        }
    }
    
    private void Start()
    {
        // Tìm TimerManager
        _timerManager = FindFirstObjectByType<TimerManager>();
        
        if (_timerManager == null && showDebugInfo)
        {
            Debug.LogWarning("[ExpSpawnManager] Không tìm thấy TimerManager! Sẽ dùng phase 1 mặc định.");
        }
        
        // Validate prefabs
        if (smallExpPrefab == null)
        {
            Debug.LogError("[ExpSpawnManager] Thiếu Small EXP Prefab! Kéo prefab vào Inspector.");
        }
        
        if (largeExpPrefab == null)
        {
            Debug.LogError("[ExpSpawnManager] Thiếu Large EXP Prefab! Kéo prefab vào Inspector.");
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[ExpSpawnManager] Initialized. Small Prefab: {smallExpPrefab?.name}, Large Prefab: {largeExpPrefab?.name}");
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
            
            // Spawn EXP (Instantiate từ prefab)
            SpawnExp(expType, position);
        }
        
        if (showDebugInfo)
        {
            float currentTime = _timerManager != null ? _timerManager.GetCurrentTimeInSeconds() : 0f;
            Debug.Log($"[ExpSpawnManager] Dropped {dropCount} EXP at {position} (Time: {currentTime:F1}s)");
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
    /// Spawn viên EXP tại vị trí cụ thể (Instantiate từ prefab)
    /// Giống như SpawnEnemy.Spawn()
    /// </summary>
    private void SpawnExp(ExpType expType, Vector3 position)
    {
        GameObject prefab = expType == ExpType.Small ? smallExpPrefab : largeExpPrefab;
        
        if (prefab == null)
        {
            Debug.LogError($"[ExpSpawnManager] Thiếu prefab cho {expType} EXP!");
            return;
        }
        
        // Random vị trí trong bán kính
        Vector2 randomOffset = Random.insideUnitCircle * dropRadius;
        Vector3 spawnPosition = position + new Vector3(randomOffset.x, randomOffset.y, 0f);
        
        // Instantiate (clone) từ prefab - GIỐNG SPAWN ENEMY
        GameObject expObj = Instantiate(prefab, spawnPosition, Quaternion.identity, spawnParent);
        
        // Đảm bảo EXP được kích hoạt
        if (expObj != null && !expObj.activeSelf)
        {
            expObj.SetActive(true);
        }
        
        // Set loại EXP
        ExpItem expItem = expObj.GetComponent<ExpItem>();
        if (expItem != null)
        {
            expItem.SetExpType(expType);
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[ExpSpawnManager] Spawned {expType} EXP at {spawnPosition}");
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
    
    /// <summary>
    /// Lấy prefab theo loại (public để có thể dùng từ ngoài)
    /// </summary>
    public GameObject GetPrefab(ExpType type)
    {
        return type == ExpType.Small ? smallExpPrefab : largeExpPrefab;
    }
}
