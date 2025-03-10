// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Testing.UnitTesting;

public partial class TestContext
{
    /// <summary>
    /// Creates an <see cref="ICompilation"/> made of a single source file.
    /// </summary>
    /// <param name="code">Source code.</param>
    /// <param name="dependentCode">Source code of another assembly added as a reference to the
    /// returned assembly. Optional.</param>
    /// <param name="ignoreErrors">Determines whether compilation errors should be ignored.
    /// Optional.</param>
    /// <param name="additionalReferences">Additional set of <see cref="MetadataReference"/>
    /// added to the compilation.</param>
    /// <param name="name">Name of the assembly.</param>
    /// <param name="addMetalamaReferences">Determines if Metalama assemblies should be added
    /// as references to the compilation. Optional. The default value is <c>true</c>.</param>
    /// <returns></returns>
    public ICompilation CreateCompilation(
        string code,
        string? dependentCode = null,
        bool ignoreErrors = false,
        IEnumerable<MetadataReference>? additionalReferences = null,
        string? name = null,
        bool addMetalamaReferences = true )
        => this.CreateCompilation(
            new Dictionary<string, string> { { "test.cs", code } },
            dependentCode,
            ignoreErrors,
            additionalReferences,
            name,
            addMetalamaReferences );

    /// <summary>
    /// Creates an <see cref="ICompilation"/> made of several source files.
    /// </summary>
    /// <param name="code">Source code. The key of the dictionary item is the file name, the value is
    /// the source code.</param>
    /// <param name="dependentCode">Source code of another assembly added as a reference to the
    /// returned assembly. Optional.</param>
    /// <param name="ignoreErrors">Determines whether compilation errors should be ignored.
    /// Optional.</param>
    /// <param name="additionalReferences">Additional set of <see cref="MetadataReference"/>
    /// added to the compilation.</param>
    /// <param name="name">Name of the assembly.</param>
    /// <param name="addMetalamaReferences">Determines if Metalama assemblies should be added
    /// as references to the compilation. Optional. The default value is <c>true</c>.</param>
    public ICompilation CreateCompilation(
        IReadOnlyDictionary<string, string> code,
        string? dependentCode = null,
        bool ignoreErrors = false,
        IEnumerable<MetadataReference>? additionalReferences = null,
        string? name = null,
        bool addMetalamaReferences = true )
    {
        var allAdditionalReferences = ImmutableArray<MetadataReference>.Empty;

        if ( !this.TestProjectOptions.AdditionalAssemblies.IsDefaultOrEmpty )
        {
            allAdditionalReferences = allAdditionalReferences.AddRange(
                this.TestProjectOptions.AdditionalAssemblies.Select( a => MetadataReference.CreateFromFile( a.Location ) ) );
        }

        if ( additionalReferences != null )
        {
            allAdditionalReferences = allAdditionalReferences.AddRange( additionalReferences );
        }

        var roslynCompilation = this.CreateCSharpCompilation(
            code,
            dependentCode,
            ignoreErrors,
            allAdditionalReferences,
            name,
            addMetalamaReferences );

        return CompilationModel.CreateInitialInstance(
            new ProjectModel( roslynCompilation, this.ServiceProvider ),
            roslynCompilation );
    }

    /// <summary>
    /// Creates an <see cref="ICompilation"/> from a <see cref="Compilation"/>.
    /// </summary>
    public ICompilation CreateCompilation( Compilation compilation )
        => CompilationModel.CreateInitialInstance(
            new ProjectModel( compilation, this.ServiceProvider ),
            compilation );

#pragma warning disable LAMA0821
    public CompilationModel CreateCompilationModel(
        string code,
        string? dependentCode = null,
        bool ignoreErrors = false,
        IEnumerable<MetadataReference>? additionalReferences = null,
        string? name = null,
        bool addMetalamaReferences = true )
        => (CompilationModel) this.CreateCompilation( code, dependentCode, ignoreErrors, additionalReferences, name, addMetalamaReferences );

    public CompilationModel CreateCompilationModel(
        IReadOnlyDictionary<string, string> code,
        string? dependentCode = null,
        bool ignoreErrors = false,
        IEnumerable<MetadataReference>? additionalReferences = null,
        string? name = null,
        bool addMetalamaReferences = true )
        => (CompilationModel) this.CreateCompilation( code, dependentCode, ignoreErrors, additionalReferences, name, addMetalamaReferences );

    public CompilationModel CreateCompilationModel( Compilation compilation ) => (CompilationModel) this.CreateCompilation( compilation );
#pragma warning restore LAMA0821
}