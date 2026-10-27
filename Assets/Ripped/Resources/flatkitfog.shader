Shader "Hidden/FlatKit/FogFilter" {
	Properties {
		[Toggle(USE_DISTANCE_FOG)] _UseDistanceFog ("Use Distance", Float) = 0
		[Toggle(USE_DISTANCE_FOG_ON_SKY)] _UseDistanceFogOnSky ("Use Distance Fog On Sky", Float) = 0
		[Space] _Near ("Near", Float) = 0
		_Far ("Far", Float) = 100
		[Space] _DistanceFogIntensity ("Distance Fog Intensity", Range(0, 1)) = 1
		[Space(25)] [Toggle(USE_HEGHT_FOG)] _UseHeightFog ("Use Height", Float) = 0
		[Toggle(USE_HEGHT_FOG_ON_SKY)] _UseHeightFogOnSky ("Use Height Fog On Sky", Float) = 0
		[Space] _LowWorldY ("Low", Float) = 0
		_HighWorldY ("High", Float) = 10
		[Space] _HeightFogIntensity ("Height Fog Intensity", Range(0, 1)) = 1
		[Space(25)] _DistanceHeightBlend ("Distance / Height blend", Range(0, 1)) = 0.5
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
	Fallback "Diffuse"
}