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
			var effects = Map.Camera.FindOrCreateHook<Sandbox.Effects.ScreenEffects>();
			effects.ChromaticAberration.Offset = new Vector3(0.0025f, 0.0025f, 0.001f);
			effects.ChromaticAberration.Scale = 1.0f;
			effects.Contrast = 1.015f;
			effects.FilmGrain.Response = 1f;
			effects.FilmGrain.Intensity = 0.025f;
			effects.Saturation = 1.35f;
			effects.Pixelation = 0.10f;
		}
	}
}
