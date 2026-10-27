using UnityEngine;
using UnityEngine.Rendering;

namespace PuffPirates.Water
{
	[RequireComponent(typeof(WaterChannel))]
	public sealed class WaterRippleCam : MonoBehaviour
	{
		[Tooltip("Boş bırakılırsa bu objenin transform'u kullanılır — DÜZ/dikdörtgen bir kanalda bu genelde doğrudur. Kıvrımlı/eğimli bir kanalda (WaterChannel._surfaceCollider dolu) bu objenin transform'u suyun DIŞINDA olabilir (bu tam olarak yaşanan bir hataydı) — botların asıl yüzdüğü noktaya taşınmış ayrı bir child Transform ver.")]
		[SerializeField]
		private Transform _areaAnchor;

		[Tooltip("İzin göründüğü kare alan (m) — botların asıl dolaştığı bölgeyi kapsayacak kadar seç.")]
		[SerializeField]
		[Min(5f)]
		private float _areaSize = 60f;

		[SerializeField]
		private int _rtResolution = 512;

		[Tooltip("Sadece bu katmandaki parçacıklar kameraya görünür (BoatRippleTrail ile aynı olmalı).")]
		[SerializeField]
		private string _rippleLayerName = "RippleFX";

		[SerializeField]
		private Color _color = Color.white;

		[SerializeField]
		[Min(0f)]
		private float _intensity = 1f;

		private static readonly int RippleRTProp = Shader.PropertyToID("_RippleRT");

		private static readonly int RippleCenterProp = Shader.PropertyToID("_RippleCenter");

		private static readonly int RippleWorldSizeProp = Shader.PropertyToID("_RippleWorldSize");

		private WaterChannel _channel;

		private Camera _cam;

		private RenderTexture _rt;

		private GameObject _overlayGo;

		private MeshRenderer _overlayMr;

		private Material _overlayMat;

		private MaterialPropertyBlock _overlayMpb;

		private MaterialPropertyBlock _waterMpb;

		private Vector3 AnchorPos
		{
			get
			{
				if (!(_areaAnchor != null))
				{
					return base.transform.position;
				}
				return _areaAnchor.position;
			}
		}

		private void OnEnable()
		{
			_channel = GetComponent<WaterChannel>();
			int num = LayerMask.NameToLayer(_rippleLayerName);
			if (num < 0)
			{
				Debug.LogWarning("WaterRippleCam (" + base.name + "): '" + _rippleLayerName + "' katmanı Project Settings > Tags and Layers'ta yok — iz devre dışı kaldı.");
				base.enabled = false;
				return;
			}
			_rt = new RenderTexture(_rtResolution, _rtResolution, 16, RenderTextureFormat.ARGB32)
			{
				name = "RippleRT_" + base.name,
				wrapMode = TextureWrapMode.Clamp
			};
			GameObject gameObject = new GameObject("RippleCam_" + base.name);
			gameObject.transform.SetParent(base.transform, worldPositionStays: false);
			Vector3 anchorPos = AnchorPos;
			float y = _channel.WaterY + _channel.LevelOffset + 40f;
			gameObject.transform.position = new Vector3(anchorPos.x, y, anchorPos.z);
			gameObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
			_cam = gameObject.AddComponent<Camera>();
			_cam.orthographic = true;
			_cam.orthographicSize = _areaSize * 0.5f;
			_cam.nearClipPlane = 0.1f;
			_cam.farClipPlane = 80f;
			_cam.clearFlags = CameraClearFlags.Color;
			_cam.backgroundColor = Color.black;
			_cam.cullingMask = 1 << num;
			_cam.targetTexture = _rt;
			_cam.useOcclusionCulling = false;
			_cam.allowHDR = false;
			_cam.allowMSAA = false;
			Shader shader = Shader.Find("PuffPirates/RippleOverlay");
			if (shader == null)
			{
				Debug.LogWarning("WaterRippleCam (" + base.name + "): 'PuffPirates/RippleOverlay' shader'ı bulunamadı — iz görünmeyecek (üretiliyor ama çizilmiyor).");
				return;
			}
			_overlayMat = new Material(shader)
			{
				name = "RippleOverlayMat_" + base.name
			};
			_overlayMat.SetColor("_Color", _color);
			_overlayMat.SetFloat("_Intensity", _intensity);
			_overlayGo = new GameObject("RippleOverlay_" + base.name);
			_overlayGo.transform.SetParent(base.transform, worldPositionStays: false);
			_overlayGo.AddComponent<MeshFilter>().sharedMesh = BuildQuad(_areaSize);
			_overlayMr = _overlayGo.AddComponent<MeshRenderer>();
			_overlayMr.sharedMaterial = _overlayMat;
			_overlayMr.shadowCastingMode = ShadowCastingMode.Off;
			_overlayMr.receiveShadows = false;
			_overlayGo.transform.position = new Vector3(anchorPos.x, _channel.WaterY + _channel.LevelOffset + 0.05f, anchorPos.z);
		}

		private void LateUpdate()
		{
			if (_rt == null)
			{
				return;
			}
			if (_overlayMr != null)
			{
				if (_overlayMpb == null)
				{
					_overlayMpb = new MaterialPropertyBlock();
				}
				_overlayMpb.SetTexture(RippleRTProp, _rt);
				_overlayMr.SetPropertyBlock(_overlayMpb);
			}
			Renderer waterSurface = _channel.WaterSurface;
			if (waterSurface != null)
			{
				if (_waterMpb == null)
				{
					_waterMpb = new MaterialPropertyBlock();
				}
				waterSurface.GetPropertyBlock(_waterMpb);
				_waterMpb.SetTexture(RippleRTProp, _rt);
				Vector3 anchorPos = AnchorPos;
				_waterMpb.SetVector(RippleCenterProp, new Vector4(anchorPos.x, anchorPos.z, 0f, 0f));
				_waterMpb.SetVector(RippleWorldSizeProp, new Vector4(_areaSize, _areaSize, 0f, 0f));
				waterSurface.SetPropertyBlock(_waterMpb);
			}
		}

		private static Mesh BuildQuad(float size)
		{
			float num = size * 0.5f;
			Mesh mesh = new Mesh
			{
				name = "RippleOverlayMesh"
			};
			mesh.vertices = new Vector3[4]
			{
				new Vector3(0f - num, 0f, 0f - num),
				new Vector3(num, 0f, 0f - num),
				new Vector3(num, 0f, num),
				new Vector3(0f - num, 0f, num)
			};
			mesh.uv = new Vector2[4]
			{
				new Vector2(0f, 0f),
				new Vector2(1f, 0f),
				new Vector2(1f, 1f),
				new Vector2(0f, 1f)
			};
			mesh.triangles = new int[6] { 0, 2, 1, 0, 3, 2 };
			mesh.RecalculateBounds();
			return mesh;
		}

		private void OnDisable()
		{
			if (_cam != null)
			{
				Object.Destroy(_cam.gameObject);
			}
			if (_overlayGo != null)
			{
				Object.Destroy(_overlayGo);
			}
			if (_overlayMat != null)
			{
				Object.Destroy(_overlayMat);
			}
			if (_rt != null)
			{
				_rt.Release();
				_rt = null;
			}
		}
	}
}
