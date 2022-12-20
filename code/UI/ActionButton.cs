using System.Linq;
using Sandbox;
using Sandbox.Component;
using Sandbox.UI;
using JustAnotherNight.Player;
using JustAnotherNight.Entities;

namespace JustAnotherNight.UI;

public class ActionButton : WorldPanel
{
	Panel Image;

	public Entity Prop { get; set; }

	bool Focused = false;

	public ActionButton()
	{
		StyleSheet.Load( "/Resource/Styles/actionbutton.scss" );

		var imgParent = AddChild<Panel>( "image-parent" );

		Image = imgParent.AddChild<Panel>( "image" );
		Image.Style.BackgroundImage = Input.GetGlyph( InputButton.PrimaryAttack, InputGlyphSize.Small );

		imgParent.AddEventListener( "onmouseover", () => {
			// Log.Info( "mouseover" );
			Focused = true;
		} );
		imgParent.AddEventListener( "onmouseout", () => {
			// Log.Info( "mouseoff" );
			Focused = false;
		} );
	}

	public override void Tick()
	{
		base.Tick();

		var player = Game.LocalPawn as JustAnotherPlayer;

		var lookAt = Camera.Position - Position;
		Rotation = Rotation.LookAt( lookAt, Vector3.Up );

		Image.Style.BackgroundImage = Input.GetGlyph( InputButton.PrimaryAttack, InputGlyphSize.Small );

		var ray = new Ray(
			player.EyePosition, player.EyeRotation.Forward
		);
		// var ray = new Ray(
		// 	player.EyePosition, Prop.Position - player.EyePosition
		// );
		// var tr = Trace.Ray( ray, 500f )
		// 	.WithTag("spill")
		// 	.Run();
		var tr = Trace.Ray( ray, 500f )
			.Run();

		// DebugOverlay.Sphere( tr.EndPosition, 75f, Color.Green );
		tr = Trace.Sphere( 50f, tr.EndPosition, tr.EndPosition )
			// .WithTag("spill")
			.WithAllTags(Prop.Tags.List.ToArray())
			.Run();

		var usableProp = Prop as IUse;

		// Get the distance from our assigned spill.
		float distance = Prop.Position.Distance( player.Position );
		// Only show when a spill was hit and the distance is less than 150 units.
		bool enabled = tr.Entity != null && distance < 100 && usableProp.IsUsable(player);

		// Set enabled class based on trace result and distance.
		SetClass( "enabled", enabled );

		// By default set use to false.
		SetClass( "use", false );

		if (Prop is IEnablerDisabler disabler)
			disabler.Disable();

		// By default glow width is zero (don't show.)
		var glowWidth = 0f;
		// If this entity is enabled then set the width based on distance.
		if (enabled) {
			glowWidth = distance.LerpInverse( 75, 0 ) * 0.75f;
		}

		// If our prop has a glow effect then adjust its width.
		if ( Prop.Components.TryGet( out Glow glow ) )
			glow.Width = glowWidth;

		if (Focused && tr.Entity != null && distance < 75) {
			if (Input.Down(InputButton.PrimaryAttack)) {
				// Prop.Use();
				// var usable = Prop as IUse;
				usableProp.OnUse(player);

				if (Prop is IEnablerDisabler enabler)
					enabler.Enable();

				SetClass( "use", true );
			} else {
				SetClass( "use", false );
			}
		}
	}
}
