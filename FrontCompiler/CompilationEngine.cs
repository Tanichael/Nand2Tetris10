namespace FrontCompiler;

public class CompilationEngine: IDisposable
{
    private StreamWriter _sw;
    
    public CompilationEngine(string inputFile, string outputFile)
    {
        FileStream outputStream = new FileStream(outputFile, FileMode.Create);
        _sw = new StreamWriter(outputStream);
    }

    public void CompileClass()
    {
        _sw.WriteLine("<class>");
        _sw.WriteLine("  <keyword>  class  </keyword>");
        _sw.WriteLine("  <symbol>  {  <symbol>");
        CompileClassVarDec();
        CompileSubroutine();
        _sw.WriteLine("  <symbol>  }  <symbol>");
        _sw.WriteLine("</class>");
    }

    public void CompileClassVarDec()
    {
        _sw.WriteLine();
    }

    public void CompileSubroutine()
    {
        
    }

    public void CompileParameterList()
    {
        
    }

    public void CompileVarDec()
    {
        
    }

    public void CompileStatements()
    {
        
    }

    public void CompileDo()
    {
        
    }

    public void CompileLet()
    {
        
    }

    public void CompileWhile()
    {
        
    }

    public void CompileReturn()
    {
        
    }

    public void CompileIf()
    {
        
    }


    public void CompileExpression()
    {
        
    }

    public void CompileTerm()
    {
        
    }

    public void CompileExpressionList()
    {
        
    }

    public void Dispose()
    {
        _sw.Close();
    }
}
