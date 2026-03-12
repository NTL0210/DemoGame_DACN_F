using UnityEngine;

/// <summary>
/// Trạng thái kết thúc game để tránh hiển thị chồng GameOver / Victory.
/// Đảm bảo chỉ một UI end-game được hiển thị, đồng thời pause game (Time.timeScale = 0).
/// </summary>
public static class EndGameUIState
{
    private static bool _ended = false;
    private static string _owner = null;

    /// <summary>
    /// Thử đánh dấu trạng thái end-game. Trả về true nếu thành công (chưa ai chiếm),
    /// false nếu đã có UI end-game khác đang hiển thị.
    /// </summary>
    public static bool TrySet(string owner)
    {
        if (_ended) return false;
        _ended = true;
        _owner = owner;
        Time.timeScale = 0f; // pause ngay khi end
        return true;
    }

    /// <summary>
    /// Reset trạng thái end-game, đồng thời khôi phục timeScale = 1.
    /// Gọi khi bấm play again / main menu.
    /// </summary>
    public static void Reset()
    {
        _ended = false;
        _owner = null;
        Time.timeScale = 1f;
    }

    public static bool IsEnded => _ended;
    public static string Owner => _owner;
}

