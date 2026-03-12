using UnityEngine;

/// <summary>
/// Nơi lưu prefab các chủng loại Enemy và cung cấp API spawn theo vị trí chỉ định
/// </summary>
public class SpawnEnemy : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject enemyTypeAPrefab; // Small
    [SerializeField] private GameObject enemyTypeBPrefab; // Big

    [Header("Settings")]
    [SerializeField] private LayerMask obstacleMask; // dùng để check vị trí spawn hợp lệ (tuỳ chọn)
    [SerializeField] private float minSpawnSeparation = 0.8f; // tránh trùng vị trí
    [SerializeField] private Transform spawnParent; // đặt parent để kế thừa scale/hệ toạ độ của nhóm Enemy

    private void Awake()
    {
        if (spawnParent == null) spawnParent = transform; // mặc định làm con của chính object Spawn Enemy
    }

    public GameObject GetPrefab(EnemyType type)
    {
        return type == EnemyType.TypeA ? enemyTypeAPrefab : enemyTypeBPrefab;
    }

    public GameObject Spawn(EnemyType type, Vector2 position, Quaternion rotation)
    {
        GameObject prefab = GetPrefab(type);
        if (prefab == null) return null;
        GameObject go = Instantiate(prefab, position, rotation, spawnParent);
        if (go != null && !go.activeSelf)
        {
            go.SetActive(true);
        }
        return go;
    }

    public Vector2 FindNearestFreePosition(Vector2 desired, float searchRadius = 1.5f, int maxTries = 10)
    {
        // Dò vòng quanh vị trí mong muốn để tránh đè lên enemy khác
        for (int i = 0; i < maxTries; i++)
        {
            float angle = (360f / Mathf.Max(1, maxTries)) * i * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (minSpawnSeparation + 0.1f * i);
            Vector2 candidate = desired + offset;

            // Có thể bổ sung Physics2D.OverlapCircle để tránh va vào chướng ngại vật hoặc enemy
            var overlap = Physics2D.OverlapCircle(candidate, minSpawnSeparation);
            if (overlap == null)
            {
                return candidate;
            }
        }
        return desired;
    }
}


