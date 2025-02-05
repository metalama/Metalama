// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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