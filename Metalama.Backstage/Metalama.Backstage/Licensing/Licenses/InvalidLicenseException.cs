// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Backstage.Licensing.Licenses
{
    /// <summary>
    /// Exception thrown when an invalid license is provided.
    /// </summary>
    public sealed class InvalidLicenseException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidLicenseException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public InvalidLicenseException( string message ) : base( message ) { }
    }
}