using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Quản lý skill Pierce Shot: spawn theo đợt, chọn hướng không trùng, thiết lập damage/cooldown theo level.
/// - Lv1: 1 tia, 100% dmg, CD 18s
/// - Lv2: 2 tia,  90% dmg, CD 16s
/// - Lv3: 3 tia,  80% dmg, CD 14s
/// - Lv4: 4 tia,  70% dmg, CD 12s
/// </summary>
public class PierceShotManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject projectilePrefab;

    [Header("Parent Overrides (Optional)")]
    [Tooltip("Nếu gán, projectile sẽ được spawn làm con của Transform này. Mặc định dùng ExpSpawnManager nếu có.")]
    [SerializeField] private Transform projectileParentOverride;

    [Header("Projectile Settings")]
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private float spawnOffset = 0.7f; // spawn lệch khỏi player theo hướng bắn
    [SerializeField] private float keepAwayRadius = 1.0f; // bán kính an toàn so với collider của player
    [SerializeField] private bool ignorePlayerCollision = true; // bỏ va chạm giữa đạn và player

    [Header("Homing Settings")]
    [SerializeField] private float homingTurnSpeedDeg = 720f; // tốc độ xoay khi dùng ContinuousCurve

    [Header("Lifetime/Bounds")]
    [Tooltip("Khoảng cách (đơn vị thế giới) vượt ra khỏi khung hình camera trước khi đạn bị hủy")] 
    [SerializeField] private float destroyPadding = 1.5f;

    [Header("Level Runtime")]
    [SerializeField] private int currentLevel = 0;

    // per level
    private readonly int[] shotCount =   { 0, 1, 2, 3, 4 };
    private readonly float[] dmgPercent = { 0f, 1.0f, 0.9f, 0.8f, 0.7f };
    private readonly float[] cooldown =   { 0f, 18f, 16f, 14f, 12f };

    private float nextSpawnTime = 0f;

    public int CurrentLevel => currentLevel;

    private Collider2D[] playerCols;

    private void Awake()
    {
        if (player == null) player = transform.root;
        // Cache collider của player để tính khoảng cách an toàn và bỏ va chạm
        playerCols = player != null ? player.GetComponentsInChildren<Collider2D>(true) : null;
    }

    private void Update()
    {
        if (currentLevel <= 0) return;
        if (Time.time >= nextSpawnTime)
        {
            Activate();
            nextSpawnTime = Time.time + cooldown[currentLevel];
        }
    }

    /// <summary>
    /// Nâng cấp level lên 1 nấc (tối đa 4)
    /// </summary>
    public void LevelUp()
    {
        if (currentLevel >= 4) { Debug.Log("[PierceShotManager] Max level"); return; }
        currentLevel++;
        if (currentLevel == 1)
        {
            nextSpawnTime = Time.time; // kích hoạt ngay lần đầu
        }
    }

    private void Activate()
    {
        if (projectilePrefab == null) { Debug.LogError("[PierceShotManager] Chưa gán projectilePrefab"); return; }

        int count = shotCount[currentLevel];
        float percent = dmgPercent[currentLevel];

        List<Vector2> dirs = new List<Vector2>();
        if (currentLevel >= 4)
        {
            // Lv4: 4 hướng cố định
            dirs.Add(Vector2.up);
            dirs.Add(Vector2.down);
            dirs.Add(Vector2.left);
            dirs.Add(Vector2.right);
        }
        else
        {
            // Chọn ngẫu nhiên, không trùng hướng
            List<Vector2> candidates = new List<Vector2> { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
            for (int i = 0; i < candidates.Count; i++)
            {
                int r = Random.Range(i, candidates.Count);
                (candidates[i], candidates[r]) = (candidates[r], candidates[i]);
            }
            for (int i = 0; i < count && i < candidates.Count; i++)
                dirs.Add(candidates[i]);
        }

        // Gán mục tiêu khác nhau (khi có thể)
        var usedTargets = new HashSet<EnemyController>();
        for (int i = 0; i < dirs.Count; i++)
        {
            float allowedCone = currentLevel >= 4 ? 45f : -1f; // Lv4: ±45°, các level khác: không giới hạn (chỉ half-plane)
            EnemyController target = FindTargetForDirection(dirs[i], allowedCone, usedTargets);
            if (target != null) usedTargets.Add(target);

            PierceShotProjectile.HomingMode mode;
            bool useLerp = false;
            switch (currentLevel)
            {
                case 1: mode = PierceShotProjectile.HomingMode.OneTimeInstant; break;
                case 2: mode = PierceShotProjectile.HomingMode.ContinuousCurve; useLerp = true; break;
                default: mode = PierceShotProjectile.HomingMode.ContinuousCurve; break; // Lv3 & Lv4
            }

            SpawnOne(dirs[i], percent, mode, useLerp, allowedCone, target != null ? target.transform : null);
        }
    }

    private EnemyController FindTargetForDirection(Vector2 dir, float allowedConeDeg, HashSet<EnemyController> exclude)
    {
        EnemyController[] all = Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        float best = float.MaxValue;
        EnemyController bestE = null;
        Vector3 origin = player != null ? player.position : Vector3.zero;
        foreach (var e in all)
        {
            if (e == null || !e.IsAlive) continue;
            if (exclude != null && exclude.Contains(e)) continue;
            Vector2 toEnemy = (Vector2)(e.transform.position - origin);
            if (Vector2.Dot(dir, toEnemy) <= 0f) continue; // half-plane
            if (allowedConeDeg >= 0f)
            {
                float ang = Vector2.Angle(dir, toEnemy);
                if (ang > allowedConeDeg + 0.001f) continue;
            }
            float d = toEnemy.sqrMagnitude;
            if (d < best) { best = d; bestE = e; }
        }
        return bestE;
    }

    private void SpawnOne(Vector2 baseDir, float percent, PierceShotProjectile.HomingMode homingMode, bool homingUseLerp, float allowedConeDeg, Transform targetOverride)
    {
        // Đảm bảo spawn cách player ít nhất keepAwayRadius
        float distance = spawnOffset + Mathf.Max(0f, keepAwayRadius);
        Vector3 spawnPos = player.position + (Vector3)(baseDir.normalized * distance);
        Transform parent = projectileParentOverride != null ? projectileParentOverride : (ExpSpawnManager.Instance != null ? ExpSpawnManager.Instance.transform : null);
        GameObject go = Instantiate(projectilePrefab, spawnPos, Quaternion.identity, parent);
        go.name = "Pierce Shot";
        // Tag và bật active cho mọi object mang tag "Pierce Shot" trong prefab (kể cả con đang inactive)
        try { go.tag = "Pierce Shot"; } catch { }
        if (!go.activeSelf) go.SetActive(true);
        var allTransforms = go.GetComponentsInChildren<Transform>(true);
        foreach (var t in allTransforms)
        {
            if (t != null && t.gameObject.CompareTag("Pierce Shot") && !t.gameObject.activeSelf)
            {
                t.gameObject.SetActive(true);
            }
        }

        if (ignorePlayerCollision && playerCols != null)
        {
            var projCols = go.GetComponentsInChildren<Collider2D>(true);
            foreach (var pCol in projCols)
            {
                if (pCol == null) continue;
                foreach (var pc in playerCols)
                {
                    if (pc == null) continue;
                    Physics2D.IgnoreCollision(pCol, pc, true);
                }
            }
        }

        var proj = go.GetComponent<PierceShotProjectile>();
        if (proj == null) proj = go.AddComponent<PierceShotProjectile>();

        float baseDmg = PlayerDamage.Instance != null ? PlayerDamage.Instance.CurrentDamage : 20f; // fallback 20
        float dmg = baseDmg * Mathf.Max(0f, percent);

        proj.Configure(new PierceShotProjectile.Config
        {
            initialDirection = baseDir.normalized,
            speed = projectileSpeed,
            damage = dmg,
            mainCamera = Camera.main,
            destroyPadding = destroyPadding,
            homingMode = homingMode,
            homingTurnSpeedDeg = homingTurnSpeedDeg,
            homingUseLerp = homingUseLerp,
            allowedConeDeg = allowedConeDeg,
            targetOverride = targetOverride
        });
    }
}

