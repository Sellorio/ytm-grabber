using Sellorio.YouTubeMusicGrabber.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sellorio.YouTubeMusicGrabber.Services.Common
{
    internal class RateLimitService : IRateLimitService
    {
        private readonly Dictionary<string, RateLimitEntry> _configuredRateLimits = new();

        public void ConfigureRateLimit(string rateLimitKey, TimeSpan rateLimit)
        {
            _configuredRateLimits.Add(rateLimitKey, new() { Lock = new(1), RateLimit = rateLimit });
        }

        public Task WithRateLimit(string waitAndReset, Func<Task> func)
        {
            return WithRateLimit([waitAndReset], [], func);
        }

        public Task WithRateLimit(string waitAndReset, string waitFor, Func<Task> func)
        {
            return WithRateLimit([waitAndReset], [waitFor], func);
        }

        public Task WithRateLimit(string[] waitAndReset, Func<Task> func)
        {
            return WithRateLimit(waitAndReset, [], func);
        }

        public Task WithRateLimit(string waitAndReset, string[] waitFor, Func<Task> func)
        {
            return WithRateLimit([waitAndReset], waitFor, func);
        }

        public async Task WithRateLimit(string[] waitAndReset, string[] waitFor, Func<Task> func)
        {
            var primaryRateLimitEntries = waitAndReset.Select(x => _configuredRateLimits[x]).ToArray();
            var additionalRateLimitEntries = waitFor.Select(x => _configuredRateLimits[x]).ToArray();
            var allRateLimitEntries = Enumerable.Concat(primaryRateLimitEntries, additionalRateLimitEntries).ToArray();

            foreach (var entry in allRateLimitEntries)
            {
                await entry.Lock.WaitAsync();
            }

            try
            {
                var now = DateTime.UtcNow;
                var highestWaitTime =
                    allRateLimitEntries.Max(x => x.LastAccessed == default ? TimeSpan.Zero : (x.LastAccessed + x.RateLimit - now));

                if (highestWaitTime > TimeSpan.Zero)
                {
                    if (highestWaitTime > TimeSpan.FromSeconds(1))
                    {
                        ConsoleHelper.Write("Rate limiting... ", ConsoleColor.DarkGray);
                    }

                    await Task.Delay(highestWaitTime);

                    if (highestWaitTime > TimeSpan.FromSeconds(1))
                    {
                        ConsoleHelper.WriteLine("Resuming...", ConsoleColor.DarkGray);
                    }
                }

                await func.Invoke();

                foreach (var entry in primaryRateLimitEntries)
                {
                    entry.LastAccessed = DateTime.UtcNow;
                }
            }
            finally
            {
                foreach (var entry in allRateLimitEntries)
                {
                    entry.Lock.Release();
                }
            }
        }

        private class RateLimitEntry
        {
            public DateTime LastAccessed { get; set; }
            public TimeSpan RateLimit { get; set; }
            public SemaphoreSlim Lock { get; set; }
        }
    }
}
