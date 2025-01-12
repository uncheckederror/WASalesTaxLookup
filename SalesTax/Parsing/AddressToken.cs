namespace SalesTax.Parsing
{
    /// <summary>
    /// This identifies the postal address component type.
    /// </summary>
    public enum StreetToken
    {
        UNKNOWN = 0,
        HOUSE = 1,
        PREDIR = 2,
        PRETYPE = 4,
        STREETQUALIF = 8,
        STREETPRE = 16,
        STREET = 32,
        STREETTYPE = 64,
        SUFDIR = 128,
        UNITTYPE = 256,
        UNITNUM = 512,
        COLLASE = 1024
    }

    /// <summary>
    /// A postal component of an address.
    /// </summary>
    public class AddressToken
    {
        private static readonly Lexicon NormalDirectionals = Lexicon.GetLexicon(Lexicon.LexiconNormalDirectional);
        private static readonly Lexicon Directionals = Lexicon.GetLexicon(Lexicon.LexiconDirectional);
        private static readonly Lexicon Roads = Lexicon.GetLexicon(Lexicon.LexiconCommonRoads);
        private static readonly Lexicon UncommonRoads = Lexicon.GetLexicon(Lexicon.LexiconUspsAbbr);
        private static readonly Lexicon Secondary = Lexicon.GetLexicon(Lexicon.LexiconSecondaryUnit);
        private static readonly Lexicon Ordinal = Lexicon.GetLexicon(Lexicon.LexiconOrdinalWord);

        public string Lexum { get; set; }

        public LexTokenType LexToken { get; set; }

        public StreetToken PossibleTokens { get; set; }

        public StreetToken ResultToken { get; set; }

        public AddressToken(string lexum, LexTokenType token)
        {
            Lexum = lexum;
            LexToken = token;
            ResultToken = StreetToken.UNKNOWN;

            switch (LexToken)
            {
                case LexTokenType.ADDRLEX_AMP:
                    PossibleTokens = StreetToken.COLLASE;
                    ResultToken = StreetToken.COLLASE;
                    break;
                case LexTokenType.ADDRLEX_DASH:
                    PossibleTokens = StreetToken.COLLASE;
                    ResultToken = StreetToken.COLLASE;
                    break;
                case LexTokenType.ADDRLEX_ONECHAR:
                    PossibleTokens = StreetToken.PREDIR | StreetToken.SUFDIR;
                    break;
                case LexTokenType.ADDRLEX_TWOCHAR:
                    PossibleTokens = StreetToken.PREDIR | StreetToken.SUFDIR | StreetToken.STREETTYPE;
                    break;
                case LexTokenType.ADDRLEX_FRACTION:
                    PossibleTokens = StreetToken.HOUSE | StreetToken.UNITNUM;
                    break;
                case LexTokenType.ADDRLEX_ALPHA:
                    PossibleTokens = StreetToken.STREET | StreetToken.STREETTYPE | StreetToken.UNITTYPE;
                    break;
                case LexTokenType.ADDRLEX_ALPHANUM:
                    PossibleTokens = StreetToken.UNITNUM;
                    break;
                case LexTokenType.ADDRLEX_NUM:
                    PossibleTokens = StreetToken.HOUSE | StreetToken.STREET | StreetToken.UNITNUM;
                    break;
                case LexTokenType.ADDRLEX_ORDINAL:
                    PossibleTokens = StreetToken.STREET | StreetToken.HOUSE;
                    break;
                default:
                    throw new Exception("Internal error");
            }
        }

        public bool CouldBe(StreetToken token)
        {
            return ((int)PossibleTokens & (int)token) != 0;
        }

        public bool IsNormalizedDirectional()
        {
            return NormalDirectionals.Contains(Lexum);
        }

        public bool IsDirectional()
        {
            return Directionals.Contains(Lexum);
        }

        public bool IsRoadType()
        {
            return Roads.Contains(Lexum);
        }

        public bool IsUspsAbbr()
        {
            return UncommonRoads.Contains(Lexum);
        }

        public bool IsUnit()
        {
            return Secondary.Contains(Lexum);
        }

        public bool IsOrdinalWord()
        {
            return Ordinal.Contains(Lexum);
        }

        public void NormalizeOrdinalWord()
        {
            var result = Ordinal.Substitute(Lexum);
            Lexum = result is not null ? result : Lexum;
        }

        public void NormalizeDirectional()
        {
            var result = Directionals.Substitute(Lexum);
            Lexum = result is not null ? result : Lexum;

            if (Lexum.Length == 2)
            {
                LexToken = LexTokenType.ADDRLEX_TWOCHAR;
            }
            else if (Lexum.Length == 1)
            {
                LexToken = LexTokenType.ADDRLEX_ONECHAR;
            }
        }

        public void NormalizeRoadType()
        {
            var result = UncommonRoads.Substitute(Lexum);
            Lexum = result is not null ? result : Lexum;
        }

        public void AppendDirectional(AddressToken atok)
        {
            NormalizeDirectional();
            atok.NormalizeDirectional();
            Lexum += atok.Lexum;
        }

        public void Append(AddressToken atok)
        {
            Lexum += " " + atok.Lexum;
            LexToken = LexTokenType.ADDRLEX_ALPHA;
        }

        public void ToNumeric()
        {
            string lex = "";
            for (int x = 0; x < Lexum.Length; x++)
            {
                if (char.IsDigit(Lexum[x]))
                {
                    lex += Lexum[x];
                }
            }
            Lexum = lex;
            LexToken = LexTokenType.ADDRLEX_NUM;
        }

        public void ToOrdinal()
        {
            Lexum += Lexum[^1] switch
            {
                '1' => "ST",
                '2' => "ND",
                '3' => "RD",
                _ => "TH",
            };
            LexToken = LexTokenType.ADDRLEX_ORDINAL;
        }
    }
}
