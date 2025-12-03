<Query Kind="Expression">
  <Connection>
    <ID>615c0f52-0344-430e-bdb4-60068ec155aa</ID>
    <NamingServiceVersion>2</NamingServiceVersion>
    <Persist>false</Persist>
    <Driver Assembly="Metalama.LinqPad" PublicKeyToken="772fca7b1db8db06">Metalama.LinqPad.MetalamaScratchpadDriver</Driver>
  </Connection>
  <NuGetReference Prerelease="true">Metalama.LinqPad</NuGetReference>
  <Namespace>Metalama.Framework.Workspaces</Namespace>
  <Namespace>Metalama.Framework.Code</Namespace>
  <Namespace>Metalama.Framework.Code.Collections</Namespace>
  <Namespace>Metalama.Framework.Introspection</Namespace>
  <Namespace>Metalama.Framework.Diagnostics</Namespace>
  <Namespace>Metalama.Framework.Metrics</Namespace>
  <Namespace>Metalama.Extensions.Metrics</Namespace>
  <Namespace>Metalama.LinqPad</Namespace>
</Query>

// Visualizes the distribution of method complexity across your codebase.
// Groups methods by their syntax node count into complexity buckets (Simple to Very High)
// and displays the results as a chart.

WorkspaceCollection.Default
    .WithServices(s => s.AddMetrics())  // Enable metrics computation
    .Load(@"%METALAMA_DEMO_SOLUTION%")
    .SourceCode
    .Methods
    .Select(m => m.Metrics().Get<SyntaxNodesCount>().Value)
    .GroupBy(complexity => complexity switch
    {
        < 10 => "1-9 (Simple)",
        < 25 => "10-24 (Low)",
        < 50 => "25-49 (Medium)",
        < 100 => "50-99 (High)",
        _ => "100+ (Very High)"
    })
    .Select(g => new { Complexity = g.Key, Count = g.Count() })
    .OrderBy(x => x.Complexity)
    .Chart(x => x.Complexity, x => x.Count)
