// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using RoslynProject = Microsoft.CodeAnalysis.Project;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

public static class WorkspaceHelper
{
    static WorkspaceHelper()
    {
        CSharpWorkspacesAssembly = LoadRoslynAssembly( "Microsoft.CodeAnalysis.CSharp.Workspaces" );
    }

    private static Assembly LoadRoslynAssembly( string name )
    {
        var referencedWorkspaceAssemblyName =
            typeof(ContextualSyntaxGenerator).Assembly.GetReferencedAssemblies()
                .Single( a => string.Equals( a.Name, "Microsoft.CodeAnalysis.Workspaces", StringComparison.OrdinalIgnoreCase ) );

        var requiredWorkspaceImplementationAssemblyName = new AssemblyName(
            referencedWorkspaceAssemblyName.ToString().ReplaceOrdinal( "Microsoft.CodeAnalysis.Workspaces", name ) );

        // See if the assembly is already loaded in the AppDomain.
        var assembly = AppDomainUtility
            .GetLoadedAssemblies( a => AssemblyName.ReferenceMatchesDefinition( requiredWorkspaceImplementationAssemblyName, a.GetName() ) )
            .MaxByOrNull( a => a.GetName().Version );

        if ( assembly != null )
        {
            return assembly;
        }

        // If we must load the assembly, we load the same version as the workspace assembly.
        var workspaceAssembly = typeof(Workspace).Assembly;

        // ReSharper disable once RedundantSuppressNullableWarningExpression
        var workspaceImplementationAssemblyName = workspaceAssembly.FullName!.ReplaceOrdinal(
            workspaceAssembly.GetName().Name!,
            name );

        return Assembly.Load( workspaceImplementationAssemblyName );
    }

    internal static Assembly CSharpWorkspacesAssembly { get; }

    public static async Task<(RoslynProject Project, Dictionary<SyntaxTree, DocumentId> SyntaxTreeMap)> CreateProjectFromCompilationAsync(
        Compilation compilation,
        CancellationToken cancellationToken )
    {
        Dictionary<SyntaxTree, DocumentId> syntaxTreeMap = new();
        var workspace = new AdhocWorkspace();

        var parseOptions = compilation.SyntaxTrees.FirstOrDefault()?.Options;

        var projectId = ProjectId.CreateNewId( compilation.AssemblyName );

        var projectInfo = ProjectInfo.Create(
            projectId,
            VersionStamp.Default,
            compilation.AssemblyName!,
            compilation.AssemblyName!,
            compilation.Language,
            parseOptions: parseOptions,
            compilationOptions: compilation.Options,
            metadataReferences: compilation.References );

        var project = workspace.AddProject( projectInfo );

        foreach ( var syntaxTree in compilation.SyntaxTrees )
        {
            // It is critical to use an overload that accepts a SyntaxNode, so we don't loose the annotations used later during formatting.
            var document = project.AddDocument( syntaxTree.FilePath, await syntaxTree.GetRootAsync( cancellationToken ) );
            syntaxTreeMap.Add( syntaxTree, document.Id );
            project = document.Project;
        }

        return (project, syntaxTreeMap);
    }
}