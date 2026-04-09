// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
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

internal sealed class InitializeMethodAdvice : Advice<AddInitializerAdviceResult>
{
    // Default name given to the InitializationContext parameter when this advice introduces a new
    // Initialize method (either a fresh one or an override of an inherited one). Existing hand-authored
    // Initialize methods on the target type are used as-is, preserving their parameter name.
    private const string _defaultContextParameterName = "context";

    private readonly TemplateMember<IMethod> _template;
    private readonly IObjectReader? _templateArguments;
    private readonly IEnumerable<IField>? _slotFields;

    private new INamedType TargetDeclaration => (INamedType) base.TargetDeclaration;

    public InitializeMethodAdvice(
        in AdviceConstructorParameters<INamedType> parameters,
        TemplateMember<IMethod> template,
        IObjectReader? templateArguments,
        IEnumerable<IField>? slotFields )
        : base( parameters )
    {
        this._template = template;
        this._templateArguments = templateArguments;
        this._slotFields = slotFields;
    }

    protected override AddInitializerAdviceResult Implement( AdviceImplementationContext context )
    {
        var targetType = this.TargetDeclaration.ToRef().GetTarget( context.MutableCompilation );
        var factory = targetType.Compilation.Factory;

        // Resolve the IInitializable interface and its Initialize method.
        var initializableType = (INamedType) factory.GetTypeByReflectionType( typeof(IInitializable) );
        var interfaceMethod = initializableType.Methods.Single();
        var initContextType = factory.GetTypeByReflectionType( typeof(InitializationContext) );

        // First check if the target type itself already has an Initialize method (from source or from a
        // previously-introduced method by another advice in the same compilation pass). This handles the case
        // where TryFindImplementationForInterfaceMember wouldn't yet see builder-introduced overrides.
        var ownInitializeMethod = targetType.Methods
            .OfName( nameof(IInitializable.Initialize) )
            .SingleOrDefault(
                m => m.Parameters.Count == 1
                     && m.Parameters[0].Type.Equals( initContextType ) );

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

            // Ensure the type implements IInitializable so that the linker will wrap call sites.
            // If the user hand-authored a correctly-shaped Initialize method but didn't declare the interface,
            // the linker's InitializableTypeRegistry won't find the type and won't wrap `new T()` call sites.
            if ( !targetType.TryFindImplementationForInterfaceMember( interfaceMethod, out _ ) )
            {
                context.AddTransformation(
                    new IntroduceInterfaceTransformation(
                        this.AspectLayerInstance,
                        targetType.ToFullRef(),
                        initializableType.ToFullRef(),
                        new Dictionary<IMember, IMember> { { interfaceMethod, ownInitializeMethod } } ) );
            }

            return Succeed( ownInitializeMethod, baseMethod: null );
        }

        // Resolves or introduces the Initialize method and returns it along with the base method (if overriding).
        // Check if the target type already implements IInitializable (directly or via a base type).
        if ( targetType.TryFindImplementationForInterfaceMember( interfaceMethod, out var existingImpl ) )
        {
            var existingMethod = (IMethod) existingImpl;

            if ( existingMethod.DeclaringType.Equals( targetType ) )
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
            var builder = new MethodBuilder( this.AspectLayerInstance, targetType, nameof(IInitializable.Initialize) )
            {
                ReturnType = factory.GetSpecialType( Code.SpecialType.Void ),
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

            builder.AddParameter( _defaultContextParameterName, initContextType, defaultValue: TypedConstant.Default( initContextType ) );

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
            // Bind the template against the resolved Initialize method so that any run-time
            // InitializationContext parameter on the template is mapped by name to the method's
            // own context parameter (hand-authored or introduced).
            var boundTemplate = this._template.ForInitializer( initializeMethod, this._templateArguments );

            // Add the template body as a statement in the Initialize method.
            context.AddTransformation(
                new InsertTemplateStatementsTransformation(
                    this.AspectLayerInstance,
                    targetType.ToRef(),
                    initializeMethod.ToFullRef(),
                    boundTemplate ) );

            // If overriding a base method, add "base.Initialize(context.Descend(...))" as first statement.
            if ( baseMethod != null )
            {
                var slotExpression = this.CreateSlotExpression();
                var contextParameterName = initializeMethod.Parameters[0].Name;

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
                                    IdentifierName( nameof(IInitializable.Initialize) ) ),
                                ArgumentList(
                                    SingletonSeparatedList(
                                        Argument(
                                            InvocationExpression(
                                                MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    SyntaxFactoryEx.SafeIdentifierName( contextParameterName ),
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
