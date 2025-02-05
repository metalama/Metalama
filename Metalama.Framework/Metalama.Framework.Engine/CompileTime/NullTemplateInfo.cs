// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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