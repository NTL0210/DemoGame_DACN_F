using UnityEngine;
using System.Collections;

/// <summary>
/// Hiệu ứng flash trắng đơn giản cho Enemy
/// Flash MỖI LẦN bị damage (KHÔNG có cooldown/invincibility)
/// </summary>
public class EnemyFlashSimple : MonoBehaviour
{
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private bool autoFindSpriteRenderer = true;
    [SerializeField] private bool autoFlashOnDamage = true; // Tự động flash khi bị damage
    [SerializeField] private bool showDebugInfo = true; // Debug: Hiện log khi flash (BẬT MẶC ĐỊNH)
    
    private SpriteRenderer sr;
    private EnemyController enemyController;
    private float lastHealth;
    private int pendingFlashes = 0; // Số lần flash cần thực hiện
    private Coroutine currentFlashCoroutine; // Track flash coroutine hiện tại
    private bool isFlashing = false; // Đang flash hay không
    
    void Awake()
    {
        if (showDebugInfo)
        {
            Debug.Log($"[EnemyFlashSimple] Awake on {gameObject.name}");
        }
        
        if (autoFindSpriteRenderer)
        {
            sr = GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = GetComponentInChildren<SpriteRenderer>();
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"[EnemyFlashSimple] SpriteRenderer found: {sr != null}");
            }
        }
        
        if (autoFlashOnDamage)
        {
            enemyController = GetComponent<EnemyController>();
            if (enemyController != null)
            {
                lastHealth = enemyController.CurrentHealth;
                
                if (showDebugInfo)
                {
                    Debug.Log($"[EnemyFlashSimple] EnemyController found. Initial HP: {lastHealth}");
                }
            }
            else if (showDebugInfo)
            {
                Debug.LogWarning($"[EnemyFlashSimple] EnemyController NOT found on {gameObject.name}!");
            }
        }
        
        if (sr == null)
        {
            Debug.LogError($"[EnemyFlashSimple] on {gameObject.name}: Không tìm thấy SpriteRenderer! Flash sẽ KHÔNG hoạt động!");
        }
    }
    
    void Update()
    {
        // Tự động flash MỖI LẦN HP giảm (KHÔNG có cooldown)
        if (autoFlashOnDamage && enemyController != null)
        {
            float currentHealth = enemyController.CurrentHealth;
            
            // Nếu HP giảm, tăng pending flash count
            if (currentHealth < lastHealth)
            {
                pendingFlashes++;
                
                if (showDebugInfo)
                {
                    Debug.Log($"Enemy {gameObject.name} HP giảm: {lastHealth} → {currentHealth} (Pending flashes: {pendingFlashes})");
                }
            }
            
            // QUAN TRỌNG: Update lastHealth MỖI FRAME (không chỉ khi HP giảm)
            // Nếu để trong if → Mỗi frame sẽ detect lại HP giảm → Spam flash!
            lastHealth = currentHealth;
        }
        
        // Xử lý pending flashes
        if (pendingFlashes > 0 && !isFlashing)
        {
            pendingFlashes--;
            Flash();
        }
    }
    
    /// <summary>
    /// Flash trắng (MỖI LẦN bị hit, KHÔNG skip)
    /// Queue-based: Đảm bảo EVERY hit đều flash (không bỏ sót)
    /// </summary>
    public void Flash()
    {
        if (sr != null && !isFlashing)
        {
            currentFlashCoroutine = StartCoroutine(FlashRoutine());
        }
    }
    
    IEnumerator FlashRoutine()
    {
        if (sr == null) yield break;
        
        isFlashing = true;
        
        Color original = sr.color;
        // Dùng màu trắng sáng (200% brightness) để thấy rõ hơn
        Color brightWhite = new Color(2f, 2f, 2f, 1f);
        sr.color = brightWhite;
        
        if (showDebugInfo)
        {
            Debug.Log($"Enemy {gameObject.name} flash START (Pending: {pendingFlashes})");
        }
        
        yield return new WaitForSeconds(flashDuration);
        
        // Kiểm tra sr vẫn còn tồn tại (enemy có thể đã chết)
        if (sr != null)
        {
            sr.color = original;
        }
        
        isFlashing = false;
        currentFlashCoroutine = null; // Flash đã kết thúc
        
        if (showDebugInfo)
        {
            Debug.Log($"Enemy {gameObject.name} flash END (Pending: {pendingFlashes})");
        }
    }
    
}

