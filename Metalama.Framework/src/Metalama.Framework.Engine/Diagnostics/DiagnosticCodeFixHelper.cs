// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Diagnostics;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Diagnostics
{
    public static class DiagnosticCodeFixHelper
    {
        public static IReadOnlyList<string> GetCodeFixTitles( Diagnostic diagnostic )
        {
            if ( diagnostic.Properties.TryGetValue( CodeFixHelper.DiagnosticPropertyName, out var values ) && values != null )
            {
                return values.Split( CodeFixHelper.DiagnosticPropertyValueSeparator );
            }
            else
            {
                return Array.Empty<string>();
            }
        }
    }
}