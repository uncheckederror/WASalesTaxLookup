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

namespace WaRateFiles.Locators.AddressNormal
{
    internal class SoundexUtils
    {
        private SoundexUtils()
        {
        }

        /**
		 * Cleans up the input string before Soundex processing by only returning
		 * upper case letters.
		 * 
		 * @param str
		 *                  The String to clean.
		 * @return A clean String.
		 */
        public static string Clean(string str)
        {
            if (str == null || str.Length == 0)
            {
                return str;
            }
            int len = str.Length;
            char[] chars = new char[len];
            int count = 0;
            for (int i = 0; i < len; i++)
            {
                if (Char.IsLetter(str[i]))
                {
                    chars[count++] = str[i];
                }
            }
            if (count == len)
            {
                return str.ToUpper();
            }
            return new string(chars, 0, count).ToUpper();
        }

        /**
		 * Returns the number of characters in the two Soundex encoded Strings that
		 * are the same.
		 * <ul>
		 * <li>For Soundex, this return value ranges from 0 through 4: 0 indicates
		 * little or no similarity, and 4 indicates strong similarity or identical
		 * values.</li>
		 * <li>For refined Soundex, the return value can be greater than 4.</li>
		 * </ul>
		 * 
		 * @param es1
		 *                  An encoded String.
		 * @param es2
		 *                  An encoded String.
		 * @return The number of characters in the two Soundex encoded Strings that
		 *             are the same.
		 * 
		 * @see <a href="http://msdn.microsoft.com/library/default.asp?url=/library/en-us/tsqlref/ts_de-dz_8co5.asp">
		 *          MS T-SQL DIFFERENCE</a>
		 */
        public static int DifferenceEncoded(string es1, string es2)
        {

            if (es1 == null || es2 == null)
            {
                return 0;
            }
            int lengthToMatch = Math.Min(es1.Length, es2.Length);
            int diff = 0;
            for (int i = 0; i < lengthToMatch; i++)
            {
                if (es1[i] == es2[i])
                {
                    diff++;
                }
            }
            return diff;
        }
    }
}
