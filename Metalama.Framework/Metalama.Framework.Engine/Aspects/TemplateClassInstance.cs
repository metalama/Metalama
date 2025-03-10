// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Engine.Aspects
{
    /// <summary>
    /// An instance of a template class, i.e. the transformed class containing the compiled templates.
    /// For a normal template, <see cref="TemplateProvider"/> is the aspect instance itself. For fabrics,
    /// the <see cref="TemplateProvider"/> is the transformed fabric class.
    /// </summary>
    internal sealed class TemplateClassInstance
    {
        public TemplateClass TemplateClass { get; }

        public TemplateProvider TemplateProvider { get; }

        public TemplateClassInstance( TemplateProvider templateProvider, TemplateClass templateClass )
        {
            this.TemplateProvider = templateProvider;
            this.TemplateClass = templateClass;
        }
    }
}