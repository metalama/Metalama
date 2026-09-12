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
        public static T Get<T>( this IConfigurationManager configurationManager, bool ignoreCache = false )
            where T : ConfigurationFile
            => (T) configurationManager.Get( typeof(T), ignoreCache );

        public static string GetFilePath<T>( this IConfigurationManager configurationManager )
            where T : ConfigurationFile
            => configurationManager.GetFilePath( typeof(T) );

        /// <summary>
        /// Creates a configuration file with its default content if it does not exist yet.
        /// </summary>
        /// <typeparam name="T">The type of the configuration file.</typeparam>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <returns><see langword="true"/> if the file was created.</returns>
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

        /// <summary>
        /// Updates a configuration file if a condition holds.
        /// </summary>
        /// <typeparam name="T">The type of the configuration file.</typeparam>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="condition">Decides whether the file needs to be updated.</param>
        /// <param name="updateFunc">Produces the new content of the file from its current content.</param>
        /// <returns><see langword="true"/> if the file was written.</returns>
        /// <remarks>
        /// <para>
        /// The condition is evaluated twice. The first evaluation takes no lock and is only a filter: most
        /// conditions ask whether the setting is already in the desired state, and the great majority of calls stop
        /// there, without the file ever being locked. The second evaluation happens inside the transaction, on the
        /// content of the file at the moment of the write, and is the one that decides.
        /// </para>
        /// <para>
        /// The first evaluation is at least as fresh as the second, because a read takes no lock and therefore sees
        /// the file exactly as the locked path would. A condition that stops here can only have been false at a
        /// moment when it would also have been false inside the lock.
        /// </para>
        /// </remarks>
        public static bool UpdateIf<T>( this IConfigurationManager configurationManager, Predicate<T> condition, Func<T, T> updateFunc )
            where T : ConfigurationFile
        {
            if ( !condition( configurationManager.Get<T>( true ) ) )
            {
                configurationManager.Logger.Trace?.Log(
                    $"Update of {typeof(T).Name} skipped because the configuration setting was already in the desired state." );

                return false;
            }

            var outcome = configurationManager.Update(
                typeof(T),
                currentValue =>
                {
                    var typedCurrentValue = (T) currentValue;

                    return condition( typedCurrentValue ) ? updateFunc( typedCurrentValue ) : null;
                } );

            return outcome == ConfigurationUpdateOutcome.Updated;
        }

        /// <summary>
        /// Updates a configuration file.
        /// </summary>
        /// <typeparam name="T">The type of the configuration file.</typeparam>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="updateFunc">Produces the new content of the file from its current content.</param>
        /// <returns><see langword="true"/> if the file was written.</returns>
        /// <remarks>
        /// Unlike <see cref="UpdateIf{T}"/>, this method has no condition to filter on, so it takes the lock at
        /// once.
        /// </remarks>
        public static bool Update<T>( this IConfigurationManager configurationManager, Func<T, T> updateFunc )
            where T : ConfigurationFile, new()
            => configurationManager.Update( typeof(T), currentValue => updateFunc( (T) currentValue ) ) == ConfigurationUpdateOutcome.Updated;
    }
}
