using System;
using System.Threading;

namespace QueueProcessor.Utils
{
    public static class ThreadLocalRandom
    {
        private static readonly Random Shared = new Random();
        private static readonly ThreadLocal<Random> Local = new ThreadLocal<Random>(() =>
        {
            lock (Shared)
            {
                return new Random(Shared.Next());
            }
        });

        public static int Next() => Local.Value.Next();
        public static int Next(int maxValue) => Local.Value.Next(maxValue);
        public static double NextDouble() => Local.Value.NextDouble();
    }
}
