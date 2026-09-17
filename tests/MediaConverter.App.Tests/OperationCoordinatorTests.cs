using MediaConverter.App.Services;

namespace MediaConverter.App.Tests;

/// <summary>
/// Tests the coordinator that blocks a second operation from starting while one is in flight.
/// </summary>
public class OperationCoordinatorTests
{
    [Fact]
    public void NewCoordinator_IsNotBusy()
    {
        var coordinator = new OperationCoordinator();

        Assert.False(coordinator.IsBusy);
    }

    [Fact]
    public void Begin_MarksBusyAndDispose_ReleasesIt()
    {
        var coordinator = new OperationCoordinator();

        var scope = coordinator.Begin();

        Assert.True(coordinator.IsBusy);
        scope.Dispose();
        Assert.False(coordinator.IsBusy);
    }

    [Fact]
    public void DisposeTwice_DoesNotUnderflowBusyState()
    {
        var coordinator = new OperationCoordinator();

        var scope = coordinator.Begin();
        scope.Dispose();
        scope.Dispose();

        Assert.False(coordinator.IsBusy);
    }

    [Fact]
    public void NestedScopes_StayBusyUntilTheLastOneDisposes()
    {
        var coordinator = new OperationCoordinator();

        var outer = coordinator.Begin();
        var inner = coordinator.Begin();

        inner.Dispose();
        Assert.True(coordinator.IsBusy);

        outer.Dispose();
        Assert.False(coordinator.IsBusy);
    }

    [Fact]
    public void BeginAndDispose_RaiseChanged()
    {
        var coordinator = new OperationCoordinator();
        var count = 0;
        coordinator.Changed += (_, _) => count++;

        coordinator.Begin().Dispose();

        Assert.Equal(2, count);
    }
}
