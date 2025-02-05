// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis.Text;
using System.Runtime.InteropServices;

namespace Metalama.Framework.DesignTime.Contracts.Classification
{
    /// <summary>
    /// A read-only collection of <see cref="DesignTimeClassifiedTextSpan"/> with additional methods.
    /// </summary>
    [ComImport]
    [Guid( "D498C406-2F33-4EFC-85FC-0B09CFD160F8" )]
    public interface IDesignTimeClassifiedTextCollection
    {
        DesignTimeClassifiedTextSpan[] GetClassifiedTextSpans();

        /// <summary>
        /// Gets all <see cref="DesignTimeClassifiedTextSpan"/> in a given <see cref="TextSpan"/>. 
        /// </summary>
        DesignTimeClassifiedTextSpan[] GetClassifiedTextSpans( int spanStart, int spanLength );
    }
}