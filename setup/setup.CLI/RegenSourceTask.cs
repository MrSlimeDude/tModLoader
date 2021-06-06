using Terraria.ModLoader.Properties;

namespace Terraria.ModLoader.Setup
{
	public class RegenSourceTask : CompositeTask
	{
		public RegenSourceTask(ITaskInterface taskInterface, params SetupOperation[] tasks) : base(taskInterface, tasks) { }

		public override bool StartupWarning() {
			if (Settings.Default.PatchMode == 2) {
				if (taskInterface.RequestConfirmation("reset-mode", "Patch mode will be reset from fuzzy to offset. [y/n]") != true)
					return false;
			}

			return taskInterface.RequestConfirmation("lost-changes", "Any changes in /src will be lost. [y/n]");
		}

		public override void Run() {
			if (Settings.Default.PatchMode == 2) {
				Settings.Default.PatchMode = 1;
				Settings.Default.Save();
			}

			base.Run();
		}
	}
}