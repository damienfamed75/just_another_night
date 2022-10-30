using Sandbox;
using Sandbox.UI;

public class Intro : Panel
{
	Label IntroLabel;

	RealTimeSince TimeSinceCreated;

	public Intro()
	{
		IntroLabel = AddChild<Label>("intro-label");
		IntroLabel.SetText(
@"Time to finish the tasks on the board before closing up."
		);

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