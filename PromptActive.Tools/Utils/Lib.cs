using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace PromptActive.Tools.Utils
{
    public static class Lib
    {
        #region Dll Imports
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpWindowText, int nMaxCount);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string ipClassName, string IpWindowName);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetWindow(IntPtr hWnd, int uCmd);

        const int GW_HWNDFIRST = 0;
        const int GW_HWNDLAST = 1;
        const int GW_HWNDNEXT = 2;
        const int GW_HWNDPREV = 3;
        const int GW_OWNER = 4;
        const int GW_CHILD = 5;
        #endregion

        public static async Task ProcessKill(this Process proc, int sleep = 5000)
        {
            if (proc == null) return;

            try
            {
                proc.Kill();
                await Task.Delay(sleep).ConfigureAwait(false);
                var pID = proc.Id;
                var Title = new StringBuilder(256);
                var tempHwnd = FindWindow(null, null);
                while (tempHwnd.ToInt32() != 0)
                {
                    tempHwnd = GetWindow(tempHwnd, GW_HWNDNEXT);
                    GetWindowText(tempHwnd, Title, Title.Capacity + 1);
                    if (Title.Length >= 0)
                    {
                        GetWindowThreadProcessId(tempHwnd, out uint processID);
                        if (processID == pID) SendMessage(tempHwnd, 0x0010, -1, -1);
                    }
                }
            }
            catch
            {
            }
        }

        public static void Copied<T>(this T target, T src)
        {
            var props = target.GetType().GetProperties();
            if (props == null) return;
            foreach (var prop in props)
            {
                var value = src.GetType().GetProperty(prop.Name).GetValue(src);
                prop.SetValue(target, value);
            }
        }

        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> list) => list != null ? new ObservableCollection<T>(list) : null;

        public static string ToIpAddress(this string ip)
        {
            try
            {
                return IPAddress.Parse(ip).ToString();
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public static byte[] ToProtobuf<T>(this T obj)
        {
            var serialize = new MemoryStream();
            ProtoBuf.Serializer.Serialize<T>(serialize, obj);
            return serialize.ToArray();
        }

        public static T ProtobufDeserialize<T>(this byte[] proto)
        {
            var serialize = new MemoryStream(proto);
            return ProtoBuf.Serializer.Deserialize<T>(serialize);
        }

        public static string ToJson(this object obj, bool canTypeNameHandling = false)
        {
            try
            {
                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = canTypeNameHandling ? TypeNameHandling.Objects : TypeNameHandling.None
                };

                if (obj == null) return null;
                return JsonConvert.SerializeObject(obj, settings);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static T JsonDeserialize<T>(this string json, bool canTypeNameHandling = false)
        {
            try
            {
                T ret = default(T);
                if (string.IsNullOrEmpty(json)) return ret;
                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = canTypeNameHandling ? TypeNameHandling.Objects : TypeNameHandling.None,
                    Error = delegate (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
                    {
                        args.ErrorContext.Handled = true;
                        throw new InvalidCastException($"{nameof(JsonDeserialize)} -> {args.ErrorContext.Error.Message} -> ORIGINAL:{json}");
                    }
                };

                ret = JsonConvert.DeserializeObject<T>(json, settings);
                return ret;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static int ToInt(this IEnumerable<char> s)
        {
            try
            {
                if (s == null) return 0;
                var sb = new StringBuilder();
                foreach (var c in s)
                {
                    switch (c)
                    {
                        case '.':
                            goto breaskfor;
                        case '-':
                            if (sb.Length == 0) sb.Append(c);
                            break;
                        case '0':
                            sb.Append(c);
                            break;
                        case '1':
                            sb.Append(c);
                            break;
                        case '2':
                            sb.Append(c);
                            break;
                        case '3':
                            sb.Append(c);
                            break;
                        case '4':
                            sb.Append(c);
                            break;
                        case '5':
                            sb.Append(c);
                            break;
                        case '6':
                            sb.Append(c);
                            break;
                        case '7':
                            sb.Append(c);
                            break;
                        case '8':
                            sb.Append(c);
                            break;
                        case '9':
                            sb.Append(c);
                            break;
                        default:
                            break;
                    }
                }
            breaskfor:
                if (int.TryParse(sb.ToString(), out int ret)) return ret;
                return 0;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static decimal ToDec(this IEnumerable<char> s)
        {
            try
            {
                if (s == null) return 0;
                if (bool.TryParse(string.Concat(s), out bool retbool)) return retbool ? 1 : 0;
                var sb = new StringBuilder();
                bool hasDot = false;
                foreach (var c in s)
                {
                    switch (c)
                    {
                        case '.':
                            if (!hasDot)
                            {
                                sb.Append(c);
                                hasDot = true;
                            }
                            break;
                        case '-':
                            if (sb.Length == 0) sb.Append(c);
                            break;
                        case '0':
                            sb.Append(c);
                            break;
                        case '1':
                            sb.Append(c);
                            break;
                        case '2':
                            sb.Append(c);
                            break;
                        case '3':
                            sb.Append(c);
                            break;
                        case '4':
                            sb.Append(c);
                            break;
                        case '5':
                            sb.Append(c);
                            break;
                        case '6':
                            sb.Append(c);
                            break;
                        case '7':
                            sb.Append(c);
                            break;
                        case '8':
                            sb.Append(c);
                            break;
                        case '9':
                            sb.Append(c);
                            break;
                        default:
                            break;
                    }
                }
                if (decimal.TryParse(sb.ToString(), out decimal ret)) return ret;
                return 0;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static decimal ToDec(this IEnumerable<char> s, ref bool hasDot)
        {
            try
            {
                if (s == null) return 0;
                if (bool.TryParse(string.Concat(s), out bool retbool)) return retbool ? 1 : 0;
                var sb = new StringBuilder();
                foreach (var c in s)
                {
                    switch (c)
                    {
                        case '.':
                            if (!hasDot)
                            {
                                sb.Append(c);
                                hasDot = true;
                            }
                            break;
                        case '-':
                            if (sb.Length == 0) sb.Append(c);
                            break;
                        case '0':
                            sb.Append(c);
                            break;
                        case '1':
                            sb.Append(c);
                            break;
                        case '2':
                            sb.Append(c);
                            break;
                        case '3':
                            sb.Append(c);
                            break;
                        case '4':
                            sb.Append(c);
                            break;
                        case '5':
                            sb.Append(c);
                            break;
                        case '6':
                            sb.Append(c);
                            break;
                        case '7':
                            sb.Append(c);
                            break;
                        case '8':
                            sb.Append(c);
                            break;
                        case '9':
                            sb.Append(c);
                            break;
                        default:
                            break;
                    }
                }
                if (decimal.TryParse(sb.ToString(), out decimal ret)) return ret;
                return 0;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static bool ToBool(this decimal d) => d <= 0 ? false : true;

        public static bool ToBool(this UInt32 d) => d <= 0 ? false : true;

        public static bool ToBool(this IEnumerable<char> s)
        {
            try
            {
                if (s == null) return false;
                var ss = string.Concat(s).Trim().ToLowerInvariant();
                if (bool.TryParse(string.Concat(s), out bool ret)) return ret;
                else if (s.ToInt() <= 0) return false;
                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static string RemoveWhitespace(this string input)
        {
            try
            {
                return string.Join("", input.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static T ToEnum<T>(this string target)
        {
            try
            {
                return (T)Enum.Parse(typeof(T), target.RemoveWhitespace());
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static T ToEnum<T>(this string target, T def = default(T))
        {
            try
            {
                def = (T)Enum.Parse(typeof(T), target.RemoveWhitespace());
            }
            catch (Exception)
            {
            }
            return def;
        }

        public static string SecureStringToString(this SecureString target)
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(target);
                return Marshal.PtrToStringUni(valuePtr);

            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }

        public static string ToHashString(this string target)
        {
            try
            {
                using (SHA256 hash = SHA256Managed.Create())
                {
                    return String.Concat(hash
                      .ComputeHash(Encoding.UTF8.GetBytes(target))
                      .Select(item => item.ToString("x2")));
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static string GetNewPath(this string path, string targetDir = null)
        {
            try
            {
                targetDir = targetDir ?? Path.GetDirectoryName(path);
                var fname = Path.GetFileNameWithoutExtension(path);
                var fext = Path.GetExtension(path);

                var serial = Lib.ToInt(fname.SkipWhile(x => x != '('));
                var newPath = string.Empty;
                do
                {
                    fname = string.Concat(fname.TakeWhile(x => x != '('));
                    newPath = Path.GetFullPath($@"{targetDir}/{fname}({++serial}){fext}");
                }
                while (File.Exists(newPath));

                return newPath;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static string GetNewPathWithTime(this string path, string targetDir)
        {
            try
            {
                var fname = Path.GetFileNameWithoutExtension(path);
                var fext = Path.GetExtension(path);

                var newPath = string.Empty;
                do
                {
                    fname = string.Concat(fname.TakeWhile(x => x != '_'));
                    var ss = DateTime.Now.ToString("yyyyMMddHHmmss");
                    newPath = Path.GetFullPath($@"{targetDir}/{fname}_{ss}{fext}");
                }
                while (File.Exists(newPath));

                return newPath;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static string GetMD5HashFromFile(this string filePath)
        {
            try
            {
                using (FileStream file = new FileStream(filePath, FileMode.Open))
                {
                    System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                    byte[] hash = md5.ComputeHash(file);

                    StringBuilder result = new StringBuilder();
                    for (int i = 0; i < hash.Length; i++)
                    {
                        result.Append(hash[i].ToString("x2"));
                    }
                    return result.ToString();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static bool GetBit(this byte b, int bitNumber) => (b & (1 << bitNumber)) != 0;

        public static UInt16 Scale(this decimal from, decimal fromLow, decimal fromHigh, UInt16 toLow = 0, UInt16 toHigh = 27648)
        {
            try
            {
                if (fromHigh == fromLow) return 0;
                var min = fromLow < fromHigh ? fromLow : fromHigh;
                var max = fromLow > fromHigh ? fromLow : fromHigh;
                from = from > max ? max : from < min ? min : from;

                var X = (toHigh - toLow) / (fromHigh - fromLow);
                return Convert.ToUInt16(toHigh + X * (from - fromHigh));
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static decimal Scale(this UInt16 from, decimal toLow, decimal toHigh, UInt16 fromLow = 0, UInt16 fromHigh = 27648)
        {
            try
            {
                if (fromHigh == fromLow) return 0;
                var min = fromLow < fromHigh ? fromLow : fromHigh;
                var max = fromLow > fromHigh ? fromLow : fromHigh;
                from = from > max ? max : from < min ? min : from;

                var X = (toHigh - toLow) / (fromHigh - fromLow);
                return Convert.ToDecimal(toHigh + X * (from - fromHigh));
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static decimal Scale(this decimal from, decimal fromLow, decimal fromHigh, decimal toLow, decimal toHigh)
        {
            try
            {
                if (fromHigh == fromLow) return 0;
                var min = fromLow < fromHigh ? fromLow : fromHigh;
                var max = fromLow > fromHigh ? fromLow : fromHigh;
                from = from > max ? max : from < min ? min : from;

                var X = (toHigh - toLow) / (fromHigh - fromLow);
                return Convert.ToDecimal(toHigh + X * (from - fromHigh));
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static decimal RawToInt(this decimal raw)
        {
            try
            {
                var v = Convert.ToUInt16(raw);
                var ret = Convert.ToDecimal(BitConverter.ToInt16(BitConverter.GetBytes(v), 0));
                return ret;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static decimal RawToDInt(this decimal raw)
        {
            try
            {
                var v = Convert.ToUInt32(raw);
                var ret = Convert.ToDecimal(BitConverter.ToInt32(BitConverter.GetBytes(v), 0));
                return ret;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static decimal RawToReal(this decimal raw)
        {
            try
            {
                var v = Convert.ToUInt32(raw);
                var v2 = BitConverter.GetBytes(v);
                var v3 = BitConverter.ToSingle(v2, 0);
                var ret = Convert.ToDecimal(v3);
                return ret;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static decimal IntToRaw(this decimal target)
        {
            try
            {
                var v = Convert.ToInt16(target);
                var ret = Convert.ToDecimal(BitConverter.ToUInt16(BitConverter.GetBytes(v), 0));
                return ret;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static decimal DIntToRaw(this decimal target)
        {
            try
            {
                var v = Convert.ToInt32(target);
                var ret = Convert.ToDecimal(BitConverter.ToUInt32(BitConverter.GetBytes(v), 0));
                return ret;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static decimal RealToRaw(this decimal target)
        {
            try
            {
                var v = Convert.ToSingle(target);
                var ret = Convert.ToDecimal(BitConverter.ToUInt32(BitConverter.GetBytes(v), 0));
                return ret;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
