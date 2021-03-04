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

using WaRateFiles.Support;

namespace WaRateFiles
{
	/// <summary>
	/// Tax periods in the files are represeted as quarters.
	/// </summary>
	public class Period
	{
		private static Period m_debugCurrentPeriod;

		private int m_periodNum;
		private int m_year;
		private string m_lit;
		private int m_startDateRevInt;

		public Period(DateTime now)
		{
			Init(now);
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
			m_periodNum = num;
			m_year = year;
			m_lit = "Q" + m_periodNum.ToString() + m_year.ToString();
			m_startDateRevInt = m_year * 10000 + (m_periodNum * 3 - 2) * 100 + 1;
		}

		public Period(int startRevInt)
		{
			int year = startRevInt / 10000;
			startRevInt -= year * 10000;
			int mo = startRevInt / 100;
			startRevInt -= mo * 100;
			int day = startRevInt / 100;
			Debug.Assert(day == 1);
			Init(new DateTime(year, mo, 1));
		}

		public void Init(DateTime dtm)
		{
			m_year = dtm.Year;
			switch (dtm.Month)
			{
				case 1:
				case 2:
				case 3:
					m_periodNum = 1;
					break;
				case 4:
				case 5:
				case 6:
					m_periodNum = 2;
					break;
				case 7:
				case 8:
				case 9:
					m_periodNum = 3;
					break;
				case 10:
				case 11:
				case 12:
					m_periodNum = 4;
					break;
			}
			m_lit = "Q" + m_periodNum.ToString() + m_year.ToString();
			m_startDateRevInt = m_year * 10000 + (m_periodNum * 3 - 2) * 100 + 1;
		}

		public int Year
		{
			get { return m_year; }
		}

		public string PeriodLit
		{
			get { return m_lit; }
		}

		public int PeriodNum
		{
			get { return m_periodNum; }
		}

		public int StartDateRevInt
		{
			get { return m_startDateRevInt; }
		}

		public Date StartDate
		{
			get { return new Date(m_startDateRevInt); }
		}

		public Date EndDate
		{
			get { return new Date(new DateTime(m_year, (m_periodNum * 3 - 2), 1).AddMonths(3).AddDays(-1)); }
		}

		public Period NextPeriod
		{
			get
			{
				int periodNum = m_periodNum + 1;
				int year = m_year;

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
			if (obj is Period)
			{
				return ((Period)obj).m_periodNum == m_periodNum &&
					((Period)obj).m_year == m_year;
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
			if (null == ((object)p1))
			{
				return null == ((object)p2);
			}
			return p1.Equals(p2);
		}

		public static bool operator !=(Period p1, Period p2)
		{
			if (null == ((object)p1))
			{
				return null != ((object)p2);
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
			string syear = periodYear.Substring(2);
			if (!StringHelper.IsNumeric(snum))
			{
				throw new FormatException(snum + " must be 1-4");
			}
			if (!StringHelper.IsNumeric(syear))
			{
				throw new FormatException("Year (" + syear.ToString() + ") must be numeric");
			}
			int inum = Int32.Parse(snum);
			int iyear = Int32.Parse(syear);

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
