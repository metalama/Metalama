// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Fabrics;
using Microsoft.CodeAnalysis;
using System;
using System.Reflection;

namespace Metalama.Framework.Engine.Fabrics;

/// <summary>
/// The base class for fabric drivers, which are responsible for ordering and executing fabrics.
/// </summary>
internal abstract partial class FabricDriver : IComparable<FabricDriver>
{
    protected FabricManager FabricManager { get; }

    public Fabric Fabric { get; }

    public CompileTimeProject CompileTimeProject { get; }

    protected FabricDriver( CreationData creationData )
    {
        this.FabricManager = creationData.FabricManager;
        this.Fabric = creationData.Fabric;

        this.OriginalPath = creationData.OriginalPath;
        this.FabricTypeSymbolId = SymbolId.Create( creationData.FabricType );
        this.FabricTypeFullName = creationData.FabricType.GetReflectionFullName().AssertNotNull();
        this.FabricTypeShortName = creationData.FabricType.Name;
        this.DiagnosticLocation = creationData.FabricType.GetDiagnosticLocation();
        this.CompileTimeProject = creationData.CompileTimeProject;
    }

    internal record struct CreationData(
        Fabric Fabric,
        FabricManager FabricManager,
        CompileTimeProject CompileTimeProject,
        INamedTypeSymbol FabricType,
        string OriginalPath,
        Compilation Compilation );

    /// <summary>
    /// Resolves the run-time symbol of a fabric type and returns the data required to create a <see cref="FabricDriver"/>,
    /// or returns <c>false</c> when the fabric type has no counterpart in the run-time compilation.
    /// </summary>
    /// <remarks>
    /// The compile-time closure is walked through <see cref="CompileTimeProject.References"/> and referenced compile-time
    /// projects are resolved through <see cref="IAssemblyLocator"/>, so the closure can contain an assembly that the
    /// run-time compilation does not reference. The reference set can also be momentarily incomplete when the IDE
    /// analyzes a project. In both cases the fabric type cannot be resolved and the caller must report a diagnostic
    /// instead of failing. See https://github.com/metalama/Metalama/issues/1759.
    /// </remarks>
    internal static bool TryGetCreationData(
        FabricManager fabricManager,
        CompileTimeProject compileTimeProject,
        Fabric fabric,
        Compilation runTimeCompilation,
        out CreationData creationData )
    {
        var fabricType = fabric.GetType();
        var originalPath = fabricType.GetCustomAttribute<OriginalPathAttribute>().AssertNotNull().Path;

        // Get the original symbol for the fabric. If it has been moved, we have a custom attribute.
        var originalId = fabricType.GetCustomAttribute<OriginalIdAttribute>()?.Id;

        INamedTypeSymbol? symbol;

        if ( originalId != null )
        {
            symbol = DocumentationCommentId.GetFirstSymbolForDeclarationId( originalId, runTimeCompilation ) as INamedTypeSymbol;
        }
        else if ( !runTimeCompilation.GetCompilationContext().ContainsOrReferencesAssembly( compileTimeProject.RunTimeIdentity.Name ) )
        {
            // The run-time assembly that declares the fabric is not a part of the compilation, so resolving the
            // symbol would throw.
            symbol = null;
        }
        else
        {
            symbol = (INamedTypeSymbol) runTimeCompilation.GetCompilationContext()
                .ReflectionMapper
                .GetTypeSymbol( fabricType );
        }

        if ( symbol == null )
        {
            creationData = default;

            return false;
        }

        creationData = new CreationData( fabric, fabricManager, compileTimeProject, symbol, originalPath, runTimeCompilation );

        return true;
    }

    public Location? DiagnosticLocation { get; }

    public SymbolId FabricTypeSymbolId { get; }

    public string FabricTypeFullName { get; }

    protected string OriginalPath { get; }

    public abstract FabricKind Kind { get; }

    public string FabricTypeShortName { get; }

    public int CompareTo( FabricDriver? other )
    {
        if ( ReferenceEquals( this, other ) )
        {
            return 0;
        }

        if ( other == null )
        {
            return 1;
        }

        var kindComparison = this.Kind.CompareTo( other.Kind );

        if ( kindComparison != 0 )
        {
            return kindComparison;
        }

        var originalPathComparison = string.Compare( this.OriginalPath, other.OriginalPath, StringComparison.Ordinal );

        if ( originalPathComparison != 0 )
        {
            return originalPathComparison;
        }

        return this.CompareToCore( other );
    }

    protected virtual int CompareToCore( FabricDriver other )
        =>

            // This implementation is common for type and namespace fabrics. It is overwritten for project fabrics.
            // With type and namespace fabrics, having several fabrics per type or namespace is not a useful use case.
            // If that happens, we sort by name of the fabric class. They are guaranteed to have the same parent type or
            // namespace, so the symbol name is sufficient.
            string.Compare( this.FabricTypeFullName, other.FabricTypeFullName, StringComparison.Ordinal );

    public abstract FormattableString FormatPredecessor();
}