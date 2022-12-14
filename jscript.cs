using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Forms;

public class Program {
	static HashSet<string> IncludedPaths = new HashSet<string>();
	
	static string systemIncludePath_ = null;
	
	static string GetSystemIncludePath() {
		if (systemIncludePath_ == null) {
			systemIncludePath_ = Environment.GetEnvironmentVariable("JSCRIPT_INCLUDE");
			if (systemIncludePath_ != null) {
				systemIncludePath_ = Path.GetFullPath(systemIncludePath_);
			} else {
				systemIncludePath_ = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "jscript/include");
			}
		}
		return systemIncludePath_;
	}
	
	static string GetIncludePath(string path, string currentPath) {
		path = path.Replace("/", "\\");
		string basePath = path.StartsWith(".") ? Path.GetDirectoryName(currentPath) : GetSystemIncludePath();
		return Path.GetFullPath(Path.Combine(basePath, path));
	}
	
	static void Include(string path, StreamWriter writer) {
		IncludedPaths.Add(path);
		int LineNumber = 0;
		writer.WriteLine("//##jscript##;" + path + ";" + LineNumber);
		using (var reader = new StreamReader(path)) {
			while (reader.Peek() != -1) {
				LineNumber ++;
				string line = reader.ReadLine();
				writer.WriteLine(line);
				Match m = Regex.Match(line, @"^//#import\s*(\w+)\s*from\s*""([^""]+)""");
				if (m.Success) {
					var name = m.Groups[1].Value;
					var incPath = GetIncludePath(m.Groups[2].Value, path);
					var incPathKey = incPath.Replace("\\", "/").ToUpper();
					if (!IncludedPaths.Contains(incPath)) {
						var dir = Path.GetDirectoryName(incPath).Replace("\\", "\\\\");
						writer.WriteLine(";(function() {const module = {exports:{}}, __dir = '" + dir + "';");
						Include(Path.Combine(Path.GetDirectoryName(path), incPath), writer);
						writer.WriteLine(";__modules['" + incPathKey + "'] = module.exports;})();");
					}
					writer.WriteLine("const " + name + " = __modules['" + incPathKey + "'];");
					writer.WriteLine("//##jscript##;" + path + ";" + LineNumber);
				}
			}
		}
	}
	
	static void Preprocess(string[] args) {
		var srcPath = Path.GetFullPath(args[0]);
		var exePath = srcPath + ".exe.js";
		using (var writer = new StreamWriter(exePath)) {
			writer.WriteLine("const __modules = {};");
			Include(srcPath, writer);
		}
		args[0] = exePath;
	}
	
	static string Escape(string s) {
		if (s.IndexOf(" ") != -1) {
			s = "\"" + s.Replace("\"", "\\\"") + "\"";
		}
		return s;
	}
	
	static string ConvertSourceInfo(string message, string exePath) {
		if (message.StartsWith(exePath)) {
			Match m = Regex.Match(message.Substring(exePath.Length), @"^\((\d+),\s*(\d+)\)(.*)");
			if (m.Success) {
				int LineNum = int.Parse(m.Groups[1].Value);
				int ColumnNum = int.Parse(m.Groups[2].Value);
				string messageBody = m.Groups[3].Value;
				string SourcePath = "";
				int SourceLine = 0;
				string line = "";
				using (var reader = new StreamReader(exePath)) {
					for (var i = 0; i < LineNum && reader.Peek() != -1; i++) {
						line = reader.ReadLine();
						m = Regex.Match(line, @"^//##jscript##;([^;]+);(\d+)");
						if (m.Success) {
							SourcePath = m.Groups[1].Value;
							SourceLine = int.Parse(m.Groups[2].Value);
						} else {
							SourceLine ++;
						}
					}
				}
				message = SourcePath + "(" + SourceLine + ", " + ColumnNum + ")" + messageBody + "\r\n" + line;
			}
		}
		return message;
	}

	static int Execute(string[] args) {
		string argsText = "";
		for (var i = 0; i < args.Length; i++) {
			argsText += " " + Escape(args[i]);
		}
		using (var process = new Process()) {
			process.StartInfo = new ProcessStartInfo() {
				FileName = @"CScript.exe",
				UseShellExecute = false,
				CreateNoWindow = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				Arguments = "//NOLOGO //E:{1B7CD997-E5FF-4932-A7A6-2A9E636DA385}" + argsText
			};
			
			process.OutputDataReceived += (sender, e) => {
				if (e.Data != null) {
					Console.WriteLine(e.Data);
				}
			};
			process.ErrorDataReceived += (sender, e) => {
				if (e.Data != null) {
					Console.Error.WriteLine(ConvertSourceInfo(e.Data, args[0]));
				}
			};

			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();
			process.WaitForExit();
			process.CancelOutputRead(); // ??????????????????????????????
			process.CancelErrorRead();
			return process.ExitCode;
		}
	}

	public static int Main(string[] args) {
		Preprocess(args);
		return Execute(args);
	}
}
