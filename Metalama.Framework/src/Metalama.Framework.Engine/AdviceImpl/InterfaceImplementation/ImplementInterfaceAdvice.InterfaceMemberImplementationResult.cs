// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;

namespace Metalama.Framework.Engine.AdviceImpl.InterfaceImplementation;

internal sealed partial class ImplementInterfaceAdvice
{
    public sealed class MemberImplementationResult : IInterfaceMemberImplementationResult
    {
        private readonly CompilationModel _compilation;
        private readonly IMember _member;

        internal MemberImplementationResult(
            CompilationModel compilation,
            IMember interfaceMember,
            InterfaceMemberImplementationOutcome outcome,
            IMember member )
        {
            this._compilation = compilation;
            this.InterfaceMember = interfaceMember;
            this.Outcome = outcome;
            this._member = member;
        }

        public IMember InterfaceMember { get; }

        public InterfaceMemberImplementationOutcome Outcome { get; }

        public IMember TargetMember => this._member.Translate( this._compilation );
    }
}