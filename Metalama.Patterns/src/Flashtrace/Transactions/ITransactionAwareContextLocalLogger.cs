// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Options;

namespace Flashtrace.Transactions;

/// <summary>
/// This means a <c>LoggingTypeSource</c> starting in 6.8.
/// </summary>
internal interface ITransactionAwareContextLocalLogger
{ 
    // ReSharper disable InvalidXmlDocComment
#pragma warning disable CS1574, CS1584
    
    // ReSharper disable once InvalidXmlDocComment
    /// <summary>
    /// Evaluates whether a transaction needs to be open for a specified <see cref="OpenActivityOptions"/> and updates
    /// its <see cref="OpenActivityOptions.TransactionRequirement"/> property.
    /// This method must be invoked before calling <see cref="FlashtraceLevelSource.OpenActivity{T}(in T, in OpenActivityOptions)"/>.
    /// </summary>
    /// <param name="options">Options of the activity that creates the transaction.</param>
    OpenActivityOptions ApplyTransactionRequirements( in OpenActivityOptions options );
    
    // ReSharper restore InvalidXmlDocComment
#pragma warning restore CS1574, CS1584
}