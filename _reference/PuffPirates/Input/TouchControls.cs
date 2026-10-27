using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace PuffPirates.Input
{
	public class TouchControls : MonoBehaviour
	{
		private static float _camAccumX;

		private static float _camAccumY;

		private static float _pinchAccum;

		private VisualElement _hud;

		private VisualElement _stickZone;

		private VisualElement _stickBase;

		private VisualElement _stickKnob;

		private bool _visibilityKnown;

		private bool _visible;

		private int _stickPointer = -1;

		private Vector2 _stickOrigin;

		private float _stickRadius = 120f;

		private const float DeadZone = 0.12f;

		private VisualElement _camZone;

		private int _camP0 = -1;

		private int _camP1 = -1;

		private Vector2 _camPos0;

		private Vector2 _camPos1;

		private float _pinchPrevDist = -1f;

		public static float SteerX { get; private set; }

		public static bool NormalGas { get; private set; }

		public static Vector2 ConsumeCameraDrag()
		{
			Vector2 result = new Vector2(_camAccumX, _camAccumY);
			_camAccumX = 0f;
			_camAccumY = 0f;
			return result;
		}

		public static float ConsumePinchDelta()
		{
			float pinchAccum = _pinchAccum;
			_pinchAccum = 0f;
			return pinchAccum;
		}

		public void Bind(VisualElement root)
		{
			_hud = root.Q<VisualElement>("touch-hud");
			_visibilityKnown = false;
			_stickZone = root.Q<VisualElement>("touch-stick-zone");
			_stickBase = root.Q<VisualElement>("touch-stick-base");
			_stickKnob = root.Q<VisualElement>("touch-stick-knob");
			if (_hud == null || _stickZone == null)
			{
				Debug.LogWarning("[TouchControls] touch-hud elementleri bulunamadı.");
				return;
			}
			_stickBase.style.visibility = Visibility.Hidden;
			_stickZone.RegisterCallback<PointerDownEvent>(OnStickDown);
			_stickZone.RegisterCallback<PointerMoveEvent>(OnStickMove);
			_stickZone.RegisterCallback<PointerUpEvent>(OnStickUp);
			_stickZone.RegisterCallback((PointerCaptureOutEvent _) =>
			{
				ResetStick();
			});
			BindButton(root, "touch-btn-gas", (bool v) =>
			{
				NormalGas = v;
			});
			_camZone = root.Q<VisualElement>("touch-camera-zone");
			if (_camZone != null)
			{
				_camZone.RegisterCallback<PointerDownEvent>(OnCamDown);
				_camZone.RegisterCallback<PointerMoveEvent>(OnCamMove);
				_camZone.RegisterCallback<PointerUpEvent>(OnCamUp);
				_camZone.RegisterCallback((PointerCaptureOutEvent e) =>
				{
					ClearCamPointer(e.pointerId);
				});
			}
		}

		private void OnCamDown(PointerDownEvent e)
		{
			if (_camP0 == -1)
			{
				_camP0 = e.pointerId;
				_camPos0 = e.localPosition;
			}
			else
			{
				if (_camP1 != -1)
				{
					return;
				}
				_camP1 = e.pointerId;
				_camPos1 = e.localPosition;
				_pinchPrevDist = Vector2.Distance(_camPos0, _camPos1);
			}
			_camZone.CapturePointer(e.pointerId);
			e.StopPropagation();
		}

		private void OnCamMove(PointerMoveEvent e)
		{
			Vector2 vector = e.localPosition;
			Vector2 vector2;
			if (e.pointerId == _camP0)
			{
				vector2 = _camPos0;
				_camPos0 = vector;
			}
			else
			{
				if (e.pointerId != _camP1)
				{
					return;
				}
				vector2 = _camPos1;
				_camPos1 = vector;
			}
			if (_camP0 != -1 && _camP1 != -1)
			{
				float num = Vector2.Distance(_camPos0, _camPos1);
				if (_pinchPrevDist > 0f)
				{
					_pinchAccum += num - _pinchPrevDist;
				}
				_pinchPrevDist = num;
			}
			else
			{
				_camAccumX += vector.x - vector2.x;
				_camAccumY += vector.y - vector2.y;
			}
			e.StopPropagation();
		}

		private void OnCamUp(PointerUpEvent e)
		{
			if (e.pointerId == _camP0 || e.pointerId == _camP1)
			{
				if (_camZone.HasPointerCapture(e.pointerId))
				{
					_camZone.ReleasePointer(e.pointerId);
				}
				ClearCamPointer(e.pointerId);
				e.StopPropagation();
			}
		}

		private void ClearCamPointer(int id)
		{
			if (_camP0 == id)
			{
				_camP0 = _camP1;
				_camPos0 = _camPos1;
				_camP1 = -1;
			}
			else if (_camP1 == id)
			{
				_camP1 = -1;
			}
			_pinchPrevDist = -1f;
		}

		private static void BindButton(VisualElement root, string name, Action<bool> set)
		{
			VisualElement b = root.Q<VisualElement>(name);
			if (b == null)
			{
				Debug.LogWarning("[TouchControls] " + name + " yok.");
				return;
			}
			b.RegisterCallback((PointerDownEvent e) =>
			{
				set(obj: true);
				b.CapturePointer(e.pointerId);
				e.StopPropagation();
			});
			b.RegisterCallback((PointerUpEvent e) =>
			{
				set(obj: false);
				if (b.HasPointerCapture(e.pointerId))
				{
					b.ReleasePointer(e.pointerId);
				}
				e.StopPropagation();
			});
			b.RegisterCallback((PointerCaptureOutEvent _) =>
			{
				set(obj: false);
			});
		}

		private void OnStickDown(PointerDownEvent e)
		{
			if (_stickPointer == -1)
			{
				_stickPointer = e.pointerId;
				_stickOrigin = e.localPosition;
				_stickZone.CapturePointer(e.pointerId);
				_stickBase.style.visibility = Visibility.Visible;
				PlaceStick(_stickOrigin, Vector2.zero);
				e.StopPropagation();
			}
		}

		private void OnStickMove(PointerMoveEvent e)
		{
			if (e.pointerId == _stickPointer)
			{
				Vector2 vector = (Vector2)e.localPosition - _stickOrigin;
				SteerX = TouchMath.ResolveStick(vector, _stickRadius, 0.12f).x;
				PlaceStick(_stickOrigin, Vector2.ClampMagnitude(vector, _stickRadius));
				e.StopPropagation();
			}
		}

		private void OnStickUp(PointerUpEvent e)
		{
			if (e.pointerId == _stickPointer)
			{
				if (_stickZone.HasPointerCapture(e.pointerId))
				{
					_stickZone.ReleasePointer(e.pointerId);
				}
				ResetStick();
				e.StopPropagation();
			}
		}

		private void PlaceStick(Vector2 baseCenter, Vector2 knobOffset)
		{
			_stickBase.style.left = baseCenter.x - _stickRadius;
			_stickBase.style.top = baseCenter.y - _stickRadius;
			if (_stickKnob != null)
			{
				_stickKnob.style.left = _stickRadius + knobOffset.x - _stickKnob.resolvedStyle.width * 0.5f;
				_stickKnob.style.top = _stickRadius + knobOffset.y - _stickKnob.resolvedStyle.height * 0.5f;
			}
		}

		private void ResetStick()
		{
			_stickPointer = -1;
			SteerX = 0f;
			if (_stickBase != null)
			{
				_stickBase.style.visibility = Visibility.Hidden;
			}
		}

		public void SetVisible(bool on)
		{
			if (!_visibilityKnown || _visible != on)
			{
				_visibilityKnown = true;
				_visible = on;
				if (_hud != null)
				{
					_hud.style.display = ((!on) ? DisplayStyle.None : DisplayStyle.Flex);
				}
				if (!on)
				{
					ResetStick();
					NormalGas = false;
					ResetCam();
				}
			}
		}

		private void ResetCam()
		{
			_camP0 = (_camP1 = -1);
			_pinchPrevDist = -1f;
			_camAccumX = (_camAccumY = 0f);
			_pinchAccum = 0f;
		}

		private void OnDisable()
		{
			_visibilityKnown = false;
			ResetStick();
			NormalGas = false;
			ResetCam();
		}
	}
}
