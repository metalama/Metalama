// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Advising;

/// <summary>
/// Represents the result of an advice that adds a contract.
/// </summary>
/// <typeparam name="T"><see cref="IParameter"/> or <see cref="IPropertyOrIndexer"/>.</typeparam>
/// <seealso cref="IAdviceResult"/>
/// <seealso cref="AdviserExtensions.AddContract(IAdviser{IFieldOrPropertyOrIndexer}, string, ContractDirection, object?, object?)"/>
/// <seealso cref="ContractDirection"/>
/// <seealso cref="ContractAspect"/>
/// <seealso href="@contracts"/>
public interface IAddContractAdviceResult<out T> : IAdviceResult
    where T : IDeclaration
{
    /// <summary>
    /// Gets the declaration to which the contract was added. When the contract is added to a field, this property returns the <see cref="IProperty"/>
    /// that the field has been transformed into.
    /// </summary>
    T Declaration { get; }
}