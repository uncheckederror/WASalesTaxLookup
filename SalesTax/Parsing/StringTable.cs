using System.Diagnostics;
using System.Text;


namespace SalesTax.Parsing
{
    public class StringTable
    {
        public Dictionary<int, List<string>> Strings = new();

        public string Get(StringBuilder sb)
        {
            int hash = Hash(sb);
            if (!Strings.ContainsKey(hash))
            {
                Strings.Add(hash, new List<string>());
            }
            List<string> lst = Strings[hash];
            string? str = null;
            if (ListContains(lst, sb, ref str))
            {
                return str;
            }
            str = sb.ToString();
            lst.Add(str);
            return str;
        }

        private static bool ListContains(List<string> lst, StringBuilder sb, ref string str)
        {
            for (int x = 0; x < lst.Count; x++)
            {
                if (sb.AreEqual(lst[x]))
                {
                    str = lst[x];
                    return true;
                }
            }
            return false;
        }

        private static int Hash(StringBuilder str)
        {
            int hash = 0;
            int len = str.Length;
            int lenmod4 = len % 4;
            int part;

            Debug.Assert((len - lenmod4) % 4 == 0);
            for (int x = 0; x < len - lenmod4; x += 4)
            {
                part = str[x];

                part |= str[x + 1] << 8;

                part |= str[x + 2] << 16;

                part |= str[x + 3] << 24;

                hash ^= part;
            }
            part = 0;
            if (lenmod4 > 2)
            {
                part |= str[len - 3];
            }
            if (lenmod4 > 1)
            {
                part |= str[len - 2];
            }
            if (lenmod4 > 0)
            {
                part |= str[len - 1];
                hash ^= part;
            }
            if (hash < 0)
            {
                return -hash;
            }
            return hash;
        }
    }
}
