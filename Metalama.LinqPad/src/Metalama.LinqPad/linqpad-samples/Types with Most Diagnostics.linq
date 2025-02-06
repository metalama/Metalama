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

Workspace.Load(@"C:\src\metalama\Metalama.sln")
    .Diagnostics
	.Where(d => d.Severity >= Metalama.Framework.Diagnostics.Severity.Warning)
	.GroupBy(g => (g.Declaration switch
	{
		IMemberOrNamedType memberOrNamedType => memberOrNamedType.DeclaringType ?? memberOrNamedType,
		_ => null
	})?.ToString() ?? "<null>" )
	.Where( g => g.Key != null )
	.Select(x => new { Declaration = x.Key, Count = x.Count() })
	.OrderBy(g => g.Count)
	.Chart(x => x.Declaration.ToString(), x => x.Count)