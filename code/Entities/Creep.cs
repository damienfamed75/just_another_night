using System.Linq;
using System.Threading.Tasks;
using Sandbox;

public partial class Creep : AnimatedMapEntity
{
	[Net]
	public CreepStates State { get; set; }

	[Net]
	public Vector3 SpawnLocation { get; set; }

	[Net]
	public bool PrevHasBeenLookedAt { get; set; } = false;

	[Net]
	public bool HasBeenLookedAt { get; set; } = false;

	public bool BeingLookedAt => (Game.Current as JustAnotherGame).LookingAtCreep;

	public RealTimeSince TimeSinceLookedAt => (Game.Current as JustAnotherGame).TimeSinceStare;

	public enum CreepStates
	{
		TakeOutTrashBags,
		WatchOutsideBuilding
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
			default:
				break;
		}

		PrevHasBeenLookedAt = HasBeenLookedAt;
		// Rotation = Rotation.LookAt( cl.Pawn.Position - Position, Vector3.Up );
	}

	public void TrashBagsSimulate(Client cl)
	{
		// If we were just looked at.
		if (HasBeenLookedAt && !PrevHasBeenLookedAt) {
			// Play the sound at the player.
			if (IsServer)
				Sound.FromEntity( "creep-trash", cl.Pawn );
		}

		if (HasBeenLookedAt && TimeSinceLookedAt > 10.0f) {
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

			// Delete self if at the goal location.
			// if (Position.AlmostEqual(goal.Position)) {
			// 	Delete();
			// }
		}
	}

	public void LookedAt()
	{
		HasBeenLookedAt = true;
	}
}