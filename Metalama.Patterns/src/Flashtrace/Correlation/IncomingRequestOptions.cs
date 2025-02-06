// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Flashtrace.Correlation;

/// <summary>
/// Logging options sent by the caller in a distributed logging transaction.
/// </summary>
[PublicAPI]
public readonly struct IncomingRequestOptions
{
    private readonly byte _isParentSampled;

    /// <summary>
    /// Initializes a new instance of the <see cref="IncomingRequestOptions"/> struct.
    /// </summary>
    /// <param name="isParentSampled">Determines whether the parent request was logged as a result of sampling,
    /// i.e. it was logged by a policy that had a sampling clause. It corresponds
    /// to the <c>sampled</c> flag of the W3C Trace Context specification
    /// (https://www.w3.org/TR/trace-context/#sampled-flag). 
    /// </param>
    public IncomingRequestOptions( bool isParentSampled )
    {
        this._isParentSampled = isParentSampled ? (byte) 1 : (byte) 0;
    }

    /// <summary>
    /// Gets a value indicating whether the parent request was logged as a result of sampling,
    /// i.e. it was logged by a policy that had a sampling clause. It corresponds
    /// to the <c>sampled</c> flag of the W3C Trace Context specification
    /// (https://www.w3.org/TR/trace-context/#sampled-flag). 
    /// </summary>
    public bool IsParentSampled => this._isParentSampled != 0;
}