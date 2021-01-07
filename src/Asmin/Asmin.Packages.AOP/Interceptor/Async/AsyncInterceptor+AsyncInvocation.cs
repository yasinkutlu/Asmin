﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace Asmin.Packages.AOP.Interceptor.Async
{
    partial class AsyncInterceptor
    {
        private sealed class AsyncInvocation : IAsyncInvocation
        {
            private readonly IInvocation invocation;
            private readonly IInvocationProceedInfo proceed;

            public AsyncInvocation(IInvocation invocation)
            {
                this.invocation = invocation;
                this.proceed = invocation.CaptureProceedInfo();
            }

            public IReadOnlyList<object> Arguments => invocation.Arguments;

            public MethodInfo Method => this.invocation.Method;

            public object Result { get; set; }

            public ValueTask ProceedAsync()
            {
                var previousReturnValue = this.invocation.ReturnValue;
                try
                {
                    this.proceed.Invoke();
                    var returnValue = this.invocation.ReturnValue;
                    if (returnValue != previousReturnValue)
                    {
                        var awaiter = returnValue.GetAwaiter();
                        if (awaiter.IsCompleted())
                        {
                            try
                            {
                                this.Result = awaiter.GetResult();
                                return default;
                            }
                            catch (Exception exception)
                            {
                                return new ValueTask(Task.FromException(exception));
                            }
                        }
                        else
                        {
                            var tcs = new TaskCompletionSource<bool>();
                            awaiter.OnCompleted(() =>
                            {
                                try
                                {
                                    this.Result = awaiter.GetResult();
                                    tcs.SetResult(true);
                                }
                                catch (Exception exception)
                                {
                                    tcs.SetException(exception);
                                }
                            });
                            return new ValueTask(tcs.Task);
                        }
                    }
                    else
                    {
                        return default;
                    }
                }
                finally
                {
                    this.invocation.ReturnValue = previousReturnValue;
                }
            }
        }
    }
}
