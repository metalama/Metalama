// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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