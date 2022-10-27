using System.Linq;
using Sandbox;

public class DeathCamera : CameraMode
{
	public RealTimeSince TimeSinceCreated { get; set; }

	public DeathCamera() : base()
	{
		TimeSinceCreated = 0;
	}

	public override void Build( ref CameraSetup camSetup )
	{
		base.Build( ref camSetup );
		FieldOfView = 70;
	}

	public override void Update()
	{
		var Pawn = Local.Pawn;

		var creep = Entity.All.OfType<Creep>().First();
		var offset = new Vector3( 0, 0, 62 );

		// Position = Pawn.EyePosition;
		Position = (Pawn as JustAnotherPlayer).DeathCameraPos;

		// FieldOfView = 65;
		FieldOfView = (FieldOfView - TimeSinceCreated * 2f).Clamp(60, 90);

		if (TimeSinceCreated > 1) {
			if (Rotation.Pitch() > -80)
				Rotation = Rotation.Angles().WithPitch( (Rotation.Pitch() - TimeSinceCreated * Time.Delta * 3).Clamp(-90, 90) ).ToRotation();


			// Log.Info( $"{Rotation.Pitch()}" );

			// if (Rotation.Pitch)

		} else {
			Rotation = Rotation.LookAt( creep.Position + offset - Position );
		}
	}
}