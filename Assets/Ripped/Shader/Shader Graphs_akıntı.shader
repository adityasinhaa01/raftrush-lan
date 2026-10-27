Shader "Shader Graphs/akıntı" {
	Properties {
		[NoScaleOffset] _f_texture ("f texture", 2D) = "white" {}
		_f_color ("f color", Vector) = (0,0,0,1)
		_f_speed ("f speed", Vector) = (0,0,0,0)
		_f_opacity ("f opacity", Float) = 0
		_f_fade ("f fade", Float) = 0
		[NoScaleOffset] _Distortion_texture ("Distortion texture", 2D) = "white" {}
		_Distortion_tiling ("Distortion tiling", Float) = 0
		_Distortion_Speed ("Distortion Speed", Vector) = (0.05,0.03,0,0)
		_Distortion_Str ("Distortion Str", Float) = 0.03
		_tiling ("tiling", Float) = 0
		[HideInInspector] _QueueOffset ("_QueueOffset", Float) = 0
		[HideInInspector] _QueueControl ("_QueueControl", Float) = -1
		[HideInInspector] [NoScaleOffset] unity_Lightmaps ("unity_Lightmaps", 2DArray) = "" {}
		[HideInInspector] [NoScaleOffset] unity_LightmapsInd ("unity_LightmapsInd", 2DArray) = "" {}
		[HideInInspector] [NoScaleOffset] unity_ShadowMasks ("unity_ShadowMasks", 2DArray) = "" {}
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
	Fallback "Hidden/Shader Graph/FallbackError"
	//CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
}