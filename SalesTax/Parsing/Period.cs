using System.Diagnostics;

namespace SalesTax.Parsing
{
    /// <summary>
    /// Tax periods in the files are represeted as quarters.
    /// </summary>
    public class Period
    {
        private static Period m_debugCurrentPeriod;

        public int PeriodNumber { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public string PeriodLit { get; set; }
        public int StartDateRevInt { get; set; }

        public Period(DateTime dtm)
        {
            Year = dtm.Year;
            switch (dtm.Month)
            {
                case 1:
                case 2:
                case 3:
                    PeriodNumber = 1;
                    Month = 12;
                    break;
                case 4:
                case 5:
                case 6:
                    PeriodNumber = 2;
                    Month = 03;
                    break;
                case 7:
                case 8:
                case 9:
                    PeriodNumber = 3;
                    Month = 06;
                    break;
                case 10:
                case 11:
                case 12:
                    PeriodNumber = 4;
                    Month = 09;
                    break;
            }
            PeriodLit = "Q" + PeriodNumber.ToString() + Year.ToString();
            StartDateRevInt = Year * 10000 + (PeriodNumber * 3 - 2) * 100 + 1;
        }

        public Period(int num, int year)
        {
            if (num < 1 || num > 4)
            {
                throw new FormatException("Period number must be in the range [1,4]");
            }
            if (year < 1900)
            {
                throw new FormatException("Please use a four digit year");
            }
            PeriodNumber = num;
            Year = year;
            PeriodLit = "Q" + PeriodNumber.ToString() + Year.ToString();
            StartDateRevInt = Year * 10000 + (PeriodNumber * 3 - 2) * 100 + 1;
        }

        public Period(int startRevInt)
        {
            int year = startRevInt / 10000;
            startRevInt -= year * 10000;
            int mo = startRevInt / 100;
            startRevInt -= mo * 100;
            int day = startRevInt / 100;
            Debug.Assert(day == 1);
            var dtm = new DateTime(year, mo, 1);
            Year = dtm.Year;
            switch (dtm.Month)
            {
                case 1:
                case 2:
                case 3:
                    PeriodNumber = 1;
                    break;
                case 4:
                case 5:
                case 6:
                    PeriodNumber = 2;
                    break;
                case 7:
                case 8:
                case 9:
                    PeriodNumber = 3;
                    break;
                case 10:
                case 11:
                case 12:
                    PeriodNumber = 4;
                    break;
            }
            PeriodLit = "Q" + PeriodNumber.ToString() + Year.ToString();
            StartDateRevInt = Year * 10000 + (PeriodNumber * 3 - 2) * 100 + 1;
        }

        public Date StartDate
        {
            get { return new Date(StartDateRevInt); }
        }

        public Date EndDate
        {
            get { return new Date(new DateTime(Year, PeriodNumber * 3 - 2, 1).AddMonths(3).AddDays(-1)); }
        }

        public Period NextPeriod
        {
            get
            {
                int periodNum = PeriodNumber + 1;
                int year = Year;

                if (periodNum > 4)
                {
                    periodNum = 1;
                    year++;
                }

                return new Period(periodNum, year);
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is Period period)
            {
                return period.PeriodNumber == PeriodNumber &&
                    period.Year == Year;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override string ToString()
        {
            return PeriodLit;
        }

        public static bool operator ==(Period p1, Period p2)
        {
            if (p1 is null)
            {
                return p2 is null;
            }
            return p1.Equals(p2);
        }

        public static bool operator !=(Period p1, Period p2)
        {
            if (p1 is null)
            {
                return p2 is not null;
            }
            return !p1.Equals(p2);
        }

        public static Period CurrentPeriod()
        {
            if (null != m_debugCurrentPeriod)
            {
                return m_debugCurrentPeriod;
            }
            return new Period(DateTime.Now);
        }

        public static Period Parse(string periodYear)
        {
            if (periodYear.Length != 6)
            {
                throw new FormatException("Period year must be 6 characters.");
            }
            if (periodYear[0] != 'Q')
            {
                throw new FormatException("Frequency (first character) must be a Q.");
            }
            string snum = periodYear.Substring(1, 1);
            string syear = periodYear[2..];
            if (!snum.IsNumeric())
            {
                throw new FormatException(snum + " must be 1-4");
            }
            if (!syear.IsNumeric())
            {
                throw new FormatException("Year (" + syear.ToString() + ") must be numeric");
            }
            int inum = int.Parse(snum);
            int iyear = int.Parse(syear);

            if (inum < 1 || inum > 4)
            {
                throw new FormatException("Period number must be in the range [1,4]");
            }
            if (iyear < 1900)
            {
                throw new FormatException("Please use a four digit year");
            }
            return new Period(inum, iyear);
        }

        public static Period DebugCurrentPeriod
        {
            get { return m_debugCurrentPeriod; }
            set { m_debugCurrentPeriod = value; }
        }
    }
}
