using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Quản lý việc lựa chọn và nâng cấp kỹ năng khi người chơi lên cấp.
/// </summary>
public class SkillSelectionManager : MonoBehaviour
{
    [System.Serializable]
    public class Skill
    {
        public string id; // Ví dụ: "FlameAttack", "HPUp"
        public GameObject button; // Button tương ứng trong UI
        public int maxLevel = 5;
        [HideInInspector] public int currentLevel = 0;

        public bool IsMaxLevel() => currentLevel >= maxLevel;
    }

    [Header("UI References")]
    [SerializeField] private GameObject skillSelectionPanel; // Panel chính chứa các button
    [SerializeField] private Transform skillButtonsContainer; // Container để xáo trộn vị trí

    [Header("Skill Configuration")]
    [SerializeField] private List<Skill> allSkills = new List<Skill>();

    [Header("System References")]
    [SerializeField] private PlayerLevelSystem playerLevelSystem; // Tham chiếu đến hệ thống level

    private Dictionary<string, Skill> _skillMap;

    void Awake()
    {
        // Tạo một dictionary để truy cập skill nhanh hơn bằng ID
        _skillMap = new Dictionary<string, Skill>();
        foreach (var skill in allSkills)
        {
            // Ép max level không vượt quá 4 (yêu cầu gameplay hiện tại)
            if (skill.maxLevel > 4) skill.maxLevel = 4;
            // Đảm bảo currentLevel không vượt maxLevel
            skill.currentLevel = Mathf.Clamp(skill.currentLevel, 0, skill.maxLevel);

            if (!_skillMap.ContainsKey(skill.id))
            {
                _skillMap.Add(skill.id, skill);
            }
        }
    }

    void Start()
    {
        // Ẩn panel và tất cả các button khi bắt đầu game
        DeactivateAllSkillButtons();
        skillSelectionPanel.SetActive(false);

        // Tự động tìm PlayerLevelSystem nếu chưa được gán
        if (playerLevelSystem == null)
        {
            playerLevelSystem = FindObjectOfType<PlayerLevelSystem>();
        }

        // Đăng ký sự kiện OnLevelUp từ PlayerLevelSystem
        if (playerLevelSystem != null)
        {
            playerLevelSystem.OnLevelUp += HandlePlayerLevelUp;
        }
        else
        {
            Debug.LogError("PlayerLevelSystem reference could not be found in the scene!");
        }
    }

    /// <summary>
    /// Được gọi khi người chơi lên cấp. Chịu trách nhiệm hiển thị các lựa chọn kỹ năng.
    /// </summary>
    public void HandlePlayerLevelUp(int newLevel)
    {
        // Nếu tất cả skill đã max thì KHÔNG bật panel
        if (AreAllSkillsMaxed()) return;

        // 1. Tạo một "pool" chứa các skill chưa đạt cấp tối đa.
        List<Skill> availableSkills = allSkills.Where(skill => !skill.IsMaxLevel()).ToList();

        // 2. Nếu không còn skill nào để nâng cấp, không mở UI.
        if (availableSkills.Count == 0) return;

        // 3. Xác định số lượng skill sẽ hiển thị (tối đa 3).
        int numberOfSkillsToOffer = Mathf.Min(availableSkills.Count, 3);
        
        // 4. Chọn ngẫu nhiên các skill từ pool.
        System.Random rng = new System.Random();
        List<Skill> skillsToOffer = availableSkills.OrderBy(s => rng.Next()).Take(numberOfSkillsToOffer).ToList();

        // 5. Hiển thị các skill đã chọn và xáo trộn vị trí của chúng.
        DisplaySkills(skillsToOffer);
    }

    /// <summary>
    /// Trả về true nếu tất cả skill trong danh sách đều đã đạt maxLevel
    /// </summary>
    public bool AreAllSkillsMaxed()
    {
        if (allSkills == null || allSkills.Count == 0) return false;
        for (int i = 0; i < allSkills.Count; i++)
        {
            var s = allSkills[i];
            // Phòng trường hợp cấu hình sai maxLevel > 4, đã clamp ở Awake nhưng vẫn kiểm tra cứng thêm 4
            int maxLvl = Mathf.Min(s.maxLevel, 4);
            if (s.currentLevel < maxLvl) return false;
        }
        return true;
    }

    private void DisplaySkills(List<Skill> skillsToDisplay)
    {
        // Không hiển thị nếu không còn skill hợp lệ
        if (skillsToDisplay == null || skillsToDisplay.Count == 0) return;

        // Tạm dừng game để người chơi lựa chọn
        Time.timeScale = 0f;

        // Ẩn tất cả các button để reset trạng thái
        DeactivateAllSkillButtons();

        // Kích hoạt các button của những skill được chọn
        foreach (var skill in skillsToDisplay)
        {
            if (skill.IsMaxLevel()) continue; // chặn đề phòng
            skill.button.SetActive(true);
        }

        // Xáo trộn vị trí các button đang active trong container
        ShuffleActiveButtons();

        // Hiển thị panel lựa chọn skill
        skillSelectionPanel.SetActive(true);
    }

    /// <summary>
    /// Được gọi từ OnClick event của các button skill trong UI.
    /// </summary>
    public void SelectSkillAndUpgrade(string skillId)
    {
        if (_skillMap.TryGetValue(skillId, out Skill selectedSkill))
        {
            if (!selectedSkill.IsMaxLevel())
            {
                selectedSkill.currentLevel++;
                Debug.Log($"Đã nâng cấp skill '{selectedSkill.id}' lên cấp {selectedSkill.currentLevel}");
            }
        }

        ClosePanelResumeGame();
    }

    // Safe API: tìm skill theo button để tránh sai ID do đặt khác nhau trong Inspector
    public void SelectSkillAndUpgradeByButton(GameObject buttonGO)
    {
        Skill found = null;
        foreach (var s in allSkills)
        {
            if (s.button == null) continue;
            if (buttonGO == s.button || buttonGO.transform.IsChildOf(s.button.transform))
            {
                found = s; break;
            }
        }

        if (found != null)
        {
            if (!found.IsMaxLevel())
            {
                found.currentLevel++;
                Debug.Log($"Đã nâng cấp skill '{found.id}' lên cấp {found.currentLevel} (by Button)");
            }
        }
        else
        {
            Debug.LogWarning("[SkillSelectionManager] Không tìm thấy Skill tương ứng với button được nhấn. Hãy kiểm tra lại cấu hình allSkills[].button");
        }

        ClosePanelResumeGame();
    }

    private void ClosePanelResumeGame()
    {
        // Ẩn panel và tiếp tục game
        if (skillSelectionPanel != null) skillSelectionPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    private void DeactivateAllSkillButtons()
    {
        foreach (var skill in allSkills)
        {
            if (skill.button != null)
            {
                skill.button.SetActive(false);
            }
        }
    }

    private void ShuffleActiveButtons()
    {
        if (skillButtonsContainer == null) return;

        List<Transform> activeButtons = new List<Transform>();
        foreach (Transform child in skillButtonsContainer)
        {
            if (child.gameObject.activeSelf)
            {
                activeButtons.Add(child);
            }
        }

        // Xáo trộn danh sách (Fisher-Yates shuffle)
        System.Random rng = new System.Random();
        int n = activeButtons.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (activeButtons[k], activeButtons[n]) = (activeButtons[n], activeButtons[k]); // Hoán vị
        }

        // Áp dụng thứ tự mới cho các button trong hierarchy để thay đổi vị trí hiển thị
        for (int i = 0; i < activeButtons.Count; i++)
        {
            activeButtons[i].SetSiblingIndex(i);
        }
    }

    void OnDestroy()
    {
        if (playerLevelSystem != null)
        {
            playerLevelSystem.OnLevelUp -= HandlePlayerLevelUp;
        }
    }
}
