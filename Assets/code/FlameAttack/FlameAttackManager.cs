using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Flame Attack Manager - Quản lý animation Flame Attack với 2 Animator riêng biệt
/// </summary>
public class FlameAttackManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform pointer;
    [SerializeField] private IsFlameLv1 flameLv1;
    [SerializeField] private IsFlameMax flameMax;
    [SerializeField] private Animator animatorLv1;
    [SerializeField] private Animator animatorMax;
    
    // [Header("Input")] - Đã xóa: Không còn nâng cấp bằng phím M
    // [SerializeField] private InputActionReference levelUpAction;
    
    [Header("Timing (seconds)")]
    [SerializeField] private float initialDelay = 1f; // Delay trước khi spawn lần đầu tiên (1 giây)
    [SerializeField] private float intervalLv1 = 3.5f;
    [SerializeField] private float intervalLv2 = 2.5f;
    [SerializeField] private float intervalLv3 = 1.2f;
    
    [Header("Rotation Settings")]
    [SerializeField] private float[] flameRotationsZ = { 0f, -45f, -90f, 45f, -180f, 135f, 90f, -135f };
    
    [Header("Debug")]
    [SerializeField] private int level = 1;
    [SerializeField] private string currentAnimation = "None";
    
    private float nextPlayTime;
    private bool isPlaying = false;
    private float activeUntilTime = 0f;
    private readonly List<GameObject> lv1Variants = new List<GameObject>();
    private bool hasSpawnedFirstTime = false; // Track xem đã spawn lần đầu chưa
    private HashSet<EnemyController> enemiesHitThisSwing = new HashSet<EnemyController>();
    
    private void Awake()
    {
        // Khởi tạo theo thứ tự: components -> variants -> visuals -> state
        InitializeComponents();
        CollectLv1Variants();
        ApplyLevelVisuals();
        ResetToIdle();
        
        // Thiết lập thời gian spawn đầu tiên - delay 1 giây sau khi game bắt đầu
        nextPlayTime = Time.time + initialDelay;
        hasSpawnedFirstTime = false;
    }
    
    // OnEnable và OnDisable đã xóa - không còn sử dụng Input Action cho phím M
    // private void OnEnable()
    // {
    //     if (levelUpAction?.action != null)
    //         levelUpAction.action.Enable();
    // }
    // 
    // private void OnDisable()
    // {
    //     if (levelUpAction?.action != null)
    //         levelUpAction.action.Disable();
    // }
    
    private void Update()
    {
        CheckAnimationEnd();
        // HandleInput(); - Đã xóa: Không còn nhận input phím M
    }
    
    private void FixedUpdate()
    {
        // Kiểm tra thời gian spawn
        if (Time.time >= nextPlayTime && !isPlaying)
        {
            PlayAnimation();
            
            // Đánh dấu đã spawn lần đầu
            if (!hasSpawnedFirstTime)
            {
                hasSpawnedFirstTime = true;
                
                if (Application.isEditor || Debug.isDebugBuild)
                {
                    Debug.Log($"FlameAttackManager: First flame attack spawned after {initialDelay} seconds");
                }
            }
        }
    }
    
    private void InitializeComponents()
    {
        // Tìm Flame Lv1 components
        if (flameLv1 == null)
        {
            var t = transform.Find("Flame Attack Lv1");
            if (t != null)
            {
                flameLv1 = t.GetComponent<IsFlameLv1>();
                if (flameLv1 == null)
                {
                    Debug.LogWarning("FlameAttackManager: Không tìm thấy IsFlameLv1 component trên 'Flame Attack Lv1'");
                }
            }
        }
        
        // Tìm Flame Max components
        if (flameMax == null)
        {
            var t = transform.Find("Flame Attack Max");
            if (t != null)
            {
                flameMax = t.GetComponent<IsFlameMax>();
                if (flameMax == null)
                {
                    Debug.LogWarning("FlameAttackManager: Không tìm thấy IsFlameMax component trên 'Flame Attack Max'");
                }
            }
        }
        
        // Tìm Animator Lv1
        if (animatorLv1 == null)
        {
            var t = transform.Find("Flame Attack Lv1");
            if (t != null) animatorLv1 = t.GetComponent<Animator>();
        }
        
        // Tìm Animator Max
        if (animatorMax == null)
        {
            var t = transform.Find("Flame Attack Max");
            if (t != null) animatorMax = t.GetComponent<Animator>();
        }
        
        // Tìm Pointer - thử nhiều đường dẫn phổ biến
        if (pointer == null)
        {
            var player = transform.root;
            pointer = player.Find("Pointer/pointer 1") 
                ?? player.Find("Pointer/Pointer") 
                ?? player.Find("pointer 1")
                ?? player.Find("Core/Pointer/pointer 1");
                
            if (pointer == null)
            {
                Debug.LogWarning("FlameAttackManager: Không tìm thấy Pointer. Flame attack sẽ không xoay theo hướng pointer.");
            }
        }
    }
    
    private void CollectLv1Variants()
    {
        lv1Variants.Clear();
        var lv1Parent = transform.Find("Flame Attack Lv1");
        if (lv1Parent != null)
        {
            for (int i = 0; i < lv1Parent.childCount; i++)
            {
                var child = lv1Parent.GetChild(i);
                if (child.name.Contains("Flame Attack Lv1"))
                {
                    lv1Variants.Add(child.gameObject);
                }
            }
        }
    }
    
    private void ApplyLevelVisuals()
    {
        HideAllVariants();
        if (level >= 1 && level <= 3 && lv1Variants.Count > 0)
        {
            int variantIndex = Mathf.Clamp(level - 1, 0, lv1Variants.Count - 1);
            lv1Variants[variantIndex].SetActive(true);
        }
    }
    
    private void ResetToIdle()
    {
        isPlaying = false;
        currentAnimation = "None";
        HideAllVariants();
        if (flameLv1 != null) flameLv1.SetVisible(false);
        if (flameMax != null) flameMax.SetVisible(false);
        
        if (animatorLv1?.runtimeAnimatorController != null)
            animatorLv1.SetBool("Lv1", false);
        if (animatorMax?.runtimeAnimatorController != null)
            animatorMax.SetBool("LvMax", false);
    }
    
        public bool CanDamage(EnemyController enemy)
    {
        // Nếu enemy CHƯA có trong danh sách, thì có thể gây sát thương.
        return !enemiesHitThisSwing.Contains(enemy);
    }

    public void RecordHit(EnemyController enemy)
    {
        // Thêm enemy vào danh sách đã bị đánh trong đòn tấn công này.
        enemiesHitThisSwing.Add(enemy);
    }

    private void PlayAnimation()
    {
        if (isPlaying) return;

        // Xóa danh sách kẻ địch đã bị đánh của đòn tấn công trước.
        enemiesHitThisSwing.Clear();
        
        HideAllVariants();
        CapturePointerPose();
        
        isPlaying = true;
        currentAnimation = level == 4 ? "FlameMax" : "FlameLv1";
        
        if (level == 4)
        {
            if (animatorMax?.runtimeAnimatorController != null)
                animatorMax.SetBool("LvMax", true);
            
            if (flameMax != null)
            {
                flameMax.PlayOnce();
                activeUntilTime = Time.time + Mathf.Max(flameMax.ActiveDuration, 0.6f);
            }
        }
        else
        {
            if (animatorLv1?.runtimeAnimatorController != null)
                animatorLv1.SetBool("Lv1", true);
            
            if (flameLv1 != null)
            {
                flameLv1.PlayOnce();
                activeUntilTime = Time.time + Mathf.Max(flameLv1.ActiveDuration, 0.6f);
            }
        }
        
        nextPlayTime = Time.time + GetCurrentInterval();
    }
    
    private void StopAnimation()
    {
        isPlaying = false;
        currentAnimation = "None";
        
        HideAllVariants();
        if (flameLv1 != null) flameLv1.SetVisible(false);
        if (flameMax != null) flameMax.SetVisible(false);
        
        if (animatorLv1?.runtimeAnimatorController != null)
            animatorLv1.SetBool("Lv1", false);
        if (animatorMax?.runtimeAnimatorController != null)
            animatorMax.SetBool("LvMax", false);
    }
    
    private void HideAllVariants()
    {
        foreach (var variant in lv1Variants)
        {
            if (variant != null) variant.SetActive(false);
        }
    }
    
    private void CapturePointerPose()
    {
        if (pointer == null) return;
        
        Vector3 pointerPosition = pointer.position;
        float scaleY = GetScaleYByLevel();
        Vector3 scale = new Vector3(8.4f, scaleY, 1f);
        
        Quaternion pointerRotation = pointer.rotation;
        float pointerZ = pointerRotation.eulerAngles.z;
        
        if (pointerZ > 180f) pointerZ -= 360f;
        
        int elementIndex = GetElementIndexFromPointerZ(pointerZ);
        float finalZ = flameRotationsZ[elementIndex];
        Quaternion finalRotation = Quaternion.Euler(0f, 0f, finalZ);
        
        if (flameLv1 != null)
        {
            flameLv1.transform.position = pointerPosition;
            flameLv1.transform.rotation = finalRotation;
            flameLv1.transform.localScale = scale;
        }
        
        if (flameMax != null)
        {
            flameMax.transform.position = pointerPosition;
            flameMax.transform.rotation = finalRotation;
            flameMax.transform.localScale = scale;
        }
    }
    
    private float GetCurrentInterval()
    {
        return level switch
        {
            1 => intervalLv1,
            2 => intervalLv2,
            3 => intervalLv3,
            4 => intervalLv3,
            _ => intervalLv1
        };
    }
    
    private float GetScaleYByLevel()
    {
        return level switch
        {
            1 => 12.3f,
            2 => 14.3f,
            3 => 16.3f,
            4 => 19.3f,
            _ => 12.3f
        };
    }
    
    private int GetElementIndexFromPointerZ(float pointerZ)
    {
        // Pointer angles: vị trí 1-8 tương ứng với 0°, -45°, -90°, -135°, -180°, 135°, 90°, 45°
        // Flame Attack angles: vị trí 1-8 tương ứng với 0°, -45°, -90°, 45°, -180°, 135°, 90°, -135°
        float[] zAngles = { 0f, -45f, -90f, -135f, -180f, 135f, 90f, 45f };
        
        int closestIndex = 0;
        float minDifference = CalculateAngleDifference(pointerZ, zAngles[0]);
        
        for (int i = 1; i < zAngles.Length; i++)
        {
            float difference = CalculateAngleDifference(pointerZ, zAngles[i]);
            if (difference < minDifference)
            {
                minDifference = difference;
                closestIndex = i;
            }
        }
        
        return closestIndex;
    }
    
    private float CalculateAngleDifference(float angle1, float angle2)
    {
        float diff = Mathf.Abs(angle1 - angle2);
        // Xử lý trường hợp góc âm và dương (ví dụ: 180° và -180°)
        if (diff > 180f)
        {
            diff = 360f - diff;
        }
        return diff;
    }
    
    // HandleInput đã xóa - không còn nâng cấp bằng phím M
    // private void HandleInput()
    // {
    //     bool mKeyPressed = false;
    //     
    //     if (levelUpAction?.action != null)
    //         mKeyPressed = levelUpAction.action.WasPressedThisFrame();
    //     
    //     if (!mKeyPressed && UnityEngine.InputSystem.Keyboard.current != null)
    //         mKeyPressed = UnityEngine.InputSystem.Keyboard.current.mKey.wasPressedThisFrame;
    //     
    //     if (mKeyPressed)
    //         CycleLevel();
    // }
    
    private void CheckAnimationEnd()
    {
        if (!isPlaying) return;
        
        Animator currentAnimator = null;
        if (level == 4 && animatorMax?.runtimeAnimatorController != null)
            currentAnimator = animatorMax;
        else if (level < 4 && animatorLv1?.runtimeAnimatorController != null)
            currentAnimator = animatorLv1;
        
        if (currentAnimator != null)
        {
            try
            {
                AnimatorStateInfo stateInfo = currentAnimator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.normalizedTime >= 1.0f)
                {
                    StopAnimation();
                    return;
                }
            }
            catch
            {
                // Ignore animator errors
            }
        }
        
        if (Time.time >= activeUntilTime)
            StopAnimation();
    }
    
    public void SetLevel(int newLevel)
    {
        if (newLevel < 1 || newLevel > 4) return;
        level = newLevel;
        ApplyLevelVisuals();
        UpdateDamageComponentLevel();
    }
    
    // CycleLevel đã xóa - không còn sử dụng (trước đây dùng cho phím M)
    // public void CycleLevel()
    // {
    //     level = level >= 4 ? 1 : level + 1;
    //     ApplyLevelVisuals();
    //     UpdateDamageComponentLevel();
    // }
    
    /// <summary>
    /// Cập nhật level cho damage component
    /// </summary>
    private void UpdateDamageComponentLevel()
    {
        // Update level cho Flame Lv1
        if (flameLv1 != null)
        {
            flameLv1.SetLevel(level);
        }
        
        // Flame Max luôn là level 4, không cần update
    }
    
    public void ForcePlay() => PlayAnimation();
    public void ForceStop() => StopAnimation();
    
    public bool IsPlaying => isPlaying;
    public int CurrentLevel => level;
    public string CurrentAnimation => currentAnimation;
}