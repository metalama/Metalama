// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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