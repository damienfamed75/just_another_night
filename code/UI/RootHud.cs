using Sandbox.UI;

public class RootHud : RootPanel
{
	public static RootHud Current { get; private set; }

	public RootHud()
	{
		Current = this;

		SetTemplate( "/Resource/Templates/hud.html" );
		StyleSheet.Load( "/Resource/Styles/hud.scss" );
	}
}