// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace PostSharp.Aspects.Advices
{
    /// <summary>
    /// In Metalama, use a local variable in the template.
    /// </summary>
    public sealed class StateAttribute : AdviceParameterAttribute
    {
        public StateAttribute( StateScope scope )
        {
            this.Scope = scope;
        }

        public StateAttribute( StateScope scope, string slotName )
        {
            this.Scope = scope;
            this.SlotName = slotName;
        }

        public StateScope Scope { get; }

        public string SlotName { get; }
    }
}