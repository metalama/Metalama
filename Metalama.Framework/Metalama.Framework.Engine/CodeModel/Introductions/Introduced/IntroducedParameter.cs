// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Templating.Expressions;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Metalama.Framework.Engine.CodeModel.Introductions.Introduced;

internal sealed class IntroducedParameter : IntroducedDeclaration, IParameterImpl
{
    private readonly ParameterBuilderData _parameterBuilder;

    public IntroducedParameter( ParameterBuilderData builder, CompilationModel compilation, IGenericContext genericContext, IHasParameters parent ) : base(
        compilation,
        genericContext )
    {
        // When BuiltParameter represents the return parameter of the pseudo getter of a promoted field, there is an ambiguity whether
        // the parent is the getter of the field or of the property. We resolve this ambiguity by explicitly passing the parent when we know it upfront.

        this.DeclaringMember = parent;
        this._parameterBuilder = builder;
    }

    public override DeclarationBuilderData BuilderData => this._parameterBuilder;

    public RefKind RefKind => this._parameterBuilder.RefKind;

    [Memo]
    public IType Type => this.MapType( this._parameterBuilder.Type );

    public string Name => this._parameterBuilder.Name;

    public int Index => this._parameterBuilder.Index;

    public TypedConstant? DefaultValue => this._parameterBuilder.DefaultValue?.ToTypedConstant( this.Compilation );

    public bool IsParams => this._parameterBuilder.IsParams;

    public bool IsThis => this._parameterBuilder.IsThis;

    public IHasParameters DeclaringMember { get; }

    public override IDeclaration ContainingDeclaration => this.DeclaringMember;

    public ParameterInfo ToParameterInfo() => throw new NotImplementedException();

    public bool IsReturnParameter => this.Index < 0;

    [Memo]
    private IFullRef<IParameter> Ref => this.RefFactory.FromIntroducedDeclaration<IParameter>( this );

    public IRef<IParameter> ToRef() => this.Ref;

    private protected override IFullRef<IDeclaration> ToFullDeclarationRef() => this.Ref;

    bool IExpression.IsAssignable => true;

    public ref object? Value => ref RefHelper.Wrap( new SyntaxUserExpression( SyntaxFactory.IdentifierName( this.Name ), this.Type, true ) );

    public TypedExpressionSyntax ToTypedExpressionSyntax( ISyntaxGenerationContext syntaxGenerationContext, IType? targetType = null )
        => new(
            new TypedExpressionSyntaxImpl(
                SyntaxFactory.IdentifierName( this.Name ),
                this.Type,
                ((SyntaxSerializationContext) syntaxGenerationContext).CompilationModel,
                true ) );

    public override bool CanBeInherited => ((IDeclarationImpl) this.ContainingDeclaration).CanBeInherited;

    public override IEnumerable<IDeclaration> GetDerivedDeclarations( DerivedTypesOptions options = default )
        => ((IMemberImpl) this.DeclaringMember).GetDerivedDeclarations( options )
            .Select( d => ((IHasParameters) d).Parameters[this.Index] );
}