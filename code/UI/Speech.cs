using Sandbox;
using Sandbox.UI;

public class Speech : WorldPanel
{
	Panel DialogueParent;
	Label Dialogue;

	RealTimeUntil TimeUntilDeletion { get; set; }

	public Speech(string message)
	{
		StyleSheet.Load( "/Resource/Styles/speech.scss" );

		DialogueParent = AddChild<Panel>( "dialogue-parent" );
		Dialogue = DialogueParent.AddChild<Label>( "dialogue" );
		TimeUntilDeletion = 3;

		Dialogue.SetText( message );
	}

	public override void Tick()
	{
		base.Tick();

		var lookAt = Camera.Position - Position;
		Rotation = Rotation.LookAt( lookAt, Vector3.Up );

		if (TimeUntilDeletion < 0) {
			Delete();
		}
	}
}
