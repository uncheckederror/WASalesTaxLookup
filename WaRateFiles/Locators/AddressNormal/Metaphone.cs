/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 *      http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Text;

namespace WaRateFiles.Locators.AddressNormal
{
    /**
	 * Encodes a string into a metaphone value. 
	 * <p>
	 * Initial Java implementation by <CITE>William B. Brogden. December, 1997</CITE>. 
	 * Permission given by <CITE>wbrogden</CITE> for code to be used anywhere.
	 * </p>
	 * <p>
	 * <CITE>Hanging on the Metaphone</CITE> by <CITE>Lawrence Philips</CITE> in <CITE>Computer Language of Dec. 1990, p
	 * 39.</CITE>
	 * </p>
	 * 
	 * @author Apache Software Foundation
	 * @version $Id$
	 */
    internal class Metaphone
    {
        /**
		 * Five values in the English language 
		 */
        private static string VOWELS = "AEIOU";

        /**
		 * Variable used in Metaphone algorithm
		 */
        private static string FRONTV = "EIY";

        /**
		 * Variable used in Metaphone algorithm
		 */
        private static string VARSON = "CSPTG";

        /**
		 * The max code length for metaphone is 4
		 */
        private static int m_maxCodeLen = 4;

        /**
		 * Creates an instance of the Metaphone encoder
		 */
        private Metaphone()
        {
        }

        /**
		 * Find the metaphone value of a String. This is similar to the
		 * soundex algorithm, but better at finding similar sounding words.
		 * All input is converted to upper case.
		 * Limitations: Input format is expected to be a single ASCII word
		 * with only characters in the A - Z range, no punctuation or numbers.
		 *
		 * @param txt String to find the metaphone code for
		 * @return A metaphone code corresponding to the String supplied
		 */
        public static string Encode(string txt)
        {
            bool hard = false;
            if ((txt == null) || (txt.Length == 0))
            {
                return "";
            }
            // single character is itself
            if (txt.Length == 1)
            {
                return txt.ToUpper();
            }

            char[] inwd = new char[txt.Length];
            for (int x = 0; x < txt.Length; x++)
            {
                inwd[x] = Char.ToUpper(txt[x]);
            }

            StringBuilder local = new StringBuilder(40); // manipulate
            StringBuilder code = new StringBuilder(10); //   output
                                                        // handle initial 2 characters exceptions
            switch (inwd[0])
            {
                case 'K':
                case 'G':
                case 'P': /* looking for KN, etc*/
                    if (inwd[1] == 'N')
                    {
                        local.Append(inwd, 1, inwd.Length - 1);
                    }
                    else
                    {
                        local.Append(inwd);
                    }
                    break;
                case 'A': /* looking for AE */
                    if (inwd[1] == 'E')
                    {
                        local.Append(inwd, 1, inwd.Length - 1);
                    }
                    else
                    {
                        local.Append(inwd);
                    }
                    break;
                case 'W': /* looking for WR or WH */
                    if (inwd[1] == 'R')
                    {   // WR -> R
                        local.Append(inwd, 1, inwd.Length - 1);
                        break;
                    }
                    if (inwd[1] == 'H')
                    {
                        local.Append(inwd, 1, inwd.Length - 1);
                        local[0] = 'W';//.SetCharAt(0, 'W'); // WH -> W
                    }
                    else
                    {
                        local.Append(inwd);
                    }
                    break;
                case 'X': /* initial X becomes S */
                    inwd[0] = 'S';
                    local.Append(inwd);
                    break;
                default:
                    local.Append(inwd);
                    break;
            } // now local has working string with initials fixed

            int wdsz = local.Length;
            int n = 0;

            while ((code.Length < m_maxCodeLen) &&
                   (n < wdsz))
            { // max code size of 4 works well
                char symb = local[n];
                // remove duplicate letters except C
                if ((symb != 'C') && (isPreviousChar(local, n, symb)))
                {
                    n++;
                }
                else
                { // not dup
                    switch (symb)
                    {
                        case 'A':
                        case 'E':
                        case 'I':
                        case 'O':
                        case 'U':
                            if (n == 0)
                            {
                                code.Append(symb);
                            }
                            break; // only use vowel if leading char
                        case 'B':
                            if (isPreviousChar(local, n, 'M') &&
                                 isLastChar(wdsz, n))
                            { // B is silent if word ends in MB
                                break;
                            }
                            code.Append(symb);
                            break;
                        case 'C': // lots of C special cases
                            /* discard if SCI, SCE or SCY */
                            if (isPreviousChar(local, n, 'S') &&
                                 !isLastChar(wdsz, n) &&
                                 (FRONTV.IndexOf(local[n + 1]) >= 0))
                            {
                                break;
                            }
                            if (regionMatch(local, n, "CIA"))
                            { // "CIA" -> X
                                code.Append('X');
                                break;
                            }
                            if (!isLastChar(wdsz, n) &&
                                (FRONTV.IndexOf(local[n + 1]) >= 0))
                            {
                                code.Append('S');
                                break; // CI,CE,CY -> S
                            }
                            if (isPreviousChar(local, n, 'S') &&
                                isNextChar(local, n, 'H'))
                            { // SCH->sk
                                code.Append('K');
                                break;
                            }
                            if (isNextChar(local, n, 'H'))
                            { // detect CH
                                if ((n == 0) &&
                                    (wdsz >= 3) &&
                                    isVowel(local, 2))
                                { // CH consonant -> K consonant
                                    code.Append('K');
                                }
                                else
                                {
                                    code.Append('X'); // CHvowel -> X
                                }
                            }
                            else
                            {
                                code.Append('K');
                            }
                            break;
                        case 'D':
                            if (!isLastChar(wdsz, n + 1) &&
                                isNextChar(local, n, 'G') &&
                                (FRONTV.IndexOf(local[n + 2]) >= 0))
                            { // DGE DGI DGY -> J 
                                code.Append('J'); n += 2;
                            }
                            else
                            {
                                code.Append('T');
                            }
                            break;
                        case 'G': // GH silent at end or before consonant
                            if (isLastChar(wdsz, n + 1) &&
                                isNextChar(local, n, 'H'))
                            {
                                break;
                            }
                            if (!isLastChar(wdsz, n + 1) &&
                                isNextChar(local, n, 'H') &&
                                !isVowel(local, n + 2))
                            {
                                break;
                            }
                            if ((n > 0) &&
                                (regionMatch(local, n, "GN") ||
                                  regionMatch(local, n, "GNED")))
                            {
                                break; // silent G
                            }
                            if (isPreviousChar(local, n, 'G'))
                            {
                                hard = true;
                            }
                            else
                            {
                                hard = false;
                            }
                            if (!isLastChar(wdsz, n) &&
                                (FRONTV.IndexOf(local[n + 1]) >= 0) &&
                                (!hard))
                            {
                                code.Append('J');
                            }
                            else
                            {
                                code.Append('K');
                            }
                            break;
                        case 'H':
                            if (isLastChar(wdsz, n))
                            {
                                break; // terminal H
                            }
                            if ((n > 0) &&
                                (VARSON.IndexOf(local[n - 1]) >= 0))
                            {
                                break;
                            }
                            if (isVowel(local, n + 1))
                            {
                                code.Append('H'); // Hvowel
                            }
                            break;
                        case 'F':
                        case 'J':
                        case 'L':
                        case 'M':
                        case 'N':
                        case 'R':
                            code.Append(symb);
                            break;
                        case 'K':
                            if (n > 0)
                            { // not initial
                                if (!isPreviousChar(local, n, 'C'))
                                {
                                    code.Append(symb);
                                }
                            }
                            else
                            {
                                code.Append(symb); // initial K
                            }
                            break;
                        case 'P':
                            if (isNextChar(local, n, 'H'))
                            {
                                // PH -> F
                                code.Append('F');
                            }
                            else
                            {
                                code.Append(symb);
                            }
                            break;
                        case 'Q':
                            code.Append('K');
                            break;
                        case 'S':
                            if (regionMatch(local, n, "SH") ||
                                regionMatch(local, n, "SIO") ||
                                regionMatch(local, n, "SIA"))
                            {
                                code.Append('X');
                            }
                            else
                            {
                                code.Append('S');
                            }
                            break;
                        case 'T':
                            if (regionMatch(local, n, "TIA") ||
                                regionMatch(local, n, "TIO"))
                            {
                                code.Append('X');
                                break;
                            }
                            if (regionMatch(local, n, "TCH"))
                            {
                                // Silent if in "TCH"
                                break;
                            }
                            // substitute numeral 0 for TH (resembles theta after all)
                            if (regionMatch(local, n, "TH"))
                            {
                                code.Append('0');
                            }
                            else
                            {
                                code.Append('T');
                            }
                            break;
                        case 'V':
                            code.Append('F'); break;
                        case 'W':
                        case 'Y': // silent if not followed by vowel
                            if (!isLastChar(wdsz, n) &&
                                isVowel(local, n + 1))
                            {
                                code.Append(symb);
                            }
                            break;
                        case 'X':
                            code.Append('K'); code.Append('S');
                            break;
                        case 'Z':
                            code.Append('S'); break;
                    } // end switch
                    n++;
                } // end else from symb != 'C'
                if (code.Length > m_maxCodeLen)
                {
                    code.Length = m_maxCodeLen;
                }
            }
            return code.ToString();
        }

        private static bool isVowel(StringBuilder str, int index)
        {
            return VOWELS.IndexOf(str[index]) >= 0;
        }

        private static bool isPreviousChar(StringBuilder str, int index, char c)
        {
            bool matches = false;
            if (index > 0 &&
                index < str.Length)
            {
                matches = str[index - 1] == c;
            }
            return matches;
        }

        private static bool isNextChar(StringBuilder str, int index, char c)
        {
            bool matches = false;
            if (index >= 0 &&
                index < str.Length - 1)
            {
                matches = str[index + 1] == c;
            }
            return matches;
        }

        private static bool regionMatch(StringBuilder str, int index, String test)
        {
            bool matches = false;
            if (index >= 0 &&
                (index + test.Length - 1) < str.Length)
            {
                string substring = str.ToString().Substring(index, test.Length);
                matches = substring.Equals(test);
            }
            return matches;
        }

        private static bool isLastChar(int wdsz, int n)
        {
            return n + 1 == wdsz;
        }

        /**
		 * Returns the maxCodeLen.
		 * @return int
		 */
        public int MaxCodeLen
        {
            get { return m_maxCodeLen; }
            set { m_maxCodeLen = value; }
        }
    }
}
