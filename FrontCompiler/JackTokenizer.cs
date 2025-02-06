using System.IO;

namespace FrontCompiler
{
    public enum TokenType
    {
        KEYWORD,
        SYMBOL,
        IDENTIFIER,
        INT_CONST,
        STRING_CONST,
    }

    public enum KeyWord
    {
        CLASS,
        METHOD,
        FUNCTION,
        CONSTRUCTOR,
        INT,
        BOOLEAN,
        CHAR,
        VOID,
        VAR,
        STATIC,
        FIELD,
        LET,
        DO,
        IF,
        ELSE,
        WHILE,
        RETURN,
        TRUE,
        FALSE,
        NULL,
        THIS
    }

    /// <summary>
    /// トークナイザ
    /// 入力ファイルからトークンの列を作る
    /// どこまでがキーワードなのかってどうやって判断するの？
    /// 文法を参照しなきゃいけないということ？
    ///
    /// 一旦テストファイルを見る
    ///
    /// シンボルを区切り文字として読む
    /// </summary>
    public class JackTokenizer: IDisposable
    {
        private FileStream _fs;
        private StreamReader _sr;
        private string _allLines;
        private TokenType _tempTokenType;
        private KeyWord _tempKeyWord;
        private string _tempSymbol;
        private string _tempIdentifier;
        private int _tempIntVal;
        private string _tempStringVal;
        private string[] _tokenArray;
        private char _readStock = ' ';
    
        public JackTokenizer(string fileName)
        {
            _fs = new FileStream(fileName, FileMode.Open);
            _sr = new StreamReader(_fs);
            if (_sr.Peek() != -1)
            {
                _readStock = (char)_sr.Read();
            }
        }
        
        public void Dispose()
        {
            _sr.Close();
        }

        // StateMachineを利用するパターン
        public void Advance()
        {
            char[] buffer = new char[100];
            int bufLength = 0;

            if (_sr.Peek() == -1)
            {
                return;
            }
            
            while (_sr.Peek() != -1)
            {
                bufLength = 0; // コメントの一部が含まれているケースがあるので、文字数カウントをリセットする
                StateMachine stateMachine = new StateMachine();
            
                while (true) // tokenが取得できたらループから抜ける
                {
                    char c = _readStock;
                    stateMachine.Process(c);
                    
                    // コメント以外の場合のみbufferを活用
                    if (!stateMachine.IsStart() && !stateMachine.IsComment())
                    {
                        buffer[bufLength] = c;
                        bufLength++;
                    }
                    
                    // 終了ならここで抜ける
                    if (stateMachine.IsEnd() || _sr.Peek() == -1)
                    {
                        break;
                    }
                    
                    _readStock = (char)_sr.Read();
                }
                
                // コメント以外の場合はtoken確定
                if (!stateMachine.IsComment())
                {
                    break;
                }
            }

            if (bufLength == 0)
            {
                // 出力しないようにスキップするための設定
                _tempTokenType = FrontCompiler.TokenType.IDENTIFIER;
                _tempIdentifier = "";
                return;
            }
            
            // bufferをカット
            char[] tokenBuffer = new char[bufLength - 1];
            for (int i = 0; i < tokenBuffer.Length; i++)
            {
                tokenBuffer[i] = buffer[i];
            }
            string token = new string(tokenBuffer);
            // Console.WriteLine($"token: {token}");
            
            // タイプによって処理を区別する
            if (StateHelper.IsKeyWord(token))
            {
                // キーワードの処理
                _tempTokenType = FrontCompiler.TokenType.KEYWORD;
                _tempKeyWord = StateHelper.GetKeyWord(token);
            }
            else if (token.Length == 1 && StateHelper.IsSymbol(token[0]))
            {
                _tempTokenType = FrontCompiler.TokenType.SYMBOL;
                if (token == "<")
                {
                    _tempSymbol = "&lt;";
                }
                else if (token == "&")
                {
                    _tempSymbol = "&amp;";
                }
                else if (token == ">")
                {
                    _tempSymbol = "&gt;";
                }
                else
                {
                    _tempSymbol = token;
                }
            }
            else if (int.TryParse(token, out int intVal))
            {
                _tempTokenType = FrontCompiler.TokenType.INT_CONST;
                _tempIntVal = intVal;
            }
            else if (token.Length >= 2 && token[0] == '\"')
            {
                _tempTokenType = FrontCompiler.TokenType.STRING_CONST;
                _tempStringVal = token.Substring(1, token.Length - 2);
            }
            else
            {
                _tempTokenType = FrontCompiler.TokenType.IDENTIFIER;
                _tempIdentifier = token;
            }
        }

        // public void Advance()
        // {
        //     char[] buffer = new char[100];
        //     int bufLength = 0;
        //     bool strStarted = false;
        //     bool isTypeDecided = false;
        //     bool isCommentFirst = false;
        //     bool isInComment = false;
        //     bool isInMultiComment = false;
        //     bool isMultiCommentEndFirst = false;
        //     char c;
        //     
        //     // 1文字ずつtokenを形成するまで読み込む
        //     while(true)
        //     {
        //         if (_readStock != ' ')
        //         {
        //             c = _readStock;
        //             _readStock = ' ';
        //         }
        //         else if (_sr.Peek() != -1)
        //         {
        //             c = (char)_sr.Read();
        //         }
        //         else
        //         {
        //             break;
        //         }
        //
        //         // コメントの処理
        //         if (!isCommentFirst)
        //         {
        //             if (c == '/')
        //             {
        //                 isCommentFirst = true;
        //             }
        //         }
        //         else
        //         {
        //             if (c != '/' && c != '*')
        //             {
        //                 isCommentFirst = false;
        //             }
        //             else if (c == '/')
        //             {
        //                 isInComment = true;
        //             }
        //             else if (c == '*')
        //             {
        //                 isInMultiComment = true;
        //             }
        //         }
        //
        //         if (isInComment)
        //         {
        //             if (c == '\n')
        //             {
        //                 isCommentFirst = false;
        //                 isInComment = false;
        //             }
        //             else
        //             {
        //                 continue;
        //             }
        //         }
        //         
        //         if (isMultiCommentEndFirst)
        //         {
        //             isMultiCommentEndFirst = false;
        //             if (c == '/')
        //             {
        //                 isCommentFirst = false;
        //                 isInMultiComment = false;
        //             }
        //
        //             continue;
        //         }
        //
        //         if (isInMultiComment)
        //         {
        //             if (c == '*')
        //             {
        //                 isMultiCommentEndFirst = true;
        //             }
        //            
        //             continue;
        //         }
        //         
        //         if (!strStarted && c == ' ' || c == '\t' || c == '\n' || c == '\r')
        //         {
        //             if (bufLength == 0)
        //             {
        //                 continue;
        //             }
        //             break;
        //         }
        //
        //         if (!strStarted && IsSymbol(c))
        //         {
        //             if (bufLength == 0)
        //             {
        //                 // コメントかどうかの確認
        //                 if (c == '/' && _sr.Peek() != -1)
        //                 {
        //                     char next = (char)_sr.Read();
        //                     _readStock = next;
        //                     if (next == '/' || next == '*')
        //                     {
        //                         // コメントなのでシンボルではない
        //                         continue;
        //                     }
        //                 }
        //                 
        //                 _tempTokenType = FrontCompiler.TokenType.SYMBOL;
        //                 isTypeDecided = true;
        //                 buffer[0] = c;
        //                 bufLength = 1;
        //                 break;
        //             }
        //             
        //             // symbolを次のcharにして打ち切り
        //             _readStock = c;
        //             break;
        //         }
        //
        //         if (c == '\"')
        //         {
        //             if (!strStarted)
        //             {
        //                 strStarted = true;
        //                 continue;
        //             }
        //
        //             strStarted = false;
        //             isTypeDecided = true;
        //             _tempTokenType = FrontCompiler.TokenType.STRING_CONST;
        //             break;
        //         }
        //         
        //         // 1文字追加
        //         buffer[bufLength] = c;
        //         bufLength += 1;
        //     }
        //     
        //     char[] buf2 = new char[bufLength];
        //     for (int i = 0; i < bufLength; i++)
        //     {
        //         buf2[i] = buffer[i];
        //     }
        //     string token = new string(buf2);
        //
        //     if (bufLength == 0)
        //     {
        //         token = "";
        //     }
        //
        //     if (!isTypeDecided)
        //     {
        //         if (token == "class" || token == "constructor" || token == "function" || token == "method"
        //             || token == "field" || token == "static" || token == "var" || token == "int" || token == "char" 
        //             || token == "boolean" || token == "void" || token == "true" || token == "false"
        //             || token == "null" || token == "this" || token == "let" || token == "do"
        //             || token == "if" || token == "else" || token == "while" || token == "return")
        //         {
        //             _tempTokenType = FrontCompiler.TokenType.KEYWORD;
        //             _tempKeyWord = GetKeyWord(token);
        //         }
        //         else if (int.TryParse(token, out int tempToken))
        //         {
        //             _tempTokenType = FrontCompiler.TokenType.INT_CONST;
        //             _tempIntVal = tempToken;
        //         }
        //         else
        //         {
        //             _tempTokenType = FrontCompiler.TokenType.IDENTIFIER;
        //             _tempIdentifier = token;
        //         }
        //     }
        //     
        //     switch (_tempTokenType)
        //     {
        //         case FrontCompiler.TokenType.SYMBOL:
        //             if (token == "<")
        //             {
        //                 _tempSymbol = "&lt;";
        //             }
        //             else if (token == "&")
        //             {
        //                 _tempSymbol = "&amp;";
        //             }
        //             else if (token == ">")
        //             {
        //                 _tempSymbol = "&gt;";
        //             }
        //             else
        //             {
        //                 _tempSymbol = token;
        //             }
        //             break;
        //         case FrontCompiler.TokenType.IDENTIFIER:
        //             _tempIdentifier = token;
        //             break;
        //         case FrontCompiler.TokenType.STRING_CONST:
        //             _tempStringVal = token;
        //             break;
        //     }
        // }

        public bool HasMoreTokens()
        {
             return _sr.Peek() != -1;
        }

        public TokenType TokenType()
        {
            return _tempTokenType;
        }

        public KeyWord KeyWord()
        {
            return _tempKeyWord;
        }

        public string Symbol()
        {
            return _tempSymbol;
        }

        public string Identifier()
        {
            return _tempIdentifier;
        }

        public int IntVal()
        {
            return _tempIntVal;
        }

        public string StringVal()
        {
            return _tempStringVal;
        }

        private bool IsSymbol(char c)
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

        private KeyWord GetKeyWord(string keyword)
        {
            switch (keyword)
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
    }
}
