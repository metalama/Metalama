// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Services;

namespace Metalama.Framework.Workspaces;

/// <summary>
/// A builder for registering custom project services that will be available to all workspaces in a <see cref="WorkspaceCollection"/>.
/// </summary>
/// <remarks>
/// Access this builder through the <see cref="WorkspaceCollection.ServiceBuilder"/> property to register services
/// that will be available to all projects loaded in that collection.
/// </remarks>
/// <seealso cref="WorkspaceCollection.ServiceBuilder"/>
/// <seealso cref="IProjectService"/>
[PublicAPI]
public sealed class ServiceBuilder : ServiceProviderBuilder<IProjectService>;