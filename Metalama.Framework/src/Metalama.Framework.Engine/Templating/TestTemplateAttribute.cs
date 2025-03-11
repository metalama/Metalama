// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using System;
using System.ComponentModel;

namespace Metalama.Framework.Engine.Templating
{
    // The attribute that marks a template method in the templating integration tests.

    /// <exclude />
    [AttributeUsage( AttributeTargets.Method )]
    [EditorBrowsable( EditorBrowsableState.Never )]
    [PublicAPI]
    public sealed class TestTemplateAttribute : TemplateAttribute;
}