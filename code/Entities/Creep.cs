using System;
using System.Linq;
using Sandbox;
using JustAnotherNight.Player;

namespace JustAnotherNight.Entities;

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

	public bool BeingLookedAt => (GameManager.Current as JustAnotherGame).LookingAtCreep;

	public RealTimeSince TimeSinceLookedAt => (GameManager.Current as JustAnotherGame).TimeSinceStare;

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

	public override void Simulate( IClient cl )
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

	public void CarSimulate(IClient cl)
	{
		if (PrevHasBeenLookedAt != HasBeenLookedAt) {
			CarSinceLookedAt = 0;
			if (MaterialGroup == 0)
				Sound.FromEntity( "creep_car", cl.Pawn as Entity );
		}

		var player = cl.Pawn as JustAnotherPlayer;

		var targetAnim = "sit";
		if (MaterialGroup != 0) {
			targetAnim = "sit" + MaterialGroup.ToString();
		} else {
			// Log.Info(GetBoneIndex( "head" ));
			const int headBone = 5;
			var boneTransform = GetBoneTransform( headBone );
			// SetBone( headBone, boneTransform.WithRotation(
			// 	boneTransform.Rotation.Angles().WithYaw()
			// ))
			var lookAt = Rotation.LookAt( player.EyePosition - boneTransform.Position, Vector3.Up );
			SetBone( headBone, boneTransform.WithRotation(
				lookAt.Angles().WithYaw(lookAt.Yaw() + 90f).WithRoll(lookAt.Roll()+90f).ToRotation()
			) );
		}

		if (CurrentSequence.Name != targetAnim || CurrentSequence.IsFinished) {
			PlaybackRate = 1f;
			DirectPlayback.Play( targetAnim );
			SetAnimation( targetAnim );
		}

		foreach(var child in Children) {
			if (child is not Car car)
				continue;


			car.Simulate( cl );

			// Wait and stare for a bit longer
			if (MaterialGroup == 0 && car.TimeSinceFinished < 10)
				continue;

			// Wait a little bit before going.
			// if (MaterialGroup != 0 && car.TimeSinceFinished < 3)
			// 	continue;
			if (MaterialGroup == 1 && car.TimeSinceFinished < 3)
				continue;

			if (MaterialGroup == 2 && car.TimeSinceFinished < 6)
				continue;

			if (Game.IsClient) {
				float dist = MathF.Abs( Position.Distance( SpawnLocation ) ) / 150f;
				// Set the pitch to go up based on distance from the window.
				car.RevSound.SetPitch(
					dist + 1f
				);
				// Set the volume to go down based on distance from the window.
				car.RevSound.SetVolume(
					(1f - dist).Clamp( 0.0f, 1.0f )
				);
				continue;
			}

			// This is extremely fucking ugly...
			//! todo refactor.
			if (car.Finished) {
				// Position += Rotation.Forward * 50 * Time.Delta;
				var dest = SpawnLocation + Rotation.Forward * 400f;
				if (!Position.AlmostEqual(dest, 1f)) {
					Position += (dest - Position) / 200f;
				} else {
					car.StopSounds();
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

	public void DeathSimulate(IClient cl)
	{
		// if (IsClient)
		// 	return;

		var player = cl.Pawn as JustAnotherPlayer;

		if (Game.IsServer) {
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

	public void KillSimulate(IClient cl)
	{
		// If we've been looked at then you are officially deaded.
		if (HasBeenLookedAt) {
			// Hacky solution but I'll take it because no time.
			if (CurrentSequence.Name != "walk" || CurrentSequence.IsFinished) {
				PlaybackRate = 2f;
				DirectPlayback.Play( "walk" );
				SetAnimation( "walk" );
			}

			if (Game.IsClient)
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

	[Net]
	public int GoingToGoal { get; set; } = 0;

	[Net]
	public RealTimeSince TimeSinceReachedGoal { get; set; }

	public void WatchSimulate(IClient cl)
	{
		// If we were just looked at.
		if (HasBeenLookedAt && !PrevHasBeenLookedAt) {
			// Play the sound at the player.
			if (Game.IsServer)
				Sound.FromEntity( "creep_watch", cl.Pawn as Entity );
		}

		var targetAnim = "walk";
		if (GoingToGoal == 1) {
			targetAnim = "idle";
		}

		// Hacky solution but I'll take it because no time.
		if (CurrentSequence.Name != targetAnim || CurrentSequence.IsFinished) {
			PlaybackRate = 1f;
			DirectPlayback.Play( targetAnim );
			SetAnimation( targetAnim );
		}

		// Don't advance any further than this if client.
		if (Game.IsClient)
			return;

		var toLoc = All.OfType<NPCSpawn>().Where( x => x.Tags.Has( "creep_stare_end" ) ).FirstOrDefault();
		var finalLoc = All.OfType<NPCSpawn>().Where( x => x.Tags.Has( "creep_stare_end_2" ) ).FirstOrDefault();

		Vector3 goalPosition = GoingToGoal switch {
			0 => toLoc.Position,
			1 => toLoc.Position, // Same position to notify the player.
			_ => finalLoc.Position,
		};


		if (Steer == null) {
			Steer = new NavSteer {
				Target = goalPosition
			};
		}
		Steer.Target = goalPosition;
		// Steer.DebugDrawPath();
		Steer.Tick( Position, Velocity );

		// Get our input velocity based on position from the target position.
		Vector3 inputVelocity = default;
		if (!Steer.Output.Finished) {
			inputVelocity = Steer.Output.Direction.Normal;
			var speed = 200f;
			Velocity = Velocity.AddClamped( inputVelocity * Time.Delta * 500, speed );
		}

		// Snap to the ground.
		var ray = new Ray( Position + Vector3.Up * 10, Vector3.Down );
		var tr = Trace.Ray( ray, 32f )
			.Ignore( this )
			.WithoutTags("player")
			.Run();
		if (tr.Hit) {
			Position = Position.WithZ( tr.EndPosition.z );
		}

		if (Steer.Output.Finished) {
			// if (GoingToGoal == 0) {
			// 	TimeSinceReachedGoal = 0;
			// 	GoingToGoal = 1;
			// }
			switch (GoingToGoal) {
				case 0:
					TimeSinceReachedGoal = 0;
					GoingToGoal = 1;
					break;
				case 1:
					if (TimeSinceReachedGoal > 10 && HasBeenLookedAt) {
						GoingToGoal = 2;
					}
					break;
				default:
					Finished = true;
					break;
			}
		} else {
			Position += inputVelocity.WithZ(0) * 1.15f;
		}

		var lookAtTarget = Steer.Target;
		if (GoingToGoal == 1) {
			lookAtTarget = cl.Pawn.Position;
		}
		// Turn to the target position.
		Rotation = Rotation.LookAt( lookAtTarget - Position, Vector3.Up );
	}

	public void TrashBagsSimulate(IClient cl)
	{
		// If we were just looked at.
		if (HasBeenLookedAt && !PrevHasBeenLookedAt) {
			// Play the sound at the player.
			if (Game.IsServer)
				Sound.FromEntity( "creep-trash-short", cl.Pawn as Entity );
		}

		if (HasBeenLookedAt && TimeSinceLookedAt > 9.0f) {
			// Hacky solution but I'll take it because no time.
			if (CurrentSequence.Name != "walk" || CurrentSequence.IsFinished) {
				DirectPlayback.Play( "walk" );
				SetAnimation( "walk" );
			}

			if (Game.IsClient)
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
