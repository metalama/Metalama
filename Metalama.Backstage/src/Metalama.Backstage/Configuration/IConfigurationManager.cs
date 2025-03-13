// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using System;

namespace Metalama.Backstage.Configuration
{
    public interface IConfigurationManager : IDisposable, IBackstageService
    {
        ILogger Logger { get; }

        string GetFilePath( string fileName );

        string GetFilePath( Type type );

        ConfigurationFile Get( Type type, bool ignoreCache = false );

        event Action<ConfigurationFile> ConfigurationFileChanged;

        /// <summary>
        /// Try to update a settings file if the base revision matches the expected value.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="expectedTimestamp"></param>
        /// <returns></returns>
        bool TryUpdate( ConfigurationFile value, ConfigurationFileTimestamp? expectedTimestamp );
    }
}