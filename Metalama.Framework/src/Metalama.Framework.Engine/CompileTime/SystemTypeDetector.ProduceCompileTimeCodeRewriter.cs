// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Engine.CompileTime
{
    internal static class SystemTypeDetector
    {
        public static bool IsSystemType( INamedTypeSymbol namedType )
        {
            var nsName = namedType.ContainingNamespace.GetFullName();

            switch ( nsName )
            {
                case "System":
                    // Syttem.Index, System.Range and types nested in them.
                    return namedType.GetTopmostContainingType().Name is nameof(Index) or nameof(Range);

                case "System.Runtime.CompilerServices":
                case "System.Diagnostics.CodeAnalysis":
                    return true;
            }

            return false;
        }
    }
}