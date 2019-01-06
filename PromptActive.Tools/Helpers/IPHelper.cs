using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PromptActive.Tools.Utils;

namespace PromptActive.Tools.Helpers
{
    public static class IpHelper
    {
        #region Fields
        private static int index = -1;
        #endregion

        #region Public Methods
        public static List<(string ip, string subnet)> GetIpList(string macAddress)
        {
            try
            {
                if (string.IsNullOrEmpty(macAddress)) throw new ArgumentNullException("Mac address can not be empty.");
                macAddress = macAddress.Replace('-', ':');
                var mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
                var moc = mc.GetInstances();
                if (moc == null) return new List<(string ip, string subnet)>();
                foreach (var mo in moc)
                {
                    // Make sure this is a IP enabled device. Not something like memory card or VM Ware
                    if ((bool)mo["ipEnabled"])
                    {
                        if (mo["MACAddress"].Equals(macAddress))
                        {
                            index = Convert.ToInt32(mo["Index"]);
                            var ipList = mo["IPAddress"] as string[];
                            var subnetList = mo["IPSubnet"] as string[];
                            //var gwList = mo["DefaultIPGateway"] as string[];

                            var list = new List<(string, string)>();
                            for (int i = 0; i < ipList.Length; i++)
                            {
                                // IPv6 제외, IPv4만 저장
                                if (!ipList[i].Contains(":"))
                                {
                                    list.Add((ipList[i], subnetList[i]));
                                }
                            }
                            return list;
                        }
                    }
                }
                throw new ArgumentException("Mac address is not matched.");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static bool InitIp(string macAddress, string ip = null, string subnet = null, string gateway = null, int setpIpTimeout = 5000)
        {
            var ret = false;
            try
            {
                if (string.IsNullOrEmpty(ip))
                {
                    var before = GetIpList(macAddress);
                    ip = before.FirstOrDefault().ip;
                    if (string.IsNullOrEmpty(subnet)) subnet = before.FirstOrDefault().subnet;
                }

                ResetIpList(macAddress, new List<(string, string)> { (ip, subnet) }, gateway, setpIpTimeout);
            }
            catch (Exception ex)
            {
                Trace.TraceError($"{nameof(AddIpAsync)} -> {ex.StackTrace}");
            }
            finally
            {
                var after = GetIpList(macAddress);
                ret = (after.Count == 1 && after.FirstOrDefault().ip == ip) ? true : false;
            }
            return ret;
        }

        public static bool ResetIpList(string macAddress, IEnumerable<(string ip, string subnet)> allIpList, string gateway = null, int setpIpTimeout = 5000)
        {
            var ret = false;
            try
            {
                if (string.IsNullOrEmpty(macAddress)) throw new ArgumentNullException("NG, Mac address can not be empty.");
                if (allIpList == null || allIpList.Count() == 0) throw new ArgumentNullException("NG, Ip list can not be empty.");

                macAddress = macAddress.Replace('-', ':');

                var mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
                var moc = mc.GetInstances();
                if (moc == null) throw new InvalidOperationException("NG, No available configs.");
                foreach (ManagementObject mo in moc)
                {
                    // Make sure this is a IP enabled device. Not something like memory card or VM Ware
                    if ((bool)mo["IPEnabled"])
                    {
                        if (mo["MACAddress"].Equals(macAddress))
                        {
                            index = Convert.ToInt32(mo["Index"]);
                            var ips = new List<string>();
                            var subnets = new List<string>();
                            var gws = new List<string>();
                            foreach (var item in allIpList)
                            {
                                var ip = item.ip.ToIpAddress();
                                var subnet = item.subnet.ToIpAddress();
                                var ip1 = ip.ToInt();
                                if (ip1 < 1 || ip1 > 223 || string.IsNullOrEmpty(subnet) || subnet.Split('.')[0] != "255") continue;

                                ips.Add(ip);
                                subnets.Add(subnet);
                                gws.Add(gateway);
                            }

                            var newIp = mo.GetMethodParameters("EnableStatic");
                            newIp["IPAddress"] = ips.ToArray();
                            newIp["SubnetMask"] = subnets.ToArray();
                            var retSetIp = mo.InvokeMethod("EnableStatic", newIp, null);
                            var retip = (uint)retSetIp["ReturnValue"];
                            if (retip != 0) throw new InvalidOperationException(retip.ToString());

                            if (!string.IsNullOrEmpty(gateway))
                            {
                                var newGw = mo.GetMethodParameters("SetGateways");
                                newGw["DefaultIPGateway"] = gws.ToArray();
                                newGw["GatewayCostMetric"] = new int[] { 1 };
                                var retSetGw = mo.InvokeMethod("SetGateways", newGw, null);
                                var retgw = (uint)newGw["ReturnValue"];
                                if (retgw != 0) throw new InvalidOperationException(retgw.ToString());
                            }

                            Thread.Sleep(setpIpTimeout);
                            ret = true;
                            break;
                        }
                    }
                }
                throw new InvalidOperationException("NG, No available interface.");
            }
            catch (Exception ex)
            {
                ret = false;
                Trace.TraceError($"{nameof(AddIpAsync)} -> {ex.StackTrace}");
            }
            return ret;
        }

        public static async Task<bool> InitIpAsync(string macAddress, string ip = null, string subnet = null, string gateway = null, int setpIpTimeout = 5000)
        {
            var ret = false;
            try
            {
                if (string.IsNullOrEmpty(ip))
                {
                    var before = GetIpList(macAddress);
                    ip = before.FirstOrDefault().ip;
                    if (string.IsNullOrEmpty(subnet)) subnet = before.FirstOrDefault().subnet;
                }

                ret = await ResetIpListAsync(macAddress, new List<(string, string)> { (ip, subnet) }, gateway, setpIpTimeout).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ret = false;
                Trace.TraceError($"{nameof(AddIpAsync)} -> {ex.StackTrace}");
            }
            return ret;
        }

        public static async Task<bool> ResetIpListAsync(string macAddress, IEnumerable<(string ip, string subnet)> allIpList, string gateway = null, int setpIpTimeout = 5000)
        {
            var ret = false;
            try
            {
                if (string.IsNullOrEmpty(macAddress)) throw new ArgumentNullException("NG, Mac address can not be empty.");
                if (allIpList == null || allIpList.Count() == 0) throw new ArgumentNullException("NG, Ip list can not be empty.");

                macAddress = macAddress.Replace('-', ':');

                var mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
                var moc = mc.GetInstances();
                if (moc == null) throw new InvalidOperationException("NG, No available configs.");
                foreach (ManagementObject mo in moc)
                {
                    // Make sure this is a IP enabled device. Not something like memory card or VM Ware
                    if ((bool)mo["IPEnabled"])
                    {
                        if (mo["MACAddress"].Equals(macAddress))
                        {
                            index = Convert.ToInt32(mo["Index"]);
                            var ips = new List<string>();
                            var subnets = new List<string>();
                            var gws = new List<string>();
                            foreach (var item in allIpList)
                            {
                                var ip = item.ip.ToIpAddress();
                                var subnet = item.subnet.ToIpAddress();
                                var ip1 = ip.ToInt();
                                if (ip1 < 1 || ip1 > 223 || string.IsNullOrEmpty(subnet) || subnet.Split('.')[0] != "255") continue;

                                ips.Add(ip);
                                subnets.Add(subnet);
                                gws.Add(gateway);
                            }

                            var newIp = mo.GetMethodParameters("EnableStatic");
                            newIp["IPAddress"] = ips.ToArray();
                            newIp["SubnetMask"] = subnets.ToArray();
                            var retSetIp = mo.InvokeMethod("EnableStatic", newIp, null);
                            var retip = (uint)retSetIp["ReturnValue"];
                            if (retip != 0) throw new InvalidOperationException(retip.ToString());

                            if (!string.IsNullOrEmpty(gateway))
                            {
                                var newGw = mo.GetMethodParameters("SetGateways");
                                newGw["DefaultIPGateway"] = gws.ToArray();
                                newGw["GatewayCostMetric"] = new int[] { 1 };
                                var retSetGw = mo.InvokeMethod("SetGateways", newGw, null);
                                var retgw = (uint)newGw["ReturnValue"];
                                if (retgw != 0) throw new InvalidOperationException(retgw.ToString());
                            }

                            await Task.Delay(setpIpTimeout).ConfigureAwait(false);
                            ret = true;
                            break;
                        }
                    }
                }
                throw new InvalidOperationException("NG, No available interface.");
            }
            catch (Exception ex)
            {
                ret = false;
                Trace.TraceError($"{nameof(AddIpAsync)} -> {ex.StackTrace}");
            }
            return ret;
        }

        public static async Task<bool> SetIpListAsync(string macAddress, IEnumerable<(string ip, string subnet)> allIpList, string gateway = null, int setpIpTimeout = 5000)
        {
            var ret = false;
            try
            {
                if (allIpList == null || allIpList.Count() == 0) return ret;
                var before = GetIpList(macAddress);

                var addList = new List<(string ip, string subnet)>();
                foreach (var ips in allIpList)
                {
                    var exist = before.Exists(x => x.ip == ips.ip);
                    if (!exist)
                    {
                        addList.Add(ips);
                    }
                }
                var addret = false;
                if (addList.Count > 0)
                {
                    addret = await ResetIpListAsync(macAddress, before.Union(addList), gateway, setpIpTimeout).ConfigureAwait(false);
                }

                var removeList = new List<string>();
                foreach (var ips in before)
                {
                    var exist = allIpList.FirstOrDefault(x => x.ip == ips.ip);
                    if (string.IsNullOrEmpty(exist.ip))
                    {
                        removeList.Add(ips.ip);
                    }
                }
                var rmret = false;
                if (removeList.Count > 0)
                {
                    rmret = await RemoveIpAsync(macAddress, removeList, gateway, setpIpTimeout).ConfigureAwait(false);
                }
                ret = addret & rmret;
            }
            catch (Exception ex)
            {
                ret = false;
                Trace.TraceError($"{nameof(AddIpAsync)} -> {ex.StackTrace}");
            }
            return ret;
        }

        public static async Task<bool> AddIpAsync(string macAddress, IEnumerable<(string ip, string subnet)> addIpList, string gateway = null, int setpIpTimeout = 5000)
        {
            var ret = false;
            try
            {
                if (addIpList == null || addIpList.Count() == 0) return ret;

                var before = GetIpList(macAddress);
                before.Reverse();
                var isAdded = false;
                foreach (var item in addIpList)
                {
                    var exist = before.Exists(x => x.ip == item.ip);
                    if (!exist)
                    {
                        before.Add(item);
                        isAdded = true;
                    }
                }
                if (isAdded)
                {
                    ret = await ResetIpListAsync(macAddress, before, gateway, setpIpTimeout).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                ret = false;
                Trace.TraceError($"{nameof(AddIpAsync)} -> {ex.StackTrace}");
            }
            return ret;
        }

        public static async Task<bool> RemoveIpAsync(string macAddress, IEnumerable<string> removeIpList, string gateway = null, int setpIpTimeout = 5000)
        {
            var ret = false;
            try
            {
                if (removeIpList == null || removeIpList.Count() == 0) return ret;
                if (index < 0) throw new ArgumentException("NG, No available interface index.");

                var p = new Process();
                p.StartInfo.FileName = "netsh.exe";
                var infoString = string.Empty;

                foreach (var ip in removeIpList)
                {
                    var sb = new StringBuilder(" interface ip del address name=\"");
                    sb.Append(index);
                    sb.Append("\" addr=").Append(ip);

                    p.StartInfo.Arguments = sb.ToString();
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.RedirectStandardOutput = true;

                    try
                    {
                        p.Start();
                        p.WaitForExit(30000);
                        infoString = p.StandardOutput.ReadToEnd();
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError($"{nameof(RemoveIpAsync)} -> {ex.StackTrace}");
                    }
                }

                await Task.Delay(setpIpTimeout).ConfigureAwait(false);
                ret = true;
            }
            catch (Exception ex)
            {
                ret = false;
                Trace.TraceError($"{nameof(RemoveIpAsync)} -> {ex.StackTrace}");
            }
            return ret;
        }
        #endregion
    }
}
