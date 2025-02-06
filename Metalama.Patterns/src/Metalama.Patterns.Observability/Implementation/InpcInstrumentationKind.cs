// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using System.ComponentModel;

namespace Metalama.Patterns.Observability.Implementation;

[CompileTime]
internal enum InpcInstrumentationKind
{
    /// <summary>
    /// No <see cref="INotifyPropertyChanged"/> implementation.
    /// </summary>
    None,

    /// <summary>
    /// The <see cref="INotifyPropertyChanged"/> interface is implemented by an aspect.
    /// </summary>
    Aspect,

    /// <summary>
    /// The <see cref="INotifyPropertyChanged"/> interface is implemented in code and the member is public.
    /// </summary>
    InpcPublicImplementation,

    /// <summary>
    /// The <see cref="INotifyPropertyChanged"/> interface is implemented in code and the member is private, requiring a cast.
    /// </summary>
    InpcPrivateImplementation,

    /// <summary>
    /// Returned at design time for types other than the current type and its ancestors.
    /// </summary>
    Unknown
}