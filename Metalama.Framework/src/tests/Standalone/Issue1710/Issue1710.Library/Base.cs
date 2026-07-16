// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Contracts;

namespace Issue1710.Library
{
    /// <summary>
    /// Declares a contract on a virtual member, so that the contract is inherited by overrides — including
    /// overrides declared in a referencing project that targets a different framework (see Issue1710.App).
    /// </summary>
    public class Base
    {
        // [Positive] derives from ContractBaseAttribute, which is conditionally inheritable: its
        // IConditionallyInheritableAspect.IsInheritable implementation reads targetDeclaration.GetContractOptions(),
        // i.e. it triggers the hierarchical-options merge on the *derived* declaration in the consuming project.
        // That merge is where issue #1710 fails.
        public virtual void SetValue( [Positive] int value ) { }
    }
}
