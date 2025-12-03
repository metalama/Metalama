// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

/// <summary>
/// Provides access to the current <see cref="IMetalamaEngineServices"/>.
/// This is initialized by the Engine assembly early in execution.
/// </summary>
internal static class MetalamaSdkExecutionContext
{
    private static IMetalamaEngineServices? _current;

    /// <summary>
    /// Gets the current execution context. Throws if the context has not been initialized.
    /// </summary>
    public static IMetalamaEngineServices Current
        => _current ?? throw new InvalidOperationException( "The MetalamaSdkExecutionContext has not been initialized." );

    /// <summary>
    /// Initializes the execution context. This should only be called once by the Engine assembly.
    /// </summary>
    public static void Initialize( IMetalamaEngineServices context )
    {
        if ( _current != null )
        {
            throw new InvalidOperationException( "The MetalamaSdkExecutionContext has already been initialized." );
        }

        _current = context;
    }
}
