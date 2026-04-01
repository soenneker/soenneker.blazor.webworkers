using Soenneker.Blazor.WebWorkers.Abstract;
using Soenneker.Tests.FixturedUnit;
using Xunit;

namespace Soenneker.Blazor.WebWorkers.Tests;

[Collection("Collection")]
public sealed class WebWorkersUtilTests : FixturedUnitTest
{
    private readonly IWebWorkersUtil _blazorlibrary;

    public WebWorkersUtilTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        _blazorlibrary = Resolve<IWebWorkersUtil>(true);
    }

    [Fact]
    public void Default()
    {

    }
}
