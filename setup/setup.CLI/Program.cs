using System;
using System.IO;
using System.Reflection;
using Terraria.ModLoader.Properties;
using System.Threading;
using System.Diagnostics;
using System.Text;
using System.Linq;

namespace Terraria.ModLoader.Setup
{
    public static class Program
    {
		public static readonly string appDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		public static readonly string logsDir = Path.Combine("setup", "logs");

		public static string SteamDir => Settings.Default.SteamDir; //@"D:\SteamLibrary\steamapps\common\Terraria"; 
		public static string TMLDevSteamDir => Settings.Default.TMLDevSteamDir;
		public static string TerrariaPath => Path.Combine(SteamDir, "Terraria.exe");
		public static string TerrariaServerPath => Path.Combine(SteamDir, "TerrariaServer.exe");

		internal static void Main(string[] args)
        {
			Settings.Default.FormatAfterDecompiling = true;
			if (args.Length == 0 || !RunProgram(args)) {
				DisplayHelp();
			}
			return;

			ITaskInterface cli = new CLIInterface(args.Skip(1));
			cli.SetMaxProgress(100);
			for (int i = 0; i < 101; i++) {
				cli.SetProgress(i);
				if (i % 15 == 0) {
					cli.SetStatus(string.Join(Environment.NewLine, "fizzbuzz".ToCharArray()));
				} else if (i % 3 == 0) {
					cli.SetStatus("fizz");
				} else if (i % 5 == 0) {
					cli.SetStatus("buzz");
				}
				if (i % 2 == 0) {
					cli.DisplayInfo("multiple of 2");
				}
				if(i == 50) {
					bool val = cli.RequestConfirmation("favnum", "Is your favourite number 69? ");
					cli.DisplayInfo($"You said that is {val}");
				}
				Thread.Sleep(200);
			}
            //Console.WriteLine("Hello World!");
        }

		private static void DisplayHelp() {
			Console.WriteLine(@"
Usage: **PROGRAM** task_name args

Tasks:
decompile
diffterraria
patchterraria
diffmodloader
patchmodloader
regensource
setup
decompileserver
formatcode
hookgen
simplifier
");
		}

		private static bool RunProgram(string[] args) {
			CLIInterface cli = new CLIInterface(args.Skip(1));
			SetupOperation task;
			try {
				task = TaskFactory.GetOperation(args[0], cli);
			}
			catch (ArgumentException) {
				return false;
			}
			cli.RunTask(task);
			return true;
		}

		public static int RunCmd(string dir, string cmd, string args,
				Action<string> output = null,
				Action<string> error = null,
				string input = null,
				CancellationToken cancel = default(CancellationToken)) {

			using (var process = new Process()) {
				process.StartInfo = new ProcessStartInfo {
					FileName = cmd,
					Arguments = args,
					WorkingDirectory = dir,
					UseShellExecute = false,
					RedirectStandardInput = input != null,
					CreateNoWindow = true
				};

				if (output != null) {
					process.StartInfo.RedirectStandardOutput = true;
					process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
				}

				if (error != null) {
					process.StartInfo.RedirectStandardError = true;
					process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
				}

				if (!process.Start())
					throw new Exception($"Failed to start process: \"{cmd} {args}\"");

				if (input != null) {
					var w = new StreamWriter(process.StandardInput.BaseStream, new UTF8Encoding(false));
					w.Write(input);
					w.Close();
				}

				while (!process.HasExited) {
					if (cancel.IsCancellationRequested) {
						process.Kill();
						throw new OperationCanceledException(cancel);
					}
					process.WaitForExit(100);

					output?.Invoke(process.StandardOutput.ReadToEnd());
					error?.Invoke(process.StandardError.ReadToEnd());
				}

				return process.ExitCode;
			}
		}

		public static void CreateTMLSteamDirIfNecessary() {
			if (Directory.Exists(TMLDevSteamDir))
				return;

			Settings.Default.TMLDevSteamDir = Path.GetFullPath(Path.Combine(Settings.Default.SteamDir, "..", "tModLoaderDev"));
			Settings.Default.Save();

			try {
				Directory.CreateDirectory(TMLDevSteamDir);
			}
			catch (Exception e) {
				Console.WriteLine($"{e.GetType().Name}: {e.Message}");
			}
		}

		private static readonly string targetsFilePath = Path.Combine("src", "WorkspaceInfo.targets");

		public static void UpdateTargetsFile() {
			SetupOperation.CreateParentDirectory(targetsFilePath);

			string gitsha = "";
			RunCmd("", "git", "rev-parse HEAD", s => gitsha = s.Trim());

			string branch = "";
			RunCmd("", "git", "rev-parse --abbrev-ref HEAD", s => branch = s.Trim());

			string GITHUB_HEAD_REF = Environment.GetEnvironmentVariable("GITHUB_HEAD_REF");
			if (!string.IsNullOrWhiteSpace(GITHUB_HEAD_REF)) {
				Console.WriteLine($"GITHUB_HEAD_REF found: {GITHUB_HEAD_REF}");
				branch = GITHUB_HEAD_REF;
			}
			string HEAD_SHA = Environment.GetEnvironmentVariable("HEAD_SHA");
			if (!string.IsNullOrWhiteSpace(HEAD_SHA)) {
				Console.WriteLine($"HEAD_SHA found: {HEAD_SHA}");
				gitsha = HEAD_SHA;
			}

			string targetsText =
$@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""14.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <!-- This file will always be overwritten, do not edit it manually. -->
  <PropertyGroup>
	<BranchName>{branch}</BranchName>
	<CommitSHA>{gitsha}</CommitSHA>
	<TerrariaSteamPath>{SteamDir}</TerrariaSteamPath>
    <tModLoaderSteamPath>{TMLDevSteamDir}</tModLoaderSteamPath>
  </PropertyGroup>
</Project>";

			if (File.Exists(targetsFilePath) && targetsText == File.ReadAllText(targetsFilePath))
				return;

			File.WriteAllText(targetsFilePath, targetsText);
		}
	}
}
