// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Engine.Templating;

internal sealed partial class TemplateCompilerRewriter
{
    private sealed class SkipCompileTimeLogicVariable
    {
        public string Name { get; }

        /// <summary>
        /// Indicates that the variable has been set in any execution path.
        /// </summary>
        public bool HasBeenSet { get; set; }
        
        /// <summary>
        /// Indicates that the variable is known to be false in the current context.
        /// </summary>
        public bool IsKnownFalse { get; set; }
        
        public bool MightBeTrue => this.HasBeenSet && !this.IsKnownFalse;
        

        public SkipCompileTimeLogicVariable( string name )
        {
            this.Name = name;
        }

        public override string ToString() => $"{this.Name}";
    }
}