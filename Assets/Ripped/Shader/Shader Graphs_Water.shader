Shader "Shader Graphs/Water" {
	Properties {
		_WaterDarkColor ("WaterDarkColor", Vector) = (0,0,0,1)
		_WaterBrightColor ("WaterBrightColor", Vector) = (0,0,0,1)
		_ShoreFoamColor ("ShoreFoamColor", Vector) = (0,0,0,1)
		_ShoreFoamSpeed ("ShoreFoamSpeed", Range(0, 0.1)) = 0
		_ShoreFoamAmount ("ShoreFoamAmount", Range(0, 1)) = 0
		_WaterFoamColor ("WaterFoamColor", Vector) = (0,0,0,1)
		_WaterFoamSpeed ("WaterFoamSpeed", Range(0, 0.4)) = 0
		_WaterFoamAmount ("WaterFoamAmount", Range(0, 1)) = 0
		_WaveIntensity ("WaveIntensity", Range(0, 1)) = 0
		_Emission ("Emission", Range(0, 1)) = 0
		_Scale ("Scale", Float) = 0
		[ToggleUI] _BasedOnWorldScale ("BasedOnWorldScale", Float) = 0
		[NoScaleOffset] _WaterDepthTexture ("WaterDepthTexture", 2D) = "white" {}
		[Toggle(_ENABLEWAVE)] _ENABLEWAVE ("EnableWave", Float) = 0
		[Toggle(_ENABLECAUSTICS)] _ENABLECAUSTICS ("EnableCaustics", Float) = 0
		[NoScaleOffset] _caustic_map ("caustic map", 2D) = "white" {}
		_causticpab ("causticpab", Float) = 0.1
		_causticscale ("causticscale", Float) = 1
		_caausticstrenght ("caausticstrenght", Float) = 1
		[NoScaleOffset] _caustic_distoriton_map ("caustic distoriton map", 2D) = "white" {}
		_causticdistortion_strenge ("causticdistortion strenge", Float) = 3
		_causticdistortion_scale ("causticdistortion scale", Float) = 2
		_caustic_color ("caustic color", Vector) = (0,0,0,0)
		[NoScaleOffset] _Surfoam_map ("Surfoam map", 2D) = "white" {}
		_Surfoam_Scale ("Surfoam Scale", Float) = 3
		_Surfoam_pan ("Surfoam pan", Vector) = (0.1,0.1,0,0)
		_Surfoam_edge_smooth ("Surfoam edge smooth", Range(0, 1)) = 0.2
		_Surfoam_edge ("Surfoam edge", Range(0, 1)) = 0.1
		_Surfoam_distortion_strenge ("Surfoam distortion strenge", Float) = 0.1
		_Surfoam_Smooth ("Surfoam Smooth", Vector) = (1,1,0,0)
		[NoScaleOffset] _Surface_distortion_map ("Surface distortion map", 2D) = "white" {}
		_Surface_distortion_pan ("Surface distortion pan", Vector) = (1,1,0,0)
		_Surface_distortion_map_Scale ("Surface distortion map Scale", Float) = 1
		_Surfacefoam_color ("Surfacefoam color", Vector) = (0,0,0,0)
		_deepth_distance ("deepth distance", Float) = 0
		_Deepht_color ("Deepht color", Vector) = (0.4723112,0.899371,0.8620237,1)
		[NoScaleOffset] _RippleRT ("RippleRT", 2D) = "white" {}
		_RippleCenter ("RippleCenter", Vector) = (0,0,0,0)
		_RippleWorldSize ("RippleWorldSize", Vector) = (0,0,0,0)
		_normal_strenge ("normal strenge", Float) = 0
		_Ds_str ("Ds str", Float) = 0.03
		[NoScaleOffset] _Ds_text ("Ds text", 2D) = "white" {}
		_Ds_tiling ("Ds tiling", Float) = 0
		_Ds_speed ("Ds speed", Vector) = (0.05,0.03,0,0)
		[NoScaleOffset] _ParallaxMapping_ca6a44575e7e4a939c2f1426afff27a2_Heightmap_1_Texture2D ("Texture2D", 2D) = "black" {}
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