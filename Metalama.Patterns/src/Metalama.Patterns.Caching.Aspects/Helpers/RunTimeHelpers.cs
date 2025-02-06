// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System.Reflection;

namespace Metalama.Patterns.Caching.Aspects.Helpers;

[PublicAPI]
public static class RunTimeHelpers
{
    public static MethodInfo ThrowIfMissing( this MethodInfo? methodInfo, string methodSignature )
        => methodInfo ?? throw new MissingMethodException( $"The method '{methodSignature}' could not be found." );
}