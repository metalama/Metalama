// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Advising;

public interface ITemplateAttribute : IAdviceAttribute
{
    // We are using this design (to expose properties as an object) to make is possible to add more properties
    // in later versions.

    TemplateAttributeProperties? Properties { get; }
}