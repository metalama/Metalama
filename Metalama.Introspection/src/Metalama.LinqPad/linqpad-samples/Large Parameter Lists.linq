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

// Finds methods with more than 5 parameters.
// Long parameter lists can indicate a method doing too much or a missing abstraction.

WorkspaceCollection.Default.Load(@"%METALAMA_DEMO_SOLUTION%")
    .SourceCode
    .Methods
    .Where(m => m.Parameters.Count > 5)
    .Select(m => new {
        Method = m,
        ParameterCount = m.Parameters.Count,
        Parameters = string.Join(", ", m.Parameters.Select(p => p.Type.ToDisplayString() + " " + p.Name))
    })
    .OrderByDescending(x => x.ParameterCount)
    .Take(50)
