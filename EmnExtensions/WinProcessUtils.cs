using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace EmnExtensions
{
    public struct ProcessExecutionResult
    {
        public string StandardOutputContents, StandardErrorContents;
        public int ExitCode;
    }

    public struct ProcessStartOptions
    {
        public ProcessPriorityClass? Priority;
        public Encoding StandardInputEncoding;
        public Encoding StandardOutputAndErrorEncoding;
        public Encoding StandardErrorOverrideEncoding;
        public string WorkingDirectory;
    }

    /// <summary>
    /// Useful in LINQpad queries
    /// </summary>
    public static class WinProcessUtil
    {
        public static ProcessExecutionResult ExecuteProcessSynchronously(string filename, string arguments, string input, ProcessStartOptions startOptions = new())
        {
            var processStartInfo = new ProcessStartInfo {
                CreateNoWindow = true, //don't need UI.
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true, //so we can capture and control the new process's standard I/O
                UseShellExecute = false, //required to be able to redirect streams
                FileName = filename,
                Arguments = arguments,
                WorkingDirectory = startOptions.WorkingDirectory,
            };
            if (startOptions.StandardOutputAndErrorEncoding != null) {
                processStartInfo.StandardOutputEncoding = startOptions.StandardOutputAndErrorEncoding;
            }

            if (startOptions.StandardErrorOverrideEncoding != null) {
                processStartInfo.StandardErrorEncoding = startOptions.StandardErrorOverrideEncoding;
            }

            using (
                var proc = Process.Start(processStartInfo)) {
                if (startOptions.Priority != null) {
                    proc.PriorityClass = startOptions.Priority.Value;
                }

                StringBuilder error = new(), output = new();
                Thread.MemoryBarrier();
                proc.ErrorDataReceived += (s, e) => error.Append(e.Data);
                proc.OutputDataReceived += (s, e) => output.Append(e.Data);
                proc.BeginErrorReadLine();
                proc.BeginOutputReadLine();
                using (var inputStream =
                    startOptions.StandardInputEncoding != null
                        ? new(proc.StandardInput.BaseStream, startOptions.StandardInputEncoding)
                        : proc.StandardInput) {
                    if (input != null) {
                        inputStream.Write(input);
                    }
                }

                proc.WaitForExit();
                Thread.MemoryBarrier();
                return new() { StandardOutputContents = output.ToString(), StandardErrorContents = error.ToString(), ExitCode = proc.ExitCode };
            }
        }
    }
}
