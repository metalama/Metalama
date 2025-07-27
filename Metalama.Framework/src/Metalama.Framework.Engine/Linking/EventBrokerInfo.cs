// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// ReSharper disable MissingIndent
// ReSharper disable BadExpressionBracesIndent

using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Linking;

/// <summary>
/// Contains event broker information for a specific event.
/// </summary>
public class EventBrokerInfo
{
    public IEventSymbol Event { get; }

    public INamedTypeSymbol EventBrokerType { get; }

    public string EventBrokerFieldName { get; }

    public StaticDelegateInfo InvokerDelegate { get; }

    public StaticDelegateInfo CastDelegate { get; }

    public EventBrokerInfo(
        IEventSymbol @event,
        INamedTypeSymbol eventBrokerType,
        string eventBrokerFieldName,
        StaticDelegateInfo invokerDelegate,
        StaticDelegateInfo castDelegate )
    {
        this.Event = @event;
        this.EventBrokerType = eventBrokerType;
        this.EventBrokerFieldName = eventBrokerFieldName;
        this.InvokerDelegate = invokerDelegate;
        this.CastDelegate = castDelegate;
    }
}