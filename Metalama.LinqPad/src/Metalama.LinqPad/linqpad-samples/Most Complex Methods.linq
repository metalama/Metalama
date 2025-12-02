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

// Lists the 50 most complex methods in your solution, ranked by syntax node count.
// High complexity often correlates with harder-to-maintain code.

WorkspaceCollection.Default
    .WithServices(s => s.AddMetrics())  // Enable metrics computation
    .Load(@"%METALAMA_DEMO_SOLUTION%")
    .SourceCode
    .Methods
    .Select(m => new { Method = m, Complexity = m.Metrics().Get<SyntaxNodesCount>().Value })
    .OrderByDescending(x => x.Complexity)
    .Take(50)
