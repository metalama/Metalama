// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.RunTime.Initialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Accessibility = Metalama.Framework.Code.Accessibility;
using TypedConstant = Metalama.Framework.Code.TypedConstant;

namespace Metalama.Framework.Engine.AdviceImpl.Initialization;

internal sealed class OnInitializedAdvice : Advice<AddInitializerAdviceResult>
{
    private readonly BoundTemplateMethod _boundTemplate;
    private readonly IEnumerable<IField>? _slotFields;

    private new INamedType TargetDeclaration => (INamedType) base.TargetDeclaration;

    public OnInitializedAdvice(
        in AdviceConstructorParameters<INamedType> parameters,
        BoundTemplateMethod boundTemplate,
        IEnumerable<IField>? slotFields )
        : base( parameters )
    {
        this._boundTemplate = boundTemplate;
        this._slotFields = slotFields;
    }

    protected override AddInitializerAdviceResult Implement( AdviceImplementationContext context )
    {
        var targetType = this.TargetDeclaration.ToRef().GetTarget( context.MutableCompilation );

        // Check if target type already has an OnInitialized method (from prior advice or hand-authored).
        var existingMethod = FindOnInitializedMethod( targetType );

        IMethod onInitializedMethod;

        if ( existingMethod == null )
        {
            // Introduce the OnInitialized method.
            var builder = new MethodBuilder( this.AspectLayerInstance, targetType, "OnInitialized" );

            // Check base for an existing overridable OnInitialized method.
            var baseMethod = FindOverridableOnInitializedMethodInBase( targetType );

            // Return type = declaring type (covariant return), unless the runtime doesn't support it.
            if ( baseMethod != null && !targetType.Compilation.Project.Features.SupportsCovariantReturnTypes )
            {
                builder.ReturnType = baseMethod.ReturnType;
            }
            else
            {
                builder.ReturnType = targetType;
            }

            builder.Accessibility = Accessibility.Public;

            // Virtual unless the type is sealed or a struct.
            if ( !targetType.IsSealed && targetType.TypeKind != Code.TypeKind.Struct )
            {
                builder.IsVirtual = true;
            }

            if ( baseMethod != null )
            {
                builder.IsOverride = true;
                builder.IsVirtual = false;
                builder.OverriddenMethod = baseMethod;
            }

            // Add parameter: InitializationContext context = default.
            var initContextType = targetType.Compilation.Factory.GetTypeByReflectionType( typeof(InitializationContext) );
            builder.AddParameter( "context", initContextType, defaultValue: TypedConstant.Default( initContextType ) );

            // Add [OnInitialized] attribute.
            builder.AddAttribute( AttributeConstruction.Create( typeof(OnInitializedAttribute) ) );

            builder.Freeze();

            context.AddTransformation(
                new IntroduceMethodTransformation( this.AspectLayerInstance, builder.BuilderData )
                {
                    DefaultReturnExpression = ThisExpression()
                } );

            onInitializedMethod = builder;
        }
        else
        {
            onInitializedMethod = existingMethod;
        }

        // Add the template body as a statement in the OnInitialized method.
        context.AddTransformation(
            new InsertTemplateStatementsTransformation(
                this.AspectLayerInstance,
                targetType.ToRef(),
                onInitializedMethod.ToFullRef(),
                this._boundTemplate ) );

        if ( existingMethod == null )
        {
            // If base has [OnInitialized], add "base.OnInitialized(context.Descend(...))" as first statement.
            // Uses InitializerPrologue to guarantee it precedes all template-injected statements.
            var baseMethod = FindOverridableOnInitializedMethodInBase( targetType );

            if ( baseMethod != null )
            {
                var slotExpression = this.CreateSlotExpression();

                context.AddTransformation(
                    new InsertSyntaxStatementsTransformation(
                        this.AspectLayerInstance,
                        targetType.ToRef(),
                        onInitializedMethod.ToFullRef(),
                        _ => ExpressionStatement(
                            InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    BaseExpression(),
                                    IdentifierName( "OnInitialized" ) ),
                                ArgumentList(
                                    SingletonSeparatedList(
                                        Argument(
                                            InvocationExpression(
                                                MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    IdentifierName( "context" ),
                                                    IdentifierName( "Descend" ) ),
                                                ArgumentList(
                                                    SingletonSeparatedList(
                                                        Argument( slotExpression ) ) ) ) ) ) ) ) ),
                        InsertedStatementKind.InitializerPrologue ) );
            }
        }

        return new AddInitializerAdviceResult( AdviceOutcome.Success, this.AdviceFactory );
    }

    private static IMethod? FindOnInitializedMethod( INamedType type )
    {
        return type.Methods
            .OfName( "OnInitialized" )
            .FirstOrDefault( m => m.Attributes.Any( a => a.Type.FullName == "Metalama.Framework.RunTime.Initialization.OnInitializedAttribute" ) );
    }

    private static IMethod? FindOverridableOnInitializedMethodInBase( INamedType type )
    {
        var baseType = type.BaseType;

        while ( baseType != null )
        {
            var method = FindOnInitializedMethod( baseType );

            if ( method != null )
            {
                // Only return the method if it is overridable (virtual or already an override).
                // A non-virtual/sealed base method cannot be overridden — the introduced method
                // will shadow it implicitly.
                return method.IsVirtual || method.IsOverride ? method : null;
            }

            baseType = baseType.BaseType;
        }

        return null;
    }

    /// <summary>
    /// Creates a Roslyn expression representing the combination of all slot fields using the <c>|</c> operator.
    /// If no slot fields are provided, returns <c>default(InitializationSlot)</c>.
    /// </summary>
    private ExpressionSyntax CreateSlotExpression()
    {
        var slotFields = this._slotFields?.ToList();

        if ( slotFields == null || slotFields.Count == 0 )
        {
            return LiteralExpression( SyntaxKind.DefaultLiteralExpression );
        }

        ExpressionSyntax? result = null;

        foreach ( var field in slotFields )
        {
            var fieldAccess = MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                CreateFullyQualifiedName( field.DeclaringType.FullName ),
                SyntaxFactoryEx.SafeIdentifierName( field.Name ) );

            result = result == null
                ? fieldAccess
                : BinaryExpression( SyntaxKind.BitwiseOrExpression, result, fieldAccess );
        }

        return result!;
    }

    /// <summary>
    /// Creates a <c>global::Namespace.Type</c> syntax from a fully-qualified type name.
    /// </summary>
    private static ExpressionSyntax CreateFullyQualifiedName( string fullName )
    {
        var parts = fullName.Split( '.' );

        ExpressionSyntax result = AliasQualifiedName(
            SyntaxFactoryEx.WellKnownIdentifierName( Token( SyntaxKind.GlobalKeyword ) ),
            SyntaxFactoryEx.SafeIdentifierName( parts[0] ) );

        for ( var i = 1; i < parts.Length; i++ )
        {
            result = MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                result,
                SyntaxFactoryEx.SafeIdentifierName( parts[i] ) );
        }

        return result;
    }

    public override AdviceKind AdviceKind => AdviceKind.AddInitializer;

    protected override AddInitializerAdviceResult CreateFailedResult( ImmutableArray<Diagnostic> diagnostics )
        => new( AdviceOutcome.Error, this.AdviceFactory, diagnostics );
}
