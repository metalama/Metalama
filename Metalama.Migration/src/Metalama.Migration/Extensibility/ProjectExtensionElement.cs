// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace PostSharp.Extensibility
{
    /// <summary>
    /// In Metalama, use <see cref="Metalama.Framework.Project.ProjectExtension"/>. However, all project extensions are programmatic.
    /// Metalama does not support XML configuration.
    /// </summary>
    [PublicAPI]
    public sealed class ProjectExtensionElement
    {
        public string Name { get; }

        public string Namespace { get; }

        public object XElement { get; }
    }
}