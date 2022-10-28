using Sandbox;
using Sandbox.UI;

public class Intro : Panel
{
	Label IntroLabel;

	RealTimeSince TimeSinceCreated;

	public Intro()
	{
		IntroLabel = AddChild<Label>("intro-label");
		IntroLabel.SetText( "It's just another late night shift..." );

		TimeSinceCreated = 0;
	}

	public override void Tick()
	{
		base.Tick();

		if (TimeSinceCreated > 5) {
			Delete();
		}
	}
}