using UnityEngine;
using System.Collections;

/// <summary>
/// Hệ thống bất tử tạm thời cho Player
/// Xử lý invincibility và hiệu ứng nháy trắng khi bị damage
/// </summary>
public class PlayerInvincibility : MonoBehaviour
{
    [Header("Cài đặt Invincibility")]
    [SerializeField] private float invincibilityDuration = 1.5f; // Thời gian bất tử (giây)
    [SerializeField] private float postInvincibilityDelay = 0.5f; // Thời gian delay sau khi hết invincibility (giây)
    
    [Header("Hiệu ứng Flash khi bị damage")]
    [SerializeField] private bool useHitFlashOnDamage = true; // Dùng hiệu ứng nháy trắng khi bị đánh
    [SerializeField] private float hitFlashDuration = 0.15f; // Thời gian flash (giây)
    [SerializeField] private HitFlashEffect hitFlashEffect; // Component HitFlashEffect (optional)
    [SerializeField] private bool autoFindHitFlash = true; // Tự động tìm HitFlashEffect
    
    [Header("Hiệu ứng nhấp nháy trong lúc Invincible (Legacy)")]
    [SerializeField] private bool useBlinkEffect = true; // Bật/tắt hiệu ứng nhấp nháy
    [SerializeField] private float blinkInterval = 0.15f; // Tần suất nhấp nháy (giây)
    [SerializeField] private SpriteRenderer[] spriteRenderers; // Các SpriteRenderer cần nhấp nháy
    [SerializeField] private bool autoFindSprites = true; // Tự động tìm SpriteRenderer
    
    // Biến riêng tư
    private bool isInvincible = false;
    private bool isInPostDelay = false; // Đang trong thời gian delay sau invincibility
    private Coroutine blinkCoroutine;
    private Coroutine postDelayCoroutine;
    private Color[] originalColors; // Lưu màu gốc của các sprite
    
    private void Awake()
    {
        // Tự động tìm HitFlashEffect nếu cần
        if (autoFindHitFlash && hitFlashEffect == null)
        {
            hitFlashEffect = GetComponent<HitFlashEffect>();
            if (hitFlashEffect == null)
            {
                hitFlashEffect = GetComponentInChildren<HitFlashEffect>();
            }
        }
        
        // Tự động tìm SpriteRenderer nếu cần (cho blink effect)
        if (autoFindSprites)
        {
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        }
        
        // Lưu màu gốc của các sprite (cho blink effect)
        if (spriteRenderers != null && spriteRenderers.Length > 0)
        {
            originalColors = new Color[spriteRenderers.Length];
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    originalColors[i] = spriteRenderers[i].color;
                }
            }
        }
    }
    
    /// <summary>
    /// Kiểm tra player có đang bất tử không
    /// </summary>
    /// <returns>True nếu đang bất tử</returns>
    public bool IsInvincible()
    {
        return isInvincible;
    }
    
    /// <summary>
    /// Kiểm tra player có thể nhận damage không (không đang invincible và không trong post-delay)
    /// </summary>
    /// <returns>True nếu có thể nhận damage</returns>
    public bool CanTakeDamage()
    {
        return !isInvincible && !isInPostDelay;
    }
    
    /// <summary>
    /// Bắt đầu trạng thái bất tử
    /// </summary>
    public void StartInvincibility()
    {
        if (isInvincible) return; // Đã đang bất tử rồi
        
        isInvincible = true;
        
        // Hiệu ứng nháy trắng khi bị đánh (1 lần)
        if (useHitFlashOnDamage)
        {
            // Ưu tiên dùng HitFlashEffect nếu có
            if (hitFlashEffect != null)
            {
                hitFlashEffect.Flash();
            }
            else
            {
                // Fallback: Flash trực tiếp bằng SpriteRenderer.color
                StartCoroutine(SimpleFlashWhite());
            }
        }
        
        // Bắt đầu hiệu ứng nhấp nháy (liên tục trong lúc invincible)
        // Delay để flash effect kết thúc trước khi bắt đầu blink
        if (useBlinkEffect)
        {
            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
            }
            // Delay blink effect để flash effect hiện trước
            blinkCoroutine = StartCoroutine(BlinkEffectDelayed(hitFlashDuration));
        }
        
        // Tự động kết thúc bất tử sau thời gian quy định
        StartCoroutine(EndInvincibilityAfterDelay());
    }
    
    /// <summary>
    /// Kết thúc trạng thái bất tử
    /// </summary>
    public void EndInvincibility()
    {
        if (!isInvincible) return; // Không đang bất tử
        
        isInvincible = false;
        
        // Dừng hiệu ứng nhấp nháy
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
        
        // Khôi phục màu gốc của các sprite
        RestoreOriginalColors();
        
        // Bắt đầu thời gian delay sau invincibility
        if (postDelayCoroutine != null)
        {
            StopCoroutine(postDelayCoroutine);
        }
        postDelayCoroutine = StartCoroutine(PostInvincibilityDelay());
    }
    
    /// <summary>
    /// Hiệu ứng nhấp nháy với delay (Blink effect delayed)
    /// Delay để flash effect kết thúc trước
    /// Nháy TRẮNG thay vì ẩn/hiện
    /// </summary>
    private IEnumerator BlinkEffectDelayed(float delay)
    {
        // Đợi flash effect kết thúc
        yield return new WaitForSeconds(delay);
        
        // Bắt đầu blink effect (NHÁY TRẮNG)
        Color brightWhite = new Color(2f, 2f, 2f, 1f); // 200% brightness
        
        while (isInvincible)
        {
            // Đổi sang trắng sáng
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    spriteRenderers[i].color = brightWhite;
                }
            }
            yield return new WaitForSeconds(blinkInterval);
            
            // Khôi phục màu gốc
            RestoreOriginalColors();
            yield return new WaitForSeconds(blinkInterval);
        }
    }
    
    /// <summary>
    /// Hiệu ứng nhấp nháy (Blink effect) - Legacy, không dùng delay
    /// Nháy TRẮNG thay vì ẩn/hiện
    /// </summary>
    private IEnumerator BlinkEffect()
    {
        Color brightWhite = new Color(2f, 2f, 2f, 1f); // 200% brightness
        
        while (isInvincible)
        {
            // Đổi sang trắng sáng
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    spriteRenderers[i].color = brightWhite;
                }
            }
            yield return new WaitForSeconds(blinkInterval);
            
            // Khôi phục màu gốc
            RestoreOriginalColors();
            yield return new WaitForSeconds(blinkInterval);
        }
    }
    
    /// <summary>
    /// Tự động kết thúc bất tử sau thời gian quy định
    /// </summary>
    private IEnumerator EndInvincibilityAfterDelay()
    {
        yield return new WaitForSeconds(invincibilityDuration);
        EndInvincibility();
    }
    
    /// <summary>
    /// Thời gian delay sau khi hết invincibility
    /// </summary>
    private IEnumerator PostInvincibilityDelay()
    {
        isInPostDelay = true;
        yield return new WaitForSeconds(postInvincibilityDelay);
        isInPostDelay = false;
    }
    
    /// <summary>
    /// Thiết lập độ trong suốt cho tất cả sprite
    /// </summary>
    /// <param name="alpha">Độ trong suốt (0-1)</param>
    private void SetSpritesAlpha(float alpha)
    {
        if (spriteRenderers == null) return;
        
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                Color color = spriteRenderers[i].color;
                color.a = alpha;
                spriteRenderers[i].color = color;
            }
        }
    }
    
    /// <summary>
    /// Khôi phục màu gốc của các sprite
    /// </summary>
    private void RestoreOriginalColors()
    {
        if (spriteRenderers == null || originalColors == null) return;
        
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null && i < originalColors.Length)
            {
                spriteRenderers[i].color = originalColors[i];
            }
        }
    }
    
    /// <summary>
    /// Flash trắng đơn giản (fallback nếu không có HitFlashEffect)
    /// </summary>
    private IEnumerator SimpleFlashWhite()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0) yield break;
        
        // Lưu màu gốc
        Color[] flashOriginalColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                flashOriginalColors[i] = spriteRenderers[i].color;
            }
        }
        
        // Đổi sang màu trắng sáng (brighten)
        // Nếu sprite đã trắng sẵn, multiply để làm sáng hơn
        Color brightWhite = new Color(2f, 2f, 2f, 1f); // 200% brightness
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                spriteRenderers[i].color = brightWhite;
            }
        }
        
        // Đợi
        yield return new WaitForSeconds(hitFlashDuration);
        
        // Khôi phục màu gốc
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                spriteRenderers[i].color = flashOriginalColors[i];
            }
        }
    }
    
    /// <summary>
    /// Thiết lập thời gian bất tử
    /// </summary>
    /// <param name="duration">Thời gian bất tử mới (giây)</param>
    public void SetInvincibilityDuration(float duration)
    {
        invincibilityDuration = duration;
    }
    
    /// <summary>
    /// Thiết lập tần suất nhấp nháy
    /// </summary>
    /// <param name="interval">Tần suất nhấp nháy mới (giây)</param>
    public void SetBlinkInterval(float interval)
    {
        blinkInterval = interval;
    }
    
    /// <summary>
    /// Bật/tắt hiệu ứng nháy trắng khi bị đánh
    /// </summary>
    public void SetUseHitFlash(bool use)
    {
        useHitFlashOnDamage = use;
    }
    
    /// <summary>
    /// Bật/tắt hiệu ứng nhấp nháy
    /// </summary>
    public void SetUseBlinkEffect(bool use)
    {
        useBlinkEffect = use;
    }
    
    /// <summary>
    /// Lấy thời gian bất tử hiện tại
    /// </summary>
    /// <returns>Thời gian bất tử (giây)</returns>
    public float GetInvincibilityDuration()
    {
        return invincibilityDuration;
    }
    
    /// <summary>
    /// Lấy tần suất nhấp nháy hiện tại
    /// </summary>
    /// <returns>Tần suất nhấp nháy (giây)</returns>
    public float GetBlinkInterval()
    {
        return blinkInterval;
    }
    
    /// <summary>
    /// Lấy HitFlashEffect component
    /// </summary>
    public HitFlashEffect GetHitFlashEffect()
    {
        return hitFlashEffect;
    }
}
