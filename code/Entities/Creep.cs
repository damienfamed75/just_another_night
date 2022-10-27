using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Sandbox;

public partial class Creep : AnimatedMapEntity
{
	[Net]
	public CreepStates State { get; set; }

	[Net]
	public bool Finished { get; private set; }

	[Net]
	public Vector3 SpawnLocation { get; set; }

	[Net]
	public bool PrevHasBeenLookedAt { get; set; } = false;

	[Net]
	public bool HasBeenLookedAt { get; set; } = false;

	[Net]
	public int TimesKilled { get; set; }

	public bool BeingLookedAt => (Game.Current as JustAnotherGame).LookingAtCreep;

	public RealTimeSince TimeSinceLookedAt => (Game.Current as JustAnotherGame).TimeSinceStare;

	NavPath Path;

	public enum CreepStates
	{
		TakeOutTrashBags,
		WatchOutsideBuilding,
		Kill,
		DeathScreen
	}

	public Creep()
	{
		Tags.Add( "creep" );
		AnimateOnServer = true;
	}

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/creep.vmdl" );
		// AnimateOnServer = true;

		// SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
		// EnableAllCollisions = true;

		SpawnLocation = Position;
	}

	public override void Simulate( Client cl )
	{
		base.Simulate( cl );

		// if (IsClient)
		// 	return;

		switch (State) {
			case CreepStates.TakeOutTrashBags:
				TrashBagsSimulate( cl );
				break;
			case CreepStates.WatchOutsideBuilding:
				WatchSimulate( cl );
				break;
			case CreepStates.Kill:
				KillSimulate( cl );
				break;
			case CreepStates.DeathScreen:
				DeathSimulate( cl );
				break;
			default:
				break;
		}

		PrevHasBeenLookedAt = HasBeenLookedAt;
		// Rotation = Rotation.LookAt( cl.Pawn.Position - Position, Vector3.Up );
	}

	public void DeathSimulate(Client cl)
	{
		// if (IsClient)
		// 	return;

		var player = cl.Pawn as JustAnotherPlayer;

		if (IsServer) {
			// Make sure we're facing the player
			Rotation = Rotation.LookAt( player.Position.WithZ(0) - Position.WithZ(0), Vector3.Up );
		}

		// Hacky solution but I'll take it because no time.
		// if (CurrentSequence.Name != "kill" || CurrentSequence.IsFinished || DirectPlayback.Name != "kill") {
		// 	PlaybackRate = 2;
		// 	DirectPlayback.Play( "kill" );
		// 	SetAnimation( "kill" );
		// }
		// Log.Info(
		// 	$"name[{CurrentSequence.Name}] fin[{CurrentSequence.IsFinished}] tim[{CurrentSequence.TimeNormalized}]"
		// );

		// Log.Info(
		// 	$"name[{DirectPlayback.Name}] time[{DirectPlayback.TimeNormalized}]"
		// );

		if ((CurrentSequence.Name != "killing" && CurrentSequence.Name != "kill") || CurrentSequence.IsFinished) {
			Log.Info( $"curr[{CurrentSequence.Name}]" );
			if (TimesKilled > 5) {
				return;
			}

			TimesKilled++;
			PlaybackRate = 1.5f;
			DirectPlayback.Play( "killing" );
			SetAnimation( "killing" );
		}

		if (IsServer) {
			Position += Rotation.Backward * 0.05f * TimesKilled;
		}
	}

	public void KillSimulate(Client cl)
	{
		// If we've been looked at then you are officially deaded.
		if (HasBeenLookedAt) {
			// Hacky solution but I'll take it because no time.
			if (CurrentSequence.Name != "walk" || CurrentSequence.IsFinished) {
				PlaybackRate = 2f;
				DirectPlayback.Play( "walk" );
				SetAnimation( "walk" );
			}

			if (IsClient)
				return;

			var player = cl.Pawn as JustAnotherPlayer;

			// Make sure we're facing the player
			Rotation = Rotation.LookAt( player.Position.WithZ(0) - Position.WithZ(0), Vector3.Up );

			if (Steer == null) {
				if (!target.IsValid())
					FindTarget();

				Steer = new NavSteer {
					Target = target.Position
				};
			}
			Steer.Target = target.Position;
			// Steer.DebugDrawPath();
			Steer.Tick( Position, Velocity );


			Vector3 inputVelocity = default;
			if (!Steer.Output.Finished) {
				inputVelocity = Steer.Output.Direction.Normal;
				var speed = 200f;
				Velocity = Velocity.AddClamped( inputVelocity * Time.Delta * 500, speed );
			}

			var ray = new Ray( Position + Vector3.Up * 10, Vector3.Down );

			var tr = Trace.Ray( ray, 32f )
				.Ignore( this )
				.WithoutTags("player")
				.Run();
			if (tr.Hit) {
				Position = Position.WithZ( tr.EndPosition.z );
			}

			Position += inputVelocity.WithZ(0) * 3.25f;

			// ray = new Ray( Position + Vector3.Up * 10f, Rotation.Forward );
			// tr = Trace.Ray( ray, 12f )
			// 	.WithTag( "player" )
			// 	.Run();
			var distance = Position.Distance( player.Position );

			// Log.Info( distance );

			if (distance < 50) {
				// Kill player
				Log.Info( "DEATH" );
				State = CreepStates.DeathScreen;
				player.Incapacitated = true;

				PlaybackRate = 1.5f;

				SetAnimation( "kill" );
				KillAnim();
			}
		}
	}

	[ClientRpc]
	private void KillAnim()
	{
		SetAnimation( "kill" );
	}

	public void WatchSimulate(Client cl)
	{
		if (IsClient)
			return;

		Rotation = Rotation.LookAt( cl.Pawn.Position - Position, Vector3.Up );
	}

	public void TrashBagsSimulate(Client cl)
	{
		// If we were just looked at.
		if (HasBeenLookedAt && !PrevHasBeenLookedAt) {
			// Play the sound at the player.
			if (IsServer)
				Sound.FromEntity( "creep-trash-short", cl.Pawn );
		}

		if (HasBeenLookedAt && TimeSinceLookedAt > 9.0f) {
			// Hacky solution but I'll take it because no time.
			if (CurrentSequence.Name != "walk" || CurrentSequence.IsFinished) {
				DirectPlayback.Play( "walk" );
				SetAnimation( "walk" );
			}

			if (IsClient)
				return;

			var goal = All.OfType<NPCSpawn>()
				.Where( x => x.Tags.Has( "creep_trash_end" ) )
				.First();

			// Make sure we're facing the goal
			Rotation = Rotation.LookAt( goal.Position.WithZ(0) - Position.WithZ(0), Vector3.Up );
			// Lerp to the goal using the relative time.
			Position = SpawnLocation.LerpTo(
				goal.Position,
				TimeSinceLookedAt.Relative.LerpInverse( 10, 20 )
			);

			if (Position.AlmostEqual(goal.Position, 0.1f)) {
				Finished = true;
			}
		}
	}

	public void LookedAt()
	{
		HasBeenLookedAt = true;
	}
}