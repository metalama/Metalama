// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis.Text;
using System.Runtime.InteropServices;

namespace Metalama.Framework.DesignTime.Contracts.Classification
{
    /// <summary>
    /// A read-only collection of <see cref="DesignTimeClassifiedTextSpan"/> with additional methods.
    /// </summary>
    [ComImport]
    [Guid( "04715C22-4D7C-42B2-AC93-17CEC26B4397" )]
    public interface IDesignTimeClassifiedTextCollection
    {
        DesignTimeClassifiedTextSpan[] GetClassifiedTextSpans();

        /// <summary>
        /// Gets all <see cref="DesignTimeClassifiedTextSpan"/> in a given <see cref="TextSpan"/>. 
        /// </summary>
        DesignTimeClassifiedTextSpan[] GetClassifiedTextSpans( int spanStart, int spanLength );
    }
}