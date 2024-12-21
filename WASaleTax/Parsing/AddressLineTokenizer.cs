using System;
using System.Collections.Generic;

using WASalesTax.Models;

namespace WASalesTax.Parsing
{
    /// <summary>
    /// AddressLineTokenizer's job is to identify the postal components of an address.
    /// </summary>
    public class AddressLineTokenizer
    {
        public string m_rawStreet { get; set; }

        public static StringTable StringTable { get; set; }

        /// <summary>
        /// m_predir ==> A directional modifier that precedes the street name. Example, 
        /// the WEST in WEST 3715 10TH AVE
        /// </summary>
        public AddressToken m_predir { get; set; }

        /// <summary>
        /// m_house ==> This is the civic address number. Example, the 3715 in 3715 10TH AVE 
        /// W. In reference records it is associated with the blockface address ranges.
        /// </summary>
        public AddressToken m_house { get; set; }

        /// <summary>
        /// m_streetQualif ==> Example, the OLD in 3715 OLD HIGHWAY 99.
        /// </summary>
        public AddressToken m_streetQualif { get; set; }

        /// <summary>
        /// m_streetPrefix ==> A street type preceding the root street name. Example, the HIGHWAY 
        /// in 3715 HIGHWAY 99.
        /// </summary>
        public AddressToken m_streetPrefix { get; set; }

        /// <summary>
        /// m_street ==> This is the root street name, stripped of directional or type modifiers. 
        /// Example, the 10TH in 3715 W 10TH AVE.
        /// </summary>
        public AddressToken m_street { get; set; }

        /// <summary>
        /// m_streetType ==> A street type following the root street name. Example, the AVE 
        /// in 3715 WEST 10TH AVE.
        /// </summary>
        public AddressToken m_streetType { get; set; }

        /// <summary>
        /// m_sufdir ==> A directional modifier that follows the street name. Example, the W in
        /// 3715 10TH AVE W
        /// </summary>
        public AddressToken m_sufdir { get; set; }

        public string RawStreet { get; set; }

        public AddressToken PrefixDir
        {
            get { return m_predir; }
            set { m_predir = value; m_predir.NormalizeDirectional(); m_predir.ResultToken = StreetToken.PREDIR; }
        }

        public AddressToken House
        {
            get { return m_house; }
            set { m_house = value; m_house.ResultToken = StreetToken.HOUSE; }
        }

        public AddressToken StreetQualifier
        {
            get { return m_streetQualif; }
            set { m_streetQualif = value; m_streetQualif.ResultToken = StreetToken.STREETQUALIF; }
        }

        public AddressToken StreetPrefix
        {
            get { return m_streetPrefix; }
            set { m_streetPrefix = value; m_streetPrefix.ResultToken = StreetToken.STREETPRE; }
        }

        public AddressToken Street
        {
            get { return m_street; }
            set
            {
                m_street = value;
                if (m_street.IsOrdinalWord())
                {
                    m_street.NormalizeOrdinalWord();
                }
                m_street.ResultToken = StreetToken.STREET;
            }
        }

        public AddressToken StreetType
        {
            get { return m_streetType; }
            set { m_streetType = value; m_streetType.NormalizeRoadType(); m_streetType.ResultToken = StreetToken.STREETTYPE; }
        }

        public AddressToken SuffixDir
        {
            get { return m_sufdir; }
            set { m_sufdir = value; m_sufdir.NormalizeDirectional(); m_sufdir.ResultToken = StreetToken.SUFDIR; }
        }

        public AddressLineTokenizer(string street)
        {
            m_house = null;
            m_predir = null;
            m_street = null;
            m_streetPrefix = null;
            m_streetQualif = null;
            m_streetType = null;
            m_sufdir = null;
            m_rawStreet = street;

            Lex lexer = new(street);

            List<AddressToken> tokens = [];
            StringTable m_strtab = new();

            while (lexer.NextToken(out LexTokenType token) && token != LexTokenType.ADDRLEX_EOF)
            {
                tokens.Add(new AddressToken(m_strtab.Get(lexer.Lexum), token));
            }

            // eliminate appartment numbers
            for (int x = 0; x < tokens.Count; x++)
            {
                AddressToken atok = tokens[x];
                if (atok.LexToken == LexTokenType.ADDRLEX_ALPHANUM && atok.Lexum[0] == '#')
                {
                    tokens.RemoveAt(x);
                    break;
                }
            }
            if (tokens.Count == 0)
            {
                return;
            }

            CoalesceDirectionals(tokens);

            if (ApplyRules(tokens))
            {
                return;
            }

            // try some substitutions

            // House number an ordinal?
            if (tokens[0].LexToken == LexTokenType.ADDRLEX_ORDINAL)
            {
                tokens[0].ToNumeric();
                if (ApplyRules(tokens))
                {
                    return;
                }
            }
            else if (tokens.Count > 1 &&
                tokens[1].LexToken == LexTokenType.ADDRLEX_ORDINAL &&
                tokens[0].LexToken != LexTokenType.ADDRLEX_NUM
                )
            {
                tokens[1].ToNumeric();
                if (ApplyRules(tokens))
                {
                    return;
                }
            }

            // Coalesce directionals
            bool retry = false;
            for (int x = 0; x < tokens.Count; x++)
            {
                AddressToken atok = tokens[x];
                if (atok.IsDirectional() && atok.Lexum.Length > 2)
                {
                    if (x > 0)
                    {
                        if (tokens[x - 1].IsDirectional())
                        {
                            continue;
                        }
                    }
                    atok.NormalizeDirectional();
                    retry = true;
                }
            }
            if (retry)
            {
                CoalesceDirectionals(tokens);
                if (ApplyRules(tokens))
                {
                    return;
                }
            }

            // Trim any unit
            retry = false;
            for (int x = 2; x < tokens.Count; x++)
            {
                AddressToken atok = tokens[x];
                if (atok.IsUnit() || atok.Lexum[0] == '%')
                {
                    retry = true;
                    // delete everything after
                    for (int y = tokens.Count - 1; y >= x; y--)
                    {
                        tokens.RemoveAt(y);
                    }
                    break;
                }
            }
            if (retry)
            {
                if (ApplyRules(tokens))
                {
                    return;
                }
            }

            // Try combining words
            if (CombineWords(tokens, false))
            {
                if (ApplyRules(tokens))
                {
                    return;
                }
                if (CombineWords(tokens, false))
                {
                    if (ApplyRules(tokens))
                    {
                        return;
                    }
                    if (CombineWords(tokens, false))
                    {
                        if (ApplyRules(tokens))
                        {
                            return;
                        }
                    }
                }
            }
            if (CombineWords(tokens, true))
            {
                if (ApplyRules(tokens))
                {
                    return;
                }
            }

            // Try looking for street names that have road type like "LA PUSH DR"
            if (FixUpRoadTypes(tokens))
            {
                if (ApplyRules(tokens))
                {
                    return;
                }
            }

            // Try consolidating any one letter directionals in the interior of the address
            if (FixUpDirectionals(tokens))
            {
                CombineWords(tokens, false);
                if (ApplyRules(tokens))
                {
                    return;
                }
            }

            // Make a last ditch effort to find a street name
            for (int x = 0; x < tokens.Count; x++)
            {
                AddressToken atok = tokens[x];
                if (atok.LexToken == LexTokenType.ADDRLEX_ALPHA)
                {
                    Street = atok;
                    break;
                }
            }
            if (Street == null)
            {
                for (int x = 0; x < tokens.Count; x++)
                {
                    AddressToken atok = tokens[x];
                    if (atok.LexToken == LexTokenType.ADDRLEX_ALPHANUM)
                    {
                        Street = atok;
                        break;
                    }
                }
            }
            Street ??= (tokens.Count > 1) ? tokens[1] : tokens[0];
            // scan for a house number
            for (int x = 0; x < tokens.Count; x++)
            {
                if (tokens[x] == Street)
                {
                    continue;
                }
                if (tokens[x].LexToken == LexTokenType.ADDRLEX_NUM)
                {
                    House = tokens[x];
                    break;
                }
            }
        }

        public double Match(AddressRange address)
        {
            AddressLineTokenizer canidate = new(address.Street);
            var tokenizer = this;

            double score = 0;

            if (tokenizer.Street.Lexum != canidate.Street.Lexum)
            {
                return score;
            }

            if (null != tokenizer.House)
            {
                int houseNum = int.Parse(tokenizer.House.Lexum);
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
        private static bool CombineWords(List<AddressToken> tokens, bool ignoreStreetType)
        {
            for (int x = 0; x < tokens.Count - 1; x++)
            {
                AddressToken atok = tokens[x];
                AddressToken atok1 = tokens[x + 1];
                if (atok.LexToken != LexTokenType.ADDRLEX_ALPHA && atok.LexToken != LexTokenType.ADDRLEX_TWOCHAR && atok.LexToken != LexTokenType.ADDRLEX_ONECHAR)
                {
                    continue;
                }
                if (atok.IsDirectional() && atok.Lexum.Length < 3)
                {
                    continue;
                }
                if (atok.IsRoadType() && atok.Lexum.Length < 3)
                {
                    continue;
                }
                if (atok1.LexToken != LexTokenType.ADDRLEX_ALPHA && atok1.LexToken != LexTokenType.ADDRLEX_TWOCHAR && atok1.LexToken != LexTokenType.ADDRLEX_ONECHAR)
                {
                    continue;
                }
                if (atok1.IsRoadType() && (atok1.Lexum.Length < 3 || x + 2 == tokens.Count))
                {
                    continue;
                }
                if (atok1.IsDirectional() && atok1.Lexum.Length < 3)
                {
                    if ((atok.IsRoadType() || atok1.IsRoadType()) && !ignoreStreetType)
                    {
                        continue;
                    }
                    if (x < tokens.Count - 2)
                    {
                        if (!tokens[x + 2].IsRoadType() || (atok1.IsRoadType() && atok1.Lexum.Length > 2))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        continue;
                    }
                }
                atok.Append(atok1);
                tokens.RemoveAt(x + 1);
                return true;
            }
            return false;
        }

        private static bool FixUpRoadTypes(List<AddressToken> tokens)
        {
            int end = 0;
            bool tryit = false;

            if (tokens[^1].IsRoadType())
            {
                tryit = true;
                end = tokens.Count - 2;
            }
            else if (tokens.Count > 1 && tokens[^1].IsDirectional() && tokens[^2].IsRoadType())
            {
                tryit = true;
                end = tokens.Count - 3;
            }
            if (tryit)
            {
                // if there are any other road types, combine them with the street name
                for (int x = 0; x < end; x++)
                {
                    AddressToken atok = tokens[x];
                    AddressToken atok1 = tokens[x + 1];
                    if (atok.IsRoadType() && (atok1.LexToken == LexTokenType.ADDRLEX_ALPHA || atok1.LexToken == LexTokenType.ADDRLEX_TWOCHAR))
                    {
                        atok.Append(atok1);
                        tokens.RemoveAt(x + 1);
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool FixUpDirectionals(List<AddressToken> tokens)
        {
            bool startsWithHouse = tokens[0].LexToken == LexTokenType.ADDRLEX_NUM;
            if ((startsWithHouse && tokens.Count < 4) || (!startsWithHouse && tokens.Count < 3))
            {
                return false;
            }
            int start = 0;
            if (startsWithHouse)
            {
                start = 1;
            }
            for (int x = start; x < tokens.Count - 2; x++)
            {
                AddressToken atok = tokens[x];
                AddressToken atok1 = tokens[x + 1];
                if (atok.IsDirectional() && atok.Lexum.Length == 1)
                {
                    if (atok1.LexToken == LexTokenType.ADDRLEX_ALPHA || atok1.Lexum.Length == 1)
                    {
                        atok.Append(atok1);
                        tokens.RemoveAt(x + 1);
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool DirectionalsCompatable(AddressToken t1, AddressToken t2)
        {
            if (t1.Lexum[0] == 'N' || t1.Lexum[0] == 'S')
            {
                return t2.Lexum[0] == 'E' || t2.Lexum[0] == 'W';
            }
            return false;
        }

        private static void CoalesceDirectionals(List<AddressToken> tokens)
        {
            for (int x = 0; x < tokens.Count - 1; x++)
            {
                AddressToken tx = tokens[x];
                AddressToken tx1 = tokens[x + 1];
                if (tx.IsDirectional() && tx1.IsDirectional())
                {
                    if ((tx.Lexum.Length == 1 && tx1.Lexum.Length == 1) ||
                        (tx.Lexum.Length > 3 && tx1.Lexum.Length > 3))
                    {
                        if (DirectionalsCompatable(tx, tx1))
                        {
                            tx.AppendDirectional(tx1);
                            tokens.RemoveAt(x + 1);
                        }
                    }
                }
            }
        }

        private bool ApplyRules(List<AddressToken> tokens)
        {
            if (Rule00FP(tokens))
            {
                return true;
            }
            if (Rule0FP(tokens))
            {
                return true;
            }
            if (Rule00FPND(tokens))
            {
                return true;
            }
            if (Rule0FPND(tokens))
            {
                return true;
            }
            if (Rule0(tokens))
            {
                return true;
            }
            if (Rule1(tokens))
            {
                return true;
            }
            if (Rule2(tokens))
            {
                return true;
            }
            if (Rule3(tokens))
            {
                return true;
            }
            if (Rule4(tokens))
            {
                return true;
            }
            if (Rule5(tokens))
            {
                return true;
            }
            if (Rule6(tokens))
            {
                return true;
            }
            if (Rule7(tokens))
            {
                return true;
            }
            if (Rule8(tokens))
            {
                return true;
            }
            if (Rule9(tokens))
            {
                return true;
            }
            if (Rule10(tokens))
            {
                return true;
            }
            if (Rule11(tokens))
            {
                return true;
            }
            if (Rule12(tokens))
            {
                return true;
            }
            if (Rule13(tokens))
            {
                return true;
            }
            if (Rule14(tokens))
            {
                return true;
            }
            if (Rule15(tokens))
            {
                return true;
            }
            if (Rule16(tokens))
            {
                return true;
            }
            if (Rule17(tokens))
            {
                return true;
            }
            if (Rule18(tokens))
            {
                return true;
            }
            if (Rule19(tokens))
            {
                return true;
            }
            if (Rule20(tokens))
            {
                return true;
            }
            if (Rule21(tokens))
            {
                return true;
            }
            if (Rule22(tokens))
            {
                return true;
            }
            if (Rule23(tokens))
            {
                return true;
            }
            if (Rule24(tokens))
            {
                return true;
            }
            if (Rule25(tokens))
            {
                return true;
            }
            if (Rule26(tokens))
            {
                return true;
            }
            if (Rule27(tokens))
            {
                return true;
            }
            if (Rule28(tokens))
            {
                return true;
            }
            if (Rule29(tokens))
            {
                return true;
            }
            if (Rule30(tokens))
            {
                return true;
            }
            if (Rule31(tokens))
            {
                return true;
            }
            if (Rule32(tokens))
            {
                return true;
            }
            if (Rule33(tokens))
            {
                return true;
            }
            if (Rule34(tokens))
            {
                return true;
            }
            if (Rule35(tokens))
            {
                return true;
            }

            if (RuleFFP(tokens))
            {
                return true;
            }
            if (RuleF0FP(tokens))
            {
                return true;
            }
            if (RuleF0(tokens))
            {
                return true;
            }
            if (RuleF1(tokens))
            {
                return true;
            }
            if (RuleF2(tokens))
            {
                return true;
            }
            if (RuleF3(tokens))
            {
                return true;
            }
            if (RuleF4(tokens))
            {
                return true;
            }
            if (RuleF5(tokens))
            {
                return true;
            }
            if (RuleF6(tokens))
            {
                return true;
            }
            if (RuleF7(tokens))
            {
                return true;
            }
            if (RuleF8(tokens))
            {
                return true;
            }
            if (RuleF9(tokens))
            {
                return true;
            }
            if (RuleF10(tokens))
            {
                return true;
            }
            if (RuleF11(tokens))
            {
                return true;
            }
            if (RuleF12(tokens))
            {
                return true;
            }
            // RuleF13 not required
            if (RuleF14(tokens))
            {
                return true;
            }
            if (RuleF15(tokens))
            {
                return true;
            }
            if (RuleF16(tokens))
            {
                return true;
            }
            if (RuleF17(tokens))
            {
                return true;
            }
            if (RuleF18(tokens))
            {
                return true;
            }
            if (RuleF19(tokens))
            {
                return true;
            }
            if (RuleF20(tokens))
            {
                return true;
            }
            if (RuleF21(tokens))
            {
                return true;
            }
            if (RuleF22(tokens))
            {
                return true;
            }
            if (RuleF23(tokens))
            {
                return true;
            }
            if (RuleF24(tokens))
            {
                return true;
            }
            if (RuleF25(tokens))
            {
                return true;
            }
            if (RuleF26(tokens))
            {
                return true;
            }
            if (RuleF27(tokens))
            {
                return true;
            }
            if (RuleF30(tokens))
            {
                return true;
            }
            if (RuleF31(tokens))
            {
                return true;
            }
            if (RuleF32(tokens))
            {
                return true;
            }
            if (RuleF33(tokens))
            {
                return true;
            }
            if (RuleF34(tokens))
            {
                return true;
            }
            if (RuleF35(tokens))
            {
                return true;
            }
            return false;
        }

        private static bool IsStreetToken(LexTokenType token)
        {
            return token == LexTokenType.ADDRLEX_ALPHA ||
                token == LexTokenType.ADDRLEX_NUM ||
                token == LexTokenType.ADDRLEX_ORDINAL ||
                token == LexTokenType.ADDRLEX_ONECHAR;
        }

        private static bool IsHigwaySyn(AddressToken atok)
        {
            return atok.Lexum == "USHY" ||
                atok.Lexum == "STHY" ||
                atok.Lexum == "HWY" ||
                atok.Lexum == "HIGHWAY";
        }

        /// <summary>
        /// rule00 ==> HOUSE:NUM PREDIR:ONECHAR|TWOCHAR ORDWORD:ALPHA STREETTYPE:ALPHA|TWOCHAR
        /// FOURTH PLAIN
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule0FP(List<AddressToken> tokens)
        {
            if (tokens.Count != 4)
            {
                return false;
            }
            if (
                tokens[0].LexToken == LexTokenType.ADDRLEX_NUM &&
                (tokens[1].LexToken == LexTokenType.ADDRLEX_TWOCHAR || tokens[1].LexToken == LexTokenType.ADDRLEX_ONECHAR) &&
                (tokens[2].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[2].LexToken == LexTokenType.ADDRLEX_ORDINAL) &&
                (tokens[3].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[3].LexToken == LexTokenType.ADDRLEX_TWOCHAR)
                )
            {
                if (!tokens[1].IsDirectional())
                {
                    return false;
                }
                if (!tokens[3].IsRoadType())
                {
                    return false;
                }
                if (!tokens[2].IsOrdinalWord() && tokens[2].LexToken != LexTokenType.ADDRLEX_ORDINAL)
                {
                    return false;
                }
                if (tokens[3].Lexum != "PLAIN")
                {
                    return false;
                }
                House = tokens[0];
                PrefixDir = tokens[1];
                tokens[2].NormalizeOrdinalWord();
                tokens[2].Append(tokens[3]);
                Street = tokens[2];
                return true;
            }
            return false;
        }

        /// <summary>
        /// rule000 ==> HOUSE:NUM PREDIR:ONECHAR|TWOCHAR ORDWORD:ALPHA STREETTYPE:ALPHA|TWOCHAR REAL_STREETTYPE:ALPHA|TWOCHAR
        /// FOURTH PLAIN
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule00FP(List<AddressToken> tokens)
        {
            if (tokens.Count != 5)
            {
                return false;
            }
            if (
                tokens[0].LexToken == LexTokenType.ADDRLEX_NUM &&
                (tokens[1].LexToken == LexTokenType.ADDRLEX_TWOCHAR || tokens[1].LexToken == LexTokenType.ADDRLEX_ONECHAR) &&
                (tokens[2].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[2].LexToken == LexTokenType.ADDRLEX_ORDINAL) &&
                (tokens[3].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[3].LexToken == LexTokenType.ADDRLEX_TWOCHAR) &&
                (tokens[4].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[4].LexToken == LexTokenType.ADDRLEX_TWOCHAR)
                )
            {
                if (!tokens[1].IsDirectional())
                {
                    return false;
                }
                if (!tokens[3].IsRoadType())
                {
                    return false;
                }
                if (!tokens[4].IsRoadType())
                {
                    return false;
                }
                if (!tokens[2].IsOrdinalWord() && tokens[2].LexToken != LexTokenType.ADDRLEX_ORDINAL)
                {
                    return false;
                }
                if (tokens[3].Lexum != "PLAIN")
                {
                    return false;
                }
                House = tokens[0];
                PrefixDir = tokens[1];
                tokens[2].NormalizeOrdinalWord();
                tokens[2].Append(tokens[3]);
                Street = tokens[2];
                StreetType = tokens[4];
                return true;
            }
            return false;
        }

        /// <summary>
        /// rule0FPND ==> HOUSE:NUM ORDWORD:ALPHA STREETTYPE:ALPHA|TWOCHAR
        /// FOURTH PLAIN
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule0FPND(List<AddressToken> tokens)
        {
            if (tokens.Count != 3)
            {
                return false;
            }
            if (
                tokens[0].LexToken == LexTokenType.ADDRLEX_NUM &&
                (tokens[1].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[1].LexToken == LexTokenType.ADDRLEX_ORDINAL) &&
                (tokens[2].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[2].LexToken == LexTokenType.ADDRLEX_TWOCHAR)
                )
            {
                if (!tokens[2].IsRoadType())
                {
                    return false;
                }
                if (!tokens[1].IsOrdinalWord() && tokens[1].LexToken != LexTokenType.ADDRLEX_ORDINAL)
                {
                    return false;
                }
                if (tokens[2].Lexum != "PLAIN")
                {
                    return false;
                }
                House = tokens[0];
                tokens[1].NormalizeOrdinalWord();
                tokens[1].Append(tokens[2]);
                Street = tokens[1];
                return true;
            }
            return false;
        }

        /// <summary>
        /// rule00FPND ==> HOUSE:NUM ORDWORD:ALPHA STREETTYPE:ALPHA|TWOCHAR REAL_STREETTYPE:ALPHA|TWOCHAR
        /// FOURTH PLAIN
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule00FPND(List<AddressToken> tokens)
        {
            if (tokens.Count != 4)
            {
                return false;
            }
            if (
                tokens[0].LexToken == LexTokenType.ADDRLEX_NUM &&
                (tokens[1].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[1].LexToken == LexTokenType.ADDRLEX_ORDINAL) &&
                (tokens[2].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[2].LexToken == LexTokenType.ADDRLEX_TWOCHAR) &&
                (tokens[3].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[3].LexToken == LexTokenType.ADDRLEX_TWOCHAR)
                )
            {
                if (!tokens[2].IsRoadType())
                {
                    return false;
                }
                if (!tokens[3].IsRoadType())
                {
                    return false;
                }
                if (!tokens[1].IsOrdinalWord() && tokens[1].LexToken != LexTokenType.ADDRLEX_ORDINAL)
                {
                    return false;
                }
                if (tokens[2].Lexum != "PLAIN")
                {
                    return false;
                }
                House = tokens[0];
                tokens[1].NormalizeOrdinalWord();
                tokens[1].Append(tokens[2]);
                Street = tokens[1];
                StreetType = tokens[3];
                return true;
            }
            return false;
        }

        /// <summary>
        /// rule0 ==> STREET:*
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule0(List<AddressToken> tokens)
        {
            if (tokens.Count != 1)
            {
                return false;
            }
            Street = tokens[0];
            return true;
        }

        /// <summary>
        /// rule1 ==> HOUSE:NUM STREET:ALPHA|ORD|NUM
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule1(List<AddressToken> tokens)
        {
            if (tokens.Count != 2)
            {
                return false;
            }
            if (tokens[0].LexToken == LexTokenType.ADDRLEX_NUM &&
                IsStreetToken(tokens[1].LexToken))
            {
                House = tokens[0];
                Street = tokens[1];
                return true;
            }
            return false;
        }

        /// <summary>
        /// rule2 ==> HOUSE:NUM STREET:ALPHA|ORD|NUM STREETTYPE:ALPHA|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule2(List<AddressToken> tokens)
        {
            if (tokens.Count != 3)
            {
                return false;
            }
            if (tokens[0].LexToken == LexTokenType.ADDRLEX_NUM &&
                IsStreetToken(tokens[1].LexToken) &&
                (tokens[2].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[2].LexToken == LexTokenType.ADDRLEX_TWOCHAR))
            {
                if (tokens[2].IsRoadType())
                {
                    House = tokens[0];
                    Street = tokens[1];
                    StreetType = tokens[2];
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// rule3 ==> PREDIR:ONECHAR|TWOCHAR HOUSE:NUM STREET:ALPHA|ORD|NUM STREETTYPE:ALPHA|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule3(List<AddressToken> tokens)
        {
            if (tokens.Count != 4)
            {
                return false;
            }
            if ((tokens[0].LexToken == LexTokenType.ADDRLEX_TWOCHAR || tokens[0].LexToken == LexTokenType.ADDRLEX_ONECHAR) &&
                tokens[1].LexToken == LexTokenType.ADDRLEX_NUM &&
                IsStreetToken(tokens[2].LexToken) &&
                (tokens[3].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[3].LexToken == LexTokenType.ADDRLEX_TWOCHAR))
            {
                if (!tokens[3].IsRoadType())
                {
                    return false;
                }
                if (!tokens[0].IsDirectional())
                {
                    return false;
                }
                PrefixDir = tokens[0];
                House = tokens[1];
                Street = tokens[2];
                StreetType = tokens[3];
                return true;
            }
            return false;
        }

        /// <summary>
        /// rule4 ==> HOUSE:NUM STREET:ALPHA|ORD|NUM STREETTYPE:ALPHA|TWOCHAR SUFDIR:ONECHAR|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule4(List<AddressToken> tokens)
        {
            if (tokens.Count != 4)
            {
                return false;
            }
            if (tokens[0].LexToken == LexTokenType.ADDRLEX_NUM &&
                IsStreetToken(tokens[1].LexToken) &&
                (tokens[2].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[2].LexToken == LexTokenType.ADDRLEX_TWOCHAR) &&
                tokens[3].IsDirectional()
                )
            {
                if (!tokens[2].IsRoadType())
                {
                    return false;
                }
                House = tokens[0];
                Street = tokens[1];
                StreetType = tokens[2];
                SuffixDir = tokens[3];
                return true;
            }
            return false;
        }

        /// <summary>
        /// rule5 ==> HOUSE:NUM STREET:ALPHA|ORD|NUM STREETPRE:ALPHA STREETTYPE:ALPHA|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule5(List<AddressToken> tokens)
        {
            if (tokens.Count != 4)
            {
                return false;
            }
            if (tokens[0].LexToken == LexTokenType.ADDRLEX_NUM &&
                tokens[2].LexToken == LexTokenType.ADDRLEX_ALPHA &&
                IsStreetToken(tokens[1].LexToken) &&
                (tokens[3].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[3].LexToken == LexTokenType.ADDRLEX_TWOCHAR)
                )
            {
                if (!tokens[3].IsRoadType())
                {
                    return false;
                }
                if (!tokens[2].IsUspsAbbr())
                {
                    return false;
                }
                if (tokens[1].IsDirectional())
                {
                    return false;
                }
                House = tokens[0];
                StreetPrefix = tokens[2];
                Street = tokens[1];
                StreetType = tokens[3];
                return true;
            }
            return false;
        }

        /// <summary>
        /// rule6 ==> HOUSE:NUM STREET:ALPHA|ORD|NUM STREETPRE:ALPHA STREETTYPE:ALPHA|TWOCHAR SUFDIR:ONECHAR|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule6(List<AddressToken> tokens)
        {
            if (tokens.Count != 5)
            {
                return false;
            }
            if (tokens[0].LexToken == LexTokenType.ADDRLEX_NUM &&
                IsStreetToken(tokens[1].LexToken) &&
                tokens[2].LexToken == LexTokenType.ADDRLEX_ALPHA &&
                (tokens[3].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[3].LexToken == LexTokenType.ADDRLEX_TWOCHAR) &&
                (tokens[4].LexToken == LexTokenType.ADDRLEX_ONECHAR || tokens[4].LexToken == LexTokenType.ADDRLEX_TWOCHAR)
                )
            {
                if (!tokens[3].IsRoadType())
                {
                    return false;
                }
                if (!tokens[2].IsUspsAbbr())
                {
                    return false;
                }
                House = tokens[0];
                Street = tokens[1];
                StreetPrefix = tokens[2];
                StreetType = tokens[3];
                SuffixDir = tokens[4];
                return true;
            }
            return false;
        }

        /// <summary>
        /// rule7 ==> HOUSE:NUM STREETQUAL:ALPHA STREETPRE:ALPHA STREET:ALPHA|ORD|NUM SUFDIR:ONECHAR|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule7(List<AddressToken> tokens)
        {
            if (tokens.Count != 5)
            {
                return false;
            }
            if (tokens[0].LexToken == LexTokenType.ADDRLEX_NUM &&
                tokens[1].LexToken == LexTokenType.ADDRLEX_ALPHA &&
                tokens[2].LexToken == LexTokenType.ADDRLEX_ALPHA &&
                IsStreetToken(tokens[3].LexToken) &&
                (tokens[4].IsDirectional() || tokens[4].IsRoadType())
                )
            {
                if (!tokens[2].IsRoadType() && tokens[2].Lexum != "STATE")
                {
                    return false;
                }
                if (!tokens[1].IsUspsAbbr() && tokens[1].Lexum != "OLD")
                {
                    return false;
                }
                House = tokens[0];
                StreetQualifier = tokens[1];
                StreetPrefix = tokens[2];
                Street = tokens[3];
                if (tokens[4].IsDirectional())
                {
                    SuffixDir = tokens[4];
                }
                else if (tokens[4].IsRoadType())
                {
                    StreetType = tokens[4];
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// rule8 ==> HOUSE:NUM STREETQUAL:ALPHA STREETPRE:ALPHA STREET:ALPHA|ORD|NUM
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule8(List<AddressToken> tokens)
        {
            if (tokens.Count != 4)
            {
                return false;
            }
            if (tokens[0].LexToken == LexTokenType.ADDRLEX_NUM &&
                tokens[1].LexToken == LexTokenType.ADDRLEX_ALPHA &&
                tokens[2].LexToken == LexTokenType.ADDRLEX_ALPHA &&
                IsStreetToken(tokens[3].LexToken)
                )
            {
                if (!tokens[2].IsUspsAbbr())
                {
                    return false;
                }
                if (tokens[3].IsRoadType())
                {
                    return false;
                }
                House = tokens[0];
                StreetQualifier = tokens[1];
                StreetPrefix = tokens[2];
                Street = tokens[3];
                return true;
            }
            return false;
        }

        /// <summary>
        /// rule9 ==> PREDIR:ONECHAR|TWOCHAR HOUSE:NUM STREETTYPE:ALPHA|TWOCHAR STREET:ALPHA|ORD|NUM
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule9(List<AddressToken> tokens)
        {
            if (tokens.Count != 4)
            {
                return false;
            }
            if ((tokens[0].LexToken == LexTokenType.ADDRLEX_TWOCHAR || tokens[0].LexToken == LexTokenType.ADDRLEX_ONECHAR) &&
                tokens[1].LexToken == LexTokenType.ADDRLEX_NUM &&
                (tokens[2].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[2].LexToken == LexTokenType.ADDRLEX_TWOCHAR) &&
                IsStreetToken(tokens[3].LexToken)
                )
            {
                if (!tokens[2].IsRoadType())
                {
                    return false;
                }
                if (!tokens[0].IsDirectional())
                {
                    return false;
                }
                PrefixDir = tokens[0];
                House = tokens[1];
                Street = tokens[3];
                StreetType = tokens[2];
                return true;
            }
            return false;
        }

        /// <summary>
        /// rule10 ==> HOUSE:NUM STREETTYPE:ALPHA|TWOCHAR STREET:ALPHA|ORD|NUM SUFDIR:ONECHAR|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule10(List<AddressToken> tokens)
        {
            if (tokens.Count != 4)
            {
                return false;
            }
            if (
                tokens[0].LexToken == LexTokenType.ADDRLEX_NUM &&
                (tokens[1].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[1].LexToken == LexTokenType.ADDRLEX_TWOCHAR) &&
                IsStreetToken(tokens[2].LexToken) &&
                tokens[3].IsDirectional()
                )
            {
                if (!tokens[1].IsRoadType())
                {
                    return false;
                }
                House = tokens[0];
                StreetType = tokens[1];
                Street = tokens[2];
                SuffixDir = tokens[3];
                return true;
            }
            return false;
        }

        /// <summary>
        /// rule11 ==> PREDIR:ONECHAR|TWOCHAR HOUSE:NUM STREET:ALPHA|ORD|NUM STREETTYPE:ALPHA|TWOCHAR SUFDIR:ONECHAR|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule11(List<AddressToken> tokens)
        {
            if (tokens.Count != 5)
            {
                return false;
            }
            if (
                (tokens[0].LexToken == LexTokenType.ADDRLEX_TWOCHAR || tokens[0].LexToken == LexTokenType.ADDRLEX_ONECHAR) &&
                tokens[1].LexToken == LexTokenType.ADDRLEX_NUM &&
                (tokens[3].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[3].LexToken == LexTokenType.ADDRLEX_TWOCHAR) &&
                IsStreetToken(tokens[2].LexToken) &&
                tokens[4].IsDirectional()
                )
            {
                if (!tokens[0].IsDirectional())
                {
                    return false;
                }
                if (!tokens[3].IsRoadType())
                {
                    return false;
                }
                PrefixDir = tokens[0];
                House = tokens[1];
                StreetType = tokens[3];
                Street = tokens[2];
                SuffixDir = tokens[4];
                return true;
            }
            return false;
        }

        /// <summary>
        /// rule12 ==> HOUSE:NUM PREDIR:ONECHAR|TWOCHAR STREETTYPE:ALPHA|TWOCHAR STREET:ALPHA|ORD|NUM SUFDIR:ONECHAR|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule12(List<AddressToken> tokens)
        {
            if (tokens.Count != 5)
            {
                return false;
            }
            if (
                tokens[0].LexToken == LexTokenType.ADDRLEX_NUM &&
                (tokens[1].LexToken == LexTokenType.ADDRLEX_TWOCHAR || tokens[1].LexToken == LexTokenType.ADDRLEX_ONECHAR) &&
                (tokens[2].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[2].LexToken == LexTokenType.ADDRLEX_TWOCHAR) &&
                IsStreetToken(tokens[3].LexToken) &&
                tokens[4].IsDirectional()
                )
            {
                if (!tokens[1].IsDirectional())
                {
                    return false;
                }
                if (!tokens[2].IsRoadType())
                {
                    return false;
                }
                House = tokens[0];
                PrefixDir = tokens[1];
                StreetType = tokens[2];
                Street = tokens[3];
                SuffixDir = tokens[4];
                return true;
            }
            return false;
        }

        /// <summary>
        /// rule13 ==> HOUSE:NUM PREDIR:ONECHAR|TWOCHAR STREET:ALPHA|ORD|NUM STREETTYPE:ALPHA|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule13(List<AddressToken> tokens)
        {
            if (tokens.Count != 4)
            {
                return false;
            }
            if (
                tokens[0].LexToken == LexTokenType.ADDRLEX_NUM &&
                (tokens[1].LexToken == LexTokenType.ADDRLEX_TWOCHAR || tokens[1].LexToken == LexTokenType.ADDRLEX_ONECHAR) &&
                (IsStreetToken(tokens[2].LexToken) || tokens[2].LexToken == LexTokenType.ADDRLEX_ONECHAR) &&
                (tokens[3].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[3].LexToken == LexTokenType.ADDRLEX_TWOCHAR)
                )
            {
                if (!tokens[1].IsDirectional())
                {
                    return false;
                }
                if (!tokens[3].IsRoadType())
                {
                    return false;
                }
                House = tokens[0];
                PrefixDir = tokens[1];
                Street = tokens[2];
                StreetType = tokens[3];
                return true;
            }
            return false;
        }

        /// <summary>
        /// rule14 ==> HOUSE:NUM PREDIR:ONECHAR|TWOCHAR STREET:ALPHA|ORD|NUM SUFDIR:ONECHAR|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule14(List<AddressToken> tokens)
        {
            if (tokens.Count != 4)
            {
                return false;
            }
            if (
                tokens[0].LexToken == LexTokenType.ADDRLEX_NUM &&
                (tokens[1].LexToken == LexTokenType.ADDRLEX_TWOCHAR || tokens[1].LexToken == LexTokenType.ADDRLEX_ONECHAR) &&
                IsStreetToken(tokens[2].LexToken) &&
                tokens[3].IsDirectional()
                )
            {
                if (!tokens[1].IsDirectional())
                {
                    return false;
                }
                House = tokens[0];
                PrefixDir = tokens[1];
                Street = tokens[2];
                SuffixDir = tokens[3];
                return true;
            }
            return false;
        }

        /// <summary>
        /// rule15 ==> HOUSE:NUM STREET:ONECHAR|TWOCHAR STREETTYPE:ALPHA|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule15(List<AddressToken> tokens)
        {
            if (tokens.Count != 3)
            {
                return false;
            }
            if (tokens[0].LexToken == LexTokenType.ADDRLEX_NUM &&
                (tokens[1].LexToken == LexTokenType.ADDRLEX_ONECHAR || tokens[1].LexToken == LexTokenType.ADDRLEX_TWOCHAR) &&
                (tokens[2].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[2].LexToken == LexTokenType.ADDRLEX_TWOCHAR))
            {
                if (tokens[2].IsRoadType() && !tokens[1].IsDirectional())
                {
                    House = tokens[0];
                    Street = tokens[1];
                    StreetType = tokens[2];
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// rule16 ==> HOUSE:NUM STREET:ONECHAR|TWOCHAR STREETTYPE:ALPHA|TWOCHAR SUFDIR:ONECHAR|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule16(List<AddressToken> tokens)
        {
            if (tokens.Count != 4)
            {
                return false;
            }
            if (tokens[0].LexToken == LexTokenType.ADDRLEX_NUM &&
                (tokens[1].LexToken == LexTokenType.ADDRLEX_ONECHAR || tokens[1].LexToken == LexTokenType.ADDRLEX_TWOCHAR) &&
                (tokens[2].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[2].LexToken == LexTokenType.ADDRLEX_TWOCHAR) &&
                tokens[3].IsDirectional()
                )
            {
                if (tokens[2].IsRoadType() && !tokens[1].IsDirectional())
                {
                    House = tokens[0];
                    Street = tokens[1];
                    StreetType = tokens[2];
                    SuffixDir = tokens[3];
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// rule17 ==> HOUSE:NUM PREDIR:ONECHAR|TWOCHAR STREET:ALPHA|NUM|TWOCHAR|ORD
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule17(List<AddressToken> tokens)
        {
            if (tokens.Count != 3)
            {
                return false;
            }
            if (tokens[0].LexToken == LexTokenType.ADDRLEX_NUM &&
                (tokens[1].LexToken == LexTokenType.ADDRLEX_TWOCHAR || tokens[1].LexToken == LexTokenType.ADDRLEX_ONECHAR) &&
                IsStreetToken(tokens[2].LexToken)
                )
            {
                if (!tokens[1].IsDirectional())
                {
                    return false;
                }
                House = tokens[0];
                PrefixDir = tokens[1];
                Street = tokens[2];
                return true;
            }
            return false;
        }

        /// <summary>
        /// rule18 ==> HOUSE:NUM STREETTYPE:ALPHA|TWOCHAR STREET:ALPHA|ORD|NUM
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule18(List<AddressToken> tokens)
        {
            if (tokens.Count != 3)
            {
                return false;
            }
            if (
                tokens[0].LexToken == LexTokenType.ADDRLEX_NUM &&
                (tokens[1].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[1].LexToken == LexTokenType.ADDRLEX_TWOCHAR) &&
                IsStreetToken(tokens[2].LexToken)
                )
            {
                if (!tokens[1].IsRoadType())
                {
                    return false;
                }
                House = tokens[0];
                StreetType = tokens[1];
                Street = tokens[2];
                return true;
            }
            return false;
        }

        /// <summary>
        /// rule19 ==> HOUSE:NUM STREET:ONECHAR|TWOCHAR SUFDIR:ONECHAR|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule19(List<AddressToken> tokens)
        {
            if (tokens.Count != 3)
            {
                return false;
            }
            if (tokens[0].LexToken == LexTokenType.ADDRLEX_NUM &&
                IsStreetToken(tokens[1].LexToken) &&
                (tokens[2].LexToken == LexTokenType.ADDRLEX_TWOCHAR || tokens[2].LexToken == LexTokenType.ADDRLEX_ONECHAR)
                )
            {
                if (!tokens[2].IsDirectional())
                {
                    return false;
                }
                House = tokens[0];
                Street = tokens[1];
                SuffixDir = tokens[2];
                return true;
            }
            return false;
        }

        /// <summary>
        /// rule20 ==> HOUSE:NUM STREETYPE:TWOCHAR|ALPHA STREET:STREET KP:KP SUFDIR:ONECHAR|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule20(List<AddressToken> tokens)
        {
            if (tokens.Count != 5)
            {
                return false;
            }
            if (tokens[0].LexToken == LexTokenType.ADDRLEX_NUM &&
                (tokens[1].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[1].LexToken == LexTokenType.ADDRLEX_TWOCHAR) &&
                IsStreetToken(tokens[2].LexToken) &&
                tokens[3].LexToken == LexTokenType.ADDRLEX_TWOCHAR &&
                tokens[4].IsDirectional()
                )
            {
                if (tokens[3].Lexum != "KP")
                {
                    // Kitsap
                    return false;
                }
                House = tokens[0];
                StreetType = tokens[1];
                Street = tokens[2];
                StreetQualifier = tokens[3];
                SuffixDir = tokens[4];
                return true;
            }
            return false;
        }

        /// <summary>
        /// rule21 ==> HOUSE:NUM STREETQUAL:ALPHA STREETPRE:ALPHA STREET:ALPHA|ORD|NUM SUFDIR:ONECHAR|TWOCHAR STREETTYPE:RD
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule21(List<AddressToken> tokens)
        {
            if (tokens.Count != 6)
            {
                return false;
            }
            if (tokens[0].LexToken == LexTokenType.ADDRLEX_NUM &&
                tokens[1].LexToken == LexTokenType.ADDRLEX_ALPHA &&
                tokens[2].LexToken == LexTokenType.ADDRLEX_ALPHA &&
                IsStreetToken(tokens[3].LexToken) &&
                (tokens[4].LexToken == LexTokenType.ADDRLEX_ONECHAR || tokens[4].LexToken == LexTokenType.ADDRLEX_TWOCHAR) &&
                (tokens[5].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[5].LexToken == LexTokenType.ADDRLEX_TWOCHAR)
                )
            {
                if (!tokens[2].IsRoadType())
                {
                    return false;
                }
                if (!tokens[5].IsRoadType())
                {
                    return false;
                }
                if (!tokens[4].IsDirectional())
                {
                    return false;
                }
                if (!tokens[1].IsUspsAbbr() && tokens[1].Lexum != "OLD")
                {
                    return false;
                }
                House = tokens[0];
                StreetQualifier = tokens[1];
                StreetPrefix = tokens[2];
                Street = tokens[3];
                SuffixDir = tokens[4];
                StreetType = tokens[5];
                return true;
            }
            return false;
        }

        /// <summary>
        /// rule22 ==> HOUSE:NUM STREETQUAL:OLD STREET:NUM|ALPHA SUFDIR:ONECHAR|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule22(List<AddressToken> tokens)
        {
            if (tokens.Count != 4)
            {
                return false;
            }
            if (tokens[0].LexToken == LexTokenType.ADDRLEX_NUM &&
                tokens[1].LexToken == LexTokenType.ADDRLEX_ALPHA &&
                (tokens[2].LexToken == LexTokenType.ADDRLEX_NUM || tokens[2].LexToken == LexTokenType.ADDRLEX_ALPHA) &&
                tokens[3].IsDirectional()
                )
            {
                if (tokens[1].Lexum != "OLD")
                {
                    return false;
                }
                House = tokens[0];
                StreetQualifier = tokens[1];
                Street = tokens[2];
                SuffixDir = tokens[3];
                return true;
            }
            return false;
        }

        /// <summary>
        /// rule23 ==> HOUSE:NUM PREDIR:ONCHAR|TWOCHAR STREET:TWOCHAR STREETTYPE:ALPHA|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule23(List<AddressToken> tokens)
        {
            if (tokens.Count != 4)
            {
                return false;
            }
            if (tokens[0].LexToken == LexTokenType.ADDRLEX_NUM &&
                (tokens[1].LexToken == LexTokenType.ADDRLEX_ONECHAR || tokens[1].LexToken == LexTokenType.ADDRLEX_TWOCHAR) &&
                tokens[2].LexToken == LexTokenType.ADDRLEX_TWOCHAR &&
                (tokens[3].LexToken == LexTokenType.ADDRLEX_TWOCHAR || tokens[3].LexToken == LexTokenType.ADDRLEX_ALPHA)
                )
            {
                if (!tokens[1].IsDirectional())
                {
                    return false;
                }
                if (!tokens[3].IsRoadType())
                {
                    return false;
                }
                House = tokens[0];
                PrefixDir = tokens[1];
                Street = tokens[2];
                StreetType = tokens[3];
                return true;
            }
            return false;
        }

        /// <summary>
        /// rule24 ==> HOUSE:NUM STREETTYPE:ALPHA|TWOCHAR STREET:TWOCHAR SUFDIR:ONCHAR|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule24(List<AddressToken> tokens)
        {
            if (tokens.Count != 4)
            {
                return false;
            }
            if (tokens[0].LexToken == LexTokenType.ADDRLEX_NUM &&
                (tokens[1].LexToken == LexTokenType.ADDRLEX_TWOCHAR || tokens[1].LexToken == LexTokenType.ADDRLEX_ALPHA) &&
                (tokens[2].LexToken == LexTokenType.ADDRLEX_TWOCHAR || tokens[2].LexToken == LexTokenType.ADDRLEX_ALPHANUM) &&
                tokens[3].IsDirectional()
                )
            {
                if (!tokens[1].IsRoadType())
                {
                    return false;
                }
                if (tokens[2].Lexum.Length != 2)
                {
                    return false;
                }
                House = tokens[0];
                StreetType = tokens[1];
                Street = tokens[2];
                SuffixDir = tokens[3];
                return true;
            }
            return false;
        }

        /// <summary>
        /// rule25 ==> HOUSE:NUM STREETQUAL:OLD STREET:NUM|ALPHA STREETTYPE:ALPHA|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule25(List<AddressToken> tokens)
        {
            if (tokens.Count != 4)
            {
                return false;
            }
            if (tokens[0].LexToken == LexTokenType.ADDRLEX_NUM &&
                tokens[1].LexToken == LexTokenType.ADDRLEX_ALPHA &&
                (tokens[2].LexToken == LexTokenType.ADDRLEX_NUM || tokens[2].LexToken == LexTokenType.ADDRLEX_ALPHA) &&
                (tokens[3].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[3].LexToken == LexTokenType.ADDRLEX_TWOCHAR)
                )
            {
                if (!tokens[3].IsRoadType())
                {
                    return false;
                }
                if (tokens[1].Lexum != "OLD")
                {
                    return false;
                }
                House = tokens[0];
                StreetQualifier = tokens[1];
                Street = tokens[2];
                StreetType = tokens[3];
                return true;
            }
            return false;
        }

        /// <summary>
        /// rule26 ==> HOUSE:NUM PREDIR:ALPHA|TWO|ONE SAINT:ST|SAINT STREET1:ALPHA STREET2:ALPHA STREETTYPE:ALPHA|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule26(List<AddressToken> tokens)
        {
            if (tokens.Count != 6)
            {
                return false;
            }
            if (tokens[0].LexToken == LexTokenType.ADDRLEX_NUM &&
                tokens[1].IsDirectional() &&
                (tokens[2].Lexum == "ST" || tokens[2].Lexum == "SAINT") &&
                tokens[3].LexToken == LexTokenType.ADDRLEX_ALPHA &&
                tokens[4].LexToken == LexTokenType.ADDRLEX_ALPHA &&
                (tokens[5].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[5].LexToken == LexTokenType.ADDRLEX_TWOCHAR)
                )
            {
                if (!tokens[5].IsRoadType())
                {
                    return false;
                }
                House = tokens[0];
                PrefixDir = tokens[1];
                if (tokens[2].Lexum == "ST")
                {
                    tokens[2].Lexum = "SAINT";
                }
                Street = tokens[2];
                Street.Append(tokens[3]);
                Street.Append(tokens[4]);
                StreetType = tokens[5];
                return true;
            }
            return false;
        }

        /// <summary>
        /// rule27 ==> HOUSE:NUM PREDIR:ALPHA|TWO|ONE SAINT:ST|SAINT STREET1:ALPHA STREETTYPE:ALPHA|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule27(List<AddressToken> tokens)
        {
            if (tokens.Count != 5)
            {
                return false;
            }
            if (tokens[0].LexToken == LexTokenType.ADDRLEX_NUM &&
                tokens[1].IsDirectional() &&
                (tokens[2].Lexum == "ST" || tokens[2].Lexum == "SAINT") &&
                tokens[3].LexToken == LexTokenType.ADDRLEX_ALPHA &&
                (tokens[4].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[4].LexToken == LexTokenType.ADDRLEX_TWOCHAR)
                )
            {
                if (!tokens[4].IsRoadType())
                {
                    return false;
                }
                House = tokens[0];
                PrefixDir = tokens[1];
                if (tokens[2].Lexum == "ST")
                {
                    tokens[2].Lexum = "SAINT";
                }
                Street = tokens[2];
                Street.Append(tokens[3]);
                StreetType = tokens[4];
                return true;
            }
            return false;
        }

        /// <summary>
        /// rule28 ==> HOUSE:NUM PREDIR:ALPHA|TWO|ONE SAINT:ST|SAINT STREET1:ALPHA 
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule28(List<AddressToken> tokens)
        {
            if (tokens.Count != 4)
            {
                return false;
            }
            if (tokens[0].LexToken == LexTokenType.ADDRLEX_NUM &&
                tokens[1].IsDirectional() &&
                (tokens[2].Lexum == "ST" || tokens[2].Lexum == "SAINT") &&
                tokens[3].LexToken == LexTokenType.ADDRLEX_ALPHA
                )
            {
                if (tokens[3].IsRoadType())
                {
                    // could be saint st
                    return false;
                }
                House = tokens[0];
                PrefixDir = tokens[1];
                if (tokens[2].Lexum == "ST")
                {
                    tokens[2].Lexum = "SAINT";
                }
                Street = tokens[2];
                Street.Append(tokens[3]);
                return true;
            }
            return false;
        }

        /// <summary>
        /// rule29 ==> HOUSE:NUM SAINT:ST|SAINT STREET1:ALPHA 
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule29(List<AddressToken> tokens)
        {
            if (tokens.Count != 3)
            {
                return false;
            }
            if (tokens[0].LexToken == LexTokenType.ADDRLEX_NUM &&
                (tokens[1].Lexum == "ST" || tokens[1].Lexum == "SAINT") &&
                tokens[2].LexToken == LexTokenType.ADDRLEX_ALPHA
                )
            {
                if (tokens[2].IsRoadType() && tokens[1].Lexum != "ST")
                {
                    // could be saint st
                    return false;
                }
                House = tokens[0];
                if (tokens[1].Lexum == "ST")
                {
                    tokens[1].Lexum = "SAINT";
                }
                Street = tokens[1];
                Street.Append(tokens[2]);
                return true;
            }
            return false;
        }

        /// <summary>
        /// rule30 ==> HOUSE:NUM PREFIX:STHY STREET:NUM|ALPHANUM
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule30(List<AddressToken> tokens)
        {
            if (tokens.Count != 3)
            {
                return false;
            }
            if (tokens[0].LexToken == LexTokenType.ADDRLEX_NUM &&
                IsHigwaySyn(tokens[1]) &&
                (tokens[2].LexToken == LexTokenType.ADDRLEX_ALPHANUM || tokens[2].LexToken == LexTokenType.ADDRLEX_NUM))
            {
                House = tokens[0];
                StreetPrefix = tokens[1];
                Street = tokens[2];
                return true;
            }
            return false;
        }

        /// <summary>
        /// rule31 ==> HOUSE:NUM PREDIR:ALPHA|TWO|ONE PREFIX:HC STREET:NUM
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule31(List<AddressToken> tokens)
        {
            if (tokens.Count != 4)
            {
                return false;
            }
            if (tokens[0].LexToken == LexTokenType.ADDRLEX_NUM &&
                tokens[1].IsDirectional() &&
                tokens[2].Lexum == "HC" &&
                tokens[3].LexToken == LexTokenType.ADDRLEX_NUM)
            {
                House = tokens[0];
                PrefixDir = tokens[1];
                StreetPrefix = tokens[2];
                Street = tokens[3];
                return true;
            }
            return false;
        }

        /// <summary>
        /// rule32 ==> HOUSE:NUM PREDIR:ALPHA|TWO|ONE PREFIX:HC STREET:NUM SUFDIR:ALPHA|TWO|ONE
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule32(List<AddressToken> tokens)
        {
            if (tokens.Count != 5)
            {
                return false;
            }
            if (tokens[0].LexToken == LexTokenType.ADDRLEX_NUM &&
                tokens[1].IsDirectional() &&
                tokens[2].Lexum == "HC" &&
                tokens[3].LexToken == LexTokenType.ADDRLEX_NUM &&
                tokens[4].IsDirectional()
            )
            {
                House = tokens[0];
                PrefixDir = tokens[1];
                StreetPrefix = tokens[2];
                Street = tokens[3];
                SuffixDir = tokens[4];
                return true;
            }
            return false;
        }

        /// <summary>
        /// rule33 ==> PO BOX
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule33(List<AddressToken> tokens)
        {
            if (tokens.Count == 3)
            {
                if (tokens[0].Lexum == "PO" &&
                    tokens[1].Lexum == "BOX")
                {
                    if (tokens[2].LexToken == LexTokenType.ADDRLEX_NUM)
                    {
                        House = tokens[2];
                    }
                    Street = tokens[0];
                    Street.Lexum = "PO BOX";
                    return true;
                }
            }
            if (tokens.Count == 2)
            {
                if (tokens[0].Lexum == "POBOX" || tokens[0].Lexum == "BOX" || tokens[0].Lexum == "POB")
                {
                    if (tokens[1].LexToken == LexTokenType.ADDRLEX_NUM)
                    {
                        House = tokens[1];
                    }
                    Street = tokens[0];
                    Street.Lexum = "PO BOX";
                }
            }
            if (tokens.Count == 4)
            {
                if (tokens[0].Lexum == "P" && tokens[1].Lexum == "O" && tokens[2].Lexum == "BOX")
                {
                    if (tokens[3].LexToken == LexTokenType.ADDRLEX_NUM)
                    {
                        House = tokens[3];
                    }
                    Street = tokens[0];
                    Street.Lexum = "PO BOX";
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// rule34 ==> HOUSE:NUM PREDIR:ALPHA|TWO|ONE STREETQUAL:ALPHA STREET1:ALPHA STREET2:ALPHA STREETTYPE:HWY
        ///					0			1					2					3			4			5
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule34(List<AddressToken> tokens)
        {
            if (tokens.Count != 6)
            {
                return false;
            }
            if (tokens[0].LexToken == LexTokenType.ADDRLEX_NUM &&
                tokens[1].IsDirectional() &&
                tokens[2].LexToken == LexTokenType.ADDRLEX_ALPHA &&
                tokens[3].LexToken == LexTokenType.ADDRLEX_ALPHA &&
                tokens[4].LexToken == LexTokenType.ADDRLEX_ALPHA &&
                IsHigwaySyn(tokens[5])
                )
            {
                if (tokens[2].Lexum != "OLD")
                {
                    return false;
                }
                House = tokens[0];
                PrefixDir = tokens[1];
                StreetQualifier = tokens[2];
                Street = tokens[3];
                Street.Append(tokens[4]);
                StreetType = tokens[5];
                return true;
            }
            return false;
        }

        /// <summary>
        /// rule35 ==> HOUSE:NUM STREETQUAL:ALPHA STREET1:ALPHA STREET2:ALPHA STREETTYPE:HWY
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool Rule35(List<AddressToken> tokens)
        {
            if (tokens.Count != 5)
            {
                return false;
            }
            if (tokens[0].LexToken == LexTokenType.ADDRLEX_NUM &&
                tokens[1].LexToken == LexTokenType.ADDRLEX_ALPHA &&
                tokens[2].LexToken == LexTokenType.ADDRLEX_ALPHA &&
                tokens[3].LexToken == LexTokenType.ADDRLEX_ALPHA &&
                IsHigwaySyn(tokens[4])
                )
            {
                if (tokens[1].Lexum != "OLD")
                {
                    return false;
                }
                House = tokens[0];
                StreetQualifier = tokens[1];
                Street = tokens[2];
                Street.Append(tokens[3]);
                StreetType = tokens[4];
                return true;
            }
            return false;
        }

        /// <summary>
        /// ruleF0FP ==> PREDIR:ONECHAR|TWOCHAR ORDWORD:ALPHA STREETTYPE:ALPHA|TWOCHAR REAL_STREETTYPE:ALPHA|TWOCHAR
        /// FOURTH PLAIN
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool RuleF0FP(List<AddressToken> tokens)
        {
            if (tokens.Count != 4)
            {
                return false;
            }
            if (
                (tokens[0].LexToken == LexTokenType.ADDRLEX_TWOCHAR || tokens[0].LexToken == LexTokenType.ADDRLEX_ONECHAR) &&
                (tokens[1].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[1].LexToken == LexTokenType.ADDRLEX_ORDINAL) &&
                (tokens[2].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[2].LexToken == LexTokenType.ADDRLEX_TWOCHAR) &&
                (tokens[3].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[3].LexToken == LexTokenType.ADDRLEX_TWOCHAR)
                )
            {
                if (!tokens[0].IsDirectional())
                {
                    return false;
                }
                if (!tokens[2].IsRoadType())
                {
                    return false;
                }
                if (!tokens[3].IsRoadType())
                {
                    return false;
                }
                if (!tokens[1].IsOrdinalWord() && tokens[1].LexToken != LexTokenType.ADDRLEX_ORDINAL)
                {
                    return false;
                }
                if (tokens[2].Lexum != "PLAIN")
                {
                    return false;
                }
                PrefixDir = tokens[0];
                tokens[1].NormalizeOrdinalWord();
                tokens[1].Append(tokens[2]);
                Street = tokens[1];
                StreetType = tokens[3];
                return true;
            }
            return false;
        }

        /// <summary>
        /// ruleFFP ==> PREDIR:ONECHAR|TWOCHAR ORDWORD:ALPHA STREETTYPE:ALPHA|TWOCHAR
        /// FOURTH PLAIN
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool RuleFFP(List<AddressToken> tokens)
        {
            if (tokens.Count != 3)
            {
                return false;
            }
            if (
                (tokens[0].LexToken == LexTokenType.ADDRLEX_TWOCHAR || tokens[0].LexToken == LexTokenType.ADDRLEX_ONECHAR) &&
                (tokens[1].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[1].LexToken == LexTokenType.ADDRLEX_ORDINAL) &&
                (tokens[2].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[2].LexToken == LexTokenType.ADDRLEX_TWOCHAR)
                )
            {
                if (!tokens[0].IsDirectional())
                {
                    return false;
                }
                if (!tokens[2].IsRoadType())
                {
                    return false;
                }
                if (!tokens[1].IsOrdinalWord() && tokens[1].LexToken != LexTokenType.ADDRLEX_ORDINAL)
                {
                    return false;
                }
                if (tokens[2].Lexum != "PLAIN")
                {
                    return false;
                }
                PrefixDir = tokens[0];
                tokens[1].NormalizeOrdinalWord();
                tokens[1].Append(tokens[2]);
                Street = tokens[1];
                return true;
            }
            return false;
        }

        /// <summary> FILE: No House
        /// ruleF0 ==> STREET:* STREETTYPE:ALPHA|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool RuleF0(List<AddressToken> tokens)
        {
            if (tokens.Count != 2)
            {
                return false;
            }
            if (IsStreetToken(tokens[1].LexToken) &&
                (tokens[0].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[0].LexToken == LexTokenType.ADDRLEX_TWOCHAR)
                )
            {
                if (tokens[1].IsRoadType())
                {
                    Street = tokens[0];
                    StreetType = tokens[1];
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// ruleF1 ==> STREET:ALPHA|ORD|NUM
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool RuleF1(List<AddressToken> tokens)
        {
            if (tokens.Count != 1)
            {
                return false;
            }
            if (IsStreetToken(tokens[0].LexToken))
            {
                Street = tokens[0];
                return true;
            }
            return false;
        }

        /// <summary>
        /// ruleF2 ==> STREET:ALPHA|ORD|NUM STREETTYPE:ALPHA|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool RuleF2(List<AddressToken> tokens)
        {
            if (tokens.Count != 2)
            {
                return false;
            }
            if (IsStreetToken(tokens[0].LexToken) &&
                (tokens[1].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[1].LexToken == LexTokenType.ADDRLEX_TWOCHAR))
            {
                if (tokens[1].IsRoadType())
                {
                    Street = tokens[0];
                    StreetType = tokens[1];
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// ruleF3 ==> PREDIR:ONECHAR|TWOCHAR STREET:ALPHA|ORD|NUM STREETTYPE:ALPHA|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool RuleF3(List<AddressToken> tokens)
        {
            if (tokens.Count != 3)
            {
                return false;
            }
            if ((tokens[0].LexToken == LexTokenType.ADDRLEX_TWOCHAR || tokens[0].LexToken == LexTokenType.ADDRLEX_ONECHAR) &&
                IsStreetToken(tokens[1].LexToken) &&
                (tokens[2].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[2].LexToken == LexTokenType.ADDRLEX_TWOCHAR))
            {
                if (!tokens[2].IsRoadType())
                {
                    return false;
                }
                if (!tokens[0].IsDirectional())
                {
                    return false;
                }
                PrefixDir = tokens[0];
                Street = tokens[1];
                StreetType = tokens[2];
                return true;
            }
            return false;
        }

        /// <summary>
        /// ruleF4 ==> STREET:ALPHA|ORD|NUM STREETTYPE:ALPHA|TWOCHAR SUFDIR:ONECHAR|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool RuleF4(List<AddressToken> tokens)
        {
            if (tokens.Count != 3)
            {
                return false;
            }
            if (IsStreetToken(tokens[0].LexToken) &&
                (tokens[1].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[1].LexToken == LexTokenType.ADDRLEX_TWOCHAR) &&
                (tokens[2].LexToken == LexTokenType.ADDRLEX_TWOCHAR || tokens[2].LexToken == LexTokenType.ADDRLEX_ONECHAR)
                )
            {
                if (!tokens[1].IsRoadType())
                {
                    return false;
                }
                if (!tokens[2].IsDirectional())
                {
                    return false;
                }
                Street = tokens[0];
                StreetType = tokens[1];
                SuffixDir = tokens[2];
                return true;
            }
            return false;
        }

        /// <summary>
        /// ruleF5 ==> STREET:ALPHA|ORD|NUM STREETPRE:ALPHA STREETTYPE:ALPHA|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool RuleF5(List<AddressToken> tokens)
        {
            if (tokens.Count != 3)
            {
                return false;
            }
            if (tokens[1].LexToken == LexTokenType.ADDRLEX_ALPHA &&
                IsStreetToken(tokens[0].LexToken) &&
                (tokens[2].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[2].LexToken == LexTokenType.ADDRLEX_TWOCHAR)
                )
            {
                if (!tokens[2].IsRoadType())
                {
                    return false;
                }
                if (!tokens[1].IsUspsAbbr())
                {
                    return false;
                }
                if (tokens[0].IsDirectional())
                {
                    return false;
                }
                StreetPrefix = tokens[1];
                Street = tokens[0];
                StreetType = tokens[2];
                return true;
            }
            return false;
        }

        /// <summary>
        /// ruleF6 ==> STREET:ALPHA|ORD|NUM STREETPRE:ALPHA STREETTYPE:ALPHA|TWOCHAR SUFDIR:ONECHAR|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool RuleF6(List<AddressToken> tokens)
        {
            if (tokens.Count != 4)
            {
                return false;
            }
            if (IsStreetToken(tokens[0].LexToken) &&
                tokens[1].LexToken == LexTokenType.ADDRLEX_ALPHA &&
                (tokens[2].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[2].LexToken == LexTokenType.ADDRLEX_TWOCHAR) &&
                (tokens[3].LexToken == LexTokenType.ADDRLEX_ONECHAR || tokens[3].LexToken == LexTokenType.ADDRLEX_TWOCHAR)
                )
            {
                if (!tokens[2].IsRoadType())
                {
                    return false;
                }
                if (!tokens[1].IsUspsAbbr())
                {
                    return false;
                }
                Street = tokens[0];
                StreetPrefix = tokens[1];
                StreetType = tokens[2];
                SuffixDir = tokens[3];
                return true;
            }
            return false;
        }

        /// <summary>
        /// ruleF7 ==> STREETQUAL:ALPHA STREETPRE:ALPHA STREET:ALPHA|ORD|NUM SUFDIR:ONECHAR|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool RuleF7(List<AddressToken> tokens)
        {
            if (tokens.Count != 4)
            {
                return false;
            }
            if (tokens[0].LexToken == LexTokenType.ADDRLEX_ALPHA &&
                tokens[1].LexToken == LexTokenType.ADDRLEX_ALPHA &&
                IsStreetToken(tokens[2].LexToken) &&
                (tokens[3].LexToken == LexTokenType.ADDRLEX_ONECHAR || tokens[3].LexToken == LexTokenType.ADDRLEX_TWOCHAR)
                )
            {
                if (!tokens[1].IsRoadType() && tokens[1].Lexum != "STATE")
                {
                    return false;
                }
                if (!tokens[0].IsUspsAbbr() && tokens[0].Lexum != "OLD")
                {
                    return false;
                }
                if (!tokens[3].IsDirectional() && !tokens[3].IsRoadType())
                {
                    return false;
                }
                StreetQualifier = tokens[0];
                StreetPrefix = tokens[1];
                Street = tokens[2];
                if (tokens[3].IsDirectional())
                {
                    SuffixDir = tokens[3];
                }
                else if (tokens[3].IsRoadType())
                {
                    StreetType = tokens[3];
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// ruleF8 ==> STREETQUAL:ALPHA STREETPRE:ALPHA STREET:ALPHA|ORD|NUM
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool RuleF8(List<AddressToken> tokens)
        {
            if (tokens.Count != 3)
            {
                return false;
            }
            if (tokens[0].LexToken == LexTokenType.ADDRLEX_ALPHA &&
                tokens[1].LexToken == LexTokenType.ADDRLEX_ALPHA &&
                IsStreetToken(tokens[2].LexToken)
                )
            {
                if (!tokens[1].IsUspsAbbr())
                {
                    return false;
                }
                if (tokens[2].IsRoadType())
                {
                    return false;
                }
                StreetQualifier = tokens[0];
                StreetPrefix = tokens[1];
                Street = tokens[2];
                return true;
            }
            return false;
        }

        /// <summary>
        /// ruleF9 ==> PREDIR:ONECHAR|TWOCHAR STREETTYPE:ALPHA|TWOCHAR STREET:ALPHA|ORD|NUM
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool RuleF9(List<AddressToken> tokens)
        {
            if (tokens.Count != 3)
            {
                return false;
            }
            if ((tokens[0].LexToken == LexTokenType.ADDRLEX_TWOCHAR || tokens[0].LexToken == LexTokenType.ADDRLEX_ONECHAR) &&
                (tokens[1].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[1].LexToken == LexTokenType.ADDRLEX_TWOCHAR) &&
                IsStreetToken(tokens[2].LexToken)
                )
            {
                if (!tokens[1].IsRoadType())
                {
                    return false;
                }
                if (!tokens[0].IsDirectional())
                {
                    return false;
                }
                PrefixDir = tokens[0];
                Street = tokens[2];
                StreetType = tokens[1];
                return true;
            }
            return false;
        }

        /// <summary>
        /// ruleF10 ==> STREETTYPE:ALPHA|TWOCHAR STREET:ALPHA|ORD|NUM SUFDIR:ONECHAR|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool RuleF10(List<AddressToken> tokens)
        {
            if (tokens.Count != 3)
            {
                return false;
            }
            if (
                (tokens[0].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[0].LexToken == LexTokenType.ADDRLEX_TWOCHAR) &&
                IsStreetToken(tokens[1].LexToken) &&
                (tokens[2].LexToken == LexTokenType.ADDRLEX_TWOCHAR || tokens[2].LexToken == LexTokenType.ADDRLEX_ONECHAR)
                )
            {
                if (!tokens[0].IsRoadType())
                {
                    return false;
                }
                if (!tokens[2].IsDirectional())
                {
                    return false;
                }
                StreetType = tokens[0];
                Street = tokens[1];
                SuffixDir = tokens[2];
                return true;
            }
            return false;
        }

        /// <summary>
        /// ruleF11 ==> PREDIR:ONECHAR|TWOCHAR STREET:ALPHA|ORD|NUM STREETTYPE:ALPHA|TWOCHAR SUFDIR:ONECHAR|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool RuleF11(List<AddressToken> tokens)
        {
            if (tokens.Count != 4)
            {
                return false;
            }
            if (
                (tokens[0].LexToken == LexTokenType.ADDRLEX_TWOCHAR || tokens[0].LexToken == LexTokenType.ADDRLEX_ONECHAR) &&
                (tokens[2].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[2].LexToken == LexTokenType.ADDRLEX_TWOCHAR) &&
                IsStreetToken(tokens[1].LexToken) &&
                (tokens[3].LexToken == LexTokenType.ADDRLEX_TWOCHAR || tokens[3].LexToken == LexTokenType.ADDRLEX_ONECHAR)
                )
            {
                if (!tokens[0].IsDirectional())
                {
                    return false;
                }
                if (!tokens[2].IsRoadType())
                {
                    return false;
                }
                if (!tokens[3].IsDirectional())
                {
                    return false;
                }
                PrefixDir = tokens[0];
                StreetType = tokens[2];
                Street = tokens[1];
                SuffixDir = tokens[3];
                return true;
            }
            return false;
        }

        /// <summary>
        /// ruleF12 ==> PREDIR:ONECHAR|TWOCHAR STREETTYPE:ALPHA|TWOCHAR STREET:ALPHA|ORD|NUM SUFDIR:ONECHAR|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool RuleF12(List<AddressToken> tokens)
        {
            if (tokens.Count != 4)
            {
                return false;
            }
            if (
                (tokens[0].LexToken == LexTokenType.ADDRLEX_TWOCHAR || tokens[0].LexToken == LexTokenType.ADDRLEX_ONECHAR) &&
                (tokens[1].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[1].LexToken == LexTokenType.ADDRLEX_TWOCHAR) &&
                IsStreetToken(tokens[2].LexToken) &&
                (tokens[3].LexToken == LexTokenType.ADDRLEX_TWOCHAR || tokens[3].LexToken == LexTokenType.ADDRLEX_ONECHAR)
                )
            {
                if (!tokens[0].IsDirectional())
                {
                    return false;
                }
                if (!tokens[1].IsRoadType())
                {
                    return false;
                }
                if (!tokens[3].IsDirectional())
                {
                    return false;
                }
                PrefixDir = tokens[0];
                StreetType = tokens[1];
                Street = tokens[2];
                SuffixDir = tokens[3];
                return true;
            }
            return false;
        }

        /// <summary>
        /// ruleF14 ==> PREDIR:ONECHAR|TWOCHAR STREET:ALPHA|ORD|NUM SUFDIR:ONECHAR|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool RuleF14(List<AddressToken> tokens)
        {
            if (tokens.Count != 3)
            {
                return false;
            }
            if (
                (tokens[0].LexToken == LexTokenType.ADDRLEX_TWOCHAR || tokens[0].LexToken == LexTokenType.ADDRLEX_ONECHAR) &&
                IsStreetToken(tokens[1].LexToken) &&
                (tokens[2].LexToken == LexTokenType.ADDRLEX_TWOCHAR || tokens[2].LexToken == LexTokenType.ADDRLEX_ONECHAR)
                )
            {
                if (!tokens[0].IsDirectional())
                {
                    return false;
                }
                if (!tokens[2].IsDirectional())
                {
                    return false;
                }
                PrefixDir = tokens[0];
                Street = tokens[1];
                SuffixDir = tokens[2];
                return true;
            }
            return false;
        }

        /// <summary>
        /// ruleF15 ==> STREET:ONECHAR|TWOCHAR STREETTYPE:ALPHA|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool RuleF15(List<AddressToken> tokens)
        {
            if (tokens.Count != 2)
            {
                return false;
            }
            if (
                (tokens[0].LexToken == LexTokenType.ADDRLEX_ONECHAR || tokens[0].LexToken == LexTokenType.ADDRLEX_TWOCHAR) &&
                (tokens[1].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[1].LexToken == LexTokenType.ADDRLEX_TWOCHAR))
            {
                if (tokens[1].IsRoadType() && !tokens[0].IsDirectional())
                {
                    Street = tokens[0];
                    StreetType = tokens[1];
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// ruleF16 ==> STREET:ONECHAR|TWOCHAR STREETTYPE:ALPHA|TWOCHAR SUFDIR:ONECHAR|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool RuleF16(List<AddressToken> tokens)
        {
            if (tokens.Count != 3)
            {
                return false;
            }
            if (
                (tokens[0].LexToken == LexTokenType.ADDRLEX_ONECHAR || tokens[0].LexToken == LexTokenType.ADDRLEX_TWOCHAR) &&
                (tokens[1].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[1].LexToken == LexTokenType.ADDRLEX_TWOCHAR) &&
                (tokens[2].LexToken == LexTokenType.ADDRLEX_TWOCHAR || tokens[2].LexToken == LexTokenType.ADDRLEX_ONECHAR)
                )
            {
                if (!tokens[2].IsDirectional())
                {
                    return false;
                }
                if (tokens[1].IsRoadType() && !tokens[0].IsDirectional())
                {
                    Street = tokens[0];
                    StreetType = tokens[1];
                    SuffixDir = tokens[2];
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// ruleF17 ==> PREDIR:ONECHAR|TWOCHAR STREET:ALPHA|NUM|TWOCHAR|ORD
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool RuleF17(List<AddressToken> tokens)
        {
            if (tokens.Count != 2)
            {
                return false;
            }
            if (
                (tokens[0].LexToken == LexTokenType.ADDRLEX_TWOCHAR || tokens[0].LexToken == LexTokenType.ADDRLEX_ONECHAR) &&
                IsStreetToken(tokens[1].LexToken)
                )
            {
                if (!tokens[0].IsDirectional())
                {
                    return false;
                }
                PrefixDir = tokens[0];
                Street = tokens[1];
                return true;
            }
            return false;
        }

        /// <summary>
        /// ruleF18 ==> STREETTYPE:ALPHA|TWOCHAR STREET:ALPHA|ORD|NUM
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool RuleF18(List<AddressToken> tokens)
        {
            if (tokens.Count != 2)
            {
                return false;
            }
            if (
                (tokens[0].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[0].LexToken == LexTokenType.ADDRLEX_TWOCHAR) &&
                IsStreetToken(tokens[1].LexToken)
                )
            {
                if (!tokens[0].IsRoadType())
                {
                    return false;
                }
                StreetType = tokens[0];
                Street = tokens[1];
                return true;
            }
            return false;
        }

        /// <summary>
        /// ruleF19 ==> STREET:ONECHAR|TWOCHAR SUFDIR:ONECHAR|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool RuleF19(List<AddressToken> tokens)
        {
            if (tokens.Count != 2)
            {
                return false;
            }
            if (
                IsStreetToken(tokens[0].LexToken) &&
                (tokens[1].LexToken == LexTokenType.ADDRLEX_TWOCHAR || tokens[1].LexToken == LexTokenType.ADDRLEX_ONECHAR)
                )
            {
                if (!tokens[1].IsDirectional())
                {
                    return false;
                }
                Street = tokens[0];
                SuffixDir = tokens[1];
                return true;
            }
            return false;
        }

        /// <summary>
        /// ruleF20 ==> STREETYPE:TWOCHAR|ALPHA STREET:STREET KP:KP SUFDIR:ONECHAR|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool RuleF20(List<AddressToken> tokens)
        {
            if (tokens.Count != 4)
            {
                return false;
            }
            if (
                (tokens[0].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[0].LexToken == LexTokenType.ADDRLEX_TWOCHAR) &&
                IsStreetToken(tokens[1].LexToken) &&
                tokens[2].LexToken == LexTokenType.ADDRLEX_TWOCHAR &&
                (tokens[3].LexToken == LexTokenType.ADDRLEX_TWOCHAR || tokens[3].LexToken == LexTokenType.ADDRLEX_ONECHAR)
                )
            {
                if (!tokens[3].IsDirectional())
                {
                    return false;
                }
                if (tokens[2].Lexum != "KP")
                {
                    // Kitsap
                    return false;
                }
                StreetType = tokens[0];
                Street = tokens[1];
                StreetQualifier = tokens[2];
                SuffixDir = tokens[3];
                return true;
            }
            return false;
        }

        /// <summary>
        /// ruleF21 ==> STREETQUAL:ALPHA STREETPRE:ALPHA STREET:ALPHA|ORD|NUM SUFDIR:ONECHAR|TWOCHAR STREETTYPE:RD
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool RuleF21(List<AddressToken> tokens)
        {
            if (tokens.Count != 5)
            {
                return false;
            }
            if (
                tokens[0].LexToken == LexTokenType.ADDRLEX_ALPHA &&
                tokens[1].LexToken == LexTokenType.ADDRLEX_ALPHA &&
                IsStreetToken(tokens[2].LexToken) &&
                (tokens[3].LexToken == LexTokenType.ADDRLEX_ONECHAR || tokens[3].LexToken == LexTokenType.ADDRLEX_TWOCHAR) &&
                (tokens[4].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[4].LexToken == LexTokenType.ADDRLEX_TWOCHAR)
                )
            {
                if (!tokens[1].IsRoadType())
                {
                    return false;
                }
                if (!tokens[4].IsRoadType())
                {
                    return false;
                }
                if (!tokens[3].IsDirectional())
                {
                    return false;
                }
                if (!tokens[0].IsUspsAbbr() && tokens[0].Lexum != "OLD")
                {
                    return false;
                }
                StreetQualifier = tokens[0];
                StreetPrefix = tokens[1];
                Street = tokens[2];
                SuffixDir = tokens[3];
                StreetType = tokens[4];
                return true;
            }
            return false;
        }

        /// <summary>
        /// ruleF22 ==> STREETQUAL:OLD STREET:NUM|ALPHA SUFDIR:ONECHAR|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool RuleF22(List<AddressToken> tokens)
        {
            if (tokens.Count != 3)
            {
                return false;
            }
            if (
                tokens[0].LexToken == LexTokenType.ADDRLEX_ALPHA &&
                (tokens[1].LexToken == LexTokenType.ADDRLEX_NUM || tokens[1].LexToken == LexTokenType.ADDRLEX_ALPHA) &&
                (tokens[2].LexToken == LexTokenType.ADDRLEX_ONECHAR || tokens[2].LexToken == LexTokenType.ADDRLEX_TWOCHAR)
                )
            {
                if (!tokens[2].IsDirectional())
                {
                    return false;
                }
                if (tokens[0].Lexum != "OLD")
                {
                    return false;
                }
                StreetQualifier = tokens[0];
                Street = tokens[1];
                SuffixDir = tokens[2];
                return true;
            }
            return false;
        }

        /// <summary>
        /// ruleF23 ==> PREDIR:ONCHAR|TWOCHAR STREET:TWOCHAR STREETTYPE:ALPHA|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool RuleF23(List<AddressToken> tokens)
        {
            if (tokens.Count != 3)
            {
                return false;
            }
            if (
                (tokens[0].LexToken == LexTokenType.ADDRLEX_ONECHAR || tokens[0].LexToken == LexTokenType.ADDRLEX_TWOCHAR) &&
                tokens[1].LexToken == LexTokenType.ADDRLEX_TWOCHAR &&
                (tokens[2].LexToken == LexTokenType.ADDRLEX_TWOCHAR || tokens[2].LexToken == LexTokenType.ADDRLEX_ALPHA)
                )
            {
                if (!tokens[0].IsDirectional())
                {
                    return false;
                }
                if (!tokens[2].IsRoadType())
                {
                    return false;
                }
                PrefixDir = tokens[0];
                Street = tokens[1];
                StreetType = tokens[2];
                return true;
            }
            return false;
        }

        /// <summary>
        /// ruleF24 ==> STREETTYPE:ALPHA|TWOCHAR STREET:TWOCHAR SUFDIR:ONCHAR|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool RuleF24(List<AddressToken> tokens)
        {
            if (tokens.Count != 3)
            {
                return false;
            }
            if (
                (tokens[0].LexToken == LexTokenType.ADDRLEX_TWOCHAR || tokens[0].LexToken == LexTokenType.ADDRLEX_ALPHA) &&
                (tokens[1].LexToken == LexTokenType.ADDRLEX_TWOCHAR || tokens[1].LexToken == LexTokenType.ADDRLEX_ALPHANUM) &&
                (tokens[2].LexToken == LexTokenType.ADDRLEX_ONECHAR || tokens[2].LexToken == LexTokenType.ADDRLEX_TWOCHAR)
                )
            {
                if (!tokens[2].IsDirectional())
                {
                    return false;
                }
                if (!tokens[0].IsRoadType())
                {
                    return false;
                }
                if (tokens[1].Lexum.Length != 2)
                {
                    return false;
                }
                StreetType = tokens[0];
                Street = tokens[1];
                SuffixDir = tokens[2];
                return true;
            }
            return false;
        }

        /// <summary>
        /// ruleF25 ==> STREETQUAL:OLD STREET:NUM|ALPHA STREETTYPE:ALPHA|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool RuleF25(List<AddressToken> tokens)
        {
            if (tokens.Count != 3)
            {
                return false;
            }
            if (
                tokens[0].LexToken == LexTokenType.ADDRLEX_ALPHA &&
                (tokens[1].LexToken == LexTokenType.ADDRLEX_NUM || tokens[1].LexToken == LexTokenType.ADDRLEX_ALPHA) &&
                (tokens[2].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[2].LexToken == LexTokenType.ADDRLEX_TWOCHAR)
                )
            {
                if (!tokens[2].IsRoadType())
                {
                    return false;
                }
                if (tokens[0].Lexum != "OLD")
                {
                    return false;
                }
                StreetQualifier = tokens[0];
                Street = tokens[1];
                StreetType = tokens[2];
                return true;
            }
            return false;
        }

        /// <summary>
        /// ruleF26 ==> PREDIR:ALPHA|TWO|ONE SAINT:ST|SAINT STREET1:ALPHA STREET2:ALPHA STREETTYPE:ALPHA|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool RuleF26(List<AddressToken> tokens)
        {
            if (tokens.Count != 5)
            {
                return false;
            }
            if (
                tokens[0].IsDirectional() &&
                (tokens[1].Lexum == "ST" || tokens[1].Lexum == "SAINT") &&
                tokens[2].LexToken == LexTokenType.ADDRLEX_ALPHA &&
                tokens[3].LexToken == LexTokenType.ADDRLEX_ALPHA &&
                (tokens[4].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[4].LexToken == LexTokenType.ADDRLEX_TWOCHAR)
                )
            {
                if (!tokens[4].IsRoadType())
                {
                    return false;
                }
                PrefixDir = tokens[0];
                if (tokens[1].Lexum == "ST")
                {
                    tokens[1].Lexum = "SAINT";
                }
                Street = tokens[1];
                Street.Append(tokens[2]);
                Street.Append(tokens[3]);
                StreetType = tokens[4];
                return true;
            }
            return false;
        }

        /// <summary>
        /// ruleF27 ==> PREDIR:ALPHA|TWO|ONE SAINT:ST|SAINT STREET1:ALPHA STREETTYPE:ALPHA|TWOCHAR
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool RuleF27(List<AddressToken> tokens)
        {
            if (tokens.Count != 4)
            {
                return false;
            }
            if (
                tokens[0].IsDirectional() &&
                (tokens[1].Lexum == "ST" || tokens[1].Lexum == "SAINT") &&
                tokens[2].LexToken == LexTokenType.ADDRLEX_ALPHA &&
                (tokens[3].LexToken == LexTokenType.ADDRLEX_ALPHA || tokens[3].LexToken == LexTokenType.ADDRLEX_TWOCHAR)
                )
            {
                if (!tokens[3].IsRoadType())
                {
                    return false;
                }
                PrefixDir = tokens[0];
                if (tokens[1].Lexum == "ST")
                {
                    tokens[1].Lexum = "SAINT";
                }
                Street = tokens[1];
                Street.Append(tokens[2]);
                StreetType = tokens[3];
                return true;
            }
            return false;
        }

        /// <summary>
        /// ruleF30 ==> PREFIX:STHY STREET:NUM|ALPHANUM
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool RuleF30(List<AddressToken> tokens)
        {
            if (tokens.Count != 2)
            {
                return false;
            }
            if (
                IsHigwaySyn(tokens[0]) &&
                (tokens[1].LexToken == LexTokenType.ADDRLEX_ALPHANUM || tokens[1].LexToken == LexTokenType.ADDRLEX_NUM))
            {
                StreetPrefix = tokens[0];
                Street = tokens[1];
                return true;
            }
            return false;
        }

        /// <summary>
        /// ruleF31 ==> PREDIR:ALPHA|TWO|ONE PREFIX:HC STREET:NUM
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool RuleF31(List<AddressToken> tokens)
        {
            if (tokens.Count != 3)
            {
                return false;
            }
            if (
                tokens[0].IsDirectional() &&
                tokens[1].Lexum == "HC" &&
                tokens[2].LexToken == LexTokenType.ADDRLEX_NUM)
            {
                PrefixDir = tokens[0];
                StreetPrefix = tokens[1];
                Street = tokens[2];
                return true;
            }
            return false;
        }

        /// <summary>
        /// ruleF32 ==> HOUSE:NUM PREDIR:ALPHA|TWO|ONE PREFIX:HC STREET:NUM SUFDIR:ALPHA|TWO|ONE
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool RuleF32(List<AddressToken> tokens)
        {
            if (tokens.Count != 4)
            {
                return false;
            }
            if (
                tokens[0].IsDirectional() &&
                tokens[1].Lexum == "HC" &&
                tokens[2].LexToken == LexTokenType.ADDRLEX_NUM &&
                tokens[3].IsDirectional()
            )
            {
                PrefixDir = tokens[0];
                StreetPrefix = tokens[1];
                Street = tokens[2];
                SuffixDir = tokens[3];
                return true;
            }
            return false;
        }

        /// <summary>
        /// ruleF33 ==> PO BOX
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool RuleF33(List<AddressToken> tokens)
        {
            if (tokens.Count != 2)
            {
                return false;
            }
            if (tokens[0].Lexum == "PO" &&
                tokens[1].Lexum == "BOX")
            {
                tokens[0].Append(tokens[1]);
                Street = tokens[0];
                return true;
            }
            return false;
        }

        /// <summary>
        /// rule34 ==> PREDIR:ALPHA|TWO|ONE STREETQUAL:ALPHA STREET1:ALPHA STREET2:ALPHA STREETTYPE:HWY
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool RuleF34(List<AddressToken> tokens)
        {
            if (tokens.Count != 5)
            {
                return false;
            }
            if (
                tokens[0].IsDirectional() &&
                tokens[1].LexToken == LexTokenType.ADDRLEX_ALPHA &&
                tokens[2].LexToken == LexTokenType.ADDRLEX_ALPHA &&
                tokens[3].LexToken == LexTokenType.ADDRLEX_ALPHA &&
                IsHigwaySyn(tokens[4])
                )
            {
                if (tokens[1].Lexum != "OLD")
                {
                    return false;
                }
                PrefixDir = tokens[0];
                StreetQualifier = tokens[1];
                Street = tokens[2];
                Street.Append(tokens[3]);
                StreetType = tokens[4];
                return true;
            }
            return false;
        }

        /// <summary>
        /// rule35 ==> STREETQUAL:ALPHA STREET1:ALPHA STREET2:ALPHA STREETTYPE:HWY
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        private bool RuleF35(List<AddressToken> tokens)
        {
            if (tokens.Count != 4)
            {
                return false;
            }
            if (
                tokens[0].LexToken == LexTokenType.ADDRLEX_ALPHA &&
                tokens[1].LexToken == LexTokenType.ADDRLEX_ALPHA &&
                tokens[2].LexToken == LexTokenType.ADDRLEX_ALPHA &&
                IsHigwaySyn(tokens[3])
                )
            {
                if (tokens[0].Lexum != "OLD")
                {
                    return false;
                }
                StreetQualifier = tokens[0];
                Street = tokens[1];
                Street.Append(tokens[2]);
                StreetType = tokens[3];
                return true;
            }
            return false;
        }
    }
}
