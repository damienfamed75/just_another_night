using System;
using System.Collections.Generic;
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

	// Used to keep track of the spooky noises that could play in the player's ears.
	[Net]
	public int EventSoundsPlayed { get; set; } = 0;

	[Net]
	public List<string> PossibleSounds { get; set; }

	public enum GameStates
	{
		FirstCustomer,
		WeirdCustomer,
		SecondNormalCustomerAndWeirdGuyStaring,
		BackDoorOpens,
		FreezerUnlocks
	}

	private bool ShouldPlaySound(int max, float chance, Entity player)
	{
		if (LookingAtCreep)
			return false;

		var tr = Trace.Ray( new Ray(
			player.EyePosition, Vector3.Up
		), 200f ).WithTag("solid").Run();
		if (!tr.Hit) {
			Log.Info( "player is outside" );
			return false;
		}

		if (EventSoundsPlayed < max && Rand.Float() < chance) {
			return true;
		}
		return false;
	}

	private void PlayRandomEventSound(Vector3 pos)
	{
		if (PossibleSounds.Count < 1)
			return;

		int max = PossibleSounds.Count-1;

		var i = Rand.Int( 0, max );
		var sound = PossibleSounds[i];
		Sound.FromWorld( sound, pos + Rotation.Random.Forward * 15f );
		PossibleSounds.RemoveAt( i );
	}

	private async Task GameLoopAsync()
	{
		StateTimer = 5.0f;
		await WaitStateTimer();

		// There should only be a single client since this is a singleplayer game.
		var client = All.OfType<Client>().First();
		var player = client.Pawn as JustAnotherPlayer;

		EventSoundsPlayed = 0;

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
			if (ShouldPlaySound(1, 0.05f, player)) {
				PlayRandomEventSound( player.EyePosition );
			}
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
			if (ShouldPlaySound(1, 0.05f, player)) {
				PlayRandomEventSound( player.EyePosition );
			}
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
		// Wait until there's nobody left.
		await WaitUntilCustomerIsGone();
		StaringCreep();

		while (player.State == JustAnotherPlayer.PlayerStates.MopFloors) {
			if (ShouldPlaySound(2, 0.05f, player)) {
				PlayRandomEventSound( player.EyePosition );
			}
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
		// Make sure the door doesn't close on its own so the player can investigate
		// what's going on.
		backDoor.TimeBeforeReset = -1;

		while (player.State == JustAnotherPlayer.PlayerStates.FixIceCreamMachine) {
			if (ShouldPlaySound(1, 0.1f, player)) {
				PlayRandomEventSound( player.EyePosition );
			}
			StateTimer = 1;
			await WaitStateTimer();
		}

		GameState = GameStates.FreezerUnlocks;
		StateTimer = Rand.Float( 1.0f, 2.0f );
		await WaitStateTimer();

		// initiate freezer door opening and guy waiting in freezer.
		FreezerCreep();

		// Find the freezer door. The map should only have one.
		// var freezerDoor = All.OfType<ControlledDoor>()
		// 	.Where(x => x.Tags.Has("freezer_door"))
		// 	.First();

		// // Unlock the freezer door to open it.
		// freezerDoor.Locked = false;
		// // Open the door using the player so the sound plays in their ears.
		// freezerDoor.OnUse( player );
		// // Lock the door again so the player cannot use it.
		// freezerDoor.Locked = true;

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