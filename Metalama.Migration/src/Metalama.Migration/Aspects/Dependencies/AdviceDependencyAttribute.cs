// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace PostSharp.Aspects.Dependencies
{
    /// <summary>
    /// In Metalama, advice methods have no dependencies.
    /// </summary>
    public sealed class AdviceDependencyAttribute : AspectDependencyAttribute
    {
        public AdviceDependencyAttribute(
            AspectDependencyAction action,
            AspectDependencyPosition position,
            string adviceMethodName )
            : base( action, position )
        {
            this.AdviceMethodName = adviceMethodName;
        }

        public AdviceDependencyAttribute( AspectDependencyAction action, string adviceMethodName )
            : base( action )
        {
            this.AdviceMethodName = adviceMethodName;
        }

        public string AdviceMethodName { get; }
    }
}