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

using WaRateFiles.Standardizer;

using WASalesTax.Models;

namespace WaRateFiles.Locators.AddressNormal
{
    internal class StreetComponent
    {
        public static double Match(AddressLineTokenizer tokenizer, AddressRange address)
        {
            AddressLineTokenizer canidate = new(address.Street);

            double score = 0;

            if (tokenizer.Street.Lexum != canidate.Street.Lexum)
            {
                return score;
            }

            if (null != tokenizer.House)
            {
                int houseNum = Int32.Parse(tokenizer.House.Lexum);
                if ((houseNum % 2) != 0 && (address.OddOrEven == 'E'))
                {
                    // tie breaker
                    score = -0.0001;
                }
                if (houseNum < address.AddressRangeLowerBound || houseNum > address.AddressRangeUpperBound)
                {
                    score -= 0.1;

                    int diff = Math.Abs(houseNum - (address.AddressRangeUpperBound ?? 0 + address.AddressRangeLowerBound ?? 0) / 2);
                    if (address.AddressRangeLowerBound > 10000)
                    {
                        score -= diff / 1000000.0;
                    }
                    else
                    {
                        score -= diff / 10000.0;
                    }
                }
            }

            // Predirectional
            if (canidate.PrefixDir == null && tokenizer.PrefixDir == null)
            {
                score += .1;
            }
            else if (canidate.PrefixDir != null && tokenizer.PrefixDir != null)
            {
                if (canidate.PrefixDir.Lexum == tokenizer.PrefixDir.Lexum)
                {
                    score += .25;
                }
                else if (canidate.PrefixDir.Lexum.IndexOf(tokenizer.PrefixDir.Lexum) > -1)
                {
                    score += .05;
                }
                else
                {
                    // User input doesn't match
                    score -= .2;
                }
            }
            else if (canidate.PrefixDir == null)
            {
                // input address has predir, but not this address
                score -= .3;
            }

            // Road type
            if (canidate.StreetType == null && tokenizer.StreetType == null)
            {
                score += .1;
            }
            else if (canidate.StreetType != null && tokenizer.StreetType != null)
            {
                if (canidate.StreetType.Lexum == tokenizer.StreetType.Lexum)
                {
                    score += .2;
                }
                else
                {
                    // User input doesn't match
                    score -= .05;
                }
            }
            else if (canidate.StreetType == null)
            {
                // input address has type, but not this address
                score -= .1;
            }

            // Postdirectional
            if (canidate.SuffixDir == null && tokenizer.SuffixDir == null)
            {
                score += .1;
            }
            else if (canidate.SuffixDir != null && tokenizer.SuffixDir != null)
            {
                if (canidate.SuffixDir.Lexum == tokenizer.SuffixDir.Lexum)
                {
                    score += .25;
                }
                else if (canidate.SuffixDir.Lexum.IndexOf(tokenizer.SuffixDir.Lexum) > -1)
                {
                    score += .05;
                }
                else
                {
                    score -= .2;
                }
            }
            else if (canidate.SuffixDir == null)
            {
                // input address has sufdir, but not this address
                score -= .1;
            }

            return score;
        }
    }
}
