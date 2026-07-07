using System;
using System.Globalization;
using UnityEngine;

namespace Immortal_Switch.Scripts.Level.Stage
{
    public static class StageFormulaEvaluator
    {
        public static float Evaluate(string formula, StageFormulaContext context, float defaultValue = 1f)
        {
            if (string.IsNullOrWhiteSpace(formula))
                return defaultValue;

            try
            {
                Parser parser = new Parser(formula, context);
                return parser.ParseExpression();
            }
            catch (Exception e)
            {
                Debug.LogError($"[StageFormulaEvaluator] Failed formula: {formula}\n{e.Message}");
                return defaultValue;
            }
        }
        
        public static double EvaluateDouble(string formula, StageFormulaContext context, double defaultValue = 0d)
        {
            if (string.IsNullOrWhiteSpace(formula))
                return defaultValue;

            try
            {
                Parser parser = new Parser(formula, context);
                return parser.ParseExpression();
            }
            catch (Exception e)
            {
                Debug.LogError($"[StageFormulaEvaluator] Failed formula: {formula}\n{e.Message}");
                return defaultValue;
            }
        }

        private class Parser
        {
            private readonly string text;
            private readonly StageFormulaContext context;
            private int index;

            public Parser(string text, StageFormulaContext context)
            {
                this.text = text;
                this.context = context;
            }

            public float ParseExpression()
            {
                float value = ParseAddSub();
                SkipWhiteSpace();
                return value;
            }

            private float ParseAddSub()
            {
                float value = ParseMulDiv();

                while (true)
                {
                    SkipWhiteSpace();

                    if (Match('+'))
                    {
                        value += ParseMulDiv();
                    }
                    else if (Match('-'))
                    {
                        value -= ParseMulDiv();
                    }
                    else
                    {
                        return value;
                    }
                }
            }

            private float ParseMulDiv()
            {
                float value = ParsePower();

                while (true)
                {
                    SkipWhiteSpace();

                    if (Match('*'))
                    {
                        value *= ParsePower();
                    }
                    else if (Match('/'))
                    {
                        float divisor = ParsePower();
                        value = Mathf.Approximately(divisor, 0f) ? 0f : value / divisor;
                    }
                    else if (Match('%'))
                    {
                        float divisor = ParsePower();
                        value = Mathf.Approximately(divisor, 0f) ? 0f : value % divisor;
                    }
                    else
                    {
                        return value;
                    }
                }
            }

            private float ParsePower()
            {
                float value = ParseUnary();

                SkipWhiteSpace();

                if (Match('^'))
                {
                    float power = ParsePower();
                    value = Mathf.Pow(value, power);
                }

                return value;
            }

            private float ParseUnary()
            {
                SkipWhiteSpace();

                if (Match('+'))
                    return ParseUnary();

                if (Match('-'))
                    return -ParseUnary();

                return ParsePrimary();
            }

            private float ParsePrimary()
            {
                SkipWhiteSpace();

                if (Match('('))
                {
                    float value = ParseAddSub();
                    Expect(')');
                    return value;
                }

                if (IsLetter(Current))
                {
                    string name = ParseIdentifier();
                    SkipWhiteSpace();

                    if (Match('('))
                        return ParseFunction(name);

                    return ResolveVariable(name);
                }

                return ParseNumber();
            }

            private float ParseFunction(string functionName)
            {
                functionName = functionName.Trim().ToLowerInvariant();

                float a = ParseAddSub();
                SkipWhiteSpace();

                float b = 0f;
                bool hasSecondArg = false;

                if (Match(','))
                {
                    b = ParseAddSub();
                    hasSecondArg = true;
                }

                Expect(')');

                switch (functionName)
                {
                    case "round":
                        return hasSecondArg ? (float)Math.Round(a, Mathf.RoundToInt(b)) : Mathf.Round(a);

                    case "floor":
                        return Mathf.Floor(a);

                    case "ceil":
                    case "ceiling":
                        return Mathf.Ceil(a);

                    case "min":
                        return Mathf.Min(a, b);

                    case "max":
                        return Mathf.Max(a, b);

                    case "abs":
                        return Mathf.Abs(a);

                    case "pow":
                        return Mathf.Pow(a, b);

                    default:
                        throw new Exception($"Unknown function: {functionName}");
                }
            }

            private float ResolveVariable(string name)
            {
                switch (name.Trim().ToLowerInvariant())
                {
                    case "stage":
                    case "globalstage":
                        return context.GlobalStage;

                    case "localstage":
                        return context.LocalStage;

                    case "chapter":
                    case "chapterid":
                        return context.ChapterId;

                    case "chapterindex":
                        return context.ChapterIndex;

                    default:
                        throw new Exception($"Unknown variable: {name}");
                }
            }

            private float ParseNumber()
            {
                SkipWhiteSpace();

                int start = index;

                while (index < text.Length)
                {
                    char c = text[index];

                    if (char.IsDigit(c) || c == '.')
                    {
                        index++;
                        continue;
                    }

                    break;
                }

                if (start == index)
                    throw new Exception($"Expected number at index {index}");

                string numberText = text.Substring(start, index - start);

                if (!float.TryParse(numberText, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                    throw new Exception($"Invalid number: {numberText}");

                return value;
            }

            private string ParseIdentifier()
            {
                int start = index;

                while (index < text.Length)
                {
                    char c = text[index];

                    if (char.IsLetterOrDigit(c) || c == '_')
                    {
                        index++;
                        continue;
                    }

                    break;
                }

                return text.Substring(start, index - start);
            }

            private void SkipWhiteSpace()
            {
                while (index < text.Length && char.IsWhiteSpace(text[index]))
                    index++;
            }

            private bool Match(char c)
            {
                SkipWhiteSpace();

                if (Current != c)
                    return false;

                index++;
                return true;
            }

            private void Expect(char c)
            {
                SkipWhiteSpace();

                if (Current != c)
                    throw new Exception($"Expected '{c}' at index {index}");

                index++;
            }

            private char Current => index < text.Length ? text[index] : '\0';

            private static bool IsLetter(char c)
            {
                return char.IsLetter(c) || c == '_';
            }
        }
    }

    public struct StageFormulaContext
    {
        public int GlobalStage;
        public int LocalStage;
        public int ChapterId;
        public int ChapterIndex;
    }
}