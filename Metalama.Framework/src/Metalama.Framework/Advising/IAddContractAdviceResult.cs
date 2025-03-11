// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;

namespace Metalama.Framework.Advising;

/// <summary>
/// Represents the result of the <see cref="IAdviceFactory.AddContract(Metalama.Framework.Code.IParameter,string,Metalama.Framework.Aspects.ContractDirection,object?,object?)"/>
/// method.
/// </summary>
/// <typeparam name="T"><see cref="IParameter"/> or <see cref="IPropertyOrIndexer"/>.</typeparam>
public interface IAddContractAdviceResult<out T> : IAdviceResult
    where T : IDeclaration
{
    /// <summary>
    /// Gets the declaration to which the contract was added. When the contracted is added to a field, returns the <see cref="IProperty"/>
    /// that the field has been transformed into.
    /// </summary>
    T Declaration { get; }
}