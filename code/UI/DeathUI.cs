using Sandbox;
using Sandbox.UI;

public class DeathUI : Panel
{
	Panel GameLogo;

	Label Credits;

	public float Opacity { get; set; }

	public DeathUI()
	{
		GameLogo = AddChild<Panel>("game-logo");
		GameLogo.Style.BackgroundImage = Texture.Load( FileSystem.Mounted, "images/logo.png" );

		Credits = AddChild<Label>( "credits" );
		Credits.SetText(
@"created by:
Damien#0812

sounds sampled from:
Breviceps
Fawfulgrox
ERH
Nicolas Drweski
InspectorJ
LennyBoy
RiverNile
Nachtmahrtv
YFJesse"
);

		Style.Opacity = 0f;
	}

	public override void Tick()
	{
		base.Tick();

		var player = Local.Pawn as JustAnotherPlayer;

		if (!player.Incapacitated)
			return;

		Opacity = player.TimeUntilDeath.Relative.LerpInverse( 8, 4 );
		Style.Opacity = Opacity;

		GameLogo.SetClass( "active", player.TimeUntilDeath < -1.25f );

		Credits.SetClass( "scroll", player.TimeUntilDeath < -2.25f );
		GameLogo.SetClass( "fadeout", player.TimeUntilDeath < -2.5f );

		// Log.Info( player.TimeUntilDeath );
		// SetClass( "active", player.TimeUntilDeath < 0 );
		// if (player.TimeUntilDeath < 0) {
		// }

		// player.TimeUntilDeath
	}
}