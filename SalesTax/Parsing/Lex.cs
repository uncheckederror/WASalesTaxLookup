using System.Text;

namespace SalesTax.Parsing
{
    public enum LexTokenType
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
    public class Lex
    {
        public enum LexState
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

        public string Text;
        public int Position;
        public StringBuilder Lexum = new();
        public LexState State;

        void AppendLex(char ch)
        {
            Lexum.Append(ch);
        }

        bool IsEOF()
        {
            return Position >= Text.Length;
        }

        static bool IsSeperator(char ch)
        {
            return char.IsWhiteSpace(ch) || ch == '&' || ch == '\0';
        }

        public Lex(string text)
        {
            Text = text;
            Position = 0;
            State = LexState.LEX_START;
            Lexum.Length = 0;
        }

        public bool NextToken(out LexTokenType token)
        {
            Lexum.Length = 0;

            if (IsEOF())
            {
                token = LexTokenType.ADDRLEX_EOF;
                return true;
            }

            State = LexState.LEX_START;
            while (true)
            {
                char ch;
                if (IsEOF())
                {
                    ch = '\0';
                }
                else
                {
                    if ((ch = char.ToUpper(Text[Position++])) == '.' || ch == ',')
                    {
                        continue;
                    }
                }
                switch (State)
                {
                    case LexState.LEX_START:
                        if (char.IsWhiteSpace(ch))
                        {
                            continue;
                        }
                        AppendLex(ch);

                        if (ch == '-')
                        {
                            State = LexState.LEX_ALPHANUM;
                            break;
                        }
                        if (ch == '&')
                        {
                            token = LexTokenType.ADDRLEX_AMP;
                            return true;
                        }
                        if (char.IsDigit(ch))
                        {
                            State = LexState.LEX_NUM;
                            break;
                        }
                        State = LexState.LEX_ALPHA1;
                        break;

                    case LexState.LEX_NUM:
                        //
                        //  We've read one or more numbers.  It may be a number
                        //  123, an ordinal 123rd, or an alphanumeric 123abc.
                        //
                        if (IsSeperator(ch))
                        {
                            if (!IsEOF())
                            {
                                Position--;
                            }
                            token = LexTokenType.ADDRLEX_NUM;
                            return true;
                        }
                        if (!char.IsDigit(ch))
                        {
                            if (Lexum.EndsWith("1") && (ch == 'S' || ch == 's'))
                            {
                                AppendLex(ch);
                                State = LexState.LEX_ORD_T;
                                break;
                            }
                            if (Lexum.EndsWith("2") && (ch == 'R' || ch == 'r' || ch == 'N' || ch == 'n'))
                            {
                                AppendLex(ch);
                                State = LexState.LEX_ORD_D;
                                break;
                            }
                            if (Lexum.EndsWith("3") && (ch == 'R' || ch == 'r'))
                            {
                                AppendLex(ch);
                                State = LexState.LEX_ORD_D;
                                break;
                            }
                            if (ch == 't' || ch == 'T')
                            {
                                AppendLex(ch);
                                State = LexState.LEX_ORD_H;
                                break;
                            }
                            if (ch == '/')
                            {
                                AppendLex(ch);
                                State = LexState.LEX_FRACT;
                                break;
                            }
                            State = LexState.LEX_ALPHANUM;
                        }
                        AppendLex(ch);
                        break;

                    case LexState.LEX_FRACT:
                        if (!char.IsDigit(ch))
                        {
                            if (!IsEOF())
                            {
                                Position--;
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
                        if (IsSeperator(ch))
                        {
                            if (!IsEOF())
                            {
                                Position--;
                            }
                            token = LexTokenType.ADDRLEX_ONECHAR;
                            return true;
                        }
                        AppendLex(ch);
                        if (char.IsLetter(ch))
                        {
                            State = LexState.LEX_ALPHA2;
                        }
                        else
                        {
                            State = LexState.LEX_ALPHANUM;
                        }
                        break;

                    case LexState.LEX_ALPHA2:
                        //
                        //  Two character have been read
                        //
                        if (IsSeperator(ch))
                        {
                            if (!IsEOF())
                            {
                                Position--;
                            }
                            token = LexTokenType.ADDRLEX_TWOCHAR;
                            return true;
                        }
                        AppendLex(ch);
                        if (char.IsLetter(ch))
                        {
                            State = LexState.LEX_ALPHA;
                        }
                        else
                        {
                            State = LexState.LEX_ALPHANUM;
                        }
                        break;

                    case LexState.LEX_ALPHA:
                        //
                        //  Read until break or digit
                        //
                        if (IsSeperator(ch))
                        {
                            if (!IsEOF())
                            {
                                Position--;
                            }
                            token = LexTokenType.ADDRLEX_ALPHA;
                            return true;
                        }
                        AppendLex(ch);
                        if (!char.IsLetter(ch))
                        {
                            State = LexState.LEX_ALPHANUM;
                        }
                        break;

                    case LexState.LEX_ALPHANUM:
                        //
                        //  Read until break;
                        //
                        if (IsSeperator(ch))
                        {
                            if (!IsEOF())
                            {
                                Position--;
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
                                Position--;
                            }
                            token = LexTokenType.ADDRLEX_ALPHANUM;
                            return true;
                        }
                        if (ch == 't' || ch == 'T')
                        {
                            AppendLex(ch);
                            State = LexState.LEX_ORD_END;
                            break;
                        }
                        Position--;
                        State = LexState.LEX_ALPHANUM;
                        break;

                    case LexState.LEX_ORD_D:
                        //
                        //  Read the 'D' after '2R'
                        //
                        if (IsSeperator(ch))
                        {
                            if (!IsEOF())
                            {
                                Position--;
                            }
                            token = LexTokenType.ADDRLEX_ALPHANUM;
                            return true;
                        }
                        if (ch == 'd' || ch == 'D')
                        {
                            AppendLex(ch);
                            State = LexState.LEX_ORD_END;
                            break;
                        }
                        Position--;
                        State = LexState.LEX_ALPHANUM;
                        break;

                    case LexState.LEX_ORD_H:
                        //
                        //  Read the 'H' after '5T'
                        //
                        if (IsSeperator(ch))
                        {
                            if (!IsEOF())
                            {
                                Position--;
                            }
                            token = LexTokenType.ADDRLEX_ALPHANUM;
                            return true;
                        }
                        if (ch == 'h' || ch == 'H')
                        {
                            AppendLex(ch);
                            State = LexState.LEX_ORD_END;
                            break;
                        }
                        Position--;
                        State = LexState.LEX_ALPHANUM;
                        break;

                    case LexState.LEX_ORD_END:
                        if (IsSeperator(ch))
                        {
                            if (!IsEOF())
                            {
                                Position--;
                            }
                            token = LexTokenType.ADDRLEX_ORDINAL;
                            return true;
                        }
                        AppendLex(ch);
                        State = LexState.LEX_ALPHANUM;
                        break;

                    default:
                        throw new Exception("Internal error");
                }
            }
        }
    }
}
