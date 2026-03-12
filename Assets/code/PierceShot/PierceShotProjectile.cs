using UnityEngine;

/// <summary>
/// Pierce Shot Projectile
/// - Cơ chế homing theo level: Instant 1-lần, Curve (liên tục cho đến khi chạm), hoặc không homing.
/// - Sau va chạm đầu tiên: luôn giữ hướng hiện tại và xuyên tiếp.
/// - Hủy khi đi ra khỏi khung hình camera chính kèm một khoảng padding (đơn vị thế giới).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PierceShotProjectile : MonoBehaviour
{
    public enum HomingMode { None, OneTimeInstant, ContinuousCurve }

    public struct Config
    {
        public Vector2 initialDirection;
        public float speed;
        public float damage;
        public Camera mainCamera;
        public float destroyPadding; // khoảng cách vượt khỏi biên camera trước khi hủy (world units)

        public HomingMode homingMode; // hành vi homing
        public float homingTurnSpeedDeg; // dùng cho ContinuousCurve
        public bool homingUseLerp;      // nếu true: dùng Quaternion.Lerp để tạo đường cong mềm
        public float allowedConeDeg; // <0: không giới hạn; >=0: chỉ chọn target trong ±cone quanh initialDirection
        public Transform targetOverride; // nếu set, sử dụng target này
    }

    private Config cfg;
    private Vector2 currentDir;
    private Collider2D col;
    private Rigidbody2D rb;

    private EnemyController target;
    private float homingTurnSpeedRad;
    private bool firstHit;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    public void Configure(Config config)
    {
        cfg = config;
        if (cfg.initialDirection == Vector2.zero) cfg.initialDirection = Vector2.right;
        currentDir = cfg.initialDirection.normalized;
        homingTurnSpeedRad = Mathf.Deg2Rad * Mathf.Max(0f, cfg.homingTurnSpeedDeg);

        // Acquire target theo yêu cầu (half-plane + cone ±allowedConeDeg)
        target = GetInitialTarget();
        if (cfg.homingMode == HomingMode.OneTimeInstant && target != null)
        {
            currentDir = ((Vector2)(target.transform.position - transform.position)).normalized;
        }
    }

    private void Update()
    {
        // Homing nếu được phép và chưa va chạm
        if (!firstHit)
        {
            if (cfg.homingMode == HomingMode.ContinuousCurve && target != null && target.IsAlive)
            {
                Vector2 toTarget = (Vector2)(target.transform.position - transform.position).normalized;
                if (cfg.homingUseLerp)
                {
                    // Lerp góc để tạo đường cong mềm
                    float curA = Mathf.Atan2(currentDir.y, currentDir.x) * Mathf.Rad2Deg;
                    float tgtA = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
                    Quaternion qFrom = Quaternion.Euler(0f, 0f, curA);
                    Quaternion qTo = Quaternion.Euler(0f, 0f, tgtA);
                    float t = Mathf.Clamp01(cfg.homingTurnSpeedDeg * Time.deltaTime / 180f);
                    Quaternion q = Quaternion.Lerp(qFrom, qTo, t);
                    float newA = q.eulerAngles.z * Mathf.Deg2Rad;
                    currentDir = new Vector2(Mathf.Cos(newA), Mathf.Sin(newA)).normalized;
                }
                else
                {
                    // Quay theo tốc độ tối đa mỗi frame
                    Vector3 newDir3 = Vector3.RotateTowards(currentDir, toTarget, homingTurnSpeedRad * Time.deltaTime, 0f);
                    currentDir = new Vector2(newDir3.x, newDir3.y).normalized;
                }
            }
        }

        // Di chuyển
        transform.position += (Vector3)(currentDir * cfg.speed * Time.deltaTime);

        // Xoay sprite theo hướng
        float angle = Mathf.Atan2(currentDir.y, currentDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // Hủy khi vượt ngoài biên camera + padding (đơn vị thế giới)
        if (cfg.mainCamera != null)
        {
            Vector3 camPos = cfg.mainCamera.transform.position;
            float halfH = cfg.mainCamera.orthographicSize;
            float halfW = halfH * cfg.mainCamera.aspect;
            float pad = Mathf.Max(0f, cfg.destroyPadding);

            float minX = camPos.x - halfW - pad;
            float maxX = camPos.x + halfW + pad;
            float minY = camPos.y - halfH - pad;
            float maxY = camPos.y + halfH + pad;

            Vector3 p = transform.position;
            if (p.x < minX || p.x > maxX || p.y < minY || p.y > maxY)
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var enemy = other.GetComponent<EnemyController>();
        if (enemy == null || !enemy.IsAlive) return;

        if (cfg.damage > 0f)
        {
            enemy.TakeDamage(cfg.damage);
        }

        // Sau va chạm đầu tiên: dừng homing, giữ hướng hiện tại
        firstHit = true;
        cfg.homingMode = HomingMode.None;
    }

    private EnemyController GetInitialTarget()
    {
        if (cfg.targetOverride != null)
        {
            var ec = cfg.targetOverride.GetComponent<EnemyController>();
            if (IsTargetValidByCone(ec)) return ec;
        }

        EnemyController[] all = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        float bestDist = float.MaxValue;
        EnemyController best = null;
        Vector3 myPos = transform.position;
        foreach (var e in all)
        {
            if (e == null || !e.IsAlive) continue;
            Vector2 toEnemy = (Vector2)(e.transform.position - myPos);
            // Half-plane check (cùng hướng spawn)
            if (Vector2.Dot(cfg.initialDirection, toEnemy) <= 0f) continue;
            // Cone check
            if (!IsWithinCone(toEnemy)) continue;

            float d = toEnemy.sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = e;
            }
        }
        return best;
    }

    private bool IsTargetValidByCone(EnemyController ec)
    {
        if (ec == null) return false;
        Vector2 toEnemy = (Vector2)(ec.transform.position - transform.position);
        if (Vector2.Dot(cfg.initialDirection, toEnemy) <= 0f) return false;
        return IsWithinCone(toEnemy);
    }

    private bool IsWithinCone(Vector2 toEnemy)
    {
        if (cfg.allowedConeDeg < 0f) return true;
        float angle = Vector2.Angle(cfg.initialDirection, toEnemy);
        return angle <= cfg.allowedConeDeg + 0.001f;
    }
}

