// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Backstage.Telemetry;

/// <summary>
/// The event published by the telemetry services when an exception report has been captured and the user may review
/// it. The user interface subscribes to it to show a notification.
/// </summary>
/// <param name="ReportFileName">The bare name of the report file (for instance <c>exception-&lt;hash&gt;.xml</c>), which identifies the report on the review page.</param>
/// <param name="Scenario">The scenario of the report: an exception or a performance problem.</param>
/// <param name="ApplicationName">The name of the application in which the exception occurred.</param>
/// <param name="AutoSent">A value indicating whether the report was sent automatically because the user had consented in advance, in which case the notification is informational only.</param>
[PublicAPI]
public sealed record ExceptionReportCaptured( string ReportFileName, TelemetryScenario Scenario, string ApplicationName, bool AutoSent );
