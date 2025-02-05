// Warning CS8619 on `Task.FromResult( default(T) )`: `Nullability of reference types in value of type 'Task<T?>' doesn't match target type 'Task<T>'.`
[TheAspect]
internal class TargetClass<T2>
{
  private global::System.Threading.Tasks.Task<T2> SomeMethod()
  {
    return (global::System.Threading.Tasks.Task<T2>)global::System.Threading.Tasks.Task.FromResult(default(T2));
  }
}