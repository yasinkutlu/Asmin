﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Asmin.Packages.AOP.Interceptor.Async
{
    partial class AsyncInterceptor
    {
        private sealed class AsyncStateMachine : IAsyncStateMachine
        {
            private readonly IAsyncInvocation asyncInvocation;
            private readonly object builder;
            private readonly ValueTask task;

            public AsyncStateMachine(IAsyncInvocation asyncInvocation, object builder, ValueTask task)
            {
                this.asyncInvocation = asyncInvocation;
                this.builder = builder;
                this.task = task;
            }

            public void MoveNext()
            {
                try
                {
                    var awaiter = this.task.GetAwaiter();

                    if (awaiter.IsCompleted)
                    {
                        awaiter.GetResult();
                        // TODO: validate `asyncInvocation.Result` against `asyncInvocation.Method.ReturnType`!
                        this.builder.SetResult(asyncInvocation.Result);
                    }
                    else
                    {
                        this.builder.AwaitOnCompleted(awaiter, this);
                    }
                }
                catch (Exception exception)
                {
                    this.builder.SetException(exception);
                }
            }

            public void SetStateMachine(IAsyncStateMachine stateMachine)
            {
            }
        }
    }
}
