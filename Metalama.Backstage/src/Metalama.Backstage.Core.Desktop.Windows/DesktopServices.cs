// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Desktop.Windows.Commands;
using System;

namespace Metalama.Backstage.Desktop.Windows;

/// <summary>
/// Holds the Backstage services of the notifier process. They are created once, on the first command, from the
/// delegate that the product supplies in <see cref="BackstageDesktopOptions"/>.
/// </summary>
internal static class DesktopServices
{
    private static readonly object _lock = new();
    private static BackstageDesktopOptions? _options;
    private static IServiceProvider? _serviceProvider;

    /// <summary>
    /// Gets the service provider when it has been created, or <c>null</c> when no command has requested it yet.
    /// </summary>
    public static IServiceProvider? ServiceProvider
    {
        get
        {
            lock ( _lock )
            {
                return _serviceProvider;
            }
        }
    }

    /// <summary>
    /// Stores the options of the product. It is called once by <see cref="BackstageDesktopProgram.Run"/>.
    /// </summary>
    public static void Initialize( BackstageDesktopOptions options )
    {
        lock ( _lock )
        {
            if ( _options != null )
            {
                throw new InvalidOperationException( "The desktop services have already been initialized." );
            }

            _options = options;
        }
    }

    /// <summary>
    /// Gets the service provider, creating it on the first call with the settings of the command.
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Initialize"/> has not been called.</exception>
    public static IServiceProvider Get( BaseSettings settings )
    {
        lock ( _lock )
        {
            if ( _serviceProvider != null )
            {
                return _serviceProvider;
            }

            if ( _options == null )
            {
                throw new InvalidOperationException( $"{nameof(BackstageDesktopProgram)}.{nameof(BackstageDesktopProgram.Run)} has not been called." );
            }

            _serviceProvider = _options.CreateBackstageServices( settings.IsDevelopmentEnvironment );

            return _serviceProvider;
        }
    }
}
