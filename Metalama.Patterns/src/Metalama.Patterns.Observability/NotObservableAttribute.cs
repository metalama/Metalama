// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Metalama.Patterns.Observability;

// ReSharper disable once RedundantTypeDeclarationBody
/// <summary>
/// Custom attribute that, when applied to a property, prevents the <see cref="ObservableAttribute"/> aspect
/// from raising change notifications for this property.
/// </summary>
/// <remarks>
/// <para>
/// This means that if you change the value of that property, the <c>PropertyChanged</c> event will not be raised.
/// </para>
/// </remarks>
[PublicAPI]
[AttributeUsage( AttributeTargets.Property | AttributeTargets.Field )]
public sealed class NotObservableAttribute : Attribute { }