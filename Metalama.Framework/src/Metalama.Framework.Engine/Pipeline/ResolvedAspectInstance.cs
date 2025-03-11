// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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