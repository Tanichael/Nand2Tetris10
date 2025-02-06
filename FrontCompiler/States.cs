namespace FrontCompiler;

public class SymbolState: AbstractState
{
    public SymbolState(StateMachine machine) : base(machine)
    {
    }

    public override void Process(char c)
    {
        m_StateMachine.ChangeState(new WordEndState(m_StateMachine));
    }

    public override bool IsEnd()
    {
        return false;
    }
}

public class NormalState : AbstractState
{
    public NormalState(StateMachine machine) : base(machine)
    {
    }

    public override void Process(char c)
    {
        
        if (c == ' ' || c == '\n')
        {
            // token確定
            m_StateMachine.ChangeState(new WordEndState(m_StateMachine));
        }

        if (StateHelper.IsSymbol(c))
        {
            // token確定
            m_StateMachine.ChangeState(new WordEndState(m_StateMachine));
        }
    }

    public override bool IsEnd()
    {
        return false;
    }
}

public class WordEndState : AbstractState
{
    public WordEndState(StateMachine machine) : base(machine)
    {
    }

    public override void Process(char c)
    {
    }

    public override bool IsEnd()
    {
        return true;
    }
}

public class CommentPotentState : AbstractState
{
    public CommentPotentState(StateMachine machine) : base(machine)
    {
    }
    
    public override void Process(char c)
    {
        if (c == '/')
        {
            m_StateMachine.ChangeState(new CommentInsideState(m_StateMachine));
        }
        else if(c == '*')
        {
            m_StateMachine.ChangeState(new MultiCommentInsideState(m_StateMachine));
        }
        else
        {
            m_StateMachine.ChangeState(new WordEndState(m_StateMachine));
        }
    }

    public override bool IsEnd()
    {
        return false;
    }
}

public class CommentInsideState : AbstractState
{
    public CommentInsideState(StateMachine machine) : base(machine)
    {
    }

    public override void Process(char c)
    {
        if (c == '\n')
        {
            m_StateMachine.ChangeState(new CommentEndState(m_StateMachine));
        }
    }
    
    public override bool IsComment()
    {
        return true;
    }
}

public class CommentEndState : AbstractState
{
    public CommentEndState(StateMachine machine) : base(machine)
    {
    }

    public override void Process(char c)
    {
        m_StateMachine.ChangeState(new AllCommentEndState(m_StateMachine));
    }

    public override bool IsComment()
    {
        return true;
    }
}

public class MultiCommentInsideState : AbstractState
{
    public MultiCommentInsideState(StateMachine machine) : base(machine)
    {
    }

    public override void Process(char c)
    {
        if (c == '*')
        {
            m_StateMachine.ChangeState(new MultiCommentEndPotentState(m_StateMachine));
        }
    }
    
    public override bool IsComment()
    {
        return true;
    }
}

public class MultiCommentEndPotentState : AbstractState
{
    public MultiCommentEndPotentState(StateMachine machine) : base(machine)
    {
    }
    
    public override void Process(char c)
    {
        if (c != '/')
        {
            m_StateMachine.ChangeState(new MultiCommentInsideState(m_StateMachine));
        }
        else
        {
            m_StateMachine.ChangeState(new MultiCommentEndState(m_StateMachine));
        }
    }
    
    public override bool IsComment()
    {
        return true;
    }
}

public class MultiCommentEndState : AbstractState
{
    public MultiCommentEndState(StateMachine machine) : base(machine)
    {
    }

    public override void Process(char c)
    {
        m_StateMachine.ChangeState(new AllCommentEndState(m_StateMachine));
    }
    
    public override bool IsComment()
    {
        return true;
    }
}

public class AllCommentEndState : AbstractState
{
    public AllCommentEndState(StateMachine machine) : base(machine)
    {
    }
    
    public override bool IsComment()
    {
        return true;
    }

    public override bool IsEnd()
    {
        return true;
    }
}

public class StringConstState : AbstractState
{
    public StringConstState(StateMachine machine) : base(machine)
    {
    }

    public override void Process(char c)
    {
        if (c == '\"')
        {
            m_StateMachine.ChangeState(new StringConstEndState(m_StateMachine));
        }
    }
}

public class StringConstEndState : AbstractState
{
    public StringConstEndState(StateMachine machine) : base(machine)
    {
    }

    public override void Process(char c)
    {
        m_StateMachine.ChangeState(new WordEndState(m_StateMachine));
    }
}
