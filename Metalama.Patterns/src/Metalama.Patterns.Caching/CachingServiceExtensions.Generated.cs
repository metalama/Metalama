// THIS FILE IS T4-GENERATED.
// To edit, go to CacheInvalidation.Generated.tt.
// To transform, run RunT4.ps1.

#pragma warning disable LAMA0320

#nullable enable

using System;
using System.Threading.Tasks;
using Metalama.Patterns.Contracts;
using Flashtrace;
using static Flashtrace.Messages.SemanticMessageBuilder;

namespace Metalama.Patterns.Caching
{
	public partial class CachingServiceExtensions
    {
        
		
		    /// <summary>
            /// Removes a method call result from the cache giving the delegate of the method. This overload is for methods with 1 parameter.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>
            /// <param name="method">A delegate of the method to invalidate.</param>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
	  			public static void Invalidate<TReturn, TParam1>( this ICachingService cachingService,  Func<TParam1, TReturn> method, TParam1 arg1 )
            {
                cachingService.InvalidateDelegate( method, arg1 );
            }

			 /// <summary>
            /// Asynchronously removes a method call result from the cache giving the delegate of the method. This overload is for methods with 1 parameter.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>
            /// <param name="method">A delegate of the method to invalidate.</param>
			/// <returns>A <see cref="Task"/>.</returns>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
				public static ValueTask InvalidateAsync<TReturn, TParam1>( this ICachingService cachingService, Func<TParam1, TReturn> method, TParam1 arg1, CancellationToken cancellationToken = default )
            {
                return cachingService.InvalidateDelegateAsync( method, new object?[] { arg1 }, cancellationToken );
            }

			/// <summary>
            /// Evaluates a method, ignoring the currently cached value, and replaces the corresponding cache item with the new return value of the method. This overload is for methods with 1 parameter.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>
            /// <param name="method">A delegate of the method to evaluate.</param>
            /// <returns>The return value of <paramref name="method"/>.</returns>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
				public static TReturn Refresh<TReturn,  TParam1>(this ICachingService cachingService,  Func< TParam1, TReturn> method, TParam1 arg1 )
            {
				using ( var activity = cachingService.Logger.Default.OpenActivity( Semantic("Refresh", ("Method", method.Method ) ) ) )
                {
                    try
                    {
                        TReturn result;

                        using ( cachingService.OpenRefreshContext( method, arg1 ) )
                        {
                            result = method( arg1  );
                        }

                        activity.SetSuccess();

                        return result;
                    }
                    catch ( Exception e ) 
                    {
                        activity.SetException(e);
                        throw;
                    }
                }
            }

			/// <summary>
            /// Asynchronously evaluates a method, ignoring the currently cached value, and replaces the corresponding cache item with the new return value of the method. This overload is for methods with 1 parameter.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="method">A delegate of the method to evaluate.</param>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>    
            /// <returns>A <see cref="Task{TResult}"/> that evaluates to the return value of <paramref name="method"/>.</returns>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
				public static async Task<TReturn> RefreshAsync<TReturn,  TParam1>(  this ICachingService cachingService, Func<TParam1, Task<TReturn>> method, TParam1 arg1, CancellationToken cancellationToken = default )
            {
                using ( var activity = cachingService.Logger.Default.OpenAsyncActivity( Semantic("Refresh", ("Method", method.Method ) ) ) )
                {
                    try
                    {
                        TReturn result;

                        using ( cachingService.OpenRefreshContext( method, arg1 ) )
                        {
                            result = await method( arg1  );
                        }

                        activity.SetSuccess();

                        return result;
                    }
                    catch ( Exception e ) 
                    {
                        activity.SetException(e);
                        throw;
                    }
                }
            }

			
		    /// <summary>
            /// Removes a method call result from the cache giving the delegate of the method. This overload is for methods with 2 parameters.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>
            /// <param name="method">A delegate of the method to invalidate.</param>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
				/// <typeparam name="TParam2">Type of the second parameter.</typeparam>
			/// <param name="arg2">Value of the second parameter.</param>
	  			public static void Invalidate<TReturn, TParam1, TParam2>( this ICachingService cachingService,  Func<TParam1, TParam2, TReturn> method, TParam1 arg1, TParam2 arg2 )
            {
                cachingService.InvalidateDelegate( method, arg1, arg2 );
            }

			 /// <summary>
            /// Asynchronously removes a method call result from the cache giving the delegate of the method. This overload is for methods with 2 parameters.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>
            /// <param name="method">A delegate of the method to invalidate.</param>
			/// <returns>A <see cref="Task"/>.</returns>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
				/// <typeparam name="TParam2">Type of the second parameter.</typeparam>
			/// <param name="arg2">Value of the second parameter.</param>
				public static ValueTask InvalidateAsync<TReturn, TParam1, TParam2>( this ICachingService cachingService, Func<TParam1, TParam2, TReturn> method, TParam1 arg1, TParam2 arg2, CancellationToken cancellationToken = default )
            {
                return cachingService.InvalidateDelegateAsync( method, new object?[] { arg1, arg2 }, cancellationToken );
            }

			/// <summary>
            /// Evaluates a method, ignoring the currently cached value, and replaces the corresponding cache item with the new return value of the method. This overload is for methods with 2 parameters.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>
            /// <param name="method">A delegate of the method to evaluate.</param>
            /// <returns>The return value of <paramref name="method"/>.</returns>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
				/// <typeparam name="TParam2">Type of the second parameter.</typeparam>
			/// <param name="arg2">Value of the second parameter.</param>
				public static TReturn Refresh<TReturn,  TParam1, TParam2>(this ICachingService cachingService,  Func< TParam1, TParam2, TReturn> method, TParam1 arg1, TParam2 arg2 )
            {
				using ( var activity = cachingService.Logger.Default.OpenActivity( Semantic("Refresh", ("Method", method.Method ) ) ) )
                {
                    try
                    {
                        TReturn result;

                        using ( cachingService.OpenRefreshContext( method, arg1, arg2 ) )
                        {
                            result = method( arg1, arg2  );
                        }

                        activity.SetSuccess();

                        return result;
                    }
                    catch ( Exception e ) 
                    {
                        activity.SetException(e);
                        throw;
                    }
                }
            }

			/// <summary>
            /// Asynchronously evaluates a method, ignoring the currently cached value, and replaces the corresponding cache item with the new return value of the method. This overload is for methods with 2 parameters.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="method">A delegate of the method to evaluate.</param>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>    
            /// <returns>A <see cref="Task{TResult}"/> that evaluates to the return value of <paramref name="method"/>.</returns>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
				/// <typeparam name="TParam2">Type of the second parameter.</typeparam>
			/// <param name="arg2">Value of the second parameter.</param>
				public static async Task<TReturn> RefreshAsync<TReturn,  TParam1, TParam2>(  this ICachingService cachingService, Func<TParam1, TParam2, Task<TReturn>> method, TParam1 arg1, TParam2 arg2, CancellationToken cancellationToken = default )
            {
                using ( var activity = cachingService.Logger.Default.OpenAsyncActivity( Semantic("Refresh", ("Method", method.Method ) ) ) )
                {
                    try
                    {
                        TReturn result;

                        using ( cachingService.OpenRefreshContext( method, arg1, arg2 ) )
                        {
                            result = await method( arg1, arg2  );
                        }

                        activity.SetSuccess();

                        return result;
                    }
                    catch ( Exception e ) 
                    {
                        activity.SetException(e);
                        throw;
                    }
                }
            }

			
		    /// <summary>
            /// Removes a method call result from the cache giving the delegate of the method. This overload is for methods with 3 parameters.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>
            /// <param name="method">A delegate of the method to invalidate.</param>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
				/// <typeparam name="TParam2">Type of the second parameter.</typeparam>
			/// <param name="arg2">Value of the second parameter.</param>
				/// <typeparam name="TParam3">Type of the third parameter.</typeparam>
			/// <param name="arg3">Value of the third parameter.</param>
	  			public static void Invalidate<TReturn, TParam1, TParam2, TParam3>( this ICachingService cachingService,  Func<TParam1, TParam2, TParam3, TReturn> method, TParam1 arg1, TParam2 arg2, TParam3 arg3 )
            {
                cachingService.InvalidateDelegate( method, arg1, arg2, arg3 );
            }

			 /// <summary>
            /// Asynchronously removes a method call result from the cache giving the delegate of the method. This overload is for methods with 3 parameters.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>
            /// <param name="method">A delegate of the method to invalidate.</param>
			/// <returns>A <see cref="Task"/>.</returns>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
				/// <typeparam name="TParam2">Type of the second parameter.</typeparam>
			/// <param name="arg2">Value of the second parameter.</param>
				/// <typeparam name="TParam3">Type of the third parameter.</typeparam>
			/// <param name="arg3">Value of the third parameter.</param>
				public static ValueTask InvalidateAsync<TReturn, TParam1, TParam2, TParam3>( this ICachingService cachingService, Func<TParam1, TParam2, TParam3, TReturn> method, TParam1 arg1, TParam2 arg2, TParam3 arg3, CancellationToken cancellationToken = default )
            {
                return cachingService.InvalidateDelegateAsync( method, new object?[] { arg1, arg2, arg3 }, cancellationToken );
            }

			/// <summary>
            /// Evaluates a method, ignoring the currently cached value, and replaces the corresponding cache item with the new return value of the method. This overload is for methods with 3 parameters.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>
            /// <param name="method">A delegate of the method to evaluate.</param>
            /// <returns>The return value of <paramref name="method"/>.</returns>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
				/// <typeparam name="TParam2">Type of the second parameter.</typeparam>
			/// <param name="arg2">Value of the second parameter.</param>
				/// <typeparam name="TParam3">Type of the third parameter.</typeparam>
			/// <param name="arg3">Value of the third parameter.</param>
				public static TReturn Refresh<TReturn,  TParam1, TParam2, TParam3>(this ICachingService cachingService,  Func< TParam1, TParam2, TParam3, TReturn> method, TParam1 arg1, TParam2 arg2, TParam3 arg3 )
            {
				using ( var activity = cachingService.Logger.Default.OpenActivity( Semantic("Refresh", ("Method", method.Method ) ) ) )
                {
                    try
                    {
                        TReturn result;

                        using ( cachingService.OpenRefreshContext( method, arg1, arg2, arg3 ) )
                        {
                            result = method( arg1, arg2, arg3  );
                        }

                        activity.SetSuccess();

                        return result;
                    }
                    catch ( Exception e ) 
                    {
                        activity.SetException(e);
                        throw;
                    }
                }
            }

			/// <summary>
            /// Asynchronously evaluates a method, ignoring the currently cached value, and replaces the corresponding cache item with the new return value of the method. This overload is for methods with 3 parameters.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="method">A delegate of the method to evaluate.</param>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>    
            /// <returns>A <see cref="Task{TResult}"/> that evaluates to the return value of <paramref name="method"/>.</returns>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
				/// <typeparam name="TParam2">Type of the second parameter.</typeparam>
			/// <param name="arg2">Value of the second parameter.</param>
				/// <typeparam name="TParam3">Type of the third parameter.</typeparam>
			/// <param name="arg3">Value of the third parameter.</param>
				public static async Task<TReturn> RefreshAsync<TReturn,  TParam1, TParam2, TParam3>(  this ICachingService cachingService, Func<TParam1, TParam2, TParam3, Task<TReturn>> method, TParam1 arg1, TParam2 arg2, TParam3 arg3, CancellationToken cancellationToken = default )
            {
                using ( var activity = cachingService.Logger.Default.OpenAsyncActivity( Semantic("Refresh", ("Method", method.Method ) ) ) )
                {
                    try
                    {
                        TReturn result;

                        using ( cachingService.OpenRefreshContext( method, arg1, arg2, arg3 ) )
                        {
                            result = await method( arg1, arg2, arg3  );
                        }

                        activity.SetSuccess();

                        return result;
                    }
                    catch ( Exception e ) 
                    {
                        activity.SetException(e);
                        throw;
                    }
                }
            }

			
		    /// <summary>
            /// Removes a method call result from the cache giving the delegate of the method. This overload is for methods with 4 parameters.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>
            /// <param name="method">A delegate of the method to invalidate.</param>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
				/// <typeparam name="TParam2">Type of the second parameter.</typeparam>
			/// <param name="arg2">Value of the second parameter.</param>
				/// <typeparam name="TParam3">Type of the third parameter.</typeparam>
			/// <param name="arg3">Value of the third parameter.</param>
				/// <typeparam name="TParam4">Type of the 4-th parameter.</typeparam>
			/// <param name="arg4">Value of the 4-th parameter.</param>
	  			public static void Invalidate<TReturn, TParam1, TParam2, TParam3, TParam4>( this ICachingService cachingService,  Func<TParam1, TParam2, TParam3, TParam4, TReturn> method, TParam1 arg1, TParam2 arg2, TParam3 arg3, TParam4 arg4 )
            {
                cachingService.InvalidateDelegate( method, arg1, arg2, arg3, arg4 );
            }

			 /// <summary>
            /// Asynchronously removes a method call result from the cache giving the delegate of the method. This overload is for methods with 4 parameters.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>
            /// <param name="method">A delegate of the method to invalidate.</param>
			/// <returns>A <see cref="Task"/>.</returns>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
				/// <typeparam name="TParam2">Type of the second parameter.</typeparam>
			/// <param name="arg2">Value of the second parameter.</param>
				/// <typeparam name="TParam3">Type of the third parameter.</typeparam>
			/// <param name="arg3">Value of the third parameter.</param>
				/// <typeparam name="TParam4">Type of the 4-th parameter.</typeparam>
			/// <param name="arg4">Value of the 4-th parameter.</param>
				public static ValueTask InvalidateAsync<TReturn, TParam1, TParam2, TParam3, TParam4>( this ICachingService cachingService, Func<TParam1, TParam2, TParam3, TParam4, TReturn> method, TParam1 arg1, TParam2 arg2, TParam3 arg3, TParam4 arg4, CancellationToken cancellationToken = default )
            {
                return cachingService.InvalidateDelegateAsync( method, new object?[] { arg1, arg2, arg3, arg4 }, cancellationToken );
            }

			/// <summary>
            /// Evaluates a method, ignoring the currently cached value, and replaces the corresponding cache item with the new return value of the method. This overload is for methods with 4 parameters.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>
            /// <param name="method">A delegate of the method to evaluate.</param>
            /// <returns>The return value of <paramref name="method"/>.</returns>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
				/// <typeparam name="TParam2">Type of the second parameter.</typeparam>
			/// <param name="arg2">Value of the second parameter.</param>
				/// <typeparam name="TParam3">Type of the third parameter.</typeparam>
			/// <param name="arg3">Value of the third parameter.</param>
				/// <typeparam name="TParam4">Type of the 4-th parameter.</typeparam>
			/// <param name="arg4">Value of the 4-th parameter.</param>
				public static TReturn Refresh<TReturn,  TParam1, TParam2, TParam3, TParam4>(this ICachingService cachingService,  Func< TParam1, TParam2, TParam3, TParam4, TReturn> method, TParam1 arg1, TParam2 arg2, TParam3 arg3, TParam4 arg4 )
            {
				using ( var activity = cachingService.Logger.Default.OpenActivity( Semantic("Refresh", ("Method", method.Method ) ) ) )
                {
                    try
                    {
                        TReturn result;

                        using ( cachingService.OpenRefreshContext( method, arg1, arg2, arg3, arg4 ) )
                        {
                            result = method( arg1, arg2, arg3, arg4  );
                        }

                        activity.SetSuccess();

                        return result;
                    }
                    catch ( Exception e ) 
                    {
                        activity.SetException(e);
                        throw;
                    }
                }
            }

			/// <summary>
            /// Asynchronously evaluates a method, ignoring the currently cached value, and replaces the corresponding cache item with the new return value of the method. This overload is for methods with 4 parameters.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="method">A delegate of the method to evaluate.</param>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>    
            /// <returns>A <see cref="Task{TResult}"/> that evaluates to the return value of <paramref name="method"/>.</returns>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
				/// <typeparam name="TParam2">Type of the second parameter.</typeparam>
			/// <param name="arg2">Value of the second parameter.</param>
				/// <typeparam name="TParam3">Type of the third parameter.</typeparam>
			/// <param name="arg3">Value of the third parameter.</param>
				/// <typeparam name="TParam4">Type of the 4-th parameter.</typeparam>
			/// <param name="arg4">Value of the 4-th parameter.</param>
				public static async Task<TReturn> RefreshAsync<TReturn,  TParam1, TParam2, TParam3, TParam4>(  this ICachingService cachingService, Func<TParam1, TParam2, TParam3, TParam4, Task<TReturn>> method, TParam1 arg1, TParam2 arg2, TParam3 arg3, TParam4 arg4, CancellationToken cancellationToken = default )
            {
                using ( var activity = cachingService.Logger.Default.OpenAsyncActivity( Semantic("Refresh", ("Method", method.Method ) ) ) )
                {
                    try
                    {
                        TReturn result;

                        using ( cachingService.OpenRefreshContext( method, arg1, arg2, arg3, arg4 ) )
                        {
                            result = await method( arg1, arg2, arg3, arg4  );
                        }

                        activity.SetSuccess();

                        return result;
                    }
                    catch ( Exception e ) 
                    {
                        activity.SetException(e);
                        throw;
                    }
                }
            }

			
		    /// <summary>
            /// Removes a method call result from the cache giving the delegate of the method. This overload is for methods with 5 parameters.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>
            /// <param name="method">A delegate of the method to invalidate.</param>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
				/// <typeparam name="TParam2">Type of the second parameter.</typeparam>
			/// <param name="arg2">Value of the second parameter.</param>
				/// <typeparam name="TParam3">Type of the third parameter.</typeparam>
			/// <param name="arg3">Value of the third parameter.</param>
				/// <typeparam name="TParam4">Type of the 4-th parameter.</typeparam>
			/// <param name="arg4">Value of the 4-th parameter.</param>
				/// <typeparam name="TParam5">Type of the 5-th parameter.</typeparam>
			/// <param name="arg5">Value of the 5-th parameter.</param>
	  			public static void Invalidate<TReturn, TParam1, TParam2, TParam3, TParam4, TParam5>( this ICachingService cachingService,  Func<TParam1, TParam2, TParam3, TParam4, TParam5, TReturn> method, TParam1 arg1, TParam2 arg2, TParam3 arg3, TParam4 arg4, TParam5 arg5 )
            {
                cachingService.InvalidateDelegate( method, arg1, arg2, arg3, arg4, arg5 );
            }

			 /// <summary>
            /// Asynchronously removes a method call result from the cache giving the delegate of the method. This overload is for methods with 5 parameters.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>
            /// <param name="method">A delegate of the method to invalidate.</param>
			/// <returns>A <see cref="Task"/>.</returns>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
				/// <typeparam name="TParam2">Type of the second parameter.</typeparam>
			/// <param name="arg2">Value of the second parameter.</param>
				/// <typeparam name="TParam3">Type of the third parameter.</typeparam>
			/// <param name="arg3">Value of the third parameter.</param>
				/// <typeparam name="TParam4">Type of the 4-th parameter.</typeparam>
			/// <param name="arg4">Value of the 4-th parameter.</param>
				/// <typeparam name="TParam5">Type of the 5-th parameter.</typeparam>
			/// <param name="arg5">Value of the 5-th parameter.</param>
				public static ValueTask InvalidateAsync<TReturn, TParam1, TParam2, TParam3, TParam4, TParam5>( this ICachingService cachingService, Func<TParam1, TParam2, TParam3, TParam4, TParam5, TReturn> method, TParam1 arg1, TParam2 arg2, TParam3 arg3, TParam4 arg4, TParam5 arg5, CancellationToken cancellationToken = default )
            {
                return cachingService.InvalidateDelegateAsync( method, new object?[] { arg1, arg2, arg3, arg4, arg5 }, cancellationToken );
            }

			/// <summary>
            /// Evaluates a method, ignoring the currently cached value, and replaces the corresponding cache item with the new return value of the method. This overload is for methods with 5 parameters.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>
            /// <param name="method">A delegate of the method to evaluate.</param>
            /// <returns>The return value of <paramref name="method"/>.</returns>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
				/// <typeparam name="TParam2">Type of the second parameter.</typeparam>
			/// <param name="arg2">Value of the second parameter.</param>
				/// <typeparam name="TParam3">Type of the third parameter.</typeparam>
			/// <param name="arg3">Value of the third parameter.</param>
				/// <typeparam name="TParam4">Type of the 4-th parameter.</typeparam>
			/// <param name="arg4">Value of the 4-th parameter.</param>
				/// <typeparam name="TParam5">Type of the 5-th parameter.</typeparam>
			/// <param name="arg5">Value of the 5-th parameter.</param>
				public static TReturn Refresh<TReturn,  TParam1, TParam2, TParam3, TParam4, TParam5>(this ICachingService cachingService,  Func< TParam1, TParam2, TParam3, TParam4, TParam5, TReturn> method, TParam1 arg1, TParam2 arg2, TParam3 arg3, TParam4 arg4, TParam5 arg5 )
            {
				using ( var activity = cachingService.Logger.Default.OpenActivity( Semantic("Refresh", ("Method", method.Method ) ) ) )
                {
                    try
                    {
                        TReturn result;

                        using ( cachingService.OpenRefreshContext( method, arg1, arg2, arg3, arg4, arg5 ) )
                        {
                            result = method( arg1, arg2, arg3, arg4, arg5  );
                        }

                        activity.SetSuccess();

                        return result;
                    }
                    catch ( Exception e ) 
                    {
                        activity.SetException(e);
                        throw;
                    }
                }
            }

			/// <summary>
            /// Asynchronously evaluates a method, ignoring the currently cached value, and replaces the corresponding cache item with the new return value of the method. This overload is for methods with 5 parameters.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="method">A delegate of the method to evaluate.</param>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>    
            /// <returns>A <see cref="Task{TResult}"/> that evaluates to the return value of <paramref name="method"/>.</returns>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
				/// <typeparam name="TParam2">Type of the second parameter.</typeparam>
			/// <param name="arg2">Value of the second parameter.</param>
				/// <typeparam name="TParam3">Type of the third parameter.</typeparam>
			/// <param name="arg3">Value of the third parameter.</param>
				/// <typeparam name="TParam4">Type of the 4-th parameter.</typeparam>
			/// <param name="arg4">Value of the 4-th parameter.</param>
				/// <typeparam name="TParam5">Type of the 5-th parameter.</typeparam>
			/// <param name="arg5">Value of the 5-th parameter.</param>
				public static async Task<TReturn> RefreshAsync<TReturn,  TParam1, TParam2, TParam3, TParam4, TParam5>(  this ICachingService cachingService, Func<TParam1, TParam2, TParam3, TParam4, TParam5, Task<TReturn>> method, TParam1 arg1, TParam2 arg2, TParam3 arg3, TParam4 arg4, TParam5 arg5, CancellationToken cancellationToken = default )
            {
                using ( var activity = cachingService.Logger.Default.OpenAsyncActivity( Semantic("Refresh", ("Method", method.Method ) ) ) )
                {
                    try
                    {
                        TReturn result;

                        using ( cachingService.OpenRefreshContext( method, arg1, arg2, arg3, arg4, arg5 ) )
                        {
                            result = await method( arg1, arg2, arg3, arg4, arg5  );
                        }

                        activity.SetSuccess();

                        return result;
                    }
                    catch ( Exception e ) 
                    {
                        activity.SetException(e);
                        throw;
                    }
                }
            }

			
		    /// <summary>
            /// Removes a method call result from the cache giving the delegate of the method. This overload is for methods with 6 parameters.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>
            /// <param name="method">A delegate of the method to invalidate.</param>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
				/// <typeparam name="TParam2">Type of the second parameter.</typeparam>
			/// <param name="arg2">Value of the second parameter.</param>
				/// <typeparam name="TParam3">Type of the third parameter.</typeparam>
			/// <param name="arg3">Value of the third parameter.</param>
				/// <typeparam name="TParam4">Type of the 4-th parameter.</typeparam>
			/// <param name="arg4">Value of the 4-th parameter.</param>
				/// <typeparam name="TParam5">Type of the 5-th parameter.</typeparam>
			/// <param name="arg5">Value of the 5-th parameter.</param>
				/// <typeparam name="TParam6">Type of the 6-th parameter.</typeparam>
			/// <param name="arg6">Value of the 6-th parameter.</param>
	  			public static void Invalidate<TReturn, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6>( this ICachingService cachingService,  Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TReturn> method, TParam1 arg1, TParam2 arg2, TParam3 arg3, TParam4 arg4, TParam5 arg5, TParam6 arg6 )
            {
                cachingService.InvalidateDelegate( method, arg1, arg2, arg3, arg4, arg5, arg6 );
            }

			 /// <summary>
            /// Asynchronously removes a method call result from the cache giving the delegate of the method. This overload is for methods with 6 parameters.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>
            /// <param name="method">A delegate of the method to invalidate.</param>
			/// <returns>A <see cref="Task"/>.</returns>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
				/// <typeparam name="TParam2">Type of the second parameter.</typeparam>
			/// <param name="arg2">Value of the second parameter.</param>
				/// <typeparam name="TParam3">Type of the third parameter.</typeparam>
			/// <param name="arg3">Value of the third parameter.</param>
				/// <typeparam name="TParam4">Type of the 4-th parameter.</typeparam>
			/// <param name="arg4">Value of the 4-th parameter.</param>
				/// <typeparam name="TParam5">Type of the 5-th parameter.</typeparam>
			/// <param name="arg5">Value of the 5-th parameter.</param>
				/// <typeparam name="TParam6">Type of the 6-th parameter.</typeparam>
			/// <param name="arg6">Value of the 6-th parameter.</param>
				public static ValueTask InvalidateAsync<TReturn, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6>( this ICachingService cachingService, Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TReturn> method, TParam1 arg1, TParam2 arg2, TParam3 arg3, TParam4 arg4, TParam5 arg5, TParam6 arg6, CancellationToken cancellationToken = default )
            {
                return cachingService.InvalidateDelegateAsync( method, new object?[] { arg1, arg2, arg3, arg4, arg5, arg6 }, cancellationToken );
            }

			/// <summary>
            /// Evaluates a method, ignoring the currently cached value, and replaces the corresponding cache item with the new return value of the method. This overload is for methods with 6 parameters.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>
            /// <param name="method">A delegate of the method to evaluate.</param>
            /// <returns>The return value of <paramref name="method"/>.</returns>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
				/// <typeparam name="TParam2">Type of the second parameter.</typeparam>
			/// <param name="arg2">Value of the second parameter.</param>
				/// <typeparam name="TParam3">Type of the third parameter.</typeparam>
			/// <param name="arg3">Value of the third parameter.</param>
				/// <typeparam name="TParam4">Type of the 4-th parameter.</typeparam>
			/// <param name="arg4">Value of the 4-th parameter.</param>
				/// <typeparam name="TParam5">Type of the 5-th parameter.</typeparam>
			/// <param name="arg5">Value of the 5-th parameter.</param>
				/// <typeparam name="TParam6">Type of the 6-th parameter.</typeparam>
			/// <param name="arg6">Value of the 6-th parameter.</param>
				public static TReturn Refresh<TReturn,  TParam1, TParam2, TParam3, TParam4, TParam5, TParam6>(this ICachingService cachingService,  Func< TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TReturn> method, TParam1 arg1, TParam2 arg2, TParam3 arg3, TParam4 arg4, TParam5 arg5, TParam6 arg6 )
            {
				using ( var activity = cachingService.Logger.Default.OpenActivity( Semantic("Refresh", ("Method", method.Method ) ) ) )
                {
                    try
                    {
                        TReturn result;

                        using ( cachingService.OpenRefreshContext( method, arg1, arg2, arg3, arg4, arg5, arg6 ) )
                        {
                            result = method( arg1, arg2, arg3, arg4, arg5, arg6  );
                        }

                        activity.SetSuccess();

                        return result;
                    }
                    catch ( Exception e ) 
                    {
                        activity.SetException(e);
                        throw;
                    }
                }
            }

			/// <summary>
            /// Asynchronously evaluates a method, ignoring the currently cached value, and replaces the corresponding cache item with the new return value of the method. This overload is for methods with 6 parameters.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="method">A delegate of the method to evaluate.</param>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>    
            /// <returns>A <see cref="Task{TResult}"/> that evaluates to the return value of <paramref name="method"/>.</returns>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
				/// <typeparam name="TParam2">Type of the second parameter.</typeparam>
			/// <param name="arg2">Value of the second parameter.</param>
				/// <typeparam name="TParam3">Type of the third parameter.</typeparam>
			/// <param name="arg3">Value of the third parameter.</param>
				/// <typeparam name="TParam4">Type of the 4-th parameter.</typeparam>
			/// <param name="arg4">Value of the 4-th parameter.</param>
				/// <typeparam name="TParam5">Type of the 5-th parameter.</typeparam>
			/// <param name="arg5">Value of the 5-th parameter.</param>
				/// <typeparam name="TParam6">Type of the 6-th parameter.</typeparam>
			/// <param name="arg6">Value of the 6-th parameter.</param>
				public static async Task<TReturn> RefreshAsync<TReturn,  TParam1, TParam2, TParam3, TParam4, TParam5, TParam6>(  this ICachingService cachingService, Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, Task<TReturn>> method, TParam1 arg1, TParam2 arg2, TParam3 arg3, TParam4 arg4, TParam5 arg5, TParam6 arg6, CancellationToken cancellationToken = default )
            {
                using ( var activity = cachingService.Logger.Default.OpenAsyncActivity( Semantic("Refresh", ("Method", method.Method ) ) ) )
                {
                    try
                    {
                        TReturn result;

                        using ( cachingService.OpenRefreshContext( method, arg1, arg2, arg3, arg4, arg5, arg6 ) )
                        {
                            result = await method( arg1, arg2, arg3, arg4, arg5, arg6  );
                        }

                        activity.SetSuccess();

                        return result;
                    }
                    catch ( Exception e ) 
                    {
                        activity.SetException(e);
                        throw;
                    }
                }
            }

			
		    /// <summary>
            /// Removes a method call result from the cache giving the delegate of the method. This overload is for methods with 7 parameters.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>
            /// <param name="method">A delegate of the method to invalidate.</param>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
				/// <typeparam name="TParam2">Type of the second parameter.</typeparam>
			/// <param name="arg2">Value of the second parameter.</param>
				/// <typeparam name="TParam3">Type of the third parameter.</typeparam>
			/// <param name="arg3">Value of the third parameter.</param>
				/// <typeparam name="TParam4">Type of the 4-th parameter.</typeparam>
			/// <param name="arg4">Value of the 4-th parameter.</param>
				/// <typeparam name="TParam5">Type of the 5-th parameter.</typeparam>
			/// <param name="arg5">Value of the 5-th parameter.</param>
				/// <typeparam name="TParam6">Type of the 6-th parameter.</typeparam>
			/// <param name="arg6">Value of the 6-th parameter.</param>
				/// <typeparam name="TParam7">Type of the 7-th parameter.</typeparam>
			/// <param name="arg7">Value of the 7-th parameter.</param>
	  			public static void Invalidate<TReturn, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7>( this ICachingService cachingService,  Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TReturn> method, TParam1 arg1, TParam2 arg2, TParam3 arg3, TParam4 arg4, TParam5 arg5, TParam6 arg6, TParam7 arg7 )
            {
                cachingService.InvalidateDelegate( method, arg1, arg2, arg3, arg4, arg5, arg6, arg7 );
            }

			 /// <summary>
            /// Asynchronously removes a method call result from the cache giving the delegate of the method. This overload is for methods with 7 parameters.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>
            /// <param name="method">A delegate of the method to invalidate.</param>
			/// <returns>A <see cref="Task"/>.</returns>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
				/// <typeparam name="TParam2">Type of the second parameter.</typeparam>
			/// <param name="arg2">Value of the second parameter.</param>
				/// <typeparam name="TParam3">Type of the third parameter.</typeparam>
			/// <param name="arg3">Value of the third parameter.</param>
				/// <typeparam name="TParam4">Type of the 4-th parameter.</typeparam>
			/// <param name="arg4">Value of the 4-th parameter.</param>
				/// <typeparam name="TParam5">Type of the 5-th parameter.</typeparam>
			/// <param name="arg5">Value of the 5-th parameter.</param>
				/// <typeparam name="TParam6">Type of the 6-th parameter.</typeparam>
			/// <param name="arg6">Value of the 6-th parameter.</param>
				/// <typeparam name="TParam7">Type of the 7-th parameter.</typeparam>
			/// <param name="arg7">Value of the 7-th parameter.</param>
				public static ValueTask InvalidateAsync<TReturn, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7>( this ICachingService cachingService, Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TReturn> method, TParam1 arg1, TParam2 arg2, TParam3 arg3, TParam4 arg4, TParam5 arg5, TParam6 arg6, TParam7 arg7, CancellationToken cancellationToken = default )
            {
                return cachingService.InvalidateDelegateAsync( method, new object?[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7 }, cancellationToken );
            }

			/// <summary>
            /// Evaluates a method, ignoring the currently cached value, and replaces the corresponding cache item with the new return value of the method. This overload is for methods with 7 parameters.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>
            /// <param name="method">A delegate of the method to evaluate.</param>
            /// <returns>The return value of <paramref name="method"/>.</returns>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
				/// <typeparam name="TParam2">Type of the second parameter.</typeparam>
			/// <param name="arg2">Value of the second parameter.</param>
				/// <typeparam name="TParam3">Type of the third parameter.</typeparam>
			/// <param name="arg3">Value of the third parameter.</param>
				/// <typeparam name="TParam4">Type of the 4-th parameter.</typeparam>
			/// <param name="arg4">Value of the 4-th parameter.</param>
				/// <typeparam name="TParam5">Type of the 5-th parameter.</typeparam>
			/// <param name="arg5">Value of the 5-th parameter.</param>
				/// <typeparam name="TParam6">Type of the 6-th parameter.</typeparam>
			/// <param name="arg6">Value of the 6-th parameter.</param>
				/// <typeparam name="TParam7">Type of the 7-th parameter.</typeparam>
			/// <param name="arg7">Value of the 7-th parameter.</param>
				public static TReturn Refresh<TReturn,  TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7>(this ICachingService cachingService,  Func< TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TReturn> method, TParam1 arg1, TParam2 arg2, TParam3 arg3, TParam4 arg4, TParam5 arg5, TParam6 arg6, TParam7 arg7 )
            {
				using ( var activity = cachingService.Logger.Default.OpenActivity( Semantic("Refresh", ("Method", method.Method ) ) ) )
                {
                    try
                    {
                        TReturn result;

                        using ( cachingService.OpenRefreshContext( method, arg1, arg2, arg3, arg4, arg5, arg6, arg7 ) )
                        {
                            result = method( arg1, arg2, arg3, arg4, arg5, arg6, arg7  );
                        }

                        activity.SetSuccess();

                        return result;
                    }
                    catch ( Exception e ) 
                    {
                        activity.SetException(e);
                        throw;
                    }
                }
            }

			/// <summary>
            /// Asynchronously evaluates a method, ignoring the currently cached value, and replaces the corresponding cache item with the new return value of the method. This overload is for methods with 7 parameters.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="method">A delegate of the method to evaluate.</param>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>    
            /// <returns>A <see cref="Task{TResult}"/> that evaluates to the return value of <paramref name="method"/>.</returns>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
				/// <typeparam name="TParam2">Type of the second parameter.</typeparam>
			/// <param name="arg2">Value of the second parameter.</param>
				/// <typeparam name="TParam3">Type of the third parameter.</typeparam>
			/// <param name="arg3">Value of the third parameter.</param>
				/// <typeparam name="TParam4">Type of the 4-th parameter.</typeparam>
			/// <param name="arg4">Value of the 4-th parameter.</param>
				/// <typeparam name="TParam5">Type of the 5-th parameter.</typeparam>
			/// <param name="arg5">Value of the 5-th parameter.</param>
				/// <typeparam name="TParam6">Type of the 6-th parameter.</typeparam>
			/// <param name="arg6">Value of the 6-th parameter.</param>
				/// <typeparam name="TParam7">Type of the 7-th parameter.</typeparam>
			/// <param name="arg7">Value of the 7-th parameter.</param>
				public static async Task<TReturn> RefreshAsync<TReturn,  TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7>(  this ICachingService cachingService, Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, Task<TReturn>> method, TParam1 arg1, TParam2 arg2, TParam3 arg3, TParam4 arg4, TParam5 arg5, TParam6 arg6, TParam7 arg7, CancellationToken cancellationToken = default )
            {
                using ( var activity = cachingService.Logger.Default.OpenAsyncActivity( Semantic("Refresh", ("Method", method.Method ) ) ) )
                {
                    try
                    {
                        TReturn result;

                        using ( cachingService.OpenRefreshContext( method, arg1, arg2, arg3, arg4, arg5, arg6, arg7 ) )
                        {
                            result = await method( arg1, arg2, arg3, arg4, arg5, arg6, arg7  );
                        }

                        activity.SetSuccess();

                        return result;
                    }
                    catch ( Exception e ) 
                    {
                        activity.SetException(e);
                        throw;
                    }
                }
            }

			
		    /// <summary>
            /// Removes a method call result from the cache giving the delegate of the method. This overload is for methods with 8 parameters.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>
            /// <param name="method">A delegate of the method to invalidate.</param>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
				/// <typeparam name="TParam2">Type of the second parameter.</typeparam>
			/// <param name="arg2">Value of the second parameter.</param>
				/// <typeparam name="TParam3">Type of the third parameter.</typeparam>
			/// <param name="arg3">Value of the third parameter.</param>
				/// <typeparam name="TParam4">Type of the 4-th parameter.</typeparam>
			/// <param name="arg4">Value of the 4-th parameter.</param>
				/// <typeparam name="TParam5">Type of the 5-th parameter.</typeparam>
			/// <param name="arg5">Value of the 5-th parameter.</param>
				/// <typeparam name="TParam6">Type of the 6-th parameter.</typeparam>
			/// <param name="arg6">Value of the 6-th parameter.</param>
				/// <typeparam name="TParam7">Type of the 7-th parameter.</typeparam>
			/// <param name="arg7">Value of the 7-th parameter.</param>
				/// <typeparam name="TParam8">Type of the 8-th parameter.</typeparam>
			/// <param name="arg8">Value of the 8-th parameter.</param>
	  			public static void Invalidate<TReturn, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8>( this ICachingService cachingService,  Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TReturn> method, TParam1 arg1, TParam2 arg2, TParam3 arg3, TParam4 arg4, TParam5 arg5, TParam6 arg6, TParam7 arg7, TParam8 arg8 )
            {
                cachingService.InvalidateDelegate( method, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 );
            }

			 /// <summary>
            /// Asynchronously removes a method call result from the cache giving the delegate of the method. This overload is for methods with 8 parameters.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>
            /// <param name="method">A delegate of the method to invalidate.</param>
			/// <returns>A <see cref="Task"/>.</returns>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
				/// <typeparam name="TParam2">Type of the second parameter.</typeparam>
			/// <param name="arg2">Value of the second parameter.</param>
				/// <typeparam name="TParam3">Type of the third parameter.</typeparam>
			/// <param name="arg3">Value of the third parameter.</param>
				/// <typeparam name="TParam4">Type of the 4-th parameter.</typeparam>
			/// <param name="arg4">Value of the 4-th parameter.</param>
				/// <typeparam name="TParam5">Type of the 5-th parameter.</typeparam>
			/// <param name="arg5">Value of the 5-th parameter.</param>
				/// <typeparam name="TParam6">Type of the 6-th parameter.</typeparam>
			/// <param name="arg6">Value of the 6-th parameter.</param>
				/// <typeparam name="TParam7">Type of the 7-th parameter.</typeparam>
			/// <param name="arg7">Value of the 7-th parameter.</param>
				/// <typeparam name="TParam8">Type of the 8-th parameter.</typeparam>
			/// <param name="arg8">Value of the 8-th parameter.</param>
				public static ValueTask InvalidateAsync<TReturn, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8>( this ICachingService cachingService, Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TReturn> method, TParam1 arg1, TParam2 arg2, TParam3 arg3, TParam4 arg4, TParam5 arg5, TParam6 arg6, TParam7 arg7, TParam8 arg8, CancellationToken cancellationToken = default )
            {
                return cachingService.InvalidateDelegateAsync( method, new object?[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 }, cancellationToken );
            }

			/// <summary>
            /// Evaluates a method, ignoring the currently cached value, and replaces the corresponding cache item with the new return value of the method. This overload is for methods with 8 parameters.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>
            /// <param name="method">A delegate of the method to evaluate.</param>
            /// <returns>The return value of <paramref name="method"/>.</returns>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
				/// <typeparam name="TParam2">Type of the second parameter.</typeparam>
			/// <param name="arg2">Value of the second parameter.</param>
				/// <typeparam name="TParam3">Type of the third parameter.</typeparam>
			/// <param name="arg3">Value of the third parameter.</param>
				/// <typeparam name="TParam4">Type of the 4-th parameter.</typeparam>
			/// <param name="arg4">Value of the 4-th parameter.</param>
				/// <typeparam name="TParam5">Type of the 5-th parameter.</typeparam>
			/// <param name="arg5">Value of the 5-th parameter.</param>
				/// <typeparam name="TParam6">Type of the 6-th parameter.</typeparam>
			/// <param name="arg6">Value of the 6-th parameter.</param>
				/// <typeparam name="TParam7">Type of the 7-th parameter.</typeparam>
			/// <param name="arg7">Value of the 7-th parameter.</param>
				/// <typeparam name="TParam8">Type of the 8-th parameter.</typeparam>
			/// <param name="arg8">Value of the 8-th parameter.</param>
				public static TReturn Refresh<TReturn,  TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8>(this ICachingService cachingService,  Func< TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TReturn> method, TParam1 arg1, TParam2 arg2, TParam3 arg3, TParam4 arg4, TParam5 arg5, TParam6 arg6, TParam7 arg7, TParam8 arg8 )
            {
				using ( var activity = cachingService.Logger.Default.OpenActivity( Semantic("Refresh", ("Method", method.Method ) ) ) )
                {
                    try
                    {
                        TReturn result;

                        using ( cachingService.OpenRefreshContext( method, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 ) )
                        {
                            result = method( arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8  );
                        }

                        activity.SetSuccess();

                        return result;
                    }
                    catch ( Exception e ) 
                    {
                        activity.SetException(e);
                        throw;
                    }
                }
            }

			/// <summary>
            /// Asynchronously evaluates a method, ignoring the currently cached value, and replaces the corresponding cache item with the new return value of the method. This overload is for methods with 8 parameters.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="method">A delegate of the method to evaluate.</param>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>    
            /// <returns>A <see cref="Task{TResult}"/> that evaluates to the return value of <paramref name="method"/>.</returns>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
				/// <typeparam name="TParam2">Type of the second parameter.</typeparam>
			/// <param name="arg2">Value of the second parameter.</param>
				/// <typeparam name="TParam3">Type of the third parameter.</typeparam>
			/// <param name="arg3">Value of the third parameter.</param>
				/// <typeparam name="TParam4">Type of the 4-th parameter.</typeparam>
			/// <param name="arg4">Value of the 4-th parameter.</param>
				/// <typeparam name="TParam5">Type of the 5-th parameter.</typeparam>
			/// <param name="arg5">Value of the 5-th parameter.</param>
				/// <typeparam name="TParam6">Type of the 6-th parameter.</typeparam>
			/// <param name="arg6">Value of the 6-th parameter.</param>
				/// <typeparam name="TParam7">Type of the 7-th parameter.</typeparam>
			/// <param name="arg7">Value of the 7-th parameter.</param>
				/// <typeparam name="TParam8">Type of the 8-th parameter.</typeparam>
			/// <param name="arg8">Value of the 8-th parameter.</param>
				public static async Task<TReturn> RefreshAsync<TReturn,  TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8>(  this ICachingService cachingService, Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, Task<TReturn>> method, TParam1 arg1, TParam2 arg2, TParam3 arg3, TParam4 arg4, TParam5 arg5, TParam6 arg6, TParam7 arg7, TParam8 arg8, CancellationToken cancellationToken = default )
            {
                using ( var activity = cachingService.Logger.Default.OpenAsyncActivity( Semantic("Refresh", ("Method", method.Method ) ) ) )
                {
                    try
                    {
                        TReturn result;

                        using ( cachingService.OpenRefreshContext( method, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 ) )
                        {
                            result = await method( arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8  );
                        }

                        activity.SetSuccess();

                        return result;
                    }
                    catch ( Exception e ) 
                    {
                        activity.SetException(e);
                        throw;
                    }
                }
            }

			
		    /// <summary>
            /// Removes a method call result from the cache giving the delegate of the method. This overload is for methods with 9 parameters.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>
            /// <param name="method">A delegate of the method to invalidate.</param>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
				/// <typeparam name="TParam2">Type of the second parameter.</typeparam>
			/// <param name="arg2">Value of the second parameter.</param>
				/// <typeparam name="TParam3">Type of the third parameter.</typeparam>
			/// <param name="arg3">Value of the third parameter.</param>
				/// <typeparam name="TParam4">Type of the 4-th parameter.</typeparam>
			/// <param name="arg4">Value of the 4-th parameter.</param>
				/// <typeparam name="TParam5">Type of the 5-th parameter.</typeparam>
			/// <param name="arg5">Value of the 5-th parameter.</param>
				/// <typeparam name="TParam6">Type of the 6-th parameter.</typeparam>
			/// <param name="arg6">Value of the 6-th parameter.</param>
				/// <typeparam name="TParam7">Type of the 7-th parameter.</typeparam>
			/// <param name="arg7">Value of the 7-th parameter.</param>
				/// <typeparam name="TParam8">Type of the 8-th parameter.</typeparam>
			/// <param name="arg8">Value of the 8-th parameter.</param>
				/// <typeparam name="TParam9">Type of the 9-th parameter.</typeparam>
			/// <param name="arg9">Value of the 9-th parameter.</param>
	  			public static void Invalidate<TReturn, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9>( this ICachingService cachingService,  Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TReturn> method, TParam1 arg1, TParam2 arg2, TParam3 arg3, TParam4 arg4, TParam5 arg5, TParam6 arg6, TParam7 arg7, TParam8 arg8, TParam9 arg9 )
            {
                cachingService.InvalidateDelegate( method, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9 );
            }

			 /// <summary>
            /// Asynchronously removes a method call result from the cache giving the delegate of the method. This overload is for methods with 9 parameters.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>
            /// <param name="method">A delegate of the method to invalidate.</param>
			/// <returns>A <see cref="Task"/>.</returns>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
				/// <typeparam name="TParam2">Type of the second parameter.</typeparam>
			/// <param name="arg2">Value of the second parameter.</param>
				/// <typeparam name="TParam3">Type of the third parameter.</typeparam>
			/// <param name="arg3">Value of the third parameter.</param>
				/// <typeparam name="TParam4">Type of the 4-th parameter.</typeparam>
			/// <param name="arg4">Value of the 4-th parameter.</param>
				/// <typeparam name="TParam5">Type of the 5-th parameter.</typeparam>
			/// <param name="arg5">Value of the 5-th parameter.</param>
				/// <typeparam name="TParam6">Type of the 6-th parameter.</typeparam>
			/// <param name="arg6">Value of the 6-th parameter.</param>
				/// <typeparam name="TParam7">Type of the 7-th parameter.</typeparam>
			/// <param name="arg7">Value of the 7-th parameter.</param>
				/// <typeparam name="TParam8">Type of the 8-th parameter.</typeparam>
			/// <param name="arg8">Value of the 8-th parameter.</param>
				/// <typeparam name="TParam9">Type of the 9-th parameter.</typeparam>
			/// <param name="arg9">Value of the 9-th parameter.</param>
				public static ValueTask InvalidateAsync<TReturn, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9>( this ICachingService cachingService, Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TReturn> method, TParam1 arg1, TParam2 arg2, TParam3 arg3, TParam4 arg4, TParam5 arg5, TParam6 arg6, TParam7 arg7, TParam8 arg8, TParam9 arg9, CancellationToken cancellationToken = default )
            {
                return cachingService.InvalidateDelegateAsync( method, new object?[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9 }, cancellationToken );
            }

			/// <summary>
            /// Evaluates a method, ignoring the currently cached value, and replaces the corresponding cache item with the new return value of the method. This overload is for methods with 9 parameters.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>
            /// <param name="method">A delegate of the method to evaluate.</param>
            /// <returns>The return value of <paramref name="method"/>.</returns>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
				/// <typeparam name="TParam2">Type of the second parameter.</typeparam>
			/// <param name="arg2">Value of the second parameter.</param>
				/// <typeparam name="TParam3">Type of the third parameter.</typeparam>
			/// <param name="arg3">Value of the third parameter.</param>
				/// <typeparam name="TParam4">Type of the 4-th parameter.</typeparam>
			/// <param name="arg4">Value of the 4-th parameter.</param>
				/// <typeparam name="TParam5">Type of the 5-th parameter.</typeparam>
			/// <param name="arg5">Value of the 5-th parameter.</param>
				/// <typeparam name="TParam6">Type of the 6-th parameter.</typeparam>
			/// <param name="arg6">Value of the 6-th parameter.</param>
				/// <typeparam name="TParam7">Type of the 7-th parameter.</typeparam>
			/// <param name="arg7">Value of the 7-th parameter.</param>
				/// <typeparam name="TParam8">Type of the 8-th parameter.</typeparam>
			/// <param name="arg8">Value of the 8-th parameter.</param>
				/// <typeparam name="TParam9">Type of the 9-th parameter.</typeparam>
			/// <param name="arg9">Value of the 9-th parameter.</param>
				public static TReturn Refresh<TReturn,  TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9>(this ICachingService cachingService,  Func< TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TReturn> method, TParam1 arg1, TParam2 arg2, TParam3 arg3, TParam4 arg4, TParam5 arg5, TParam6 arg6, TParam7 arg7, TParam8 arg8, TParam9 arg9 )
            {
				using ( var activity = cachingService.Logger.Default.OpenActivity( Semantic("Refresh", ("Method", method.Method ) ) ) )
                {
                    try
                    {
                        TReturn result;

                        using ( cachingService.OpenRefreshContext( method, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9 ) )
                        {
                            result = method( arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9  );
                        }

                        activity.SetSuccess();

                        return result;
                    }
                    catch ( Exception e ) 
                    {
                        activity.SetException(e);
                        throw;
                    }
                }
            }

			/// <summary>
            /// Asynchronously evaluates a method, ignoring the currently cached value, and replaces the corresponding cache item with the new return value of the method. This overload is for methods with 9 parameters.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="method">A delegate of the method to evaluate.</param>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>    
            /// <returns>A <see cref="Task{TResult}"/> that evaluates to the return value of <paramref name="method"/>.</returns>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
				/// <typeparam name="TParam2">Type of the second parameter.</typeparam>
			/// <param name="arg2">Value of the second parameter.</param>
				/// <typeparam name="TParam3">Type of the third parameter.</typeparam>
			/// <param name="arg3">Value of the third parameter.</param>
				/// <typeparam name="TParam4">Type of the 4-th parameter.</typeparam>
			/// <param name="arg4">Value of the 4-th parameter.</param>
				/// <typeparam name="TParam5">Type of the 5-th parameter.</typeparam>
			/// <param name="arg5">Value of the 5-th parameter.</param>
				/// <typeparam name="TParam6">Type of the 6-th parameter.</typeparam>
			/// <param name="arg6">Value of the 6-th parameter.</param>
				/// <typeparam name="TParam7">Type of the 7-th parameter.</typeparam>
			/// <param name="arg7">Value of the 7-th parameter.</param>
				/// <typeparam name="TParam8">Type of the 8-th parameter.</typeparam>
			/// <param name="arg8">Value of the 8-th parameter.</param>
				/// <typeparam name="TParam9">Type of the 9-th parameter.</typeparam>
			/// <param name="arg9">Value of the 9-th parameter.</param>
				public static async Task<TReturn> RefreshAsync<TReturn,  TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9>(  this ICachingService cachingService, Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, Task<TReturn>> method, TParam1 arg1, TParam2 arg2, TParam3 arg3, TParam4 arg4, TParam5 arg5, TParam6 arg6, TParam7 arg7, TParam8 arg8, TParam9 arg9, CancellationToken cancellationToken = default )
            {
                using ( var activity = cachingService.Logger.Default.OpenAsyncActivity( Semantic("Refresh", ("Method", method.Method ) ) ) )
                {
                    try
                    {
                        TReturn result;

                        using ( cachingService.OpenRefreshContext( method, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9 ) )
                        {
                            result = await method( arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9  );
                        }

                        activity.SetSuccess();

                        return result;
                    }
                    catch ( Exception e ) 
                    {
                        activity.SetException(e);
                        throw;
                    }
                }
            }

			
		    /// <summary>
            /// Removes a method call result from the cache giving the delegate of the method. This overload is for methods with 10 parameters.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>
            /// <param name="method">A delegate of the method to invalidate.</param>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
				/// <typeparam name="TParam2">Type of the second parameter.</typeparam>
			/// <param name="arg2">Value of the second parameter.</param>
				/// <typeparam name="TParam3">Type of the third parameter.</typeparam>
			/// <param name="arg3">Value of the third parameter.</param>
				/// <typeparam name="TParam4">Type of the 4-th parameter.</typeparam>
			/// <param name="arg4">Value of the 4-th parameter.</param>
				/// <typeparam name="TParam5">Type of the 5-th parameter.</typeparam>
			/// <param name="arg5">Value of the 5-th parameter.</param>
				/// <typeparam name="TParam6">Type of the 6-th parameter.</typeparam>
			/// <param name="arg6">Value of the 6-th parameter.</param>
				/// <typeparam name="TParam7">Type of the 7-th parameter.</typeparam>
			/// <param name="arg7">Value of the 7-th parameter.</param>
				/// <typeparam name="TParam8">Type of the 8-th parameter.</typeparam>
			/// <param name="arg8">Value of the 8-th parameter.</param>
				/// <typeparam name="TParam9">Type of the 9-th parameter.</typeparam>
			/// <param name="arg9">Value of the 9-th parameter.</param>
				/// <typeparam name="TParam10">Type of the 10-th parameter.</typeparam>
			/// <param name="arg10">Value of the 10-th parameter.</param>
	  			public static void Invalidate<TReturn, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10>( this ICachingService cachingService,  Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TReturn> method, TParam1 arg1, TParam2 arg2, TParam3 arg3, TParam4 arg4, TParam5 arg5, TParam6 arg6, TParam7 arg7, TParam8 arg8, TParam9 arg9, TParam10 arg10 )
            {
                cachingService.InvalidateDelegate( method, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10 );
            }

			 /// <summary>
            /// Asynchronously removes a method call result from the cache giving the delegate of the method. This overload is for methods with 10 parameters.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>
            /// <param name="method">A delegate of the method to invalidate.</param>
			/// <returns>A <see cref="Task"/>.</returns>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
				/// <typeparam name="TParam2">Type of the second parameter.</typeparam>
			/// <param name="arg2">Value of the second parameter.</param>
				/// <typeparam name="TParam3">Type of the third parameter.</typeparam>
			/// <param name="arg3">Value of the third parameter.</param>
				/// <typeparam name="TParam4">Type of the 4-th parameter.</typeparam>
			/// <param name="arg4">Value of the 4-th parameter.</param>
				/// <typeparam name="TParam5">Type of the 5-th parameter.</typeparam>
			/// <param name="arg5">Value of the 5-th parameter.</param>
				/// <typeparam name="TParam6">Type of the 6-th parameter.</typeparam>
			/// <param name="arg6">Value of the 6-th parameter.</param>
				/// <typeparam name="TParam7">Type of the 7-th parameter.</typeparam>
			/// <param name="arg7">Value of the 7-th parameter.</param>
				/// <typeparam name="TParam8">Type of the 8-th parameter.</typeparam>
			/// <param name="arg8">Value of the 8-th parameter.</param>
				/// <typeparam name="TParam9">Type of the 9-th parameter.</typeparam>
			/// <param name="arg9">Value of the 9-th parameter.</param>
				/// <typeparam name="TParam10">Type of the 10-th parameter.</typeparam>
			/// <param name="arg10">Value of the 10-th parameter.</param>
				public static ValueTask InvalidateAsync<TReturn, TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10>( this ICachingService cachingService, Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TReturn> method, TParam1 arg1, TParam2 arg2, TParam3 arg3, TParam4 arg4, TParam5 arg5, TParam6 arg6, TParam7 arg7, TParam8 arg8, TParam9 arg9, TParam10 arg10, CancellationToken cancellationToken = default )
            {
                return cachingService.InvalidateDelegateAsync( method, new object?[] { arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10 }, cancellationToken );
            }

			/// <summary>
            /// Evaluates a method, ignoring the currently cached value, and replaces the corresponding cache item with the new return value of the method. This overload is for methods with 10 parameters.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>
            /// <param name="method">A delegate of the method to evaluate.</param>
            /// <returns>The return value of <paramref name="method"/>.</returns>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
				/// <typeparam name="TParam2">Type of the second parameter.</typeparam>
			/// <param name="arg2">Value of the second parameter.</param>
				/// <typeparam name="TParam3">Type of the third parameter.</typeparam>
			/// <param name="arg3">Value of the third parameter.</param>
				/// <typeparam name="TParam4">Type of the 4-th parameter.</typeparam>
			/// <param name="arg4">Value of the 4-th parameter.</param>
				/// <typeparam name="TParam5">Type of the 5-th parameter.</typeparam>
			/// <param name="arg5">Value of the 5-th parameter.</param>
				/// <typeparam name="TParam6">Type of the 6-th parameter.</typeparam>
			/// <param name="arg6">Value of the 6-th parameter.</param>
				/// <typeparam name="TParam7">Type of the 7-th parameter.</typeparam>
			/// <param name="arg7">Value of the 7-th parameter.</param>
				/// <typeparam name="TParam8">Type of the 8-th parameter.</typeparam>
			/// <param name="arg8">Value of the 8-th parameter.</param>
				/// <typeparam name="TParam9">Type of the 9-th parameter.</typeparam>
			/// <param name="arg9">Value of the 9-th parameter.</param>
				/// <typeparam name="TParam10">Type of the 10-th parameter.</typeparam>
			/// <param name="arg10">Value of the 10-th parameter.</param>
				public static TReturn Refresh<TReturn,  TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10>(this ICachingService cachingService,  Func< TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, TReturn> method, TParam1 arg1, TParam2 arg2, TParam3 arg3, TParam4 arg4, TParam5 arg5, TParam6 arg6, TParam7 arg7, TParam8 arg8, TParam9 arg9, TParam10 arg10 )
            {
				using ( var activity = cachingService.Logger.Default.OpenActivity( Semantic("Refresh", ("Method", method.Method ) ) ) )
                {
                    try
                    {
                        TReturn result;

                        using ( cachingService.OpenRefreshContext( method, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10 ) )
                        {
                            result = method( arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10  );
                        }

                        activity.SetSuccess();

                        return result;
                    }
                    catch ( Exception e ) 
                    {
                        activity.SetException(e);
                        throw;
                    }
                }
            }

			/// <summary>
            /// Asynchronously evaluates a method, ignoring the currently cached value, and replaces the corresponding cache item with the new return value of the method. This overload is for methods with 10 parameters.
            /// </summary>
            /// <typeparam name="TReturn">The return type of the method.</typeparam>
            /// <param name="method">A delegate of the method to evaluate.</param>
            /// <param name="cachingService">The <see cref="ICachingService"/>.</param>    
            /// <returns>A <see cref="Task{TResult}"/> that evaluates to the return value of <paramref name="method"/>.</returns>
			/// <typeparam name="TParam1">Type of the first parameter.</typeparam>
			/// <param name="arg1">Value of the first parameter.</param>
				/// <typeparam name="TParam2">Type of the second parameter.</typeparam>
			/// <param name="arg2">Value of the second parameter.</param>
				/// <typeparam name="TParam3">Type of the third parameter.</typeparam>
			/// <param name="arg3">Value of the third parameter.</param>
				/// <typeparam name="TParam4">Type of the 4-th parameter.</typeparam>
			/// <param name="arg4">Value of the 4-th parameter.</param>
				/// <typeparam name="TParam5">Type of the 5-th parameter.</typeparam>
			/// <param name="arg5">Value of the 5-th parameter.</param>
				/// <typeparam name="TParam6">Type of the 6-th parameter.</typeparam>
			/// <param name="arg6">Value of the 6-th parameter.</param>
				/// <typeparam name="TParam7">Type of the 7-th parameter.</typeparam>
			/// <param name="arg7">Value of the 7-th parameter.</param>
				/// <typeparam name="TParam8">Type of the 8-th parameter.</typeparam>
			/// <param name="arg8">Value of the 8-th parameter.</param>
				/// <typeparam name="TParam9">Type of the 9-th parameter.</typeparam>
			/// <param name="arg9">Value of the 9-th parameter.</param>
				/// <typeparam name="TParam10">Type of the 10-th parameter.</typeparam>
			/// <param name="arg10">Value of the 10-th parameter.</param>
				public static async Task<TReturn> RefreshAsync<TReturn,  TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10>(  this ICachingService cachingService, Func<TParam1, TParam2, TParam3, TParam4, TParam5, TParam6, TParam7, TParam8, TParam9, TParam10, Task<TReturn>> method, TParam1 arg1, TParam2 arg2, TParam3 arg3, TParam4 arg4, TParam5 arg5, TParam6 arg6, TParam7 arg7, TParam8 arg8, TParam9 arg9, TParam10 arg10, CancellationToken cancellationToken = default )
            {
                using ( var activity = cachingService.Logger.Default.OpenAsyncActivity( Semantic("Refresh", ("Method", method.Method ) ) ) )
                {
                    try
                    {
                        TReturn result;

                        using ( cachingService.OpenRefreshContext( method, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10 ) )
                        {
                            result = await method( arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10  );
                        }

                        activity.SetSuccess();

                        return result;
                    }
                    catch ( Exception e ) 
                    {
                        activity.SetException(e);
                        throw;
                    }
                }
            }

				
	
	}
}

