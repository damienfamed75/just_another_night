using System;
using System.Linq;
using System.Threading.Tasks;
using Sandbox;

public partial class JustAnotherGame
{
	public static GameStates CurrentState => (Current as JustAnotherGame)?.GameState ?? GameStates.FirstCustomer;

	[Net]
	public GameStates GameState { get; set; } = GameStates.FirstCustomer;

	[Net]
	public RealTimeUntil StateTimer { get; set; } = 0f;

	public enum GameStates
	{
		FirstCustomer,
		WeirdCustomer,
		SecondNormalCustomerAndWeirdGuyStaring,
		BackDoorOpens,
		FreezerUnlocks
	}

	private async Task GameLoopAsync()
	{
		StateTimer = 5.0f;
		await WaitStateTimer();

		// There should only be a single client since this is a singleplayer game.
		var client = All.OfType<Client>().First();
		var player = client.Pawn as JustAnotherPlayer;

		while (player.State == JustAnotherPlayer.PlayerStates.PickupTrash) {
			StateTimer = 1;
			await WaitStateTimer();
		}

		// Initiate first customer
		TrashStaringCreep();

		GameState = GameStates.FirstCustomer;
		StateTimer = Rand.Float( 10.0f, 15.0f );
		await WaitStateTimer();


		while (player.State == JustAnotherPlayer.PlayerStates.TakeOutTrashBags) {
			StateTimer = 1;
			await WaitStateTimer();
		}

		GameState = GameStates.WeirdCustomer;
		StateTimer = Rand.Float( 5.0f, 8.0f );
		await WaitStateTimer();
		// Delete the creep from staring down the path.
		DeleteCreep();

		StateTimer = Rand.Float( 1.0f, 2.0f );
		await WaitStateTimer();

		// initiate weird customer.

		while (player.State == JustAnotherPlayer.PlayerStates.WashDishes) {
			StateTimer = 1;
			await WaitStateTimer();
		}

		GameState = GameStates.SecondNormalCustomerAndWeirdGuyStaring;
		StateTimer = Rand.Float( 10.0f, 15.0f );
		await WaitStateTimer();

		// initiate second normal customer.

		StateTimer = Rand.Float( 20.0f, 25.0f );
		await WaitStateTimer();

		// initiate customer staring through windows.
		StaringCreep();

		while (player.State == JustAnotherPlayer.PlayerStates.MopFloors) {
			StateTimer = 1;
			await WaitStateTimer();
		}

		GameState = GameStates.BackDoorOpens;
		StateTimer = Rand.Float( 0.0f, 1.0f );
		await WaitStateTimer();

		// initiate back door opening.

		while (player.State == JustAnotherPlayer.PlayerStates.FixIceCreamMachine) {
			StateTimer = 1;
			await WaitStateTimer();
		}

		GameState = GameStates.FreezerUnlocks;
		StateTimer = Rand.Float( 1.0f, 2.0f );
		await WaitStateTimer();

		// initiate freezer door opening and guy waiting in freezer.

		// Find the freezer door. The map should only have one.
		var freezerDoor = All.OfType<ControlledDoor>()
			.Where(x => x.Tags.Has("freezer_door"))
			.First();

		// Unlock the freezer door to open it.
		freezerDoor.Locked = false;
		// Open the door using the player so the sound plays in their ears.
		freezerDoor.OnUse( player );
		// Lock the door again so the player cannot use it.
		freezerDoor.Locked = true;
	}

	private async Task WaitStateTimer()
	{
		while (StateTimer > 0) {
			await Task.DelayRealtimeSeconds( 1.0f );
		}

		await Task.DelayRealtimeSeconds( 1.0f );
	}
}