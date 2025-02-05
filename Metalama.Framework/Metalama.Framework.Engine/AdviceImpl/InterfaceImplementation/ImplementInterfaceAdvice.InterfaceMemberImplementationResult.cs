// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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