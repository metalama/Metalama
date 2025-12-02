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

// Shows code transformations performed by each aspect.
// Useful for understanding how aspects modify your codebase.
// Requires: Metalama aspects in your solution

WorkspaceCollection.Default.Load(@"%METALAMA_DEMO_SOLUTION%")
    .Transformations
    .GroupBy(t => t.Advice.AspectInstance.AspectClass.ShortName)
    .Select(g => new {
        Aspect = g.Key,
        TransformationCount = g.Count(),
        TransformationTypes = g.GroupBy(t => t.TransformationKind).Select(t => new { Kind = t.Key, Count = t.Count() })
    })
    .OrderByDescending(x => x.TransformationCount)
