using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Hệ thống tự động gộp 10 Small EXP gần nhau thành 1 Large EXP
/// - Quét định kỳ để tìm các nhóm Small EXP gần nhau
/// - Khi có 5 Small EXP trong vùng merge radius → gộp thành 1 Large EXP
/// - Giảm lag bằng cách giảm số lượng object EXP trên scene
/// </summary>
public class ExpMergeSystem : MonoBehaviour
{
    [Header("Merge Settings")]
    [SerializeField] private float mergeRadius = 1.5f; // Bán kính để coi là "gần nhau"
    [SerializeField] private int mergeCount = 10; // Số Small EXP cần để gộp thành 1 Large
    [SerializeField] private float scanInterval = 0.5f; // Thời gian quét (giây)
    [SerializeField] private float mergeStartTime = 120f; // Thời gian bắt đầu merge (2 phút = 120 giây)
    
    [Header("Prefab Reference")]
    [SerializeField] private GameObject largeExpPrefab; // Prefab Large EXP để spawn sau merge
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    [SerializeField] private bool showGizmos = false;
    
    // State
    private float _scanTimer = 0f;
    private List<ExpItem> _smallExpItems = new List<ExpItem>();
    
    // Singleton
    private static ExpMergeSystem _instance;
    public static ExpMergeSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<ExpMergeSystem>();
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
        if (largeExpPrefab == null)
        {
            Debug.LogError("[ExpMergeSystem] Thiếu Large EXP Prefab! Hãy gán trong Inspector.");
        }
        
        // Tìm TimerManager
        _timerManager = FindFirstObjectByType<TimerManager>();
        
        if (_timerManager == null && showDebugInfo)
        {
            Debug.LogWarning("[ExpMergeSystem] Không tìm thấy TimerManager! Merge sẽ hoạt động ngay từ đầu.");
        }
    }
    
    private void Update()
    {
        _scanTimer += Time.deltaTime;
        
        if (_scanTimer >= scanInterval)
        {
            _scanTimer = 0f;
            ScanAndMergeExp();
        }
    }
    
    /// <summary>
    /// Quét và gộp các Small EXP gần nhau
    /// LOGIC: Kiểm tra TỪNG cục Small EXP trên map → đếm có bao nhiêu cục Small khác trong vùng mergeRadius
    /// Nếu đủ 5 cục → gộp thành 1 Large EXP
    /// ⚠️ CHỈ MERGE SAU 4 PHÚT ĐẦU (vì 4p đầu chỉ có Small EXP)
    /// </summary>
    private void ScanAndMergeExp()
    {
        // Kiểm tra thời gian game - CHỈ merge sau 4 phút
        float currentTime = _timerManager != null ? _timerManager.GetCurrentTimeInSeconds() : 0f;
        
        if (currentTime < mergeStartTime)
        {
            // Chưa đến 4 phút → KHÔNG merge
            return;
        }
        
        // Tìm tất cả Small EXP hiện có trên toàn map
        FindAllSmallExp();
        
        if (_smallExpItems.Count < mergeCount)
        {
            return; // Không đủ để merge
        }
        
        // Tìm các nhóm Small EXP gần nhau (clustering)
        List<List<ExpItem>> clusters = FindExpClusters();
        
        // Merge các nhóm đủ điều kiện (>= 5 cục)
        foreach (var cluster in clusters)
        {
            if (cluster.Count >= mergeCount)
            {
                MergeExpCluster(cluster);
            }
        }
    }
    
    /// <summary>
    /// Tìm tất cả Small EXP trên scene
    /// </summary>
    private void FindAllSmallExp()
    {
        _smallExpItems.Clear();
        
        // Tìm tất cả ExpItem
        ExpItem[] allExpItems = FindObjectsByType<ExpItem>(FindObjectsSortMode.None);
        
        foreach (var expItem in allExpItems)
        {
            if (expItem != null && expItem.GetExpType() == ExpType.Small)
            {
                _smallExpItems.Add(expItem);
            }
        }
    }
    
    /// <summary>
    /// Tìm các cụm Small EXP gần nhau (clustering algorithm)
    /// LOGIC: 
    /// 1. Duyệt qua TẤT CẢ Small EXP trên map
    /// 2. Với MỖI cục Small EXP, kiểm tra vùng tròn radius xung quanh nó
    /// 3. Đếm có bao nhiêu cục Small khác trong vùng đó
    /// 4. Nếu đủ 5 cục → tạo 1 cluster để merge
    /// 5. Dùng BFS (Breadth-First Search) để tìm cụm liên thông
    /// </summary>
    private List<List<ExpItem>> FindExpClusters()
    {
        List<List<ExpItem>> clusters = new List<List<ExpItem>>();
        HashSet<ExpItem> processedItems = new HashSet<ExpItem>();
        
        // Duyệt qua TẤT CẢ Small EXP trên toàn map
        foreach (var expItem in _smallExpItems)
        {
            if (processedItems.Contains(expItem))
                continue;
            
            // Tạo cluster mới cho cục EXP này
            List<ExpItem> cluster = new List<ExpItem>();
            Queue<ExpItem> queue = new Queue<ExpItem>();
            
            queue.Enqueue(expItem);
            processedItems.Add(expItem);
            
            // BFS: Tìm TẤT CẢ EXP trong vùng merge radius (liên thông)
            while (queue.Count > 0)
            {
                ExpItem current = queue.Dequeue();
                cluster.Add(current);
                
                // Kiểm tra XUNG QUANH cục current trong vùng radius
                foreach (var nearby in _smallExpItems)
                {
                    if (processedItems.Contains(nearby))
                        continue;
                    
                    // Tính khoảng cách giữa current và nearby
                    float distance = Vector2.Distance(current.transform.position, nearby.transform.position);
                    
                    // Nếu nearby nằm trong vùng mergeRadius → thêm vào cluster
                    if (distance <= mergeRadius)
                    {
                        queue.Enqueue(nearby);
                        processedItems.Add(nearby);
                    }
                }
            }
            
            // Chỉ thêm cluster vào danh sách nếu đủ 5 cục
            if (cluster.Count >= mergeCount)
            {
                clusters.Add(cluster);
            }
        }
        
        return clusters;
    }
    
    /// <summary>
    /// Gộp một cụm Small EXP thành Large EXP
    /// LOGIC:
    /// 1. Lấy 5 cục Small EXP đầu tiên trong cluster
    /// 2. Tính vị trí trung tâm của 5 cục đó
    /// 3. Destroy 5 cục Small EXP
    /// 4. Spawn 1 Large EXP tại vị trí trung tâm
    /// </summary>
    private void MergeExpCluster(List<ExpItem> cluster)
    {
        if (cluster.Count < mergeCount || largeExpPrefab == null)
            return;
        
        // Lấy ĐÚNG 5 viên Small EXP đầu tiên để merge
        List<ExpItem> toMerge = cluster.Take(mergeCount).ToList();
        
        // Tính vị trí trung tâm của 5 cục Small EXP
        Vector3 centerPosition = Vector3.zero;
        foreach (var expItem in toMerge)
        {
            centerPosition += expItem.transform.position;
        }
        centerPosition /= toMerge.Count;
        
        // Destroy 5 cục Small EXP
        foreach (var expItem in toMerge)
        {
            if (expItem != null)
            {
                Destroy(expItem.gameObject);
            }
        }
        
        // Spawn 1 Large EXP tại vị trí trung tâm
        GameObject largeExpObj = Instantiate(largeExpPrefab, centerPosition, Quaternion.identity);
        
        ExpItem largeExpItem = largeExpObj.GetComponent<ExpItem>();
        if (largeExpItem != null)
        {
            largeExpItem.SetExpType(ExpType.Large);
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[ExpMergeSystem] ✨ Merged {toMerge.Count} Small EXP → 1 Large EXP at {centerPosition}");
        }
    }
    
    /// <summary>
    /// Vẽ Gizmos để debug vùng merge
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showGizmos || !Application.isPlaying)
            return;
        
        // Vẽ merge radius xung quanh mỗi Small EXP
        Gizmos.color = Color.cyan;
        
        ExpItem[] allExpItems = FindObjectsByType<ExpItem>(FindObjectsSortMode.None);
        foreach (var expItem in allExpItems)
        {
            if (expItem != null && expItem.GetExpType() == ExpType.Small)
            {
                Gizmos.DrawWireSphere(expItem.transform.position, mergeRadius);
            }
        }
    }
    
    /// <summary>
    /// Force merge tất cả Small EXP gần nhau (dùng cho testing)
    /// </summary>
    public void ForceMergeAll()
    {
        ScanAndMergeExp();
    }
}

