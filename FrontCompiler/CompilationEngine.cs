using System.Runtime.InteropServices.ComTypes;

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
    private VMWriter _vmWriter;
    private int _indentLevel;
    private string _tempIndent = "";
    private bool _hasNoToken = false;
    private Dictionary<TagType, string> _tagNames = new Dictionary<TagType, string>();
    private SymbolTable m_SymbolTable;
    private int _ifCounter = 0;
    private int _whileCounter = 0;
    private string _fileBase = "";
    
    public CompilationEngine(string inputFile, string outputFile)
    {
        FileStream outputStream = new FileStream(outputFile, FileMode.Create);
        _sw = new StreamWriter(outputStream);
        m_SymbolTable = new SymbolTable();
        _tokenizer = new JackTokenizer(inputFile);
        
        string baseName = Path.GetFileNameWithoutExtension(inputFile);
        _fileBase = baseName;
        string? directoryName = Path.GetDirectoryName(inputFile);
        string compiledFilePath = directoryName + "/" + baseName + ".vm";
        _vmWriter = new VMWriter(compiledFilePath);
        
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
        
        m_SymbolTable.AddScope();
       
        CompileLine(TagType.Class, null, LineType.Open);
        CompileLine(TagType.KeyWord, _tokenizer.KeyWord(), LineType.Both);
        string className = _tokenizer.Identifier();
        CompileIdentifier(className, true);
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
                    m_SymbolTable.AddScope();
                    CompileSubroutine();
                    m_SymbolTable.EndScope();
                    continue;
            }

            break;
        }
        
        CompileLine(TagType.Symbol, "}", LineType.Both);
        CompileLine(TagType.Class, null, LineType.Close);
        
        m_SymbolTable.EndScope();
    }

    public void CompileClassVarDec()
    {
        CompileLine(TagType.ClassVarDec, null, LineType.Open);
        bool isStatic = _tokenizer.KeyWord() == KeyWord.STATIC;
        CompileLine(TagType.KeyWord, _tokenizer.KeyWord(), LineType.Both);

        // type
        string typeName = CompileType();
        
        // フィールド or static変数 を定義
        m_SymbolTable.Define(
            _tokenizer.Identifier(),
            typeName,
            isStatic ? SymbolTable.Kind.STATIC : SymbolTable.Kind.FIELD
        );
        
        CompileIdentifier(
            _tokenizer.Identifier(), 
            true
        );
        
        // 次が,なら変数の宣言が連続する
        while (_tokenizer.TokenType() == TokenType.SYMBOL && _tokenizer.Symbol() == ",")
        {
            CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
        
            // フィールド or static変数 を定義
            m_SymbolTable.Define(
                _tokenizer.Identifier(),
                typeName,
                isStatic ? SymbolTable.Kind.STATIC : SymbolTable.Kind.FIELD
            );
            
            CompileIdentifier(
                _tokenizer.Identifier(), 
                true
            );
        }
        
        CompileLine(TagType.Symbol, ";", LineType.Both);
        CompileLine(TagType.ClassVarDec, null, LineType.Close);
    }

    public void CompileSubroutine()
    {
        CompileLine(TagType.SubroutineDec, null, LineType.Open);
        KeyWord subroutineKeyWord = _tokenizer.KeyWord();
        CompileLine(TagType.KeyWord, _tokenizer.KeyWord(), LineType.Both);

        // ("void" | type)
        CompileType();

        // subroutineName
        string subroutineName = _tokenizer.Identifier();
        CompileIdentifier(subroutineName, true);
        
        // (
        CompileLine(TagType.Symbol, "(", LineType.Both);
        
        // methodの時は引数1つ目にthisを入れる
        if (subroutineKeyWord == KeyWord.METHOD)
        {
            m_SymbolTable.Define(
                "this",
                _fileBase,
                SymbolTable.Kind.ARG
            );
        }
        
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
        
        // メソッドの定義
        _vmWriter.WriteFunction(
            $"{_fileBase}.{subroutineName}", 
            m_SymbolTable.VarCount(SymbolTable.Kind.VAR)
        );
        
        // thisの値を設定しておく
        // methodの場合はフィールドへのアクセスで用いる
        if (subroutineKeyWord == KeyWord.METHOD)
        {
            _vmWriter.WritePush(VMWriter.Segment.ARG, 0);
            _vmWriter.WritePop(VMWriter.Segment.POINTER, 0);
        }
        else if (subroutineKeyWord == KeyWord.CONSTRUCTOR)
        {
            // サイズ分のメモリブロックを確保
            int count = m_SymbolTable.VarCount(SymbolTable.Kind.FIELD);
            _vmWriter.WritePush(VMWriter.Segment.CONST, count);
            _vmWriter.WriteCall("Memory.alloc", 1);
            
            // 割り当てられたアドレスをTHISに設定
            _vmWriter.WritePop(VMWriter.Segment.POINTER, 0);
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

        string typeName0 = "";
        
        // typeがあるならコンパイル
        if (_tokenizer.TokenType() == TokenType.KEYWORD)
        {
            typeName0 = StateHelper.AnalyzeKeyword(_tokenizer.KeyWord());
            CompileLine(TagType.KeyWord, _tokenizer.KeyWord(), LineType.Both);
        }
        else if (_tokenizer.TokenType() == TokenType.IDENTIFIER)
        {
            typeName0 = _tokenizer.Identifier();
            CompileIdentifier(_tokenizer.Identifier());
        }
        else // typeがなければ切り上げて終了
        {
            CompileLine(TagType.ParameterList, null, LineType.Close);
            return;
        }
        
        // varName
        // 引数を定義
        m_SymbolTable.Define(
            _tokenizer.Identifier(),
            typeName0,
            SymbolTable.Kind.ARG
        );
        CompileIdentifier(_tokenizer.Identifier());
        
        while (_tokenizer.TokenType() == TokenType.SYMBOL && _tokenizer.Symbol() == ",")
        {
            // ,
            CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
            
            // type
            string typeName = CompileType();
            
            m_SymbolTable.Define(
                _tokenizer.Identifier(),
                typeName,
                SymbolTable.Kind.ARG
            );
            
            // varName
            CompileIdentifier(_tokenizer.Identifier());
        }
        
        CompileLine(TagType.ParameterList, null, LineType.Close);
    }

    /// <summary>
    /// 'var' type varName (',' varName)* ';'
    /// </summary>
    public void CompileVarDec()
    {
        CompileLine(TagType.VarDec, null, LineType.Open);
        KeyWord varKeyWord = _tokenizer.KeyWord();
        CompileLine(TagType.KeyWord, varKeyWord, LineType.Both);
        string typeName = CompileType();

        // 変数を定義
        m_SymbolTable.Define(
            _tokenizer.Identifier(),
            typeName,
            SymbolTable.Kind.VAR
        );
        CompileIdentifier(_tokenizer.Identifier(), true);
        
        while (_tokenizer.TokenType() == TokenType.SYMBOL && _tokenizer.Symbol() == ",")
        {
            CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
            
            // 変数を定義
            m_SymbolTable.Define(
                _tokenizer.Identifier(),
                typeName,
                SymbolTable.Kind.VAR
            );
            CompileIdentifier(_tokenizer.Identifier(), true);
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
        // 次を見るまでクラス名かサブルーチン名かがわからない
        string functionName = _tokenizer.Identifier();
        CompileIdentifier(_tokenizer.Identifier());
        
        int beforeLocalCount = _vmWriter.PushedCount;
        
        if (_tokenizer.TokenType() == TokenType.SYMBOL)
        {
            if (_tokenizer.Symbol() == ".")
            {
                CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
                
                // functionNameはインスタンスあるいはクラスの名称
                // クラス名の場合はstaticなのでthisは入れない
                // インスタンス名の場合は引数の1つ目にthisを入れる
                // 1文字目が大文字かどうかで判定する
                if (functionName[0] >= 'a' && functionName[0] <= 'z')
                {
                    // インスタンス名の場合はそのアドレスを引数の1つ目に設定
                    SymbolTable.Kind kind = m_SymbolTable.KindOf(functionName);
                    int index = m_SymbolTable.IndexOf(functionName);
                    string type = m_SymbolTable.TypeOf(functionName);
                    _vmWriter.WritePush(StateHelper.GetSegmentFromKind(kind), index);
                    functionName = type;
                }
                                
                functionName = $"{functionName}.{_tokenizer.Identifier()}";
                CompileIdentifier(_tokenizer.Identifier());
                CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
                CompileExpressionList();
                CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
            }
            else if (_tokenizer.Symbol() == "(")
            {
                // functionNameはローカル関数の名前なので、THISの値を引数1つ目にする
                // コンストラクタ内でTHISの値は設定しておく
                _vmWriter.WritePush(VMWriter.Segment.POINTER, 0);
                CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
                CompileExpressionList();
                CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
                functionName = $"{_fileBase}.{functionName}";
            }
            
            int afterLocalCount = _vmWriter.PushedCount;
            int argCount = afterLocalCount - beforeLocalCount;
            _vmWriter.WriteCall(functionName, argCount);
        }
        
        CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
        CompileLine(TagType.DoStatement, null, LineType.Close);
        // 戻り値がvoidの場合popする必要がある
        _vmWriter.WritePop(VMWriter.Segment.TEMP, 0);
    }

    public void CompileLet()
    {
        CompileLine(TagType.LetStatement, null, LineType.Open);
        CompileLine(TagType.KeyWord, _tokenizer.KeyWord(), LineType.Both);
        
        string type = m_SymbolTable.TypeOf(_tokenizer.Identifier());
        SymbolTable.Kind kind = m_SymbolTable.KindOf(_tokenizer.Identifier());
        int varIndex = m_SymbolTable.IndexOf(_tokenizer.Identifier());
        CompileIdentifier(_tokenizer.Identifier());
        bool isArray = false;
        
        if (_tokenizer.TokenType() == TokenType.SYMBOL && _tokenizer.Symbol() == "[")
        {
            isArray = true;
            
            // [
            CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
            
            // expression
            CompileExpression();
            
            // アドレスを退避させる
            _vmWriter.WritePush(StateHelper.GetSegmentFromKind(kind), varIndex);
            _vmWriter.WriteArithmetic(VMWriter.Command.ADD);
            _vmWriter.WritePop(VMWriter.Segment.TEMP, 1);
        
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

        if (!isArray)
        {
            switch (kind)
            {
                case SymbolTable.Kind.STATIC:
                    _vmWriter.WritePop(VMWriter.Segment.STATIC, varIndex);
                    break;
                case SymbolTable.Kind.FIELD:
                    // ポインタを使ってアクセス
                    // 事前に pointer 0 に pop することで THIS にアドレスを設定しておく必要がある
                    _vmWriter.WritePop(VMWriter.Segment.THIS, varIndex);
                    break;
                case SymbolTable.Kind.ARG:
                    _vmWriter.WritePop(VMWriter.Segment.ARG, varIndex);
                    break;
                case SymbolTable.Kind.VAR:
                    _vmWriter.WritePop(VMWriter.Segment.LOCAL, varIndex);
                    break;
            }
        }
        else // 配列の場合
        {
            // アドレスを復帰する
            _vmWriter.WritePush(VMWriter.Segment.TEMP, 1);
            _vmWriter.WritePop(VMWriter.Segment.POINTER, 1);
            _vmWriter.WritePop(VMWriter.Segment.THAT, 0);
        }
    }

    public void CompileWhile()
    {
        int whileCounter = _whileCounter++;
        _vmWriter.WriteLabel($"while_{_fileBase}{whileCounter}");
        CompileLine(TagType.WhileStatement, null, LineType.Open);
        CompileLine(TagType.KeyWord, _tokenizer.KeyWord(), LineType.Both);
        CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
        CompileExpression();
        CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
        _vmWriter.WriteIf($"while_{_fileBase}_true{whileCounter}");
        _vmWriter.WriteGoto($"while_{_fileBase}_false{whileCounter}");
        
        _vmWriter.WriteLabel($"while_{_fileBase}_true{whileCounter}");
        CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
        CompileStatements();
        CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
        _vmWriter.WriteGoto($"while_{_fileBase}{whileCounter}");
        
        _vmWriter.WriteLabel($"while_{_fileBase}_false{whileCounter}");
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
        else
        {
            // voidの場合は0をreturnするように設定
            _vmWriter.WritePush(VMWriter.Segment.CONST, 0);
        }
        
        CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
        CompileLine(TagType.ReturnStatement, null, LineType.Close);
        
        _vmWriter.WriteReturn();
    }

    public void CompileIf()
    {
        int counter = _ifCounter++;
        CompileLine(TagType.IfStatement, null, LineType.Open);
        CompileLine(TagType.KeyWord, _tokenizer.KeyWord(), LineType.Both);
        CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
        CompileExpression();
        CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
        
        // trueの場合
        _vmWriter.WriteIf($"if_{_fileBase}_true{counter}");
        
        // falseの場合
        _vmWriter.WriteGoto($"if_{_fileBase}_false{counter}");
        
        // label宣言
        _vmWriter.WriteLabel($"if_{_fileBase}_true{counter}");
        CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
        CompileStatements();
        CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
        // goto
        _vmWriter.WriteGoto($"if_{_fileBase}_end{counter}");
        
        // label宣言
        _vmWriter.WriteLabel($"if_{_fileBase}_false{counter}");
        if (_tokenizer.TokenType() == TokenType.KEYWORD && _tokenizer.KeyWord() == KeyWord.ELSE)
        {
            CompileLine(TagType.KeyWord, _tokenizer.KeyWord(), LineType.Both);
            CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
            CompileStatements();
            CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
            // goto
            _vmWriter.WriteGoto($"if_{_fileBase}_end{counter}");
        }
        
        _vmWriter.WriteLabel($"if_{_fileBase}_end{counter}");
        CompileLine(TagType.IfStatement, null, LineType.Close);
    }


    public void CompileExpression()
    {
        CompileLine(TagType.Expression, null, LineType.Open);
        CompileTerm();
        while (IsOp())
        {
            string op = _tokenizer.Symbol();
            CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
            CompileTerm();
            VMWriter.Command command = _vmWriter.GetCommand(op);
            if (command == VMWriter.Command.MULT)
            {
                _vmWriter.WriteCall("Math.multiply", 2);
            }
            else if (command == VMWriter.Command.DIV)
            {
                _vmWriter.WriteCall("Math.divide", 2);
            }
            else
            {
                _vmWriter.WriteArithmetic(command);
            }
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
                _vmWriter.WritePush(VMWriter.Segment.CONST, _tokenizer.IntVal());
                break;
            case TokenType.STRING_CONST:
                CompileLine(TagType.StringConstant, _tokenizer.StringVal(), LineType.Both);
                int strLength = _tokenizer.StringVal().Length;
                _vmWriter.WritePush(VMWriter.Segment.CONST, strLength);
                _vmWriter.WriteCall("String.new", 1);

                _vmWriter.WritePop(VMWriter.Segment.POINTER, 1);
                _vmWriter.WritePush(VMWriter.Segment.POINTER, 1);

                for (int i = 0; i < strLength; i++)
                {
                    _vmWriter.WritePush(VMWriter.Segment.POINTER, 1);
                    _vmWriter.WritePush(VMWriter.Segment.CONST, _tokenizer.StringVal()[i] - 'a' + 97);
                    _vmWriter.WriteCall("String.appendChar", 2);
                    _vmWriter.WritePop(VMWriter.Segment.TEMP, 0);
                }
                break;
            case TokenType.KEYWORD:
                switch (_tokenizer.KeyWord())
                {
                    case KeyWord.TRUE:
                        _vmWriter.WritePush(VMWriter.Segment.CONST, 1);
                        _vmWriter.WriteArithmetic(VMWriter.Command.NEG);
                        CompileLine(TagType.KeyWord, _tokenizer.KeyWord(), LineType.Both);
                        break;
                    case KeyWord.FALSE:
                        _vmWriter.WritePush(VMWriter.Segment.CONST, 0);
                        CompileLine(TagType.KeyWord, _tokenizer.KeyWord(), LineType.Both);
                        break;
                    case KeyWord.NULL:
                        _vmWriter.WritePush(VMWriter.Segment.CONST, 0);
                        CompileLine(TagType.KeyWord, _tokenizer.KeyWord(), LineType.Both);
                        break;
                    case KeyWord.THIS:
                        _vmWriter.WritePush(VMWriter.Segment.POINTER, 0);
                        CompileLine(TagType.KeyWord, _tokenizer.KeyWord(), LineType.Both);
                        break;
                }
                break;
            case TokenType.IDENTIFIER:
                string identifier = _tokenizer.Identifier();
                CompileIdentifier(_tokenizer.Identifier());

                if (_tokenizer.TokenType() == TokenType.SYMBOL && _tokenizer.Symbol() == "[" || _tokenizer.Symbol() == "(" || _tokenizer.Symbol() == ".")
                {
                    // varName[]のパターン
                    if (_tokenizer.Symbol() == "[")
                    {
                        CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
                        CompileExpression();
                        SymbolTable.Kind kind = m_SymbolTable.KindOf(identifier);
                        int index = m_SymbolTable.IndexOf(identifier);
                        _vmWriter.WritePush(StateHelper.GetSegmentFromKind(kind), index);
                        _vmWriter.WriteArithmetic(VMWriter.Command.ADD);
                        _vmWriter.WritePop(VMWriter.Segment.POINTER, 1);
                        _vmWriter.WritePush(VMWriter.Segment.THAT, 0);
                        CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
                    }

                    // subroutineCallのパターン
                    if (_tokenizer.Symbol() == "(" || _tokenizer.Symbol() == ".")
                    {
                        int beforeLocalCount = _vmWriter.PushedCount;

                        if (_tokenizer.Symbol() == ".")
                        {
                            CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
                            
                            if (identifier[0] >= 'a' && identifier[0] <= 'z')
                            {
                                // インスタンス名の場合
                                // 引数1つ目にインスタンスのベースアドレスを入れておく
                                SymbolTable.Kind kind = m_SymbolTable.KindOf(identifier);
                                int index = m_SymbolTable.IndexOf(identifier);
                                string type = m_SymbolTable.TypeOf(identifier);
                                _vmWriter.WritePush(StateHelper.GetSegmentFromKind(kind), index);
                                identifier = type;
                            }

                            identifier = $"{identifier}.{_tokenizer.Identifier()}";
                            CompileIdentifier(_tokenizer.Identifier());
                            CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
                            CompileExpressionList();
                            CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
                        }
                        else if (_tokenizer.Symbol() == "(")
                        {
                            // THISの値を引数1つ目に設定
                            _vmWriter.WritePush(VMWriter.Segment.POINTER, 0);
                            CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
                            CompileExpressionList();
                            CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
                        }
                        
                        int afterLocalCount = _vmWriter.PushedCount;
                        int argCount = afterLocalCount - beforeLocalCount;
                        _vmWriter.WriteCall(identifier, argCount);
                    }
                }
                else
                {
                    // 単なるvarNameの場合
                    int index = m_SymbolTable.IndexOf(identifier);
                    SymbolTable.Kind kind = m_SymbolTable.KindOf(identifier);
                    switch (kind)
                    {
                        case SymbolTable.Kind.STATIC:
                            _vmWriter.WritePush(VMWriter.Segment.STATIC, index);
                            break;
                        case SymbolTable.Kind.FIELD:
                            // フィールド変数の処理
                            _vmWriter.WritePush(VMWriter.Segment.THIS, index);
                            break;
                        case SymbolTable.Kind.ARG:
                            _vmWriter.WritePush(VMWriter.Segment.ARG, index);
                            break;
                        case SymbolTable.Kind.VAR:
                            _vmWriter.WritePush(VMWriter.Segment.LOCAL, index);
                            break;
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
                    string unarySymbol = _tokenizer.Symbol();
                    CompileLine(TagType.Symbol, _tokenizer.Symbol(), LineType.Both);
                    CompileTerm();

                    if (unarySymbol == "-")
                    {
                        _vmWriter.WriteArithmetic(VMWriter.Command.NEG);
                    }
                    else if(unarySymbol == "~")
                    {
                        _vmWriter.WriteArithmetic(VMWriter.Command.NOT);
                    }
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

    public string CompileType()
    {
        string typeName = "";
        
        if (_tokenizer.TokenType() == TokenType.KEYWORD)
        {
            typeName = StateHelper.AnalyzeKeyword(_tokenizer.KeyWord());
            CompileLine(TagType.KeyWord, _tokenizer.KeyWord(), LineType.Both);
        }
        else if (_tokenizer.TokenType() == TokenType.IDENTIFIER)
        {
            typeName = _tokenizer.Identifier();
            CompileIdentifier(_tokenizer.Identifier());
        }
        
        return typeName;
    }

    public void Dispose()
    {
        _sw.Close();
        _vmWriter.Close();
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
        
        if (tagType == TagType.Identifier)
        {
            // Identifier は別で処理する
            return;
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

    private void CompileIdentifier(string? str, bool isDefined = false)
    {
        if (str == null)
        {
            return;
        }
        
        SymbolTable.Kind kind = m_SymbolTable.KindOf(str);
        string categoryName = "";
        int index = -1;
        if (kind != SymbolTable.Kind.ELSE)
        {
            categoryName = StateHelper.GetKindName(kind);
            index = m_SymbolTable.IndexOf(str);
        }
        else
        {
            // 1文字目が大文字かどうかで判定
            if (str[0] >= 'a' && str[0] <= 'z')
            {
                categoryName = "subroutine";
            }
            else if (str[0] >= 'A' && str[0] <= 'Z')
            {
                categoryName = "class";
            }
        }
        string definedStr = isDefined ? "defined" : "used";
        string kindStr = index >= 0 ? " " + categoryName + " " + index.ToString() : ""; 
        _sw.Write($"{GetIndentStr()}");
        _sw.WriteLine($"<identifier {categoryName} {definedStr}{kindStr}> {str} </identifier>");
        Advance();
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
