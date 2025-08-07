using System;
using System.Threading.Tasks;

namespace Sellorio.YouTubeMusicGrabber.Commands;

internal interface ICommand
{
    Task ExecuteAsync(IServiceProvider serviceProvider);
}
