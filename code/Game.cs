using Sandbox;
using Sandbox.UI.Construct;
using System;
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

	Sound Violins { get; set; }
	Sound Heartbeat { get; set; }
	float EffectsVolume { get; set; }

	[Net]
	public bool LookingAtCreep { get; set; } = false;

	[Net, Predicted]
	public RealTimeSince TimeSinceStare { get; set; }

	[Net, Predicted]
	public RealTimeUntil TimeUntilDecayEffects { get; set; }

	public JustAnotherGame()
	{
		if (IsServer) {
			Hud = new JustAnotherHud();

			_ = GameLoopAsync();
		}
	}

	[ConCmd.Server("delete_ent")]
	public static void DeleteEntity(string name)
	{
		var ent = All.OfType<Entity>().Where( x => x.Name == name ).First();
		ent.Delete();
	}

	[ConCmd.Server("increment_task")]
	public static void IncrementTask()
	{
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
		var freezerDoor = All.OfType<ControlledDoor>()
			.Where( x => x.Tags.Has( "back_door" ) )
			.First();

		freezerDoor.OnUse( caller );
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
	}

	public override void FrameSimulate( Client cl )
	{
		base.FrameSimulate( cl );


		if (TimeSinceClientJoined > 5) {
			var player = cl.Pawn as Player;

			var effects = Map.Camera.FindOrCreateHook<Sandbox.Effects.ScreenEffects>();
			effects.ChromaticAberration.Offset = new Vector3(0.0025f, 0.0025f, 0.001f);
			effects.ChromaticAberration.Scale = 1.0f;
			effects.Contrast = 1.015f;
			effects.FilmGrain.Response = 1f;
			effects.FilmGrain.Intensity = 0.025f;
			effects.Saturation = 1.35f;
			effects.Pixelation = 0.10f;

			// var creep = All.OfType<Creep>().FirstOrDefault();
			// if (creep != null && creep.IsValid()) {
			// 	var ray = new Ray(
			// 		player.EyePosition, player.EyeRotation.Forward
			// 	);

			// 	var tr = Trace.Ray( ray, player.Position.Distance( creep.Position ) )
			// 		.WithAnyTags("creep", "solid")
			// 		.Run();

			// 	DebugOverlay.ScreenText( $"{tr.EndPosition.Distance( creep.Position + Vector3.Up * 60f )}", new Vector2( 10, 10 ), 0, Color.Yellow );

			// 	var distance = tr.EndPosition.Distance( creep.Position + Vector3.Up * 60f );

			// 	if (distance < 1300.0f) {
			// 		if (!LookingAtCreep) {
			// 			Heartbeat = Sound.FromEntity( "heartbeat", player );
			// 			Violins = Sound.FromEntity( "violins-loop", player );
			// 			TimeSinceStare = 0;
			// 			LookingAtCreep = true;
			// 		}

			// 		effects.Vignette.Intensity = (effects.Vignette.Intensity + Time.Delta / 4).Clamp( 0.0f, 0.6f );

			// 		Violins.SetVolume( distance.LerpInverse( 1250.0f, 50f ) );
			// 		Heartbeat.SetVolume( distance.LerpInverse( 1250.0f, 50f ) );
			// 	} else {
			// 		if (LookingAtCreep) {
			// 			Heartbeat.Stop();
			// 			Violins.Stop();
			// 			TimeSinceStare = 0;
			// 			LookingAtCreep = false;
			// 		}

			// 		effects.Vignette.Intensity = (effects.Vignette.Intensity - Time.Delta / 4).Clamp( 0.0f, 1.0f );
			// 	}

			// 	if (TimeSinceStare > 5) {

			// 	}
			// }
			if (LookingAtCreep) {
				effects.Vignette.Intensity = (effects.Vignette.Intensity + Time.Delta / 4).Clamp( 0.0f, 0.6f );

				// EffectsVolume = TimeSinceStare / 5.0f;
				// Heartbeat.SetVolume( EffectsVolume );
				// Violins.SetVolume( EffectsVolume );
			} else {
				effects.Vignette.Intensity = (effects.Vignette.Intensity - Time.Delta / 4).Clamp( 0.0f, 1.0f );

				EffectsVolume = (EffectsVolume - Time.Delta / 4).Clamp(0.0f, 1.0f);
			}

			Heartbeat.SetVolume( EffectsVolume );
			Violins.SetVolume( EffectsVolume );

			if (EffectsVolume == 0.0f) {
				Heartbeat.Stop();
				Violins.Stop();
			}
		}
	}

	[ClientRpc]
	public void StartLookAtCreepEffects()
	{
		// Heartbeat = Sound.FromEntity( "heartbeat", Local.Pawn );
		// Heartbeat.SetVolume( 0.0f );
		// Violins = Sound.FromEntity( "violins-loop", Local.Pawn );
		// Violins.SetVolume( 0.0f );
	}

	[ClientRpc]
	public void StopLookAtCreepEffects()
	{
		// Heartbeat.Stop();
		// Violins.Stop();
	}

	public override void Simulate( Client cl )
	{
		base.Simulate( cl );

		foreach (var child in Children) {
			child.Simulate( cl );
		}

		CheckForCreep( cl );
	}

	public void CheckForCreep(Client cl)
	{
		var player = cl.Pawn as Player;

		if (Rand.Int(0,10) < 9) {
			return;
		}

		var creep = All.OfType<Creep>().FirstOrDefault();
		if (creep != null && creep.IsValid()) {
			var ray = new Ray(
				player.EyePosition, player.EyeRotation.Forward
			);

			var tr = Trace.Ray( ray, player.Position.Distance( creep.Position ) )
				.WithAnyTags("creep", "solid")
				.Run();

			var distance = tr.EndPosition.Distance( creep.Position + Vector3.Up * 60f );

			// Check to see if we can see him in our eye sight.
			tr = Trace.Ray( player.EyePosition, creep.Position + Vector3.Up * 10f )
				.WithAnyTags( "creep", "solid" )
				.Run();

			if (distance < 1000.0f && tr.Entity is Creep) {
				if (!LookingAtCreep) {
					creep.LookedAt();
					StartLookAtCreepEffects();
					TimeSinceStare = 0;
					LookingAtCreep = true;
				}
			} else {
				if (LookingAtCreep) {
					StopLookAtCreepEffects();
					TimeUntilDecayEffects = 5;
					LookingAtCreep = false;
				}
			}
		}
	}
}
