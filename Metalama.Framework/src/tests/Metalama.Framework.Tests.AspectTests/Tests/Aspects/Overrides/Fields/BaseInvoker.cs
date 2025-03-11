// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

#pragma warning disable CS0169

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Overrides.Fields.BaseInvoker
{
    internal class Aspect : OverrideFieldOrPropertyAspect
    {
        public override dynamic? OverrideProperty
        {
            get => meta.Target.FieldOrProperty.Value;
            set => meta.Target.FieldOrProperty.Value = value;
        }
    }

    // <target>
    internal class TargetCode
    {
        [Aspect]
        private int field;
    }
}