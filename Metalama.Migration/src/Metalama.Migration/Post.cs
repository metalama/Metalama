// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace PostSharp
{
    /// <summary>
    /// No equivalent in Metalama.
    /// </summary>
    [PublicAPI]
    public static class Post
    {
        /// <summary>
        /// Not needed in Metalama. Hurrah!
        /// </summary>
        public static TTarget Cast<TSource, TTarget>( TSource o )
            where TSource : class
            where TTarget : class
            => (TTarget) (object) o;

        /// <summary>
        /// No equivalent in Metalama.
        /// </summary>
        public static bool IsTransformed { get; }

        /// <summary>
        /// No equivalent in Metalama.
        /// </summary>
        public static ref T GetMutableRef<T>( in T reference )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not required in Metalama.
        /// </summary>
        public static T GetValue<T>( T value ) where T : struct => value;
    }
}