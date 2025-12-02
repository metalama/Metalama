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

// Shows aspects that were skipped due to errors or SkipAspect().
// Useful for debugging aspect application issues.
// Requires: Metalama aspects in your solution

WorkspaceCollection.Default.Load(@"%METALAMA_DEMO_SOLUTION%")
    .AspectInstances
    .Where(a => a.IsSkipped)
    .Select(a => new {
        Aspect = a.AspectClass.ShortName,
        Target = a.TargetDeclaration.ToString(),
        Diagnostics = a.Diagnostics.Select(d => d.Id + ": " + d.Message)
    })
