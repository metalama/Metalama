// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising.PullStrategies;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.AdviceImpl.Introduction.Constructors;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.RunTime;
using System.Linq;
using RefKind = Metalama.Framework.Code.RefKind;
using TypedConstant = Metalama.Framework.Code.TypedConstant;

namespace Metalama.Framework.Engine.AdviceImpl.Initialization;

/// <summary>
/// Helper that introduces (or reuses) an <c>InitializationContext</c> parameter on a constructor and
/// recursively pulls it through constructor chains using the existing
/// <see cref="PullConstructorParameterAdviceImpl"/> infrastructure. Also registers the transitive aspect
/// so that derived types in other projects pick up the parameter.
/// </summary>
internal static class InitializationContextParameterHelper
{
    /// <summary>
    /// Ensures that <paramref name="sourceConstructor"/> has an <c>InitializationContext</c> parameter.
    /// If an existing parameter of the given type is found, its name is returned. Otherwise an implicit
    /// constructor is materialized as needed, a new parameter is introduced with
    /// <c>default(InitializationContext)</c> as its default value, and the parameter is pulled recursively
    /// through <c>:this(...)</c> / <c>:base(...)</c> chains (and across project boundaries via the
    /// transitive aspect mechanism).
    /// </summary>
    /// <returns>
    /// The (possibly materialized) constructor and the name of the <c>InitializationContext</c> parameter.
    /// </returns>
    public static (IConstructor Constructor, string ContextParameterName) EnsureContextParameter(
        IConstructor sourceConstructor,
        IType initializationContextType,
        AdviceImplementationContext context,
        AspectLayerInstance aspectLayerInstance,
        string defaultParameterName )
    {
        // 1. Look for an existing parameter of type InitializationContext on this constructor.
        var existing = sourceConstructor.Parameters.FirstOrDefault(
            p => p.Type.Equals( initializationContextType ) );

        if ( existing != null )
        {
            return (sourceConstructor, existing.Name);
        }

        // 2. Materialize implicit constructor to an explicit one if needed.
        var constructor = sourceConstructor;

        if ( constructor.IsImplicitInstanceConstructor() )
        {
            var constructorBuilder = new ConstructorBuilder( aspectLayerInstance, constructor );
            constructorBuilder.Freeze();
            context.AddTransformation( constructorBuilder.CreateTransformation() );
            constructor = constructorBuilder;
        }

        // 3. Introduce the parameter.
        var parameterBuilder = new ParameterBuilder(
            constructor,
            constructor.Parameters.Count,
            defaultParameterName,
            initializationContextType,
            RefKind.None,
            aspectLayerInstance ) { DefaultValue = TypedConstant.Default( initializationContextType ) };

        if ( constructor.CanBeChainedFromOutsideAssembly() )
        {
            parameterBuilder.AddAttribute( AttributeConstruction.Create( typeof(AspectGeneratedAttribute) ) );
        }

        parameterBuilder.Freeze();

        context.AddTransformation(
            new IntroduceParameterTransformation( aspectLayerInstance, parameterBuilder.BuilderData ) );

        // 4. Recursively pull into constructors that chain to this one. Passing an
        //    IntroduceParameterPullStrategy enables cross-project propagation via
        //    PullConstructorParameterTransitiveAspect registered inside PullConstructorParameterAdviceImpl.
        //    The default value is serialized as text; a typed `default(T)` is used so that
        //    TypedConstant.TryConvertFromExpression recognizes it as a DefaultExpressionSyntax
        //    (rather than as a DefaultLiteralExpression whose token value is the string "default").
        var defaultValueText = $"default(global::{typeof(Framework.RunTime.Initialization.InitializationContext).FullName})";
        var pullStrategy = new IntroduceParameterPullStrategy( defaultParameterName, initializationContextType.ToRef(), defaultValueText );

        var pullImpl = new PullConstructorParameterAdviceImpl(
            context,
            pullStrategy,
            aspectLayerInstance,
            onlyProcessDerivedTypes: false );

        pullImpl.PullConstructorParameterRecursive( parameterBuilder );

        return (constructor, defaultParameterName);
    }
}
