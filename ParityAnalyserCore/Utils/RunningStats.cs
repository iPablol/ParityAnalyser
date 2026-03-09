using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParityAnalyserCore
{
	public class RunningStats
	{
		private int _count;
		private float _mean;
		private float _m2;

		public void Add(float value)
		{
			_count++;
			float delta = value - _mean;
			_mean += delta / _count;
			float delta2 = value - _mean;
			_m2 += delta * delta2;
		}

		public int Count => _count;
		public float Mean => _mean;
		public float Variance => _count > 1 ? _m2 / (_count - 1) : 0;
		public float StdDev => (float)Math.Sqrt(Variance);

		public bool IsOutlier(float value)
		{
			if (StdDev == 0) return false;
			return Math.Abs(value - Mean) / StdDev > 2.5f;
		}
	}

}
