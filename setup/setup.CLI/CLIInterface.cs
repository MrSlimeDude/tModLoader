using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using DiffPatch;

namespace Terraria.ModLoader.Setup
{
	internal class CLIInterface : ITaskInterface {
		private int max;
		private int lineNum;

		private const string progressFormat = "Progress: [{0}]";

		private Dictionary<string, string> cmdArgs;

		private CancellationTokenSource cancelSource;
		public CancellationToken CancellationToken => cancelSource.Token;
		public CLIInterface(IEnumerable<string> args) {
			max = 0;
			lineNum = 0;
			cmdArgs = new Dictionary<string, string>();

			Console.CancelKeyPress += (sender, e) => { //Cancel the tokens before exiting
				cancelSource.Cancel();
				e.Cancel = true;
			};

			foreach (string arg in args) {
				if(arg[0..2] != "--")
					throw new ArgumentException($"Argument {arg} does not begin with \"--\"");

				string[] argSplit = arg[2..].Split('=');
				if (argSplit.Length != 2)
					throw new ArgumentException("Argument does not contain a value in the form --name=value");
				else
					cmdArgs.Add(argSplit[0], argSplit[1]);
			}

			//Console.WriteLine(Enumerable.Range(0, Console.WindowWidth).Select(_ => '*').ToArray());
		}

		private void ClearLineAndMove(int line) {
			//int old = Console.CursorTop;
			Console.SetCursorPosition(0, line);
			Console.Write(new string(' ', Console.WindowWidth));
			//Console.CursorTop = old;
		}

		public object Invoke(Delegate action) => action.DynamicInvoke();

		public void SetMaxProgress(int max) => this.max = max;

		public void SetProgress(int progress) {
			if(Console.IsOutputRedirected) {
				Console.WriteLine(string.Format("Value: {0:P2}.", (float)progress / max));
			}
			else {
				Console.SetCursorPosition(0, lineNum); //Move to progress bar location
				int barLen = Console.WindowWidth - progressFormat.Length + 3; //Add 3 to account for the {0}
				string output = string.Format(progressFormat, new string(Enumerable.Range(0, barLen).Select(i => {
					return (float)i / barLen < ((float)progress / max) ? '*' : '-';
				}).ToArray()));
				Console.Write(output);
			}
		}

		public void DisplayPatchReviewer(IEnumerable<FilePatcher> results, string baseDir) => throw new NotSupportedException("The patch reviewer is not available on the CLI");
		public void DisplayInfo(string info) => LogMessage("[INFO] -> " + info);
		public void DisplayWarning(string warning) => LogMessage("[WARNING] -> " + warning);
		public void DisplayError(string error) => LogMessage("[ERROR] -> " + error);

		private void LogMessage(string message) {
			if (Console.IsOutputRedirected) {
				Console.WriteLine(message);
			}
			else {
				if (Environment.OSVersion.Platform == PlatformID.Win32NT)
					Console.MoveBufferArea(0, lineNum, Console.WindowWidth, 1 + lastStatusLength, 0, lineNum + 1);

				Console.SetCursorPosition(0, lineNum);
				Console.Write(message);
				lineNum++;
			}
		}

		private int lastStatusLength = 0;

		public void SetStatus(string status) {
			if(Console.IsOutputRedirected) {
				Console.WriteLine(status);
			}
			else {
				int statusPos = lineNum + 1; //Add one for the progress bar line
				Console.SetCursorPosition(0, statusPos); //Move to status location
				//Clear the old status
				for (int i = 0; i < lastStatusLength; i++) {
					ClearLineAndMove(statusPos + i);
				}
				Console.SetCursorPosition(0, statusPos); //Return to the status location

				string[] lines = status.Split(Environment.NewLine);
				lastStatusLength = lines.Length;
				foreach (string line in lines) {
					Console.WriteLine(line);
				}
			}
		}

		public void RunTask(SetupOperation task) {
			cancelSource = new CancellationTokenSource();

			new Thread(() => RunTaskThread(task)).Start();
		}

		private void RunTaskThread(SetupOperation task) {
			var errorLogFile = Path.Combine(Program.logsDir, "error.log");
			try {
				SetupOperation.DeleteFile(errorLogFile);

				if (!task.ConfigurationDialog())
					return;

				if (!task.StartupWarning())
					return;

				try {
					task.Run();

					if (cancelSource.IsCancellationRequested)
						throw new OperationCanceledException();
				}
				catch (OperationCanceledException e) {
					Invoke(new Action(() => {
						//labelStatus.Text = "Cancelled";
						//if (e.Message != new OperationCanceledException().Message)
						//	labelStatus.Text += ": " + e.Message;
						string status = "Cancelled";
						if (e.Message != new OperationCanceledException().Message)
							status = ": " + e.Message;
						SetStatus(status);
					}));
					return;
				}

				if (task.Failed() || task.Warnings())
					task.FinishedDialog();

				Invoke(new Action(() => {
					//labelStatus.Text = task.Failed() ? "Failed" : "Done";
					SetStatus(task.Failed() ? "Failed" : "Done");
				}));
			}
			catch (Exception e) {
				var status = "";
				Invoke(new Action(() => {
					//status = labelStatus.Text;
					//labelStatus.Text = "Error: " + e.Message.Trim();
					SetStatus("Error: " + e.Message.Trim());
				}));

				SetupOperation.CreateDirectory(Program.logsDir);
				File.WriteAllText(errorLogFile, status + "\r\n" + e);
			}
		}

		string GetInput(string key, string str) {
			if (cmdArgs.TryGetValue(key, out string val))
				return val;

			int promptPos = lineNum + 1 + lastStatusLength; //Add one for the progress bar line 
			Console.SetCursorPosition(0, promptPos);
			Console.Write(str);
			return Console.ReadLine();
		}

		public bool RequestConfirmation(string key, string message) => GetInput(key, $"[{key}] {message}").ToLower() is "y" or "yes";
		public string RequestFile(string key, string fileType, string message) => GetInput(key, $"[{key}] {message}");
	}
}
