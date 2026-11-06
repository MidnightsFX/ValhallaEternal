// Upgrade NOTE: upgraded instancing buffer 'MFX_AMP_Elemental' to new syntax.

// Made with Amplify Shader Editor v1.9.8.1
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "MFX_AMP_Elemental"
{
	Properties
	{
		_Cutoff( "Mask Clip Value", Float ) = 0.5
		_EffectInternalMovement("Effect Internal Movement", Range( -1 , 1)) = -0.1569773
		_EffectColor("Effect Color", Color) = (0.1221449,0.5754717,0.0787202,0)
		_EmissiveIntensity("Emissive Intensity", Range( 0 , 5)) = 2.039391
		_EffectTexture("Effect Texture", 2D) = "white" {}
		_MaskMovement("Mask Movement", Range( -1 , 1)) = 0.1500823
		_StrengthofMaskMax("Strength of Mask Max", Range( 0 , 30)) = 7
		_RuneSymbols("RuneSymbols", 2D) = "white" {}
		_Noise("Noise", 2D) = "white" {}
		_Pixelation("Pixelation", Float) = 64
		_Rotation_Speed("Rotation_Speed", Range( -1 , 1)) = 0.2
		_StrengthofMaskMin("Strength of Mask Min", Float) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "TransparentCutout"  "Queue" = "AlphaTest+0" "IsEmissive" = "true"  }
		Cull Off
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.5
		#pragma multi_compile_instancing
		#define ASE_VERSION 19801
		#pragma only_renderers d3d11 metal vulkan 
		#pragma surface surf Standard keepalpha 
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform float _EmissiveIntensity;
		uniform sampler2D _EffectTexture;
		uniform float _EffectInternalMovement;
		uniform sampler2D _Noise;
		uniform float _Rotation_Speed;
		uniform float _StrengthofMaskMin;
		uniform float _StrengthofMaskMax;
		uniform sampler2D _RuneSymbols;
		uniform float _MaskMovement;
		uniform float _Pixelation;
		uniform float _Cutoff = 0.5;

		UNITY_INSTANCING_BUFFER_START(MFX_AMP_Elemental)
			UNITY_DEFINE_INSTANCED_PROP(float4, _EffectColor)
#define _EffectColor_arr MFX_AMP_Elemental
		UNITY_INSTANCING_BUFFER_END(MFX_AMP_Elemental)

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float4 _EffectColor_Instance = UNITY_ACCESS_INSTANCED_PROP(_EffectColor_arr, _EffectColor);
			float2 panner5 = ( 1.0 * _Time.y * float2( 0,0 ) + i.uv_texcoord);
			float mulTime6 = _Time.y * _EffectInternalMovement;
			float cos46 = cos( mulTime6 );
			float sin46 = sin( mulTime6 );
			float2 rotator46 = mul( panner5 - float2( 0,0 ) , float2x2( cos46 , -sin46 , sin46 , cos46 )) + float2( 0,0 );
			o.Emission = ( ( _EffectColor_Instance * _EmissiveIntensity ) * ( tex2D( _EffectTexture, rotator46 ).g * 0.5 ) ).rgb;
			o.Alpha = 1;
			float mulTime71 = _Time.y * _Rotation_Speed;
			float cos44 = cos( ( mulTime71 * -1.0 ) );
			float sin44 = sin( ( mulTime71 * -1.0 ) );
			float2 rotator44 = mul( i.uv_texcoord - float2( 0,0 ) , float2x2( cos44 , -sin44 , sin44 , cos44 )) + float2( 0,0 );
			float2 panner66 = ( -0.5 * _Time.y * float2( 0,0 ) + rotator44);
			float2 panner51 = ( 0.5 * _Time.y * float2( 0,0 ) + i.uv_texcoord);
			float cos68 = cos( mulTime71 );
			float sin68 = sin( mulTime71 );
			float2 rotator68 = mul( panner51 - float2( 0,0 ) , float2x2( cos68 , -sin68 , sin68 , cos68 )) + float2( 0,0 );
			float4 lerpResult70 = lerp( tex2D( _Noise, panner66 ) , tex2D( _Noise, rotator68 ) , float4( 0,0,0,0 ));
			float dotResult4_g3 = dot( float2( 1.11,1.24 ) , float2( 12.9898,78.233 ) );
			float lerpResult10_g3 = lerp( _StrengthofMaskMin , _StrengthofMaskMax , frac( ( sin( dotResult4_g3 ) * 43758.55 ) ));
			float2 temp_cast_1 = (_MaskMovement).xx;
			float2 temp_cast_2 = (2.0).xx;
			float2 temp_cast_3 = (0.0).xx;
			float2 uv_TexCoord45 = i.uv_texcoord * temp_cast_2 + temp_cast_3;
			float cos52 = cos( mulTime6 );
			float sin52 = sin( mulTime6 );
			float2 rotator52 = mul( uv_TexCoord45 - float2( 0,0 ) , float2x2( cos52 , -sin52 , sin52 , cos52 )) + float2( 0,0 );
			float2 panner27 = ( 1.0 * _Time.y * temp_cast_1 + rotator52);
			float pixelWidth63 =  1.0f / _Pixelation;
			float pixelHeight63 = 1.0f / _Pixelation;
			half2 pixelateduv63 = half2((int)(panner27.x / pixelWidth63) * pixelWidth63, (int)(panner27.y / pixelHeight63) * pixelHeight63);
			clip( ( lerpResult70 * lerpResult10_g3 * tex2D( _RuneSymbols, pixelateduv63 ) ).r - _Cutoff );
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "AmplifyShaderEditor.MaterialInspector"
}
/*ASEBEGIN
Version=19801
Node;AmplifyShaderEditor.RangedFloatNode;73;-640,112;Inherit;False;Property;_Rotation_Speed;Rotation_Speed;10;0;Create;True;0;0;0;False;0;False;0.2;0.15;-1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;48;-336,464;Inherit;False;Constant;_Rune_Tiling;Rune_Tiling;8;0;Create;True;0;0;0;False;0;False;2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;49;-368,608;Inherit;False;Constant;_Float2;Float 2;8;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;71;-480,208;Inherit;False;1;0;FLOAT;0.2;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;7;-784,256;Inherit;False;Property;_EffectInternalMovement;Effect Internal Movement;1;0;Create;True;0;0;0;False;0;False;-0.1569773;-0.1;-1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;45;-112,464;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;8;-544,-80;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;72;-272,128;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;-1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;6;-480,304;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;5;64,-320;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RotatorNode;52;144,448;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0.3;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;51;-128,288;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;0.5;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;28;-240,688;Inherit;False;Property;_MaskMovement;Mask Movement;5;0;Create;True;0;0;0;False;0;False;0.1500823;0.01;-1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RotatorNode;44;-80,128;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0.3;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RotatorNode;46;352,-272;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;27;352,496;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;66;160,160;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;-0.5;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RotatorNode;68;160,288;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;-0.3;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;74;288,688;Inherit;False;Property;_Pixelation;Pixelation;9;0;Create;True;0;0;0;False;0;False;64;64;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;39;128,-32;Inherit;True;Property;_Noise;Noise;8;0;Create;True;0;0;0;False;0;False;21b9f4e0af3140a99ef0bc5a43d58a97;21b9f4e0af3140a99ef0bc5a43d58a97;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.ColorNode;3;1056,-416;Inherit;False;InstancedProperty;_EffectColor;Effect Color;2;0;Create;True;0;0;0;False;0;False;0.1221449,0.5754717,0.0787202,0;0.4235294,0,0.01176471,1;True;True;0;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.RangedFloatNode;11;1024,-160;Inherit;False;Property;_EmissiveIntensity;Emissive Intensity;3;0;Create;True;0;0;0;False;0;False;2.039391;3.9;0;5;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;55;752,16;Inherit;False;Constant;_Float1;Float 1;9;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;62;496,720;Inherit;False;Property;_StrengthofMaskMin;Strength of Mask Min;11;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;29;432,832;Inherit;False;Property;_StrengthofMaskMax;Strength of Mask Max;6;0;Create;True;0;0;0;False;0;False;7;7.055362;0;30;0;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCPixelate;63;528,528;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT;94;False;2;FLOAT;94;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;2;416,272;Inherit;True;Property;_MaskTexture;Mask Texture;2;0;Create;True;0;0;0;False;0;False;-1;21b9f4e0af3140a99ef0bc5a43d58a97;21b9f4e0af3140a99ef0bc5a43d58a97;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SamplerNode;65;432,64;Inherit;True;Property;_TextureSample0;Texture Sample 0;10;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SamplerNode;1;608,-256;Inherit;True;Property;_EffectTexture;Effect Texture;4;0;Create;True;0;0;0;False;0;False;-1;5d220876d64a6f3498562743f7b5a0ea;5d220876d64a6f3498562743f7b5a0ea;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;16;1344,-208;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;36;720,480;Inherit;True;Property;_RuneSymbols;RuneSymbols;7;0;Create;True;0;0;0;False;0;False;-1;8d30dc716943a184fb8df97d2fc5b9e2;8d30dc716943a184fb8df97d2fc5b9e2;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;6;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT3;5
Node;AmplifyShaderEditor.FunctionNode;58;816,720;Inherit;False;Random Range;-1;;3;7b754edb8aebbfb4a9ace907af661cfc;0;3;1;FLOAT2;1.11,1.24;False;2;FLOAT;0;False;3;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;54;992,-32;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;70;800,208;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;17;1230.548,123.2552;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;30;1056,336;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;25;1424,0;Float;False;True;-1;3;AmplifyShaderEditor.MaterialInspector;0;0;Standard;MFX_AMP_Elemental;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Off;0;False;;0;False;;False;0;False;;0;False;;False;0;Masked;0.5;True;False;0;False;TransparentCutout;;AlphaTest;All;3;d3d11;metal;vulkan;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;True;0;0;False;;0;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;0;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;17;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;16;FLOAT4;0,0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;71;0;73;0
WireConnection;45;0;48;0
WireConnection;45;1;49;0
WireConnection;72;0;71;0
WireConnection;6;0;7;0
WireConnection;5;0;8;0
WireConnection;52;0;45;0
WireConnection;52;2;6;0
WireConnection;51;0;8;0
WireConnection;44;0;8;0
WireConnection;44;2;72;0
WireConnection;46;0;5;0
WireConnection;46;2;6;0
WireConnection;27;0;52;0
WireConnection;27;2;28;0
WireConnection;66;0;44;0
WireConnection;68;0;51;0
WireConnection;68;2;71;0
WireConnection;63;0;27;0
WireConnection;63;1;74;0
WireConnection;63;2;74;0
WireConnection;2;0;39;0
WireConnection;2;1;68;0
WireConnection;65;0;39;0
WireConnection;65;1;66;0
WireConnection;1;1;46;0
WireConnection;16;0;3;0
WireConnection;16;1;11;0
WireConnection;36;1;63;0
WireConnection;58;2;62;0
WireConnection;58;3;29;0
WireConnection;54;0;1;2
WireConnection;54;1;55;0
WireConnection;70;0;65;0
WireConnection;70;1;2;0
WireConnection;17;0;16;0
WireConnection;17;1;54;0
WireConnection;30;0;70;0
WireConnection;30;1;58;0
WireConnection;30;2;36;0
WireConnection;25;2;17;0
WireConnection;25;10;30;0
ASEEND*/
//CHKSM=06B1A1B32597D32BCF4BF7574BA4CC6397252FB0