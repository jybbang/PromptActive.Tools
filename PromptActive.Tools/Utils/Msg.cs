using System;
using System.Collections.Generic;
using System.Text;
using PubSub.Core;

namespace PromptActive.Tools.Utils
{
    public static class Msg
    {
        private static readonly Hub h = new Hub();

        #region Messenger
        public static void Pub<T>(T data = default(T))
        {
            try
            {
                h.Publish<T>(data);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static void Sub<T>(Action<T> handler)
        {
            try
            {
                h.Subscribe<T>(handler);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static void UnSub<T>()
        {
            try
            {
                h.Unsubscribe<T>();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static void UnSub<T>(Action<T> handler)
        {
            try
            {
                h.Unsubscribe<T>(handler);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static void UnSub<T>(object target = null, Action<T> handler = null)
        {
            try
            {
                h.Unsubscribe<T>(target, handler);
            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion
    }
}
