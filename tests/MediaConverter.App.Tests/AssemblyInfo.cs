using Xunit;

// The localization tests switch the process UI culture, so this assembly runs its tests
// sequentially to keep that shared state deterministic.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
