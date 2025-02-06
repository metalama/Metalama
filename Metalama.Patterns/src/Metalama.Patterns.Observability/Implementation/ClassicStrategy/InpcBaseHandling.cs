// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;

namespace Metalama.Patterns.Observability.Implementation.ClassicStrategy;

/// <summary>
/// Describes the way a property or child property is handled by base types.
/// </summary>
[CompileTime]
internal enum InpcBaseHandling
{
    /// <summary>
    /// Applies only at design-time to properties where <see cref="InpcInstrumentationKind.Unknown"/> applies.
    /// </summary>
    Unknown,

    /// <summary>
    /// Not applicable. The property is not an INPC-type property, or the property (or root property of the current property)
    /// is declared in the current target type.
    /// </summary>
    NotApplicable,

    /// <summary>
    /// The base class provides no support for this property.
    /// </summary>
    /// <remarks>
    /// <see cref="None"/> is not valid for root property nodes defined by base types: a type must
    /// always provide some support for its root properties via <see cref="OnPropertyChanged"/>
    /// or <see cref="OnObservablePropertyChanged"/>.
    /// <see cref="None"/> applies to child properties first referenced in the current target type
    /// (ie, not referenced by any base type).
    /// </remarks>
    None,

    /// <summary>
    /// The INPC property is fully supported by a base class (including subscribe/unsubscribe), OnChildPropertyChanged will be called for this property.
    /// </summary>
    OnChildPropertyChanged,

    /// <summary>
    /// The INPC property is partially supported by a base class which will track reference changes
    /// and call OnObservablePropertyChanged and then, for root properties only, OnPropertyChanged.
    /// </summary>
    OnObservablePropertyChanged,

    /// <summary>
    /// Changes to the root property will be reported only by OnPropertyChanged. For INPC root properties, this is the
    /// most minimal form of support (OnObservablePropertyChanged will not be called for this property).
    /// </summary>
    OnPropertyChanged
}