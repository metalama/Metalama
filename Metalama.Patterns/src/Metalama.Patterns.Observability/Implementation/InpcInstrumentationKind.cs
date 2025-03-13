// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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