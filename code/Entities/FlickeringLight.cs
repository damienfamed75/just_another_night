using System.Collections.Generic;
using Sandbox;
using Editor;
using System;

[HammerEntity]
[CanBeClientsideOnly]
[Category( "Lighting" )]
[Title("Flickering Light")]
[EditorModel( "models/editor/omni", "rgb(0, 255, 192)", "rgb(255, 64, 64)" )]
public partial class FlickeringLight : Entity
{
	[Net]
	[Property]
	public PointLightEntity Light { get; set; }

	public RealTimeUntil TimeUntilFlicker { get; set; }

	float lastSum = 0;
	public int MaxSmoothingSteps = 5;
	public Queue<float> SmoothingQueue { get; set; }

	public FlickeringLight() : base()
	{
		SmoothingQueue = new Queue<float>( MaxSmoothingSteps );

		Event.Register( this );
	}

	public override void Spawn()
	{
		base.Spawn();

		Light = new PointLightEntity{
			Brightness = 0.6f,
			Range = 300f,
			Color = Color.FromRgb( 0xe7faea ),
			Enabled = true,
			EnableDrawing = true,
			Position = Position,
			Rotation = Rotation
		};
	}

	[Event.Tick]
	public void Tick()
	{
		if (Game.IsServer)
			return;

		var rnd = new Random();
		// Only call sometimes so it's not too fast and crazy.
		if (rnd.Int(0,10) < 8)
			return;

		// Subtract from the last sum when there's too many steps in the queue.
		while (SmoothingQueue.Count >= MaxSmoothingSteps) {
			lastSum -= SmoothingQueue.Dequeue();
		}
		// Create a new random value to our light values.
		float newVal = rnd.Float( 0.0f, 1.0f );
		SmoothingQueue.Enqueue( newVal );
		lastSum += newVal;
		// Set the light's brightness.
		Light.SetLightBrightness( lastSum / SmoothingQueue.Count );
	}
}
