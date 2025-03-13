// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;

namespace Metalama.Framework.Engine.CompileTime;

internal sealed class NullTemplateInfo : ITemplateInfo
{
    public static ITemplateInfo Instance { get; } = new NullTemplateInfo();

    private NullTemplateInfo() { }

    public SerializableDeclarationId Id => default;

    public bool IsAbstract => false;

    public bool HasNoBody => false;

    public TemplateAttributeType AttributeType => TemplateAttributeType.None;

    public bool IsNone => true;

    public RoslynApiVersion? UsedApiVersion => null;

    public override string ToString() => "(None)";
}