using System;


namespace MapView
{
	internal static class Globals
	{
		#region Fields (static)
		internal const double ScaleMinimum = 0.3;
		internal const double ScaleMaximum = 3.0;
		#endregion


		#region Properties (static)
		private static double _scale = 1.0;
		/// <summary>
		/// The scale-factor for sprites and clicks (etc) in MainView only.
		/// </summary>
		internal static double Scale
		{
			get { return _scale; }
			set { _scale = value; }
		}

		private static bool _autoScale = true;
		internal static bool AutoScale
		{
			get { return _autoScale; }
			set { _autoScale = value; }
		}

		internal static XCom.SpriteCollection ExtraSprites
		{ get; private set; }
		#endregion


		#region Methods (static)
		/// <summary>
		/// Loads the sprites for TopView's blank quads and TileView's eraser.
		/// </summary>
		internal static void LoadExtraSprites()
		{
			using (var fsPck = System.Reflection.Assembly.GetExecutingAssembly()
								.GetManifestResourceStream("MapView._Embedded.Extra.PCK"))
			using (var fsTab = System.Reflection.Assembly.GetExecutingAssembly()
								.GetManifestResourceStream("MapView._Embedded.Extra.TAB"))
			{
				ExtraSprites = new XCom.SpriteCollection(
													fsPck,
													fsTab,
													2,
													XCom.Palette.UfoBattle);
			}
		}

		/// <summary>
		/// Clamps a value between min and max inclusively. Note that no check
		/// is done to ensure that min is less than max.
		/// </summary>
		/// <param name="val"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <returns>min if val is less than min; max if value is greater than
		/// max; else the value itself</returns>
		internal static T Clamp<T>(
				this T val,
				T min,
				T max) where T
			:
				IComparable<T>
		{
			if (val.CompareTo(min) < 0)
				return min;

			if (val.CompareTo(max) > 0)
				return max;

			return val;
		}
		#endregion
	}
}
