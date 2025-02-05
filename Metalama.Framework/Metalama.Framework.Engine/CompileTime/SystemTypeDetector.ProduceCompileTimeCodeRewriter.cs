// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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