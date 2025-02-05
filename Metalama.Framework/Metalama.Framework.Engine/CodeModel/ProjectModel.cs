// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Source;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Project;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel;

public sealed class ProjectModel : IProject
{
#pragma warning disable CS0618 // Type or member is obsolete
    private readonly ConcurrentDictionary<Type, ProjectExtension> _extensions = new();
#pragma warning restore CS0618 // Type or member is obsolete
    private readonly IProjectOptions _projectOptions;

    internal ProjectServiceProvider ServiceProvider { get; }

    internal ISymbolClassificationService? ClassificationService { get; }

    private bool _isFrozen;

    public ProjectModel( Compilation compilation, in ProjectServiceProvider serviceProvider ) : this(
        serviceProvider,
        compilation.ReferencedAssemblyNames,
        compilation.SyntaxTrees.FirstOrDefault()?.Options.PreprocessorSymbolNames ) { }

    internal ProjectModel(
        ProjectServiceProvider serviceProvider,
        IEnumerable<AssemblyIdentity>? references = null,
        IEnumerable<string>? preprocessorSymbolNames = null )
    {
        references ??= [];
        preprocessorSymbolNames ??= [];

        this.ClassificationService = serviceProvider.GetService<ISymbolClassificationService>();
        this._projectOptions = serviceProvider.GetRequiredService<IProjectOptions>();

        this.PreprocessorSymbols = preprocessorSymbolNames.ToImmutableHashSet();

        this.ServiceProvider = serviceProvider.Underlying;

        // Do not evaluate this property lazily because this would keep a reference to the Compilation.
        this.AssemblyReferences =
            references.Select( a => new AssemblyIdentityModel( a ) ).ToImmutableArray<IAssemblyIdentity>();
    }

    [Memo]
    public string Name => this._projectOptions.ProjectName ?? this._projectOptions.AssemblyName ?? "";

    public string AssemblyName => this._projectOptions.AssemblyName ?? "";

    public string? Path => this._projectOptions.ProjectPath;

    public ImmutableArray<IAssemblyIdentity> AssemblyReferences { get; }

    public ImmutableHashSet<string> PreprocessorSymbols { get; }

    public string? Configuration => this._projectOptions.Configuration;

    public string? TargetFramework => this._projectOptions.TargetFramework;

    public bool TryGetProperty( string name, [NotNullWhen( true )] out string? value ) => this._projectOptions.TryGetProperty( name, out value );

#pragma warning disable CS0618 // Type or member is obsolete
    public T Extension<T>()
        where T : ProjectExtension, new()
        => (T) this._extensions.GetOrAdd( typeof(T), this.CreateProjectExtension );

    private ProjectExtension CreateProjectExtension( Type t )
    {
        var data = (ProjectExtension) Activator.CreateInstance( t ).AssertNotNull();
        data.Initialize( this, this._isFrozen );

        return data;
    }
#pragma warning restore CS0618 // Type or member is obsolete

    IServiceProvider<IProjectService> IProject.ServiceProvider => this.ServiceProvider.Underlying;

    internal void Freeze()
    {
        if ( this._isFrozen )
        {
            return;
        }

        this._isFrozen = true;

        foreach ( var data in this._extensions.Values )
        {
            data.MakeReadOnly();

            // Also set the property explicitly in case an implementer skips the call to base.MakeReadOnly.
            data.IsReadOnly = true;
        }
    }

    public override string ToString() => this.Path ?? "(no project path)";
}