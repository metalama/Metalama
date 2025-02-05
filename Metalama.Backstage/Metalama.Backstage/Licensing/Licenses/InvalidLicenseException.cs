// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Backstage.Licensing.Licenses
{
    /// <summary>
    /// Exception thrown when an invalid license is provided.
    /// </summary>
    public class InvalidLicenseException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidLicenseException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public InvalidLicenseException( string message ) : base( message ) { }
    }
}