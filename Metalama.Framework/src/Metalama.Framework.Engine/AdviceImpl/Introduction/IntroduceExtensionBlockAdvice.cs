// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if ROSLYN_5_0_0_OR_GREATER
using K4os.Hash.xxHash;
using Metalama.Framework.Advising;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.Diagnostics;
using System;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction;

/// <summary>
/// Advice for introducing extension blocks into static classes.
/// </summary>
internal sealed class IntroduceExtensionBlockAdvice : IntroduceDeclarationAdvice<IExtensionBlock, ExtensionBlockBuilder>
{
    private readonly IType _receiverType;
    private readonly string? _receiverParameterName;

    public override AdviceKind AdviceKind => AdviceKind.IntroduceExtensionBlock;

    public IntroduceExtensionBlockAdvice(
        in AdviceConstructorParameters<INamedType> parameters,
        IType receiverType,
        string? receiverParameterName,
        Action<ExtensionBlockBuilder>? buildAction )
        : base( parameters, buildAction )
    {
        this._receiverType = receiverType;
        this._receiverParameterName = receiverParameterName;
    }

    protected override ExtensionBlockBuilder CreateBuilder()
    {
        var targetType = (INamedType) this.TargetDeclaration.AssertNotNull();

        return new ExtensionBlockBuilder(
            this.AspectLayerInstance,
            targetType,
            this._receiverType,
            this._receiverParameterName );
    }

    protected override IntroductionAdviceResult<IExtensionBlock> ImplementCore(
        ExtensionBlockBuilder builder,
        AdviceImplementationContext context )
    {
        var targetType = (INamedType) this.TargetDeclaration.ForCompilation( context.MutableCompilation );

        // Validate: target must be a static class
        if ( !targetType.IsStatic )
        {
            return this.CreateFailedResult(
                AdviceDiagnosticDescriptors.ExtensionBlockTargetMustBeStaticClass.CreateRoslynDiagnostic(
                    targetType.GetDiagnosticLocation(),
                    (this.AspectInstance.AspectClass.ShortName, builder.ReceiverParameterBuilder.Type, targetType),
                    this ) );
        }

        // Validate: target cannot be an extension block
        if ( targetType is IExtensionBlock )
        {
            return this.CreateFailedResult(
                AdviceDiagnosticDescriptors.CannotIntroduceExtensionBlockIntoExtensionBlock.CreateRoslynDiagnostic(
                    targetType.GetDiagnosticLocation(),
                    (this.AspectInstance.AspectClass.ShortName, builder.ReceiverParameterBuilder.Type, targetType),
                    this ) );
        }

        // No uniqueness check - multiple extension blocks with the same receiver are allowed.

        // Get ordering values for deterministic naming.
        var orders = context.GetNextAdviceOrderIndices();

        // Set deterministic name if not user-defined.
        if ( string.IsNullOrEmpty( builder.Name ) )
        {
            XXH64 hash = new();
            hash.Update( orders.OrderWithinPipeline );
            hash.Update( orders.OrderWithinPipelineStepAndType );
            hash.Update( orders.OrderWithinPipelineStepAndTypeAndAspectInstance );
            builder.Name = $"Extension_{(ushort) hash.Digest():x4}";
        }

        builder.Freeze();

        var transformation = builder.CreateTransformation();
        transformation.SetAdviceOrderingIndices( orders );
        context.AddTransformationWithoutSettingOrders( transformation );

        return this.CreateSuccessResult( AdviceOutcome.Default, builder );
    }
}
#endif