// The tests swap global compiler settings: the compiler factory, the design time hooks. They run one at a time, as in the other test projects.
[assembly: Xunit.CollectionBehavior(Xunit.CollectionBehavior.CollectionPerAssembly, DisableTestParallelization = true)]
