using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParityAnalyserCore
{
	/*				Up
	 *			   +-180
	 *		  -135		  135
	 * Left	-90				90	Right
	 *		  -45		  45
	 *				 0
	 *				Down
	 * */
	public class AngleRange(float center, float range = 60f, float safeMargin = 25f)
	{

		private float collapsedAngle = center;

		public void Collapse(float angle) => collapsedAngle = angle;
		public float CutAngle(float currentAngle, bool ignoreRange = false)
		{
			if (ignoreRange) return center;
			if (Contains(currentAngle)) return currentAngle; // Don't rotate if you can just hit the note comfortably

			return ClosestAngle(currentAngle);
		}

		public float ClosestAngle(float angle) => angle + Math.MinAbs(minValue - angle, maxValue - angle);

		public bool Contains(float angle) => minValue <= angle && angle <= maxValue;

		public float minValue => center - range + safeMargin;
		public float maxValue => center + range - safeMargin;

		public static implicit operator AngleRange(float angle) => new(angle);

		public static implicit operator float(AngleRange range) => range.collapsedAngle;
	}
}
