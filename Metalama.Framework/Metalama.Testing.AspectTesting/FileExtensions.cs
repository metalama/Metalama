// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Testing.AspectTesting
{
    /// <summary>
    /// List of file extensions used by the test framework.
    /// </summary>
    [PublicAPI]
    public static class FileExtensions
    {
        /// <summary>
        /// Transformed C# code (<c>.t.cs</c>).
        /// </summary>
        public const string TransformedCode = ".t.cs";
        
        /// <summary>
        /// Introduced (generated) C# code (<c>.i.cs</c>).
        /// </summary>
        public const string IntroducedCode = ".i.cs";

        /// <summary>
        /// Program output (<c>.t.txt</c>).
        /// </summary>
        public const string ProgramOutput = ".t.txt";

        /// <summary>
        /// HTML rendering of the input C# (<c>.cs.html</c>).
        /// </summary>
        public const string InputHtml = ".cs.html";

        /// <summary>
        /// HTML rendering of the transformed C# (<c>.cs.html</c>).
        /// </summary>
        public const string TransformedHtml = ".t.cs.html";
        
        /// <summary>
        /// HTML rendering of the introduced C# (<c>.cs.html</c>).
        /// </summary>
        public const string IntroducedHtml = ".t.cs.html";
    }
}