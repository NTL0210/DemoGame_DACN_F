using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Đảm bảo thứ tự scene theo yêu cầu:
/// - Khi khởi động game: luôn vào MainMenu.
/// - Không để TestLan01 auto-load; nếu có (do mở sẵn trong Editor hay leftover additive) thì unload ngay.
/// - TestLan01 chỉ được load khi người chơi nhấn Play trong MainMenu.
/// Cơ chế: sử dụng RuntimeInitializeOnLoadMethod để ép thứ tự scene khi bắt đầu Play (Editor) hoặc khi build chạy.
/// </summary>
public static class SceneBootstrap
{
    // Đổi tên tại đây nếu bạn đổi tên scene
    private const string MainMenuScene = "MainMenu";
    private const string GameplayScene = "TestLan01";

    // Gọi trước khi scene đầu tiên được load
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ForceMainMenuOnStart()
    {
        if (!Application.isPlaying) return;

        // Nếu scene active hiện tại KHÔNG phải MainMenu, ép load MainMenu ở chế độ Single
        // Hữu ích khi bạn ấn Play từ một scene khác trong Editor
        var active = SceneManager.GetActiveScene();
        if (!active.IsValid() || active.name != MainMenuScene)
        {
            try
            {
                SceneManager.LoadScene(MainMenuScene, LoadSceneMode.Single);
            }
            catch
            {
                Debug.LogError($"SceneBootstrap: Không thể load scene '{MainMenuScene}'. Hãy đảm bảo scene này có trong Build Settings.");
            }
        }
    }

    // Gọi sau khi scene đầu tiên đã load để dọn dẹp mọi leftover
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void UnloadGameplayIfLeftover()
    {
        if (!Application.isPlaying) return;

        // Nếu vì lý do nào đó TestLan01 đang được load additively, unload nó ngay
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (s.IsValid() && s.name == GameplayScene)
            {
                // Nếu lỡ active vào gameplay, quay về MainMenu trước
                if (s == SceneManager.GetActiveScene())
                {
                    try
                    {
                        SceneManager.LoadScene(MainMenuScene, LoadSceneMode.Single);
                    }
                    catch
                    {
                        Debug.LogError($"SceneBootstrap: Không thể load '{MainMenuScene}' khi TestLan01 đang active.");
                    }
                }

                SceneManager.UnloadSceneAsync(s);
                Debug.Log("SceneBootstrap: Unload leftover 'TestLan01' lúc start game.");
            }
        }
    }
}

