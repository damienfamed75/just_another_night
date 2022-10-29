using Sandbox;
using Sandbox.UI;

public class SelfSpeak : Panel
{
	public static SelfSpeak Current { get; set; }

	Label Dialogue;
	RealTimeUntil TimeUntilDisappear { get; set; }


	public SelfSpeak()
	{
		Current = this;

		Dialogue = AddChild<Label>( "dialogue" );
	}

	public override void Tick()
	{
		base.Tick();

		SetClass( "active", TimeUntilDisappear > 0 );
	}

	public void SayCustomerWaiting()
	{
		Say( "maybe I should take care of the customer first." );
	}

	public void SayCustomerArrive()
	{
		string message = Rand.Int( 0, 4 ) switch
		{
			0 => "hoorah, another customer...",
			1 => "great, a customer is here...",
			2 => "who's eating this late a night",
			_ => "guess there's someone at the window.",
		};

		Say( message );
	}

	public void SayTaskComplete()
	{
		string message = Rand.Int( 0, 4 ) switch
		{
			0 => "that's a task done. better check the board for more.",
			_ => "alright, let's check the board for what's next.",
		};

		Say( message );
	}

	private void Say(string message)
	{
		Dialogue.SetText( message );
		TimeUntilDisappear = 5;
	}
}