#region References
using Mono.Cecil;
using Mono.Cecil.Cil;
using DP.Compiling;
#endregion

// This patches files is provided as an example of what it is possible.

namespace DP.Patches
{
	/// <summary>
	/// Patch the game "Raft".
	/// </summary>
	public class K2c
		: IPatch
	{
		#region Properties
		/// <summary>
		/// Get the patch Id.
		/// </summary>
		/// <value>An Id to reference the patch.</value>
		public string Id
		{
			get { return "k2c"; }
		}
		#endregion

		#region Methods
		/// <summary>
		/// Apply this patch on this assembly definition.
		/// </summary>
		/// <param name="asm">The assembly definition.</param>
		public bool Apply(AssemblyDefinition asm)
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

					/* OpCode Method
					// Finds required references
					TypeDefinition Player = Patcher.GetTypeDefinition(ilp);
					FieldDefinition Player_wallet = Player.F("_wallet");
					FieldDefinition Player_steed = Player.F("_steed");

					TypeDefinition Wallet = Player_wallet.FieldType.Resolve();
					MethodDefinition Wallet_get_coins = Wallet.M("get_coins");
					MethodDefinition Wallet_set_coins = Wallet.M("set_coins");
					MethodDefinition Wallet_get_gems = Wallet.M("get_gems");
					MethodDefinition Wallet_set_gems = Wallet.M("set_gems");

					TypeDefinition Steed = Player_steed.FieldType.Resolve();
					FieldDefinition Steed_runStaminaRate = Steed.F("runStaminaRate");

					// Prefix method
					Instruction start = ilp.Body.Instructions[0];

					// if (_wallet.coins == 0) {
					ilp.InsertBefore(start, ilp.Create(OpCodes.Ldarg_0));
					ilp.InsertBefore(start, ilp.Create(OpCodes.Ldfld, Player_wallet));
					ilp.InsertBefore(start, ilp.Create(OpCodes.Callvirt, Wallet_get_coins));
					ilp.InsertBefore(start, ilp.Create(OpCodes.Ldc_I4_0));
					Instruction b1 = start.Previous;
					// _wallet.coin = 1;
					ilp.InsertBefore(start, ilp.Create(OpCodes.Ldarg_0));
					ilp.InsertBefore(start, ilp.Create(OpCodes.Ldfld, Player_wallet));
					ilp.InsertBefore(start, ilp.Create(OpCodes.Ldc_I4_1));
					ilp.InsertBefore(start, ilp.Create(OpCodes.Callvirt, Wallet_set_coins));
					Instruction l1 = start.Previous; // }

					// if (_wallet.gems == 0) {
					ilp.InsertBefore(start, ilp.Create(OpCodes.Ldarg_0));
					ilp.InsertBefore(start, ilp.Create(OpCodes.Ldfld, Player_wallet));
					ilp.InsertBefore(start, ilp.Create(OpCodes.Callvirt, Wallet_get_gems));
					ilp.InsertBefore(start, ilp.Create(OpCodes.Ldc_I4_0));
					Instruction b2 = start.Previous;
					// _wallet.gems = 1;
					ilp.InsertBefore(start, ilp.Create(OpCodes.Ldarg_0));
					ilp.InsertBefore(start, ilp.Create(OpCodes.Ldfld, Player_wallet));
					ilp.InsertBefore(start, ilp.Create(OpCodes.Ldc_I4_1));
					ilp.InsertBefore(start, ilp.Create(OpCodes.Callvirt, Wallet_set_gems));
					Instruction l2 = start.Previous; // }

					// _steed.runStaminaRate = 0;
					ilp.InsertBefore(start, ilp.Create(OpCodes.Ldarg_0));
					ilp.InsertBefore(start, ilp.Create(OpCodes.Ldfld, Player_steed));
					ilp.InsertBefore(start, ilp.Create(OpCodes.Ldc_R4, 0f));
					ilp.InsertBefore(start, ilp.Create(OpCodes.Stfld, Steed_runStaminaRate));
					
					// Now that all the opcodes are known, add the branching
					ilp.InsertAfter(b1, ilp.Create(OpCodes.Bne_Un, l1.Next));
					ilp.InsertAfter(b2, ilp.Create(OpCodes.Bne_Un, l2.Next));
					*/
				}
			);

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

			// Walls can't be destroyed
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

			// Always at least one citizen to hire
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

			// Arrow damages
			asm.Patch(
				(td) => string.CompareOrdinal(td.FullName, "Arrow") == 0,
				(md) => md.IsConstructor,
				(ilp) => {
					//ilp.Clear();
					//1 = hitdamage
					ilp.Replace(ilp.Body.Instructions[1], ilp.Create(OpCodes.Ldc_I4_8));
					//13 ! damagePerTicks
					ilp.Replace(ilp.Body.Instructions[13], ilp.Create(OpCodes.Ldc_I4_8));

				}
			);

			return true;
		}
		#endregion
	}
}