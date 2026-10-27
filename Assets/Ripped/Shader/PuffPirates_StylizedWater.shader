Shader "PuffPirates/StylizedWater" {
	Properties {
		_ShallowColor ("Shallow Color", Vector) = (0.08,0.72,0.88,0.92)
		_DeepColor ("Deep Color", Vector) = (0.015,0.16,0.42,0.96)
		_FoamColor ("Foam Color", Vector) = (0.72,0.96,1,1)
		_WaveScale ("Wave Scale", Range(0.05, 2)) = 0.32
		_WaveSpeed ("Wave Speed", Range(0, 5)) = 1.15
		_WaveHeight ("Wave Height", Range(0, 0.3)) = 0.055
		_FoamAmount ("Foam Amount", Range(0, 1)) = 0.34
		_Shimmer ("Shimmer", Range(0, 2)) = 0.65
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