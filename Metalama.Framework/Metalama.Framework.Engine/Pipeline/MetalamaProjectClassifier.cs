// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

// ReSharper disable InconsistentlySynchronizedField

namespace Metalama.Framework.Engine.Pipeline;

internal sealed class MetalamaProjectClassifier : IMetalamaProjectClassifier
{
    public bool TryGetMetalamaVersion( Compilation compilation, [NotNullWhen( true )] out Version? version )
    {
        if ( compilation.SyntaxTrees.FirstOrDefault()?.Options.PreprocessorSymbolNames.Contains( "METALAMA" ) != true )
        {
            version = null;

            return false;
        }

        var reference = compilation.SourceModule.ReferencedAssemblies
            .Where( identity => identity.Name == "Metalama.Framework" )
            .MaxByOrNull( identity => identity.Version );

        if ( reference == null )
        {
            version = null;

            return false;
        }

        version = reference.Version;

        return true;
    }
}