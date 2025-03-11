// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.Source;
using Metalama.Framework.Engine.ReflectionMocks;
using Metalama.Framework.Engine.Utilities;
using System.Collections.Generic;
using System.Reflection;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Introduced;

internal abstract class IntroducedPropertyOrIndexer : IntroducedMember, IPropertyOrIndexerImpl
{
    protected IntroducedPropertyOrIndexer( CompilationModel compilation, IGenericContext genericContext ) : base( compilation, genericContext ) { }

    protected abstract PropertyOrIndexerBuilderData PropertyOrIndexerBuilderData { get; }

    public RefKind RefKind => this.PropertyOrIndexerBuilderData.RefKind;

    public Writeability Writeability => this.PropertyOrIndexerBuilderData.Writeability;

    [Memo]
    public IType Type => this.MapType( this.PropertyOrIndexerBuilderData.Type ).AssertNotNull();

    [Memo]
    public IMethod? GetMethod
        => this.PropertyOrIndexerBuilderData.GetMethod != null
            ? new IntroducedAccessor( this, this.PropertyOrIndexerBuilderData.GetMethod )
            : null;

    [Memo]
    public IMethod? SetMethod
        => this.PropertyOrIndexerBuilderData.SetMethod != null
            ? new IntroducedAccessor( this, this.PropertyOrIndexerBuilderData.SetMethod )
            : null;

    IRef<IFieldOrPropertyOrIndexer> IFieldOrPropertyOrIndexer.ToRef() => this.ToFullDeclarationRef().As<IFieldOrPropertyOrIndexer>();

    public PropertyInfo ToPropertyInfo() => CompileTimePropertyInfo.Create( this );

    IRef<IPropertyOrIndexer> IPropertyOrIndexer.ToRef() => this.ToFullDeclarationRef().As<IPropertyOrIndexer>();

    public IMethod? GetAccessor( MethodKind methodKind ) => this.GetAccessorImpl( methodKind );

    public IEnumerable<IMethod> Accessors
        => (this.GetMethod, this.SetMethod) switch
        {
            (null, { } setMethod) => [setMethod],
            ({ } getMethod, null) => [getMethod],
            ({ } getMethod, { } setMethod) => [getMethod, setMethod],
            _ => []
        };
}