Shader "UberStylizedWater" {
	Properties {
		[HDR] _Color_Shallow ("Color_Shallow", Vector) = (0.2269659,0.822786,0.4507858,0)
		[HDR] _Color_Deep ("Color_Deep", Vector) = (0,0.2541522,0.4507858,1)
		_Water_Depth ("Water_Depth", Float) = 0.3
		[ToggleUI] _WorldSpaceDepth ("WorldSpaceDepth", Float) = 1
		_DistanceMask_Start ("DistanceMask_Start", Float) = 5
		_DistanceMask_Fade ("DistanceMask_Fade", Float) = 10
		[ToggleUI] _ShoreFade ("ShoreFade", Float) = 1
		_ShoreFade_Smoothness ("ShoreFade_Smoothness", Range(0, 1)) = 0.2
		[ToggleUI] _Enable_SurfaceFoam ("Enable_SurfaceFoam", Float) = 1
		_SurfFoam_Color ("SurfFoam_Color", Vector) = (1,1,1,1)
		[NoScaleOffset] _SurfFoam_Map ("SurfFoam_Map", 2D) = "white" {}
		_SurfFoam_AlphaBlend ("SurfFoam_AlphaBlend", Range(0, 1)) = 0
		[ToggleUI] _Invert_SurfFoam ("Invert_SurfFoam", Float) = 0
		_SurfFoam_Pan ("SurfFoam_Pan", Vector) = (0.1,0.1,0,0)
		_SurfFoam_Scale ("SurfFoam_Scale", Float) = 3
		_SurfFoam_Tile ("SurfFoam_Tile", Vector) = (1,1,0,0)
		_SurfFoam_Edge ("SurfFoam_Edge", Range(0, 1)) = 0.1
		_SurfFoam_EdgeSmooth ("SurfFoam_EdgeSmooth", Range(0, 1)) = 0.2
		[NoScaleOffset] _SurfaceDistortion_Map ("SurfaceDistortion_Map", 2D) = "white" {}
		_SurfaceDistortion_Scale ("SurfaceDistortion_Scale", Float) = 1
		_SurfaceDistortion_Strength ("SurfaceDistortion_Strength", Float) = 0.1
		_SurfaceDistortion_Pan ("SurfaceDistortion_Pan", Vector) = (1,1,0,0)
		[ToggleUI] _Enable_Intersection ("Enable_Intersection", Float) = 0
		_InterSec_Color ("InterSec_Color", Vector) = (1,1,1,1)
		_InterSec_Width ("InterSec_Width", Float) = 0.5
		[NoScaleOffset] _InterSec_Foam_Mask ("InterSec_Foam_Mask", 2D) = "white" {}
		_InterSec_Dissolve ("InterSec_Dissolve", Float) = 1.6
		[ToggleUI] _InterSec_Foam_Invert ("InterSec_Foam_Invert", Float) = 0
		_InterSec_Foam_Scale ("InterSec_Foam_Scale", Float) = 4
		_InterSec_Foam_Tile ("InterSec_Foam_Tile", Vector) = (1,1,0,0)
		_InterSec_Foam_Pan ("InterSec_Foam_Pan", Vector) = (0,0,0,0)
		_InterSec_Foam_Distortion ("InterSec_Foam_Distortion", Float) = 3
		_InterSec_Foam_Smooth ("InterSec_Foam_Smooth", Range(0, 1)) = 0.01
		[ToggleUI] _InterSec_GradientDissolve ("InterSec_GradientDissolve", Float) = 0
		_InterSec_Edge_Fade ("InterSec_Edge_Fade", Range(0, 1)) = 0
		[Toggle(_ENABLESHORELINE)] _ENABLESHORELINE ("EnableShoreLine", Float) = 0
		_SL_Color ("SL_Color", Vector) = (1,1,1,1)
		_SL_WaterDepth ("SL_WaterDepth", Float) = 0.3
		_SL_Speed ("SL_Speed", Float) = 0.05
		_SL_Ammount ("SL_Ammount", Float) = 5
		_SL_Thickness ("SL_Thickness", Range(0, 1)) = 0.3
		_SL_CenterMask ("SL_CenterMask", Range(0, 1)) = 0.5
		_SL_CenterMaskFade ("SL_CenterMaskFade", Range(0, 1)) = 0
		[NoScaleOffset] _SL_Dissolve_Mask ("SL_Dissolve_Mask", 2D) = "white" {}
		_SL_Dissolve ("SL_Dissolve", Float) = 0.7
		_SL_GradientDissolve ("SL_Dissolve_ShoreGradient", Range(-1, 1)) = 0
		_SL_MaskPan ("SL_MaskPan", Vector) = (0.01,0,0,0)
		_SL_MaskScale ("SL_MaskScale", Float) = 4
		_SL_MaskTile ("SL_MaskTile", Vector) = (1,1,0,0)
		[ToggleUI] _SL_EnableTrail ("SL_EnableTrail", Float) = 0
		_SL_Trail_Fade ("SL_Trail_Fade", Range(0, 2)) = 1
		[Toggle(_ENABLE_UNDERWATERLAYER)] _ENABLE_UNDERWATERLAYER ("Enable_UnderWaterLayer", Float) = 0
		_UnderWater_Depth ("UnderWater_Depth", Float) = -4
		[ToggleUI] _ConformToGeometry ("ConformToGeometry", Float) = 1
		_Underwater_Color ("Underwater_Color", Vector) = (0,0,0,0.5019608)
		_UnderWater_ScaleModifier ("UnderWater_ScaleModifier", Range(-2, 2)) = 0
		_UnderWater_Start ("UnderWater_Start", Float) = 5
		_Underwater_Fade ("Underwater_Fade", Float) = 15
		[Toggle(_ENABLENORMAL)] _ENABLENORMAL ("EnableNormal", Float) = 0
		[NoScaleOffset] [Normal] _Normal_Map ("Normal_Map", 2D) = "bump" {}
		_Normal_Strength ("Normal_Strength", Float) = 0.1
		_Normal_Pan ("Normal_Pan", Float) = 0.1
		_Normal_Scale ("Normal_Scale", Float) = 5
		_Normal_DistanceStrength ("Normal_DistanceStrength", Float) = 0.01
		_ShadowColor ("ShadowColor", Vector) = (0,0,0,0.6)
		[HDR] _Specular_Color ("Specular_Color", Vector) = (1,1,1,1)
		_Specular_Spread ("Specular_Spread", Range(0, 1)) = 0
		_Specular_Hardness ("Specular_Hardness", Range(0, 1)) = 0
		_Specular_Size ("Specular_Size", Range(0, 1)) = 1
		[Toggle(_ENABLEPLANERREFLECTION)] _ENABLEPLANERREFLECTION ("EnablePlanerReflection", Float) = 0
		_Reflection_Strength ("Reflection_Strength", Range(0, 1)) = 1
		_Reflection_Fresnel ("Reflection_Fresnel", Float) = 1
		_Reflection_Distortion ("Reflection_Distortion", Float) = 0
		[Toggle(_ENABLEREFRACTION)] _ENABLEREFRACTION ("EnableRefraction", Float) = 0
		_Refraction_Strength ("Refraction_Strength", Float) = 0.5
		_Refraction_Distance_Strength ("Refraction_Distance_Strength", Float) = 0.1
		_Refraction_Distance_Fade ("Refraction_Distance_Fade", Float) = 0.5
		[Toggle(_ENABLECAUSTICS)] _ENABLECAUSTICS ("EnableCaustics", Float) = 0
		_Caustics_Depth ("Caustics_Depth", Float) = -4
		[NoScaleOffset] _Caustics_Map ("Caustics_Map", 2D) = "white" {}
		_Caustics_Pan ("Caustics_Pan", Float) = 0.1
		_Caustics_Scale ("Caustics_Scale", Float) = 1
		_Caustics_Strength ("Caustics_Strength", Float) = 1
		[NoScaleOffset] _Caustics_Distortion_Map ("Caustics_Distortion_Map", 2D) = "white" {}
		_Caustics_Distortion_Strength ("Caustics_Distortion_Strength", Float) = 3
		_Caustics_Distortion_Scale ("Caustics_Distortion_Scale", Float) = 2
		_Caustics_Start ("Caustics_Start", Float) = 5
		_Caustics_Fade ("Caustics_Fade", Float) = 5
		[Toggle(_ENABLEWAVE)] _ENABLEWAVE ("EnableWave", Float) = 0
		_Wave_Top_Color ("Wave_Top_Color", Vector) = (0.2982912,0.9207434,0.9245283,1)
		_1st_Wave_Length ("1st_Wave_Length", Float) = 3
		_1st_Wave_Height ("1st_Wave_Height", Float) = 0.01
		_1st_Wave_Speed ("1st_Wave_Speed", Float) = 1
		_1st_Wave_Direction ("1st_Wave_Direction", Vector) = (1,0,0,0)
		_1st_Wave_Sharpness ("1st_Wave_Sharpness", Float) = 0.3
		_2nd_Wave_Length ("2nd_Wave_Length", Float) = 5
		_2nd_Wave_Height ("2nd_Wave_Height", Float) = 0.015
		_2nd_Wave_Speed ("2nd_Wave_Speed", Float) = 1.3
		_2nd_Wave_Sharpness ("2nd_Wave_Sharpness", Float) = 0.3
		_2nd_Wave_Direction ("2nd_Wave_Direction", Vector) = (-1,0,1,0)
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