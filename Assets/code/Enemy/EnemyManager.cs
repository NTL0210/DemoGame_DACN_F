using System.Collections;
using UnityEngine;

/// <summary>
/// Quản lý nhịp spawn, tỉ lệ, giới hạn số lượng, và hướng spawn chặn đầu.
/// Gắn script này vào GameObject "Enemy" (cha) cùng với "SpawnEnemy".
/// </summary>
public class EnemyManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SpawnEnemy spawner;
    [SerializeField] private Transform player;
    [SerializeField] private Camera mainCamera;

    [Header("Spawn Pace")]
    [SerializeField] private float startInterval = 2.0f;
    [SerializeField] private float intervalDecrease = 0.2f;
    [SerializeField] private float decreaseEverySeconds = 120f;
    [SerializeField] private float minInterval = 0.5f;
    [SerializeField] private int maxAlive = 80;

    [Header("Head-Off Spawn")]
    [SerializeField] private float requiredStraightMoveSeconds = 2f;
    [SerializeField] private float headOffAngleOffsetDeg = 10f;
    [SerializeField] private float spawnDistanceFromPlayer = 12f; // cách player một khoảng xa ngoài camera

    [Header("Per-wave Count (Random spawn)")]
    [Tooltip("Số quái spawn mỗi lần 0-2 phút đầu")] 
    [SerializeField] private Vector2Int randomSpawnCountFirst2Min = new Vector2Int(1, 3);
    [Tooltip("Số quái spawn mỗi lần trong khoảng 2-3 phút")] 
    [SerializeField] private Vector2Int randomSpawnCount2To3Min = new Vector2Int(2, 3);
    [Tooltip("Số quái spawn mỗi lần từ phút thứ 3 trở đi")] 
    [SerializeField] private Vector2Int randomSpawnCountAfter3Min = new Vector2Int(3, 4);

    private float _currentInterval;
    private float _startTime;
    private int _aliveCount;
    private Vector2 _lastPlayerDir;
    private float _dirStableTime;

    private void Awake()
    {
        if (spawner == null) spawner = GetComponentInChildren<SpawnEnemy>();
        if (mainCamera == null) mainCamera = Camera.main;
        _currentInterval = startInterval;
        _startTime = Time.time;
    }

    private void OnEnable()
    {
        StartCoroutine(SpawnLoop());
        StartCoroutine(IntervalRoutine());
    }

    private void Update()
    {
        UpdatePlayerDirectionState();
    }

    private void UpdatePlayerDirectionState()
    {
        if (player == null)
        {
            var pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.transform;
            return;
        }

        Vector2 v = player.GetComponent<Rigidbody2D>() != null ? player.GetComponent<Rigidbody2D>().linearVelocity : Vector2.zero;
        Vector2 dir = v.sqrMagnitude > 0.01f ? v.normalized : Vector2.zero;

        if (dir == Vector2.zero)
        {
            _dirStableTime = 0f;
            _lastPlayerDir = Vector2.zero;
            return;
        }

        if (_lastPlayerDir == Vector2.zero)
        {
            _lastPlayerDir = dir;
            _dirStableTime = 0f;
        }
        else
        {
            float dot = Vector2.Dot(_lastPlayerDir, dir);
            if (dot > 0.98f) // gần như cùng hướng
            {
                _dirStableTime += Time.deltaTime;
            }
            else
            {
                _lastPlayerDir = dir;
                _dirStableTime = 0f;
            }
        }
    }

    private IEnumerator IntervalRoutine()
    {
        while (enabled)
        {
            yield return new WaitForSeconds(decreaseEverySeconds);
            _currentInterval = Mathf.Max(minInterval, _currentInterval - intervalDecrease);
        }
    }

    private IEnumerator SpawnLoop()
    {
        float lastSpawnTime = Time.time;
        while (enabled)
        {
            if (_aliveCount < maxAlive && Time.time - lastSpawnTime >= _currentInterval)
            {
                DoSpawnWave();
                lastSpawnTime = Time.time;
            }
            yield return null;
        }
    }

    private void DoSpawnWave()
    {
        // Quyết định kiểu spawn: chặn đầu hay ngẫu nhiên ngoài camera
        bool doHeadOff = _dirStableTime >= requiredStraightMoveSeconds && _lastPlayerDir != Vector2.zero;

        if (doHeadOff)
        {
            int count = GetHeadOffCountByTime();
            for (int i = 0; i < count && _aliveCount < maxAlive; i++)
            {
                float sign = (i % 2 == 0) ? 1f : -1f; // lệch trái/phải
                float angle = headOffAngleOffsetDeg * sign;
                Vector2 dir = Rotate(_lastPlayerDir, angle);
                Vector2 pos = (Vector2)player.position + dir.normalized * spawnDistanceFromPlayer;
                SpawnOneAt(pos);
            }
        }
        else
        {
            // Spawn nhiều con tại vị trí ngẫu nhiên ngoài tầm camera theo mốc thời gian
            int remaining = Mathf.Max(0, maxAlive - _aliveCount);
            if (remaining <= 0) return;

            int count = GetRandomSpawnCountByTime();
            count = Mathf.Min(count, remaining);

            for (int i = 0; i < count; i++)
            {
                Vector2 pos = GetRandomPositionOutsideCamera();
                SpawnOneAt(pos);
                if (_aliveCount >= maxAlive) break;
            }
        }
    }

    private void SpawnOneAt(Vector2 desired)
    {
        if (spawner == null) return;
        Vector2 pos = spawner.FindNearestFreePosition(desired);
        EnemyType type = RollTypeByElapsed(Time.time - _startTime);
        var go = spawner.Spawn(type, pos, Quaternion.identity);
        if (go != null)
        {
            _aliveCount++;
            var deathHook = go.AddComponent<EnemyOnDeathHook>();
            deathHook.Init(this);
        }
    }

    public void OnEnemyDeath()
    {
        _aliveCount = Mathf.Max(0, _aliveCount - 1);
    }

    private int GetHeadOffCountByTime()
    {
        float elapsed = Time.time - _startTime;
        if (elapsed < 300f) return 5;       // 0-5p
        if (elapsed < 600f) return 7;       // 5-10p
        return 8;                            // 10-15p+
    }

    // Số lượng spawn cho nhánh ngẫu nhiên ngoài camera
    private int GetRandomSpawnCountByTime()
    {
        float elapsed = Time.time - _startTime;
        if (elapsed < 120f)
        {
            // 0 - 2 phút
            return Random.Range(randomSpawnCountFirst2Min.x, randomSpawnCountFirst2Min.y + 1);
        }
        else if (elapsed < 180f)
        {
            // 2 - 3 phút
            return Random.Range(randomSpawnCount2To3Min.x, randomSpawnCount2To3Min.y + 1);
        }
        else
        {
            // 3 phút trở đi
            return Random.Range(randomSpawnCountAfter3Min.x, randomSpawnCountAfter3Min.y + 1);
        }
    }

    private EnemyType RollTypeByElapsed(float elapsed)
    {
        float minutes = elapsed / 60f;
        float r = Random.value;
        if (minutes < 5f)
        {
            return r < 0.8f ? EnemyType.TypeA : EnemyType.TypeB;
        }
        else if (minutes < 10f)
        {
            return r < 0.6f ? EnemyType.TypeA : EnemyType.TypeB;
        }
        else
        {
            return r < 0.4f ? EnemyType.TypeA : EnemyType.TypeB;
        }
    }

    private Vector2 GetRandomPositionOutsideCamera()
    {
        if (mainCamera == null)
        {
            return (Vector2)player.position + Random.insideUnitCircle.normalized * spawnDistanceFromPlayer;
        }

        // Lấy biên camera (orthographic) và spawn ngoài biên
        float z = Mathf.Abs(mainCamera.transform.position.z - player.position.z);
        Vector3 screenBL = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, z));
        Vector3 screenTR = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, z));
        float width = screenTR.x - screenBL.x;
        float height = screenTR.y - screenBL.y;

        // Chọn một cạnh ngẫu nhiên và đẩy ra ngoài
        int edge = Random.Range(0, 4);
        Vector2 pos = player.position;
        switch (edge)
        {
            case 0: pos += Vector2.up * (height * 0.6f + 2f); break;    // top
            case 1: pos += Vector2.down * (height * 0.6f + 2f); break;  // bottom
            case 2: pos += Vector2.left * (width * 0.6f + 2f); break;   // left
            default: pos += Vector2.right * (width * 0.6f + 2f); break; // right
        }
        return pos;
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(cos * v.x - sin * v.y, sin * v.x + cos * v.y);
    }
}

/// <summary>
/// Hook đơn giản để báo về Manager khi enemy chết (có thể gắn vào sự kiện chết sẵn có)
/// </summary>
public class EnemyOnDeathHook : MonoBehaviour
{
    private EnemyManager _manager;
    public void Init(EnemyManager manager) { _manager = manager; }

    private void OnDestroy()
    {
        if (_manager != null && Application.isPlaying)
        {
            _manager.OnEnemyDeath();
        }
    }
}


