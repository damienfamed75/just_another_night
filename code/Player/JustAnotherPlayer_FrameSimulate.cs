using System.Linq;
using Sandbox;
using JustAnotherNight.Entities;

namespace JustAnotherNight.Player;

public partial class JustAnotherPlayer
{
	public override void FrameSimulate(IClient cl)
	{
		if (Incapacitated) {
			var creep = Entity.All.OfType<Creep>().First();
			var offset = new Vector3( 0, 0, 62 );

			Camera.Position = DeathCameraPos;
			Camera.FieldOfView = Screen.CreateVerticalFieldOfView( Game.Preferences.FieldOfView );

			if (TimeSinceIncapacitated > 1) {
				if (Camera.Rotation.Pitch() > -80)
					Camera.Rotation = Camera.Rotation
						.Angles()
						.WithPitch(	
							(Camera.Rotation.Pitch() - TimeSinceIncapacitated * Time.Delta * 3)
								.Clamp( -90, 90 )
							)
						.ToRotation();
			} else {
				Camera.Rotation = Rotation.LookAt( creep.Position + offset - Camera.Position );
			}

			return;
		}

		Camera.Rotation = ViewAngles.ToRotation();
		Camera.Position = EyePosition;
		Camera.FieldOfView = Screen.CreateVerticalFieldOfView( Game.Preferences.FieldOfView );
		Camera.FirstPersonViewer = this;
		Camera.Main.SetViewModelCamera( Camera.FieldOfView );
	}
}
