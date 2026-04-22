using Soenneker.Blazor.WebWorkers.Abstract;
using Soenneker.Tests.HostedUnit;

namespace Soenneker.Blazor.WebWorkers.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public sealed class WebWorkersUtilTests : HostedUnitTest
{
    private readonly IWebWorkersUtil _blazorlibrary;

    public WebWorkersUtilTests(Host host) : base(host)
    {
        _blazorlibrary = Resolve<IWebWorkersUtil>(true);
    }

    [Test]
    public void Default()
    {

    }
}
