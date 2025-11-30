// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;

namespace Metalama.Patterns.Observability.Configuration;

/// <summary>
/// Builder for configuring options specific to the classic (default) implementation strategy of the <see cref="ObservableAttribute"/> aspect.
/// </summary>
/// <remarks>
/// <para>
/// The classic strategy generates code that follows standard MVVM patterns with an <c>OnPropertyChanged</c> method
/// and support for child object monitoring through <c>OnChildPropertyChanged</c> and <c>OnObservablePropertyChanged</c> methods.
/// </para>
/// </remarks>
/// <seealso cref="ObservableAttribute"/>
/// <seealso cref="ObservabilityExtensions"/>
/// <seealso cref="ObservabilityTypeOptionsBuilder"/>
/// <seealso href="@observability"/>
[PublicAPI]
[CompileTime]
public sealed class ClassicObservabilityStrategyOptionsBuilder
{
    internal ClassicObservabilityStrategyOptions? Options { get; private set; }

    internal ClassicObservabilityStrategyOptionsBuilder( ClassicObservabilityStrategyOptions? options )
    {
        this.Options = options;
    }
}