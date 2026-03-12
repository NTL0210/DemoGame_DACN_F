/// <summary>
/// Interface cho tất cả các State của Player
/// </summary>
public interface IPlayerState
{
    void Enter();
    void Update();
    void FixedUpdate();
    void Exit();
}
