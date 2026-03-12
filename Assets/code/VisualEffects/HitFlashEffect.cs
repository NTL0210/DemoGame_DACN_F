using UnityEngine;
using System.Collections;

/// <summary>
/// Hiệu ứng nháy trắng khi bị trúng đòn
/// Hỗ trợ cả Material Swap và Color Lerp
/// Có thể dùng cho cả Player và Enemy
/// </summary>
public class HitFlashEffect : MonoBehaviour
{
    [Header("Cài đặt Flash Effect")]
    [SerializeField] private float flashDuration = 0.15f; // Thời gian nháy (giây)
    [SerializeField] private Color flashColor = Color.white; // Màu flash
    [SerializeField] private AnimationCurve flashIntensityCurve = AnimationCurve.EaseInOut(0, 1, 1, 0); // Độ mạnh flash theo thời gian
    
    [Header("Phương thức Flash")]
    [SerializeField] private FlashMethod flashMethod = FlashMethod.ColorReplace;
    [Tooltip("Material flash (màu trắng toàn bộ). Để trống nếu dùng Color method")]
    [SerializeField] private Material flashMaterial;
    
    [Header("Cài đặt Sprite")]
    [SerializeField] private SpriteRenderer[] spriteRenderers; // Các SpriteRenderer cần flash
    [SerializeField] private bool autoFindSprites = true; // Tự động tìm SpriteRenderer
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    // Enum phương thức flash
    public enum FlashMethod
    {
        ColorAdditive,      // Thêm màu trắng lên sprite (giữ nguyên màu gốc + sáng lên)
        ColorReplace,       // Đổi màu sprite sang màu flash (mất màu gốc tạm thời)
        MaterialSwap        // Đổi material (yêu cầu flashMaterial)
    }
    
    // Biến riêng tư
    private Material[] originalMaterials;
    private Color[] originalColors;
    private Coroutine currentFlashCoroutine;
    private bool isFlashing = false;
    
    private void Awake()
    {
        InitializeSpriteRenderers();
        CacheOriginalValues();
    }
    
    /// <summary>
    /// Khởi tạo SpriteRenderer
    /// </summary>
    private void InitializeSpriteRenderers()
    {
        if (autoFindSprites || spriteRenderers == null || spriteRenderers.Length == 0)
        {
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            
            if (showDebugInfo)
            {
                Debug.Log($"HitFlashEffect on {gameObject.name}: Tìm thấy {spriteRenderers.Length} SpriteRenderer(s)");
            }
        }
        
        if (spriteRenderers.Length == 0)
        {
            Debug.LogWarning($"HitFlashEffect on {gameObject.name}: Không tìm thấy SpriteRenderer nào!");
        }
    }
    
    /// <summary>
    /// Lưu giá trị gốc của material và color
    /// </summary>
    private void CacheOriginalValues()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0) return;
        
        // Cache original materials
        originalMaterials = new Material[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                originalMaterials[i] = spriteRenderers[i].material;
            }
        }
        
        // Cache original colors
        originalColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                originalColors[i] = spriteRenderers[i].color;
            }
        }
    }
    
    /// <summary>
    /// Bắt đầu hiệu ứng flash
    /// </summary>
    public void Flash()
    {
        // Nếu đang flash, dừng flash cũ
        if (currentFlashCoroutine != null)
        {
            StopCoroutine(currentFlashCoroutine);
        }
        
        currentFlashCoroutine = StartCoroutine(FlashRoutine());
    }
    
    /// <summary>
    /// Bắt đầu hiệu ứng flash với thời gian tùy chỉnh
    /// </summary>
    public void Flash(float duration)
    {
        float originalDuration = flashDuration;
        flashDuration = duration;
        Flash();
        flashDuration = originalDuration;
    }
    
    /// <summary>
    /// Bắt đầu hiệu ứng flash với màu tùy chỉnh
    /// </summary>
    public void Flash(Color customFlashColor)
    {
        Color originalColor = flashColor;
        flashColor = customFlashColor;
        Flash();
        flashColor = originalColor;
    }
    
    /// <summary>
    /// Coroutine xử lý flash effect
    /// </summary>
    private IEnumerator FlashRoutine()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0) yield break;
        
        isFlashing = true;
        float elapsedTime = 0f;
        
        if (showDebugInfo)
        {
            Debug.Log($"HitFlashEffect: Bắt đầu flash trên {gameObject.name} (Method: {flashMethod})");
        }
        
        // Chọn phương thức flash
        switch (flashMethod)
        {
            case FlashMethod.ColorAdditive:
                yield return FlashColorAdditive(elapsedTime);
                break;
            
            case FlashMethod.ColorReplace:
                yield return FlashColorReplace(elapsedTime);
                break;
            
            case FlashMethod.MaterialSwap:
                yield return FlashMaterialSwap();
                break;
        }
        
        isFlashing = false;
        
        if (showDebugInfo)
        {
            Debug.Log($"HitFlashEffect: Kết thúc flash trên {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Flash bằng cách thêm màu trắng (Additive) - RECOMMENDED
    /// Giữ nguyên màu gốc, chỉ làm sáng lên
    /// </summary>
    private IEnumerator FlashColorAdditive(float elapsedTime)
    {
        while (elapsedTime < flashDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / flashDuration;
            float intensity = flashIntensityCurve.Evaluate(normalizedTime);
            
            // Lerp từ màu gốc sang màu gốc + flashColor (additive)
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    Color targetColor = originalColors[i] + (flashColor * intensity);
                    spriteRenderers[i].color = targetColor;
                }
            }
            
            yield return null;
        }
        
        // Khôi phục màu gốc
        RestoreOriginalColors();
    }
    
    /// <summary>
    /// Flash bằng cách thay thế màu (Replace)
    /// Đổi toàn bộ màu sprite sang màu flash
    /// </summary>
    private IEnumerator FlashColorReplace(float elapsedTime)
    {
        while (elapsedTime < flashDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / flashDuration;
            float intensity = flashIntensityCurve.Evaluate(normalizedTime);
            
            // Lerp từ màu gốc sang flashColor
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    Color targetColor = Color.Lerp(originalColors[i], flashColor, intensity);
                    spriteRenderers[i].color = targetColor;
                }
            }
            
            yield return null;
        }
        
        // Khôi phục màu gốc
        RestoreOriginalColors();
    }
    
    /// <summary>
    /// Flash bằng cách đổi material
    /// Yêu cầu có flashMaterial (Unlit white material)
    /// </summary>
    private IEnumerator FlashMaterialSwap()
    {
        if (flashMaterial == null)
        {
            Debug.LogWarning($"HitFlashEffect on {gameObject.name}: flashMaterial is null! Fallback to ColorAdditive.");
            yield return FlashColorAdditive(0f);
            yield break;
        }
        
        // Đổi sang flash material
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                spriteRenderers[i].material = flashMaterial;
            }
        }
        
        yield return new WaitForSeconds(flashDuration);
        
        // Khôi phục material gốc
        RestoreOriginalMaterials();
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
    /// Khôi phục material gốc của các sprite
    /// </summary>
    private void RestoreOriginalMaterials()
    {
        if (spriteRenderers == null || originalMaterials == null) return;
        
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null && i < originalMaterials.Length)
            {
                spriteRenderers[i].material = originalMaterials[i];
            }
        }
    }
    
    /// <summary>
    /// Dừng hiệu ứng flash ngay lập tức
    /// </summary>
    public void StopFlash()
    {
        if (currentFlashCoroutine != null)
        {
            StopCoroutine(currentFlashCoroutine);
            currentFlashCoroutine = null;
        }
        
        RestoreOriginalColors();
        RestoreOriginalMaterials();
        isFlashing = false;
    }
    
    /// <summary>
    /// Kiểm tra có đang flash không
    /// </summary>
    public bool IsFlashing()
    {
        return isFlashing;
    }
    
    /// <summary>
    /// Thiết lập thời gian flash
    /// </summary>
    public void SetFlashDuration(float duration)
    {
        flashDuration = duration;
    }
    
    /// <summary>
    /// Thiết lập màu flash
    /// </summary>
    public void SetFlashColor(Color color)
    {
        flashColor = color;
    }
    
    /// <summary>
    /// Thiết lập phương thức flash
    /// </summary>
    public void SetFlashMethod(FlashMethod method)
    {
        flashMethod = method;
    }
    
    /// <summary>
    /// Lấy thời gian flash hiện tại
    /// </summary>
    public float GetFlashDuration()
    {
        return flashDuration;
    }
    
    private void OnDestroy()
    {
        // Cleanup khi destroy
        StopFlash();
    }
}

