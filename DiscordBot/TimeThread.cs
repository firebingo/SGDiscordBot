using SGMessageBot.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace SGMessageBot.DiscordBot
{
	public class TimeThread
	{
		private bool doRun = false;
		private int lastHour = DateTime.UtcNow.Hour;
		private int lastDay = DateTime.UtcNow.Day;
		private int lastMonth = DateTime.UtcNow.Month;
		private int lastYear = DateTime.UtcNow.Year;
		private CultureInfo culture = CultureInfo.CurrentCulture;
		public delegate Task TimePassedHandler(object sender, DateTime time);
		public static event TimePassedHandler OnHourPassed;
		public static event TimePassedHandler OnDayPassed;
		public static event TimePassedHandler OnWeekPassed;
		public static event TimePassedHandler OnMonthPassed;
		public static event TimePassedHandler OnYearPassed;

		public void Start()
		{
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

		public void AddBindings(TimePassedHandler onHourChanged = null, TimePassedHandler onDayChanged = null, 
			TimePassedHandler onWeekChanged = null, TimePassedHandler onMonthChanged = null, TimePassedHandler onYearChanged = null)
		{
			if (onHourChanged != null)
				OnHourPassed += onHourChanged;
			if (onDayChanged != null)
				onDayChanged += onDayChanged;
			if (onWeekChanged != null)
				onWeekChanged += onWeekChanged;
			if (onMonthChanged != null)
				onMonthChanged += onMonthChanged;
			if (onYearChanged != null)
				onYearChanged += onYearChanged;
		}

		public void RemoveBindings(TimePassedHandler onHourChanged = null, TimePassedHandler onDayChanged = null,
			TimePassedHandler onWeekChanged = null, TimePassedHandler onMonthChanged = null, TimePassedHandler onYearChanged = null)
		{
			try
			{
				if (onHourChanged != null)
					OnHourPassed -= onHourChanged;
				if (onDayChanged != null)
					onDayChanged -= onDayChanged;
				if (onWeekChanged != null)
					onWeekChanged -= onWeekChanged;
				if (onMonthChanged != null)
					onMonthChanged -= onMonthChanged;
				if (onYearChanged != null)
					onYearChanged -= onYearChanged;
			}
			catch(Exception ex)
			{
				ErrorLog.WriteError(ex);
			}
		}

		public void RunThread()
		{
			do
			{
				var now = DateTime.UtcNow;
				if (now.Hour != lastHour)
				{
					OnHourPassed?.Invoke(this, now);
					lastHour = now.Hour;
				}
				if (now.Day != lastDay)
				{
					OnDayPassed?.Invoke(this, now);
					lastDay = now.Day;
					if(now.DayOfWeek == culture.DateTimeFormat.FirstDayOfWeek)
						OnWeekPassed?.Invoke(this, now);
				}
				if (now.Month != lastMonth)
				{
					OnMonthPassed?.Invoke(this, now);
					lastMonth = now.Month;
				}
				if (now.Year != lastYear)
				{
					OnYearPassed?.Invoke(this, now);
					lastYear = now.Year;
				}
				Thread.Sleep(5000);
			} while (doRun);
		}
	}
}
