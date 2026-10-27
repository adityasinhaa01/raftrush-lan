using PuffPirates.Players;
using UnityEngine;

namespace PuffPirates.Rendering
{
	public class LocalCrewQuickOutline : MonoBehaviour
	{
		[SerializeField]
		private Color _charColor = new Color(1f, 0.85f, 0.1f);

		[SerializeField]
		[Range(0f, 10f)]
		private float _width = 4f;

		[SerializeField]
		private Outline.Mode _mode = Outline.Mode.OutlineVisible;

		private PiratePlayer _pirate;

		private Outline _charOutline;

		private Mesh _charMesh;

		private float _nextResolve;

		private void LateUpdate()
		{
			if (!(Time.unscaledTime < _nextResolve))
			{
				_nextResolve = Time.unscaledTime + 0.5f;
				Resolve();
			}
		}

		private void Resolve()
		{
			if (_pirate == null)
			{
				_pirate = PiratePlayer.Local;
			}
			if (_pirate == null)
			{
				return;
			}
			SkinnedMeshRenderer componentInChildren = _pirate.GetComponentInChildren<SkinnedMeshRenderer>();
			if (componentInChildren != null && componentInChildren.sharedMesh != _charMesh)
			{
				if (_charOutline != null)
				{
					Object.DestroyImmediate(_charOutline);
				}
				Outline outline = AttachOutline(componentInChildren.gameObject, _charColor, 1);
				if (outline != null)
				{
					_charOutline = outline;
					_charMesh = componentInChildren.sharedMesh;
				}
			}
			Outline[] componentsInChildren = _pirate.GetComponentsInChildren<Outline>(includeInactive: true);
			foreach (Outline outline2 in componentsInChildren)
			{
				if (outline2 != _charOutline && outline2.OutlineMode != _mode)
				{
					outline2.OutlineMode = _mode;
				}
			}
		}

		private Outline AttachOutline(GameObject rendererGo, Color color, int stencilRef)
		{
			Outline outline = rendererGo.GetComponent<Outline>();
			if (outline == null)
			{
				outline = rendererGo.AddComponent<Outline>();
			}
			outline.OutlineMode = _mode;
			outline.OutlineColor = color;
			outline.OutlineWidth = _width;
			outline.StencilRef = stencilRef;
			outline.enabled = true;
			return outline;
		}
	}
}
