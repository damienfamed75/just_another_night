
HEADER
{
	Description = "";
}

FEATURES
{
	#include "vr_common_features.fxc"
	Feature( F_ADDITIVE_BLEND, 0..1, "Blending" );
	Feature( F_METALNESS, 0..1, "Metalness");
}

COMMON
{
	#ifndef S_ALPHA_TEST
	#define S_ALPHA_TEST 0
	#endif
	#ifndef S_TRANSLUCENT
	#define S_TRANSLUCENT 0
	#endif

	#include "common/shared.hlsl"

	#define S_UV2 1
	#define CUSTOM_MATERIAL_INPUTS
}

struct VertexInput
{
	#include "common/vertexinput.hlsl"
};

struct PixelInput
{
	#include "common/pixelinput.hlsl"
	float3 vPositionOs : TEXCOORD14;
};

VS
{
	#include "common/vertex.hlsl"

    int g_RoundToDecimalPlace< Range(0, 10); Default(0); UiGroup( "Go Back Settings,10/10" ); >;
    float g_flSnapScale< Range(1.0f, 5.0f); Default(2.0f); UiGroup( "Go Back Settings,10/10" ); >;
	float g_flAffineAmount< Range(0.0f, 5.0f); Default(1.0f); UiGroup( "Go Back Settings,10/10" ); >;

	PixelInput MainVs( VertexInput i )
	{
		PixelInput o = ProcessVertex( i );

		// Vertex snapping
		float flRound = pow(10, g_RoundToDecimalPlace) * g_flSnapScale;
		o.vPositionPs.xyz = round(o.vPositionPs.xyz * flRound) / flRound;

		// Affine texture mapping
		o.vTextureCoords *= lerp(1.0f, o.vPositionPs.w, g_flAffineAmount * 0.005f);
		// o.vTextureCoords *= o.vPositionPs.w;

		return FinalizeVertex( o );
	}
}

PS
{
	// #include "sbox_pixel.fxc"
	// #include "common/pixel.material.structs.hlsl"
	// #include "common/pixel.lighting.hlsl"
	// #include "common/pixel.shading.hlsl"
	// #include "common/pixel.material.helpers.hlsl"
	// #include "common/pixel.color.blending.hlsl"
	// #include "common/proceedural.hlsl"

	#include "common/pixel.hlsl"
	#include "procedural.hlsl"
	#include "blendmodes.hlsl"

	// #include "blendmodes.hlsl"
	// #include "common/pixel.hlsl"
	// #include "procedural.hlsl"
	
	// RenderState( DepthEnable, true );
	// RenderState( DepthWriteEnable, true );
	// RenderState( DepthFunc, ALWAYS );
	// RenderState( FillMode, SOLID );
	// RenderState( AlphaTestEnable, false );

	SamplerState g_sSampler0 <
	 Filter( POINT ); AddressU( WRAP ); AddressV( WRAP ); >;CreateInputTexture2D( Texture, Srgb, 8,
	 "None", "_color", ",0/,0/0", Default4( 1.00, 1.00, 1.00, 1.00 ) );Texture2D g_tTexture < Channel( RGBA, Box( Texture ), Srgb );
	 OutputFormat( DXT5 ); SrgbRead( True ); >;bool g_bCameraMoveJiggle < UiGroup( ",0/,0/0" ); Default( 1 ); >;
	bool g_bObjectMoveJiggle < UiGroup( ",0/,0/0" ); Default( 1 ); >;
	float g_flVertexJiggle < UiGroup( ",0/,0/0" ); Default1( 0.0128339715 ); Range1( 0, 1 ); >;
	float g_flAffineAmount< Range(0.0f, 5.0f); Default(1.0f); UiGroup( "Go Back Settings,10/10" ); >;

	// bool g_bUseMetalnessTexture < UiGroup( ",0/,0/0" ); Default( 0 ); >;
	// bool g_bUseMetalnessTexture < UiGroup( "Metalness,10/,0/0" ); Default( 0 ); >;
	bool g_bUseMetalnessTexture < UiGroup( "Metal,10/,0/0" ); Default( 0 ); >;

	float4 MainPs( PixelInput i ) : SV_Target0
	{
		i.vTextureCoords.xy /= lerp(1.0f, i.vPositionSs.w, g_flAffineAmount * 0.005f);
		// i.vTextureCoords.xy /= lerp(1.0f, i.vPositionSs.w, g_flAffineAmount * 0.005f);

		Material m;
		m.Albedo = float3( 1, 1, 1 );
		m.Normal = TransformNormal( i, float3( 0, 0, 1 ) );
		m.Roughness = 1;
		m.Metalness = 0;
		m.AmbientOcclusion = 0;
		m.TintMask = 1;
		m.Opacity = 1;
		m.Emission = float3( 0, 0, 0 );
		m.Transmission = 0;

		float4 local0 = Tex2DS( g_tTexture, g_sSampler0, i.vTextureCoords.xy );

		float local1 = g_bUseMetalnessTexture ? 0 : 0;
		// float local1 = 0;
		// if (g_bUseMetalnessTexture) {
		// }
		// float4 local0 = Tex2DS( g_tTexture, g_sSampler0, i.vTextureCoords.xy );

		m.Albedo = local0.xyz;
		m.Opacity = 1;
		m.Roughness = 1;
		// m.Metalness = 0;
		m.Metalness = local1;
		m.AmbientOcclusion = 0;

		m.AmbientOcclusion = saturate( m.AmbientOcclusion );
		m.Roughness = saturate( m.Roughness );
		m.Metalness = saturate( m.Metalness );
		m.Opacity = saturate( m.Opacity );

		// ShadingModelValveStandard sm;
		// return FinalizePixelMaterial( i, m, sm );
		return ShadingModelStandard::Shade( i, m );
		// return FinalizePixelMaterial( i, m );
	}
}
