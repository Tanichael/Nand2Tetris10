using System.Data;
using System.Xml.Schema;

namespace FrontCompiler;

public class SymbolTable
{
    public enum Kind
    {
        STATIC,
        FIELD,
        ARG,
        VAR,
        ELSE,
    }

    public struct SymbolElement
    {
        public string Type;
        public Kind Kind;
        public int Index;
    }

    private List<Dictionary<string, SymbolElement>> m_SymbolTable;
    private int m_TempScopeCount;
    
    public SymbolTable()
    {
        m_SymbolTable = new List<Dictionary<string, SymbolElement>>();
        m_TempScopeCount = -1;
    }

    public void Define(string name, string type, Kind kind)
    {
        int count = VarCount(kind);
        if (m_SymbolTable.Count <= m_TempScopeCount)
        {
            m_SymbolTable.Add(new Dictionary<string, SymbolElement>());
        }
        Dictionary<string, SymbolElement> symbolElements = m_SymbolTable[m_TempScopeCount];

        // まだ定義されていなければ追加
        if (!symbolElements.ContainsKey(name))
        {
            symbolElements.Add(name, new SymbolElement { Type = type, Kind = kind, Index = count});
        }
    } 

    public int VarCount(Kind kind)
    {
        if (m_TempScopeCount == -1 || m_SymbolTable.Count <= m_TempScopeCount)
        {
            return 0;
        }

        // 現在のスコープのハッシュテーブルを探索し、同じKindのものの数を返す
        int scopeCount = m_TempScopeCount;
        int varCount = 0;
        while (scopeCount >= 0)
        {
            Dictionary<string, SymbolElement> symbolElements = m_SymbolTable[scopeCount];
            foreach (var kvp in symbolElements)
            {
                if (kvp.Value.Kind == kind)
                {
                    varCount++;
                }
            }

            scopeCount--;
        }
        
       
        
        return varCount;
    }

    public Kind KindOf(string name)
    {
        if (m_TempScopeCount == -1 || m_SymbolTable.Count <= m_TempScopeCount)
        {
            return Kind.STATIC;
        }

        int scopeCount = m_TempScopeCount;
        while (scopeCount >= 0)
        {
            Dictionary<string, SymbolElement> symbolElements = m_SymbolTable[scopeCount];
            if (symbolElements.TryGetValue(name, out SymbolElement symbolElement))
            {
                return symbolElement.Kind;
            }

            scopeCount--;
        }

        return Kind.ELSE;
    }

    public string TypeOf(string name)
    {
        if (m_TempScopeCount == -1 || m_SymbolTable.Count <= m_TempScopeCount)
        {
            return "";
        }
        
        int scopeCount = m_TempScopeCount;
        while (scopeCount >= 0)
        {
            Dictionary<string, SymbolElement> symbolElements = m_SymbolTable[scopeCount];
            if (symbolElements.TryGetValue(name, out SymbolElement symbolElement))
            {
                return symbolElement.Type;
            }

            scopeCount--;
        }        
        return "";
    }

    public int IndexOf(string name)
    {
        if (m_TempScopeCount == -1 || m_SymbolTable.Count <= m_TempScopeCount)
        {
            return 0;
        }
        
        int scopeCount = m_TempScopeCount;
        while (scopeCount >= 0)
        {
            Dictionary<string, SymbolElement> symbolElements = m_SymbolTable[scopeCount];
            if (symbolElements.TryGetValue(name, out SymbolElement symbolElement))
            {
                return symbolElement.Index;
            }

            scopeCount--;
        }            
        return 0;
    }

    public void AddScope()
    {
        m_SymbolTable.Add(new Dictionary<string, SymbolElement>());
        m_TempScopeCount++;
    }

    public void EndScope()
    {
        m_SymbolTable[m_TempScopeCount].Clear();
        m_TempScopeCount--;
    }
}