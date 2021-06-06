using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terraria.ModLoader.Setup
{
	public static class TaskFactory
	{
		public static SetupOperation GetOperation(string name, ITaskInterface @interface) {
			return name switch {
				"decompile" => new DecompileTask(@interface, "src/decompiled"),
				"diffterraria" => new DiffTask(@interface, "src/decompiled", "src/Terraria", "patches/Terraria", new ProgramSetting<DateTime>("TerrariaDiffCutoff")),
				"patchterraria" => new PatchTask(@interface, "src/decompiled", "src/Terraria", "patches/Terraria", new ProgramSetting<DateTime>("TerrariaDiffCutoff")),
				"diffmodloader" => new DiffTask(@interface, "src/Terraria", "src/tModLoader", "patches/tModLoader", new ProgramSetting<DateTime>("tModLoaderDiffCutoff")),
				"patchmodloader" => new PatchTask(@interface, "src/Terraria", "src/tModLoader", "patches/tModLoader", new ProgramSetting<DateTime>("tModLoaderDiffCutoff")),
				"regensource" => new RegenSourceTask(@interface, GetOperation("patchterraria", @interface), GetOperation("patchmodloader", @interface)),
				"setup" => new SetupTask(@interface, GetOperation("decompile", @interface), GetOperation("regensource", @interface)),
				"decompileserver" => new DecompileTask(@interface, "src/decompiled_server", true),
				"formatcode" => new FormatTask(@interface),
				"hookgen" => new HookGenTask(@interface),
				"simplifier" => new SimplifierTask(@interface),
				_ => throw new ArgumentException($"Operation \"{name}\" is not a valid operation name"),
			};
		}
	}
}
