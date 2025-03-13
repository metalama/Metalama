// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using System;
using System.Runtime.CompilerServices;

namespace Metalama.Framework.Engine.Introspection;

internal static class FormattableStringHelper
{
    public static FormattableString MapString( FormattableString formattableString, ICompilation compilation )
    {
        var arguments = formattableString.GetArguments();

        for ( var i = 0; i < arguments.Length; i++ )
        {
            if ( arguments[i] is IRef<IDeclaration> declarationRef )
            {
                arguments[i] = declarationRef.GetTarget( compilation );
            }
        }

        return FormattableStringFactory.Create( formattableString.Format, arguments );
    }
}