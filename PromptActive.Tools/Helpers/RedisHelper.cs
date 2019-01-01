using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PromptActive.Tools.Utils;
using StackExchange.Redis;

namespace PromptActive.Tools.Helpers
{
    public class RedisHelper
    {
        #region Fields
        private static readonly Dictionary<string, (RedisHelper, int)> pool = new Dictionary<string, (RedisHelper, int)>();
        private ConnectionMultiplexer conn;
        #endregion

        #region Properties
        public bool IsConnected { get; private set; }
        public string Host { get; set; }

        public IDatabase Db { get; private set; }
        public IServer Server { get; private set; }
        public ISubscriber Sub { get; private set; }
        #endregion

        #region Constructors
        public static RedisHelper Create(string host)
        {
            try
            {
                var ip = IPAddress.Parse(host.Trim()).ToString();

                if (!pool.ContainsKey(ip)) pool.Add(ip, (new RedisHelper(), 0));
                var (redis, cnt) = pool[ip];
                redis.Host = ip;
                pool[ip] = (redis, ++cnt);

                return redis;
            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion

        #region Public Methods
        public bool Connect(string pwd = "", int port = 6379, bool CanReconnect = false)
        {
            try
            {
                if (IsConnected)
                {
                    if (CanReconnect) Disconnect();
                    else return true;
                }

                if (port < 1024 || port > 65535) throw new ArgumentOutOfRangeException($"NG, Port number is wrong. ({port})[1024-65535]");
                var config = new ConfigurationOptions();
                config.EndPoints.Add(Host, port);
                config.Password = pwd;
                config.AbortOnConnectFail = true;
                config.ConnectTimeout = 5000;
                conn = ConnectionMultiplexer.Connect(config);

                if (conn.IsConnected)
                {
                    Db = conn.GetDatabase();
                    Server = conn.GetServer(conn.GetEndPoints()[0]);
                    Sub = conn.GetSubscriber();

                    IsConnected = true;
                }
            }
            catch (Exception)
            {
                Disconnect();
                throw;
            }

            return IsConnected;
        }

        public async Task<bool> ConnectAsync(string pwd = "", int port = 6379, bool CanReconnect = false)
        {
            try
            {
                if (IsConnected)
                {
                    if (CanReconnect) Disconnect();
                    else return true;
                }

                var config = new ConfigurationOptions();
                config.EndPoints.Add(Host, port);
                config.Password = pwd;
                config.AbortOnConnectFail = true;
                conn = await ConnectionMultiplexer.ConnectAsync(config);

                if (conn.IsConnected)
                {
                    Db = conn.GetDatabase();
                    Server = conn.GetServer(conn.GetEndPoints()[0]);
                    Sub = conn.GetSubscriber();

                    IsConnected = true;
                }
            }
            catch (Exception)
            {
                Disconnect();
                throw;
            }

            return IsConnected;
        }

        public void Disconnect()
        {
            try
            {
                if (!IsConnected) return;

                var (redis, cnt) = pool[Host];
                cnt--;
                if (cnt <= 0)
                {
                    pool.Remove(Host);
                    if (conn != null && conn.IsConnected)
                    {
                        Sub.UnsubscribeAll();
                        conn.Close();
                    }
                }
                else
                {
                    pool[Host] = (redis, cnt);
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                IsConnected = false;
            }
        }

        public void Publish(string ch, string msg)
        {
            try
            {
                Sub.Publish(ch, msg);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void PublishJson<Tin>(string ch, Tin msg) => Publish(ch, msg.ToJson(true));

        public void Subscribe(string ch, Action<string> handler)
        {
            try
            {
                Sub.Unsubscribe(ch);
                Sub.Subscribe(ch, (c, m) =>
                {
                    try
                    {
                        handler(m);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError($"NG, {nameof(RedisHelper)}.Subscribe() -> {ex.StackTrace}");
                    }
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void SubscribeJson<Tin>(string ch, Action<Tin> handler)
        {
            try
            {
                Sub.Unsubscribe(ch);
                Sub.Subscribe(ch, (c, m) =>
                {
                    try
                    {
                        var s = Lib.JsonDeserialize<Tin>(m, true);
                        handler(s);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError($"NG, {nameof(RedisHelper)}.SubscribeJson() -> {ex.StackTrace}");
                    }
                });
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void Unsubscribe(string ch)
        {
            try
            {
                Sub.Unsubscribe(ch);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void SaveFile(string key, string title, string path)
        {
            try
            {
                byte[] result = File.ReadAllBytes(path);
                var str = Convert.ToBase64String(result);
                Db.HashSet(key, title, str);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task SaveFileAsync(string key, string title, string path)
        {
            try
            {
                byte[] result;
                using (FileStream stream = File.Open(path, FileMode.Open))
                {
                    result = new byte[stream.Length];
                    await stream.ReadAsync(result, 0, (int)stream.Length);
                }

                var str = Convert.ToBase64String(result);
                await Db.HashSetAsync(key, title, str);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void LoadFile(string key, string title, string savePath)
        {
            try
            {
                var str = Db.HashGet(key, title);
                var result = Convert.FromBase64String(str);
                File.WriteAllBytes(savePath, result);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task LoadFileAsync(string key, string title, string savePath)
        {
            try
            {
                string str = await Db.HashGetAsync(key, title);
                var result = Convert.FromBase64String(str);
                using (FileStream sourceStream = new FileStream(savePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
                {
                    await sourceStream.WriteAsync(result, 0, result.Length);
                };
            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion
    }
}
