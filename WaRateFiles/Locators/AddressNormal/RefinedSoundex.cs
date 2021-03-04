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
	 * Encodes a string into a Refined Soundex value. A refined soundex code is
	 * optimized for spell checking words. Soundex method originally developed by
	 * <CITE>Margaret Odell</CITE> and <CITE>Robert Russell</CITE>.
	 * 
	 * @author Apache Software Foundation
	 * @version $Id$
	 */
	internal class RefinedSoundex 
	{
	
	   /**
		 * RefinedSoundex is *refined* for a number of reasons one being that the
		 * mappings have been altered. This implementation contains default
		 * mappings for US English.
		 */
	    public static string US_ENGLISH_MAPPING = "01360240043788015936020505";
	
	    /**
		 * Every letter of the alphabet is "mapped" to a numerical value. This char
		 * array holds the values to which each letter is mapped. This
		 * implementation contains a default map for US_ENGLISH
		 */
	    private static string soundexMapping = US_ENGLISH_MAPPING;
		
	     /**
		 * Creates an instance of the RefinedSoundex object using the default US
		 * English mapping.
		 */
	    private RefinedSoundex() {
	    }
	
	    /**
		 * Returns the number of characters in the two encoded Strings that are the
		 * same. This return value ranges from 0 to the length of the shortest
		 * encoded String: 0 indicates little or no similarity, and 4 out of 4 (for
		 * example) indicates strong similarity or identical values. For refined
		 * Soundex, the return value can be greater than 4.
		 * 
		 * @param s1
		 *                  A String that will be encoded and compared.
		 * @param s2
		 *                  A String that will be encoded and compared.
		 * @return The number of characters in the two encoded Strings that are the
		 *             same from 0 to to the length of the shortest encoded String.
		 * 
		 * @see SoundexUtils#difference(StringEncoder,String,String)
		 * @see <a href="http://msdn.microsoft.com/library/default.asp?url=/library/en-us/tsqlref/ts_de-dz_8co5.asp">
		 *          MS T-SQL DIFFERENCE</a>
		 * 
		 * @throws EncoderException
		 *                  if an error occurs encoding one of the strings
	     * @since 1.3
		 */
	    public static int Difference(string s1, string s2) {
	        return SoundexUtils.DifferenceEncoded(Encode(s1), Encode(s2));
	    }

		public static int DifferenceEncoded(string s1, string s2)
		{
			return SoundexUtils.DifferenceEncoded(s1, s2);
		}
	
	    /**
		 * Encodes a String using the refined soundex algorithm.
		 * 
		 * @param pString
		 *                  A String object to encode
		 * @return A Soundex code corresponding to the String supplied
		 */
	    public static string Encode(string str) {
	        if (str == null) {
	            return null;
	        }
	        str = SoundexUtils.Clean(str);
	        if (str.Length == 0) {
	            return str;
	        }
	
	        StringBuilder sBuf = new StringBuilder();
	        sBuf.Append(str[0]);
	
	        char last, current;
	        last = '*';
	
	        for (int i = 0; i < str.Length; i++) {
	
	            current = GetMappingCode(str[i]);
	            if (current == last) {
	                continue;
	            } else if (current != 0) {
	                sBuf.Append(current);
	            }
	
	            last = current;
	
	        }
	
	        return sBuf.ToString();
		}
	
	    /**
		 * Returns the mapping code for a given character. The mapping codes are
		 * maintained in an internal char array named soundexMapping, and the
		 * default values of these mappings are US English.
		 * 
		 * @param c
		 *                  char to get mapping for
		 * @return A character (really a numeral) to return for the given char
		 */
	    private static char GetMappingCode(char c) {
	        if (!Char.IsLetter(c)) {
	            return '\0';
	        }
	        return soundexMapping[Char.ToUpper(c) - 'A'];
	    }	
	}
}
