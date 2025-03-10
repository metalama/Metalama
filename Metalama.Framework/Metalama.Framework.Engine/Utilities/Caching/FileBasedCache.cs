// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.IO;

namespace Metalama.Framework.Engine.Utilities.Caching;

/// <summary>
/// A cache where the key is a file and the last write time of that file.
/// </summary>
/// <typeparam name="T"></typeparam>
internal sealed class FileBasedCache<T> : TimeBasedCache<string, T, DateTime>
{
    public FileBasedCache( TimeSpan rotationTimeSpan, IEqualityComparer<string>? keyComparer = null ) : base( rotationTimeSpan, keyComparer ) { }

    protected override DateTime GetTag( string key ) => File.GetLastWriteTime( key );

    protected override bool Validate( string key, in Item item ) => File.GetLastWriteTime( key ) <= item.Tag;
}