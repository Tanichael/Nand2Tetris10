// See https://aka.ms/new-console-template for more information

using FrontCompiler;

class Program
{
    public static void Main(string[] args)
    {
        JackAnalyzer jackAnalyzer = new JackAnalyzer();
        jackAnalyzer.Analyze(args);
    }
}