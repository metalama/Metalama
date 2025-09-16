// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// ReSharper disable MissingIndent
// ReSharper disable BadExpressionBracesIndent

using Metalama.Framework.Engine.AdviceImpl.Override;
using Metalama.Framework.Engine.SyntaxGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace Metalama.Framework.Engine.Linking;

internal class EventBrokerTransformationInfo
{
    public EventBrokerInfo Parent { get; }

    public OverrideEventTransformation Transformation { get; }
    
    public string EventBrokerFieldName { get; }

    public Func<SyntaxGenerationContext, ExpressionSyntax> FieldInitializationExpression { get; }

    public EventBrokerTransformationInfo(
        EventBrokerInfo parent,
        OverrideEventTransformation transformation,
        string eventBrokerFieldName,
        Func<SyntaxGenerationContext, ExpressionSyntax> fieldInitializationExpression )
    {
        this.Parent = parent;
        this.Transformation = transformation;
        this.EventBrokerFieldName = eventBrokerFieldName;
        this.FieldInitializationExpression = fieldInitializationExpression;
    }
}