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
using System.Diagnostics;
using System.Text;

using WaRateFiles.Support;

namespace WaRateFiles
{
	internal enum LexTokenType
	{
		ADDRLEX_EOF = 0,
		ADDRLEX_AMP = 1,
		ADDRLEX_DASH = 2,
		ADDRLEX_ONECHAR = 3,
		ADDRLEX_TWOCHAR = 4,
		ADDRLEX_FRACTION = 5,
		ADDRLEX_ALPHA = 6,
		ADDRLEX_ALPHANUM = 7,
		ADDRLEX_NUM = 8,
		ADDRLEX_ORDINAL = 9
	}

	/// <summary>
	/// Lexical analyzer for street addresses.
	/// </summary>
	internal class Lex
	{
		private enum LexState
		{
			LEX_START = 0,
			LEX_NUM = 1,
			LEX_ALPHA1 = 3,
			LEX_ALPHA2 = 4,
			LEX_ALPHA = 5,
			LEX_ALPHANUM = 6,
			LEX_ORD_T = 7,
			LEX_ORD_D = 8,
			LEX_ORD_H = 9,
			LEX_ORD_END = 10,
			LEX_FRACT = 11
		}

		private string m_text;
		private int m_pos;
		private StringBuilder m_lex = new StringBuilder();
		private LexState m_state;
		
		public StringBuilder Lexum() 
		{ 
			return m_lex; 
		}	

		void AppendLex(char ch)
		{
			m_lex.Append( ch );
		}
	
		bool IsEOF()
		{
			return m_pos >= m_text.Length;
		}
	
		static bool IsSeperator(char ch)
		{
			return Char.IsWhiteSpace(ch) || ch == '&' || ch == '\0';
		}
	
		public Lex(string text)
		{
			m_text = text;
			m_pos = 0;
			m_state = LexState.LEX_START;
		}
	
		public Lex()
		{
			m_text = "";
			m_state = LexState.LEX_START;
		}
		
		public void Init(string text)
		{
			m_text = text;
			m_pos = 0;
			m_state = LexState.LEX_START;
			m_lex.Length = 0;
		}
				
		public bool NextToken( out LexTokenType token )
		{
			m_lex.Length = 0;

			if (IsEOF())
			{
				token = LexTokenType.ADDRLEX_EOF;
				return true;
			}
			
			m_state = LexState.LEX_START;
			while ( true )
			{
				char ch;
				if ( IsEOF() )
				{
					ch = '\0';
				}
				else
				{
					if ( (ch = Char.ToUpper(m_text[m_pos++])) == '.' || ch == ',' )
					{
						continue;
					}					
				}								
				switch ( m_state )
				{
					case LexState.LEX_START:
						if (Char.IsWhiteSpace(ch))
						{
							continue;
						}
						AppendLex(ch);
						
						if (ch == '-')
						{
							m_state = LexState.LEX_ALPHANUM;
							break;
						}
						if ( ch == '&' )
						{
							token = LexTokenType.ADDRLEX_AMP;
							return true;
						}
						if ( Char.IsDigit(ch) )
						{
							m_state = LexState.LEX_NUM;
							break;
						}
						m_state = LexState.LEX_ALPHA1;
						break;
								
					case LexState.LEX_NUM:
						//
						//  We've read one or more numbers.  It may be a number
						//  123, an ordinal 123rd, or an alphanumeric 123abc.
						//
						if ( IsSeperator(ch) )
						{
							if (!IsEOF())
							{
								m_pos--;
							}
							token = LexTokenType.ADDRLEX_NUM;
							return true;
						}
						if ( !Char.IsDigit(ch) )
						{
							if ( StringHelper.EndsWith(m_lex, "1") && (ch == 'S' || ch == 's') )
							{
								AppendLex(ch);
								m_state = LexState.LEX_ORD_T;
								break;
							}
							if ( StringHelper.EndsWith(m_lex, "2") && (ch == 'R' || ch == 'r' || ch == 'N' || ch == 'n') )
							{
								AppendLex(ch);
								m_state = LexState.LEX_ORD_D;
								break;
							}
							if ( StringHelper.EndsWith(m_lex, "3") && (ch == 'R' || ch == 'r') )
							{
								AppendLex(ch);
								m_state = LexState.LEX_ORD_D;
								break;
							}
							if ( ch == 't' || ch == 'T' )
							{
								AppendLex(ch);
								m_state = LexState.LEX_ORD_H;
								break;						
							}
							if ( ch == '/' )
							{
								AppendLex(ch);			
								m_state = LexState.LEX_FRACT;
								break;
							}
							m_state = LexState.LEX_ALPHANUM;
						}
						AppendLex(ch);
						break;
							
					case LexState.LEX_FRACT:
						if ( !Char.IsDigit(ch) )
						{
							if (!IsEOF())
							{
								m_pos--;
							}
							token = LexTokenType.ADDRLEX_FRACTION;
							return true;
						}
						AppendLex(ch);			
						break;
					
					case LexState.LEX_ALPHA1:
						//
						//  A single alpha character has been read
						//
						if ( IsSeperator(ch) )
						{
							if (!IsEOF())
							{
								m_pos--;
							}
							token = LexTokenType.ADDRLEX_ONECHAR;
							return true;
						}
						AppendLex(ch);
						if ( Char.IsLetter(ch) )
						{
							m_state = LexState.LEX_ALPHA2;
						}
						else
						{
							m_state = LexState.LEX_ALPHANUM;
						}
						break;
					
					case LexState.LEX_ALPHA2:
						//
						//  Two character have been read
						//
						if ( IsSeperator(ch) )
						{
							if (!IsEOF())
							{
								m_pos--;
							}
							token = LexTokenType.ADDRLEX_TWOCHAR;
							return true;
						}
						AppendLex(ch);
						if ( Char.IsLetter(ch) )
						{
							m_state = LexState.LEX_ALPHA;
						}
						else
						{
							m_state = LexState.LEX_ALPHANUM;
						}
						break;
					
					case LexState.LEX_ALPHA:
						//
						//  Read until break or digit
						//
						if ( IsSeperator(ch) )
						{
							if (!IsEOF())
							{
								m_pos--;
							}
							token = LexTokenType.ADDRLEX_ALPHA;
							return true;
						}
						AppendLex(ch);
						if ( !Char.IsLetter(ch) )
						{
							m_state = LexState.LEX_ALPHANUM;
						}
						break;
						
					case LexState.LEX_ALPHANUM:
						//
						//  Read until break;
						//
						if ( IsSeperator(ch) )
						{
							if (!IsEOF())
							{
								m_pos--;
							}
							token = LexTokenType.ADDRLEX_ALPHANUM;
							return true;
						}
						AppendLex(ch);
						break;
					
					case LexState.LEX_ORD_T:
						//
						//  Read the 'T' after '1S'
						//
						if (IsSeperator(ch))
						{
							if (!IsEOF())
							{
								m_pos--;
							}
							token = LexTokenType.ADDRLEX_ALPHANUM;
							return true;
						}
						if (ch == 't' || ch == 'T')
						{
							AppendLex(ch);
							m_state = LexState.LEX_ORD_END;
							break;
						}
						m_pos--;
						m_state = LexState.LEX_ALPHANUM;
						break;
							
					case LexState.LEX_ORD_D:
						//
						//  Read the 'D' after '2R'
						//
						if (IsSeperator(ch))
						{
							if (!IsEOF())
							{
								m_pos--;
							}
							token = LexTokenType.ADDRLEX_ALPHANUM;
							return true;
						}
						if (ch == 'd' || ch == 'D')
						{
							AppendLex(ch);
							m_state = LexState.LEX_ORD_END;
							break;
						}
						m_pos--;
						m_state = LexState.LEX_ALPHANUM;
						break;
						
					case LexState.LEX_ORD_H:
						//
						//  Read the 'H' after '5T'
						//
						if (IsSeperator(ch))
						{
							if (!IsEOF())
							{
								m_pos--;
							}
							token = LexTokenType.ADDRLEX_ALPHANUM;
							return true;
						}
						if (ch == 'h' || ch == 'H')
						{
							AppendLex(ch);
							m_state = LexState.LEX_ORD_END;
							break;
						}
						m_pos--;
						m_state = LexState.LEX_ALPHANUM;
						break;
					
					case LexState.LEX_ORD_END:
						if ( IsSeperator(ch) )
						{
							if (!IsEOF())
							{
								m_pos--;
							}
							token = LexTokenType.ADDRLEX_ORDINAL;
							return true;
						}
						AppendLex(ch);
						m_state = LexState.LEX_ALPHANUM;				
						break;
					
					default:
						throw new Exception("Internal error");
				}
			}
		}		
	}
}
