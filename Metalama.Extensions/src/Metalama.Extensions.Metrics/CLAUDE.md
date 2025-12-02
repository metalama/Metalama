# Metalama.Extensions.Metrics

- Use `WorkspaceCollection.Default.WithServices(s => s.AddMetrics())` to enable metrics
- Available metrics: StatementsCount, SyntaxNodesCount, LinesOfCode
- Query with `declaration.Metrics().Get<MetricType>()`
