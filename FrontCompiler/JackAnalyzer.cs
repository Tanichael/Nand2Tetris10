using System.Diagnostics;

namespace FrontCompiler;

public class JackAnalyzer
{
    public void Analyze(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Usage: jack filename");
            return;
        }
        
        string path = args[0];
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
        // CompilationEngineの利用
        
        string baseName = Path.GetFileNameWithoutExtension(filePath);
        string? directoryName = Path.GetDirectoryName(filePath);
        
        Console.WriteLine($"compile: {filePath}");
        string compiledFilePath = directoryName + "/" + baseName + ".xml";
        CompilationEngine compilationEngine = new CompilationEngine(filePath, compiledFilePath);
        compilationEngine.CompileClass();
        compilationEngine.Dispose();
    }
}