
HEADER
{
	Description = "";
}

FEATURES
{
	#include "vr_common_features.fxc"
	Feature( F_ADDITIVE_BLEND, 0..1, "Blending" );
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

	PixelInput MainVs( VertexInput i )
	{
		PixelInput o = ProcessVertex( i );
		o.vPositionOs = i.vPositionOs.xyz;
		return FinalizeVertex( o );
	}
}

PS
{
	#include "sbox_pixel.fxc"
	#include "common/pixel.material.structs.hlsl"
	#include "common/pixel.lighting.hlsl"
	#include "common/pixel.shading.hlsl"
	#include "common/pixel.material.helpers.hlsl"
	#include "common/pixel.color.blending.hlsl"
	#include "common/proceedural.hlsl"
	
	SamplerState g_sSampler0 <
	 Filter( POINT ); AddressU( WRAP ); AddressV( WRAP ); >;CreateInputTexture2D( Texture, Srgb, 8,
	 "None", "_color", ",0/,0/0", Default4( 1.00, 1.00, 1.00, 1.00 ) );Texture2D g_tTexture < Channel( RGBA, Box( Texture ), Srgb );
	 OutputFormat( DXT5 ); SrgbRead( True ); >;bool g_bCameraMoveJiggle < UiGroup( ",0/,0/0" ); Default( 1 ); >;
	bool g_bObjectMoveJiggle < UiGroup( ",0/,0/0" ); Default( 1 ); >;
	float g_flVertexJiggle < UiGroup( ",0/,0/0" ); Default1( 0.0128339715 ); Range1( 0, 1 ); >;
	
	float4 MainPs( PixelInput i ) : SV_Target0
	{
		Material m;
		m.Albedo = float3( 1, 1, 1 );
		m.Normal = TransformNormal( i, float3( 0, 0, 1 ) );
		m.Roughness = 1;
		m.Metalness = 0;
		m.AmbientOcclusion = 1;
		m.TintMask = 1;
		m.Opacity = 1;
		m.Emission = float3( 0, 0, 0 );
		m.Transmission = 0;
		
		float2 local0 = i.vTextureCoords.xy * float2( 1, 1 );
		float3 local1 = float3( 0, 0, 0 );
		float2 local2 = i.vTextureCoords.xy * float2( 1, 1 );
		float2 local3 = i.vTextureCoords.xy * float2( 1, 1 );
		float2 local4 = float2( 1, 1 );
		float3 local5 = i.vPositionSs.xyz;
		float3 local6 = float3( 0, 0, 0 );
		float3 local7 = g_bCameraMoveJiggle ? local5 : local6;
		float3 local8 = i.vPositionWithOffsetWs.xyz + g_vHighPrecisionLightingOffsetWs.xyz;
		float3 local9 = float3( 0, 0, 0 );
		float3 local10 = g_bObjectMoveJiggle ? local8 : local9;
		float3 local11 = local7 + local10;
		float2 local12 = TileAndOffsetUv( local3, local4, local11.xy );
		float2 local13 = local12 * float2( 0.008, 0.008 );
		float local14 = Simplex2D( local13 );
		float2 local15 = local12 * float2( 0.01, 0.01 );
		float local16 = ValueNoise( local15 );
		float local17 = saturate( local16 );
		float local18 = i.vPositionSs.z;
		float local19 = saturate( lerp( local14, local17, local18 ) );
		float2 local20 = local2 * float2( local19, local19 );
		float local21 = g_flVertexJiggle;
		float2 local22 = local20 * float2( local21, local21 );
		float3 local23 = local7.x == local10.x ? local1 : float3( local22, 0 );
		float3 local24 = float3( local0, 0 ) + local23;
		float4 local25 = Tex2DS( g_tTexture, g_sSampler0, local24.xy );
		
		m.Albedo = local25.xyz;
		m.Opacity = 1;
		m.Roughness = 1;
		m.Metalness = 0;
		m.AmbientOcclusion = 1;
		
		m.AmbientOcclusion = saturate( m.AmbientOcclusion );
		m.Roughness = saturate( m.Roughness );
		m.Metalness = saturate( m.Metalness );
		m.Opacity = saturate( m.Opacity );
		
		ShadingModelValveStandard sm;
		return FinalizePixelMaterial( i, m, sm );
	}
}
