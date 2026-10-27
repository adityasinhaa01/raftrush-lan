Shader "Custom/SkyStylized" {
	Properties {
		[Header(Features)] [Toggle] _EnableGradient ("Enable Gradient", Float) = 1
		[Toggle] _EnableClouds ("Enable Clouds", Float) = 1
		[Toggle] _EnableSun ("Enable Sun", Float) = 1
		[Header(Sky Colors)] _SkyColor ("Sky Color", Vector) = (0.5,0.7,1,1)
		[AllIn13DShaderGradientDrawer] _GradientRamp ("Gradient Ramp", 2D) = "white" {}
		_RampTiling ("Ramp Tiling", Range(0.1, 4)) = 1
		_HorizonOffset ("Horizon Offset", Range(-0.5, 0.5)) = 0.15
		[Header(Cloud Shape)] _CloudColor ("Cloud Color", Vector) = (1,1,1,1)
		_CloudThreshold ("Cloud Cutoff", Range(0, 1)) = 0.5
		_CloudContrast ("Cloud Contrast", Range(1, 10)) = 2
		_CloudDensity ("Cloud Coverage", Range(0, 1)) = 0.5
		[Header(Cloud Movement)] _CloudSpeed ("Cloud Speed", Vector) = (0.1,0,0.05,0)
		_WindStretch ("Wind Stretch", Range(0, 2)) = 0.5
		[Header(Cloud Pattern)] _CloudScale1 ("Large Pattern Scale", Range(1, 20)) = 8
		_CloudScale2 ("Detail Scale", Range(1, 20)) = 12
		_WarpStrength ("Pattern Distortion", Range(0, 1)) = 0.3
		_CloudHeight ("Cloud Height", Range(0, 1)) = 0.3
		_CloudThickness ("Cloud Thickness", Range(0, 1)) = 0.1
		[Header(Sun)] _SunColor ("Sun Color", Vector) = (1,0.54,0.17,1)
		_SunSize ("Sun Size", Range(0.0001, 1)) = 0.05
		[Header(Sun Glow)] _GlowColor ("Glow Color", Vector) = (1,0.95,0.8,1)
		_GlowSize ("Glow Size", Range(1, 10)) = 4
		_GlowPower ("Glow Falloff", Range(1, 10)) = 3
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType" = "Opaque" }
		LOD 200

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4x4 unity_ObjectToWorld;
			float4x4 unity_MatrixVP;

			struct Vertex_Stage_Input
			{
				float4 pos : POSITION;
			};

			struct Vertex_Stage_Output
			{
				float4 pos : SV_POSITION;
			};

			Vertex_Stage_Output vert(Vertex_Stage_Input input)
			{
				Vertex_Stage_Output output;
				output.pos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.pos));
				return output;
			}

			float4 frag(Vertex_Stage_Output input) : SV_TARGET
			{
				return float4(1.0, 1.0, 1.0, 1.0); // RGBA
			}

			ENDHLSL
		}
	}
}