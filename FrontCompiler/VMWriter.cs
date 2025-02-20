using System.Diagnostics;

namespace FrontCompiler;

public class VMWriter
{
    public enum Segment
    {
        CONST,
        ARG,
        LOCAL,
        STATIC,
        THIS,
        THAT,
        POINTER,
        TEMP,
    }

    public enum Command
    {
        ADD,
        SUB,
        NEG,
        EQ,
        GT,
        LT,
        AND,
        OR,
        NOT,
        MULT,
        DIV,
        ELSE,
    }

    private StreamWriter _sw;
    private int _pushedCount = 0;
    public int PushedCount => _pushedCount;

    public VMWriter(string filename)
    {
        FileStream fs = new FileStream(filename, FileMode.Create);
        _sw = new StreamWriter(fs);
    }

    /// <summary>
    /// push コマンドを書く
    /// </summary>
    public void WritePush(Segment segment, int index)
    {
        _pushedCount++;
        _sw.WriteLine($"push {GetSegmentName(segment)} {index}");
        // Console.WriteLine($"pushed: {_pushedCount}");
    }

    /// <summary>
    /// pop コマンドを書く
    /// </summary>
    /// <param name="segment"></param>
    /// <param name="index"></param>
    public void WritePop(Segment segment, int index)
    {
        _pushedCount--;
        _sw.WriteLine($"pop {GetSegmentName(segment)} {index}");
        // Console.WriteLine($"popped: {_pushedCount}");
    }
    
    /// <summary>
    /// 算術コマンドを書く
    /// </summary>
    /// <param name="command"></param>
    public void WriteArithmetic(Command command)
    {
        switch (command)
        {
            case Command.ADD:
                _pushedCount--;
                _sw.WriteLine("add");
                break;
            case Command.SUB:
                _pushedCount--;
                _sw.WriteLine("sub");
                break;
            case Command.NEG:
                _sw.WriteLine("neg");
                break;
            case Command.EQ:
                _pushedCount--;
                _sw.WriteLine("eq");
                break;
            case Command.GT:
                _pushedCount--;
                _sw.WriteLine("gt");
                break;
            case Command.LT:
                _pushedCount--;
                _sw.WriteLine("lt");
                break;
            case Command.AND:
                _pushedCount--;
                _sw.WriteLine("and");
                break;
            case Command.OR:
                _pushedCount--;
                _sw.WriteLine("or");
                break;
            case Command.NOT:
                _sw.WriteLine("not");
                break;
        }
    }

    public void WriteLabel(string label)
    {
        _sw.WriteLine($"label {label}");
    }

    /// <summary>
    /// gotoコマンドを書く
    /// </summary>
    /// <param name="label"></param>
    public void WriteGoto(string label)
    {
        _sw.WriteLine($"goto {label}");
    }

    /// <summary>
    /// if-gotoコマンドを書く
    /// </summary>
    /// <param name="label"></param>
    public void WriteIf(string label)
    {
        _sw.WriteLine($"if-goto {label}");
    }

    public void WriteCall(string name, int nArgs)
    {
        _pushedCount = _pushedCount - nArgs + 1;
        _sw.WriteLine($"call {name} {nArgs}");
        // Console.WriteLine($"called: {name} {_pushedCount}");
    }

    public void WriteFunction(string name, int nLocals)
    {
        _sw.WriteLine($"function {name} {nLocals}");
    }

    public void WriteReturn()
    {
        _sw.WriteLine("return");
    }

    public void Close()
    {
        _sw.Close();
    }

    public Command GetCommand(string name)
    {
        switch (name)
        {
            case "+":
                return Command.ADD;
            case "-":
                return Command.SUB;
            case "*":
                return Command.MULT;
            case "/":
                return Command.DIV;
            case "&amp;":
                return Command.AND;
            case "|":
                return Command.OR;
            case "&lt;":
                return Command.LT;
            case "&gt;":
                return Command.GT;
            case "=":
                return Command.EQ;
        }

        return Command.ELSE;
    }

    private string GetSegmentName(Segment segment)
    {
        switch (segment)
        {
            case Segment.CONST:
                return "constant";
            case Segment.ARG:
                return "argument";
            case Segment.LOCAL:
                return "local";
            case Segment.STATIC:
                return "static";
            case Segment.THIS:
                return "this";
            case Segment.THAT:
                return "that";
            case Segment.POINTER:
                return "pointer";
            case Segment.TEMP:
                return "temp";
        }

        return "";
    }
}