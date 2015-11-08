﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.Caching;
using Castle.DynamicProxy;

namespace Flatwhite.Strategy
{
    /*
        builder
                .RegisterInstance(svc)
                .As<IUserService>()
                .CacheWithStrategy(CacheStrategies.ForService<IUserService>()
                                                  .ForMember(x => x.GetById(Arg.Any<int>()))
                                                  .Duration(1000)
                                                  .VaryByParam("userId")
                                                  .VaryByCustom("threadId")

                                                  .ForMember(x => x.GetByEmail(Arg.Any<string>()))
                                                  .Duration(1000)
                                                  .VaryByParam("email")
                                                  .VaryByCustom("threadId")
                );
    */

    /// <summary>
    /// A strategy to cache on only selected methods
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TCacheAttribute"></typeparam>
    public class CacheSelectedMethodsInvocationStrategy<T, TCacheAttribute> : DefaultCacheStrategy, IMethodCacheStrategy<T>
        where T : class 
        where TCacheAttribute : OutputCacheAttribute, new()
    {
        
        private ExpressionSetting<T, TCacheAttribute> _currentExpression;

        private List<ExpressionSetting<T, TCacheAttribute>> Expressions => ((ExpressionBaseCacheAttributeProvider<T, TCacheAttribute>) _cacheAttributeProvider).Expressions;

        /// <summary>
        /// The setting for a specific invocation
        /// </summary>


        internal CacheSelectedMethodsInvocationStrategy() : base(new ExpressionBaseCacheAttributeProvider<T, TCacheAttribute>())
        {
            CacheKeyProvider = new DefaultCacheKeyProvider(_cacheAttributeProvider);
        }

        /// <summary>
        /// Specify the member for the output cache
        /// </summary>
        /// <param name="functionExpression"></param>
        /// <returns></returns>
        public IMethodCacheRuleBuilder<T> ForMember(Expression<Func<T, object>> functionExpression)
        {
            var expression = new ExpressionSetting<T, TCacheAttribute>
            {
                Expression = functionExpression,
                CacheAttribute = new TCacheAttribute()
            };
            Expressions.Add(expression);
            _currentExpression = expression;
            return this;
        }

        /// <summary>
        /// Set the cache duration
        /// </summary>
        /// <param name="duration"></param>
        /// <returns></returns>
        public IMethodCacheStrategy<T> Duration(int duration)
        {
            _currentExpression.CacheAttribute.Duration = duration;
            return this;
        }

        /// <summary>
        /// Set vary by param for the method
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public IMethodCacheStrategy<T> VaryByParam(string param)
        {
            _currentExpression.CacheAttribute.VaryByParam = param;
            return this;
        }

        /// <summary>
        /// Set var by custom for the method
        /// </summary>
        /// <param name="custom"></param>
        /// <returns></returns>
        public IMethodCacheStrategy<T> VaryByCustom(string custom)
        {
            _currentExpression.CacheAttribute.VaryByCustom = custom;
            return this;
        }

        /// <summary>
        /// Set the change monitors factory that will create the new change monitors when new cache entry is created
        /// </summary>
        /// <param name="changeMonitorFactory"></param>
        /// <returns></returns>
        public IMethodCacheStrategy<T> WithChangeMonitors(Func<IInvocation, IDictionary<string, object>, IEnumerable<ChangeMonitor>> changeMonitorFactory)
        {
            _currentExpression.ChangeMonitorFactory = changeMonitorFactory;
            return this;
        }

        /// <summary>
        /// Cache key provider
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override ICacheKeyProvider CacheKeyProvider { get; }

        /// <summary>
        /// Get change monitors
        /// </summary>
        /// <param name="invocation"></param>
        /// <param name="invocationContext"></param>
        /// <returns></returns>
        public override IEnumerable<ChangeMonitor> GetChangeMonitors(IInvocation invocation, IDictionary<string, object> invocationContext)
        {
            foreach (var e in Expressions)
            {
                var m = ExpressionHelper.ToMethodInfo(e.Expression);
                if (m == invocation.Method)
                {
                    return e.ChangeMonitorFactory(invocation, invocationContext);
                }
            }
            return new ChangeMonitor[0];
        }
    }
}