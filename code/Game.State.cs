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

		// var triggerPlacement = All.OfType<TriggerOnce>().Where( x => x.Tags.Has( "freezer_trigger" ) ).First();
		// var trigger = new TriggerOnce();
		// trigger.Owner = this;
		// trigger.Parent = this;
		// trigger.ActivationTags.Add( "player" );
		// trigger.Position = triggerPlacement.Position;
		// trigger.Spawn();
		// trigger.SetupPhysicsFromAABB( PhysicsMotionType.Keyframed,
		// 	triggerPlacement.CollisionBounds.Mins, triggerPlacement.CollisionBounds.Maxs );
		// trigger.ActivationTags.Add( "player" );

		while (player.State == JustAnotherPlayer.PlayerStates.PickupTrash) {
			StateTimer = 1;
			await WaitStateTimer();
		}

		// Initiate first customer
		TrashStaringCreep();

		GameState = GameStates.FirstCustomer;
		StateTimer = Rand.Float( 5.0f, 10.0f );
		await WaitStateTimer();

		await FirstNormalCustomer();
		Sound.FromEntity( "customer-beep", player );

		while (player.State == JustAnotherPlayer.PlayerStates.TakeOutTrashBags) {
			StateTimer = 1;
			await WaitStateTimer();
		}

		GameState = GameStates.WeirdCustomer;
		StateTimer = Rand.Float( 5.0f, 8.0f );
		await WaitStateTimer();
		// Delete the creep from staring down the path.
		await CreepCustomer();
		Sound.FromEntity( "customer-beep", player );

		StateTimer = Rand.Float( 1.0f, 2.0f );
		await WaitStateTimer();

		// initiate weird customer.

		while (player.State == JustAnotherPlayer.PlayerStates.WashDishes) {
			StateTimer = 1;
			await WaitStateTimer();
		}

		GameState = GameStates.SecondNormalCustomerAndWeirdGuyStaring;
		StateTimer = Rand.Float( 1.0f, 5.0f );
		await WaitStateTimer();

		await SecondNormalCustomer();
		Sound.FromEntity( "customer-beep", player );
		// initiate second normal customer.

		StateTimer = Rand.Float( 1.0f, 5.0f );
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
		var backDoor = All.OfType<ControlledDoor>()
			.Where( x => x.Tags.Has( "back_door" ) )
			.First();

		backDoor.OnUse( player );

		while (player.State == JustAnotherPlayer.PlayerStates.FixIceCreamMachine) {
			StateTimer = 1;
			await WaitStateTimer();
		}

		GameState = GameStates.FreezerUnlocks;
		StateTimer = Rand.Float( 1.0f, 2.0f );
		await WaitStateTimer();

		// initiate freezer door opening and guy waiting in freezer.
		FreezerCreep();

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

		while (player.State == JustAnotherPlayer.PlayerStates.OrganizeFreezer) {
			StateTimer = 1;
			await WaitStateTimer();
		}
	}

	private async Task WaitStateTimer()
	{
		while (StateTimer > 0) {
			await Task.DelayRealtimeSeconds( 1.0f );
		}

		await Task.DelayRealtimeSeconds( 1.0f );
	}
}