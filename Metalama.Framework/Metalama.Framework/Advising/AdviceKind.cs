// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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