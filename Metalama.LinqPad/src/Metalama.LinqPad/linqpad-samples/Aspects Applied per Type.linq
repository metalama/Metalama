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
  <Namespace>Metalama.LinqPad</Namespace>
</Query>

// Shows how many times each aspect is applied in your solution.
// Groups aspect instances by aspect class and shows sample targets.
// Requires: Metalama aspects in your solution

WorkspaceCollection.Default.Load(@"%METALAMA_DEMO_SOLUTION%")
    .AspectInstances
    .GroupBy(a => a.AspectClass.FullName)
    .Select(g => new { Aspect = g.Key, Count = g.Count(), Targets = g.Select(a => a.TargetDeclaration.ToString()).Take(10) })
    .OrderByDescending(x => x.Count)
