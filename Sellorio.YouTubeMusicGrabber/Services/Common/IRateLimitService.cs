using System;
using System.Threading.Tasks;

namespace Sellorio.YouTubeMusicGrabber.Services.Common
{
    internal interface IRateLimitService
    {
        Task WithRateLimit(string waitAndReset, Func<Task> func);
        Task WithRateLimit(string waitAndReset, string waitFor, Func<Task> func);
        Task WithRateLimit(string waitAndReset, string[] waitFor, Func<Task> func);
        Task WithRateLimit(string[] waitAndReset, Func<Task> func);
        Task WithRateLimit(string[] waitAndReset, string[] waitFor, Func<Task> func);
    }
}
