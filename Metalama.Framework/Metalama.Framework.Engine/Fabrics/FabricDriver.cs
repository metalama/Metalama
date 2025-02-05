// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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

    protected record struct CreationData(
        Fabric Fabric,
        FabricManager FabricManager,
        CompileTimeProject CompileTimeProject,
        INamedTypeSymbol FabricType,
        string OriginalPath,
        Compilation Compilation );

    protected static CreationData GetCreationData(
        FabricManager fabricManager,
        CompileTimeProject compileTimeProject,
        Fabric fabric,
        Compilation runTimeCompilation )
    {
        var originalPath = fabric.GetType().GetCustomAttribute<OriginalPathAttribute>().AssertNotNull().Path;

        // Get the original symbol for the fabric. If it has been moved, we have a custom attribute.
        var originalId = fabric.GetType().GetCustomAttribute<OriginalIdAttribute>()?.Id;

        INamedTypeSymbol symbol;

        if ( originalId != null )
        {
            symbol = (INamedTypeSymbol) DocumentationCommentId.GetFirstSymbolForDeclarationId( originalId, runTimeCompilation ).AssertSymbolNotNull();
        }
        else
        {
            symbol = (INamedTypeSymbol) runTimeCompilation.GetCompilationContext()
                .ReflectionMapper
                .GetTypeSymbol( fabric.GetType() );
        }

        return new CreationData( fabric, fabricManager, compileTimeProject, symbol, originalPath, runTimeCompilation );
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