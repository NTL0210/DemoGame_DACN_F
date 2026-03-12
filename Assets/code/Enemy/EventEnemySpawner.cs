using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EventEnemySpawner : MonoBehaviour
{
    [Header("Mode Selection")]
    [SerializeField] private EventMode eventMode = EventMode.SpawnNew;
    [Tooltip("SpawnNew: Tạo enemy mới | ReuseExisting: Tái sử dụng enemy có sẵn")]
    public enum EventMode { SpawnNew, ReuseExisting }

    [Header("References")]
    [SerializeField] private SpawnEnemy spawner;
    [SerializeField] private Transform player;
    [SerializeField] private TimerManager timerManager;

    [Header("Event Timing")]
    [SerializeField] private float[] eventMinutes = { 2f, 5f, 8f, 11f, 13f };
    [Tooltip("Cách hiểu mốc trong Event Minutes: Elapsed = phút đã trôi qua | Remaining = phút còn lại trên đồng hồ")]
    [SerializeField] private TriggerTimeMode triggerTimeMode = TriggerTimeMode.ElapsedMinutes;
    public enum TriggerTimeMode { ElapsedMinutes, RemainingMinutes }
    [SerializeField] private float eventDuration = 25f;
    [SerializeField] private float warningDuration = 7f;
    [SerializeField] private float warningBlinkInterval = 0.3f;

    [Header("Warning GameObjects")]
    [Tooltip("Kéo các GameObject cha (ví dụ: Warning events 1) vào đây.")]
    [SerializeField] private GameObject warningLeft;
    [SerializeField] private GameObject warningRight;
    [SerializeField] private GameObject warningTop;
    [SerializeField] private GameObject warningBottom;

    [Header("Mode: Spawn New Settings")]
    [SerializeField] private float spawnInterval = 0.5f;
    [SerializeField] private int enemiesPerWave = 3;
    [SerializeField] private float spawnDistance = 15f;
    [SerializeField] private float enemySpacing = 1.5f;

    [Header("Mode: Reuse Existing Settings")]
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField] private int maxEnemiesToConvert = 20;
    [SerializeField] private float searchRadius = 30f;
    [SerializeField] private bool revertOnEnd = false;

    [Header("General Enemy Settings")]
    [SerializeField] private float healthMultiplier = 5f;
    [SerializeField] private float eventEnemySpeed = 3f;

    [Header("Circle Event Settings")]
    [SerializeField] private EnemyCircleSurroundEvent circleEventPrefab;
    [SerializeField, Range(0.5f, 1f)] private float circleCompletionRatioToStartDuration = 1f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    private HashSet<int> triggeredEvents = new HashSet<int>();
    private List<GameObject> currentEventEnemies = new List<GameObject>();
    private bool isEventActive = false;
    private SpawnDirection currentSpawnDirection;
    private EnemyType currentEventEnemyType;
    private EventType currentEventType;
    private EnemyCircleSurroundEvent activeCircleEvent;
    private Queue<int> pendingEvents = new Queue<int>();
    private Coroutine activeSpawnCoroutine;
    private Coroutine activeEventSequenceCoroutine;

    private enum SpawnDirection { LeftRight, TopBottom }
    private enum EventType { Type1_Alternating, Type2_CircleSurround }

    private void Awake()
    {
        if (spawner == null && eventMode == EventMode.SpawnNew)
            spawner = FindFirstObjectByType<SpawnEnemy>();

        if (timerManager == null)
            timerManager = FindFirstObjectByType<TimerManager>();

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }
    }

    private void Start()
    {
        if (timerManager != null)
        {
            timerManager.OnTimeUpdate += CheckEventTrigger;
        }
        else
        {
            Debug.LogError("[EventEnemySpawner] Không tìm thấy TimerManager!");
        }
        HideAllWarnings();
    }

    private void OnDestroy()
    {
        if (timerManager != null)
        {
            timerManager.OnTimeUpdate -= CheckEventTrigger;
        }
    }

    private void CheckEventTrigger(float currentSeconds)
    {
        float elapsedMinutes = currentSeconds / 60f;
        float remainingMinutes = timerManager != null ? timerManager.GetMaxTimeInMinutes() - elapsedMinutes : 0f;
        
        if (showDebugInfo)
            Debug.Log($"[EventEnemySpawner] CheckEventTrigger: elapsed={elapsedMinutes:F2}m, remaining={remainingMinutes:F2}m, mode={triggerTimeMode}, isEventActive={isEventActive}, pending={pendingEvents.Count}");
        
        for (int i = 0; i < eventMinutes.Length; i++)
        {
            bool shouldTrigger = triggerTimeMode == TriggerTimeMode.ElapsedMinutes
                ? (elapsedMinutes >= eventMinutes[i])
                : (remainingMinutes <= eventMinutes[i]);

            if (shouldTrigger && !triggeredEvents.Contains(i))
            {
                if (showDebugInfo)
                    Debug.Log($"[EventEnemySpawner] ✓ Trigger event index={i} at minute={eventMinutes[i]} (mode={triggerTimeMode})");
                
                triggeredEvents.Add(i);
                
                if (isEventActive)
                {
                    if (showDebugInfo)
                        Debug.Log($"[EventEnemySpawner] Event đang chạy, thêm event {i} vào queue");
                    pendingEvents.Enqueue(i);
                }
                else
                {
                    StartEvent();
                }
            }
        }
        
        if (!isEventActive && pendingEvents.Count > 0)
        {
            if (showDebugInfo)
                Debug.Log("[EventEnemySpawner] Kích hoạt 1 event từ queue ngay lập tức");
            pendingEvents.Dequeue();
            StartEvent();
        }
    }

    private void StartEvent()
    {
        if (showDebugInfo) Debug.Log("[EventEnemySpawner] Event bắt đầu!");
        isEventActive = true;
        currentEventEnemies.Clear();

        currentEventType = SpawnEventRandomSelector();

        if (currentEventType == EventType.Type1_Alternating)
        {
            currentSpawnDirection = Random.value > 0.5f ? SpawnDirection.LeftRight : SpawnDirection.TopBottom;
            currentEventEnemyType = Random.value > 0.5f ? EnemyType.TypeA : EnemyType.TypeB;
            activeEventSequenceCoroutine = StartCoroutine(ProcessEventSequence());
        }
        else
        {
            StartCoroutine(ProcessCircleEventSequence());
        }
    }

    private EventType SpawnEventRandomSelector()
    {
        float randomValue = Random.value;
        if (showDebugInfo)
        {
            Debug.Log($"[EventEnemySpawner] SpawnEventRandomSelector: {randomValue}");
        }

        if (randomValue > 0.5f)
        {
            if (showDebugInfo) Debug.Log("[EventEnemySpawner] Chọn Event Loại 1 (Alternating)");
            return EventType.Type1_Alternating;
        }
        else
        {
            if (showDebugInfo) Debug.Log("[EventEnemySpawner] Chọn Event Loại 2 (Circle Surround)");
            return EventType.Type2_CircleSurround;
        }
    }

    private IEnumerator ProcessEventSequence()
    {
        GameObject[] warningsToShow = GetWarningsForDirection(currentSpawnDirection);
        yield return StartCoroutine(BlinkWarnings(warningsToShow, warningDuration));

        if (eventMode == EventMode.SpawnNew)
        {
            activeSpawnCoroutine = StartCoroutine(SpawnEventEnemies());
        }
        else
        {
            ConvertNearbyEnemies();
        }

        yield return new WaitForSeconds(eventDuration);
        EndEvent();
    }

    private IEnumerator ProcessCircleEventSequence()
    {
        GameObject[] warningsToShow = new[] { warningLeft, warningRight, warningTop, warningBottom };
        yield return StartCoroutine(BlinkWarnings(warningsToShow, warningDuration));

        if (activeCircleEvent == null)
        {
            if (circleEventPrefab != null)
            {
                GameObject circleEventObj = Instantiate(circleEventPrefab.gameObject);
                activeCircleEvent = circleEventObj.GetComponent<EnemyCircleSurroundEvent>();
            }
            else
            {
                GameObject circleEventObj = new GameObject("CircleEvent");
                activeCircleEvent = circleEventObj.AddComponent<EnemyCircleSurroundEvent>();
            }
        }

        if (activeCircleEvent != null)
        {
            activeCircleEvent.StartCircleEvent(this, eventDuration, circleCompletionRatioToStartDuration);
        }

        yield return new WaitUntil(() => activeCircleEvent == null);
    }

    public void NotifyCircleEventEnded(EnemyCircleSurroundEvent ev)
    {
        if (showDebugInfo) Debug.Log("[EventEnemySpawner] Circle Event kết thúc (callback từ child)");

        if (activeCircleEvent == ev)
        {
            activeCircleEvent = null;
        }

        isEventActive = false;

        if (pendingEvents.Count > 0)
        {
            if (showDebugInfo)
                Debug.Log($"[EventEnemySpawner] Circle Event kết thúc, trigger event tiếp theo từ queue");
            StartEvent();
        }
        else
        {
            TryTriggerCatchUp();
        }
    }

    public void HandleCircleEventTimeUpFromChild()
    {
        if (activeCircleEvent != null)
        {
            activeCircleEvent.EndCircleEvent();
        }
    }

    #region Warning Logic
    private GameObject[] GetWarningsForDirection(SpawnDirection direction)
    {
        if (direction == SpawnDirection.LeftRight)
            return new[] { warningLeft, warningRight };
        else
            return new[] { warningTop, warningBottom };
    }

    private IEnumerator BlinkWarnings(GameObject[] warnings, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            foreach (var warning in warnings)
            {
                if (warning != null) warning.SetActive(!warning.activeSelf);
            }
            yield return new WaitForSeconds(warningBlinkInterval);
            elapsed += warningBlinkInterval;
        }
        foreach (var warning in warnings)
        {
            if (warning != null) warning.SetActive(false);
        }
    }

    private void HideAllWarnings()
    {
        if (warningLeft != null) warningLeft.SetActive(false);
        if (warningRight != null) warningRight.SetActive(false);
        if (warningTop != null) warningTop.SetActive(false);
        if (warningBottom != null) warningBottom.SetActive(false);
    }
    #endregion

    #region SpawnNew Mode Logic
    private IEnumerator SpawnEventEnemies()
    {
        float elapsed = 0f;
        while (elapsed < eventDuration && isEventActive)
        {
            SpawnWave();
            yield return new WaitForSeconds(spawnInterval);
            elapsed += spawnInterval;
        }
    }

    private void SpawnWave()
    {
        if (player == null || spawner == null) return;

        float staggerOffset = enemySpacing / 2f;

        if (currentSpawnDirection == SpawnDirection.LeftRight)
        {
            SpawnEnemyLine(Vector2.left, Vector2.right, enemiesPerWave, 0f);
            SpawnEnemyLine(Vector2.right, Vector2.left, enemiesPerWave, staggerOffset);
        }
        else
        {
            SpawnEnemyLine(Vector2.up, Vector2.down, enemiesPerWave, 0f);
            SpawnEnemyLine(Vector2.down, Vector2.up, enemiesPerWave, staggerOffset);
        }
    }

    private void SpawnEnemyLine(Vector2 spawnDirection, Vector2 moveDirection, int count, float staggerOffset)
    {
        Vector2 basePosition = (Vector2)player.position + spawnDirection * spawnDistance;
        Vector2 perpendicular = new Vector2(-spawnDirection.y, spawnDirection.x);
        
        List<GameObject> newEnemies = new List<GameObject>();
        
        for (int i = 0; i < count; i++)
        {
            float lineOffset = (i - (count - 1) / 2f) * enemySpacing;
            Vector2 spawnPos = basePosition + perpendicular * (lineOffset + staggerOffset);

            GameObject enemy = spawner.Spawn(currentEventEnemyType, spawnPos, Quaternion.identity);
            if (enemy != null)
            {
                SetupEventEnemy(enemy, moveDirection);
                newEnemies.Add(enemy);
            }
        }
        
        currentEventEnemies.AddRange(newEnemies);
    }

    private void SetupEventEnemy(GameObject enemy, Vector2 moveDirection)
    {
        EnemyController controller = enemy.GetComponent<EnemyController>();
        if (controller != null)
        {
            controller.ApplyHealthMultiplier(healthMultiplier);
        }

        EnemyMove normalMove = enemy.GetComponent<EnemyMove>();
        if (normalMove != null)
        {
            normalMove.SetSpeedOverride(eventEnemySpeed);
            normalMove.EnableEventMode(moveDirection);
        }

        EnemyCollisionAvoidance avoidance = enemy.GetComponent<EnemyCollisionAvoidance>();
        if (avoidance != null) avoidance.enabled = false;

        EventEnemy eventEnemy = enemy.AddComponent<EventEnemy>();
        eventEnemy.Initialize(moveDirection, this);
    }
    #endregion

    #region ReuseExisting Mode Logic
    private void ConvertNearbyEnemies()
    {
        if (player == null) return;

        List<GameObject> nearbyEnemies = GameObject.FindGameObjectsWithTag(enemyTag)
            .Where(e => Vector2.Distance(player.position, e.transform.position) <= searchRadius && 
                        (e.GetComponent<EventEnemyConverter>() == null || !e.GetComponent<EventEnemyConverter>().IsEventEnemy()))
            .OrderBy(e => Vector2.Distance(player.position, e.transform.position))
            .Take(maxEnemiesToConvert)
            .ToList();

        if (nearbyEnemies.Count == 0) return;

        List<GameObject> convertedEnemies = new List<GameObject>();
        
        int halfCount = nearbyEnemies.Count / 2;
        for (int i = 0; i < nearbyEnemies.Count; i++)
        {
            GameObject enemy = nearbyEnemies[i];
            Vector2 moveDirection = (currentSpawnDirection == SpawnDirection.LeftRight) 
                ? (i < halfCount ? Vector2.right : Vector2.left) 
                : (i < halfCount ? Vector2.down : Vector2.up);

            EventEnemyConverter converter = enemy.GetComponent<EventEnemyConverter>();
            if (converter == null) converter = enemy.AddComponent<EventEnemyConverter>();

            converter.ConvertToEventEnemy(moveDirection, this, healthMultiplier, eventEnemySpeed);
            convertedEnemies.Add(enemy);
        }
        
        currentEventEnemies.AddRange(convertedEnemies);

        if (showDebugInfo) Debug.Log($"[EventSpawner] Đã convert {convertedEnemies.Count} enemy.");
    }
    #endregion

    private void EndEvent()
    {
        if (showDebugInfo) Debug.Log($"[EventSpawner] Event Loại 1 kết thúc! Xử lý {currentEventEnemies.Count} enemy.");

        // 1. Dừng ngay lập tức các Coroutine đang chạy để không sinh thêm quái
        if (activeSpawnCoroutine != null)
        {
            StopCoroutine(activeSpawnCoroutine);
            activeSpawnCoroutine = null;
        }
        
        if (activeEventSequenceCoroutine != null)
        {
            StopCoroutine(activeEventSequenceCoroutine);
            activeEventSequenceCoroutine = null;
        }

        // 2. TẠO BẢN SAO DANH SÁCH (Fix lỗi Collection was modified)
        // Chúng ta copy danh sách ra một List tạm để duyệt, ngắt sự phụ thuộc vào List gốc
        List<GameObject> enemiesToProcess = new List<GameObject>(currentEventEnemies);

        // 3. Dọn dẹp List gốc ngay lập tức
        currentEventEnemies.Clear();
        
        // Đặt trạng thái về false trước khi xử lý tiếp
        isEventActive = false;

        // 4. Duyệt trên bản sao để xóa/revert enemy (An toàn tuyệt đối)
        foreach (GameObject enemy in enemiesToProcess)
        {
            if (enemy == null) continue;
            
            try
            {
                if (eventMode == EventMode.SpawnNew)
                {
                    EventEnemy eventComp = enemy.GetComponent<EventEnemy>();
                    if (eventComp != null) eventComp.DestroyWithoutDrop();
                    else Destroy(enemy); // Fallback an toàn
                }
                else
                {
                    EventEnemyConverter converter = enemy.GetComponent<EventEnemyConverter>();
                    if (converter != null && converter.IsEventEnemy())
                    {
                        if (revertOnEnd) converter.RevertToNormalEnemy();
                        else converter.DestroyAsEventEnemy();
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EventSpawner] Error processing enemy: {e.Message}");
            }
        }

        // 5. Kiểm tra và kích hoạt Event tiếp theo (nếu có)
        if (pendingEvents.Count > 0)
        {
            if (showDebugInfo)
                Debug.Log($"[EventSpawner] Event kết thúc, trigger event tiếp theo từ queue");
            StartEvent();
        }
        else
        {
            TryTriggerCatchUp();
        }
    }
    // private IEnumerator EndEventDelayed()
    // {
    //     // --- SỬA LỖI: CHUYỂN LOGIC SAO CHÉP LÊN ĐẦU ---
        
    //     // 1. Tạo bản sao danh sách và Clear danh sách gốc NGAY LẬP TỨC.
    //     // Khi gọi StartCoroutine, đoạn code này sẽ chạy đồng bộ ngay, khóa dữ liệu lại an toàn.
    //     GameObject[] enemiesToProcess = currentEventEnemies.ToArray();
    //     currentEventEnemies.Clear();
        
    //     // 2. Sau khi đã chốt dữ liệu an toàn, bạn mới cho phép đợi 1 frame (nếu muốn giữ tính chất delay)
    //     yield return null;
        
    //     // 3. Duyệt trên mảng bản sao (enemiesToProcess) thay vì list gốc
    //     foreach (GameObject enemy in enemiesToProcess)
    //     {
    //         if (enemy == null) continue;
            
    //         try
    //         {
    //             if (eventMode == EventMode.SpawnNew)
    //             {
    //                 EventEnemy eventComp = enemy.GetComponent<EventEnemy>();
    //                 if (eventComp != null) eventComp.DestroyWithoutDrop();
    //                 // Thêm fallback nếu không tìm thấy component để đảm bảo object luôn bị xóa
    //                 else Destroy(enemy); 
    //             }
    //             else
    //             {
    //                 EventEnemyConverter converter = enemy.GetComponent<EventEnemyConverter>();
    //                 if (converter != null && converter.IsEventEnemy())
    //                 {
    //                     if (revertOnEnd) converter.RevertToNormalEnemy();
    //                     else converter.DestroyAsEventEnemy();
    //                 }
    //             }
    //         }
    //         catch (System.Exception e)
    //         {
    //             Debug.LogError($"[EventSpawner] Error processing enemy: {e.Message}");
    //         }
    //     }
        
    //     // Reset trạng thái
    //     isEventActive = false;

    //     // Logic trigger event tiếp theo giữ nguyên
    //     if (pendingEvents.Count > 0)
    //     {
    //         if (showDebugInfo)
    //             Debug.Log($"[EventSpawner] Event kết thúc, trigger event tiếp theo từ queue");
    //         StartEvent();
    //     }
    //     else
    //     {
    //         TryTriggerCatchUp();
    //     }
    // }

    // ✅ QUAN TRỌNG: Method này không làm gì cả
    public void RemoveEventEnemy(GameObject enemy)
    {
        // Không làm gì - để EndEvent() tự cleanup
    }

    public bool IsEventActive()
    {
        return isEventActive;
    }

    private void TryTriggerCatchUp()
    {
        if (timerManager == null) return;
        if (isEventActive) return;

        float elapsedMinutes = timerManager.GetCurrentTimeInSeconds() / 60f;
        float remainingMinutes = timerManager.GetMaxTimeInMinutes() - elapsedMinutes;

        for (int i = 0; i < eventMinutes.Length; i++)
        {
            bool shouldTrigger = triggerTimeMode == TriggerTimeMode.ElapsedMinutes
                ? (elapsedMinutes >= eventMinutes[i])
                : (remainingMinutes <= eventMinutes[i]);

            if (shouldTrigger && !triggeredEvents.Contains(i))
            {
                if (showDebugInfo) Debug.Log($"[EventEnemySpawner] Catch-up trigger at minute {eventMinutes[i]} (mode={triggerTimeMode})");
                triggeredEvents.Add(i);
                pendingEvents.Enqueue(i);
                if (!isEventActive)
                {
                    pendingEvents.Dequeue();
                    StartEvent();
                }
                break;
            }
        }
    }
}