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

// Summarizes your public API surface by namespace.
// Shows public types, methods, and properties per namespace.

WorkspaceCollection.Default.Load(@"%METALAMA_DEMO_SOLUTION%")
    .SourceCode
    .Types
    .Where(t => t.Accessibility == Metalama.Framework.Code.Accessibility.Public)
    .GroupBy(t => t.ContainingNamespace?.FullName ?? "(global)")
    .Select(g => new {
        Namespace = g.Key,
        TypeCount = g.Count(),
        PublicMethods = g.Sum(t => t.Methods.Count(m => m.Accessibility == Metalama.Framework.Code.Accessibility.Public)),
        PublicProperties = g.Sum(t => t.Properties.Count(p => p.Accessibility == Metalama.Framework.Code.Accessibility.Public))
    })
    .OrderByDescending(x => x.TypeCount)
