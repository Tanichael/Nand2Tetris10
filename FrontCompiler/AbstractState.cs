namespace FrontCompiler;

public class AbstractState: IState
{
    protected StateMachine m_StateMachine;

    public AbstractState(StateMachine machine)
    {
        m_StateMachine = machine;
    }

    public virtual bool IsStart()
    {
        return false;
    }
    
    public virtual void Process(char c)
    {
    }

    public virtual bool IsEnd()
    {
        return false;
    }

    public virtual bool IsComment()
    {
        return false;
    }
}
