// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// ReSharper disable MissingIndent
// ReSharper disable BadExpressionBracesIndent

using Metalama.Framework.Engine.AdviceImpl.Override;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Transformations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Linking;

/// <summary>
/// Contains event broker information for a specific event.
/// </summary>
internal class EventBrokerInfo
{
    public IEventSymbol Event { get; }

    public INamedTypeSymbol EventBrokerType { get; }

    public StaticDelegateInfo CastDelegate { get; }

    public IReadOnlyDictionary<ITransformation, EventBrokerTransformationInfo> Transformations { get; }

    public EventBrokerInfo(
        IEventSymbol @event,
        INamedTypeSymbol eventBrokerType,
        StaticDelegateInfo castDelegate )
    {
        this.Event = @event;
        this.EventBrokerType = eventBrokerType;
        this.CastDelegate = castDelegate;
        this.Transformations = new Dictionary<ITransformation, EventBrokerTransformationInfo>();
    }
}

internal class EventBrokerTransformationInfo
{
    public OverrideEventTransformation Transformation { get; }
    
    public string EventBrokerFieldName { get; }

    public StaticDelegateInfo InvokerDelegate { get; }

    public Func<SyntaxGenerationContext, ExpressionSyntax> FieldInitializationExpression { get; }

    public EventBrokerTransformationInfo(
        OverrideEventTransformation transformation,
        string eventBrokerFieldName,
        StaticDelegateInfo invokerDelegate,
        Func<SyntaxGenerationContext, ExpressionSyntax> fieldInitializationExpression )
    {
        this.Transformation = transformation;
        this.EventBrokerFieldName = eventBrokerFieldName;
        this.InvokerDelegate = invokerDelegate;
        this.FieldInitializationExpression = fieldInitializationExpression;
    }
}