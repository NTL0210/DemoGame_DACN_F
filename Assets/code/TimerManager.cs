using UnityEngine;
using TMPro;

public class TimerManager : MonoBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private float maxTimeInMinutes = 20f; // Thời gian tối đa là 20 phút
    [SerializeField] private bool autoStart = true; // Tự động bắt đầu khi game start
    [SerializeField] private bool displayAsCountdown = true; // Hiển thị kiểu đếm ngược 20:00 -> 00:00
    
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timeText; // Tham chiếu đến TextMeshPro component
    
    [Header("Timer State")]
    [SerializeField] private bool isRunning = false;
    [SerializeField] private float currentTimeInSeconds = 0f;
    
    // Events
    public System.Action OnTimerComplete; // Event khi timer đạt thời gian tối đa
    public System.Action<float> OnTimeUpdate; // Event khi thời gian cập nhật (truyền thời gian hiện tại)
    
    private void Start()
    {
        // Tự động tìm TextMeshPro component nếu chưa được gán
        if (timeText == null)
        {
            timeText = GetComponentInChildren<TextMeshProUGUI>();
        }
        
        // Khởi tạo hiển thị thời gian
        UpdateTimeDisplay();
        
        // Tự động bắt đầu timer nếu được cấu hình
        if (autoStart)
        {
            StartTimer();
        }
    }
    
    private void Update()
    {
        if (isRunning)
        {
            // Tăng thời gian
            currentTimeInSeconds += Time.deltaTime;
            
            // Kiểm tra nếu đã đạt thời gian tối đa
            if (currentTimeInSeconds >= maxTimeInMinutes * 60f)
            {
                currentTimeInSeconds = maxTimeInMinutes * 60f;
                StopTimer();
                OnTimerComplete?.Invoke();
            }
            
            // Cập nhật hiển thị và gọi event
            UpdateTimeDisplay();
            OnTimeUpdate?.Invoke(currentTimeInSeconds);
        }
    }
    
    /// <summary>
    /// Bắt đầu timer
    /// </summary>
    public void StartTimer()
    {
        isRunning = true;
    }
    
    /// <summary>
    /// Dừng timer
    /// </summary>
    public void StopTimer()
    {
        isRunning = false;
    }
    
    /// <summary>
    /// Tạm dừng/tiếp tục timer
    /// </summary>
    public void ToggleTimer()
    {
        isRunning = !isRunning;
    }
    
    /// <summary>
    /// Reset timer về 00:00
    /// </summary>
    public void ResetTimer()
    {
        currentTimeInSeconds = 0f;
        isRunning = false;
        UpdateTimeDisplay();
    }
    
    /// <summary>
    /// Thiết lập thời gian cụ thể (tính bằng giây)
    /// </summary>
    /// <param name="timeInSeconds">Thời gian tính bằng giây</param>
    public void SetTime(float timeInSeconds)
    {
        currentTimeInSeconds = Mathf.Clamp(timeInSeconds, 0f, maxTimeInMinutes * 60f);
        UpdateTimeDisplay();
    }
    
    /// <summary>
    /// Lấy thời gian hiện tại tính bằng giây
    /// </summary>
    /// <returns>Thời gian hiện tại (giây)</returns>
    public float GetCurrentTimeInSeconds()
    {
        return currentTimeInSeconds;
    }
    
    /// <summary>
    /// Lấy thời gian hiện tại tính bằng phút
    /// </summary>
    /// <returns>Thời gian hiện tại (phút)</returns>
    public float GetCurrentTimeInMinutes()
    {
        return currentTimeInSeconds / 60f;
    }
    
    /// <summary>
    /// Kiểm tra timer có đang chạy không
    /// </summary>
    /// <returns>True nếu timer đang chạy</returns>
    public bool IsTimerRunning()
    {
        return isRunning;
    }
    
    /// <summary>
    /// Kiểm tra timer đã hoàn thành chưa
    /// </summary>
    /// <returns>True nếu timer đã đạt thời gian tối đa</returns>
    public bool IsTimerComplete()
    {
        return currentTimeInSeconds >= maxTimeInMinutes * 60f;
    }
    
    /// <summary>
    /// Cập nhật hiển thị thời gian trên UI
    /// </summary>
    private void UpdateTimeDisplay()
    {
        if (timeText != null)
        {
            float totalSeconds = maxTimeInMinutes * 60f;
            float displaySeconds = displayAsCountdown
                ? Mathf.Max(0f, totalSeconds - currentTimeInSeconds)
                : Mathf.Clamp(currentTimeInSeconds, 0f, totalSeconds);
            
            int minutes = Mathf.FloorToInt(displaySeconds / 60f);
            int seconds = Mathf.FloorToInt(displaySeconds % 60f);
            
            // Format: MM:SS (ví dụ: 05:30, 20:00)
            timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
    
    /// <summary>
    /// Thiết lập thời gian tối đa (tính bằng phút)
    /// </summary>
    /// <param name="maxMinutes">Thời gian tối đa tính bằng phút</param>
    public void SetMaxTime(float maxMinutes)
    {
        maxTimeInMinutes = maxMinutes;
    }
    
    /// <summary>
    /// Lấy thời gian tối đa (tính bằng phút)
    /// </summary>
    /// <returns>Thời gian tối đa tính bằng phút</returns>
    public float GetMaxTimeInMinutes()
    {
        return maxTimeInMinutes;
    }

    /// <summary>
    /// Lấy thời gian còn lại (giây) khi hiển thị đếm ngược
    /// </summary>
    public float GetRemainingTimeInSeconds()
    {
        return Mathf.Max(0f, maxTimeInMinutes * 60f - currentTimeInSeconds);
    }
}
