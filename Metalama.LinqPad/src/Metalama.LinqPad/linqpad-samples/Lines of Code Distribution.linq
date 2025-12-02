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

// Visualizes the distribution of method sizes by lines of code.
// Groups methods into size buckets and displays the results as a chart.
// Lines of code excludes blank lines and comments.

WorkspaceCollection.Default
    .WithServices(s => s.AddMetrics())  // Enable metrics computation
    .Load(@"%METALAMA_DEMO_SOLUTION%")
    .SourceCode
    .Methods
    .Select(m => m.Metrics().Get<LinesOfCode>().Logical)
    .GroupBy(loc => loc switch
    {
        <= 5 => "1-5 (Tiny)",
        <= 10 => "6-10 (Small)",
        <= 20 => "11-20 (Medium)",
        <= 50 => "21-50 (Large)",
        _ => "50+ (Very Large)"
    })
    .Select(g => new { Size = g.Key, Count = g.Count() })
    .OrderBy(x => x.Size)
    .Chart(x => x.Size, x => x.Count)
