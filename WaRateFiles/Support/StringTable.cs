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
using System.Diagnostics;
using System.Text;

namespace WaRateFiles.Support
{
	public class StringTable
	{
		private Dictionary<int, List<string>> m_strings = new Dictionary<int, List<string>>();

		public StringTable()
		{
		}

		public string Get(StringBuilder sb)
		{
			int hash = Hash(sb);
			if (!m_strings.ContainsKey(hash))
			{
				m_strings.Add(hash, new List<string>());
			}
			List<string> lst = m_strings[hash];
			string str = null;
			if (ListContains(lst, sb, ref str))
			{
				return str;
			}
			str = sb.ToString();
			lst.Add(str);
			return str;
		}

		private bool ListContains(List<string> lst, StringBuilder sb, ref string str)
		{
			for (int x = 0; x < lst.Count; x++)
			{
				if (StringHelper.AreEqual(sb, lst[x]))
				{
					str = lst[x];
					return true;
				}
			}
			return false;
		}

		public string Get(string str)
		{
			int hash = Hash(str);
			if (!m_strings.ContainsKey(hash))
			{
				m_strings.Add(hash, new List<string>());
			}
			List<string> lst = m_strings[hash];
			if (ListContains(lst, ref str))
			{
				return str;
			}
			lst.Add(str);
			return str;
		}

		private bool ListContains(List<string> lst, ref string str)
		{
			for (int x = 0; x < lst.Count; x++)
			{
				if (lst[x].Equals(str))
				{
					str = lst[x];
					return true;
				}
			}
			return false;
		}

		private static int Hash(string str)
		{
			int hash = 0;
			int len = str.Length;
			int lenmod4 = len % 4;
			int part;

			Debug.Assert((len - lenmod4) % 4 == 0);
			for (int x = 0; x < len - lenmod4; x += 4)
			{
				part = str[x];

				part |= (int)str[x + 1] << 8;

				part |= (int)str[x + 2] << 16;

				part |= (int)str[x + 3] << 24;

				hash ^= part;
			}
			part = 0;
			if (lenmod4 > 2)
			{
				part |= (int)str[len - 3];
			}
			if (lenmod4 > 1)
			{
				part |= (int)str[len - 2];
			}
			if (lenmod4 > 0)
			{
				part |= (int)str[len - 1];
				hash ^= part;
			}
			if (hash < 0)
			{
				return -hash;
			}
			return hash;
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

				part |= (int)str[x + 1] << 8;

				part |= (int)str[x + 2] << 16;

				part |= (int)str[x + 3] << 24;

				hash ^= part;
			}
			part = 0;
			if (lenmod4 > 2)
			{
				part |= (int)str[len - 3];
			}
			if (lenmod4 > 1)
			{
				part |= (int)str[len - 2];
			}
			if (lenmod4 > 0)
			{
				part |= (int)str[len - 1];
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
