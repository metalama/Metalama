// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Patterns.Observability;

// ReSharper disable once RedundantTypeDeclarationBody
/// <summary>
/// Custom attribute that, when applied to a property or field, excludes it from change notification processing
/// by the <see cref="ObservableAttribute"/> aspect.
/// </summary>
/// <remarks>
/// <para>
/// When this attribute is applied, changes to the property or field will not raise the <c>PropertyChanged</c> event.
/// Other properties that depend on this property will also not be notified of changes.
/// </para>
/// <para>
/// This attribute is useful for properties that change frequently but don't need UI updates (e.g., properties updated by a timer),
/// or for properties whose change notifications are handled through a different mechanism.
/// </para>
/// </remarks>
/// <seealso cref="ObservableAttribute"/>
/// <seealso cref="SuppressObservabilityWarningsAttribute"/>
/// <seealso href="@observability"/>
[PublicAPI]
[AttributeUsage( AttributeTargets.Property | AttributeTargets.Field )]
public sealed class NotObservableAttribute : Attribute { }