// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Collections;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CompileTime.Serialization.Serializers;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Metrics;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using TypedConstant = Metalama.Framework.Code.TypedConstant;

namespace Metalama.Framework.Engine.CodeModel;

internal sealed class DeserializedAttribute : IAttributeImpl
{
    private readonly AttributeSerializationData _serializationData;

    public DeserializedAttribute( AttributeSerializationData serializationData, CompilationModel compilation )
    {
        this._serializationData = serializationData;
        this._compilation = compilation;
    }

    string IDisplayable.ToDisplayString( CodeDisplayFormat? format, CodeDisplayContext? context ) => throw new NotImplementedException();

    private readonly CompilationModel _compilation;

    public ICompilationElement Translate(
        CompilationModel newCompilation,
        IGenericContext? genericContext = null,
        Type? interfaceType = null )
        => throw new NotImplementedException();

    ICompilation ICompilationElement.Compilation => this._compilation;

    ImmutableArray<SyntaxReference> IDeclarationImpl.DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;

    bool IDeclarationImpl.CanBeInherited => false;

    SyntaxTree? IDeclarationImpl.PrimarySyntaxTree => null;

    IEnumerable<IDeclaration> IDeclarationImpl.GetDerivedDeclarations( DerivedTypesOptions options ) => [];

    DeclarationImplementationKind IDeclarationImpl.ImplementationKind => DeclarationImplementationKind.DeserializedAttribute;

    bool IEquatable<IDeclaration>.Equals( IDeclaration? other ) => throw new NotImplementedException();

    [Memo]
    public IDeclaration ContainingDeclaration => this._serializationData.ContainingDeclaration.GetTarget( this._compilation );

    [Memo]
    private AttributeRef AttributeRef => new DeserializedAttributeRef( this._serializationData );

    IRef<IDeclaration> IDeclaration.ToRef() => this.AttributeRef;

    IRef<IAttribute> IAttribute.ToRef() => this.AttributeRef;

    SerializableDeclarationId IDeclaration.ToSerializableId() => throw new NotSupportedException();

    IAssembly IDeclaration.DeclaringAssembly => this.ContainingDeclaration.DeclaringAssembly;

    IDeclarationOrigin IDeclaration.Origin => DeclarationOrigin.External;

    IAttributeCollection IDeclaration.Attributes => AttributeCollection.Empty;

    DeclarationKind ICompilationElement.DeclarationKind => DeclarationKind.Attribute;

    bool IDeclaration.IsImplicitlyDeclared => false;

    int IDeclaration.Depth => this.ContainingDeclaration.Depth + 1;

    bool IDeclaration.BelongsToCurrentProject => this.ContainingDeclaration.BelongsToCurrentProject;

    ImmutableArray<SourceReference> IDeclaration.Sources => ImmutableArray<SourceReference>.Empty;

    public IGenericContext GenericContext => this.ContainingDeclaration.GenericContext;

    [Memo]
    public INamedType Type => this._serializationData.Type.GetTarget( this._compilation );

    [Memo]
    public IConstructor Constructor => this._serializationData.Constructor.GetTarget( this._compilation );

    [Memo]
    public ImmutableArray<TypedConstant> ConstructorArguments
        => this._serializationData.ConstructorArguments.SelectAsImmutableArray( x => x.ToTypedConstant( this._compilation ) );

    [Memo]
    public INamedArgumentList NamedArguments
        => new NamedArgumentList(
            this._serializationData.NamedArguments.SelectAsMutableList(
                x => new KeyValuePair<string, TypedConstant>( x.Key, x.Value.ToTypedConstant( this._compilation ) ) ) );

    int IAspectPredecessor.PredecessorDegree => 0;

    IRef<IDeclaration> IAspectPredecessor.TargetDeclaration => this.ContainingDeclaration.ToRef();

    ImmutableArray<AspectPredecessor> IAspectPredecessor.Predecessors => ImmutableArray<AspectPredecessor>.Empty;

    FormattableString IAspectPredecessorImpl.FormatPredecessor( ICompilation compilation )
        => $"Attribute of type '{this._serializationData.Type}' on '{this._serializationData.ContainingDeclaration}'";

    Location? IAspectPredecessorImpl.GetDiagnosticLocation( Compilation compilation ) => null;

    int IAspectPredecessorImpl.TargetDeclarationDepth => 0;

    ImmutableArray<SyntaxTree> IAspectPredecessorImpl.PredecessorTreeClosure => ImmutableArray<SyntaxTree>.Empty;

    Location? ISdkDeclaration.DiagnosticLocation => null;

    T IMeasurableInternal.GetMetric<T>() => throw new NotSupportedException();

    CompilationModel ICompilationElementImpl.Compilation => this._compilation;
}