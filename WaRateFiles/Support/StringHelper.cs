/*
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace WaRateFiles.Support
{
	public class StringHelper
	{
		private StringHelper()
		{
		}

		public static int CountOccurancesOf(string str, char ch)
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

		public static bool IsNumeric(string str)
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

		public static bool IsInt(string str)
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

		public static bool EndsWith(string str, string cp)
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

		public static string EnsureTrailingChar(string str, char ch)
		{
			if (str.Length == 0)
			{
				return str;
			}
			if (str[str.Length - 1] != ch)
			{
				return str + ch.ToString();
			}
			return str;
		}

		public static bool EndsWith(StringBuilder str, string cp)
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

		public static bool AreEqual(StringBuilder sb, string str)
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

		public static string MidStr(string str, int start, int stop)
		{
			return str.Substring(start, stop - start);
		}

	}
}
