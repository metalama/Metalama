// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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