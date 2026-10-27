using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UIElements;

namespace PuffPirates.UI
{
	[UxmlElement]
	public class TrapezoidBackground : VisualElement
	{
		[Serializable]
		public new class UxmlSerializedData : VisualElement.UxmlSerializedData
		{
			[SerializeField]
			private Color FillColor;

			[SerializeField]
			private float SlantWidth;

			[SerializeField]
			private float SlantWidthLeft;

			[SerializeField]
			[UxmlIgnore]
			[HideInInspector]
			private UxmlAttributeFlags FillColor_UxmlAttributeFlags;

			[SerializeField]
			[UxmlIgnore]
			[HideInInspector]
			private UxmlAttributeFlags SlantWidth_UxmlAttributeFlags;

			[SerializeField]
			[UxmlIgnore]
			[HideInInspector]
			private UxmlAttributeFlags SlantWidthLeft_UxmlAttributeFlags;

			[RegisterUxmlCache]
			[Conditional("UNITY_EDITOR")]
			public new static void Register()
			{
				UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[3]
				{
					new UxmlAttributeNames("FillColor", "fill-color", null),
					new UxmlAttributeNames("SlantWidth", "slant-width", null),
					new UxmlAttributeNames("SlantWidthLeft", "slant-width-left", null)
				});
			}

			public override object CreateInstance()
			{
				return new TrapezoidBackground();
			}

			public override void Deserialize(object obj)
			{
				base.Deserialize(obj);
				TrapezoidBackground trapezoidBackground = (TrapezoidBackground)obj;
				if (UnityEngine.UIElements.UxmlSerializedData.ShouldWriteAttributeValue(FillColor_UxmlAttributeFlags))
				{
					trapezoidBackground.FillColor = FillColor;
				}
				if (UnityEngine.UIElements.UxmlSerializedData.ShouldWriteAttributeValue(SlantWidth_UxmlAttributeFlags))
				{
					trapezoidBackground.SlantWidth = SlantWidth;
				}
				if (UnityEngine.UIElements.UxmlSerializedData.ShouldWriteAttributeValue(SlantWidthLeft_UxmlAttributeFlags))
				{
					trapezoidBackground.SlantWidthLeft = SlantWidthLeft;
				}
			}
		}

		[UxmlAttribute]
		public Color FillColor { get; set; } = new Color(0f, 0f, 0f, 0.6f);

		[UxmlAttribute]
		public float SlantWidth { get; set; } = 40f;

		[UxmlAttribute]
		public float SlantWidthLeft { get; set; }

		public TrapezoidBackground()
		{
			base.generateVisualContent = (Action<MeshGenerationContext>)Delegate.Combine(base.generateVisualContent, new Action<MeshGenerationContext>(OnGenerateVisualContent));
		}

		private void OnGenerateVisualContent(MeshGenerationContext mgc)
		{
			float width = base.contentRect.width;
			float height = base.contentRect.height;
			if (!(width <= 0f) && !(height <= 0f))
			{
				float num = Mathf.Min(SlantWidth, width);
				float x = Mathf.Min(SlantWidthLeft, width - num);
				Painter2D painter2D = mgc.painter2D;
				painter2D.fillColor = FillColor;
				painter2D.BeginPath();
				painter2D.MoveTo(new Vector2(0f, 0f));
				painter2D.LineTo(new Vector2(width, 0f));
				painter2D.LineTo(new Vector2(width - num, height));
				painter2D.LineTo(new Vector2(x, height));
				painter2D.ClosePath();
				painter2D.Fill();
			}
		}
	}
}
