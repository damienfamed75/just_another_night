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

		// Log.Info( player.TimeUntilDeath );
		// SetClass( "active", player.TimeUntilDeath < 0 );
		// if (player.TimeUntilDeath < 0) {
		// }

		// player.TimeUntilDeath
	}
}