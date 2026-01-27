// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Advising;

/// <summary>
/// Enumerates the different types of code transformations (advice) that can be applied by aspects.
/// Each advice kind corresponds to a specific transformation operation available through <see cref="AdviserExtensions"/>.
/// </summary>
/// <seealso cref="IAdviceResult"/>
/// <seealso cref="AdviserExtensions"/>
/// <seealso href="@advising-code"/>
[CompileTime]
public enum AdviceKind
{
    None,
    OverrideMethod,
    OverrideFieldOrPropertyOrIndexer,
    OverrideEvent,
    IntroduceMethod,
    IntroduceEvent,
    IntroduceAttribute,
    IntroduceParameter,
    IntroduceField,
    IntroduceFinalizer,

    [Obsolete( "Use IntroduceMethod with IMethodBuilder.OperatorKind instead." )]
    IntroduceOperator,

    IntroduceProperty,
    IntroduceIndexer,
    OverrideFinalizer,
    RemoveAttributes,
    AddInitializer,
    AddContract,
    ImplementInterface,
    AddAnnotation,
    IntroduceConstructor,
    OverrideConstructor,
    OverrideConstructorChainCall,
    IntroduceType,
    IntroduceNamespace,
    OverrideEventRaise,
    OverrideEventInvoke,
    PullConstructorParameter,
    IntroduceExtensionBlock
}