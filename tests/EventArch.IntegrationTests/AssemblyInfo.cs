// Test collections run one at a time. Each fixture boots the API's Program, which sets the
// process-wide Serilog logger (Log.Logger); two fixtures starting in parallel race on it and
// one of them fails before building the host. Sequential collections also avoid starting
// several sets of containers at once on the CI runner.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
