using System;
using System.Text;

namespace WASalesTax.Parsing
{
    public static class StringExtensions
    {
        public static int CountOccurancesOf(this string str, char ch)
        {
            int count = 0;
            for (int x = 0; x < str.Length; x++)
            {
                if (str[x] == ch)
                {
                    count++;
                }
            }
            return count;
        }

        public static bool IsNumeric(this string str)
        {
            if (str.Length == 0)
            {
                return false;
            }
            int dotcount = 0;
            for (int x = 0; x < str.Length; x++)
            {
                char ch = str[x];
                if (ch == '.' && dotcount == 0)
                {
                    dotcount++;
                    continue;
                }
                if (!Char.IsDigit(ch))
                {
                    return false;
                }
            }
            return true;
        }

        public static bool IsInt(this string str)
        {
            if (str.Length == 0)
            {
                return false;
            }
            for (int x = 0; x < str.Length; x++)
            {
                if (!Char.IsDigit(str[x]))
                {
                    return false;
                }
            }
            return true;
        }

        public static bool EndsWith(this string str, string cp)
        {
            int pos = str.Length - cp.Length;
            int cppos = 0;
            int x;

            if (pos <= 0)
            {
                return false;
            }
            for (x = pos; x < str.Length; x++)
            {
                if (str[x] != cp[cppos++])
                {
                    return false;
                }
            }
            return true;
        }

        public static string EnsureTrailingChar(this string str, char ch)
        {
            if (str.Length == 0)
            {
                return str;
            }
            if (str[^1] != ch)
            {
                return str + ch.ToString();
            }
            return str;
        }

        public static bool EndsWith(this StringBuilder str, string cp)
        {
            int pos = str.Length - cp.Length;
            int cppos = 0;
            int x;

            if (pos < 0)
            {
                return false;
            }
            for (x = pos; x < str.Length; x++)
            {
                if (str[x] != cp[cppos++])
                {
                    return false;
                }
            }
            return true;
        }

        public static bool AreEqual(this StringBuilder sb, string str)
        {
            if (sb.Length != str.Length)
            {
                return false;
            }
            for (int x = 0; x < str.Length; x++)
            {
                if (str[x] != sb[x])
                {
                    return false;
                }
            }
            return true;
        }

        public static string MidStr(this string str, int start, int stop)
        {
            return str[start..stop];
        }

    }
}
