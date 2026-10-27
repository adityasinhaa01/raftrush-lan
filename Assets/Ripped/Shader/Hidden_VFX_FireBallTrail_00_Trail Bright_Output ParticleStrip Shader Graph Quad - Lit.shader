Shader "Hidden/VFX/FireBallTrail_00/Trail Bright/Output ParticleStrip Shader Graph Quad - Lit" {
	Properties {
		[HDR] _Trail_Color ("Trail Color", Vector) = (1,1,1,0)
		[NoScaleOffset] _Trail_Texture ("Trail Texture", 2D) = "white" {}
		_Trail_Texture_Y_Offset ("Trail Texture Y Offset", Float) = 0
		[NoScaleOffset] _Noise_Texture ("Noise Texture", 2D) = "white" {}
		_Contrast ("Contrast", Float) = 1
		_Intensity ("Intensity", Float) = 1
		[HDR] _Emission_Color ("Emission Color", Vector) = (1,1,1,0)
		_Opacity_Contrast ("Opacity Contrast", Float) = 1
		_Alpha ("Alpha", Float) = 1
		_Noise_Power ("Noise Power", Float) = 2.71
		_Trail_Speed ("Trail Speed", Float) = 1
		_Noise_Speed ("Noise Speed", Vector) = (-1,1,0,0)
		_Noise_Scale ("Noise Scale", Float) = 10
		[NoScaleOffset] _SampleTexture2D_0c5f30aadcec413c9c40b475993e25e1_Texture_1_Texture2D ("Texture2D", 2D) = "white" {}
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