using DiffPatch;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Terraria.ModLoader.Setup
{
	public interface ITaskInterface
	{
		void DisplayPatchReviewer(IEnumerable<FilePatcher> results, string baseDir);

		void DisplayInfo(string info);
		void DisplayWarning(string warning);
		void DisplayError(string error);

		void SetMaxProgress(int max);
		void SetStatus(string status);
		void SetProgress(int progress);
		CancellationToken CancellationToken { get; }

		object Invoke(Delegate action);

		void RunTask(SetupOperation task);
		bool RequestConfirmation(string key, string message);
		//bool RequestTerraria();
		string RequestFile(string key, string filter, string message);
	}
}
