// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;

namespace Metalama.Patterns.Wpf.Implementation;

// See project README.md for details of supported signatures.

[CompileTime]
internal enum ChangeHandlerSignatureKind
{
    InstanceNoParameters,
    InstanceValue,
    InstanceOldValueAndNewValue,
    InstanceDependencyProperty,
    StaticNoParameters,
    StaticDependencyProperty,
    StaticInstance,
    StaticDependencyPropertyAndInstance
}