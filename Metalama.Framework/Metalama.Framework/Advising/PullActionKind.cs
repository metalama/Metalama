// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Advising;

[CompileTime]
internal enum PullActionKind
{
    AppendParameterAndPull,
    UseExpression,
    DoNotPull
}