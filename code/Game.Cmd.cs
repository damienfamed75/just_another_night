using System.Linq;
using Sandbox;

public partial class JustAnotherGame
{
	/// <summary>
	/// DeleteEntity is a command to the server to delete an entity with the
	/// given name
	/// </summary>
	[ConCmd.Server("delete_ent")]
	public static void DeleteEntity(string name)
	{
		var ent = All.OfType<Entity>().Where( x => x.Name == name ).FirstOrDefault();

		if (ent != null && ent.IsValid)
			ent.Delete();
	}

	/// <summary>
	/// Increment the player's task.
	/// </summary>
	[ConCmd.Server("increment_task")]
	public static void IncrementTask()
	{
		var game = Current as JustAnotherGame;
		if (game.TimeSinceStateChanged < 5)
			return;

		var player = ConsoleSystem.Caller.Pawn as JustAnotherPlayer;
		player.State++;
		Log.Info( $"playerstate[{player.State}]" );
	}

	/// <summary>
	/// Equip an entity. This is used for trash bags.
	/// </summary>
	[ConCmd.Server("equip_ent")]
	public static void EquipEntity(string name)
	{
		var player = ConsoleSystem.Caller.Pawn as JustAnotherPlayer;
		var ent = All.OfType<Entity>().Where( x => x.Name == name ).First();

		player.ActiveChild = ent;
		ent.Owner = player;
	}

	/// <summary>
	/// Unequip an equipped entity. This is used for trash bags.
	/// </summary>
	[ConCmd.Server("unequip_ent")]
	public static void UnEquipEntity()
	{
		var player = ConsoleSystem.Caller.Pawn as JustAnotherPlayer;
		if (player.ActiveChild == null)
			return;

		player.ActiveChild.Delete();
		player.ActiveChild = null;
	}

	/// <summary>
	/// Spawn in the staring creep outside the building starting into the
	/// second window.
	/// </summary>
	[ConCmd.Admin("creep_stare")]
	public static void SpawnStaringCreep()
	{
		(Current as JustAnotherGame).StaringCreep();
	}

	/// <summary>
	/// Open the freezer door of the restaurant.
	/// </summary>
	[ConCmd.Admin("freezer_open")]
	public static void OpenFreezerDoor()
	{
		var caller = ConsoleSystem.Caller.Pawn;
		var freezerDoor = All.OfType<ControlledDoor>()
			.Where( x => x.Tags.Has( "freezer_door" ) )
			.First();

		freezerDoor.Locked = false;
		freezerDoor.OnUse( caller );
		freezerDoor.Locked = true;
	}

	/// <summary>
	/// Open the backdoor of the restaurant.
	/// </summary>
	[ConCmd.Admin("back_open")]
	public static void OpenBackDoor()
	{
		var caller = ConsoleSystem.Caller.Pawn;
		var backDoor = All.OfType<ControlledDoor>()
			.Where( x => x.Tags.Has( "back_door" ) )
			.First();

		backDoor.OnUse( caller );
		backDoor.TimeBeforeReset = -1;
	}

	/// <summary>
	/// Spawn the creep to kill the player in the freezer.
	/// </summary>
	[ConCmd.Admin("death")]
	public static void DeathSpawn()
	{
		OpenFreezerDoor();
		(Current as JustAnotherGame).FreezerCreep();
	}

	/// <summary>
	/// Spawn a customer with the given material group.
	/// </summary>
	/// <param name="group">Material Group of the Creep model</param>
	[ConCmd.Admin("spawn_customer")]
	public static void TestingSpawnCustomer(int group)
	{
		(Current as JustAnotherGame).SpawnCustomer(group);
	}
}