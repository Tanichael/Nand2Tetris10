namespace FrontCompiler;

public static class StateHelper
{
    public static bool IsSymbol(char c)
    {
        switch (c)
        {
            case '{':
            case '}':
            case '(':
            case ')':
            case '[':
            case ']':
            case '.':
            case ',':
            case ';':
            case '+':
            case '-':
            case '*':
            case '/':
            case '&':
            case '|':
            case '<':
            case '>':
            case '=':
            case '~':
                return true;
            default:
                return false;
        }
    }

    // 英数字 + _ であれば trueを返す
    public static bool IsNormal(char c)
    {
        if (c >= 'a' && c <= 'z')
        {
            return true;
        }

        if (c >= 'A' && c <= 'Z')
        {
            return true;
        }

        if (c == '_')
        {
            return true;
        }

        if (c >= '0' && c <= '9')
        {
            return true;
        }

        return false;
    }

    public static bool IsKeyWord(string token)
    {
        switch (token)
        {
            case "class":
            case "constructor":
            case "function":
            case "method":
            case "field":
            case "static":
            case "var":
            case "int":
            case "char":
            case "boolean":
            case "void":
            case "true":
            case "false":
            case "null":
            case "this":
            case "let":
            case "do":
            case "if":
            case "else":
            case "while":
            case "return":
                return true;
        }

        return false;
    }

    public static KeyWord GetKeyWord(string token)
    {
        switch (token)
        {
            case "class":
                return FrontCompiler.KeyWord.CLASS;
            case "constructor":
                return FrontCompiler.KeyWord.CONSTRUCTOR;
            case "function":
                return FrontCompiler.KeyWord.FUNCTION;
            case "method":
                return FrontCompiler.KeyWord.METHOD;
            case "field":
                return FrontCompiler.KeyWord.FIELD;
            case "static":
                return FrontCompiler.KeyWord.STATIC;
            case "var":
                return FrontCompiler.KeyWord.VAR;
            case "int":
                return FrontCompiler.KeyWord.INT;
            case "char":
                return FrontCompiler.KeyWord.CHAR;
            case "boolean":
                return FrontCompiler.KeyWord.BOOLEAN;
            case "void":
                return FrontCompiler.KeyWord.VOID;
            case "true":
                return FrontCompiler.KeyWord.TRUE;
            case "false":
                return FrontCompiler.KeyWord.FALSE;
            case "null":
                return FrontCompiler.KeyWord.NULL;
            case "this":
                return FrontCompiler.KeyWord.THIS;
            case "let":
                return FrontCompiler.KeyWord.LET;
            case "do":
                return FrontCompiler.KeyWord.DO;
            case "if":
                return FrontCompiler.KeyWord.IF;
            case "else":
                return FrontCompiler.KeyWord.ELSE;
            case "while":
                return FrontCompiler.KeyWord.WHILE;
            case "return":
                return FrontCompiler.KeyWord.RETURN;
        }
        return FrontCompiler.KeyWord.CLASS;
    }
    
    public static string AnalyzeKeyword(KeyWord keyword)
    {
        switch (keyword)
        {
            case KeyWord.CLASS:
                return "class";
            case KeyWord.METHOD:
                return "method";
            case KeyWord.FUNCTION:
                return "function";
            case KeyWord.CONSTRUCTOR:
                return "constructor";
            case KeyWord.INT:
                return "int";
            case KeyWord.BOOLEAN:
                return "boolean";
            case KeyWord.CHAR:
                return "char";
            case KeyWord.VOID:
                return "void";
            case KeyWord.VAR:
                return "var";
            case KeyWord.STATIC:
                return "static";
            case KeyWord.FIELD:
                return "field";
            case KeyWord.LET:
                return "let";
            case KeyWord.DO:
                return "do";
            case KeyWord.IF:
                return "if";
            case KeyWord.ELSE:
                return "else";
            case KeyWord.WHILE:
                return "while";
            case KeyWord.RETURN:
                return "return";
            case KeyWord.TRUE:
                return "true";
            case KeyWord.FALSE:
                return "false";
            case KeyWord.NULL:
                return "null";
            case KeyWord.THIS:
                return "this";
        }

        return "";
    }
}