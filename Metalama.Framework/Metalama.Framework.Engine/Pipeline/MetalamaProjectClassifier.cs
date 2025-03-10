// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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