// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Project;
using System;

namespace PostSharp.Extensibility
{
    /// <summary>
    /// In Metalama, use <see cref="MetalamaExecutionContext"/>.<see cref="MetalamaExecutionContext.Current"/>.<see cref="IExecutionContext.FormatProvider"/>.
    /// </summary>
    public interface IFormattingService : IService
    {
        [Obsolete( "Pass the IFormatProvider. This helps the analyzers." )]
        string Format( string format, params object[] arguments );

        string Format( IFormatProvider provider, string format, params object[] arguments );
    }
}