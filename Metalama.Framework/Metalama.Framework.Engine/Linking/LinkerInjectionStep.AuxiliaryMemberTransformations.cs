// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Transformations;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Linking;

internal sealed partial class LinkerInjectionStep
{
    private sealed class AuxiliaryMemberTransformations
    {
        // Indicates that source declaration has to be injected. This declaration will receive inserted statements and initializers.
        // Used for primary constructors.
        private volatile bool _shouldInjectAuxiliarySourceMember;

        // Lists all insert statement transformations that are the origin for contract auxiliaries.
        // Typically this is the first insert statement after an override (or source version) when there is an output contract statement before the next override (or final version),
        // or when there is any (input or output) contract statement for auto property.
        private List<(IInsertStatementTransformation, string?)>? _auxiliaryContractMembers;

        public bool ShouldInjectAuxiliarySourceMember => this._shouldInjectAuxiliarySourceMember;

        public IReadOnlyList<(IInsertStatementTransformation OriginTransformation, string? ReturnVariableName)> AuxiliaryContractMembers
            => (IReadOnlyList<(IInsertStatementTransformation, string?)>?) this._auxiliaryContractMembers
               ?? Array.Empty<(IInsertStatementTransformation, string?)>();

        public void InjectAuxiliarySourceMember() => this._shouldInjectAuxiliarySourceMember = true;

        public void InjectAuxiliaryContractMember( IInsertStatementTransformation originTransformation, string? returnVariableName )
        {
            this._auxiliaryContractMembers ??= [];
            this._auxiliaryContractMembers.Add( (originTransformation, returnVariableName) );
        }
    }
}