using System;
using System.Text.RegularExpressions;
using System.Text;
using System.IO;

namespace bmpurge
{
	enum Modes {
		BEFORE,
		AFTER,
		BETWEEN,
		NONE
	}
	
	class Program
	{
		static long ToUnixTime(DateTime date) {
			DateTime date_univ = date.ToUniversalTime();
			DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
			TimeSpan diff = date_univ - epoch;
			return (long)Math.Floor(diff.TotalSeconds);
		}
		
		public static void Main(string[] args)
		{
			if(args.Length < 3) {
				Console.WriteLine("Invalid number of arguments: expecting 2 or more!");
				return;
			}
			String path = null;
			
			for(int i = 0; i < args.Length; i++) {
				if(File.Exists(args[i])) {
					path = args[i];
					break;
				}
			}
			if(String.IsNullOrEmpty(path)) {
				Console.WriteLine("Invalid argument: please specify an existing file!");
				return;
			}
			
			bool before = false;
			bool after = false;
			String before_str = null;
			String after_str = null;
			Modes mode = Modes.NONE;
			for(int i = 0; i < args.Length; i++) {
				args[i] = args[i].Trim();
				
				if(
					(
						(
							(args[i] == "-before" && !before) || (args[i] == "-after" && !after) // if arg is before/after and no before/after arg has been found yet
						) && (
							args.Length <= (i + 1) // but if there is no index after the arg
							||
							(args[i+1] == "-before" || args[i+1] == "-after") // or if the next arg is after/before instead of a timestamp
						)
					)
				) {
					// then -> invalid arg: no timestamp
					Console.WriteLine("Invalid argument: no time stamp specified!");
					return;
				}
				// if arg is before but before has already been found
				if((args[i] == "-before" && before) || (args[i] == "-after" && after)) {
					// then skip
					continue;
				}
				
				if(args[i] == "-before") {
					before = true;
					before_str = args[i+1];
					mode = Modes.BEFORE;
				} else if(args[i] == "-after") {
					after = true;
					after_str = args[i+1];
					mode = Modes.AFTER;
				}
			}
			if(before && after) {
				mode = Modes.BETWEEN;
			} 
			if(!before && !after) {
				Console.WriteLine("Invalid argument: missing arguments!");
				return;
			}
			
			// first index: default before date: 1/1/1970
			// second index: default after date: 1/1/2500
			// if mistakenly not read from args[] then nothing will be deleted
			// since no bookmarks should have been created before 01 Jan 1970 or after 01 Jan 2500
			long[] dates = {0,16725225600};
			
			if(before) {
				DateTime beforeT;
				if(String.IsNullOrEmpty(before_str) || !DateTime.TryParse(before_str, out beforeT)) {
					Console.WriteLine("Invalid argument: could not read 'before' timestamp!");
					return;
				}
				
				dates[0] = ToUnixTime(new DateTime(beforeT.Ticks, DateTimeKind.Local));
			}
			
			if(after) {
				DateTime afterT;
				if(String.IsNullOrEmpty(after_str) || !DateTime.TryParse(after_str, out afterT)) {
					Console.WriteLine("Invalid argument: could not read 'after' timestamp!");
					return;
				}
				
				dates[1] = ToUnixTime(new DateTime(afterT.Ticks, DateTimeKind.Local));
			}			
			
			// if document isn't a netscape bookmarks file, quit
			String file_content = File.ReadAllText(path, Encoding.UTF8);
			String result = file_content;
			if(file_content.Length == 0 || !file_content.Contains("<!DOCTYPE NETSCAPE-Bookmark-file-1>")) {
				Console.WriteLine("Invalid argument: file is empty or invalid.");
				return;
			}
			
			// regex (unescaped): /<a[^>^<]*add_date="(?'date'\d*)"[^>^<]*>[^>^<]*<\/a>/gmis
			Regex bookmark_regex = new Regex("\\s*<dt><a[^>^<]*add_date=\"(?'date'\\d*)\"[^>^<]*>(?'title'[^>^<]*)<\\/a>\\s", RegexOptions.IgnoreCase | RegexOptions.Singleline);
			MatchCollection matches = bookmark_regex.Matches(file_content);
			if(matches.Count == 0) {
				Console.WriteLine("No results found!");
				return;
			}
			
			foreach(Match m in matches) {
				if(!m.Groups["date"].Success || !m.Groups["title"].Success)
					continue;
				
				long date = Int64.Parse(m.Groups["date"].Value);
				
				switch(mode) {
					case Modes.AFTER:
						if(date > dates[1]) {
							result = result.Replace(m.Value, "");
							Console.WriteLine("REMOVED: {0}", m.Groups["title"].Value);
						}
						break;
					case Modes.BEFORE:
						if(date < dates[0]) {
							result = result.Replace(m.Value, "");
							Console.WriteLine("REMOVED: {0}", m.Groups["title"].Value);
						}
						break;
					case Modes.BETWEEN:
						if(date > dates[1] && date < dates[0]) {
							result = result.Replace(m.Value, "");
							Console.WriteLine("REMOVED: {0}", m.Groups["title"].Value);
						}
						break;
					case Modes.NONE:
						Console.WriteLine("An unknown error occured.");
						break;
				}
			}
			File.WriteAllText(path, result, Encoding.UTF8);
			return;
		}
	}
}