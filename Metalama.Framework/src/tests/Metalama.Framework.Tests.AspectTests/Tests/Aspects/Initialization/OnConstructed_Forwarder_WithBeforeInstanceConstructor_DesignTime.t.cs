public partial class Document : ISecurable
{
}
public partial class Report : ISecurable
{
  public Report()
  {
  }
  public Report(string title)
  {
    _ = title;
  }
}
public partial class WeightedReport : Report
{
}