using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quản lý UI hiển thị Level và EXP
/// - Hiển thị Level hiện tại (Text: "Lv 5")
/// - Hiển thị EXP Bar (Slider với fill amount)
/// - Đơn giản, không có animation phức tạp
/// </summary>
public class ExpUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI levelText; // Text hiển thị Level (VD: "Lv 5")
    [SerializeField] private Slider expSlider; // Slider cho EXP bar
    [SerializeField] private Image expFillImage; // Fill image (nếu dùng Image thay vì Slider)
    
    [Header("Settings")]
    [SerializeField] private bool useSmoothFill = true; // Smooth animation cho fill bar
    [SerializeField] private float fillSpeed = 2f; // Tốc độ fill smooth
    [SerializeField] private Color expBarColor = Color.cyan; // Màu EXP bar
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    // References
    private PlayerLevelSystem _playerLevelSystem;
    
    // State
    private float _targetFillAmount = 0f;
    private float _currentFillAmount = 0f;
    
    private void Awake()
    {
        // Tự động tìm UI components nếu chưa gán
        if (levelText == null)
        {
            levelText = transform.Find("Level Text")?.GetComponent<TextMeshProUGUI>();
        }
        
        if (expSlider == null)
        {
            expSlider = transform.Find("EXP Slider")?.GetComponent<Slider>();
        }
        
        if (expFillImage == null && expSlider != null)
        {
            expFillImage = expSlider.fillRect?.GetComponent<Image>();
        }
    }
    
    private void Start()
    {
        // Tìm PlayerLevelSystem
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _playerLevelSystem = playerObj.GetComponent<PlayerLevelSystem>();
            
            if (_playerLevelSystem != null)
            {
                // Subscribe vào events
                _playerLevelSystem.OnLevelUp += OnPlayerLevelUp;
                _playerLevelSystem.OnExpGained += OnPlayerExpGained;
                _playerLevelSystem.OnLevelChanged += OnPlayerLevelChanged;
                
                // Cập nhật UI ban đầu
                UpdateUI();
            }
            else
            {
                Debug.LogError("[ExpUIManager] Không tìm thấy PlayerLevelSystem trên Player!");
            }
        }
        else
        {
            Debug.LogError("[ExpUIManager] Không tìm thấy Player! Đảm bảo Player có tag 'Player'");
        }
        
        // Set màu mặc định cho EXP bar
        if (expFillImage != null)
        {
            expFillImage.color = expBarColor;
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe events
        if (_playerLevelSystem != null)
        {
            _playerLevelSystem.OnLevelUp -= OnPlayerLevelUp;
            _playerLevelSystem.OnExpGained -= OnPlayerExpGained;
            _playerLevelSystem.OnLevelChanged -= OnPlayerLevelChanged;
        }
    }
    
    private void Update()
    {
        // Smooth fill animation
        if (useSmoothFill && Mathf.Abs(_currentFillAmount - _targetFillAmount) > 0.001f)
        {
            _currentFillAmount = Mathf.Lerp(_currentFillAmount, _targetFillAmount, fillSpeed * Time.deltaTime);
            
            if (expSlider != null)
            {
                expSlider.value = _currentFillAmount;
            }
            else if (expFillImage != null)
            {
                expFillImage.fillAmount = _currentFillAmount;
            }
        }
    }
    
    /// <summary>
    /// Callback khi Player lên level
    /// </summary>
    private void OnPlayerLevelUp(int newLevel)
    {
        if (showDebugInfo)
        {
            Debug.Log($"[ExpUIManager] Player lên Level {newLevel}!");
        }
        
        // Cập nhật UI
        UpdateUI();
    }
    
    /// <summary>
    /// Callback khi Player nhận EXP
    /// </summary>
    private void OnPlayerExpGained(float currentExp, float requiredExp)
    {
        // Cập nhật fill amount
        _targetFillAmount = currentExp / requiredExp;
        
        if (!useSmoothFill)
        {
            _currentFillAmount = _targetFillAmount;
            
            if (expSlider != null)
            {
                expSlider.value = _currentFillAmount;
            }
            else if (expFillImage != null)
            {
                expFillImage.fillAmount = _currentFillAmount;
            }
        }
    }
    
    /// <summary>
    /// Callback khi Level hoặc EXP thay đổi
    /// </summary>
    private void OnPlayerLevelChanged(int level, float currentExp, float requiredExp)
    {
        UpdateUI();
    }
    
    /// <summary>
    /// Cập nhật toàn bộ UI
    /// </summary>
    private void UpdateUI()
    {
        if (_playerLevelSystem == null) return;
        
        // Cập nhật Level text
        UpdateLevelText(_playerLevelSystem.CurrentLevel);
        
        // Cập nhật EXP bar
        float progress = _playerLevelSystem.ExpProgress;
        _targetFillAmount = progress;
        
        if (!useSmoothFill)
        {
            _currentFillAmount = progress;
            
            if (expSlider != null)
            {
                expSlider.value = progress;
            }
            else if (expFillImage != null)
            {
                expFillImage.fillAmount = progress;
            }
        }
    }
    
    /// <summary>
    /// Cập nhật text Level
    /// </summary>
    private void UpdateLevelText(int level)
    {
        if (levelText != null)
        {
            levelText.text = $"Lv {level}";
        }
    }
    
    /// <summary>
    /// Set màu EXP bar (có thể dùng để thay đổi màu theo level)
    /// </summary>
    public void SetExpBarColor(Color color)
    {
        expBarColor = color;
        if (expFillImage != null)
        {
            expFillImage.color = color;
        }
    }
}

