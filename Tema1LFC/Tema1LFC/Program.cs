using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Tema1LFC
{
    public class DeterministicFiniteAutomaton
    {
        public HashSet<int> States { get; } = new HashSet<int>();
        public HashSet<char> Alphabet { get; } = new HashSet<char>();
        public Dictionary<int, Dictionary<char, int>> Transitions { get; } = new Dictionary<int, Dictionary<char, int>>();
        public int InitialState { get; set; }
        public HashSet<int> FinalStates { get; } = new HashSet<int>();

        public bool VerifyAutomaton(out string message)
        {
            if (!States.Contains(InitialState))
            {
                message = "Starea initiala nu apartine multimii starilor.";
                return false;
            }

            if (!FinalStates.IsSubsetOf(States))
            {
                message = "Exista stari finale care nu apartin multimii starilor.";
                return false;
            }

            foreach (var kv in Transitions)
            {
                int stareCurenta = kv.Key;
                if (!States.Contains(stareCurenta))
                {
                    message = $"Tabelul de tranzitie contine starea sursa {stareCurenta} care nu e in Q.";
                    return false;
                }

                foreach (var tranzitie in kv.Value)
                {
                    char simbol = tranzitie.Key;
                    int stareDestinatie = tranzitie.Value;

                    if (!Alphabet.Contains(simbol))
                    {
                        message = $"Tranzitie prin simbolul '{simbol}' care nu e in Alfabet.";
                        return false;
                    }
                    if (!States.Contains(stareDestinatie))
                    {
                        message = $"Tranzitie catre starea {stareDestinatie} care nu e in Q.";
                        return false;
                    }
                }
            }

            message = "Automatul este valid.";
            return true;
        }

        public void PrintAutomaton(TextWriter writer)
        {
            const int COL_WIDTH = 8;
            string Fmt(object obj)
            {
                string s = obj.ToString();
                if (s.Length > COL_WIDTH - 2) s = s.Substring(0, COL_WIDTH - 2);
                return s.PadRight(COL_WIDTH);
            }

            writer.WriteLine("Deterministic Finite Automaton (M):");
            writer.WriteLine($"Multimea Starilor (Q): {{ {string.Join(", ", States.OrderBy(x => x))} }}");
            writer.WriteLine($"Alfabetul (Sigma): {{ {string.Join(", ", Alphabet.OrderBy(c => c))} }}");
            writer.WriteLine($"Starea Initiala (q0): {InitialState}");
            writer.WriteLine($"Starile Finale (F): {{ {string.Join(", ", FinalStates.OrderBy(x => x))} }}");
            writer.WriteLine();

            var symbols = Alphabet.OrderBy(c => c).ToList();

            writer.Write(Fmt("Stare"));
            foreach (var s in symbols) writer.Write(Fmt(s));
            writer.WriteLine();
            writer.WriteLine(new string('-', COL_WIDTH * (symbols.Count + 1)));

            foreach (var q in States.OrderBy(x => x))
            {
                string label = q.ToString();
                if (q == InitialState) label = "->" + label;
                if (FinalStates.Contains(q)) label += "*";

                writer.Write(Fmt(label));

                foreach (var a in symbols)
                {
                    if (Transitions.TryGetValue(q, out var map) && map.TryGetValue(a, out var t))
                        writer.Write(Fmt(t));
                    else
                        writer.Write(Fmt("-"));
                }
                writer.WriteLine();
            }
        }

        public bool CheckWord(string w)
        {
            int cur = InitialState;
            foreach (char c in w)
            {
                if (!Alphabet.Contains(c)) return false;
                if (!Transitions.TryGetValue(cur, out var map) || !map.TryGetValue(c, out var nxt))
                    return false;
                cur = nxt;
            }
            return FinalStates.Contains(cur);
        }
    }

    public class Transition
    {
        public int From;
        public int To;
        public char Symbol;
        public Transition(int f, char s, int t) { From = f; Symbol = s; To = t; }
    }

    public class NFA
    {
        public int Start;
        public int Final;
        public List<Transition> Transitions = new List<Transition>();
        public HashSet<int> AllStates = new HashSet<int>();
        public HashSet<char> Alphabet = new HashSet<char>();

        public void AddTrans(int f, char s, int t)
        {
            Transitions.Add(new Transition(f, s, t));
            AllStates.Add(f); AllStates.Add(t);
            if (s != '\0') Alphabet.Add(s);
        }
    }

    public class SyntaxNode
    {
        public string Value;
        public SyntaxNode Left, Right;
    }

    public static class AutomataBuilder
    {
        const char LAMBDA = '\0';
        const char CONCAT = '.';
        const char UNION = '|';
        const char STAR = '*';
        const char PLUS = '+';
        const char QUEST = '?';
        const char LPAR = '(';
        const char RPAR = ')';

        public static string ToPostfix(string regex)
        {
            string explicitConcat = InsertExplicitConcat(regex);
            StringBuilder output = new StringBuilder();
            Stack<char> stack = new Stack<char>();

            int Precedence(char c)
            {
                if (c == STAR || c == PLUS || c == QUEST) return 3;
                if (c == CONCAT) return 2;
                if (c == UNION) return 1;
                return 0;
            }

            foreach (char c in explicitConcat)
            {
                if (IsSymbol(c)) output.Append(c);
                else if (c == LPAR) stack.Push(c);
                else if (c == RPAR)
                {
                    while (stack.Count > 0 && stack.Peek() != LPAR) output.Append(stack.Pop());
                    if (stack.Count > 0) stack.Pop();
                }
                else
                {
                    while (stack.Count > 0 && Precedence(stack.Peek()) >= Precedence(c))
                        output.Append(stack.Pop());
                    stack.Push(c);
                }
            }
            while (stack.Count > 0) output.Append(stack.Pop());
            return output.ToString();
        }

        private static bool IsSymbol(char c)
        {
            return c != STAR && c != CONCAT && c != UNION && c != LPAR && c != RPAR && c != PLUS && c != QUEST;
        }

        private static string InsertExplicitConcat(string s)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                sb.Append(s[i]);
                if (i + 1 < s.Length)
                {
                    char c1 = s[i];
                    char c2 = s[i + 1];
                    bool isC1Operand = IsSymbol(c1) || c1 == STAR || c1 == PLUS || c1 == QUEST || c1 == RPAR;
                    bool isC2Operand = IsSymbol(c2) || c2 == LPAR;
                    if (isC1Operand && isC2Operand) sb.Append(CONCAT);
                }
            }
            return sb.ToString();
        }

        private static int stateCounter = 0;
        private static int NewState() => stateCounter++;

        public static NFA BuildNFA(string postfix)
        {
            stateCounter = 0;
            Stack<NFA> stack = new Stack<NFA>();

            foreach (char c in postfix)
            {
                if (IsSymbol(c))
                {
                    NFA frag = new NFA();
                    frag.Start = NewState(); frag.Final = NewState();
                    frag.AddTrans(frag.Start, c, frag.Final);
                    stack.Push(frag);
                }
                else if (c == STAR)
                {
                    NFA a = stack.Pop();
                    NFA frag = new NFA();
                    frag.Start = NewState(); frag.Final = NewState();
                    frag.Transitions.AddRange(a.Transitions);
                    frag.AllStates.UnionWith(a.AllStates);
                    frag.Alphabet.UnionWith(a.Alphabet);

                    frag.AddTrans(frag.Start, LAMBDA, a.Start);
                    frag.AddTrans(a.Final, LAMBDA, frag.Final);
                    frag.AddTrans(a.Final, LAMBDA, a.Start);
                    frag.AddTrans(frag.Start, LAMBDA, frag.Final);
                    stack.Push(frag);
                }
                else if (c == PLUS)
                {
                    NFA a = stack.Pop();
                    NFA frag = new NFA();
                    frag.Start = NewState(); frag.Final = NewState();
                    frag.Transitions.AddRange(a.Transitions);
                    frag.AllStates.UnionWith(a.AllStates);
                    frag.Alphabet.UnionWith(a.Alphabet);

                    frag.AddTrans(frag.Start, LAMBDA, a.Start);
                    frag.AddTrans(a.Final, LAMBDA, frag.Final);
                    frag.AddTrans(a.Final, LAMBDA, a.Start);
                    stack.Push(frag);
                }
                else if (c == QUEST)
                {
                    NFA a = stack.Pop();
                    NFA frag = new NFA();
                    frag.Start = NewState(); frag.Final = NewState();
                    frag.Transitions.AddRange(a.Transitions);
                    frag.AllStates.UnionWith(a.AllStates);
                    frag.Alphabet.UnionWith(a.Alphabet);

                    frag.AddTrans(frag.Start, LAMBDA, a.Start);
                    frag.AddTrans(a.Final, LAMBDA, frag.Final);
                    frag.AddTrans(frag.Start, LAMBDA, frag.Final);
                    stack.Push(frag);
                }
                else if (c == CONCAT)
                {
                    NFA b = stack.Pop(); NFA a = stack.Pop();
                    a.AddTrans(a.Final, LAMBDA, b.Start);
                    a.Final = b.Final;
                    a.Transitions.AddRange(b.Transitions);
                    a.AllStates.UnionWith(b.AllStates);
                    a.Alphabet.UnionWith(b.Alphabet);
                    stack.Push(a);
                }
                else if (c == UNION)
                {
                    NFA b = stack.Pop(); NFA a = stack.Pop();
                    NFA frag = new NFA();
                    frag.Start = NewState(); frag.Final = NewState();
                    frag.Transitions.AddRange(a.Transitions);
                    frag.Transitions.AddRange(b.Transitions);
                    frag.AllStates.UnionWith(a.AllStates);
                    frag.AllStates.UnionWith(b.AllStates);
                    frag.Alphabet.UnionWith(a.Alphabet);
                    frag.Alphabet.UnionWith(b.Alphabet);

                    frag.AddTrans(frag.Start, LAMBDA, a.Start);
                    frag.AddTrans(frag.Start, LAMBDA, b.Start);
                    frag.AddTrans(a.Final, LAMBDA, frag.Final);
                    frag.AddTrans(b.Final, LAMBDA, frag.Final);
                    stack.Push(frag);
                }
            }
            return stack.Pop();
        }

        private static HashSet<int> LambdaClosure(NFA nfa, HashSet<int> states)
        {
            var closure = new HashSet<int>(states);
            var stack = new Stack<int>(states);
            while (stack.Count > 0)
            {
                int p = stack.Pop();
                foreach (var t in nfa.Transitions)
                    if (t.From == p && t.Symbol == LAMBDA && !closure.Contains(t.To))
                    {
                        closure.Add(t.To);
                        stack.Push(t.To);
                    }
            }
            return closure;
        }

        private static HashSet<int> Move(NFA nfa, HashSet<int> states, char symbol)
        {
            var result = new HashSet<int>();
            foreach (int p in states)
                foreach (var t in nfa.Transitions)
                    if (t.From == p && t.Symbol == symbol)
                        result.Add(t.To);
            return result;
        }

        public static DeterministicFiniteAutomaton ConvertNFAtoDFA(NFA nfa)
        {
            var dfa = new DeterministicFiniteAutomaton();
            dfa.Alphabet.UnionWith(nfa.Alphabet);

            var startSet = LambdaClosure(nfa, new HashSet<int> { nfa.Start });
            Dictionary<string, int> setsToId = new Dictionary<string, int>();
            Dictionary<int, HashSet<int>> idToSets = new Dictionary<int, HashSet<int>>();
            Queue<int> unmarked = new Queue<int>();

            string GetKey(HashSet<int> set) => string.Join(",", set.OrderBy(x => x));

            int nextId = 0;
            string startKey = GetKey(startSet);
            setsToId[startKey] = nextId;
            idToSets[nextId] = startSet;

            dfa.InitialState = nextId;
            dfa.States.Add(nextId);
            unmarked.Enqueue(nextId);
            nextId++;

            while (unmarked.Count > 0)
            {
                int curDfaState = unmarked.Dequeue();
                HashSet<int> curNfaStates = idToSets[curDfaState];

                if (curNfaStates.Contains(nfa.Final)) dfa.FinalStates.Add(curDfaState);
                if (!dfa.Transitions.ContainsKey(curDfaState)) dfa.Transitions[curDfaState] = new Dictionary<char, int>();

                foreach (char sym in dfa.Alphabet)
                {
                    var targetSet = LambdaClosure(nfa, Move(nfa, curNfaStates, sym));
                    if (targetSet.Count > 0)
                    {
                        string key = GetKey(targetSet);
                        if (!setsToId.ContainsKey(key))
                        {
                            setsToId[key] = nextId;
                            idToSets[nextId] = targetSet;
                            dfa.States.Add(nextId);
                            unmarked.Enqueue(nextId);
                            nextId++;
                        }
                        dfa.Transitions[curDfaState][sym] = setsToId[key];
                    }
                }
            }
            return dfa;
        }

        public static DeterministicFiniteAutomaton RegexToDFA(string regex)
        {
            string postfix = ToPostfix(regex);
            NFA nfa = BuildNFA(postfix);
            return ConvertNFAtoDFA(nfa);
        }

        public static void PrintSyntaxTree(string postfix, TextWriter writer)
        {
            Stack<SyntaxNode> stack = new Stack<SyntaxNode>();
            foreach (char c in postfix)
            {
                if (IsSymbol(c))
                {
                    stack.Push(new SyntaxNode { Value = c.ToString() });
                }
                else
                {
                    var node = new SyntaxNode { Value = c.ToString() };
                    if (c == STAR || c == PLUS || c == QUEST)
                    {
                        if (stack.Count > 0) node.Left = stack.Pop();
                    }
                    else
                    {
                        if (stack.Count > 0) node.Right = stack.Pop();
                        if (stack.Count > 0) node.Left = stack.Pop();
                    }
                    stack.Push(node);
                }
            }
            if (stack.Count > 0) PrintTreeRecursive(stack.Pop(), "", true, writer);
        }

        private static void PrintTreeRecursive(SyntaxNode node, string indent, bool last, TextWriter writer)
        {
            if (node == null) return;
            writer.Write(indent);
            writer.Write(last ? "└─" : "├─");
            writer.WriteLine(node.Value);
            indent += last ? "  " : "│ ";

            var children = new List<SyntaxNode>();
            if (node.Left != null) children.Add(node.Left);
            if (node.Right != null) children.Add(node.Right);

            for (int i = 0; i < children.Count; i++)
                PrintTreeRecursive(children[i], indent, i == children.Count - 1, writer);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            string inputFile = "regex_input.txt";
            string outputFile = "dfa_output.txt";
            string regex;

            try
            {
                regex = File.ReadAllText(inputFile).Trim();
                if (string.IsNullOrEmpty(regex)) throw new Exception();
            }
            catch
            {
                Console.WriteLine($"Nu s-a putut citi '{inputFile}'. Introduceti regex manual:");
                Console.Write("> ");
                regex = Console.ReadLine() ?? "";
            }
            regex = regex.Trim();
            Console.WriteLine($"Regex Citit: {regex}");

            DeterministicFiniteAutomaton dfa = null;
            try
            {
                dfa = AutomataBuilder.RegexToDFA(regex);
                Console.WriteLine("\n[INFO] Automat generat cu succes.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Eroare generare automat: " + ex.Message);
                return;
            }

            while (true)
            {
                Console.WriteLine("\n================ MENU ================");
                Console.WriteLine("1. Afiseaza Forma Poloneza Postfixata");
                Console.WriteLine("2. Afiseaza Arbore Sintactic");
                Console.WriteLine("3. Afiseaza Automat M (Consola + Fisier)");
                Console.WriteLine("4. Verifica Cuvinte");
                Console.WriteLine("5. Verifica Validitate (VerifyAutomaton)");
                Console.WriteLine("0. Iesire");
                Console.WriteLine("======================================");
                Console.Write("Optiune: ");

                string choice = Console.ReadLine()?.Trim();
                if (choice == "0") break;

                switch (choice)
                {
                    case "1":
                        Console.WriteLine($"\nPostfix: {AutomataBuilder.ToPostfix(regex)}");
                        break;
                    case "2":
                        Console.WriteLine("\nArbore Sintactic:");
                        AutomataBuilder.PrintSyntaxTree(AutomataBuilder.ToPostfix(regex), Console.Out);
                        break;
                    case "3":
                        Console.WriteLine();
                        dfa.PrintAutomaton(Console.Out);
                        using (StreamWriter sw = new StreamWriter(outputFile))
                        {
                            dfa.PrintAutomaton(sw);
                        }
                        Console.WriteLine($"\n[INFO] Automatul a fost salvat si in '{outputFile}'.");
                        break;
                    case "4":
                        Console.WriteLine("\n--- Verificare Cuvinte ---");
                        Console.WriteLine("Tip: Apasa Enter pentru cuvant vid. Scrie '/quit' pentru meniu.");
                        while (true)
                        {
                            Console.Write("Cuvant > ");
                            string w = Console.ReadLine();
                            if (w == null || w.Trim() == "/quit") break;
                            bool ok = dfa.CheckWord(w);
                            Console.WriteLine($"'{w}' -> {(ok ? "ACCEPTAT" : "RESPINS")}");
                        }
                        break;
                    case "5":
                        if (dfa.VerifyAutomaton(out string msg)) Console.WriteLine("OK: " + msg);
                        else Console.WriteLine("INVALID: " + msg);
                        break;
                    default:
                        Console.WriteLine("Optiune invalida.");
                        break;
                }
            }
        }
    }
}