// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace Metalama.Backstage.Configuration
{
    [PublicAPI]
    public static class ConfigurationManagerExtensions
    {
        private const int _maxUpdateAttempts = 10;

        public static T Get<T>( this IConfigurationManager configurationManager, bool ignoreCache = false )
            where T : ConfigurationFile
            => (T) configurationManager.Get( typeof(T), ignoreCache );

        public static string GetFilePath<T>( this IConfigurationManager configurationManager )
            where T : ConfigurationFile
            => configurationManager.GetFilePath( typeof(T) );

        public static bool CreateIfMissing<T>( this IConfigurationManager configurationManager )
            where T : ConfigurationFile
        {
            // Do a first check that does not skip the cache.
            if ( configurationManager.Get<T>().Timestamp != null )
            {
                return false;
            }

            return configurationManager.UpdateIf<T>( c => !c.Timestamp.HasValue, c => c );
        }

        public static bool UpdateIf<T>( this IConfigurationManager configurationManager, Predicate<T> condition, Func<T, T> updateFunc )
            where T : ConfigurationFile
        {
            var attempts = 0;

            while ( true )
            {
                attempts++;

                configurationManager.Logger.Trace?.Log( $"{attempts}-th attempt to update {typeof(T).Name}" );

                if ( attempts > _maxUpdateAttempts )
                {
                    // We no longer throw an exception here because we have a known random issue and throwing an exception seems to be worse
                    // than ignoring it.

                    // Include the call stack so that, if this recurs, the contending caller can be identified from the log.
                    configurationManager.Logger.Error?.Log(
                        $"Too many attempts to update the configuration {typeof(T).Name}. There must be an unaddressed race condition.{
                            Environment.NewLine}{Environment.StackTrace}" );

                    return false;
                }

                var originalSettings = configurationManager.Get<T>( true );

                if ( !condition( originalSettings ) )
                {
                    configurationManager.Logger.Trace?.Log(
                        $"Update of {typeof(T).Name} skipped because the configuration setting was already in the desired state." );

                    return false;
                }

                var newSettings = updateFunc( originalSettings );

                if ( originalSettings.Timestamp.HasValue && newSettings.Equals( originalSettings ) )
                {
                    configurationManager.Logger.Trace?.Log( $"Update of {typeof(T).Name} skipped because no change was required." );

                    return false;
                }

                switch ( TryUpdateCore( configurationManager, newSettings, originalSettings.Timestamp ) )
                {
                    case UpdateResult.Updated:
                        return true;

                    case UpdateResult.Abandoned:
                        return false;
                }
            }
        }

        public static bool Update<T>( this IConfigurationManager configurationManager, Func<T, T> updateFunc )
            where T : ConfigurationFile, new()
        {
            var attempts = 0;

            while ( true )
            {
                attempts++;

                configurationManager.Logger.Trace?.Log( $"{attempts}-th attempt to update {typeof(T).Name}" );

                if ( attempts > _maxUpdateAttempts )
                {
                    // We no longer throw an exception here because we have a known random issue and throwing an exception seems to be worse
                    // than ignoring it.

                    // Include the call stack so that, if this recurs, the contending caller can be identified from the log.
                    configurationManager.Logger.Error?.Log(
                        $"Too many attempts to update the configuration {typeof(T).Name}. There must be an unaddressed race condition.{
                            Environment.NewLine}{Environment.StackTrace}" );

                    return false;
                }

                var originalSettings = configurationManager.Get<T>( true );

                var newSettings = updateFunc( originalSettings );

                if ( originalSettings.Timestamp.HasValue && newSettings.Equals( originalSettings ) )
                {
                    configurationManager.Logger.Trace?.Log( $"Update of {typeof(T).Name} skipped because no change was required." );

                    return false;
                }

                switch ( TryUpdateCore( configurationManager, newSettings, originalSettings.Timestamp ) )
                {
                    case UpdateResult.Updated:
                        return true;

                    case UpdateResult.Abandoned:
                        return false;
                }
            }
        }

        /// <summary>
        /// Performs a single attempt of the optimistic update loop, and reports whether the update succeeded, must be
        /// retried, or must be abandoned.
        /// </summary>
        /// <remarks>
        /// An update is abandoned when the global configuration mutex is unavailable. Retrying would wait for the same
        /// mutex again, and no configuration file is important enough to fail the operation during which it is written.
        /// See issue #1847.
        /// </remarks>
        private static UpdateResult TryUpdateCore(
            IConfigurationManager configurationManager,
            ConfigurationFile newSettings,
            ConfigurationFileTimestamp? expectedTimestamp )
        {
            try
            {
                return configurationManager.TryUpdate( newSettings, expectedTimestamp ) ? UpdateResult.Updated : UpdateResult.Conflict;
            }
            catch ( ConfigurationMutexTimeoutException e )
            {
                configurationManager.Logger.Error?.Log( $"The configuration {newSettings.GetType().Name} was not updated. {e.Message}" );

                return UpdateResult.Abandoned;
            }
        }

        /// <summary>
        /// The outcome of a single attempt of the optimistic update loop.
        /// </summary>
        private enum UpdateResult
        {
            /// <summary>
            /// The configuration file was written.
            /// </summary>
            Updated,

            /// <summary>
            /// The configuration file was modified by somebody else since it was read, so the update must be retried.
            /// </summary>
            Conflict,

            /// <summary>
            /// The configuration file could not be written and the update must not be retried.
            /// </summary>
            Abandoned
        }
    }
}