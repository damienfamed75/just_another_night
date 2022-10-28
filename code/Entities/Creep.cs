using System;
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
	public int TimesKilled { get; set; } = 0;

	[Net]
	public int MaterialGroup { get; set; } = 0;

	[Net]
	public bool HasDialogued { get; set; } = false;

	public bool BeingLookedAt => (Game.Current as JustAnotherGame).LookingAtCreep;

	public RealTimeSince TimeSinceLookedAt => (Game.Current as JustAnotherGame).TimeSinceStare;

	[Net]
	public RealTimeSince CarSinceLookedAt { get; set; }

	NavPath Path;

	public enum CreepStates
	{
		TakeOutTrashBags,
		WatchOutsideBuilding,
		Kill,
		DeathScreen,
		Car,
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

		if (MaterialGroup != 0)
			SetMaterialGroup( MaterialGroup );
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
			case CreepStates.Car:
				CarSimulate( cl );
				break;
			default:
				break;
		}

		// DebugOverlay.Sphere(
		// 	Position + Vector3.Up * 45f + Rotation.Left * 36f,
		// 	5f, Color.Red
		// );

		PrevHasBeenLookedAt = HasBeenLookedAt;
		// Rotation = Rotation.LookAt( cl.Pawn.Position - Position, Vector3.Up );
	}

	public void CarSimulate(Client cl)
	{
		if (PrevHasBeenLookedAt != HasBeenLookedAt) {
			CarSinceLookedAt = 0;
		}
		// if (!HasDialogued && HasBeenLookedAt && Position.AlmostEqual(SpawnLocation, 5f)) {
		// 	if (IsClient) {
		// 		string message = MaterialGroup switch {
		// 			0 => "...",
		// 			1 => "hey what's up",
		// 			2 => "can i just get my order please.",
		// 			_ => "i am error"
		// 		};

		// 		var dialogue = new Speech(message);
		// 		dialogue.Position = Position + Vector3.Up * 55f + Rotation.Left * 34f;

		// 		Log.Info( "created speech bubble" );
		// 	}

		// 	HasDialogued = true;
		// }

		var targetAnim = "sit";
		if (MaterialGroup != 0) {
			targetAnim = "sit" + MaterialGroup.ToString();
		}

		if (CurrentSequence.Name != targetAnim || CurrentSequence.IsFinished) {
			PlaybackRate = 1f;
			DirectPlayback.Play( targetAnim );
			SetAnimation( targetAnim );
		}

		// if (IsClient)
		// 	return;

		// if (Position)
		// Position += Rotation.Forward * 50 * Time.Delta;

		foreach(var child in Children) {
			if (child is not Car car)
				continue;

			car.Simulate( cl );

			if (IsClient) {
				car.RevSound.SetPitch(
					(MathF.Abs( Position.Distance( SpawnLocation ) ) / 150f) + 1f
				);
				continue;
			}

			// This is extremely fucking ugly...
			//! todo refactor.
			if (car.Finished) {
				// Position += Rotation.Forward * 50 * Time.Delta;
				var dest = SpawnLocation + Rotation.Forward * 300f;
				if (!Position.AlmostEqual(dest, 1f)) {
					Position += (dest - Position) / 100f;
				} else {
					Finished = true;
				}
			} else {
				if (!Position.AlmostEqual(SpawnLocation, 1f)) {
					Position += (SpawnLocation - Position) / 50f;
				} else {
					car.CreateButton();
				}
			}
		}
	}

	public void DeathSimulate(Client cl)
	{
		// if (IsClient)
		// 	return;

		var player = cl.Pawn as JustAnotherPlayer;

		if (IsServer) {
			// Make sure we're facing the player
			Rotation = Rotation.LookAt( player.Position.WithZ(0) - Position.WithZ(0), Vector3.Up );
			Position += Rotation.Backward * 0.05f;
		}

		if ((CurrentSequence.Name != "killing" && CurrentSequence.Name != "kill") || CurrentSequence.IsFinished) {
			if (player.TimeUntilDeath < 1f) {
				return;
			}
			// Log.Info( $"curr[{CurrentSequence.Name}]" );

			TimesKilled += 1;
			PlaybackRate = 1.5f;
			DirectPlayback.Play( "killing" );
			SetAnimation( "killing" );
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