// Upgrade NOTE: upgraded instancing buffer 'MFX_AMP_FLOW' to new syntax.

// Made with Amplify Shader Editor v1.9.9.5
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "MFX_AMP_FLOW"
{
	Properties
	{
		_Cutoff( "Mask Clip Value", Float ) = 0.5
		_Movement( "Movement", Vector ) = ( 0.1, 0, 0, 0 )
		_Intensity( "Intensity", Float ) = 5
		_MaskIntensity( "Mask Intensity", Float ) = 1
		_Texture0( "Texture 0", 2D ) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Geometry+0" }
		Cull Off
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.5
		#pragma multi_compile_instancing
		#define ASE_VERSION 19905
		#pragma surface surf Standard keepalpha exclude_path:deferred 
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform sampler2D _Texture0;
		uniform float2 _Movement;
		uniform float _MaskIntensity;
		uniform float _Cutoff = 0.5;

		UNITY_INSTANCING_BUFFER_START(MFX_AMP_FLOW)
			UNITY_DEFINE_INSTANCED_PROP(float, _Intensity)
#define _Intensity_arr MFX_AMP_FLOW
		UNITY_INSTANCING_BUFFER_END(MFX_AMP_FLOW)


		struct Gradient
		{
			int type;
			int colorsLength;
			int alphasLength;
			float4 colors[8];
			float2 alphas[8];
		};


		Gradient NewGradient(int type, int colorsLength, int alphasLength, 
		float4 colors0, float4 colors1, float4 colors2, float4 colors3, float4 colors4, float4 colors5, float4 colors6, float4 colors7,
		float2 alphas0, float2 alphas1, float2 alphas2, float2 alphas3, float2 alphas4, float2 alphas5, float2 alphas6, float2 alphas7)
		{
			Gradient g;
			g.type = type;
			g.colorsLength = colorsLength;
			g.alphasLength = alphasLength;
			g.colors[ 0 ] = colors0;
			g.colors[ 1 ] = colors1;
			g.colors[ 2 ] = colors2;
			g.colors[ 3 ] = colors3;
			g.colors[ 4 ] = colors4;
			g.colors[ 5 ] = colors5;
			g.colors[ 6 ] = colors6;
			g.colors[ 7 ] = colors7;
			g.alphas[ 0 ] = alphas0;
			g.alphas[ 1 ] = alphas1;
			g.alphas[ 2 ] = alphas2;
			g.alphas[ 3 ] = alphas3;
			g.alphas[ 4 ] = alphas4;
			g.alphas[ 5 ] = alphas5;
			g.alphas[ 6 ] = alphas6;
			g.alphas[ 7 ] = alphas7;
			return g;
		}


		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float4 color10 = IsGammaSpace() ? float4( 0.1364862, 0.6603774, 0, 0.8 ) : float4( 0.01664654, 0.3936214, 0, 0.8 );
			Gradient gradient16 = NewGradient( 0, 2, 3, float4( 0.125534, 0.8584906, 0.1454094, 0 ), float4( 0.07929868, 0.6226415, 0.173052, 1 ), 0, 0, 0, 0, 0, 0, float2( 0, 0 ), float2( 0.5843138, 0.397055 ), float2( 0, 1 ), 0, 0, 0, 0, 0 );
			float4 lerpResult20 = lerp( color10 , 0 , float4( 0,0,0,0 ));
			float2 panner4 = ( 1.0 * _Time.y * _Movement + i.uv_texcoord);
			float4 tex2DNode5 = tex2D( _Texture0, panner4 );
			float _Intensity_Instance = UNITY_ACCESS_INSTANCED_PROP(_Intensity_arr, _Intensity);
			o.Albedo = ( lerpResult20 * ( tex2DNode5 * _Intensity_Instance ) ).rgb;
			o.Alpha = 1;
			clip( ( tex2DNode5.a * _MaskIntensity ) - _Cutoff );
		}

		ENDCG
	}
	Fallback Off
	CustomEditor "AmplifyShaderEditor.MaterialInspector"
}
/*ASEBEGIN
Version=19905
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;3;-816,-224;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;9;-800,-48;Inherit;False;Property;_Movement;Movement;1;0;Create;True;0;0;0;False;0;False;0.1,0;-0.1,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.PannerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;4;-576,-160;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TexturePropertyNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;21;-608,80;Inherit;True;Property;_Texture0;Texture 0;4;0;Create;True;0;0;0;False;0;False;None;7e91736c6f694984f9c03f52911c5e57;False;white;Auto;Texture2D;False;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;11;-192,144;Inherit;False;InstancedProperty;_Intensity;Intensity;2;0;Create;True;0;0;0;False;0;False;5;3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;10;-208,-400;Inherit;False;Constant;_EmissiveColor;Emissive Color;2;0;Create;True;0;0;0;False;0;False;0.1364862,0.6603774,0,0.8;0,0,0,0;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.GradientNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;16;-192,-192;Inherit;False;0;2;3;0.125534,0.8584906,0.1454094,0;0.07929868,0.6226415,0.173052,1;0,0;0.5843138,0.397055;0,1;0;1;OBJECT;0
Node;AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;5;-384,-80;Inherit;True;Property;_Texture;Texture;1;0;Create;True;0;0;0;False;0;False;-1;7e91736c6f694984f9c03f52911c5e57;7e91736c6f694984f9c03f52911c5e57;True;0;False;white;Auto;False;Instance;-1;Auto;Texture2D;False;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;13;-32,-80;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;15;-144,240;Inherit;False;Property;_MaskIntensity;Mask Intensity;3;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;20;32,-320;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;17;-464,320;Inherit;False;0;2;0;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;14;80,160;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;18;48,336;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT2;1,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;12;208,-256;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.VertexColorNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;22;-320,464;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StandardSurfaceOutputNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;1;400,-240;Float;False;True;-1;3;AmplifyShaderEditor.MaterialInspector;0;0;Standard;MFX_AMP_FLOW;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Off;0;False;;0;False;;False;0;False;;0;False;;False;0;Custom;0.5;True;False;0;True;Transparent;;Geometry;ForwardOnly;14;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;0;0;False;;0;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;0;1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;17;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;4;0;3;0
WireConnection;4;2;9;0
WireConnection;5;0;21;0
WireConnection;5;1;4;0
WireConnection;13;0;5;0
WireConnection;13;1;11;0
WireConnection;20;0;10;0
WireConnection;20;1;16;0
WireConnection;14;0;5;4
WireConnection;14;1;15;0
WireConnection;18;0;17;0
WireConnection;12;0;20;0
WireConnection;12;1;13;0
WireConnection;1;0;12;0
WireConnection;1;10;14;0
ASEEND*/
//CHKSM=4846F330DAEDB44BBD7BE70376D22BED413E08A4