using UnityEngine;

/// <summary>
/// State Machine Core - Quản lý các trạng thái của Player
/// </summary>
public class StateMachine : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private string currentStateName;
    
    private IPlayerState currentState;
    private IPlayerState previousState;
    
    public IPlayerState CurrentState => currentState;
    public IPlayerState PreviousState => previousState;
    
    public void ChangeState(IPlayerState newState)
    {
        if (currentState != null)
        {
            currentState.Exit();
            previousState = currentState;
        }
        
        currentState = newState;
        currentStateName = currentState.GetType().Name;
        
        if (currentState != null)
        {
            currentState.Enter();
        }
    }
    
    private void Update()
    {
        if (currentState != null)
        {
            currentState.Update();
        }
    }
    
    private void FixedUpdate()
    {
        if (currentState != null)
        {
            currentState.FixedUpdate();
        }
    }
    
    public bool IsInState<T>() where T : IPlayerState
    {
        return currentState is T;
    }
}
