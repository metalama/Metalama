// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Eligibility;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Abstractions;

namespace Metalama.Framework.Engine.Pipeline
{
    /// <summary>
    /// An <see cref="AspectInstance"/> with resolved <see cref="TargetDeclaration"/> and <see cref="Eligibility"/>.
    /// </summary>
    internal readonly struct ResolvedAspectInstance
    {
        public AspectInstance AspectInstance { get; }

        public IDeclarationImpl TargetDeclaration { get; }

        public EligibleScenarios Eligibility { get; }

        public ResolvedAspectInstance( AspectInstance aspectInstance, IDeclarationImpl targetDeclaration, EligibleScenarios eligibility )
        {
            this.AspectInstance = aspectInstance;
            this.TargetDeclaration = targetDeclaration;
            this.Eligibility = eligibility;
        }

        public override string ToString() => this.AspectInstance.ToString();
    }
}