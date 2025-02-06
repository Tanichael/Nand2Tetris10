namespace FrontCompiler;

public interface IState
{
    public void Process(char c);
    public bool IsStart();
    public bool IsEnd();
    public bool IsComment();
}