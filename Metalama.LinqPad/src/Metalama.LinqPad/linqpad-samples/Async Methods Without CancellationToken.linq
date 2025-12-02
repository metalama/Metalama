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

// Finds async methods that don't accept a CancellationToken parameter.
// These methods cannot be cancelled, which may cause issues in long-running operations.

WorkspaceCollection.Default.Load(@"%METALAMA_DEMO_SOLUTION%")
    .SourceCode
    .Methods
    .Where(m => m.IsAsync)
    .Where(m => !m.Parameters.Any(p => p.Type.Equals(typeof(System.Threading.CancellationToken))))
    .Select(m => new { Method = m, DeclaringType = m.DeclaringType.FullName })
    .OrderBy(x => x.DeclaringType)
