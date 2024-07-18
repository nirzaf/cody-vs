﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Cody.VisualStudio.Connector
{
    public class AgentProcess : IDisposable
    {
        private Process process = new Process();
        private string agentDirectory;
        private bool debugMode;
        private Action<int> onExit;

        private AgentProcess(string agentDirectory, bool debugMode, Action<int> onExit)
        {
            this.agentDirectory = agentDirectory;
            this.debugMode = debugMode;
            this.onExit = onExit;
        }

        public Stream SendingStream => process.StandardInput.BaseStream;

        public Stream ReceivingStream => process.StandardOutput.BaseStream;

        public static AgentProcess Start(string agentDirectory, bool debugMode, Action<int> onExit)
        {
            if (!Directory.Exists(agentDirectory))
                throw new ArgumentException("Directory does not exist");

            var agentProcess = new AgentProcess(agentDirectory, debugMode, onExit);
            agentProcess.StartInternal();

            return agentProcess;
        }


        private void StartInternal()
        {
            var path = Path.Combine(agentDirectory, GetAgentFileName());

            if (!File.Exists(path))
                throw new FileNotFoundException("Agent file not found", path);

            process.StartInfo.FileName = path;
            process.StartInfo.Arguments = GetAgentArguments(debugMode);
            process.StartInfo.WorkingDirectory = agentDirectory;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.CreateNoWindow = true;
            process.EnableRaisingEvents = true;
            process.Exited += OnProcessExited;

            process.Start();
        }

        private void OnProcessExited(object sender, EventArgs e)
        {
            if (onExit != null) onExit(process.ExitCode);
        }

        private string GetAgentFileName()
        {
            if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                return "node-win-arm64.exe";

            return "node-win-x64.exe";
        }

        private string GetAgentArguments(bool debugMode)
        {
            var argList = new List<string>();

            if (debugMode)
            {
                argList.Add("--inspect");
                argList.Add("--enable-source-maps");
            }

            argList.Add("index.js api jsonrpc-stdio");

            var arguments = string.Join(" ", argList);
            return arguments;
        }

        public void Dispose()
        {
            if (!process.HasExited) process.Kill();

            process.Dispose();
        }
    }
}