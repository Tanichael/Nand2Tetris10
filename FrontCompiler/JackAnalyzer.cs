using System.Diagnostics;

namespace FrontCompiler;

public class JackAnalyzer
{
    private JackTokenizer _jackTokenizer;
    private CompilationEngine _compilationEngine;

    public void Analyze(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Usage: jack filename");
            return;
        }
        
        string path = args[0];
        Console.WriteLine(path);
        bool isFile = File.Exists(path);
        bool isDirectory = Directory.Exists(path);

        if (isFile)
        {
            // ファイルの処理
            string extension = Path.GetExtension(path);
            if (extension != ".jack")
            {
                return;
            }
            
            AnalyzeFile(path);
        }

        if (isDirectory)
        {  
            // 再帰的に全てのjackファイルをコンパイル
            SearchDirectory(path);
        }
    }

    private void SearchDirectory(string directoryPath)
    {
        // ディレクトリ内のファイルを処理
        string[] jackFiles = Directory.GetFiles(directoryPath);
    
        foreach (string jackFile in jackFiles)
        {
            // コンパイル
            string extension = Path.GetExtension(jackFile);
            if (extension != ".jack")
            {
                continue;
            }
            AnalyzeFile(jackFile);
        }
        
        // ディレクトリ内のディレクトリを再起的に探索
        string[] nextDirectories = Directory.GetDirectories(directoryPath);
        foreach (var nextDirectoryPath in nextDirectories)
        {
            SearchDirectory(nextDirectoryPath);
        }
    }

    private void AnalyzeFile(string filePath)
    {
        JackTokenizer jackTokenizer = new JackTokenizer(filePath);
        string baseName = Path.GetFileNameWithoutExtension(filePath);
        string? directoryName = Path.GetDirectoryName(filePath);
        string tokensFilePath = directoryName + "/" + baseName + "T.xml";
        FileStream tokensStream = new FileStream(tokensFilePath, FileMode.Create);
        StreamWriter writer = new StreamWriter(tokensStream);

        Console.WriteLine($"analyze: {filePath}");
        writer.WriteLine("<tokens>");
        while (jackTokenizer.HasMoreTokens())
        {
            jackTokenizer.Advance();
            switch (jackTokenizer.TokenType())
            {
                case TokenType.KEYWORD:
                    writer.WriteLine(
                        $"<keyword> {AnalyzeKeyword(jackTokenizer.KeyWord())} </keyword>"
                    );
                    break;
                case TokenType.SYMBOL:
                    writer.WriteLine(
                        $"<symbol> {jackTokenizer.Symbol()} </symbol>"
                    );
                    break;
                case TokenType.INT_CONST:
                    writer.WriteLine(
                        $"<integerConstant> {jackTokenizer.IntVal()} </integerConstant>"
                    );
                    break;
                case TokenType.STRING_CONST:
                    writer.WriteLine(
                        $"<stringConstant> {jackTokenizer.StringVal()} </stringConstant>"
                    );
                    break;
                case TokenType.IDENTIFIER:
                    if (jackTokenizer.Identifier() == "")
                    {
                        continue;
                    }
                    writer.WriteLine(
                        $"<identifier> {jackTokenizer.Identifier()} </identifier>"
                    );
                    break;
            }
        }
        writer.WriteLine("</tokens>");

        writer.Close();
        jackTokenizer.Dispose();
    }

    private string AnalyzeKeyword(KeyWord keyword)
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