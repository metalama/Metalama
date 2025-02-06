// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;

namespace Metalama.Patterns.Contracts.Numeric;

[RunTimeOrCompileTime]
internal enum ConversionResult
{
    WithinRange,
    ExactlyMaxValue,
    TooLarge,
    ExactlyMinValue,
    TooSmall,
    UnsupportedType
}