using System;
using System.Diagnostics;

namespace WASalesTax.Parsing
{
    public class Date
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="dt"></param>
        public Date(DateTime dt)
        {
            var year = dt.Year;
            var month = dt.Month;
            var day = dt.Day;

            if (year < 1900 || month <= 0 || month > 12 || day <= 0 || day > 31)
            {
                throw new ArgumentOutOfRangeException("Date", "Invalid date (" + month + "/" + day + "/" + year + ")");
            }
            Year = year;
            Month = month;
            Day = day;
        }

        /// <summary>
        /// initialize a date from 20040101 format
        /// </summary>
        /// <param name="revdate"></param>
        public Date(int revdate)
        {
            Year = revdate / 10000;
            Month = (revdate - (Year * 10000)) / 100;
            Day = ((revdate - (Year * 10000)) - (Month * 100));
        }

        /// <summary>
        /// Are the two dates equal?
        /// </summary>
        /// <param name="d1"></param>
        /// <param name="d2"></param>
        /// <returns></returns>
        public static bool operator ==(Date d1, Date d2)
        {
            if ((object)d1 == (object)d2)
            {
                return true;
            }
            if (d1 is null || d2 is null)
            {
                return false;
            }
            return d1.Day == d2.Day && d1.Month == d2.Month && d1.Year == d2.Year;
        }

        /// <summary>
        /// Are the two dates equal?
        /// </summary>
        /// <param name="d1"></param>
        /// <param name="d2"></param>
        /// <returns></returns>
        public static bool operator ==(Date d1, DateTime d2)
        {
            if (d1 is null)
            {
                return d2 == DateTime.MinValue;
            }
            return d1.Equals(d2);
        }

        /// <summary>
        /// Are the two date different
        /// </summary>
        /// <param name="d1"></param>
        /// <param name="d2"></param>
        /// <returns></returns>
        public static bool operator !=(Date d1, Date d2)
        {
            if (d1 is null)
            {
                return d2 is not null;
            }
            return !d1.Equals(d2);
        }

        /// <summary>
        /// Are the two date different
        /// </summary>
        /// <param name="d1"></param>
        /// <param name="d2"></param>
        /// <returns></returns>
        public static bool operator !=(Date d1, DateTime d2)
        {
            if (d1 is null)
            {
                return d2 == DateTime.MinValue;
            }
            return d1.Day != d2.Day || d1.Month != d2.Month || d1.Year != d2.Year;
        }

        /// <summary>
        /// same as ==
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public override bool Equals(object o)
        {
            Date d2 = o as Date;
            if (d2 == null)
            {
                return false;
            }
            return Day == d2.Day && Month == d2.Month && Year == d2.Year;
        }

        /// <summary>
        /// required override
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return Month.ToString("00") + "/" + Day.ToString("00") + "/" + Year.ToString();
        }
    }
}
