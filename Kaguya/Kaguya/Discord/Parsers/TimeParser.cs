using System;
using System.Linq;
using System.Text.RegularExpressions;
using Humanizer;
using Humanizer.Localisation;
using Kaguya.Discord.DiscordExtensions;
using Kaguya.Migrations;

namespace Kaguya.Discord.Parsers
{
	public class TimeParser
	{
		private readonly string _input;
		private readonly TimeSpan _time;

		public TimeParser(string input)
		{
			_input = input;
			_time = ParseTime();
		}

		/// <summary>
		/// Parses a string, formatted as 'XdXmXhXs' into a <see cref="System.TimeSpan"/>.
		/// Returns TimeSpan.Zero if the string could not be parsed.
		/// </summary>
		/// <returns></returns>
		public TimeSpan ParseTime()
		{
			Regex[] regexs =
			{
				new Regex("(([0-9])*s)"),
				new Regex("(([0-9])*m)"),
				new Regex("(([0-9])*h)"),
				new Regex("(([0-9])*d)")
			};

			string s = regexs[0].Match(_input).Value;
			string m = regexs[1].Match(_input).Value;
			string h = regexs[2].Match(_input).Value;
			string d = regexs[3].Match(_input).Value;

			string seconds = s.Split('s').FirstOrDefault();
			string minutes = m.Split('m').FirstOrDefault();
			string hours = h.Split('h').FirstOrDefault();
			string days = d.Split('d').FirstOrDefault();

			if (string.IsNullOrWhiteSpace(seconds) &&
			    string.IsNullOrWhiteSpace(minutes) &&
			    string.IsNullOrWhiteSpace(hours) &&
			    string.IsNullOrWhiteSpace(days))
			{
				return TimeSpan.Zero;
			}

			int sec = default, min = default, hour = default, day = default;
			if (!string.IsNullOrWhiteSpace(seconds))
				int.TryParse(seconds, out sec);

			if (!string.IsNullOrWhiteSpace(minutes))
				int.TryParse(minutes, out min);

			if (!string.IsNullOrWhiteSpace(hours))
				int.TryParse(hours, out hour);

			if (!string.IsNullOrWhiteSpace(days))
				int.TryParse(days, out day);

			return new TimeSpan(day, hour, min, sec);
		}

		public string FormattedTimestring() { return _time.Humanize(3, maxUnit: TimeUnit.Day, minUnit: TimeUnit.Second); }
	}

	public class TimeParseException : Exception
	{
		public TimeParseException(string userInputTime) : base($"{userInputTime} is an invalid time.\n" +
		                                                       $"Times are formatted in `xdxhxmxs` where `x` is a number " +
		                                                       $"and `dhms` represent `days`, `hours`, `minutes`, and `seconds` respectively.\n" +
		                                                       $"Example: `2h30m` = 2 hours and 30 minutes. 5d5s = 5 days and 5 seconds.") { }
	}
}