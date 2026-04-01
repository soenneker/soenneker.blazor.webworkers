using System.Threading.Tasks;

namespace Soenneker.Blazor.WebWorkers.Dtos.Abstract;

internal interface IDotNetPendingInvocation
{
    ValueTask SetCompletedAsync(CoordinatorEvent coordinatorEvent);
    void SetCancelled(CoordinatorEvent coordinatorEvent);
    void SetFaulted(CoordinatorEvent coordinatorEvent);
    void SetDisposed();
}
