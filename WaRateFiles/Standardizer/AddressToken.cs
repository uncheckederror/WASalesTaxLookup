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

namespace WaRateFiles.Standardizer
{
	/// <summary>
	/// This identifies the postal address component type.  It could be converted
	/// to orinary integers instead of bits.
	/// </summary>
	internal enum StreetToken
	{
		UNKNOWN = 0,
		HOUSE = (1 << 0),
		PREDIR = (1 << 1),
		PRETYPE = (1 << 2),
		STREETQUALIF = (1 << 3),
		STREETPRE = (1 << 4),
		STREET = (1 << 5),
		STREETTYPE = (1 << 6),
		SUFDIR = (1 << 7),
		UNITTYPE = (1 << 8),
		UNITNUM = (1 << 9),
		COLLASE = (1 << 10)
	}

	/// <summary>
	/// A postal component of an address.
	/// </summary>
	internal class AddressToken
	{
		private string m_lexum;
		private LexTokenType m_lextoken;
		private StreetToken m_possibleTokens;
		private StreetToken m_resultToken;

		private static LexiconNormalDirectional m_wordsNormalDir = new LexiconNormalDirectional();
		private static LexiconDirectional m_wordsDir = new LexiconDirectional();
		private static LexiconCommonRoads m_roads = new LexiconCommonRoads();
		private static LexiconUspsAbbr m_uspAbbr = new LexiconUspsAbbr();
		private static LexiconSecondaryUnit m_sunit = new LexiconSecondaryUnit();
		private static LexiconOrdinalWord m_ordwords = new LexiconOrdinalWord();

		public string Lexum
		{
			get { return m_lexum; }
			set { m_lexum = value; }
		}

		public LexTokenType LexToken
		{
			get { return m_lextoken; }
		}

		public int PossibleTokens
		{
			get { return (int)m_possibleTokens; }
		}

		public StreetToken ResultToken
		{
			get { return m_resultToken; }
			set { m_resultToken = value; }
		}

		public AddressToken(string lexum, LexTokenType token)
		{
			Init(lexum, token);
		}

		public void Init(string lexum, LexTokenType token)
		{
			m_lexum = lexum;
			m_lextoken = token;
			m_resultToken = StreetToken.UNKNOWN;

			switch (m_lextoken)
			{
				case LexTokenType.ADDRLEX_AMP:
					m_possibleTokens = StreetToken.COLLASE;
					m_resultToken = StreetToken.COLLASE;
					break;
				case LexTokenType.ADDRLEX_DASH:
					m_possibleTokens = StreetToken.COLLASE;
					m_resultToken = StreetToken.COLLASE;
					break;
				case LexTokenType.ADDRLEX_ONECHAR:
					m_possibleTokens = StreetToken.PREDIR | StreetToken.SUFDIR;
					break;
				case LexTokenType.ADDRLEX_TWOCHAR:
					m_possibleTokens = StreetToken.PREDIR | StreetToken.SUFDIR | StreetToken.STREETTYPE;
					break;
				case LexTokenType.ADDRLEX_FRACTION:
					m_possibleTokens = StreetToken.HOUSE | StreetToken.UNITNUM;
					break;
				case LexTokenType.ADDRLEX_ALPHA:
					m_possibleTokens = StreetToken.STREET | StreetToken.STREETTYPE | StreetToken.UNITTYPE;
					break;
				case LexTokenType.ADDRLEX_ALPHANUM:
					m_possibleTokens = StreetToken.UNITNUM;
					break;
				case LexTokenType.ADDRLEX_NUM:
					m_possibleTokens = StreetToken.HOUSE | StreetToken.STREET | StreetToken.UNITNUM;
					break;
				case LexTokenType.ADDRLEX_ORDINAL:
					m_possibleTokens = StreetToken.STREET | StreetToken.HOUSE;
					break;
				default:
					throw new Exception("Internal error");
			}
		}

		public bool CouldBe(StreetToken token)
		{
			return ((int)m_possibleTokens & (int)token) != 0;
		}

		public bool IsNormalizedDirectional()
		{
			return m_wordsNormalDir.Contains(m_lexum);
		}

		public bool IsDirectional()
		{
			return m_wordsDir.Contains(m_lexum);
		}

		public bool IsRoadType()
		{
			return m_roads.Contains(m_lexum);
		}
		
		public bool IsUspsAbbr()
		{
			return m_uspAbbr.Contains(m_lexum);
		}

		public bool IsUnit()
		{
			return m_sunit.Contains(m_lexum);
		}

		public bool IsOrdinalWord()
		{
			return m_ordwords.Contains(m_lexum);
		}

		public void NormalizeOrdinalWord()
		{
			m_ordwords.Substitute(ref m_lexum);
		}

		public void NormalizeDirectional()
		{
			m_wordsDir.Substitute(ref m_lexum);
			if (m_lexum.Length == 2)
			{
				m_lextoken = LexTokenType.ADDRLEX_TWOCHAR;
			}
			else if (m_lexum.Length == 1)
			{
				m_lextoken = LexTokenType.ADDRLEX_ONECHAR;
			}
		}

		public void NormalizeRoadType()
		{
			m_uspAbbr.Substitute(ref m_lexum);
		}

		public void AppendDirectional(AddressToken atok)
		{
			NormalizeDirectional();
			atok.NormalizeDirectional();
			m_lexum += atok.m_lexum;
		}
		
		public void Append(AddressToken atok)
		{
			m_lexum += " " + atok.m_lexum;
			m_lextoken = LexTokenType.ADDRLEX_ALPHA;
		}

		public void ToNumeric()
		{
			string lex = "";
			for ( int x = 0; x < m_lexum.Length; x++ )
			{
				if ( Char.IsDigit(m_lexum[x]) )
				{
					lex += m_lexum[x];
				}
			}
			m_lexum = lex;
			m_lextoken = LexTokenType.ADDRLEX_NUM;
		}

		public void ToOrdinal()
		{
			Debug.Assert(m_lextoken == LexTokenType.ADDRLEX_NUM);
			switch (m_lexum[m_lexum.Length - 1])
			{
				case '1':
					m_lexum += "ST";
					break;
				case '2':
					m_lexum += "ND";
					break;
				case '3':
					m_lexum += "RD";
					break;
				default:
					m_lexum += "TH";
					break;
			}
			m_lextoken = LexTokenType.ADDRLEX_ORDINAL;
		}
	}
}
