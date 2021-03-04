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
using System.Diagnostics;
using System.Text;

namespace WaRateFiles.Support
{
    public class Date
    {
        private int m_year;
        private int m_month;
        private int m_day;

        /// <summary>
        /// Initialize to current date
        /// </summary>
        public Date()
        {
            DateTime now = DateTime.Now;
            Init(now.Year, now.Month, now.Day);
        }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="dt"></param>
        public Date(DateTime dt)
        {
            Init(dt.Year, dt.Month, dt.Day);
        }

        /// <summary>
        /// Create a new date
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public Date(int year, int month, int day)
        {
            Init(year, month, day);
        }

        /// <summary>
        /// initialize a date from 20040101 format
        /// </summary>
        /// <param name="revdate"></param>
        public Date(int revdate)
        {
            m_year = revdate / 10000;
            m_month = (revdate - (m_year * 10000)) / 100;
            m_day = ((revdate - (m_year * 10000)) - (m_month * 100));
        }

        /// <summary>
        /// year part
        /// </summary>
        public int Year
        {
            get
            {
                return m_year;
            }
        }

        /// <summary>
        /// month part
        /// </summary>
        public int Month
        {
            get
            {
                return m_month;
            }
        }

        /// <summary>
        /// day part
        /// </summary>
        public int Day
        {
            get
            {
                return m_day;
            }
        }

        /// <summary>
        /// convert to datetime
        /// </summary>
        public DateTime AsDateTime
        {
            get
            {
                return new DateTime(m_year, m_month, m_day);
            }
        }

        public DateTime ToDateTime()
        {
            return AsDateTime;
        }

        public int ToRevInt()
        {
            return m_year * 10000 + m_month * 100 + m_day;
        }

        /// <summary>
        /// Extremely limited format cap
        /// </summary>
        /// <param name="frmt">"MMDDYY"</param>
        /// <returns></returns>
        public string Format(string frmt)
        {
            if (frmt == "MMDDYY")
            {
                Debug.Assert(1.ToString("00") == "01", "ToString format assumption failure");
                return m_month.ToString("00") + m_day.ToString("00") + TwoDigitYear(m_year).ToString("00");
            }
            throw new ArgumentException("Can't format to " + frmt);
        }

        /// <summary>
        /// Return a four digit year from a two digit one
        /// </summary>
        /// <param name="year"></param>
        /// <returns></returns>
        public static int TwoDigitYear(int year)
        {
            Debug.Assert(Int32.Parse(year.ToString().Substring(2, 2)) == year % 100, "Conversion assumption failure");
            return year % 100;
        }

        public static bool operator >(Date d1, Date d2)
        {
            if (d1.m_year > d2.Year)
            {
                return true;
            }
            if (d1.m_year == d2.Year && (d1.m_month > d2.Month || (d1.m_month == d2.Month && d1.Day > d2.Day)))
            {
                return true;
            }
            return false;
        }

        public static bool operator <(Date d1, Date d2)
        {
            if (d1.m_year < d2.Year)
            {
                return true;
            }
            if (d1.m_year == d2.Year && (d1.m_month < d2.Month || (d1.m_month == d2.Month && d1.Day < d2.Day)))
            {
                return true;
            }
            return false;
        }

        public static bool operator >(Date d1, DateTime d2)
        {
            if (d1.m_year > d2.Year)
            {
                return true;
            }
            if (d1.m_year == d2.Year && (d1.m_month > d2.Month || (d1.m_month == d2.Month && d1.Day > d2.Day)))
            {
                return true;
            }
            return false;
        }

        public static bool operator <(Date d1, DateTime d2)
        {
            if (d1.m_year < d2.Year)
            {
                return true;
            }
            if (d1.m_year == d2.Year && (d1.m_month < d2.Month || (d1.m_month == d2.Month && d1.Day < d2.Day)))
            {
                return true;
            }
            return false;
        }

        public static bool operator <=(Date d1, DateTime d2)
        {
            return d1 < d2 || d1 == d2;
        }

        public static bool operator >=(Date d1, DateTime d2)
        {
            return d1 > d2 || d1 == d2;
        }

        public static bool operator <=(Date d1, Date d2)
        {
            return d1 < d2 || d1 == d2;
        }

        public static bool operator >=(Date d1, Date d2)
        {
            return d1 > d2 || d1 == d2;
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
            if ((object)d1 == null || (object)d2 == null)
            {
                return false;
            }
            return d1.m_day == d2.Day && d1.m_month == d2.Month && d1.m_year == d2.Year;
        }

        /// <summary>
        /// Are the two dates equal?
        /// </summary>
        /// <param name="d1"></param>
        /// <param name="d2"></param>
        /// <returns></returns>
        public static bool operator ==(Date d1, DateTime d2)
        {
            if ((object)d1 == null)
            {
                return null == (object)d2;
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
            if (null == (object)d1)
            {
                return null != (object)d2;
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
            if (null == (object)d1)
            {
                return null != (object)d2;
            }
            return d1.m_day != d2.Day || d1.m_month != d2.Month || d1.m_year != d2.Year;
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
            return m_day == d2.m_day && m_month == d2.m_month && m_year == d2.m_year;
        }

        /// <summary>
        /// required override
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public int AsRevInt
        {
            get { return m_year * 10000 + m_month * 100 + m_day; }
        }

        public override string ToString()
        {
            return m_month.ToString("00") + "/" + m_day.ToString("00") + "/" + m_year.ToString();
        }

        public static Date Parse(string dt)
        {
            int mo, dy, yr;

            if (dt.IndexOf(',') > -1)
            {
                switch (dt.Substring(0, 3))
                {
                    case "Jan":
                        mo = 1;
                        break;
                    case "Feb":
                        mo = 2;
                        break;
                    case "Mar":
                        mo = 3;
                        break;
                    case "Apr":
                        mo = 4;
                        break;
                    case "May":
                        mo = 5;
                        break;
                    case "Jun":
                        mo = 6;
                        break;
                    case "Jul":
                        mo = 7;
                        break;
                    case "Aug":
                        mo = 8;
                        break;
                    case "Sep":
                        mo = 9;
                        break;
                    case "Oct":
                        mo = 10;
                        break;
                    case "Nov":
                        mo = 11;
                        break;
                    case "Dec":
                        mo = 12;
                        break;
                    default:
                        throw new FormatException();
                }
                int pos = dt.IndexOf(' ') + 1;
                int cmaidx = dt.IndexOf(',');
                dy = Int32.Parse(StringHelper.MidStr(dt, pos, cmaidx));
                yr = Int32.Parse(dt.Substring(cmaidx + 1));

                return new Date(yr, mo, dy);
            }
            if (CharCount(dt, '/') != 2)
            {
                throw new FormatException();
            }
            string[] parts = dt.Split(new char[] { '/' });
            if (!Int32.TryParse(parts[0], out mo))
            {
                throw new FormatException();
            }
            if (!Int32.TryParse(parts[1], out dy))
            {
                throw new FormatException();
            }
            if (!Int32.TryParse(parts[2], out yr))
            {
                throw new FormatException();
            }
            return new Date(yr, mo, dy);
        }

        public static bool IsDate(string dt)
        {
            return Parse(dt) != null;
        }

        public static Date Now
        {
            get { return new Date(); }
        }

        private static int CharCount(string str, char ch)
        {
            int count = 0;
            for (int x = 0; x < str.Length; x++)
            {
                if (str[x] == ch)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Init
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        private void Init(int year, int month, int day)
        {
            Debug.Assert(year > 1900, "Invalid year of " + year);
            Debug.Assert(month > 0 && month < 13, "Invalid month of " + month);
            Debug.Assert(day > 0 && day < 32, "Invalid day of " + day);

            if (year < 1900 || month <= 0 || month > 12 || day <= 0 || day > 31)
            {
                throw new ArgumentOutOfRangeException("Date", "Invalid date (" + month + "/" + day + "/" + year + ")");
            }
            m_year = year;
            m_month = month;
            m_day = day;
        }

        public static bool IsDate(int year, int mo, int day)
        {
            return mo > 0 && mo < 12 && day > 0 && day < 32;
        }
    }
}
