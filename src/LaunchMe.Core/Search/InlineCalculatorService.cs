using System.Globalization;
using LaunchMe.Contracts.Search;

namespace LaunchMe.Core.Search;

public sealed class InlineCalculatorService : ICalculatorService
{
    public CalculationResult? Evaluate(string expression)
    {
        // Rejection of empty or excessively long expressions to prevent denial of service (DoS)
        if (string.IsNullOrWhiteSpace(expression) || expression.Length > 256)
        {
            return new CalculationResult
            {
                Expression = expression,
                IsValid = false
            };
        }

        try
        {
            var tokens = Tokenize(expression);
            var parser = new MathParser(tokens);
            var value = parser.Parse();

            return new CalculationResult
            {
                Expression = expression,
                Result = value,
                FormattedResult = FormatResult(value),
                IsValid = true
            };
        }
        catch
        {
            return new CalculationResult
            {
                Expression = expression,
                IsValid = false
            };
        }
    }

    private static string FormatResult(double value)
    {
        if (double.IsNaN(value)) return "NaN";
        if (double.IsPositiveInfinity(value)) return "Infinity";
        if (double.IsNegativeInfinity(value)) return "-Infinity";

        return (value % 1 == 0)
            ? ((long)value).ToString(CultureInfo.InvariantCulture)
            : Math.Round(value, 8).ToString(CultureInfo.InvariantCulture);
    }

    private static List<string> Tokenize(string expr)
    {
        var tokens = new List<string>();
        var i = 0;
        var clean = expr.Trim();

        while (i < clean.Length)
        {
            var c = clean[i];
            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            if (char.IsDigit(c) || c == '.')
            {
                var start = i;
                while (i < clean.Length && (char.IsDigit(clean[i]) || clean[i] == '.'))
                {
                    i++;
                }
                tokens.Add(clean[start..i]);
                continue;
            }

            if (char.IsLetter(c))
            {
                var start = i;
                while (i < clean.Length && char.IsLetter(clean[i]))
                {
                    i++;
                }
                tokens.Add(clean[start..i].ToLowerInvariant());
                continue;
            }

            if ("+-*/^%(),".Contains(c))
            {
                tokens.Add(c.ToString());
                i++;
                continue;
            }

            throw new FormatException($"Unexpected character: '{c}'");
        }

        return tokens;
    }

    private sealed class MathParser
    {
        private const int MaxRecursionDepth = 32;
        private readonly List<string> _tokens;
        private int _pos;
        private int _depth;

        public MathParser(List<string> tokens)
        {
            _tokens = tokens;
        }

        public double Parse()
        {
            if (_tokens.Count == 0) throw new InvalidOperationException("No tokens to parse.");
            var result = ParseExpression();
            if (_pos < _tokens.Count)
            {
                throw new InvalidOperationException($"Unexpected token: '{_tokens[_pos]}'");
            }
            return result;
        }

        private double ParseExpression()
        {
            if (++_depth > MaxRecursionDepth)
            {
                throw new InvalidOperationException("Expression exceeds maximum allowed recursion depth.");
            }

            try
            {
                var left = ParseTerm();
                while (_pos < _tokens.Count && (_tokens[_pos] == "+" || _tokens[_pos] == "-"))
                {
                    var op = _tokens[_pos++];
                    var right = ParseTerm();
                    left = op == "+" ? left + right : left - right;
                }
                return left;
            }
            finally
            {
                _depth--;
            }
        }

        private double ParseTerm()
        {
            var left = ParseFactor();
            while (_pos < _tokens.Count && (_tokens[_pos] == "*" || _tokens[_pos] == "/" || _tokens[_pos] == "%"))
            {
                var op = _tokens[_pos++];
                var right = ParseFactor();
                left = op switch
                {
                    "*" => left * right,
                    "/" => right == 0 ? throw new DivideByZeroException("Division by zero.") : left / right,
                    "%" => left % right,
                    _ => left
                };
            }
            return left;
        }

        private double ParseFactor()
        {
            var left = ParsePrimary();
            while (_pos < _tokens.Count && _tokens[_pos] == "^")
            {
                _pos++;
                var right = ParseFactor();
                left = Math.Pow(left, right);
            }
            return left;
        }

        private double ParsePrimary()
        {
            if (_pos >= _tokens.Count) throw new InvalidOperationException("Unexpected end of expression.");

            var token = _tokens[_pos++];

            if (token == "-")
            {
                return -ParsePrimary();
            }

            if (token == "+")
            {
                return ParsePrimary();
            }

            if (token == "(")
            {
                var val = ParseExpression();
                if (_pos >= _tokens.Count || _tokens[_pos++] != ")")
                {
                    throw new FormatException("Missing closing parenthesis ')'.");
                }
                return val;
            }

            if (token == "pi") return Math.PI;
            if (token == "e") return Math.E;

            if (token is "sqrt" or "abs" or "sin" or "cos" or "tan" or "log" or "ceil" or "floor")
            {
                if (_pos >= _tokens.Count || _tokens[_pos++] != "(")
                {
                    throw new FormatException($"Function '{token}' must be followed by '('.");
                }
                var arg = ParseExpression();
                if (_pos >= _tokens.Count || _tokens[_pos++] != ")")
                {
                    throw new FormatException($"Missing ')' after function argument for '{token}'.");
                }

                return token switch
                {
                    "sqrt" => arg < 0 ? throw new ArgumentException("Cannot take sqrt of negative number.") : Math.Sqrt(arg),
                    "abs" => Math.Abs(arg),
                    "sin" => Math.Sin(arg),
                    "cos" => Math.Cos(arg),
                    "tan" => Math.Tan(arg),
                    "log" => Math.Log(arg),
                    "ceil" => Math.Ceiling(arg),
                    "floor" => Math.Floor(arg),
                    _ => arg
                };
            }

            if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var num))
            {
                return num;
            }

            throw new FormatException($"Invalid number or identifier: '{token}'");
        }
    }
}
