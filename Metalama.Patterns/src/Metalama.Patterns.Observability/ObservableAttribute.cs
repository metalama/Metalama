// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Patterns.Observability.Configuration;

namespace Metalama.Patterns.Observability;

/// <summary>
/// Aspect that implements the <see cref="System.ComponentModel.INotifyPropertyChanged"/> interface for the target type,
/// automatically raising the <see cref="System.ComponentModel.INotifyPropertyChanged.PropertyChanged"/> event when
/// property values change.
/// </summary>
/// <remarks>
/// <para>
/// This aspect analyzes the dependencies between all properties in the type and ensures that the <c>PropertyChanged</c>
/// event is raised for all affected properties when a value changes. This includes:
/// </para>
/// <list type="bullet">
/// <item><description>Automatic properties</description></item>
/// <item><description>Field-backed properties</description></item>
/// <item><description>Computed properties that depend on other properties</description></item>
/// <item><description>Properties that depend on child objects implementing <see cref="System.ComponentModel.INotifyPropertyChanged"/></description></item>
/// </list>
/// <para>
/// If the target type already implements <see cref="System.ComponentModel.INotifyPropertyChanged"/>, the aspect will
/// use the existing <c>OnPropertyChanged</c> method (or equivalent) to raise notifications rather than introducing a new implementation.
/// </para>
/// <para>
/// When the aspect encounters an unsupported code construct, it reports a warning (code <c>LAMA51**</c>).
/// Use <see cref="NotObservableAttribute"/> to exclude specific properties from change notifications,
/// <see cref="SuppressObservabilityWarningsAttribute"/> to suppress warnings, or
/// <see cref="ConstantAttribute"/> to mark methods whose output depends only on their input parameters.
/// </para>
/// </remarks>
/// <seealso cref="NotObservableAttribute"/>
/// <seealso cref="ConstantAttribute"/>
/// <seealso cref="SuppressObservabilityWarningsAttribute"/>
/// <seealso cref="Configuration.ObservabilityExtensions"/>
/// <seealso href="@observability"/>
/// <seealso href="@observability-standard-cases"/>
[AttributeUsage( AttributeTargets.Class | AttributeTargets.Interface )]
[Inheritable]
public sealed class ObservableAttribute : Attribute, IAspect<INamedType>
{
    void IEligible<INamedType>.BuildEligibility( IEligibilityBuilder<INamedType> builder )
    {
        builder.MustNotBeStatic();
        builder.ExceptForInheritance().MustSatisfy( x => x.TypeKind is TypeKind.Class, x => $"{x} must be a class or a record class" );
    }

    void IAspect<INamedType>.BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var options = builder.Target.Enhancements().GetOptions<ObservabilityOptions>();
        options.ImplementationStrategy!.BuildAspect( builder );
    }
}