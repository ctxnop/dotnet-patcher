#region References
using System.ComponentModel;
using Mono.Cecil;
using Mono.Cecil.Cil;
using DP.Compiling;
#endregion

// This patches files is provided as an example of what it is possible.

namespace DP.Patches
{
	/// <summary>
	/// Patch the game "Kingdom Two Crowns".
	/// </summary>
	[DisplayName("k2c"), Description("Kingdom Two Crowns")]
	public class K2c
		: IPatch
	{
		#region Methods
		[DisplayName("money"), Description("Always have at least one coin and one gem.")]
		public void CoinAndGem(AssemblyDefinition asm)
		{
			// No stamina loss on running, always have at least one coin and one gem (everything is free basically)
			asm.Patch(
				(td) => string.CompareOrdinal(td.FullName, "Player") == 0,
				(md) => string.CompareOrdinal(md.Name, "Update") == 0,
				(ilp) => {

					Compiler.PrefixMethod(ilp.Body.Method, @"
						if (_wallet.coins == 0) _wallet.coins = 1;
						if (_wallet.gems == 0) _wallet.gems = 1;
					", "public Wallet _wallet;");
				}
			);
		}

		[DisplayName("beggars"), Description("Increase beggers max number and spawn rate.")]
		public void BeggarSpawn(AssemblyDefinition asm)
		{
			// Beggar max spawn and spawn rate
			asm.Patch(
				(td) => string.CompareOrdinal(td.FullName, "BeggarCamp") == 0,
				(md) => md.IsConstructor,
				(ilp) => {
					ilp.Replace(ilp.Body.Instructions[1], ilp.Create(OpCodes.Ldc_I4_5));
					ilp.Replace(ilp.Body.Instructions[4], ilp.Create(OpCodes.Ldc_R4, 10f));
				}
			);

			// Beggar max spawn and spawn rate
			asm.Patch(
				(td) => string.CompareOrdinal(td.FullName, "BeggarCamp") == 0,
				(md) => string.CompareOrdinal(md.Name, "Start") == 0,
				(ilp) => {
					TypeDefinition BeggarCamp = Patcher.GetTypeDefinition(ilp);
					FieldDefinition maxBeggars = BeggarCamp.F("maxBeggars");
					FieldDefinition spawnInterval = BeggarCamp.F("spawnInterval");

					// Remove the last instruction which is the closing 'Ret'.
					ilp.RemoveAt(ilp.Body.Instructions.Count - 1);

					ilp.Emit(OpCodes.Ldarg_0);
					ilp.Emit(OpCodes.Dup);
					ilp.Emit(OpCodes.Ldc_I4_5);
					ilp.Emit(OpCodes.Stfld, maxBeggars);
					ilp.Emit(OpCodes.Ldc_R4, 10f);
					ilp.Emit(OpCodes.Stfld, spawnInterval);
					ilp.Emit(OpCodes.Ret);
				}
			);
		}

		[DisplayName("units"), Description("Units can't be demoted and don't drop items.")]
		public void ProtectUnits(AssemblyDefinition asm)
		{
			// Units can't be demoted
			asm.Patch(
				(td) => string.CompareOrdinal(td.FullName, "Character") == 0,
				(md) => string.CompareOrdinal(md.Name, "Demote") == 0,
				(ilp) => {
					ilp.Clear();
					ilp.Append(ilp.Create(OpCodes.Ldarg_0));
					ilp.Append(ilp.Create(OpCodes.Ret));
				}
			);

			// Units don't drops items
			asm.Patch(
				(td) => string.CompareOrdinal(td.FullName, "Character") == 0,
				(md) => string.CompareOrdinal(md.Name, "DropItem") == 0,
				(ilp) => {
					ilp.Clear();
					ilp.Append(ilp.Create(OpCodes.Ldnull));
					ilp.Append(ilp.Create(OpCodes.Ret));
				}
			);
		}

		[DisplayName("wall"), Description("Wall can't be destroyed.")]
		public void IndestructibleWalls(AssemblyDefinition asm)
		{
			asm.Patch(
				(td) => string.CompareOrdinal(td.FullName, "Wall") == 0,
				(md) => string.CompareOrdinal(md.Name, "Start") == 0,
				(ilp) => {

					TypeDefinition Wall = Patcher.GetTypeDefinition(ilp);
					FieldDefinition Wall_damageable = Wall.F("_damageable");
					MethodDefinition Wall_set_InvunerableWalls = Wall.M("set_InvunerableWalls");

					TypeDefinition Damageable = Patcher.GetAssemblyDefinition(ilp).T("Damageable");
					MethodDefinition Damageable_set_invulnerable = Damageable.M("set_invulnerable");

					Instruction start = ilp.Body.Instructions[0];
					ilp.InsertBefore(start, ilp.Create(OpCodes.Ldarg_0));
					ilp.InsertBefore(start, ilp.Create(OpCodes.Ldfld, Wall_damageable));
					ilp.InsertBefore(start, ilp.Create(OpCodes.Ldc_I4_1));
					ilp.InsertBefore(start, ilp.Create(OpCodes.Callvirt, Damageable_set_invulnerable));
				}
			);
		}

		[DisplayName("hire"), Description("Always at least one citizen to hire.")]
		public void CitizensHireable(AssemblyDefinition asm)
		{
			asm.Patch((td) => string.CompareOrdinal(td.FullName, "CitizenHousePayable") == 0,
				(md) => string.CompareOrdinal(md.Name, "Update") == 0,
				(ilp) => {
					// Remove the last instruction which is the closing 'Ret'.
					ilp.RemoveAt(ilp.Body.Instructions.Count - 1);

					TypeDefinition CitizenHousePayable = Patcher.GetTypeDefinition(ilp);
					PropertyDefinition _numberOfAvaliableCitizens = CitizenHousePayable.P("_numberOfAvaliableCitizens");
					MethodDefinition SpawnPeasant = CitizenHousePayable.M("SpawnPeasant");

					Instruction ret = ilp.Create(OpCodes.Ret);

					ilp.Emit(OpCodes.Ldarg_0);
					ilp.Emit(OpCodes.Callvirt, _numberOfAvaliableCitizens.GetMethod);
					ilp.Emit(OpCodes.Brtrue, ret);
					ilp.Emit(OpCodes.Ldarg_0);
					ilp.Emit(OpCodes.Callvirt, SpawnPeasant);
					ilp.Append(ret);
				}
			);
		}

		[DisplayName("grab"), Description("Squids can't grab citizens.")]
		public void SquidGrab(AssemblyDefinition asm)
		{
			// Squids can't grab citizens
			asm.Patch((td) => string.CompareOrdinal(td.FullName, "Squid") == 0,
				(md) => string.CompareOrdinal(md.Name, "GetClosestTarget") == 0,
				(ilp) => {
					ilp.Clear();
					ilp.Emit(OpCodes.Ldnull);
					ilp.Emit(OpCodes.Ret);
				}
			);

			// Squids don't even try to grab citizens
			asm.Patch((td) => string.CompareOrdinal(td.FullName, "Squid") == 0,
				(md) => string.CompareOrdinal(md.Name, "ShouldGrabCitizen") == 0,
				(ilp) => {
					ilp.Clear();
					ilp.Emit(OpCodes.Ldc_I4_0);
					ilp.Emit(OpCodes.Ret);
				}
			);
		}

		[DisplayName("weak"), Description("Squids are easy to kill.")]
		public void SquidWeak(AssemblyDefinition asm)
		{
			// Squids have almost no hitpoints
			asm.Patch((td) => string.CompareOrdinal(td.FullName, "Squid") == 0,
				(md) => string.CompareOrdinal(md.Name, "Awake") == 0,
				(ilp) => {
					// Remove the last instruction which is the closing 'Ret'.
					ilp.RemoveAt(ilp.Body.Instructions.Count - 1);

					TypeDefinition Squid = Patcher.GetTypeDefinition(ilp);
					FieldDefinition _damageable = Squid.F("_damageable");
					TypeDefinition Damageable = Patcher.GetAssemblyDefinition(ilp).T("Damageable");
					PropertyDefinition hitPoints = Damageable.P("hitPoints");

					ilp.Emit(OpCodes.Ldarg_0);
					ilp.Emit(OpCodes.Ldfld, _damageable);
					ilp.Emit(OpCodes.Ldc_I4_1);
					ilp.Emit(OpCodes.Callvirt, hitPoints.SetMethod);

					ilp.Emit(OpCodes.Ret);
				}
			);
		}

		[DisplayName("arrow"), Description("Increase arrow damage.")]
		public void ArrowDamage(AssemblyDefinition asm)
		{
			asm.Patch(
				(td) => string.CompareOrdinal(td.FullName, "Arrow") == 0,
				(md) => md.IsConstructor,
				(ilp) => {
					//1 = hitdamage
					ilp.Replace(ilp.Body.Instructions[1], ilp.Create(OpCodes.Ldc_I4_8));
					//13 = damagePerTicks
					ilp.Replace(ilp.Body.Instructions[13], ilp.Create(OpCodes.Ldc_I4_8));

				}
			);
		}
		#endregion
	}
}
