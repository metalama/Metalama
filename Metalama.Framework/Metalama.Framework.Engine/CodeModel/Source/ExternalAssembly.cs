// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel.Collections;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Source;

internal sealed class ExternalAssembly : SourceDeclaration, IAssembly
{
    private readonly IAssemblySymbol _assemblySymbol;

    public ExternalAssembly( IAssemblySymbol assemblySymbol, CompilationModel compilation ) : base( compilation, null )
    {
        this._assemblySymbol = assemblySymbol;
    }

    public override IDeclaration ContainingDeclaration => this.Compilation;

    public override DeclarationKind DeclarationKind => DeclarationKind.AssemblyReference;

    public override ISymbol Symbol => this._assemblySymbol;

    public override bool CanBeInherited => false;

    public override IEnumerable<IDeclaration> GetDerivedDeclarations( DerivedTypesOptions options = default ) => [];

    public INamespace GlobalNamespace => this.Compilation.Factory.GetNamespace( this._assemblySymbol.GlobalNamespace );

    bool IAssembly.IsExternal => true;

    [Memo]
    public IAssemblyIdentity Identity => new AssemblyIdentityModel( this._assemblySymbol.Identity );

    [Memo]
    public INamedTypeCollection Types => new ExternalAssemblyTypeCollection( this._assemblySymbol, this.Compilation, false );

    [Memo]
    public INamedTypeCollection AllTypes => new ExternalAssemblyTypeCollection( this._assemblySymbol, this.Compilation, true );

    public bool AreInternalsVisibleFrom( IAssembly assembly ) => this._assemblySymbol.AreInternalsVisibleToImpl( assembly.GetSymbol() );

    [Memo]
    public IAssemblyCollection ReferencedAssemblies => new ReferencedAssemblyCollection( this.Compilation, this._assemblySymbol.Modules.First() );

    public override SyntaxTree? PrimarySyntaxTree => null;

    public override IDeclarationOrigin Origin => DeclarationOrigin.External;

    public override IAssembly DeclaringAssembly => this;

    public override ImmutableArray<SourceReference> Sources => ImmutableArray<SourceReference>.Empty;

    public override bool BelongsToCurrentProject => false;

    [Memo]
    private IFullRef<IAssembly> Ref => this.RefFactory.FromSymbolBasedDeclaration<IAssembly>( this );

    private protected override IFullRef<IDeclaration> ToFullDeclarationRef() => this.Ref;

    IRef<IAssembly> IAssembly.ToRef() => this.Ref;
}