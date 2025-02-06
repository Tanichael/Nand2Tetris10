namespace FrontCompiler;

public class StateMachine
{
    private IState m_CurrentState;

    public StateMachine()
    {
        m_CurrentState = new StartState(this);
    }
    
    /// <summary>
    /// 文字を状態に渡す
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    public void Process(char c)
    {
        m_CurrentState.Process(c);
    }

    public bool IsStart()
    {
        return m_CurrentState.IsStart();
    }

    public bool IsEnd()
    {
        return m_CurrentState.IsEnd();
    }

    public bool IsComment()
    {
        return m_CurrentState.IsComment();
    }

    public void ChangeState(IState nextState)
    {
        m_CurrentState = nextState;
        // Console.WriteLine($"next: {m_CurrentState.GetType().Name}");
    }

    public string GetCurrentName()
    {
        return m_CurrentState.GetType().Name;
    }
}