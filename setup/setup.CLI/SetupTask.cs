namespace Terraria.ModLoader.Setup
{
	public class SetupTask : CompositeTask
	{
		public SetupTask(ITaskInterface taskInterface, params SetupOperation[] tasks) : base(taskInterface, tasks) {}

		public override bool StartupWarning() {
			return taskInterface.RequestConfirmation("lost-changes", "Any changes in /src will be lost. [y/n]");
		}
	}
}
