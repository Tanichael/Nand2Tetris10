namespace FrontCompiler;

public class StartState: AbstractState
{
    public StartState(StateMachine stateMachine): base(stateMachine)
    {
    }

    public override bool IsStart()
    {
        return true;
    }
    
    public override void Process(char c)
    {
        // シンボルで確定の場合
        if (StateHelper.IsSymbol(c) && c != '/')
        {
            m_StateMachine.ChangeState(new SymbolState(m_StateMachine));
        }
        else if (c == '/')
        {
            m_StateMachine.ChangeState(new CommentPotentState(m_StateMachine));
        }
        else if (c == '\"')
        {
            m_StateMachine.ChangeState(new StringConstState(m_StateMachine));
        }
        else if (StateHelper.IsNormal(c))
        {
            m_StateMachine.ChangeState(new NormalState(m_StateMachine));
        }
    }

    public override bool IsEnd()
    {
        return false;
    }
}
