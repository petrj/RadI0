using LoggerService;
using RTLSDR.Common;
using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;

namespace RTLSDR
{

    /// <summary>
    /// RTL-SDR driver that uses the rtl_tcp process.
    /// </summary>
    public class RTLSDRPCDriver : RTLSDRDriver
    {
        /// <summary>
        /// Initializes a new instance of the RTLSDRPCDriver class.
        /// </summary>
        /// <param name="loggingService">The logging service to use.</param>
        public RTLSDRPCDriver(ILoggingService loggingService)
            : base(loggingService)
        {
        }

        private Process? _process;

        protected override async Task Connect()
        {
            try
            {
                _ = Task.Run(async () =>
                {
                    await Task.Run(() =>
                    {
                        Run("rtl_tcp", $"-f {Frequency} -s {Settings.SDRSampleRate}", _loggingService);
                    });
                });

                _loggingService?.Info("Waiting 5 secs for init driver");
                await Task.Delay(5000);

                await base.Connect();
            } catch (Exception ex)
            {
                _loggingService?.Error(ex);
            }
        }

        /// <summary>
        /// Disconnects from the SDR device and kills the rtl_tcp process.
        /// </summary>
        public override void Disconnect()
        {
            base.Disconnect();

            _process?.Kill(true);
        }

        /// <summary>
        /// Runs a command in a new process.
        /// </summary>
        /// <param name="command">The command to run.</param>
        /// <param name="args">The arguments for the command.</param>
        /// <param name="loggerService">The logging service to use.</param>
        /// <param name="workingDir">The working directory for the process.</param>
        public void Run(string command, string args, ILoggingService? loggerService, string? workingDir = null)
        {
            try
            {
                _process = new System.Diagnostics.Process();

                _process.StartInfo.FileName = workingDir == null ? command : Path.Combine(workingDir, command);
                _process.StartInfo.Arguments = args;
                _process.StartInfo.WorkingDirectory = workingDir ?? "";

                _process.StartInfo.UseShellExecute = false;
                _process.StartInfo.CreateNoWindow = true;

                _process.StartInfo.RedirectStandardOutput = true;
                _process.StartInfo.RedirectStandardError = true;

                _process.OutputDataReceived += (sender, a) =>
                {
                    if (!string.IsNullOrEmpty(a.Data))
                        loggerService?.Info($"{a.Data}");
                };

                _process.ErrorDataReceived += (sender, a) =>
                {
                    if (!string.IsNullOrEmpty(a.Data))
                        loggerService?.Info($"{a.Data}"); // nebo Error
                };

                _process.Start();

                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();

                _process.WaitForExit();
            }
            catch (Exception ex)
            {
                loggerService?.Error(ex);
            }
        }
    }
}