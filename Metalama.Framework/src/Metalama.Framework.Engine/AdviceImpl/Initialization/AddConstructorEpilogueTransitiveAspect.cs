// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Services;

namespace Metalama.Framework.Engine.AdviceImpl.Initialization;

/// <summary>
/// System-internal transitive aspect that propagates the <c>OnConstructed</c> epilogue obligation
/// across project boundaries. Registered from <see cref="OnConstructedMethodAdvice"/> alongside
/// <c>PullConstructorParameterTransitiveAspect</c>; both are reapplied to the base type in dependent
/// projects, with the pull aspect running first (system-layer ordering) so that this aspect can
/// observe whatever <c>InitializationContext</c> parameter the pull may have introduced.
/// </summary>
/// <remarks>
/// This aspect is intentionally independent from <c>PullConstructorParameterTransitiveAspect</c>:
/// even when the pull is a no-op (because the constructor already has an <c>InitializationContext</c>
/// parameter from another source), this aspect still emits the epilogue. It is also independent from
/// the user aspect being marked <c>[Inheritable]</c>: the epilogue propagates regardless, because the
/// user template body is not re-run on derived types — only the inherited <c>OnConstructed</c> method
/// is dispatched virtually from the derived constructor's epilogue.
/// </remarks>
internal sealed partial class AddConstructorEpilogueTransitiveAspect : IAspect<INamedType>
{
    void IEligible<INamedType>.BuildEligibility( IEligibilityBuilder<INamedType> builder ) { }

    public void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // Every secondary instance of this aspect on a given target type would emit the identical
        // per-derived-type transformation on `builder.Target`, so a single call suffices regardless of
        // how many user aspects on the base type registered this transitive aspect.
        var internalBuilder = (AspectBuilder<INamedType>) builder;
        internalBuilder.AdviceFactory.EmitOnConstructedEpilogueOnDerivedTypes( builder.Target );
    }

    public static IBoundAspectClass CreateAspectClass( in ProjectServiceProvider serviceProvider, CompilationModel compilation )
        => new SystemAspectClass(
            serviceProvider,
            compilation,
            $"<{nameof(AddConstructorEpilogueTransitiveAspect)}>",
            typeof(AddConstructorEpilogueTransitiveAspect) );
}