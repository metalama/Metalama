// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.AdviceImpl.Attributes;
using Metalama.Framework.Engine.Queries;
using Metalama.Framework.Utilities;
using System;

namespace Metalama.Framework.Engine.Advising;

/// <summary>
/// Gives extensions access to the engine state behind an <see cref="IAdviser"/>.
/// </summary>
[PublicAPI]
public static class AdviserExtensibility
{
    /// <summary>
    /// Gets the extension context of an adviser.
    /// </summary>
    /// <param name="adviser">An adviser created by the engine, of any kind.</param>
    /// <returns>A new context. The context is valid only during the execution of the aspect or fabric that received the adviser.</returns>
    /// <remarks>
    /// <see cref="IAdviser"/> is marked with <see cref="InternalImplementAttribute"/>, so every adviser is created by the engine and implements
    /// <see cref="IAdviserInternal"/>.
    /// </remarks>
    /// <exception cref="NotSupportedException">The adviser is the result of an attribute introduction.</exception>
    /// <exception cref="InvalidOperationException">The adviser is the result of an introduction whose outcome is an error or was ignored, so it has no target.</exception>
    public static AdviserExtensionContext GetExtensionContext( this IAdviser adviser )
    {
        if ( adviser is AddAttributeAdviceResult )
        {
            throw new NotSupportedException( "The result of an attribute introduction cannot be used as an adviser." );
        }

        // Reading the target throws InvalidOperationException for the result of an introduction that failed or was ignored.
        _ = adviser.Target;

        var factory = (IAdviceFactoryImpl) ((IAdviserInternal) adviser).AdviceFactory;

        // An adviser that is itself an owner, such as an aspect builder or a type fabric amender, is the owner of the contributions.
        // Otherwise, the owner is the one that the factory carries.
        return factory.CreateExtensionContext( adviser as IQueryOwner );
    }
}
