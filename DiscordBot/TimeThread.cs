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
			try
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
			catch (Exception ex)
			{
				ErrorLog.WriteError(ex);
			}
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
			catch (Exception ex)
			{
				ErrorLog.WriteError(ex);
			}
		}

		public void RunThread()
		{
			do
			{
				try
				{
					DebugLog.WriteLog(DebugLogTypes.StatTracker, () => "TimeThread.RunThread looping");
					var now = DateTime.UtcNow;
					DebugLog.WriteLog(DebugLogTypes.StatTracker, () => $"now.Hour: {now.Hour}, lastHour: {lastHour}");
					if (now.Hour != lastHour)
					{
						DebugLog.WriteLog(DebugLogTypes.StatTracker, () => $"Invoking OnHourPassed, InvocationList.Length: {OnHourPassed.GetInvocationList().Length}");
						OnHourPassed?.Invoke(this, now);
						lastHour = now.Hour;
					}
					DebugLog.WriteLog(DebugLogTypes.StatTracker, () => $"now.Day: {now.Day}, lastDay: {lastDay}");
					if (now.Day != lastDay)
					{
						DebugLog.WriteLog(DebugLogTypes.StatTracker, () => $"Invoking OnDayPassed, InvocationList.Length: {OnDayPassed.GetInvocationList().Length}");
						OnDayPassed?.Invoke(this, now);
						lastDay = now.Day;
						DebugLog.WriteLog(DebugLogTypes.StatTracker, () => $"now.DayOfWeek: {now.DayOfWeek}, FirstDayOfWeek: {culture.DateTimeFormat.FirstDayOfWeek}");
						if (now.DayOfWeek == culture.DateTimeFormat.FirstDayOfWeek)
						{
							DebugLog.WriteLog(DebugLogTypes.StatTracker, () => $"Invoking OnWeekPassed, InvocationList.Length: {OnWeekPassed.GetInvocationList().Length}");
							OnWeekPassed?.Invoke(this, now);
						}
					}
					DebugLog.WriteLog(DebugLogTypes.StatTracker, () => $"now.Month: {now.Month}, lastMonth: {lastMonth}");
					if (now.Month != lastMonth)
					{
						DebugLog.WriteLog(DebugLogTypes.StatTracker, () => $"Invoking OnMonthPassed, InvocationList.Length: {OnMonthPassed.GetInvocationList().Length}");
						OnMonthPassed?.Invoke(this, now);
						lastMonth = now.Month;
					}
					DebugLog.WriteLog(DebugLogTypes.StatTracker, () => $"now.Year: {now.Year}, lastYear: {lastYear}");
					if (now.Year != lastYear)
					{
						DebugLog.WriteLog(DebugLogTypes.StatTracker, () => $"Invoking OnYearPassed, InvocationList.Length: {OnYearPassed.GetInvocationList().Length}");
						OnYearPassed?.Invoke(this, now);
						lastYear = now.Year;
					}
				}
				catch (Exception ex)
				{
					ErrorLog.WriteError(ex);
				}
				Thread.Sleep(300000); //5 minutes
			} while (doRun);
		}
	}
}
