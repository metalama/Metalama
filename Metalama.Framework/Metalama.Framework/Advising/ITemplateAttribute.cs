// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Advising;

public interface ITemplateAttribute : IAdviceAttribute
{
    // We are using this design (to expose properties as an object) to make is possible to add more properties
    // in later versions.

    TemplateAttributeProperties? Properties { get; }
}