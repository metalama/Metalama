// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.ComponentModel;

namespace Metalama.Patterns.Observability.Implementation;

/// <summary>
/// Introduces the minimum set of observable members, without bodies, to suit the Metalama design-time execution scenario.
/// </summary>
[CompileTime]
internal class DesignTimeObservabilityStrategy : IObservabilityStrategy, ITemplateProvider
{
    public virtual void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // Introduce the INotifyPropertyChanged if it's not already implemented.
        builder.Advice.WithTemplateProvider( this ).ImplementInterface( builder.Target, typeof(INotifyPropertyChanged), OverrideStrategy.Ignore );
    }

    // ReSharper disable once EventNeverSubscribedTo.Global
    [InterfaceMember]
#pragma warning disable CS0067
    public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067
}