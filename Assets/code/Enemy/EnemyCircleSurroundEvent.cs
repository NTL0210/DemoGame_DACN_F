using System.Collections;

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Event Loại 2: Enemy bao vây player theo hình tròn
/// 
/// Cơ chế:
/// - Enemy spawn theo vòng tròn xung quanh player (radius ~4-6 units)
/// - Enemy di chuyển để hình thành vòng tròn (ring)
/// - Khi vào đúng vòng tròn, chúng đứng yên 0.3-0.6s rồi bắt đầu tấn công
/// - Enemy không bị ảnh hưởng bởi AI di chuyển bình thường
/// - Tồn tại 25s, sau đó tự động destroy
/// - Máu enemy x2.5 so với event loại 1
/// </summary>
public class EnemyCircleSurroundEvent : MonoBehaviour
{
    [Header("Circle Formation Settings")]
    [SerializeField] private float circleRadius = 5f; // Bán kính vòng tròn bao vây (sẽ tự tính theo Camera nếu followCamera)
    [SerializeField] private float cameraMargin = 1.5f; // Khoảng cách vượt ra ngoài viền camera
    [SerializeField] private float desiredArcSpacing = 0.9f; // Khoảng cách giữa 2 enemy trên cung tròn để không hở lỗ
    [SerializeField] private int safetyMaxEnemies = 180; // Nắp an toàn tránh spam quá nhiều
    [SerializeField] private float spawnDistance = 20f; // Khoảng cách spawn ban đầu (xa hơn circle radius)
    [SerializeField, Range(0.5f, 1f)] private float completionRatioToStartDuration = 1f; // 1f = đợi đủ vòng, 0.9f = 90% đủ vòng

    [Header("Enemy Behavior")]
    [SerializeField] private float circleMovementSpeed = 2.5f; // Tốc độ di chuyển vào vòng tròn
    [SerializeField] private float healthMultiplier = 4f; // Máu enemy x4 (so với bình thường)

    [Header("Event Duration")]
    [SerializeField] private float eventDuration = 25f; // Thời gian tồn tại event (sẽ được override từ Spawner)

    [Header("References")]
    [SerializeField] private SpawnEnemy spawner;
    [SerializeField] private Transform player;
    [SerializeField] private TimerManager timerManager;

    [Header("Options")]
    [SerializeField] private bool followCamera = false; // Nếu bật, vòng bám theo camera; mặc định tắt theo yêu cầu

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    private readonly List<GameObject> currentEventEnemies = new List<GameObject>();
    private readonly List<Collider2D> ringColliders = new List<Collider2D>();
    private bool isEventActive = false;
    private Coroutine eventCoroutine;
    private Coroutine collisionRoutine;
    private Coroutine durationCoroutine;
    private EventEnemySpawner eventSpawner; // Reference để gọi RemoveEventEnemy
    private Vector2 formationCenter; // Tâm vòng tròn (camera) được cập nhật theo thời gian
    private float formationRadius;   // Bán kính vòng hiện tại
    private int formationCount;      // Số enemy trên vòng

    // Progress tracking for ring completion
    private int reachedCount = 0;
    private bool durationStarted = false;

    private void Awake()
    {
        if (spawner == null)
            spawner = FindFirstObjectByType<SpawnEnemy>();

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        if (timerManager == null)
            timerManager = FindFirstObjectByType<TimerManager>();
    }

    /// <summary>
    /// Khởi động event bao vây hình tròn
    /// </summary>
    public void StartCircleEvent(EventEnemySpawner spawnerRef)
    {
        StartCircleEvent(spawnerRef, eventDuration, completionRatioToStartDuration);
    }

    /// <summary>
    /// Overload cho phép Spawner truyền thời lượng và tỉ lệ hoàn thành vòng để bắt đầu đếm
    /// </summary>
    public void StartCircleEvent(EventEnemySpawner spawnerRef, float durationSeconds, float completionRatio)
    {
        if (isEventActive) return;

        if (showDebugInfo) Debug.Log("[EnemyCircleSurroundEvent] Event Loại 2 bắt đầu!");

        eventSpawner = spawnerRef;
        eventDuration = durationSeconds;
        completionRatioToStartDuration = Mathf.Clamp01(completionRatio);

        isEventActive = true;
        durationStarted = false;
        reachedCount = 0;
        currentEventEnemies.Clear();
        ringColliders.Clear();

        // Tính tâm và bán kính dựa theo Main Camera và tạo vòng tròn ban đầu
        RecomputeCameraCircle(out formationCenter, out formationRadius, out formationCount);
        SpawnCircleRing();

        // Duy trì bỏ qua va chạm giữa quái thường và ring
        if (collisionRoutine != null) StopCoroutine(collisionRoutine);
        collisionRoutine = StartCoroutine(MaintainCollisionIgnores());
        
        // KHÔNG đếm duration ngay tại đây; chỉ khi vòng đã hoàn/đạt tỉ lệ
        if (eventCoroutine != null) { StopCoroutine(eventCoroutine); eventCoroutine = null; }
        if (durationCoroutine != null) { StopCoroutine(durationCoroutine); durationCoroutine = null; }
    }

    /// <summary>
    /// Vòng đời event: chỉ chờ đủ thời gian rồi kết thúc
    /// </summary>
    private IEnumerator CircleEventLifetime()
    {
        yield return new WaitForSeconds(eventDuration);
        EndCircleEvent();
    }

    // Bắt đầu đếm duration khi vòng đạt tỉ lệ hoàn thành
    private IEnumerator DurationCountdown()
    {
        if (showDebugInfo) Debug.Log($"[EnemyCircleSurroundEvent] Bắt đầu đếm duration: {eventDuration}s");
        yield return new WaitForSeconds(eventDuration);
        if (eventSpawner != null)
        {
            eventSpawner.HandleCircleEventTimeUpFromChild();
        }
        else
        {
            EndCircleEvent();
        }
    }

    // Được gọi từ từng enemy khi nó đã vào vị trí trên vòng
    public void NotifyMemberReachedCircle()
    {
        if (!isEventActive) return;
        reachedCount++;
        float ratio = formationCount > 0 ? (float)reachedCount / formationCount : 0f;
        if (!durationStarted && ratio >= completionRatioToStartDuration)
        {
            durationStarted = true;
            if (durationCoroutine != null) StopCoroutine(durationCoroutine);
            durationCoroutine = StartCoroutine(DurationCountdown());
        }
    }

    /// <summary>
    /// Tính tâm và bán kính bao trọn màn hình Camera.main (orthographic)
    /// - Tâm = vị trí camera
    /// - Bán kính = nửa đường chéo khung hình + margin → đảm bảo không hở ở 4 góc
    /// - Số lượng = chu vi / desiredArcSpacing, giới hạn bởi safetyMaxEnemies
    /// </summary>
    private void RecomputeCameraCircle(out Vector2 center, out float radius, out int count)
    {
        Camera cam = Camera.main;
        if (cam != null && cam.orthographic)
        {
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;
            radius = Mathf.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight) + cameraMargin;
            center = (Vector2)cam.transform.position;
        }
        else
        {
            center = player != null ? (Vector2)player.position : Vector2.zero;
            radius = circleRadius; // fallback
        }

        float circumference = 2f * Mathf.PI * Mathf.Max(0.1f, radius);
        count = Mathf.Clamp(Mathf.CeilToInt(circumference / Mathf.Max(0.1f, desiredArcSpacing)), 8, safetyMaxEnemies);
    }

    /// <summary>
    /// Tính lại tâm và bán kính theo camera nhưng KHÔNG đổi số lượng đã spawn
    /// </summary>
    private void RecomputeCameraCenterAndRadius(out Vector2 center, out float radius)
    {
        Camera cam = Camera.main;
        if (cam != null && cam.orthographic)
        {
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;
            radius = Mathf.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight) + cameraMargin;
            center = (Vector2)cam.transform.position;
        }
        else
        {
            center = player != null ? (Vector2)player.position : Vector2.zero;
            radius = circleRadius;
        }
    }

    /// <summary>
    /// Tạo một vòng tròn duy nhất gồm tối đa maxCircleEnemies kẻ địch
    /// </summary>
    private void SpawnCircleRing()
    {
        if (spawner == null) return;

        float step = 360f / Mathf.Max(1, formationCount);

        for (int i = 0; i < formationCount; i++)
        {
            float angle = i * step;
            float rad = angle * Mathf.Deg2Rad;

            Vector2 spawnPos = formationCenter + new Vector2(
                Mathf.Cos(rad) * (formationRadius + spawnDistance),
                Mathf.Sin(rad) * (formationRadius + spawnDistance)
            );

            Vector2 targetPos = formationCenter + new Vector2(
                Mathf.Cos(rad) * formationRadius,
                Mathf.Sin(rad) * formationRadius
            );

            EnemyType enemyType = Random.value > 0.5f ? EnemyType.TypeA : EnemyType.TypeB;
            GameObject enemy = spawner.Spawn(enemyType, spawnPos, Quaternion.identity);
            if (enemy == null) continue;

            SetupCircleEnemy(enemy, targetPos, rad);
            currentEventEnemies.Add(enemy);

            // Lưu collider để quản lý collision
            Collider2D col = enemy.GetComponent<Collider2D>();
            if (col != null) ringColliders.Add(col);
        }
    }

    /// <summary>
    /// Liên tục bỏ qua va chạm giữa quái thường và ring (nhưng vẫn va chạm với Player)
    /// </summary>
    private IEnumerator MaintainCollisionIgnores()
    {
        var wait = new WaitForSeconds(1f);
        while (isEventActive)
        {
            // Tất cả enemy thường trong scene
            GameObject[] allEnemies = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (var go in allEnemies)
            {
                if (go == null) continue;
                if (go.GetComponent<CircleEnemyBehavior>() != null) continue; // Bỏ qua ring members

                Collider2D otherCol = go.GetComponent<Collider2D>();
                if (otherCol == null) continue;

                foreach (var ringCol in ringColliders)
                {
                    if (ringCol == null) continue;
                    Physics2D.IgnoreCollision(ringCol, otherCol, true);
                }
            }
            yield return wait;
        }
    }

    /// <summary>
    /// Thiết lập enemy cho event bao vây hình tròn
    /// </summary>
    private void SetupCircleEnemy(GameObject enemy, Vector2 targetPos, float angleRad)
    {
        // Tăng máu
        EnemyController controller = enemy.GetComponent<EnemyController>();
        if (controller != null)
        {
            controller.ApplyHealthMultiplier(healthMultiplier);
        }

        // Vô hiệu hóa AI di chuyển bình thường trong giai đoạn bao vây
        EnemyMove normalMove = enemy.GetComponent<EnemyMove>();
        if (normalMove != null)
        {
            normalMove.SetSpeedOverride(circleMovementSpeed);
            normalMove.DisableEventMode(); // Tắt event mode để không bị ảnh hưởng
            normalMove.enabled = false;    // Tạm tắt AI di chuyển thường
        }

        // Vô hiệu hóa collision avoidance
        EnemyCollisionAvoidance avoidance = enemy.GetComponent<EnemyCollisionAvoidance>();
        if (avoidance != null) avoidance.enabled = false;

        // Thêm component để quản lý hành vi bao vây
        CircleEnemyBehavior circleBehavior = enemy.AddComponent<CircleEnemyBehavior>();
        circleBehavior.Initialize(targetPos, angleRad, circleMovementSpeed, this);

        // Thêm component EventEnemy để quản lý vòng đời
        EventEnemy eventEnemy = enemy.AddComponent<EventEnemy>();
        eventEnemy.Initialize(Vector2.zero, eventSpawner); // Direction không dùng cho circle event
    }


    /// <summary>
    /// Kết thúc event
    /// </summary>
    public void EndCircleEvent()
    {
        if (showDebugInfo) Debug.Log($"[EnemyCircleSurroundEvent] Event Loại 2 kết thúc! Xử lý {currentEventEnemies.Count} enemy.");

        // Dừng routine collision
        if (collisionRoutine != null)
        {
            StopCoroutine(collisionRoutine);
            collisionRoutine = null;
        }

        if (durationCoroutine != null)
        {
            StopCoroutine(durationCoroutine);
            durationCoroutine = null;
        }

        isEventActive = false;

        foreach (GameObject enemy in currentEventEnemies)
        {
            if (enemy == null) continue;

            EventEnemy eventComp = enemy.GetComponent<EventEnemy>();
            if (eventComp != null)
            {
                eventComp.DestroyWithoutDrop();
            }
        }

        currentEventEnemies.Clear();
        ringColliders.Clear();

        // Thông báo cho Spawner để nó set activeCircleEvent = null và tiếp tục flow
        if (eventSpawner != null)
        {
            eventSpawner.NotifyCircleEventEnded(this);
        }

        // Hủy chính GameObject event để dọn rác
        Destroy(gameObject);
    }

    /// <summary>
    /// Gọi từ CircleEnemyBehavior khi enemy bị destroy
    /// </summary>
    public void RemoveCircleEnemy(GameObject enemy)
    {
        currentEventEnemies.Remove(enemy);
        if (eventSpawner != null)
        {
            eventSpawner.RemoveEventEnemy(enemy);
        }
    }

    public bool IsEventActive()
    {
        return isEventActive;
    }

    private void Update()
    {
        if (!isEventActive || !followCamera) return;
        // Theo dõi camera và cập nhật tâm/bán kính; số lượng giữ nguyên
        RecomputeCameraCenterAndRadius(out formationCenter, out formationRadius);
        for (int i = 0; i < currentEventEnemies.Count; i++)
        {
            var e = currentEventEnemies[i];
            if (e == null) continue;
            var beh = e.GetComponent<CircleEnemyBehavior>();
            if (beh != null)
            {
                beh.UpdateLockedTarget(formationCenter, formationRadius);
            }
        }
    }

    private void OnDestroy()
    {
        if (eventCoroutine != null)
        {
            StopCoroutine(eventCoroutine);
        }
        if (collisionRoutine != null)
        {
            StopCoroutine(collisionRoutine);
        }
    }
}

