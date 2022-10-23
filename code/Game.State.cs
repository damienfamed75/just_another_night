using System;
using System.Threading.Tasks;
using Sandbox;

public partial class JustAnotherGame
{
	public static GameStates CurrentState => (Current as JustAnotherGame)?.GameState ?? GameStates.FirstCustomer;

	[Net]
	public GameStates GameState { get; set; } = GameStates.FirstCustomer;

	[Net]
	public RealTimeUntil StateTimer { get; set; } = 0f;

	public enum GameStates
	{
		FirstCustomer,
		FirstScare,
		CustomerComeBack,
		SecondScare,
	}

	private async Task GameLoopAsync()
	{
		GameState = GameStates.FirstCustomer;
		StateTimer = Rand.Float( 30.0f, 60.0f );
		await WaitStateTimer();

		// initiate first customer

		GameState = GameStates.FirstScare;
	}

	private async Task WaitStateTimer()
	{
		while (StateTimer > 0) {
			await Task.DelayRealtimeSeconds( 1.0f );
		}

		await Task.DelayRealtimeSeconds( 1.0f );
	}
}