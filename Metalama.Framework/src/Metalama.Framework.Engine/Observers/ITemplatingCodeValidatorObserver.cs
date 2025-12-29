// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Services;

namespace Metalama.Framework.Engine.Observers
{
    /// <summary>
    /// An interface that can be injected into the service provider to get callbacks from the <see cref="Templating.TemplatingCodeValidator"/>
    /// class. For testing and benchmarking only.
    /// </summary>
    public interface ITemplatingCodeValidatorObserver : IGlobalService
    {
        /// <summary>
        /// Method invoked when the semantic model is used (e.g., GetSymbolInfo, GetTypeInfo, GetDeclaredSymbol).
        /// </summary>
        void OnSemanticModelUsed();

        /// <summary>
        /// Method invoked when the symbol classifier is used (e.g., GetTemplatingScope, GetTemplateInfo, IsTemplateOnly).
        /// </summary>
        void OnSymbolClassifierUsed();
    }
}
