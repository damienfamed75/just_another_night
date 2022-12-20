using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sandbox;
using JustAnotherNight.Player;
using JustAnotherNight.Entities;

namespace JustAnotherNight;

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
	public IList<string> PossibleSounds { get; set; }

	public enum GameStates
	{
		FirstCustomer,
		WeirdCustomer,
		SecondNormalCustomerAndWeirdGuyStaring,
		BackDoorOpens,
		FreezerUnlocks
	}

	private bool ShouldPlaySound( int max, float chance, JustAnotherPlayer player )
	{
		if ( LookingAtCreep )
			return false;

		var tr = Trace.Ray( new Ray(
			player.EyePosition, Vector3.Up
		), 200f ).WithTag( "solid" ).Run();
		if ( !tr.Hit )
		{
			Log.Info( "player is outside" );
			return false;
		}

		var rnd = new Random();

		if ( EventSoundsPlayed < max && rnd.Float() < chance )
		{
			return true;
		}
		return false;
	}

	private void PlayRandomEventSound( Vector3 pos )
	{
		if ( PossibleSounds.Count < 1 )
			return;

		int max = PossibleSounds.Count - 1;

		var rnd = new Random();

		var i = rnd.Int( 0, max );
		var sound = PossibleSounds[i];
		Sound.FromWorld( sound, pos + Rotation.Random.Forward * 15f );
		PossibleSounds.RemoveAt( i );
	}

	/// <summary>
	/// This is the gameplay loop. Not much of a loop as much as it is a string
	/// of events that should happen ingame.
	/// </summary>
	private async Task GameLoopAsync()
	{
		// Wait 5 seconds for the intro to fade away.
		StateTimer = 5.0f;
		await WaitStateTimer();

		// There should only be a single client since this is a singleplayer game.
		var client = All.OfType<IClient>().First();
		var player = client.Pawn as JustAnotherPlayer;

		EventSoundsPlayed = 0;
		// Wait until the player finished picking up trash.
		while ( player.State == JustAnotherPlayer.PlayerStates.PickupTrash )
		{
			StateTimer = 1;
			await WaitStateTimer();
		}

		// Initiate first customer
		TrashStaringCreep();

		GameState = GameStates.FirstCustomer;
		// Randomly wait until first customer comes.
		var rnd = new Random();
		StateTimer = rnd.Float( 5.0f, 10.0f );
		await WaitStateTimer();

		await FirstNormalCustomer();
		Sound.FromEntity( "customer-beep", player );
		// Wait until player is done taking out trash.
		while ( player.State == JustAnotherPlayer.PlayerStates.TakeOutTrashBags )
		{
			// Randomly choose whether to play a sound or not.
			if ( ShouldPlaySound( 1, 0.05f, player ) )
			{
				PlayRandomEventSound( player.EyePosition );
			}
			StateTimer = 1;
			await WaitStateTimer();
		}

		GameState = GameStates.WeirdCustomer;
		StateTimer = rnd.Float( 5.0f, 8.0f );
		await WaitStateTimer();
		// Delete the creep from staring down the path.
		await CreepCustomer();
		Sound.FromEntity( "customer-beep", player );

		StateTimer = rnd.Float( 1.0f, 2.0f );
		await WaitStateTimer();

		// initiate weird customer.

		while ( player.State == JustAnotherPlayer.PlayerStates.WashDishes )
		{
			// Randomly choose whether to play a sound or not.
			if ( ShouldPlaySound( 1, 0.05f, player ) )
			{
				PlayRandomEventSound( player.EyePosition );
			}
			StateTimer = 1;
			await WaitStateTimer();
		}

		GameState = GameStates.SecondNormalCustomerAndWeirdGuyStaring;
		StateTimer = rnd.Float( 1.0f, 5.0f );
		await WaitStateTimer();

		await SecondNormalCustomer();
		Sound.FromEntity( "customer-beep", player );
		// initiate second normal customer.

		StateTimer = rnd.Float( 1.0f, 5.0f );
		await WaitStateTimer();

		// initiate customer staring through windows.
		// Wait until there's nobody left.
		await WaitUntilCustomerIsGone();
		StaringCreep();

		while ( player.State == JustAnotherPlayer.PlayerStates.MopFloors )
		{
			// Randomly choose whether to play a sound or not.
			if ( ShouldPlaySound( 2, 0.05f, player ) )
			{
				PlayRandomEventSound( player.EyePosition );
			}
			StateTimer = 1;
			await WaitStateTimer();
		}

		GameState = GameStates.BackDoorOpens;
		StateTimer = rnd.Float( 0.0f, 1.0f );
		await WaitStateTimer();

		// initiate back door opening.
		var backDoor = All.OfType<ControlledDoor>()
			.Where( x => x.Tags.Has( "back_door" ) )
			.First();

		backDoor.OnUse( player );
		// Make sure the door doesn't close on its own so the player can investigate
		// what's going on.
		backDoor.TimeBeforeReset = -1;

		while ( player.State == JustAnotherPlayer.PlayerStates.FixIceCreamMachine )
		{
			// Randomly choose whether to play a sound or not.
			if ( ShouldPlaySound( 1, 0.1f, player ) )
			{
				PlayRandomEventSound( player.EyePosition );
			}
			StateTimer = 1;
			await WaitStateTimer();
		}

		GameState = GameStates.FreezerUnlocks;
		StateTimer = rnd.Float( 1.0f, 2.0f );
		await WaitStateTimer();

		// initiate freezer door opening and guy waiting in freezer.
		FreezerCreep();

		// Wait until the player is dead pretty much.
		while ( player.State == JustAnotherPlayer.PlayerStates.OrganizeFreezer )
		{
			StateTimer = 1;
			await WaitStateTimer();
		}
	}

	private async Task WaitStateTimer()
	{
		while ( StateTimer > 0 )
		{
			await Task.DelayRealtimeSeconds( 1.0f );
		}

		await Task.DelayRealtimeSeconds( 1.0f );
	}
}
