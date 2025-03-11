// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Advising;

/// <summary>
/// Enumerates the kinds of advice.
/// </summary>
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
    IntroduceNamespace
}