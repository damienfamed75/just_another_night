using Sandbox;
using Sandbox.UI.Construct;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// This is your game class. This is an entity that is created serverside when
/// the game starts, and is replicated to the client. 
/// 
/// You can use this to create things like HUDs and declare which player class
/// to use for spawned players.
/// </summary>
public partial class JustAnotherGame : Sandbox.GameManager
{
	[Net]
	JustAnotherHud Hud { get; set; }

	[Net]
	RealTimeSince TimeSinceClientJoined { get; set; }

	[Net]
	public bool LookingAtCreep { get; set; } = false;

	[Net, Predicted]
	public RealTimeSince TimeSinceStare { get; set; }

	[Net, Predicted]
	public RealTimeUntil TimeUntilDecayEffects { get; set; }

	[Net]
	public bool WaitingCustomer { get; set; }

	[Net]
	public RealTimeSince TimeSinceStateChanged { get; set; }

	public JustAnotherGame()
	{
		if (Game.IsServer) {
			//
			// A list of random sounds that could play throughout the game.
			//
			PossibleSounds = new List<string>()
			{
				"rat_scurry",
				"wood_grazing",
				"wobble",
				"distant_drop1",
				"distant_drop2",
				"close_quiet",
				"close_loud"
			};
			// Create the UI
			Hud = new JustAnotherHud();
			// Start the async gameplay loop.
			_ = GameLoopAsync();
		}
	}

	/// <summary>
	/// Clientside RPC to have the UI popup that a customer has arrived.
	/// </summary>
	[ClientRpc]
	private void SayCustomerArrived()
	{
		SelfSpeak.Current.SayCustomerArrive();
	}

	/// <summary>
	/// A client has joined the server. Make them a pawn to play with
	/// </summary>
	public override void ClientJoined( IClient client )
	{
		base.ClientJoined( client );

		// Create a pawn for this client to play with
		// var pawn = new Pawn();
		// client.Pawn = pawn;
		var pawn = new JustAnotherPlayer();
		pawn.Respawn();

		client.Pawn = pawn;

		// Get all of the spawnpoints
		var spawnpoints = Entity.All.OfType<SpawnPoint>();

		// chose a random one
		var randomSpawnPoint = spawnpoints.OrderBy( x => Guid.NewGuid() ).FirstOrDefault();

		// if it exists, place the pawn there
		if ( randomSpawnPoint != null )
		{
			var tx = randomSpawnPoint.Transform;
			tx.Position = tx.Position + Vector3.Up * 50.0f; // raise it up
			pawn.Transform = tx;
		}

		TimeSinceClientJoined = 0;
		TimeSinceStateChanged = 0;
	}

	/// <summary>
	/// Called each frame on the client only to simulate things that need to be
	/// updated every frame.
	/// </summary>
	public override void FrameSimulate( IClient cl )
	{
		base.FrameSimulate( cl );

		// If the player hasn't been in the game for more than 5 seconds yet
		// then do not apply the post-processing effects.
		if (TimeSinceClientJoined < 5)
			return;

		var player = cl.Pawn as JustAnotherPlayer;

		var effects = Camera.Main.FindOrCreateHook<Sandbox.Effects.ScreenEffects>();
		effects.ChromaticAberration.Scale = 1.0f;
		effects.Contrast = 1.015f; // Very slightly increase contrast.
		effects.FilmGrain.Response = 1f; // Only show grain in dark areas.
		effects.FilmGrain.Intensity = 0.025f;
		effects.Saturation = 1.35f; // Saturate more to get a deepfried look
		effects.Pixelation = 0.15f; // 0.1f is also good. 0.2f is more accurate to PSX

		// If you're looking at at creep or the player is incapacitated.
		if (LookingAtCreep || player.Incapacitated) {
			var delta = Time.Delta / 4f;
			// Increase vignette.
			effects.Vignette.Intensity = (effects.Vignette.Intensity + delta).Clamp( 0.0f, 0.6f );
			// Intensify the chromatic abberation by making the offset greater.
			effects.ChromaticAberration.Offset = new Vector3(
				(effects.ChromaticAberration.Offset.x + delta/75).Clamp( 0.0025f, 0.005f ),
				(effects.ChromaticAberration.Offset.y + delta/75).Clamp( 0.0025f, 0.01f ),
				(effects.ChromaticAberration.Offset.z + delta/75).Clamp( 0.001f, 0.015f )
			);
		} else {
			var delta = Time.Delta / 4f;
			// Decrease vignette.
			effects.Vignette.Intensity = (effects.Vignette.Intensity - delta).Clamp( 0.0f, 1.0f );
			// Decrease chromatic abberation offset.
			effects.ChromaticAberration.Offset = new Vector3(
				(effects.ChromaticAberration.Offset.x - delta/75).Clamp( 0.0025f, 1.0f ),
				(effects.ChromaticAberration.Offset.y - delta/75).Clamp( 0.0025f, 1.0f ),
				(effects.ChromaticAberration.Offset.z - delta/75).Clamp( 0.001f, 1.0f )
			);
		}
	}

	/// <summary>
	/// Serverside: Called for each client every tick
	/// Clientside: Called for each tick for local client. Can be called multiple times per tick.
	/// </summary>
	public override void Simulate( IClient cl )
	{
		base.Simulate( cl );

		//
		// Disconnect the player when the game is over.
		//
		var player = cl.Pawn as JustAnotherPlayer;
		if (player.Incapacitated && player.TimeUntilDeath < -30f) {
			if (Game.IsServer) {
				cl.Kick();
				return;
			}
		}

		// All children in this will be of type Creep.
		// So this loop is specifically only for updating creeps spawned on the map.
		foreach (var child in Children) {
			// Simulate the child.
			child.Simulate( cl );

			if (child is not Creep creep)
				continue;

			// Check to see if the player can see this creep.
			CheckForCreep( cl, creep );
			// If this creep is marked as finished, then delete them.
			if (creep.Finished) {
				creep.Delete();
				break;
			}
			// If we're looking at this creep then break.
			if (LookingAtCreep)
				break;
		}

		// If there aren't any creeps on the map then you're not looking at one.
		// note: This is to fix when looking at a creep when they delete.
		if (!All.OfType<Creep>().Any()) {
			LookingAtCreep = false;
		}
	}

	public void CheckForCreep(IClient cl, Creep creep)
	{
		var player = cl.Pawn as Player;

		var rnd = new Random();

		// Only check every so often.
		// this is pretty hacky but this is a game jam entry afterall.
		if (rnd.Int(0,10) < 9) {
			return;
		}

		if (creep != null && creep.IsValid()) {
			// Trace forward from where the player is looking.
			var ray = new Ray(
				player.EyePosition, player.EyeRotation.Forward
			);
			var tr = Trace.Ray( ray, player.Position.Distance( creep.Position ) )
				.WithAnyTags("creep", "solid")
				.Run();

			// Check the distance of where we're looking to this creep.
			var distance = tr.EndPosition.Distance( creep.Position + Vector3.Up * 60f );
			var angle = Vector3.GetAngle( player.EyeRotation.Forward, creep.Position - player.EyePosition );

			// Check to see if we can see him in our eye sight.
			tr = Trace.Ray( player.EyePosition + player.EyeRotation.Forward * 10f, creep.Position + Vector3.Up * 10f )
				.WithAnyTags( "creep", "solid" )
				.Run();

			// If the distance is less than 1000 then they're considered on the
			// screen and we're looking at them.
			if (distance < 1000.0f && tr.Entity == creep && angle < 60.0f) {
				if (!LookingAtCreep) {
					creep.LookedAt();
					TimeSinceStare = 0;
					// if (creep.State != Creep.CreepStates.Car)

					// If this material group is zero then that means it's the
					// ACTUAL creep and not a customer or someone else.
					// this needs a constant.
					if (creep.MaterialGroup == 0)
						LookingAtCreep = true;
				}
			} else {
				// Since we're not currently looking at a creep,
				// if we are already looking at a creep, then just override it
				// and say we're not looking at a creep.
				if (LookingAtCreep) {
					TimeUntilDecayEffects = 5;
					LookingAtCreep = false;
				}
			}
		}
	}
}
