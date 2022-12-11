#region References
using System.ComponentModel;
using Mono.Cecil;
using Mono.Cecil.Cil;
using DP.Compiling;
#endregion

namespace DP.Patches
{
	/// <summary>
	/// Patch the game "Raft".
	/// </summary>
	[DisplayName("raft"), Description("Raft")]
	public class Raft
		: IPatch
	{
		#region Methods
		[DisplayName("durab"), Description("Item have infinite durability.")]
		public void Durability(AssemblyDefinition asm)
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
		}

		[DisplayName("stats"), Description("No stat depletion.")]
		public void Stats(AssemblyDefinition asm)
		{
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
		}

		[DisplayName("weapon"), Description("Melee weapon hist from far away and really hard")]
		public void Weapon(AssemblyDefinition asm)
		{
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
		}

		[DisplayName("items"), Description("Infinite items")]
		public void InifniteItems(AssemblyDefinition asm)
		{
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
		}
		#endregion
	}
}
