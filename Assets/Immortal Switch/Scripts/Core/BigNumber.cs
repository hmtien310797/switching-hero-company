using System;
using System.Globalization;

public static class AlphabetSuffix
{
    // tier: 0 => "", 1 => a, 26 => z, 27 => aa ...
    public static string FromTier(int tier)
    {
        if (tier <= 0) return "";
        int n = tier - 1;
        string result = "";
        while (n >= 0)
        {
            int r = n % 26;
            result = (char)('a' + r) + result;
            n = (n / 26) - 1;
        }
        return result;
    }

    public static bool TryToTier(string suffix, out int tier)
    {
        tier = 0;
        if (string.IsNullOrWhiteSpace(suffix)) return true;

        suffix = suffix.Trim().ToLowerInvariant();
        for (int i = 0; i < suffix.Length; i++)
            if (suffix[i] < 'a' || suffix[i] > 'z') return false;

        int value = 0;
        for (int i = 0; i < suffix.Length; i++)
        {
            int digit = (suffix[i] - 'a') + 1; // a=1..z=26
            value = value * 26 + digit;
        }
        tier = value;
        return true;
    }
}

[Serializable]
public struct BigNumber : IComparable<BigNumber>, IEquatable<BigNumber>
{
    // Value = Mantissa * 1000^Tier
    // Mantissa normalized in [1, 1000) or 0
    public double Mantissa;
    public int Tier;

    // Nếu lệch tier quá lớn, phép + - bỏ qua số nhỏ (tối ưu & ổn định)
    public const int AddSubIgnoreTierDiff = 6; // 1000^6 = 1e18 tương đối hợp lý

    public static readonly BigNumber Zero = new BigNumber(0, 0, normalize: false);
    public static readonly BigNumber One = new BigNumber(1, 0);

    public BigNumber(double mantissa, int tier, bool normalize = true)
    {
        Mantissa = mantissa;
        Tier = tier;
        if (normalize) Normalize();
    }

    public bool IsZero => Mantissa == 0;

    public void Normalize()
    {
        if (Mantissa == 0 || double.IsNaN(Mantissa) || double.IsInfinity(Mantissa))
        {
            if (Mantissa == 0) Tier = 0;
            return;
        }

        double abs = Math.Abs(Mantissa);

        // Bring to [1, 1000)
        while (abs >= 1000.0)
        {
            Mantissa /= 1000.0;
            Tier += 1;
            abs = Math.Abs(Mantissa);
        }

        while (abs > 0 && abs < 1.0)
        {
            Mantissa *= 1000.0;
            Tier -= 1;
            abs = Math.Abs(Mantissa);
        }

        // Avoid negative tier going too far for tiny numbers: convert to normal tier 0 when possible
        if (Tier < 0)
        {
            // e.g. 0.5 with Tier -1 => 0.0005 tier 0
            Mantissa *= Pow1000(Tier);
            Tier = 0;
        }

        if (Mantissa == 0) Tier = 0;
    }

    private static double Pow1000(int tier)
    {
        return Math.Pow(1000.0, tier);
    }
    
    public static bool TryParse(string input, out BigNumber result)
    {
        result = Zero;
        if (string.IsNullOrWhiteSpace(input)) return false;

        string s = input.Trim();
        s = s.Replace("_", "").Replace(" ", "");

        // 1) scientific notation?
        if (s.IndexOf('e') >= 0 || s.IndexOf('E') >= 0)
            return TryParseScientific(s, out result);

        // 2) split numeric + suffix (letters)
        int i = 0;
        while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.' || s[i] == '-' || s[i] == '+' || s[i] == ','))
            i++;

        string numPart = s.Substring(0, i).Replace(",", "");
        string sufPart = s.Substring(i).Trim();

        if (!double.TryParse(numPart, NumberStyles.Float, CultureInfo.InvariantCulture, out double num))
        {
            // If plain numeric is too big for double, fallback to "big integer string parse"
            return TryParsePlainHugeNumberString(numPart, sufPart, out result);
        }

        // plain number with no suffix
        if (string.IsNullOrEmpty(sufPart))
        {
            result = FromDouble(num);
            return true;
        }

        // alphabet suffix
        if (!AlphabetSuffix.TryToTier(sufPart, out int tier)) return false;
        result = new BigNumber(num, tier);
        return true;
    }

    private static bool TryParseScientific(string s, out BigNumber result)
    {
        result = Zero;
        s = s.Replace(",", "");
        int ePos = s.IndexOf('e');
        if (ePos < 0) ePos = s.IndexOf('E');
        if (ePos < 0) return false;

        string mPart = s.Substring(0, ePos);
        string ePart = s.Substring(ePos + 1);

        if (!double.TryParse(mPart, NumberStyles.Float, CultureInfo.InvariantCulture, out double m)) return false;
        if (!int.TryParse(ePart, NumberStyles.Integer, CultureInfo.InvariantCulture, out int exp10)) return false;

        int tier = (int)Math.Floor(exp10 / 3.0);
        int rem = exp10 - tier * 3; // 0..2 or negative

        double m2 = m * Math.Pow(10, rem);
        result = new BigNumber(m2, tier);
        return true;
    }

    private static bool TryParsePlainHugeNumberString(string numericOnly, string suffix, out BigNumber result)
    {
        result = Zero;
        
        if (!string.IsNullOrEmpty(suffix))
        {
            if (!AlphabetSuffix.TryToTier(suffix, out int outTier)) return false;
            if (!double.TryParse(numericOnly, NumberStyles.Float, CultureInfo.InvariantCulture, out double m)) return false;
            result = new BigNumber(m, outTier);
            return true;
        }

        // Plain huge integer string like "123456789012345678901234..."
        // We convert by digit length -> tier, and take leading digits for mantissa.
        bool neg = false;
        string s = numericOnly;
        if (s.StartsWith("+")) s = s.Substring(1);
        if (s.StartsWith("-")) { neg = true; s = s.Substring(1); }

        // remove leading zeros
        int p = 0;
        while (p < s.Length && s[p] == '0') p++;
        s = p == s.Length ? "0" : s.Substring(p);

        if (s == "0") { result = Zero; return true; }
        if (s.Contains(".")) return false; // extremely huge decimal string not supported in this fallback

        int digits = s.Length;
        int tier = (digits - 1) / 3;
        int leadCount = digits - tier * 3; // 1..3
        int take = Math.Min(leadCount + 12, digits); // take a bit more for decimals

        string head = s.Substring(0, take);

        // Build mantissa = head / 10^(take - leadCount)
        // Example digits=7 => tier=2? digits 7 => (7-1)/3=2 tier => leadCount=7-6=1 => head=first 13 digits...
        // mantissa ~ first digits with decimals scaled.
        if (!double.TryParse(head, NumberStyles.Integer, CultureInfo.InvariantCulture, out double headNum)) return false;
        int decPlaces = take - leadCount;
        double mantissa = headNum / Math.Pow(10, decPlaces);

        if (neg) mantissa = -mantissa;
        result = new BigNumber(mantissa, tier);
        return true;
    }

    public static BigNumber FromDouble(double value)
    {
        if (value == 0) return Zero;

        double abs = Math.Abs(value);
        int tier = 0;
        while (abs >= 1000.0)
        {
            value /= 1000.0;
            abs /= 1000.0;
            tier++;
        }
        return new BigNumber(value, tier);
    }

    // ---------- Operators ----------
    public static BigNumber operator +(BigNumber a, BigNumber b)
    {
        if (a.IsZero) return b;
        if (b.IsZero) return a;

        int diff = a.Tier - b.Tier;
        if (Math.Abs(diff) > AddSubIgnoreTierDiff)
            return diff > 0 ? a : b;

        if (diff == 0)
            return new BigNumber(a.Mantissa + b.Mantissa, a.Tier);

        if (diff > 0)
        {
            // bring b up to a tier: bMantissa / 1000^diff
            double scaledB = b.Mantissa / Pow1000(diff);
            return new BigNumber(a.Mantissa + scaledB, a.Tier);
        }
        else
        {
            double scaledA = a.Mantissa / Pow1000(-diff);
            return new BigNumber(scaledA + b.Mantissa, b.Tier);
        }
    }

    public static BigNumber operator -(BigNumber a, BigNumber b)
        => a + new BigNumber(-b.Mantissa, b.Tier, normalize: false);

    public static BigNumber operator *(BigNumber a, BigNumber b)
    {
        if (a.IsZero || b.IsZero) return Zero;
        return new BigNumber(a.Mantissa * b.Mantissa, a.Tier + b.Tier);
    }

    public static BigNumber operator /(BigNumber a, BigNumber b)
    {
        if (b.IsZero) throw new DivideByZeroException();
        if (a.IsZero) return Zero;
        return new BigNumber(a.Mantissa / b.Mantissa, a.Tier - b.Tier);
    }

    public static BigNumber operator *(BigNumber a, double k)
        => k == 0 ? Zero : new BigNumber(a.Mantissa * k, a.Tier);

    public static BigNumber operator /(BigNumber a, double k)
        => k == 0 ? throw new DivideByZeroException() : new BigNumber(a.Mantissa / k, a.Tier);

    // ---------- Compare ----------
    public int CompareTo(BigNumber other)
    {
        if (IsZero && other.IsZero) return 0;

        // sign
        if (Mantissa > 0 && other.Mantissa < 0) return 1;
        if (Mantissa < 0 && other.Mantissa > 0) return -1;

        bool positive = Mantissa >= 0;

        if (Tier != other.Tier)
            return positive ? Tier.CompareTo(other.Tier) : other.Tier.CompareTo(Tier);

        return Mantissa.CompareTo(other.Mantissa);
    }

    public bool Equals(BigNumber other)
    {
        if (IsZero && other.IsZero) return true;
        return Tier == other.Tier && Math.Abs(Mantissa - other.Mantissa) < 1e-12;
    }

    public override bool Equals(object obj) => obj is BigNumber bn && Equals(bn);
    public override int GetHashCode() => HashCode.Combine(Math.Round(Mantissa, 12), Tier);

    public static bool operator >(BigNumber a, BigNumber b) => a.CompareTo(b) > 0;
    public static bool operator <(BigNumber a, BigNumber b) => a.CompareTo(b) < 0;
    public static bool operator >=(BigNumber a, BigNumber b) => a.CompareTo(b) >= 0;
    public static bool operator <=(BigNumber a, BigNumber b) => a.CompareTo(b) <= 0;
    public static bool operator ==(BigNumber a, BigNumber b) => a.Equals(b);
    public static bool operator !=(BigNumber a, BigNumber b) => !a.Equals(b);

    // ---------- Formatting ----------
    /// <summary>
    /// minSuffixTier:
    /// 0 => luôn có suffix (tier>=1 => a...)
    /// 1 => từ 1e3 mới suffix
    /// 2 => từ 1e6 mới suffix (khuyến nghị nếu bạn muốn 1e3..1e6 vẫn là số thường)
    /// 3 => từ 1e9 mới suffix ...
    /// </summary>
    public string ToStringAlphabet(
        int decimals = 1,
        int minSuffixTier = 2,
        bool useGroupingSeparators = true)
    {
        if (IsZero) return "0";
        
        if (Tier < minSuffixTier)
        {
            if (Tier <= 5)
            {
                double v = Mantissa * Pow1000(Tier);
                string fmt = useGroupingSeparators ? "N0" : "F0";
                return v.ToString(fmt, CultureInfo.InvariantCulture);
            }
            
            return $"{Mantissa.ToString($"F{decimals}", CultureInfo.InvariantCulture)}e{Tier * 3}";
        }

        string suffix = AlphabetSuffix.FromTier(Tier);
        return Mantissa.ToString($"F{decimals}", CultureInfo.InvariantCulture) + suffix;
    }

    public override string ToString() => ToStringAlphabet(decimals: 1, minSuffixTier: 2);
}