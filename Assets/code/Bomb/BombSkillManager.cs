using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Quản lý skill Bomb: level, cooldown, spawn bomb prefab và truyền tham số.
/// Stats mới:
/// Lv1: 1 bomb, 80% sát thương cơ bản, tồn tại 15s, cooldown 20s
/// Lv2: 1 bomb, 100% sát thương cơ bản, tồn tại 17s, cooldown 20s
/// Lv3: 1 bomb, 120% sát thương cơ bản, tồn tại 19s, cooldown 20s
/// Lv4: 3 bomb (hướng ngẫu nhiên), 140% sát thương cơ bản, tồn tại 21s, cooldown 15s
/// </summary>
public class BombSkillManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject bombPrefab; // nên là prefab "Bomb item"
    [SerializeField] private GameObject bombEffectPrefab; // prefab nổ: "Bomb Effect"

    [Header("Parent Overrides (Optional)")]
    [Tooltip("Nếu gán, Bomb sẽ được spawn làm con của Transform này. Nếu bỏ trống sẽ dùng ExpSpawnManager.Instance")] 
    [SerializeField] private Transform bombParentOverride;
    [Tooltip("Nếu gán, Bomb Effect sẽ spawn làm con của Transform này. Nếu bỏ trống sẽ dùng bombParent hoặc ExpSpawnManager.Instance")] 
    [SerializeField] private Transform effectParentOverride;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnOffset = 0.6f; // ném ra cách player 1 đoạn nhỏ
    [SerializeField] private float throwDistanceFirst = 3.0f; // quãng đường bounce 1
    [SerializeField] private float throwDistanceSecond = 1.7f; // quãng đường bounce 2
    [SerializeField] private float arcHeightFirst = 1.2f; // độ cao vòng cung 1 (trục Y)
    [SerializeField] private float arcHeightSecond = 0.7f; // độ cao vòng cung 2
    [SerializeField] private float bounceDuration1 = 0.45f;
    [SerializeField] private float bounceDuration2 = 0.35f;

    [Header("Explosion Settings")]
    [SerializeField] private float explosionRadius = 1.6f; // bán kính nổ ngắn

    [Header("Level Runtime")]
    [SerializeField] private int currentLevel = 0;

    // Per-level config arrays (index = level)
    private readonly int[] bombCount = { 0, 1, 1, 1, 3 };
    private readonly float[] damagePercent = { 0f, 0.80f, 1.00f, 1.20f, 1.40f };
    private readonly float[] lifeTimePerLevel = { 0f, 15f, 17f, 19f, 21f };
    private readonly float[] cooldownPerLevel = { 0f, 20f, 20f, 20f, 15f };

    private float nextSpawnTime = 0f;
    private readonly List<GameObject> activeBombs = new List<GameObject>();

    public int CurrentLevel => currentLevel;

    private void Awake()
    {
        if (player == null) player = transform.root;
    }

    private void Update()
    {
        if (currentLevel <= 0) return;
        if (Time.time >= nextSpawnTime)
        {
            SpawnWave();
            nextSpawnTime = Time.time + cooldownPerLevel[currentLevel];
        }

        // Dọn các bomb null khỏi danh sách
        for (int i = activeBombs.Count - 1; i >= 0; i--)
        {
            if (activeBombs[i] == null) activeBombs.RemoveAt(i);
        }
    }

    public void LevelUp()
    {
        if (currentLevel >= 4)
        {
            Debug.Log("[BombSkillManager] Đã max level");
            return;
        }
        currentLevel++;

        // Kích hoạt spawn ngay lần đầu nâng lên Lv1
        if (currentLevel == 1)
        {
            nextSpawnTime = Time.time; // spawn ngay
        }
    }

    private void SpawnWave()
    {
        if (bombPrefab == null)
        {
            Debug.LogError("[BombSkillManager] Chưa gán bombPrefab (Bomb item)");
            return;
        }

        int count = bombCount[currentLevel];
        for (int i = 0; i < count; i++)
        {
            Vector2 dir = Random.insideUnitCircle.normalized; // hướng ngẫu nhiên
            if (dir == Vector2.zero) dir = Vector2.right;

            Vector3 spawnPos = player.position + (Vector3)(dir * spawnOffset);
            Transform parent = bombParentOverride != null ? bombParentOverride : (ExpSpawnManager.Instance != null ? ExpSpawnManager.Instance.transform : null);
            GameObject bombGO = Instantiate(bombPrefab, spawnPos, Quaternion.identity, parent);
            bombGO.name = count == 1 ? "Bomb" : $"Bomb {i + 1}";
            bombGO.tag = "Bomb"; // đảm bảo tag
            if (!bombGO.activeSelf) bombGO.SetActive(true);

            var bomb = bombGO.GetComponent<BombProjectile>();
            if (bomb == null) bomb = bombGO.AddComponent<BombProjectile>();

            bomb.Configure(new BombProjectile.Config
            {
                direction = dir,
                distance1 = throwDistanceFirst,
                distance2 = throwDistanceSecond,
                arcHeight1 = arcHeightFirst,
                arcHeight2 = arcHeightSecond,
                bounceTime1 = bounceDuration1,
                bounceTime2 = bounceDuration2,
                armDelay = 0.3f,
                lifeTime = lifeTimePerLevel[currentLevel],
                damagePercent = damagePercent[currentLevel],
                explosionRadius = explosionRadius,
                effectPrefab = bombEffectPrefab
            });

            activeBombs.Add(bombGO);
        }
    }
}

