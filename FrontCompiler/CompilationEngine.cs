namespace FrontCompiler;

/// <summary>
/// Compilexxx関数は一律最後にAdvanceして終わることにする
/// 次のTokenを見ないと分岐できないケースがあるため
/// CompileClass 以外のコードでは元々
/// </summary>
public class CompilationEngine: IDisposable
{
    private enum LineType
    {
        Open,
        Both,
        Close,
    }

    private enum TagType
    {
        KeyWord,
        Symbol,
        IntegerConstant,
        StringConstant,
        Identifier,
        Class,
        ClassVarDec,
        Type,
        SubroutineDec,
        ParameterList,
        SubroutineBody,
        VarDec,
        ClassName,
        SubroutineName,
        VarName,
        Statements,
        LetStatement,
        IfStatement,
        WhileStatement,
        DoStatement,
        ReturnStatement,
        Expression,
        Term,
        ExpressionList,
    }
    
    private StreamWriter _sw;
    private JackTokenizer _tokenizer;
    private int _indentLevel;
    private string _tempIndent = "";
    private bool _hasNoToken = false;
    private Dictionary<TagType, string> _tagNames = new Dictionary<TagType, string>();
    
    public CompilationEngine(string inputFile, string outputFile)
    {
        FileStream outputStream = new FileStream(outputFile, FileMode.Create);
        _sw = new StreamWriter(outputStream);
        _tokenizer = new JackTokenizer(inputFile);
        _indentLevel = 0;
        InitTagNames();
        if (!_tokenizer.HasMoreTokens())
        {
            _hasNoToken = true;
        }
        else
        {
            Advance();
        }
    }

    public void CompileClass()
    {
        if (_hasNoToken)
        {
            return;
        }
       
        CompileLine(TagType.Class, null, LineType.Open);
        CompileLine(TagType.KeyWord, _tokenizer.KeyWord(), LineType.Both);
        CompileLine(TagType.Identifier, $"{_tokenizer.Identifier()}", LineType.Both);
        CompileLine(TagType.Symbol, "{", LineType.Both);
        
        while (_tokenizer.TokenType() == TokenType.KEYWORD)
        {
            switch (_tokenizer.KeyWord())
            {
                case KeyWord.STATIC:
                case KeyWord.FIELD:
                    CompileClassVarDec();
                    continue;
            }

            break;
        }

        while (_tokenizer.TokenType() == TokenType.KEYWORD)
        {
            switch (_tokenizer.KeyWord())
            {
                case KeyWord.CONSTRUCTOR:
                case KeyWord.FUNCTION:
                case KeyWord.METHOD:
                    CompileSubroutine();
                    continue;
            }

            break;
        }
        
        CompileLine(TagType.Symbol, "}", LineType.Both);
        CompileLine(TagType.Class, null, LineType.Close);
    }

    public void CompileClassVarDec()
    {
        CompileLine(TagType.ClassVarDec, null, LineType.Open);
        CompileLine(TagType.KeyWord, _tokenizer.KeyWord(), LineType.Both);

        // type
        CompileType();
        
        CompileLine(TagType.Identifier, _tokenizer.Identifier(), LineType.Both);
        
        // 次が,なら変数の宣言が連続する
        while (_tokenizer.TokenType() == TokenType.SYMBOL && _tokenizer.Symbol() == ",")
        {
            CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
        
            CompileLine(TagType.Identifier, _tokenizer.Identifier(), LineType.Both);
        }
        
        CompileLine(TagType.Symbol, ";", LineType.Both);
        CompileLine(TagType.ClassVarDec, null, LineType.Close);
    }

    public void CompileSubroutine()
    {
        CompileLine(TagType.SubroutineDec, null, LineType.Open);
        CompileLine(TagType.KeyWord, _tokenizer.KeyWord(), LineType.Both);

        // ("void" | type)
        CompileType();

        // subroutineName
        CompileLine(TagType.Identifier, _tokenizer.Identifier(), LineType.Both);
        
        // (
        CompileLine(TagType.Symbol, "(", LineType.Both);
        
        // parameterList
        CompileParameterList();
        
        // )
        CompileLine(TagType.Symbol, ")", LineType.Both);
        
        // subroutineBody
        CompileLine(TagType.SubroutineBody, null, LineType.Open);
        CompileLine(TagType.Symbol, "{", LineType.Both);
        while (_tokenizer.TokenType() == TokenType.KEYWORD && _tokenizer.KeyWord() == KeyWord.VAR)
        {
            CompileVarDec();
        }
        // statements
        CompileStatements();
        CompileLine(TagType.Symbol, "}", LineType.Both);
        CompileLine(TagType.SubroutineBody, null, LineType.Close);

        CompileLine(TagType.SubroutineDec, null, LineType.Close);
    }

    /// <summary>
    /// ((type varName) (',', type varName)*)?
    /// </summary>
    public void CompileParameterList()
    {
        CompileLine(TagType.ParameterList, null, LineType.Open);
        
        // typeがあるならコンパイル
        if (_tokenizer.TokenType() == TokenType.KEYWORD)
        {
            CompileLine(TagType.KeyWord, _tokenizer.KeyWord(), LineType.Both);
        }
        else if (_tokenizer.TokenType() == TokenType.IDENTIFIER)
        {
            CompileLine(TagType.Identifier, _tokenizer.Identifier(), LineType.Both);
        }
        else // typeがなければ切り上げて終了
        {
            CompileLine(TagType.ParameterList, null, LineType.Close);
            return;
        }
        
        // varName
        CompileLine(TagType.Identifier, _tokenizer.Identifier(), LineType.Both);

        while (_tokenizer.TokenType() == TokenType.SYMBOL && _tokenizer.Symbol() == ",")
        {
            // ,
            CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
            
            // type
            CompileType();
            
            // varName
            CompileLine(TagType.Identifier, _tokenizer.Identifier(), LineType.Both);
        }
        
        CompileLine(TagType.ParameterList, null, LineType.Close);
    }

    /// <summary>
    /// 'var' type varName (',' varName)* ';'
    /// </summary>
    public void CompileVarDec()
    {
        CompileLine(TagType.VarDec, null, LineType.Open);
        CompileLine(TagType.KeyWord, _tokenizer.KeyWord(), LineType.Both);
        CompileType();
        CompileLine(TagType.Identifier, _tokenizer.Identifier(), LineType.Both);
        while (_tokenizer.TokenType() == TokenType.SYMBOL && _tokenizer.Symbol() == ",")
        {
            CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
            CompileLine(TagType.Identifier, _tokenizer.Identifier(), LineType.Both);
        }
        CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
        CompileLine(TagType.VarDec, null, LineType.Close);
    }

    public void CompileStatements()
    {
        CompileLine(TagType.Statements, null, LineType.Open);
        
        // 終了時は } になるのでKeyWordかどうかのチェックでOK
        while (_tokenizer.TokenType() == TokenType.KEYWORD)
        {
            switch (_tokenizer.KeyWord())
            {
                case KeyWord.LET:
                    CompileLet();
                    continue;
                case KeyWord.IF:
                    CompileIf();
                    continue;
                case KeyWord.WHILE:
                    CompileWhile();
                    continue;
                case KeyWord.DO:
                    CompileDo();
                    continue;
                case KeyWord.RETURN:
                    CompileReturn();
                    continue;
            }

            break;
        }
        CompileLine(TagType.Statements, null, LineType.Close);
    }

    public void CompileDo()
    {
        CompileLine(TagType.DoStatement, null, LineType.Open);
        CompileLine(TagType.KeyWord, _tokenizer.KeyWord(), LineType.Both);
        
        // subroutineCall
        CompileLine(TagType.Identifier, _tokenizer.Identifier(), LineType.Both);
        if (_tokenizer.TokenType() == TokenType.SYMBOL)
        {
            if (_tokenizer.Symbol() == ".")
            {
                CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
                CompileLine(TagType.Identifier, _tokenizer.Identifier(), LineType.Both);
                CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
                CompileExpressionList();
                CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
            }
            else if (_tokenizer.Symbol() == "(")
            {
                CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
                CompileExpressionList();
                CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
            }
        }
        
        CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
        CompileLine(TagType.DoStatement, null, LineType.Close);
    }

    public void CompileLet()
    {
        CompileLine(TagType.LetStatement, null, LineType.Open);
        CompileLine(TagType.KeyWord, _tokenizer.KeyWord(), LineType.Both);
        CompileLine(TagType.Identifier, _tokenizer.Identifier(), LineType.Both);

        if (_tokenizer.TokenType() == TokenType.SYMBOL && _tokenizer.Symbol() == "[")
        {
            // [
            CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
            
            // expression
            CompileExpression();
        
            // ]
            CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
        }
        
        // =
        CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
        
        // expression
        CompileExpression();
        
        // ;
        CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
        CompileLine(TagType.LetStatement, null, LineType.Close);
    }

    public void CompileWhile()
    {
        CompileLine(TagType.WhileStatement, null, LineType.Open);
        CompileLine(TagType.KeyWord, _tokenizer.KeyWord(), LineType.Both);
        CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
        CompileExpression();
        CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
        
        CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
        CompileStatements();
        CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
        
        CompileLine(TagType.WhileStatement, null, LineType.Close);
    }

    public void CompileReturn()
    {
        CompileLine(TagType.ReturnStatement, null, LineType.Open);
        CompileLine(TagType.KeyWord, _tokenizer.KeyWord(), LineType.Both);
        
        // expression?
        if (IsTerm())
        {
            CompileExpression();
        }
        
        CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
        CompileLine(TagType.ReturnStatement, null, LineType.Close);

    }

    public void CompileIf()
    {
        CompileLine(TagType.IfStatement, null, LineType.Open);
        CompileLine(TagType.KeyWord, _tokenizer.KeyWord(), LineType.Both);
        CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
        CompileExpression();
        CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
        
        CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
        CompileStatements();
        CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);

        if (_tokenizer.TokenType() == TokenType.KEYWORD && _tokenizer.KeyWord() == KeyWord.ELSE)
        {
            CompileLine(TagType.KeyWord, _tokenizer.KeyWord(), LineType.Both);
            CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
            CompileStatements();
            CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
        }
        
        CompileLine(TagType.IfStatement, null, LineType.Close);
    }


    public void CompileExpression()
    {
        CompileLine(TagType.Expression, null, LineType.Open);
        CompileTerm();
        while (IsOp())
        {
            CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
            CompileTerm();
        }
        CompileLine(TagType.Expression, null, LineType.Close);
    }
    
    public void CompileTerm()
    {
        CompileLine(TagType.Term, null, LineType.Open);
        switch (_tokenizer.TokenType())
        {
            case TokenType.INT_CONST:
                CompileLine(TagType.IntegerConstant, _tokenizer.IntVal().ToString(), LineType.Both);
                break;
            case TokenType.STRING_CONST:
                CompileLine(TagType.StringConstant, _tokenizer.StringVal(), LineType.Both);
                break;
            case TokenType.KEYWORD:
                switch (_tokenizer.KeyWord())
                {
                    case KeyWord.TRUE:
                    case KeyWord.FALSE:
                    case KeyWord.NULL:
                    case KeyWord.THIS:
                        CompileLine(TagType.KeyWord, _tokenizer.KeyWord(), LineType.Both);
                        break;
                }
                break;
            case TokenType.IDENTIFIER:
                CompileLine(TagType.Identifier, _tokenizer.Identifier(), LineType.Both);
                
                if (_tokenizer.TokenType() == TokenType.SYMBOL)
                {
                    // varName[]のパターン
                    if (_tokenizer.Symbol() == "[")
                    {
                        CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
                        CompileExpression();
                        CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
                    }

                    // subroutineCallのパターン
                    if (_tokenizer.Symbol() == "(" || _tokenizer.Symbol() == ".")
                    {
                        if (_tokenizer.Symbol() == ".")
                        {
                            CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
                            CompileLine(TagType.Identifier, _tokenizer.Identifier(), LineType.Both);
                            CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
                            CompileExpressionList();
                            CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
                        }
                        else if (_tokenizer.Symbol() == "(")
                        {
                            CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
                            CompileExpressionList();
                            CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
                        }
                    }
                }
                break;
            case TokenType.SYMBOL:
                if (_tokenizer.Symbol() == "(") // expressionのパターン
                {
                    CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
                    CompileExpression();
                    CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
                    break;
                }
                
                if (_tokenizer.Symbol() == "-" || _tokenizer.Symbol() == "~") // unaryOp term のパターン
                {
                    CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
                    CompileTerm();
                }
                break;
        }
        CompileLine(TagType.Term, null, LineType.Close);
    }

    public void CompileExpressionList()
    {
        CompileLine(TagType.ExpressionList, null, LineType.Open);
        if (IsTerm())
        {
            CompileExpression();
            while (_tokenizer.TokenType() == TokenType.SYMBOL && _tokenizer.Symbol() == ",")
            {
                CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
                CompileExpression();
            }
        }
        CompileLine(TagType.ExpressionList, null, LineType.Close);
    }

    public void CompileType()
    {
        if (_tokenizer.TokenType() == TokenType.KEYWORD)
        {
            CompileLine(TagType.KeyWord, _tokenizer.KeyWord(), LineType.Both);
        }
        else if (_tokenizer.TokenType() == TokenType.IDENTIFIER)
        {
            CompileLine(TagType.Identifier, _tokenizer.Identifier(), LineType.Both);
        }
    }

    public void Dispose()
    {
        _sw.Close();
    }

    private void Advance()
    {
        // 次のtokenの読み出し
        _tokenizer.Advance();
    }

    private void CompileLine(TagType tagType, KeyWord keyword, LineType lineType)
    {
        CompileLine(tagType, StateHelper.AnalyzeKeyword(keyword), lineType);
    }

    private void CompileLine(TagType tagType, string? str, LineType lineType)
    {
        if (!IsTerminatorTag(tagType) && lineType == LineType.Close)
        {
            RemoveIndent();
        }
        
        string tagName = _tagNames[tagType];
        _sw.Write($"{GetIndentStr()}");
        if (lineType == LineType.Open)
        {
            _sw.WriteLine($"<{tagName}>");
        }
        else if (lineType == LineType.Both)
        {
            _sw.WriteLine($"<{tagName}> {str} </{tagName}>");
        }
        else if (lineType == LineType.Close)
        {
            _sw.WriteLine($"</{tagName}>");
        }

        if (IsTerminatorTag(tagType))
        {
            Advance();
        }
        else if (lineType == LineType.Open)
        {
            AddIndent();
        }
    }

    private bool IsTerminatorTag(TagType tagType)
    {
        switch (tagType)
        {
            // 終端文字の場合次のTokenを読み込む
            case TagType.KeyWord:
            case TagType.Symbol:
            case TagType.IntegerConstant:
            case TagType.StringConstant:
            case TagType.Identifier:
                return true;
            default:
                return false;
        }
    }
    
    private string GetIndentStr()
    {
        if (_tempIndent.Length == _indentLevel * 2)
        {
            return _tempIndent;
        }
        char[] indentChars = new char[_indentLevel * 2];
        for (int i = 0; i < _indentLevel * 2; i++)
        {
            indentChars[i] = ' ';
        }
        _tempIndent = new string(indentChars);
        return _tempIndent;
    }

    private void AddIndent()
    {
        _indentLevel++;
    }

    private void RemoveIndent()
    {
        _indentLevel--;
    }

    private void InitTagNames()
    {
        _tagNames.Add(TagType.KeyWord, "keyword");
        _tagNames.Add(TagType.Symbol, "symbol");
        _tagNames.Add(TagType.IntegerConstant, "integerConstant");
        _tagNames.Add(TagType.StringConstant, "stringConstant");
        _tagNames.Add(TagType.Identifier, "identifier");
        _tagNames.Add(TagType.Class, "class");
        _tagNames.Add(TagType.ClassVarDec, "classVarDec");
        _tagNames.Add(TagType.Type, "type");
        _tagNames.Add(TagType.SubroutineDec, "subroutineDec");
        _tagNames.Add(TagType.ParameterList, "parameterList");
        _tagNames.Add(TagType.SubroutineBody, "subroutineBody");
        _tagNames.Add(TagType.VarDec, "varDec");
        _tagNames.Add(TagType.ClassName, "className");
        _tagNames.Add(TagType.SubroutineName, "subroutineName");
        _tagNames.Add(TagType.VarName, "varName");
        _tagNames.Add(TagType.Statements, "statements");
        _tagNames.Add(TagType.LetStatement, "letStatement");
        _tagNames.Add(TagType.IfStatement, "ifStatement");
        _tagNames.Add(TagType.WhileStatement, "whileStatement");
        _tagNames.Add(TagType.DoStatement, "doStatement");
        _tagNames.Add(TagType.ReturnStatement, "returnStatement");
        _tagNames.Add(TagType.Expression, "expression");
        _tagNames.Add(TagType.Term, "term");
        _tagNames.Add(TagType.ExpressionList, "expressionList");
    }

    /// <summary>
    /// tokenizerの今のtokenがtermかどうか確かめる
    /// </summary>
    /// <returns></returns>
    private bool IsTerm()
    {
        switch (_tokenizer.TokenType())
        {
            case TokenType.INT_CONST:
            case TokenType.STRING_CONST:
                return true;
            case TokenType.KEYWORD:
                switch (_tokenizer.KeyWord())
                {
                    case KeyWord.TRUE:
                    case KeyWord.FALSE:
                    case KeyWord.NULL:
                    case KeyWord.THIS:
                        return true;
                }
                break;
            case TokenType.IDENTIFIER:
                return true;
            case TokenType.SYMBOL:
                if (_tokenizer.Symbol() == "(") // expressionのパターン
                {
                    return true;
                }
                
                if (_tokenizer.Symbol() == "-" || _tokenizer.Symbol() == "~") // unaryOp term のパターン
                {
                    return true;
                }
                break;
        }

        return false;
    }

    private bool IsOp()
    {
        if(_tokenizer.TokenType() == TokenType.SYMBOL)
        {
            switch (_tokenizer.Symbol())
            {
                case "+":
                case "-":
                case "*":
                case "/":
                case "&amp;":
                case "|":
                case "&lt;":
                case "&gt;":
                case "=":
                    return true;
            }
        }

        return false;
    }
}
