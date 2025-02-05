using Metalama.Framework.Workspaces;

if ( args.Length != 1 )
{
    Console.Error.WriteLine("Usage: WorkspaceTest <path-to-sln>");
    return 1;
}

var workspace = Workspace.Load(args[0]);

var references = workspace.SourceCode.Types.SelectMany( t => t.GetInboundReferences() );

Console.WriteLine($"{references.Count()} references found.");

return 0;