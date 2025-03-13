// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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