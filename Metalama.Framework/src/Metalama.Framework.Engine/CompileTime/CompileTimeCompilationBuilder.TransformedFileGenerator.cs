// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CompileTime;

internal sealed partial class CompileTimeCompilationBuilder
{
    /// <summary>
    /// Allocates the path of a transformed compile-time syntax tree.
    /// </summary>
    /// <remarks>
    /// Internal rather than private so that <c>TransformedPathGeneratorTests</c> can exercise the uniqueness contract
    /// directly. Reaching it through <see cref="CompileTimeCompilationBuilder"/> would require constructing a hash
    /// collision through real source code, which no test can arrange deterministically.
    /// </remarks>
    internal sealed class TransformedPathGenerator
    {
        /// <summary>
        /// The number of characters reserved for the ordinal that disambiguates a collision, including its separator.
        /// </summary>
        private const int _maxOrdinalLength = 3;

        private static int NameMaxLength
            => OutputPathHelper.MaxOutputFilenameLength - 1 /* backslash */ - 1 /* - */ - 8 /* hash */ - 3 /* .cs */ - _maxOrdinalLength;

        private readonly HashSet<string> _generatedNames = new( StringComparer.OrdinalIgnoreCase );

        /// <summary>
        /// Gets the path of the transformed tree of a compile-time file, given the file name without its extension and
        /// the hash of its content.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The name must not depend on the directory the repository is checked out in. If it did, the same project built
        /// on two machines, or checked out in two directories, would have one source hash and two different
        /// <c>manifest.json</c> files. Only the file name and the content therefore contribute to it.
        /// </para>
        /// <para>
        /// That leaves a collision reachable, because the name carries only the low thirty-two bits of the hash: two
        /// compile-time files of one name whose contents differ can agree on those bits. Two files of one name and
        /// identical content collide as well, though that is rarer, since identical content usually means duplicate type
        /// declarations. Neither is a reason to fail the compile-time project, which is what this method used to do. See
        /// issue #1742.
        /// </para>
        /// <para>
        /// A collision is resolved by appending an ordinal. That keeps the result independent of the checkout directory
        /// as long as the call order is, and it is: the caller orders the trees by file name and then by content hash
        /// before calling. Widening the hash instead would only make the collision rarer, and would spend eight
        /// characters of a name budget that <see cref="OutputPathHelper.MaxOutputFilenameLength"/> already constrains.
        /// </para>
        /// </remarks>
        public string GetTransformedFilePath( string fileName, ulong hash )
        {
            var transformedFileName = fileName;

            if ( transformedFileName.Length > NameMaxLength )
            {
                transformedFileName = transformedFileName.Substring( 0, NameMaxLength );
            }

            string fileNameWithHash;

            unchecked
            {
                fileNameWithHash = $"{transformedFileName}_{(uint) hash:x8}";
            }

            var candidate = fileNameWithHash + ".cs";

            for ( var ordinal = 2; !this._generatedNames.Add( candidate ); ordinal++ )
            {
                if ( ordinal > 99 )
                {
                    // Unreachable in practice: it would take a hundred compile-time files of one name colliding on the
                    // same thirty-two bits. Reported rather than silently truncated, because beyond this the ordinal no
                    // longer fits the reserved budget and the name could exceed the maximum length.
                    throw new InvalidOperationException(
                        $"More than 99 compile-time files named '{transformedFileName}' have a content hash with the same lower 32 bits." );
                }

                candidate = $"{fileNameWithHash}_{ordinal}.cs";
            }

            return candidate;
        }
    }
}