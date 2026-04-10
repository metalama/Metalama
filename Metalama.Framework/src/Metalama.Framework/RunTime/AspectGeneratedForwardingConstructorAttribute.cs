// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.RunTime;

/// <summary>
/// A custom attribute added by the framework to an aspect-generated forwarding constructor: a compile-time stub that
/// keeps the pre-mutation signature of a source constructor callable and chains, via <c>: this(...)</c>, to the
/// constructor that <see cref="Metalama.Framework.Advising.IAdviceFactory.IntroduceParameter(Metalama.Framework.Code.IConstructor, string, Metalama.Framework.Code.IType, Metalama.Framework.Code.TypedConstant, Metalama.Framework.Advising.IPullStrategy?, System.Collections.Immutable.ImmutableArray{Metalama.Framework.Code.DeclarationBuilders.AttributeConstruction}, Metalama.Framework.Advising.IConstructorOverloadingStrategy?)"/>
/// has mutated with an appended parameter.
/// </summary>
/// <remarks>Aspect authors should not add this attribute directly. Use
/// <see cref="Metalama.Framework.Advising.PullStrategyExtensions.IsAspectGeneratedForwarder"/> from a custom <see cref="Metalama.Framework.Advising.IPullStrategy"/> to detect such constructors.</remarks>
[AttributeUsage( AttributeTargets.Constructor )]
public sealed class AspectGeneratedForwardingConstructorAttribute : Attribute;
