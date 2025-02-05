// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Maintenance;
using System;
using System.Globalization;
using System.IO;
using System.Runtime.Versioning;

namespace Metalama.Framework.Engine.CompileTime;

internal sealed class OutputPathHelper
{
    private readonly ITempFileManager _tempFileManager;

    public const int MaxOutputFilenameLength = 24;

    private static int SuffixAndPrefixLength
        => CompileTimeCompilationBuilder.CompileTimeAssemblyPrefix.Length + 1 /* _ */ + 16 /* hash */ + 4 /* .dll */ - 1 /* backslash */;

    public OutputPathHelper( ITempFileManager tempFileManager )
    {
        this._tempFileManager = tempFileManager;
    }

    public OutputPaths GetOutputPaths( string runTimeAssemblyName, FrameworkName? targetFramework, ulong projectHash )
    {
        if ( runTimeAssemblyName.StartsWith( CompileTimeCompilationBuilder.CompileTimeAssemblyPrefix, StringComparison.Ordinal ) )
        {
            throw new ArgumentOutOfRangeException( nameof(runTimeAssemblyName) );
        }

        // Note: we must generate file paths that are smaller than 256 characters.

        // Get a shorter name for the target framework.
        string targetFrameworkName;

        if ( targetFramework != null )
        {
            targetFrameworkName = "";

            var splitByComma = targetFramework.FullName.Split( ',' );

            for ( var partIndex = 0; partIndex < splitByComma.Length; partIndex++ )
            {
                var namePart = splitByComma[partIndex];
                var splitByEqual = namePart.Split( '=' );

                string part;

                if ( splitByEqual.Length == 1 )
                {
                    part = splitByEqual[0].ToLowerInvariant();
                }
                else
                {
                    part = splitByEqual[1].ToLowerInvariant();
                }

                if ( partIndex == 1 )
                {
                    part = part.TrimStart( 'v' );
                }

                if ( partIndex > 1 )
                {
                    targetFrameworkName += "-";
                }

                targetFrameworkName += part;
            }
        }
        else
        {
            targetFrameworkName = "unspecified";
        }

        // Get a shorter assembly name.
        string shortAssemblyName;

        if ( runTimeAssemblyName.Length > 32 )
        {
            // It does not matter if we put several assemblies in the same directory because we include a unique hash of the full name in a subdirectory anyway.
            shortAssemblyName = runTimeAssemblyName.Substring( 0, 32 );
        }
        else
        {
            shortAssemblyName = runTimeAssemblyName;
        }

        // Get the directory name.
        var hash = projectHash.ToString( "x16", CultureInfo.InvariantCulture );

        var directory = this._tempFileManager.GetTempDirectory(
            Path.Combine( "CompileTime", shortAssemblyName, targetFrameworkName, hash ),
            CleanUpStrategy.WhenUnused );

        // Make sure that the base path is short enough. There should be 16 characters left.
        const int maxPathLength = 255;
        var remainingPathLength = maxPathLength - directory.Length;

        var minRemainingPathLength = MaxOutputFilenameLength + SuffixAndPrefixLength;

        if ( remainingPathLength < minRemainingPathLength )
        {
            throw new PathTooLongException(
                $"The temporary path '{directory}' is too long. It has {directory.Length} characters but must have max {maxPathLength - minRemainingPathLength}." );
        }

        var baseCompileTimeAssemblyName = $"{CompileTimeCompilationBuilder.CompileTimeAssemblyPrefix}{runTimeAssemblyName}";
        var maxCompileTimeAssemblyNameLength = remainingPathLength - SuffixAndPrefixLength;

        if ( baseCompileTimeAssemblyName.Length > maxCompileTimeAssemblyNameLength )
        {
            baseCompileTimeAssemblyName = baseCompileTimeAssemblyName.Substring( 0, maxCompileTimeAssemblyNameLength );
        }

        var compileTimeAssemblyName = baseCompileTimeAssemblyName + "_" + hash;

        var outputPaths = new OutputPaths( directory, compileTimeAssemblyName, null );

        if ( outputPaths.Pe.Length > maxPathLength )
        {
            throw new AssertionFailedException( $"The generated path '{outputPaths.Pe}' is too long: {outputPaths.Pe.Length} characters." );
        }

        if ( outputPaths.Manifest.Length > maxPathLength )
        {
            throw new AssertionFailedException( $"The generated path '{outputPaths.Manifest}' is too long: {outputPaths.Manifest.Length} characters." );
        }

        return outputPaths;
    }
}