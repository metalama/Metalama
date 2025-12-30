using Metalama.Backstage.Application;

namespace Metalama.Framework.Tests.Benchmarks;

internal sealed class BenchmarkApplicationInfo : ApplicationInfoBase
{
    public BenchmarkApplicationInfo() : base( typeof(BenchmarkApplicationInfo).Assembly ) { }

    public override string Name => "Metalama.Framework.Tests.Benchmarks";

    public override bool ShouldCreateLocalCrashReports => false;
}