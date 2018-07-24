using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace SGMessageBot.Bot
{
	public class TimeThread
	{
		private bool doRun = false;
		private int lastHour = DateTime.UtcNow.Hour;
		private int lastDay = DateTime.UtcNow.Day;
		private int lastMonth = DateTime.UtcNow.Month;
		private int lastYear = DateTime.UtcNow.Year;
		private CultureInfo culture = CultureInfo.CurrentCulture;
		public delegate void TimePassedHandler(object sender, DateTime time);
		public static event TimePassedHandler onHourPassed;
		public static event TimePassedHandler onDayPassed;
		public static event TimePassedHandler onWeekPassed;
		public static event TimePassedHandler onMonthPassed;
		public static event TimePassedHandler onYearPassed;

		public void Start()
		{
			onHourPassed += SGMessageBot.statTracker.onHourChanged;
			onDayPassed += SGMessageBot.statTracker.onDayChanged;
			onWeekPassed += SGMessageBot.statTracker.onWeekChanged;
			onMonthPassed += SGMessageBot.statTracker.onMonthChanged;
			onYearPassed += SGMessageBot.statTracker.onYearChanged;

			var now = DateTime.UtcNow;
			int lastHour = now.Hour;
			int lastDay = now.Day;
			int lastMonth = now.Month;
			int lastYear = now.Year;

			Thread runThread = new Thread(RunThread)
			{
				Name = "TimeThread",
				IsBackground = true
			};
			runThread.Start();
			doRun = true;
		}

		public void Stop()
		{
			doRun = false;
		}

		public void RunThread()
		{
			do
			{
				var now = DateTime.UtcNow;
				if (now.Hour != lastHour)
				{
					onHourPassed?.Invoke(this, now);
					lastHour = now.Hour;
				}
				if (now.Day != lastDay)
				{
					onDayPassed?.Invoke(this, now);
					lastDay = now.Day;
					if(now.DayOfWeek == culture.DateTimeFormat.FirstDayOfWeek)
						onWeekPassed?.Invoke(this, now);
				}
				if (now.Month != lastMonth)
				{
					onMonthPassed?.Invoke(this, now);
					lastMonth = now.Month;
				}
				if (now.Year != lastYear)
				{
					onYearPassed?.Invoke(this, now);
					lastYear = now.Year;
				}
				Thread.Sleep(5000);
			} while (doRun);
		}
	}
}
