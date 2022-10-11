using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Forms;

public class Program {
	static string SystemIncludePath_ = null;
	
	static string GetSystemIncludePath() {
		if (SystemIncludePath_ == null) {
			SystemIncludePath_ = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "include");
		}
		return SystemIncludePath_;
	}
	
	static HashSet<string> IncludedPaths = new HashSet<string>();
	
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
					var incPath = Path.Combine(Path.GetDirectoryName(path), m.Groups[2].Value.Replace('/', '\\'));
					if (!IncludedPaths.Contains(incPath)) {
						writer.WriteLine(";(function() {var module = {exports:{}};");
						Include(Path.Combine(Path.GetDirectoryName(path), incPath), writer);
						writer.WriteLine(";__modules['" + incPath + "'] = module.exports;})();");
					}
					writer.WriteLine("var " + name + " = __modules['" + incPath + "'];");
					writer.WriteLine("//##jscript##;" + path + ";" + LineNumber);
				}
			}
		}
	}
	
	static void Preprocess(string[] args) {
		var srcPath = Path.GetFullPath(args[0]);
		var exePath = srcPath + ".exe.js";
		using (var writer = new StreamWriter(exePath)) {
			writer.WriteLine("var __modules = {};");
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
			process.CancelOutputRead(); // 使い終わったら止める
			process.CancelErrorRead();
			return process.ExitCode;
		}
	}

	public static int Main(string[] args) {
		Preprocess(args);
		return Execute(args);
	}
}
