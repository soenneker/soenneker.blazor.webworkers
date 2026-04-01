using System.Threading.Tasks;

namespace Soenneker.Blazor.WebWorkers.Dtos.Abstract;

internal interface IPendingJob
{
    ValueTask ReportProgressAsync(WebWorkerJobProgress progress);
    void SetCompleted(CoordinatorEvent coordinatorEvent);
    void SetCancelled(CoordinatorEvent coordinatorEvent);
    void SetFaulted(CoordinatorEvent coordinatorEvent);
    void SetDisposed();
}
