// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Introspection.Navigation.Links;
using System.Text.Json.Nodes;

namespace Metalama.Mcp.Worker;

/// <summary>
/// A rendered result: the text for the model, the JSON form for the MCP App view, and, for the html format, the document of
/// the page that the supervisor serves.
/// </summary>
internal sealed record RenderedResult( string Text, JsonObject? Json, JsonObject? Page, LinkBase? Base );
