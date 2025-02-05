// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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