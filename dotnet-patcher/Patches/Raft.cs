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
	public class Raft
		: IPatch
	{
		#region Properties
		/// <summary>
		/// Get the patch Id.
		/// </summary>
		/// <value>An Id to reference the patch.</value>
		public string Id
		{
			get { return "raft"; }
		}
		#endregion

		#region Methods
		/// <summary>
		/// Apply this patch on this assembly definition.
		/// </summary>
		/// <param name="asm">The assembly definition.</param>
		public bool Apply(AssemblyDefinition asm)
		{
			// Infinite durability
			asm.Patch(
				(td) => { return string.CompareOrdinal(td.FullName, "PlayerInventory") == 0; },
				(md) => {
					return
						// public bool RemoveDurabillityFromHotSlot(int durabilityStacksToRemove = 1)
						string.CompareOrdinal(md.Name, "RemoveDurabillityFromHotSlot") == 0 ||
						// public bool RemoveDurabillityFromEquipment(EquipSlotType equipmentType, int durabilityStacksToRemove = 1)
						string.CompareOrdinal(md.Name, "RemoveDurabillityFromEquipment") == 0;
				},
				(ilp) => {
					Compiler.ReplaceMethod(
						ilp,
						"return false;"
					);
				}
			);

			// Infinite durability (bis)
			asm.Patch(
				(td) => { return string.CompareOrdinal(td.FullName, "Slot") == 0; },
				(md) => { return string.CompareOrdinal(md.Name, "IncrementUses") == 0; },
				(ilp) => {
					ilp.Clear();
					ilp.Emit(OpCodes.Ret);
				}
			);

			// Reduce stat depletion
			asm.Patch(
				(td) => { return string.CompareOrdinal(td.FullName, "Stat_Consumable") == 0; },
				(md) => { return string.CompareOrdinal(md.FullName, "get_LostPerSecond") == 0; },
				(ilp) => {
					ilp.RemoveAt(ilp.Body.Instructions.Count - 1);
					ilp.Emit(OpCodes.Ldc_R4, 0.0f);
					ilp.Emit(OpCodes.Mul);
					ilp.Emit(OpCodes.Ret);
				}
			);

			// No shark attack
			asm.Patch(
				(td) => { return string.CompareOrdinal(td.FullName, "Shark") == 0; },
				(md) => { return string.CompareOrdinal(md.Name, "ChangeState") == 0; },
				(ilp) => {

					// Insert first block
					Instruction next = ilp.Body.Instructions[0];
					ilp.InsertBefore(next, ilp.Create(OpCodes.Ldarg_1));
					ilp.InsertBefore(next, ilp.Create(OpCodes.Ldc_I4_2));
					ilp.InsertBefore(next, ilp.Create(OpCodes.Bne_Un_S, next));
					ilp.InsertBefore(next, ilp.Create(OpCodes.Ret));

					// Insert second block
					next = ilp.Body.Instructions[0];
					ilp.InsertBefore(next, ilp.Create(OpCodes.Ldarg_1));
					ilp.InsertBefore(next, ilp.Create(OpCodes.Ldc_I4_4));
					ilp.InsertBefore(next, ilp.Create(OpCodes.Bne_Un_S, next));
					ilp.InsertBefore(next, ilp.Create(OpCodes.Ret));

					// Insert thrid block
					next = ilp.Body.Instructions[0];
					ilp.InsertBefore(next, ilp.Create(OpCodes.Ldarg_0));
					ilp.InsertBefore(next, ilp.Create(OpCodes.Ldc_I4_1));
					ilp.InsertBefore(next, ilp.Create(OpCodes.Stfld, ilp.FieldRef("hasBitten")));
					ilp.InsertBefore(next, ilp.Create(OpCodes.Ldarg_0));
					ilp.InsertBefore(next, ilp.Create(OpCodes.Ldc_I4_0));
					ilp.InsertBefore(next, ilp.Create(OpCodes.Stfld, ilp.FieldRef("canAttack")));
				}
			);

			asm.Patch(
				(td) => { return string.CompareOrdinal(td.FullName, "Shark") == 0; },
				(md) => { return string.CompareOrdinal(md.Name, "Attack") == 0; },
				(ilp) => {
					ilp.Clear();
					ilp.Emit(OpCodes.Ldarg_0);
					ilp.Emit(OpCodes.Ldc_I4_1);
					ilp.Emit(OpCodes.Stfld, ilp.FieldRef("hasBitten"));
					ilp.Emit(OpCodes.Ldarg_0);
					ilp.Emit(OpCodes.Ldc_I4_0);
					ilp.Emit(OpCodes.Stfld, ilp.FieldRef("canAttack"));
					ilp.Emit(OpCodes.Ret);
				}
			);

			asm.Patch(
				(td) => { return string.CompareOrdinal(td.FullName, "Shark") == 0; },
				(md) => { return string.CompareOrdinal(md.Name, "Update") == 0; },
				(ilp) => {
					Instruction next = ilp.Body.Instructions[0];
					ilp.InsertBefore(next, ilp.Create(OpCodes.Ldarg_0));
					ilp.InsertBefore(next, ilp.Create(OpCodes.Ldc_I4_1));
					ilp.InsertBefore(next, ilp.Create(OpCodes.Stfld, ilp.FieldRef("hasBitten")));
					ilp.InsertBefore(next, ilp.Create(OpCodes.Ldarg_0));
					ilp.InsertBefore(next, ilp.Create(OpCodes.Ldc_I4_0));
					ilp.InsertBefore(next, ilp.Create(OpCodes.Stfld, ilp.FieldRef("canAttack")));
					ilp.InsertBefore(next, ilp.Create(OpCodes.Ldarg_0));
					ilp.InsertBefore(next, ilp.Create(OpCodes.Ldc_I4_0));
					ilp.InsertBefore(next, ilp.Create(OpCodes.Stfld, ilp.FieldRef("bitingRaft")));
					ilp.InsertBefore(next, ilp.Create(OpCodes.Ldarg_0));
					ilp.InsertBefore(next, ilp.Create(OpCodes.Ldc_R4, 0.0f));
					ilp.InsertBefore(next, ilp.Create(OpCodes.Stfld, ilp.FieldRef("biteRaftTimer")));
				}
			);

			asm.Patch(
				(td) => { return string.CompareOrdinal(td.FullName, "Shark") == 0; },
				(md) => { return string.CompareOrdinal(md.Name, "OnDamageTaken") == 0; },
				(ilp) => {
					Instruction next = ilp.Body.Instructions[0];
					ilp.InsertBefore(next, ilp.Create(OpCodes.Ldc_R4, 20.0f));
					ilp.InsertBefore(next, ilp.Create(OpCodes.Ldarg_1));
					ilp.InsertBefore(next, ilp.Create(OpCodes.Mul));
					ilp.InsertBefore(next, ilp.Create(OpCodes.Starg, 1));
				}
			);

			asm.Patch(
				(td) => { return string.CompareOrdinal(td.FullName, "MeleeWeapon") == 0; },
				(md) => { return string.CompareOrdinal(md.Name, "Start") == 0; },
				(ilp) => {
					ilp.RemoveAt(ilp.Body.Instructions.Count - 1);
					ilp.Emit(OpCodes.Ldarg_0);
					ilp.Emit(OpCodes.Ldc_I4, 200);
					ilp.Emit(OpCodes.Stfld, ilp.FieldRef("damage"));
					ilp.Emit(OpCodes.Ldarg_0);
					ilp.Emit(OpCodes.Ldc_R4, 50f);
					ilp.Emit(OpCodes.Stfld, ilp.FieldRef("attackRange"));
					ilp.Emit(OpCodes.Ret);
				}
			);

			asm.Patch(
				(td) => { return string.CompareOrdinal(td.FullName, "ItemInstance") == 0; },
				(md) => { return string.CompareOrdinal(md.Name, "get_BaseItemMaxUses") == 0; },
				(ilp) => {
					Instruction ip = ilp.Body.Instructions[ilp.Body.Instructions.Count - 3];
					ilp.InsertBefore(ip, ilp.Create(OpCodes.Ldc_I4, 200));
					ilp.InsertBefore(ip, ilp.Create(OpCodes.Mul));
				}
			);

			asm.Patch(
				(td) => { return string.CompareOrdinal(td.FullName, "ItemInstance") == 0; },
				(md) => { return string.CompareOrdinal(md.Name, "set_Uses") == 0; },
				(ilp) => {
					ilp.Clear();
					ilp.Emit(OpCodes.Ldarg_0);
					ilp.Emit(OpCodes.Dup);
					ilp.Emit(OpCodes.Call, ilp.MethodRef("get_BaseItemMaxUses"));
					ilp.Emit(OpCodes.Stfld, ilp.FieldRef("uses"));
					ilp.Emit(OpCodes.Ret);
				}
			);

			asm.Patch(
				(td) => { return string.CompareOrdinal(td.FullName, "Fuel") == 0; },
				(md) => { return string.CompareOrdinal(md.Name, "UpdateFuel") == 0; },
				(ilp) => {
					ilp.RemoveAt(ilp.Body.Instructions.Count - 1);
					ilp.Emit(OpCodes.Ldarg_0);
					ilp.Emit(OpCodes.Dup);
					ilp.Emit(OpCodes.Ldfld, ilp.FieldRef("maxFuel"));
					ilp.Emit(OpCodes.Stfld, ilp.FieldRef("fuelCount"));
					ilp.Emit(OpCodes.Ret);
				}
			);

			asm.Patch(
				(td) => { return string.CompareOrdinal(td.FullName, "Tank") == 0; },
				(md) => { return string.CompareOrdinal(md.Name, "SetTankAmount") == 0; },
				(ilp) => {
					Instruction start = ilp.Body.Instructions[0];
					ilp.InsertBefore(start, ilp.Create(OpCodes.Ldarg_0));
					ilp.InsertBefore(start, ilp.Create(OpCodes.Ldfld, ilp.FieldRef("maxCapacity")));
					ilp.InsertBefore(start, ilp.Create(OpCodes.Starg, 1));
				}
			);

			return true;
		}
		#endregion
	}
}
