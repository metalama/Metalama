[Introduction]
public class TargetType
{
  public delegate void Consumer<TItem>(TItem item);
  public delegate TOutput Transformer<in TInput, out TOutput>(TInput value);
}