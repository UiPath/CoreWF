// The tests change process-wide compiler settings, so they run one at a time.
[assembly: Xunit.CollectionBehavior(Xunit.CollectionBehavior.CollectionPerAssembly, DisableTestParallelization = true)]
