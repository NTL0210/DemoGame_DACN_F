using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Spin Attack Manager - Quản lý hệ thống Spin Attack
/// - Tạo và quản lý các spin objects xoay quanh player
/// - Tự động chia đều góc giữa các spin
/// - Quản lý level progression và stats
/// </summary>
public class SpinAttackManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject spinPrefab; // Prefab để tạo spin mới (từ lv 3 trở đi)

    // References to initial spins in the scene
    private GameObject initialSpin1;
    private GameObject initialSpin2;
    [SerializeField] private Transform spinParent; // Parent object "Spinning Attack"

    [Header("Spin Settings")]
    [SerializeField] private float spinRadius = 1.5f; // Khoảng cách từ player đến spin (giống pointer)
    [SerializeField] private float rotationSpeed = 180f; // Tốc độ xoay (độ/giây)

    [Header("Level Stats")]
    [SerializeField] private int currentLevel = 0;
    
    // Level 1: 2 spins, 40% damage, 10s duration, 8s cooldown
    // Level 2: 3 spins, 45% damage, 12s duration, 8s cooldown
    // Level 3: 4 spins, 50% damage, 15s duration, 8s cooldown
    // Level 4: 5 spins, 55% damage, 20s duration, 5s cooldown
    
    private readonly int[] spinCountPerLevel = { 0, 2, 3, 4, 5 };
    private readonly float[] damagePercentPerLevel = { 0f, 0.40f, 0.45f, 0.50f, 0.55f };
    private readonly float[] durationPerLevel = { 0f, 10f, 12f, 15f, 20f };
    private readonly float[] cooldownPerLevel = { 0f, 8f, 8f, 8f, 5f };

    [Header("Runtime Info")]
    [SerializeField] private bool isActive = false;
    [SerializeField] private float nextActivationTime = 0f;
    [SerializeField] private float deactivationTime = 0f;

    private List<GameObject> activeSpins = new List<GameObject>();
    private float currentAngle = 0f;

    public int CurrentLevel => currentLevel;
    public float CurrentDamagePercent => currentLevel > 0 ? damagePercentPerLevel[currentLevel] : 0f;

    private void Awake()
    {
        // Tìm player nếu chưa được gán
        if (player == null)
        {
            player = transform.root;
        }

        // Tìm spin parent
        if (spinParent == null)
        {
            spinParent = transform.Find("Spin level");
            if (spinParent == null)
            {
                Debug.LogError("[SpinAttackManager] Không tìm thấy 'Spin level' parent!");
            }
        }

        // Tìm các spin ban đầu trong scene
        if (spinParent != null)
        {
            Transform spin1_transform = spinParent.Find("Spin 1");
            if (spin1_transform != null) initialSpin1 = spin1_transform.gameObject;
            else Debug.LogError("[SpinAttackManager] Không tìm thấy 'Spin 1' trong parent!");

            Transform spin2_transform = spinParent.Find("Spin 2");
            if (spin2_transform != null) initialSpin2 = spin2_transform.gameObject;
            else Debug.LogError("[SpinAttackManager] Không tìm thấy 'Spin 2' trong parent!");
        }

        // Đảm bảo các spin ban đầu inactive
        if(initialSpin1 != null) initialSpin1.SetActive(false);
        if(initialSpin2 != null) initialSpin2.SetActive(false);

        // Fallback: nếu spinPrefab chưa được gán, dùng initialSpin1 làm mẫu
        if (spinPrefab == null && initialSpin1 != null)
        {
            spinPrefab = initialSpin1;
        }
    }

    private void Update()
    {
        // Kiểm tra cooldown để kích hoạt lại
        if (!isActive && currentLevel > 0 && Time.time >= nextActivationTime)
        {
            ActivateSpins();
        }

        // Kiểm tra thời gian tồn tại
        if (isActive && Time.time >= deactivationTime)
        {
            DeactivateSpins();
        }

        // Xoay các spin quanh player
        if (isActive)
        {
            RotateSpins();
        }
    }

    /// <summary>
    /// Nâng cấp Spin Attack lên level tiếp theo
    /// </summary>
    public void LevelUp()
    {
        if (currentLevel >= 4)
        {
            Debug.LogWarning("[SpinAttackManager] Đã đạt max level!");
            return;
        }

        currentLevel++;
        Debug.Log($"[SpinAttackManager] Level Up! Hiện tại: Level {currentLevel}");

        // Nếu đang active, cập nhật số lượng spin ngay lập tức
        if (isActive)
        {
            UpdateSpinCount();
        }
        else
        {
            // Nếu chưa active, kích hoạt ngay lần đầu tiên
            ActivateSpins();
        }
    }

    /// <summary>
    /// Kích hoạt các spin
    /// </summary>
    private void ActivateSpins()
    {
        if (currentLevel <= 0) return;

        isActive = true;
        float duration = durationPerLevel[currentLevel];
        deactivationTime = Time.time + duration;

        Debug.Log($"[SpinAttackManager] Kích hoạt Spin Attack Level {currentLevel} - Duration: {duration}s");

        // Tạo hoặc cập nhật số lượng spin
        UpdateSpinCount();
    }

    /// <summary>
    /// Tắt các spin
    /// </summary>
    private void DeactivateSpins()
    {
        isActive = false;
        float cooldown = cooldownPerLevel[currentLevel];
        nextActivationTime = Time.time + cooldown;

        Debug.Log($"[SpinAttackManager] Tắt Spin Attack - Cooldown: {cooldown}s");

        // Tắt 2 spin ban đầu và phá hủy các spin đã tạo thêm
        for (int i = activeSpins.Count - 1; i >= 0; i--)
        {
            GameObject spin = activeSpins[i];
            if (spin != null)
            {
                // Chỉ tắt 2 spin ban đầu, không phá hủy chúng
                if (spin == initialSpin1 || spin == initialSpin2)
                {
                    spin.SetActive(false);
                }
                else
                {
                    // Phá hủy các spin được tạo ra (Spin 3, 4, 5...)
                    Destroy(spin);
                }
            }
        }

        // Xóa danh sách để chuẩn bị cho lần kích hoạt tiếp theo
        activeSpins.Clear();
    }

    /// <summary>
    /// Cập nhật số lượng spin theo level.
    /// Logic này sẽ sử dụng lại 2 spin ban đầu và chỉ tạo mới khi cần.
    /// </summary>
    private void UpdateSpinCount()
    {
        int targetCount = spinCountPerLevel[currentLevel];

        // Xóa các spin đã được tạo ra (Instantiated) ở các level trước
        // Bắt đầu từ index 2 vì 0 và 1 là các spin ban đầu
        for (int i = activeSpins.Count - 1; i >= 2; i--)
        {
            if (activeSpins[i] != null) Destroy(activeSpins[i]);
        }
        activeSpins.Clear();

        // Thêm 2 spin ban đầu vào danh sách nếu level >= 1
        if (currentLevel >= 1)
        {
            if (initialSpin1 != null) activeSpins.Add(initialSpin1);
            if (initialSpin2 != null) activeSpins.Add(initialSpin2);
        }

        // Tạo thêm các spin mới nếu cần (từ spin thứ 3 trở đi)
        while (activeSpins.Count < targetCount)
        {
            CreateNewSpin();
        }

        // Kích hoạt tất cả các spin đang có trong danh sách
        foreach (var spin in activeSpins)
        {
            if (spin != null)
            {
                spin.SetActive(true);
                // Đảm bảo component SpinDamage được khởi tạo
                SpinDamage spinDamage = spin.GetComponent<SpinDamage>();
                if (spinDamage == null) spinDamage = spin.AddComponent<SpinDamage>();
                spinDamage.Initialize(this);
            }
        }
        
        // Sắp xếp lại vị trí các spin
        RepositionSpins();
    }

    /// <summary>
    /// Tạo một spin mới (từ spin thứ 3 trở đi)
    /// </summary>
    private void CreateNewSpin()
    {
        if (spinPrefab == null)
        {
            Debug.LogError("[SpinAttackManager] spinPrefab chưa được gán trong Inspector!");
            return;
        }

        // Tạo một bản sao từ prefab
        GameObject newSpin = Instantiate(spinPrefab, spinParent);

        newSpin.name = $"Spin {activeSpins.Count + 1}";
        newSpin.SetActive(true); // Sẽ được quản lý bởi UpdateSpinCount

        activeSpins.Add(newSpin);
    }

    /// <summary>
    /// Sắp xếp lại vị trí các spin để chia đều góc
    /// </summary>
    private void RepositionSpins()
    {
        int count = activeSpins.Count;
        if (count == 0) return;

        float angleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            if (activeSpins[i] == null) continue;

            float angle = i * angleStep;
            UpdateSpinPosition(activeSpins[i], angle);
        }
    }

    /// <summary>
    /// Xoay các spin quanh player
    /// </summary>
    private void RotateSpins()
    {
        currentAngle += rotationSpeed * Time.deltaTime;
        if (currentAngle >= 360f) currentAngle -= 360f;

        int count = activeSpins.Count;
        if (count == 0) return;

        float angleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            if (activeSpins[i] == null) continue;

            float angle = currentAngle + (i * angleStep);
            UpdateSpinPosition(activeSpins[i], angle);
        }
    }

    /// <summary>
    /// Cập nhật vị trí của một spin
    /// </summary>
    private void UpdateSpinPosition(GameObject spin, float angle)
    {
        float radian = angle * Mathf.Deg2Rad;
        float x = player.position.x + Mathf.Cos(radian) * spinRadius;
        float y = player.position.y + Mathf.Sin(radian) * spinRadius;

        spin.transform.position = new Vector3(x, y, player.position.z);

        // Xoay spin theo hướng di chuyển (optional - để spin quay theo)
        spin.transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    /// <summary>
    /// Lấy damage hiện tại của spin (dựa trên base damage của player)
    /// </summary>
    public float GetSpinDamage()
    {
        if (PlayerDamage.Instance == null) return 0f;

        float baseDamage = PlayerDamage.Instance.CurrentDamage;
        float damagePercent = CurrentDamagePercent;

        return baseDamage * damagePercent;
    }

    /// <summary>
    /// Kiểm tra xem spin có đang active không
    /// </summary>
    public bool IsActive()
    {
        return isActive;
    }

    private void OnDrawGizmosSelected()
    {
        // Vẽ vòng tròn để visualize spin radius
        if (player != null)
        {
            Gizmos.color = Color.cyan;
            DrawCircle(player.position, spinRadius, 32);
        }
    }

    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
}

