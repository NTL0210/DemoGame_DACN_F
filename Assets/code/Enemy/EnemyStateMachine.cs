using UnityEngine;

/// <summary>
/// Interface cho tất cả các Enemy States
/// </summary>
public interface IEnemyState
{
    void Enter();
    void Update();
    void FixedUpdate();
    void Exit();
}

/// <summary>
/// Enemy State Machine Core - Quản lý các trạng thái của Enemy
/// </summary>
public class EnemyStateMachine : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private string currentStateName;
    
    private IEnemyState currentState;
    private IEnemyState previousState;
    
    public IEnemyState CurrentState => currentState;
    public IEnemyState PreviousState => previousState;
    
    public void ChangeState(IEnemyState newState)
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
    
    public bool IsInState<T>() where T : IEnemyState
    {
        return currentState is T;
    }
}
