using System.Diagnostics;
using System;
using System.Text;
using System.Threading;

namespace EmnExtensions {
	/// <summary>
	/// Useful in LINQpad queries
	/// </summary>
	public static class WinProcessUtil {
		public struct ExecutionResult {
			public string StandardOutputContents, StandardErrorContents;
			public int ExitCode;
		}
		public static ExecutionResult ExecuteProcessSynchronously(string filename, string arguments, string input) {
			using (
				var proc = Process.Start(
					new ProcessStartInfo {
						CreateNoWindow = true,//don't need UI.
						RedirectStandardOutput = true,
						RedirectStandardError = true,
						RedirectStandardInput = true, //so we can capture and control the new process's standard I/O
						UseShellExecute = false,//required to be able to redirect streams
						FileName = filename,
						Arguments = arguments,
					}
				)) {
				StringBuilder error = new StringBuilder(), output = new StringBuilder();
				Thread.MemoryBarrier();
				proc.ErrorDataReceived += (s, e) => error.Append(e.Data);
				proc.OutputDataReceived += (s, e) => output.Append(e.Data);
				proc.BeginErrorReadLine();
				proc.BeginOutputReadLine();
				if (input != null)
					proc.StandardInput.Write(input);
				proc.StandardInput.Close();
				proc.WaitForExit();
				Thread.MemoryBarrier();
				return new ExecutionResult { StandardOutputContents = output.ToString(), StandardErrorContents = error.ToString(), ExitCode = proc.ExitCode };
			}
		}

	}
}

