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
public partial class JustAnotherGame : Sandbox.Game
{
	[Net]
	JustAnotherHud Hud { get; set; }

	[Net]
	RealTimeSince TimeSinceClientJoined { get; set; }

	float EffectsVolume { get; set; }

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
		if (IsServer) {
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

			Hud = new JustAnotherHud();

			_ = GameLoopAsync();
		}
	}

	[ConCmd.Server("delete_ent")]
	public static void DeleteEntity(string name)
	{
		var ent = All.OfType<Entity>().Where( x => x.Name == name ).FirstOrDefault();

		if (ent != null && ent.IsValid)
			ent.Delete();
	}

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

	[ConCmd.Server("equip_ent")]
	public static void EquipEntity(string name)
	{
		var player = ConsoleSystem.Caller.Pawn as JustAnotherPlayer;
		var ent = All.OfType<Entity>().Where( x => x.Name == name ).First();

		player.ActiveChild = ent;
		ent.Owner = player;
	}

	[ConCmd.Server("unequip_ent")]
	public static void UnEquipEntity()
	{
		var player = ConsoleSystem.Caller.Pawn as JustAnotherPlayer;
		if (player.ActiveChild == null)
			return;

		player.ActiveChild.Delete();
		player.ActiveChild = null;
	}

	[ConCmd.Admin("creep_stare")]
	public static void SpawnStaringCreep()
	{
		(Current as JustAnotherGame).StaringCreep();
	}

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

	[ClientRpc]
	private void SayCustomerArrived()
	{
		SelfSpeak.Current.SayCustomerArrive();
	}

	[ConCmd.Admin("death")]
	public static void DeathSpawn()
	{
		OpenFreezerDoor();
		(Current as JustAnotherGame).FreezerCreep();
	}

	[ConCmd.Admin("spawn_customer")]
	public static void TestingSpawnCustomer(int group)
	{
		(Current as JustAnotherGame).SpawnCustomer(group);
	}

	// [ConCmd.Admin("creep_anim")]
	// public static void SetCreepAnim(string anim)
	// {
	// 	var creep = All.OfType<Creep>().First();
	// 	if (creep != null)
	// 		creep.SetAnimation( anim );
	// }

	/// <summary>
	/// A client has joined the server. Make them a pawn to play with
	/// </summary>
	public override void ClientJoined( Client client )
	{
		base.ClientJoined( client );

		// Create a pawn for this client to play with
		// var pawn = new Pawn();
		// client.Pawn = pawn;
		var pawn = new JustAnotherPlayer();
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

	public override void FrameSimulate( Client cl )
	{
		base.FrameSimulate( cl );


		if (TimeSinceClientJoined > 5) {
			var player = cl.Pawn as JustAnotherPlayer;

			var effects = Map.Camera.FindOrCreateHook<Sandbox.Effects.ScreenEffects>();
			// effects.ChromaticAberration.Offset = new Vector3(0.0025f, 0.0025f, 0.001f);
			effects.ChromaticAberration.Scale = 1.0f;
			effects.Contrast = 1.015f;
			effects.FilmGrain.Response = 1f;
			effects.FilmGrain.Intensity = 0.025f;
			effects.Saturation = 1.35f;
			// effects.Pixelation = 0.10f;
			effects.Pixelation = 0.15f;
			// effects.Pixelation = 0.20f;

			if (LookingAtCreep || player.Incapacitated) {
				var delta = Time.Delta / 4f;

				effects.Vignette.Intensity = (effects.Vignette.Intensity + delta).Clamp( 0.0f, 0.6f );

				effects.ChromaticAberration.Offset = new Vector3(
					(effects.ChromaticAberration.Offset.x + delta/75).Clamp( 0.0025f, 0.005f ),
					(effects.ChromaticAberration.Offset.y + delta/75).Clamp( 0.0025f, 0.01f ),
					(effects.ChromaticAberration.Offset.z + delta/75).Clamp( 0.001f, 0.015f )
				);

			} else {
				var delta = Time.Delta / 4f;

				effects.Vignette.Intensity = (effects.Vignette.Intensity - delta).Clamp( 0.0f, 1.0f );

				effects.ChromaticAberration.Offset = new Vector3(
					(effects.ChromaticAberration.Offset.x - delta/75).Clamp( 0.0025f, 1.0f ),
					(effects.ChromaticAberration.Offset.y - delta/75).Clamp( 0.0025f, 1.0f ),
					(effects.ChromaticAberration.Offset.z - delta/75).Clamp( 0.001f, 1.0f )
				);

				// EffectsVolume = (EffectsVolume - Time.Delta / 4).Clamp(0.0f, 1.0f);
			}
		}
	}

	public override void Simulate( Client cl )
	{
		base.Simulate( cl );

		var player = cl.Pawn as JustAnotherPlayer;
		if (player.Incapacitated && player.TimeUntilDeath < -30f) {
			if (IsServer) {
				cl.Kick();

				return;
			}
		}

		foreach (var child in Children) {
			child.Simulate( cl );

			if (child is not Creep creep)
				continue;

			bool prevLook = LookingAtCreep;
			CheckForCreep( cl, creep );

			if (creep.Finished) {
				creep.Delete();
				break;
			}

			if (LookingAtCreep)
				break;
		}

		// If there aren't any creeps on the map then you're not looking at one.
		// note: This is to fix when looking at a creep when they delete.
		if (!All.OfType<Creep>().Any()) {
			LookingAtCreep = false;
		}

		// CheckForCreep( cl );
	}

	public void CheckForCreep(Client cl, Creep creep)
	{
		var player = cl.Pawn as Player;

		if (Rand.Int(0,10) < 9) {
			return;
		}

		// var creep = All.OfType<Creep>().FirstOrDefault();


		if (creep != null && creep.IsValid()) {
			// If the creep is in a car then don't trigger any effects.
			// if (creep.State == Creep.CreepStates.Car) {
			// 	creep.LookedAt();
			// 	TimeSinceStare = 0;

			// 	return;
			// }

			var ray = new Ray(
				player.EyePosition, player.EyeRotation.Forward
			);

			var tr = Trace.Ray( ray, player.Position.Distance( creep.Position ) )
				.WithAnyTags("creep", "solid")
				.Run();

			var distance = tr.EndPosition.Distance( creep.Position + Vector3.Up * 60f );
			var angle = Vector3.GetAngle( player.EyeRotation.Forward, creep.Position - player.EyePosition );

			// Check to see if we can see him in our eye sight.
			tr = Trace.Ray( player.EyePosition + player.EyeRotation.Forward * 10f, creep.Position + Vector3.Up * 10f )
				.WithAnyTags( "creep", "solid" )
				.Run();

			if (distance < 1000.0f && tr.Entity == creep && angle < 60.0f) {
				if (!LookingAtCreep) {
					creep.LookedAt();
					TimeSinceStare = 0;
					// if (creep.State != Creep.CreepStates.Car)
					if (creep.MaterialGroup == 0)
						LookingAtCreep = true;
				}
			} else {
				if (LookingAtCreep) {
					TimeUntilDecayEffects = 5;
					LookingAtCreep = false;
				}
			}
		}
	}
}
