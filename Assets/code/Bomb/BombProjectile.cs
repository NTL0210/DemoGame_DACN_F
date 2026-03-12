using UnityEngine;
using System.Collections;

/// <summary>
/// Quả bom được ném ra: nảy 2 lần theo cung parabol trên trục Y, sau đó "arm" (kích hoạt) sau 0.3s.
/// - Trước khi arm: enemy chạm vào không nổ
/// - Sau khi arm: chạm enemy sẽ nổ ngay (AOE)
/// - Hết thời gian tồn tại -> tự nổ
/// - Nổ: gây damage AOE cho tất cả enemy trong bán kính ngắn, spawn hiệu ứng và destroy
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class BombProjectile : MonoBehaviour
{
    public struct Config
    {
        public Vector2 direction; // hướng ném (đã normalize)
        public float distance1;   // quãng đường arc 1
        public float distance2;   // quãng đường arc 2
        public float arcHeight1;  // độ cao vòng cung 1
        public float arcHeight2;  // độ cao vòng cung 2
        public float bounceTime1; // thời gian di chuyển arc 1
        public float bounceTime2; // thời gian di chuyển arc 2
        public float armDelay;    // chờ trước khi kích hoạt collider
        public float lifeTime;    // tổng thời gian tồn tại (tính từ spawn)
        public float damagePercent; // % damage theo level (0.8 ~ 1.4)
        public float explosionRadius; // bán kính nổ ngắn
        public GameObject effectPrefab; // Bomb Effect prefab
        public Transform effectParent; // parent cho effect (vd: Exp Spawn)
    }

    private Config cfg;

    private CircleCollider2D col;
    private bool armed = false;
    private bool exploded = false;

    private void Awake()
    {
        col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.enabled = false; // chỉ bật khi arm
    }

    public void Configure(Config config)
    {
        cfg = config;
        // Bảo đảm direction chuẩn hóa
        if (cfg.direction == Vector2.zero) cfg.direction = Vector2.right;
        cfg.direction.Normalize();

        // Bắt đầu các routine
        StartCoroutine(BounceAndArm());
        StartCoroutine(LifeTimer());
    }

    private IEnumerator BounceAndArm()
    {
        Vector3 start = transform.position;
        Vector3 p1End = start + (Vector3)(cfg.direction * cfg.distance1);
        Vector3 p2End = p1End + (Vector3)(cfg.direction * cfg.distance2);

        // Bezier control points (nhấc theo trục Y)
        Vector3 p1Ctrl = Vector3.Lerp(start, p1End, 0.5f) + Vector3.up * cfg.arcHeight1;
        Vector3 p2Ctrl = Vector3.Lerp(p1End, p2End, 0.5f) + Vector3.up * cfg.arcHeight2;

        // Arc 1
        yield return StartCoroutine(BezierMove(start, p1Ctrl, p1End, cfg.bounceTime1));
        // Arc 2
        yield return StartCoroutine(BezierMove(p1End, p2Ctrl, p2End, cfg.bounceTime2));

        // Đợi armDelay rồi cho phép kích nổ khi va chạm
        yield return new WaitForSeconds(cfg.armDelay);
        armed = true;
        col.enabled = true;
    }

    private IEnumerator LifeTimer()
    {
        float t = 0f;
        while (t < cfg.lifeTime)
        {
            t += Time.deltaTime;
            yield return null;
        }
        // Hết thời gian mà chưa nổ -> nổ AOE
        Explode();
    }

    private IEnumerator BezierMove(Vector3 p0, Vector3 p1, Vector3 p2, float duration)
    {
        if (duration <= 0f)
        {
            transform.position = p2;
            yield break;
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float u = Mathf.Clamp01(t);
            // Quadratic Bezier: B(u) = (1-u)^2 p0 + 2(1-u)u p1 + u^2 p2
            Vector3 pos = (1 - u) * (1 - u) * p0 + 2 * (1 - u) * u * p1 + u * u * p2;
            transform.position = pos;
            yield return null;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!armed || exploded) return;
        // Chỉ quan tâm enemy
        if (other.GetComponent<EnemyController>() != null)
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (exploded) return;
        exploded = true;
        armed = false;
        col.enabled = false;

        // Gây damage AOE
        float baseDmg = PlayerDamage.Instance != null ? PlayerDamage.Instance.CurrentDamage : 0f;
        float dmg = baseDmg * Mathf.Max(0f, cfg.damagePercent);

        if (cfg.explosionRadius > 0f)
        {
            var hits = Physics2D.OverlapCircleAll(transform.position, cfg.explosionRadius);
            foreach (var h in hits)
            {
                var enemy = h.GetComponent<EnemyController>();
                if (enemy != null && enemy.IsAlive)
                {
                    enemy.TakeDamage(dmg);
                }
            }
        }

        // Spawn hiệu ứng
        if (cfg.effectPrefab != null)
        {
            Transform parent = cfg.effectParent != null ? cfg.effectParent : (ExpSpawnManager.Instance != null ? ExpSpawnManager.Instance.transform : null);
            var fx = GameObject.Instantiate(cfg.effectPrefab, transform.position, Quaternion.identity, parent);
            if (fx != null && !fx.activeSelf) fx.SetActive(true);
            GameObject.Destroy(fx, 2f);
        }

        Destroy(gameObject);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (cfg.explosionRadius > 0f)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, cfg.explosionRadius);
        }
    }
#endif
}

