// a C# program to visualize 32 bit signed bit operations, like >>, &, |, ^, ~, etc. The program takes a the expression as a parameter and then visualizes step-by-step what happens in binary representation.
// GPT-3 output (manually modified):

public class BitOperationsVisualizer
{
    public static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: visualize <expression>");
            Console.WriteLine("Example: visualize (--tree|--interactive) \"~(1 << 31)\"");
            // expected output:
            // 00000000_00000000_00000000_00000001
            // <<
            // 31
            // = 
            // 10000000_00000000_00000000_00000000
            // ~
            // 01111111_11111111_11111111_11111111
            return;
        }
        bool interactive = false;
        string expression = args[0];
        if (args[0].Equals("--tree", StringComparison.OrdinalIgnoreCase))
        {
            ExpressionParser.TreeMode = true;
            expression = args[1];
        }
        else if (args[0].Equals("--interactive", StringComparison.OrdinalIgnoreCase))
        {
            interactive = true;
            Console.Clear();
        }
        IExpression root;
        while (interactive)
        {
            try
            {
                Console.Write("> ");
                expression = Console.ReadLine();
                if (expression == null)
                {
                    continue;
                }
                if (expression.Trim() == "cls")
                {
                    Console.Clear();
                    continue;
                }
                else if (expression.Trim() == "exit")
                {
                    return;
                }
                else if (expression.Trim() == "tree-mode=on")
                {
                    ExpressionParser.TreeMode = true;
                    continue;
                }
                else if (expression.Trim() == "tree-mode=off")
                {
                    ExpressionParser.TreeMode = false;
                    continue;
                }
                else if (expression.Trim() == "help")
                {
                    Console.WriteLine("Commands:");
                    Console.WriteLine("  cls - clear the screen");
                    Console.WriteLine("  exit - exit the program");
                    Console.WriteLine("  tree-mode=on - enable tree mode");
                    Console.WriteLine("  tree-mode=off - disable tree mode");
                    Console.WriteLine("  help - show this help");
                    continue;
                }
                root = ExpressionParser.Parse(expression);
                root.VisualizeSteps(0);
                Console.WriteLine();
            }
            catch (KeyNotFoundException e)
            {
                Console.WriteLine(e.Message);
            }
        }
        root = ExpressionParser.Parse(expression);
        root.VisualizeSteps(0);
    }
}

public interface IExpression
{
    /// <summary>
    /// Visualizes the steps taken during evaluation and displays intermediate results as a 32 bit signed binary integer with '_' byte delimiters.
    /// </summary>
    public void VisualizeSteps(int depth);

    public int Evaluate();
}

public static class ExpressionParser
{
    private static readonly List<VariableDefinition> _variables = new();

    public static bool TreeMode { get; set; } = false;

    public static IExpression Parse(string expression)
    {
        int index = 0;
        return Parse(expression, ref index);
    }

    public static IExpression Parse(string expression, ref int index)
    {
        // replace all whitespace with nothing
        expression = expression.Replace(" ", "");

        IExpression result = null;

        while (index < expression.Length)
        {
            result = ParseNext(expression, result, ref index);
        }

        return result;
    }

    public static IExpression ParseNext(string expression, IExpression left, ref int index)
    {
        char c = expression[index];
        if (char.IsLetter(c) && index == 0)
        {
            int start = index;
            while (index < expression.Length && char.IsLetter(expression[index]))
            {
                index++;
            }
            string name = expression.Substring(start, index - start);
            if (index < expression.Length && expression[index] == '=')
            {
                index++;
                string innerExpression = expression[index..];
                index += innerExpression.Length;
                VariableDefinition variable = new(name, Parse(innerExpression));
                _variables.RemoveAll(var => var.Name == name);
                _variables.Add(variable);
                return variable;
            }
            index = start;
        }
        if (c == '(')
        {
            if (left != null)
            {
                throw new Exception("Unexpected '('");
            }

            int endIndex = expression.LastIndexOf(')');
            if (endIndex == -1)
            {
                throw new Exception("Missing ')'");
            }
            string innerExpression = expression.Substring(index + 1, endIndex - index - 1);
            IExpression bracketExpression = new Brackets(Parse(innerExpression));
            index = endIndex + 1;
            return bracketExpression;
        }
        // parse natural numbers
        else if (char.IsDigit(c))
        {
            int endIndex = index + 1;
            while (endIndex < expression.Length && char.IsDigit(expression[endIndex]))
            {
                endIndex++;
            }
            string numberString = expression.Substring(index, endIndex - index);
            int number = int.Parse(numberString);
            index = endIndex;
            return new Number(number);
        }
        // Variable reference
        else if (char.IsLetter(c))
        {
            int endIndex = index + 1;
            while (endIndex < expression.Length && char.IsLetter(expression[endIndex]))
            {
                endIndex++;
            }
            string name = expression.Substring(index, endIndex - index);
            index = endIndex;
            return _variables.SingleOrDefault(var => var.Name == name) ?? throw new KeyNotFoundException($"Variable '{name}' was undefined.");
        }
        // parse unary operators (~, -)
        else if (c == '~')
        {
            if (left != null)
            {
                throw new Exception("Unexpected '~'");
            }
            index++;
            IExpression right = ParseNext(expression, null, ref index);
            return new NotOperator(right);
        }
        else if (c == '-' && left == null)
        {
            index++;
            IExpression right = ParseNext(expression, null, ref index);
            return new NegateOperator(right);
        }
        // parse binary operators (&, |, ^, *, +, -, <<, >>)
        else if (c is '&' or '|' or '^' or '*' or '+' or '-' or '<' or '>')
        {
            if (left == null)
            {
                throw new Exception("Unexpected '" + c + "'");
            }
            index++;
            if (c is '<' or '>')
            {
                if (expression[index] != c)
                {
                    throw new Exception("Unexpected '" + c + "'");
                }
                index++;
            }
            IExpression right = ParseNext(expression, null, ref index);
            return c switch
            {
                '&' => new AndOperator(left, right),
                '|' => new OrOperator(left, right),
                '^' => new XorOperator(left, right),
                '*' => new MultiplyOperator(left, right),
                '+' => new AddOperator(left, right),
                '-' => new SubtractOperator(left, right),
                '<' => new ShiftLeftOperator(left, right),
                '>' => new ShiftRightOperator(left, right),
            };
        }
        else
        {
            throw new Exception("Unexpected '" + c + "'");
        }
    }

    public static string ToTwosComplementBinaryString(int value)
    {
        string result = Convert.ToString(value, 2);
        if (value < 0)
        {
            result = result.Substring(result.Length - 32);
        }
        else
        {
            result = result.PadLeft(32, '0');
        }
        return result.Substring(0, 8) + "_" + result.Substring(8, 8) + "_" + result.Substring(16, 8) + "_" + result.Substring(24, 8);
    }

    public static void PrintBinaryStringColored(int value, int depth, string? leftName = null)
    {
        if (TreeMode)
        {
            Console.Write(new string(' ', depth * 2));
        }
        string dec = leftName ?? value.ToString();
        string padded = dec.PadRight(Math.Max(12, 0));
        if (leftName is not null)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
        }
        Console.Write(padded);
        Console.ResetColor();
        Console.Write(" => ");
        string result = ToTwosComplementBinaryString(value);
        for (int i = 0; i < result.Length; i++)
        {
            if (result[i] == '0')
            {
                Console.ForegroundColor = ConsoleColor.Blue;
            }
            else if (result[i] == '1')
            {
                Console.ForegroundColor = ConsoleColor.Green;
            }
            Console.Write(result[i]);
            Console.ResetColor();
        }
        Console.WriteLine();
    }

    public static void PrintLineAtDepth(string text, int depth, ConsoleColor consoleColor)
    {
        Console.ForegroundColor = consoleColor;
        if (TreeMode)
        {
            Console.Write(new string(' ', depth * 2));
        }
        Console.WriteLine(text);
        Console.ResetColor();
    }

    public static void PrintAtDepth(string text, int depth, ConsoleColor consoleColor)
    {
        Console.ForegroundColor = consoleColor;
        if (TreeMode)
        {
            Console.Write(new string(' ', depth * 2));
        }
        Console.Write(text);
        Console.ResetColor();
    }
}

public class Number : IExpression
{
    public int Value { get; }

    public Number(int value)
    {
        Value = value;
    }

    public void VisualizeSteps(int depth)
    {
        ExpressionParser.PrintBinaryStringColored(Value, depth);
    }

    public int Evaluate()
    {
        return Value;
    }
}

public class Brackets : IExpression
{
    public IExpression Expression { get; }

    public Brackets(IExpression expression)
    {
        Expression = expression;
    }

    public void VisualizeSteps(int depth)
    {
        //ExpressionParser.PrintAtDepth("(", depth, ConsoleColor.Red);
        Expression.VisualizeSteps(depth);
        //ExpressionParser.PrintAtDepth(")", depth, ConsoleColor.Red);
    }

    public int Evaluate()
    {
        return Expression.Evaluate();
    }
}

public abstract class BinaryOperator : IExpression
{
    public IExpression Left { get; }
    public IExpression Right { get; }

    public BinaryOperator(IExpression left, IExpression right)
    {
        Left = left;
        Right = right;
    }

    public virtual void VisualizeSteps(int depth)
    {
        Left.VisualizeSteps(depth + 1);
        ExpressionParser.PrintLineAtDepth(Operator, depth, ConsoleColor.Red);
        Right.VisualizeSteps(depth + 1);
        ExpressionParser.PrintLineAtDepth(new string('-', 51), depth, ConsoleColor.White);
        ExpressionParser.PrintBinaryStringColored(Evaluate(), depth);
    }

    public abstract string Operator { get; }
    public abstract int Evaluate();
}

public class AndOperator : BinaryOperator
{
    public AndOperator(IExpression left, IExpression right) : base(left, right) { }

    public override string Operator => "&";
    public override int Evaluate() => Left.Evaluate() & Right.Evaluate();
}

public class OrOperator : BinaryOperator
{
    public OrOperator(IExpression left, IExpression right) : base(left, right) { }

    public override string Operator => "|";
    public override int Evaluate() => Left.Evaluate() | Right.Evaluate();
}

public class XorOperator : BinaryOperator
{
    public XorOperator(IExpression left, IExpression right) : base(left, right) { }

    public override string Operator => "^";
    public override int Evaluate() => Left.Evaluate() ^ Right.Evaluate();
}

public class MultiplyOperator : BinaryOperator
{
    public MultiplyOperator(IExpression left, IExpression right) : base(left, right) { }

    public override string Operator => "*";
    public override int Evaluate() => Left.Evaluate() * Right.Evaluate();
}

public class AddOperator : BinaryOperator
{
    public AddOperator(IExpression left, IExpression right) : base(left, right) { }

    public override string Operator => "+";
    public override int Evaluate() => Left.Evaluate() + Right.Evaluate();
}

public class SubtractOperator : BinaryOperator
{
    public SubtractOperator(IExpression left, IExpression right) : base(left, right) { }

    public override string Operator => "-";
    public override int Evaluate() => Left.Evaluate() - Right.Evaluate();
}

public abstract class ShiftOperator : BinaryOperator
{
    public ShiftOperator(IExpression left, IExpression right) : base(left, right) { }

    public override void VisualizeSteps(int depth)
    {
        Left.VisualizeSteps(depth + 1);
        if (Right is Number number)
        {
            ExpressionParser.PrintLineAtDepth(Operator + " " + number.Value, depth, ConsoleColor.Red);
        }
        else
        {
            ExpressionParser.PrintLineAtDepth(Operator, depth, ConsoleColor.Red);
            Right.VisualizeSteps(depth + 1);
        }
        ExpressionParser.PrintLineAtDepth(new string('-', 51), depth, ConsoleColor.White);
        ExpressionParser.PrintBinaryStringColored(Evaluate(), depth);
    }
}

public class ShiftLeftOperator : ShiftOperator
{
    public ShiftLeftOperator(IExpression left, IExpression right) : base(left, right) { }

    public override string Operator => "<<";
    
    public override int Evaluate() => Left.Evaluate() << Right.Evaluate();
}

public class ShiftRightOperator : ShiftOperator
{
    public ShiftRightOperator(IExpression left, IExpression right) : base(left, right) { }

    public override string Operator => ">>";
    public override int Evaluate() => Left.Evaluate() >> Right.Evaluate();
}

public class NotOperator : IExpression
{
    public IExpression Expression { get; }

    public NotOperator(IExpression expression)
    {
        Expression = expression;
    }

    public void VisualizeSteps(int depth)
    {
        Expression.VisualizeSteps(depth + 1);
        ExpressionParser.PrintLineAtDepth("~", depth, ConsoleColor.Red);
        ExpressionParser.PrintLineAtDepth(new string('-', 51), depth, ConsoleColor.White);
        ExpressionParser.PrintBinaryStringColored(~Expression.Evaluate(), depth);
    }

    public int Evaluate() => ~Expression.Evaluate();
}

public class NegateOperator : IExpression
{
    public IExpression Expression { get; }

    public NegateOperator(IExpression expression)
    {
        Expression = expression;
    }

    public void VisualizeSteps(int depth)
    {
        if (Expression is not Number)
        {
            Expression.VisualizeSteps(depth + 1);
            ExpressionParser.PrintLineAtDepth("-", depth, ConsoleColor.Red);
            ExpressionParser.PrintLineAtDepth(new string('-', 51), depth, ConsoleColor.White);
        }
        ExpressionParser.PrintBinaryStringColored(-Expression.Evaluate(), depth);
    }

    public int Evaluate() => -Expression.Evaluate();
}

public class VariableDefinition : IExpression
{
    public string Name { get; }

    public Number? Value { get; private set; }

    public IExpression Expression { get; }

    public VariableDefinition(string name, IExpression expression)
    {
        Name = name;
        Expression = expression;
    }

    public void VisualizeSteps(int depth)
    {
        if (Value == null)
        {
            Expression.VisualizeSteps(depth + 1);
            ExpressionParser.PrintLineAtDepth(new string('-', 51), depth, ConsoleColor.White);
            ExpressionParser.PrintBinaryStringColored(Evaluate(), depth, Name);
            Value = new Number(Expression.Evaluate());
        }
        else
        {
            Value.VisualizeSteps(depth);
        }
    }

    public int Evaluate()
    {
        return Value?.Value ?? Expression.Evaluate();
    }
}