// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Reflection;

namespace Metalama.Framework.Engine.Utilities;

internal static class AssemblyExtensions
{
    /// <summary>
    /// Returns <see cref="Assembly.Location"/>, or a non-null placeholder for assemblies that have no on-disk
    /// location. Dynamic and in-memory assemblies throw <see cref="NotSupportedException"/> from
    /// <see cref="Assembly.Location"/> on .NET; some hosts return an empty string. Both are mapped to
    /// <c>"&lt;in-memory&gt;"</c> so that diagnostic strings remain non-null and meaningful.
    /// </summary>
    public static string GetLocationSafe( this Assembly assembly )
    {
        try
        {
            return string.IsNullOrEmpty( assembly.Location ) ? "<in-memory>" : assembly.Location;
        }
        catch ( NotSupportedException )
        {
            return "<in-memory>";
        }
    }
}
