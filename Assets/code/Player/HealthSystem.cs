using UnityEngine;
using TMPro; // Thêm thư viện cho TextMeshPro

/// <summary>
/// Hệ thống quản lý máu - Điều khiển máu và hiển thị tim của người chơi
/// </summary>
public class HealthSystem : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 5; // Máu tối đa
    [SerializeField] private float currentHealth = 5f; // Máu hiện tại (dùng float để hỗ trợ nửa tim)
    
    [Header("Damage Cooldown")]
    [SerializeField] private float damageCooldown = 1.5f; // Thời gian hồi chiêu nhận sát thương (giây) - Sẽ được thay thế bởi PlayerInvincibility
    
    [Header("Heart Object References")]
    [SerializeField] private Transform heartParent; // Đối tượng cha Heart
    
    [Header("Death System")]
    [SerializeField] private PlayerMoveNew playerMove; // Tham chiếu bộ điều khiển người chơi
    
    [Header("Invincibility System")]
    [SerializeField] private PlayerInvincibility playerInvincibility; // Tham chiếu hệ thống bất tử
    
    [Header("UI Effects")]
    [SerializeField] private HeartRippleEffect heartRippleEffect; // Tham chiếu hiệu ứng gợn sóng

    [Header("Heart Up & Shield System")]
    [SerializeField] private PlayerShield playerShield; // Tham chiếu hệ thống khiên
    [SerializeField] private TextMeshProUGUI healthText; // Text hiển thị máu
    [SerializeField] private int heartLevel = 0; // Cấp độ của Heart Up
    private const int maxPossibleHealth = 9; // Số lượng tim tối đa có thể có
    
    // Mảng đối tượng tim
    private GameObject[] fullHearts;
    private GameObject[] halfHearts;
    private GameObject[] emptyHearts;
    private GameObject[] heartContainers; // Mảng chứa các GameObject cha (Heart 1, Heart 2,...)
    
    // Biến hồi chiêu sát thương
    private float lastDamageTime = 0f;
    private bool canTakeDamage = true;
    
    // Sự kiện
    public System.Action<float> OnHealthChanged; // Sự kiện thay đổi máu
    public System.Action OnPlayerDeath; // Sự kiện người chơi chết
    
    private void Awake()
    {
        // Tự động tìm đối tượng cha Heart
        if (heartParent == null)
        {
            heartParent = transform.Find("Heart");
            if (heartParent == null)
            {
                Debug.LogError("Không tìm thấy đối tượng cha Heart! Hãy đảm bảo đối tượng Heart là con của Player.");
                return;
            }
        }
        
        // Tự động lấy tham chiếu PlayerMoveNew
        if (playerMove == null)
        {
            playerMove = GetComponent<PlayerMoveNew>();
        }
        
        // Tự động lấy tham chiếu PlayerInvincibility
        if (playerInvincibility == null)
        {
            playerInvincibility = GetComponent<PlayerInvincibility>();
        }

        // Tự động tìm hoặc tạo PlayerShield
        if (playerShield == null)
        {
            playerShield = GetComponent<PlayerShield>();
            if (playerShield == null) playerShield = gameObject.AddComponent<PlayerShield>();
        }
        
        // Khởi tạo mảng tim
        InitializeHeartArrays();

        // Tìm hoặc tạo và khởi tạo HeartRippleEffect
        if (heartParent != null)
        {
            heartRippleEffect = heartParent.GetComponent<HeartRippleEffect>();
            if (heartRippleEffect == null)
            {
                heartRippleEffect = heartParent.gameObject.AddComponent<HeartRippleEffect>();
            }
            // Giả sử cấu trúc UI của bạn có các container "Heart 1", "Heart 2",...
            // Nếu không, bạn cần điều chỉnh lại logic tìm kiếm trong HeartRippleEffect.cs
            heartRippleEffect.Initialize(heartParent, maxHealth);
        }
        
        // Thiết lập máu ban đầu
        currentHealth = maxHealth;
    }
    
    private void Start()
    {
        // Khởi tạo trạng thái hiển thị tim và text
        UpdateHeartDisplay();
    }
    
    private void Update()
    {
        // Kiểm tra hồi chiêu sát thương
        if (!canTakeDamage && Time.time - lastDamageTime >= damageCooldown)
        {
            canTakeDamage = true;
        }
    }

    /// <summary>
    /// Kiểm tra người chơi còn sống không
    /// </summary>
    public bool IsAlive()
    {
        return currentHealth > 0f;
    }
    
    /// <summary>
    /// Khởi tạo mảng đối tượng tim
    /// </summary>
    private void InitializeHeartArrays()
    {
        // Khởi tạo các mảng với số lượng tim tối đa có thể có
        heartContainers = new GameObject[maxPossibleHealth];
        fullHearts = new GameObject[maxPossibleHealth];
        halfHearts = new GameObject[maxPossibleHealth];
        emptyHearts = new GameObject[maxPossibleHealth];

        for (int i = 0; i < maxPossibleHealth; i++)
        {
            int heartIndex = i + 1;
            string containerName = $"Heart {heartIndex}";
            Transform heartContainerTransform = heartParent.Find(containerName);

            if (heartContainerTransform != null)
            {
                // Lưu trữ container cha
                heartContainers[i] = heartContainerTransform.gameObject;

                // Tìm các đối tượng con bên trong container
                fullHearts[i] = heartContainerTransform.Find($"Full Heart {heartIndex}")?.gameObject;
                halfHearts[i] = heartContainerTransform.Find($"Half Heart {heartIndex}")?.gameObject;
                emptyHearts[i] = heartContainerTransform.Find($"Empty Heart {heartIndex}")?.gameObject;

                if (emptyHearts[i] == null)
                {
                    // Thử tìm với lỗi chính tả cũ để tương thích ngược
                    emptyHearts[i] = heartContainerTransform.Find($"Emty Heart {heartIndex}")?.gameObject;
                    if(emptyHearts[i] != null) Debug.LogWarning($"Phát hiện tên cũ 'Emty Heart {heartIndex}'. Vui lòng đổi tên thành 'Empty Heart {heartIndex}'.");
                }
            }
            else
            {
                // Không cần cảnh báo nếu không tìm thấy các tim cao hơn (6, 7, 8, 9) ban đầu
            }
        }
    }
    
    /// <summary>
    /// Nhận sát thương (mỗi lần giảm 0.5, tức là nửa tim)
    /// </summary>
    /// <param name="damage">Giá trị sát thương (0.5 = nửa tim)</param>
    public void LevelUp()
    {
        if (heartLevel >= 4) return; // Đã đạt cấp tối đa

        heartLevel++;

        float healthPercentage = currentHealth / maxHealth;

        maxHealth++;
        currentHealth = maxHealth * healthPercentage;

        // Hồi lại 1 máu khi nâng cấp
        currentHealth = Mathf.Min(maxHealth, currentHealth + 1);

        // Kích hoạt khiên ở cấp 4
        if (heartLevel == 4 && playerShield != null)
        {
            playerShield.ActivateShieldSystem();
        }

        Debug.Log($"Heart Up! Level: {heartLevel}, Max Health: {maxHealth}");
        UpdateHeartDisplay();
    }

    public int GetHeartLevel()
    {
        return heartLevel;
    }

    public void TakeDamage(float damage = 0.5f)
    {
        // Kiểm tra các điều kiện không thể nhận sát thương trước
        if (currentHealth <= 0 || !canTakeDamage) return;

        // Kích hoạt trạng thái bất tử ngay lập tức
        canTakeDamage = false;
        lastDamageTime = Time.time;
        if (playerInvincibility != null) playerInvincibility.StartInvincibility();

        // Ưu tiên kiểm tra và sử dụng khiên
        if (playerShield != null && playerShield.HasShield())
        {
            playerShield.UseShield();
            // Không trừ máu, chỉ kích hoạt invincibility và thoát
            return; 
        }


        // Xác định trái tim nào sẽ bị ảnh hưởng để hiệu ứng chạy đến đó
        int affectedHeartIndex = Mathf.CeilToInt(currentHealth) - 1;

        // Giảm máu
        currentHealth = Mathf.Max(0, currentHealth - damage);

        // Kích hoạt các hệ thống khác
        canTakeDamage = false;
        lastDamageTime = Time.time;
        if (playerInvincibility != null) playerInvincibility.StartInvincibility();

        // Chạy hiệu ứng gợn sóng và cập nhật UI sau khi hoàn tất
        if (heartRippleEffect != null)
        {
            heartRippleEffect.PlayRippleEffect(affectedHeartIndex, () => {
                UpdateHeartDisplay();
                CheckForDeath(); // Kiểm tra cái chết sau khi UI đã cập nhật
            });
        }
        else
        {
            // Fallback nếu không có hiệu ứng
            UpdateHeartDisplay();
            CheckForDeath();
        }

        OnHealthChanged?.Invoke(currentHealth);
    }

    // Tách logic kiểm tra cái chết ra một hàm riêng
    private void CheckForDeath()
    {
        if (currentHealth <= 0)
        {
            OnPlayerDeath?.Invoke();
            TriggerDeathAnimation();
        }
    }
    
    /// <summary>
    /// Hồi máu
    /// </summary>
    /// <param name="healAmount">Lượng hồi máu</param>
    public void Heal(float healAmount)
    {
        if (currentHealth >= maxHealth) return;

        // Xác định trái tim sẽ được hồi máu
        int affectedHeartIndex = Mathf.FloorToInt(currentHealth);

        // Hồi máu
        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);

        // Chạy hiệu ứng gợn sóng và cập nhật UI sau khi hoàn tất
        if (heartRippleEffect != null)
        {
            heartRippleEffect.PlayRippleEffect(affectedHeartIndex, () => {
                UpdateHeartDisplay();
            });
        }
        else
        {
            // Fallback nếu không có hiệu ứng
            UpdateHeartDisplay();
        }

        OnHealthChanged?.Invoke(currentHealth);
    }
    
    /// <summary>
    /// Cập nhật trạng thái hiển thị tim
    /// </summary>
    private void UpdateHeartDisplay()
    {
        // Cập nhật Text hiển thị máu
        if (healthText != null)
        {
            healthText.text = $"{Mathf.CeilToInt(currentHealth)}/{maxHealth}";
        }

        // Cập nhật các icon trái tim
        for (int i = 0; i < maxPossibleHealth; i++)
        {
            // Kiểm tra xem container cha có tồn tại không
            if (heartContainers[i] == null) continue;

            // Nếu index của trái tim lớn hơn hoặc bằng số máu tối đa hiện tại, hãy tắt container cha đi
            if (i >= maxHealth)
            {
                heartContainers[i].SetActive(false);
                continue;
            }

            // Nếu không, hãy bật container cha lên
            heartContainers[i].SetActive(true);

            // Bây giờ mới xử lý các trạng thái con bên trong
            bool fullHeartActive = false;
            bool halfHeartActive = false;
            bool emptyHeartActive = false;
            
            float heartValue = currentHealth - i; // Giá trị của tim hiện tại
            
            if (heartValue >= 1f)
            {
                fullHeartActive = true;
            }
            else if (heartValue >= 0.5f)
            {
                halfHeartActive = true;
            }
            else
            {
                emptyHeartActive = true;
            }
            
            // Thiết lập trạng thái hiển thị tim con
            if (fullHearts[i] != null) fullHearts[i].SetActive(fullHeartActive);
            if (halfHearts[i] != null) halfHearts[i].SetActive(halfHeartActive);
            if (emptyHearts[i] != null) emptyHearts[i].SetActive(emptyHeartActive);
        }
    }
    
    /// <summary>
    /// Kích hoạt animation chết
    /// </summary>
    private void TriggerDeathAnimation()
    {
        if (playerMove != null)
        {
            playerMove.ChangeToDeathState();
        }
    }
    
    /// <summary>
    /// Reset máu về giá trị tối đa
    /// </summary>
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        UpdateHeartDisplay();
        OnHealthChanged?.Invoke(currentHealth);
    }
    
    /// <summary>
    /// Lấy máu hiện tại
    /// </summary>
    public float GetCurrentHealth()
    {
        return currentHealth;
    }
    
    /// <summary>
    /// Lấy máu tối đa
    /// </summary>
    public int GetMaxHealth()
    {
        return maxHealth;
    }
    
    /// <summary>
    /// Kiểm tra có chết không
    /// </summary>
    public bool IsDead()
    {
        return currentHealth <= 0;
    }
    
    /// <summary>
    /// Kiểm tra có thể nhận sát thương không
    /// </summary>
    /// <returns>Có thể nhận sát thương không</returns>
    public bool CanTakeDamage()
    {
        // Ưu tiên sử dụng PlayerInvincibility nếu có
        if (playerInvincibility != null)
        {
            return playerInvincibility.CanTakeDamage();
        }
        
        // Fallback về hệ thống cũ nếu không có PlayerInvincibility
        return canTakeDamage;
    }
    
    /// <summary>
    /// Thiết lập thời gian hồi chiêu sát thương
    /// </summary>
    /// <param name="cooldown">Thời gian hồi chiêu mới (giây)</param>
    public void SetDamageCooldown(float cooldown)
    {
        damageCooldown = cooldown;
    }
    
    /// <summary>
    /// Lấy thời gian hồi chiêu sát thương
    /// </summary>
    /// <returns>Thời gian hồi chiêu hiện tại</returns>
    public float GetDamageCooldown()
    {
        return damageCooldown;
    }
    
    /// <summary>
    /// Reset hồi chiêu sát thương (cho phép nhận sát thương ngay lập tức)
    /// </summary>
    public void ResetDamageCooldown()
    {
        canTakeDamage = true;
        lastDamageTime = 0f;
    }
}
