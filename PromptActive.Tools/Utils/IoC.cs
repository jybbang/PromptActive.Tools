using System;
using System.Collections.Generic;
using System.Text;
using DryIoc;

namespace PromptActive.Tools.Utils
{
    public static class IoC
    {
        private static readonly Container c = new Container();

        #region IoC
        public static bool CanResolve<T>(string serviceName = null)
        {
            try
            {
                return c.IsRegistered<T>(serviceName);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static bool TryResolve<T>(out T instance, string serviceName = null)
        {
            try
            {
                instance = default(T);
                var ret = c.IsRegistered<T>(serviceName);
                if (ret)
                {
                    instance = c.Resolve<T>(serviceName);
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static T Resolve<T>(string serviceName = null)
        {
            try
            {
                return c.Resolve<T>(serviceName);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static IEnumerable<T> ResolveAll<T>(string serviceName = null)
        {
            try
            {
                return c.ResolveMany<T>(serviceKey: serviceName);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static void Register<TService, TSIngleton>(string serviceName = null, bool isSingleton = true)
        {
            try
            {
                if (isSingleton) c.Register(typeof(TService), typeof(TSIngleton), Reuse.Singleton, serviceKey: serviceName);
                else c.Register(typeof(TService), typeof(TSIngleton), serviceKey: serviceName);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static void Register<T>(object instance, string serviceName = null)
        {
            try
            {
                c.UseInstance(typeof(T), instance, serviceKey: serviceName);
            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion
    }
}
