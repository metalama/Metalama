// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.CompileTime;

/// <summary>
/// This class provides a <see cref="ITemplateReflectionContext"/> that can be cached because the <see cref="Compilation"/>
/// stores only references to <see cref="PortableExecutableReference"/>, and nothing that holds a <see cref="SyntaxTree"/>.
/// </summary>
/// <remarks>
/// <para>
/// A template declared in a referenced assembly cannot be discovered against the compilation that the compiler supplies,
/// because that compilation imports only the public and protected members of its references, and a template is
/// frequently neither. The compilation created here therefore sets <see cref="MetadataImportOptions.All"/>, which cannot
/// be set on the compilation of the project itself without changing the accessibility rules that apply to the code of
/// the user.
/// </para>
/// <para>
/// The current instance holds the references and the options of the compilation it is built from, but not the
/// compilation itself. The compilation of a project is replaced on every keystroke at design time, whereas an instance
/// of this class is reachable from the pipeline configuration, which is reused across keystrokes, so holding one would
/// pin a version of the project that the user has already replaced. See issue #1808.
/// </para>
/// </remarks>
internal sealed class CacheableTemplateDiscoveryContextProvider
{
    private readonly ImmutableArray<PortableExecutableReference> _references;
    private readonly CSharpCompilationOptions? _compilationOptions;
    private readonly Lazy<CacheableContext?> _lazyImpl;
    private readonly ProjectServiceProvider _serviceProvider;
    private bool _mustEnlargeVisibility;

    public CacheableTemplateDiscoveryContextProvider( Compilation compilation, in ProjectServiceProvider serviceProvider )
    {
        this._references = compilation.References.OfType<PortableExecutableReference>().ToImmutableArray();
        this._compilationOptions = (CSharpCompilationOptions?) compilation.Options.WithMetadataImportOptions( MetadataImportOptions.All );
        this._serviceProvider = serviceProvider;

        this._lazyImpl = new Lazy<CacheableContext?>( this.CreateContext );
    }

    public void OnPortableExecutableReferenceDiscovered() => this._mustEnlargeVisibility = true;

    private CacheableContext? CreateContext()
    {
        if ( this._mustEnlargeVisibility )
        {
            Compilation compilation = CSharpCompilation.Create(
                nameof(CacheableTemplateDiscoveryContextProvider),
                references: this._references,
                options: this._compilationOptions );

            return new CacheableContext( compilation.GetCompilationContext(), this._serviceProvider );
        }
        else
        {
            // If we don't have external aspect PE references, we don't need a cacheable ITemplateReflectionContext.
            // We can always use the source context.
            return null;
        }
    }

    public ITemplateReflectionContext? GetTemplateDiscoveryContext() => this._lazyImpl.Value;

    private sealed class CacheableContext : ITemplateReflectionContext
    {
        private readonly Lazy<CompilationModel> _compilationModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheableContext"/> class.
        /// </summary>
        /// <remarks>
        /// The service provider is passed rather than the <see cref="CacheableTemplateDiscoveryContextProvider"/> that
        /// creates the instance, and is captured directly by the closure below, so that neither the instance nor the
        /// closure reaches the provider. That is what keeps the current instance cacheable in the sense stated by
        /// <see cref="IsCacheable"/>.
        /// </remarks>
        public CacheableContext( CompilationContext compilationContext, in ProjectServiceProvider serviceProvider )
        {
            this.CompilationContext = compilationContext;

            var capturedServiceProvider = serviceProvider;

            this._compilationModel = new Lazy<CompilationModel>(
                () => CompilationModel.CreateInitialInstance(
                    new ProjectModel( compilationContext.Compilation, capturedServiceProvider ),
                    compilationContext.Compilation,
                    new CompilationModelOptions( true ),
                    "CacheableTemplateDiscoveryContextProvider" ) );
        }

        public Compilation Compilation => this.CompilationContext.Compilation;

        public CompilationContext CompilationContext { get; }

        public CompilationModel GetCompilationModel( ICompilation sourceCompilation ) => this._compilationModel.Value;

        public bool IsCacheable => true;

        public override string ToString() => nameof(CacheableContext);
    }
}
