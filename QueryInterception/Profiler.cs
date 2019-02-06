using System;
using System.Runtime.CompilerServices;

namespace QueryInterception
{
	internal class Profiler
	{
		public Profiler()
		{
		}

		public static IDisposable Step(string interceptingQuery)
		{
			return new Disposable(() => {
			});
		}
	}
}