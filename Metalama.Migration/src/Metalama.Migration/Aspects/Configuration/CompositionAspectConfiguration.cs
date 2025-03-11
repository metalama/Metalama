// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using PostSharp.Aspects.Advices;

namespace PostSharp.Aspects.Configuration
{
    /// <summary>
    /// There is no aspect configuration in Metalama.
    /// </summary>
    public sealed class CompositionAspectConfiguration : AspectConfiguration
    {
        public TypeIdentity[] PublicInterfaces { get; set; }

        public InterfaceOverrideAction? OverrideAction { get; set; }

        public InterfaceOverrideAction? AncestorOverrideAction { get; set; }

        public bool? NonSerializedImplementation { get; set; }

        public bool? GenerateImplementationAccessor { get; set; }
    }
}