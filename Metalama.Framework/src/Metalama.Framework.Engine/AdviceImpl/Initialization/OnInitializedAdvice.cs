// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.AdviceImpl.InterfaceImplementation;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Diagnostics;
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

        // Resolve the IInitializable interface and its Initialize method.
        var initializableType = (INamedType) targetType.Compilation.Factory.GetTypeByReflectionType( typeof(IInitializable) );
        var interfaceMethod = initializableType.Methods.Single();
        var initContextType = targetType.Compilation.Factory.GetTypeByReflectionType( typeof(InitializationContext) );

        // First check if the target type itself already has an Initialize method (from source or from a
        // previously-introduced method by another advice in the same compilation pass). This handles the case
        // where TryFindImplementationForInterfaceMember wouldn't yet see builder-introduced overrides.
        var ownInitializeMethod = targetType.Methods
            .OfName( "Initialize" )
            .SingleOrDefault(
                m => m.Parameters.Count == 1
                     && targetType.Compilation.Comparers.Default.Equals( m.Parameters[0].Type, initContextType ) );

        if ( ownInitializeMethod != null )
        {
            // The target type itself declares Initialize — validate and use it.
            if ( (!ownInitializeMethod.IsVirtual && !ownInitializeMethod.IsOverride
                  || ownInitializeMethod.Accessibility != Accessibility.Public)
                 && !targetType.IsSealed && targetType.TypeKind == Code.TypeKind.Class )
            {
                return this.CreateFailedResult(
                    AdviceDiagnosticDescriptors.InitializeNotVirtual.CreateRoslynDiagnostic(
                        targetType.GetDiagnosticLocation(),
                        (this.AspectInstance.AspectClass.ShortName, targetType),
                        this ) );
            }

            return Succeed( ownInitializeMethod, baseMethod: null );
        }

        // Resolves or introduces the Initialize method and returns it along with the base method (if overriding).
        // Check if the target type already implements IInitializable (directly or via a base type).
        if ( targetType.TryFindImplementationForInterfaceMember( interfaceMethod, out var existingImpl ) )
        {
            var existingMethod = (IMethod) existingImpl;

            if ( targetType.Compilation.Comparers.Default.Equals( existingMethod.DeclaringType, targetType ) )
            {
                // The target type itself declares Initialize — validate and use it.
                if ( (!existingMethod.IsVirtual && !existingMethod.IsOverride
                      || existingMethod.Accessibility != Accessibility.Public)
                     && !targetType.IsSealed && targetType.TypeKind == Code.TypeKind.Class )
                {
                    return this.CreateFailedResult(
                        AdviceDiagnosticDescriptors.InitializeNotVirtual.CreateRoslynDiagnostic(
                            targetType.GetDiagnosticLocation(),
                            (this.AspectInstance.AspectClass.ShortName, targetType),
                            this ) );
                }

                return Succeed( existingMethod, baseMethod: null );
            }

            // Inherited from a base type — override it if possible.
            if ( existingMethod.IsVirtual || existingMethod.IsOverride )
            {
                return Succeed( IntroduceMethod( existingMethod ), baseMethod: existingMethod );
            }
        }

        return Succeed( IntroduceMethod( null ), baseMethod: null );

        IMethod IntroduceMethod( IMethod? baseMethodToOverride )
        {
            var builder = new MethodBuilder( this.AspectLayerInstance, targetType, "Initialize" )
            {
                ReturnType = targetType.Compilation.Factory.GetSpecialType( Code.SpecialType.Void ),
                Accessibility = Accessibility.Public
            };

            if ( baseMethodToOverride != null )
            {
                // Override the base method.
                builder.IsOverride = true;
                builder.OverriddenMethod = baseMethodToOverride;
            }
            else
            {
                // New method — virtual unless the type is sealed or a struct.
                builder.IsVirtual = !targetType.IsSealed && targetType.TypeKind != Code.TypeKind.Struct;
            }

            builder.AddParameter( "context", initContextType, defaultValue: TypedConstant.Default( initContextType ) );

            builder.Freeze();

            context.AddTransformation(
                new IntroduceMethodTransformation( this.AspectLayerInstance, builder.BuilderData ) );

            // Introduce the IInitializable interface if the type doesn't already implement it.
            if ( baseMethodToOverride == null )
            {
                context.AddTransformation(
                    new IntroduceInterfaceTransformation(
                        this.AspectLayerInstance,
                        targetType.ToFullRef(),
                        initializableType.ToFullRef(),
                        new Dictionary<IMember, IMember> { { interfaceMethod, builder } } ) );
            }

            return builder;
        }

        AddInitializerAdviceResult Succeed( IMethod initializeMethod, IMethod? baseMethod )
        {
            // Add the template body as a statement in the Initialize method.
            context.AddTransformation(
                new InsertTemplateStatementsTransformation(
                    this.AspectLayerInstance,
                    targetType.ToRef(),
                    initializeMethod.ToFullRef(),
                    this._boundTemplate ) );

            // If overriding a base method, add "base.Initialize(context.Descend(...))" as first statement.
            if ( baseMethod != null )
            {
                var slotExpression = this.CreateSlotExpression();

                context.AddTransformation(
                    new InsertSyntaxStatementsTransformation(
                        this.AspectLayerInstance,
                        targetType.ToRef(),
                        initializeMethod.ToFullRef(),
                        _ => ExpressionStatement(
                            InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    BaseExpression(),
                                    IdentifierName( "Initialize" ) ),
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

            return new AddInitializerAdviceResult( AdviceOutcome.Success, this.AdviceFactory );
        }
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
                SyntaxFactoryEx.CreateFullyQualifiedName( field.DeclaringType.FullName ),
                SyntaxFactoryEx.SafeIdentifierName( field.Name ) );

            result = result == null
                ? fieldAccess
                : BinaryExpression( SyntaxKind.BitwiseOrExpression, result, fieldAccess );
        }

        return result!;
    }

    public override AdviceKind AdviceKind => AdviceKind.AddInitializer;

    protected override AddInitializerAdviceResult CreateFailedResult( ImmutableArray<Diagnostic> diagnostics )
        => new( AdviceOutcome.Error, this.AdviceFactory, diagnostics );
}
