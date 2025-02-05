// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Runtime.CompilerServices;

namespace Metalama.Framework.Code;

internal static class RefHelper
{
    public static ref object? Wrap( object? value ) => ref new StrongBox<object?>( value ).Value;
}