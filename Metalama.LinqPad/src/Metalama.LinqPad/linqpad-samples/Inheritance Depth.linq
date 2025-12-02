<Query Kind="Statements">
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

// Finds classes with deep inheritance hierarchies.
// Deep inheritance can indicate design complexity and may benefit from composition.

int GetInheritanceDepth(INamedType type)
{
    int depth = 0;
    var current = type.BaseType;
    while (current != null && current.FullName != "System.Object")
    {
        depth++;
        current = current.BaseType;
    }
    return depth;
}

WorkspaceCollection.Default.Load(@"%METALAMA_DEMO_SOLUTION%")
    .SourceCode
    .Types
    .Where(t => t.TypeKind == TypeKind.Class)
    .Select(t => new { Type = t, Depth = GetInheritanceDepth(t) })
    .Where(x => x.Depth > 0)
    .OrderByDescending(x => x.Depth)
    .Take(50)
    .Dump();
