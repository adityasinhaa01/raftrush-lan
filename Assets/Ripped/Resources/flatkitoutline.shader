Shader "Hidden/FlatKit/OutlineFilter" {
	Properties {
		[HideInInspector] _BaseMap ("Base (RGB)", 2D) = "white" {}
		_EdgeColor ("Outline Color", Vector) = (1,1,1,1)
		_Thickness ("Thickness", Range(0, 5)) = 1
		[Space(15)] [Toggle(OUTLINE_USE_DEPTH)] _UseDepth ("Use Depth", Float) = 1
		_DepthThresholdMin ("Min Depth Threshold", Range(0, 1)) = 0
		_DepthThresholdMax ("Max Depth Threshold", Range(0, 1)) = 0.25
		[Space(15)] [Toggle(OUTLINE_USE_NORMALS)] _UseNormals ("Use Normals", Float) = 0
		_NormalThresholdMin ("Min Normal Threshold", Range(0, 1)) = 0.5
		_NormalThresholdMax ("Max Normal Threshold", Range(0, 1)) = 1
		[Space(15)] [Toggle(OUTLINE_USE_COLOR)] _UseColor ("Use Color", Float) = 0
		_ColorThresholdMin ("Min Color Threshold", Range(0, 1)) = 0
		_ColorThresholdMax ("Max Color Threshold", Range(0, 1)) = 0.25
		[Space(15)] [Toggle(OUTLINE_ONLY)] _OutlineOnly ("Outline Only", Float) = 0
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