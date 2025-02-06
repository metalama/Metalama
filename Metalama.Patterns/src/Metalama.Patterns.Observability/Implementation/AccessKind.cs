// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;

namespace Metalama.Patterns.Observability.Implementation;

[CompileTime]
internal enum AccessKind
{
    Undefined,
    Read,
    Write,
    ReadWrite // eg, ++x
}