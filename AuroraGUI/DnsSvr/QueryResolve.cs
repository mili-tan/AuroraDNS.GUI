using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using System.Threading.Tasks;
using System.Windows.Forms;
using ARSoft.Tools.Net;
using ARSoft.Tools.Net.Dns;
using AuroraGUI.Tools;
using MojoUnity;
using static AuroraGUI.Tools.MyTools;

// ReSharper disable CollectionNeverUpdated.Global
#pragma warning disable 649
#pragma warning disable 1998

namespace AuroraGUI.DnsSvr
{
    static class QueryResolve
    {
        public static async Task ServerOnQueryReceived(object sender, QueryReceivedEventArgs e)
        {
            try
            {
                if (!(e.Query is DnsMessage query))
                    return;

                IPAddress clientAddress = e.RemoteEndpoint.Address;
                if (DnsSettings.EDnsCustomize)
                    clientAddress = Equals(DnsSettings.EDnsIp, IPAddress.Parse("0.0.0.1"))
                        ? IPAddress.Parse(MainWindow.IntIPAddr.ToString().Substring(
                            0, MainWindow.IntIPAddr.ToString().LastIndexOf(".", StringComparison.Ordinal)) + ".1") : DnsSettings.EDnsIp;
                else if (Equals(clientAddress, IPAddress.Loopback) || IpTools.InSameLaNet(clientAddress, MainWindow.LocIPAddr))
                    clientAddress = MainWindow.IntIPAddr;

                DnsMessage response = query.CreateResponseInstance();

                if (query.Questions.Count <= 0)
                    response.ReturnCode = ReturnCode.ServerFailure;

                else
                {
                    foreach (DnsQuestion dnsQuestion in query.Questions)
                    {
                        response.ReturnCode = ReturnCode.NoError;

                        if (DnsSettings.DebugLog)
                            BackgroundLog($@"| {DateTime.Now} {e.RemoteEndpoint.Address} : {dnsQuestion.Name} | {dnsQuestion.RecordType.ToString().ToUpper()}");

                        if (DomainName.Parse(new Uri(DnsSettings.HttpsDnsUrl).DnsSafeHost) == dnsQuestion.Name ||
                            DomainName.Parse(new Uri(DnsSettings.SecondHttpsDnsUrl).DnsSafeHost) == dnsQuestion.Name ||
                            DomainName.Parse(new Uri(UrlSettings.WhatMyIpApi).DnsSafeHost) == dnsQuestion.Name)
                        {
                            if (!DnsSettings.StartupOverDoH)
                            {
                                response.AnswerRecords.AddRange(new DnsClient(DnsSettings.SecondDnsIp, 5000)
                                    .Resolve(dnsQuestion.Name, dnsQuestion.RecordType).AnswerRecords);
                                if (DnsSettings.DebugLog)
                                    BackgroundLog($"| -- Startup SecondDns : {DnsSettings.SecondDnsIp}");
                            }
                            else
                            {
                                response.AnswerRecords.AddRange(ResolveOverHttpsByDnsJson(clientAddress.ToString(),
                                    dnsQuestion.Name.ToString(), "https://1.0.0.1/dns-query", DnsSettings.ProxyEnable,
                                    DnsSettings.WProxy, dnsQuestion.RecordType).list);
                                if (DnsSettings.DebugLog)
                                    BackgroundLog("| -- Startup DoH : https://1.0.0.1/dns-query");
                            }
                        }
                        else if (DnsSettings.DnsCacheEnable && MemoryCache.Default.Contains($"{dnsQuestion.Name}{dnsQuestion.RecordType}"))
                        {
                            response.AnswerRecords.AddRange(
                                (List<DnsRecordBase>)MemoryCache.Default.Get($"{dnsQuestion.Name}{dnsQuestion.RecordType}"));
                            response.AnswerRecords.Add(new TxtRecord(DomainName.Parse("cache.auroradns.mili.one"), 0,
                                "AuroraDNSC Cached"));

                            if (DnsSettings.DebugLog)
                                BackgroundLog($@"|- CacheContains : {dnsQuestion.Name} | Count : {MemoryCache.Default.Count()}");
                        }
                        else if (DnsSettings.BlackListEnable && DnsSettings.BlackList.Contains(dnsQuestion.Name))
                        {
                            response.AnswerRecords.Add(new ARecord(dnsQuestion.Name, 10, IPAddress.Any));
                            response.AnswerRecords.Add(new TxtRecord(DomainName.Parse("blacklist.auroradns.mili.one"), 0,
                                "AuroraDNSC Blocked"));

                            if (DnsSettings.DebugLog)
                                BackgroundLog(@"|- BlackList");
                        }
                        else if (DnsSettings.WhiteListEnable && DnsSettings.WhiteList.ContainsKey(dnsQuestion.Name))
                        {
                            List<DnsRecordBase> whiteRecords = new List<DnsRecordBase>();
                            if (!IpTools.IsIp(DnsSettings.WhiteList[dnsQuestion.Name]))
                                whiteRecords.AddRange(new DnsClient(DnsSettings.SecondDnsIp, 5000)
                                    .Resolve(dnsQuestion.Name, dnsQuestion.RecordType).AnswerRecords);
                            else
                                whiteRecords.Add(new ARecord(dnsQuestion.Name, 10,
                                    IPAddress.Parse(DnsSettings.WhiteList[dnsQuestion.Name])));

                            response.AnswerRecords.AddRange(whiteRecords);
                            response.AnswerRecords.Add(new TxtRecord(DomainName.Parse("whitelist.auroradns.mili.one"), 0,
                                "AuroraDNSC Rewrote"));

                            if (DnsSettings.DebugLog)
                                BackgroundLog(@"|- WhiteList");
                        }
                        else if (DnsSettings.ChinaListEnable && DomainNameInChinaList(dnsQuestion.Name))
                        {
                            try
                            {
                                var resolvedDnsList = ResolveOverHttpByDPlus(dnsQuestion.Name.ToString());
                                if (resolvedDnsList != null && resolvedDnsList != new List<DnsRecordBase>())
                                {
                                    resolvedDnsList.Add(new TxtRecord(DomainName.Parse("chinalist.auroradns.mili.one"),
                                        0, "AuroraDNSC ChinaList - DNSPod D+"));
                                    foreach (var item in resolvedDnsList)
                                        response.AnswerRecords.Add(item);

                                    if (DnsSettings.DebugLog)
                                        BackgroundLog(@"|- ChinaList - DNSPOD D+");

                                    if (DnsSettings.DnsCacheEnable && response.ReturnCode == ReturnCode.NoError)
                                        BackgroundWriteCache(
                                            new CacheItem($"{dnsQuestion.Name}{dnsQuestion.RecordType}",
                                                resolvedDnsList),
                                            resolvedDnsList[0].TimeToLive);
                                }
                            }
                            catch (Exception exception)
                            {
                                BackgroundLog(exception.ToString());
                            }
                        }
                        else
                        {
                            //Resolve
                            try
                            {
                                (List<DnsRecordBase> resolvedDnsList, ReturnCode statusCode) = DnsSettings.DnsMsgEnable
                                    ? ResolveOverHttpsByDnsMsg(clientAddress.ToString(),
                                        dnsQuestion.Name.ToString(), DnsSettings.HttpsDnsUrl, DnsSettings.ProxyEnable,
                                        DnsSettings.WProxy, dnsQuestion.RecordType)
                                    : ResolveOverHttpsByDnsJson(clientAddress.ToString(),
                                        dnsQuestion.Name.ToString(), DnsSettings.HttpsDnsUrl, DnsSettings.ProxyEnable,
                                        DnsSettings.WProxy, dnsQuestion.RecordType);

                                if (resolvedDnsList != null && resolvedDnsList.Count != 0 && statusCode == ReturnCode.NoError)
                                {
                                    response.AnswerRecords.AddRange(resolvedDnsList);

                                    if (DnsSettings.DnsCacheEnable)
                                        BackgroundWriteCache(
                                            new CacheItem($"{dnsQuestion.Name}{dnsQuestion.RecordType}", resolvedDnsList),
                                            resolvedDnsList[0].TimeToLive);
                                }
                                else if (statusCode == ReturnCode.ServerFailure)
                                {
                                    response.AnswerRecords = new DnsClient(DnsSettings.SecondDnsIp, 1000)
                                        .Resolve(dnsQuestion.Name, dnsQuestion.RecordType).AnswerRecords;
                                    BackgroundLog($"| -- SecondDns : {DnsSettings.SecondDnsIp}");
                                }
                                else
                                    response.ReturnCode = statusCode;
                            }
                            catch (Exception ex)
                            {
                                response.ReturnCode = ReturnCode.ServerFailure;
                                BackgroundLog(@"| " + ex);
                            }
                        }

                    }
                }
                e.Response = response;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                BackgroundLog(exception.ToString());
            }
        }

        public static (List<DnsRecordBase> list, ReturnCode statusCode) ResolveOverHttpsByDnsJson(string clientIpAddress,
            string domainName, string dohUrl,
            bool proxyEnable = false, IWebProxy wProxy = null, RecordType type = RecordType.A)
        {
            string dnsStr;
            List<DnsRecordBase> recordList = new List<DnsRecordBase>();

            try
            {
                dnsStr = MyCurl.GetString(dohUrl + @"?ct=application/dns-json&" +
                                          $"name={domainName}&type={type.ToString().ToUpper()}&edns_client_subnet={clientIpAddress}",
                    DnsSettings.Http2Enable, proxyEnable, wProxy, DnsSettings.AllowAutoRedirect);
            }
            catch (WebException e)
            {
                HttpWebResponse response = (HttpWebResponse) e.Response;
                try
                {
                    BackgroundLog($@"| - Catch WebException : {Convert.ToInt32(response.StatusCode)} {response.StatusCode} | {e.Status} | {domainName} | {response.ResponseUri}");
                    if (DnsSettings.HTTPStatusNotify)
                        MainWindow.NotifyIcon.ShowBalloonTip(360, "AuroraDNS - 错误",
                            $"异常 :{Convert.ToInt32(response.StatusCode)} {response.StatusCode} {Environment.NewLine} {domainName}", ToolTipIcon.Warning);
                    if (response.StatusCode == HttpStatusCode.BadRequest) DnsSettings.DnsMsgEnable = true;
                }
                catch (Exception exception)
                {
                    BackgroundLog($@"| - Catch WebException : {exception.Message} | {e.Status} | {domainName} | {dohUrl}" + @"?ct=application/dns-json&" +
                                  $"name={domainName}&type={type.ToString().ToUpper()}&edns_client_subnet={clientIpAddress}");
                    if (DnsSettings.HTTPStatusNotify)
                        MainWindow.NotifyIcon.ShowBalloonTip(360, "AuroraDNS - 错误",
                            $"异常 : {exception.Message} {Environment.NewLine} {domainName}", ToolTipIcon.Warning);
                }

                if (dohUrl != DnsSettings.HttpsDnsUrl) return (new List<DnsRecordBase>(), ReturnCode.ServerFailure);
                BackgroundLog($@"| -- SecondDoH : {DnsSettings.SecondHttpsDnsUrl}");
                return ResolveOverHttpsByDnsJson(clientIpAddress, domainName, DnsSettings.SecondHttpsDnsUrl,
                    proxyEnable, wProxy, type);
            }

            JsonValue dnsJsonValue = Json.Parse(dnsStr);

            int statusCode = dnsJsonValue.AsObjectGetInt("Status");
            if (statusCode != 0)
                return (new List<DnsRecordBase>(), (ReturnCode) statusCode);

            if (dnsStr.Contains("\"Answer\""))
            {
                var dnsAnswerJsonList = dnsJsonValue.AsObjectGetArray("Answer");

                foreach (var itemJsonValue in dnsAnswerJsonList)
                {
                    string answerAddr = itemJsonValue.AsObjectGetString("data");
                    string answerDomainName = itemJsonValue.AsObjectGetString("name");
                    int answerType = itemJsonValue.AsObjectGetInt("type");
                    int ttl = itemJsonValue.AsObjectGetInt("TTL");

                    switch (type)
                    {
                        case RecordType.A when Convert.ToInt32(RecordType.A) == answerType && !DnsSettings.Ipv4Disable:
                        {
                            ARecord aRecord = new ARecord(
                                DomainName.Parse(answerDomainName), ttl, IPAddress.Parse(answerAddr));

                            recordList.Add(aRecord);
                            break;
                        }

                        case RecordType.A:
                        {
                            if (Convert.ToInt32(RecordType.CName) == answerType)
                            {
                                CNameRecord cRecord = new CNameRecord(
                                    DomainName.Parse(answerDomainName), ttl, DomainName.Parse(answerAddr));

                                recordList.Add(cRecord);

                                //recordList.AddRange(ResolveOverHttps(clientIpAddress,answerAddr));
                                //return recordList;
                            }

                            break;
                        }

                        case RecordType.Aaaa when Convert.ToInt32(RecordType.Aaaa) == answerType && !DnsSettings.Ipv6Disable:
                        {
                            AaaaRecord aaaaRecord = new AaaaRecord(
                                DomainName.Parse(answerDomainName), ttl, IPAddress.Parse(answerAddr));
                            recordList.Add(aaaaRecord);
                            break;
                        }

                        case RecordType.Aaaa:
                        {
                            if (Convert.ToInt32(RecordType.CName) == answerType)
                            {
                                CNameRecord cRecord = new CNameRecord(
                                    DomainName.Parse(answerDomainName), ttl, DomainName.Parse(answerAddr));
                                recordList.Add(cRecord);
                            }

                            break;
                        }

                        case RecordType.CName when answerType == Convert.ToInt32(RecordType.CName):
                        {
                            CNameRecord cRecord = new CNameRecord(
                                DomainName.Parse(answerDomainName), ttl, DomainName.Parse(answerAddr));
                            recordList.Add(cRecord);
                            break;
                        }

                        case RecordType.Ns when answerType == Convert.ToInt32(RecordType.Ns):
                        {
                            NsRecord nsRecord = new NsRecord(
                                DomainName.Parse(answerDomainName), ttl, DomainName.Parse(answerAddr));
                            recordList.Add(nsRecord);
                            break;
                        }

                        case RecordType.Mx when answerType == Convert.ToInt32(RecordType.Mx):
                        {
                            MxRecord mxRecord = new MxRecord(
                                DomainName.Parse(answerDomainName), ttl,
                                ushort.Parse(answerAddr.Split(' ')[0]),
                                DomainName.Parse(answerAddr.Split(' ')[1]));
                            recordList.Add(mxRecord);
                            break;
                        }

                        case RecordType.Txt when answerType == Convert.ToInt32(RecordType.Txt):
                        {
                            TxtRecord txtRecord = new TxtRecord(DomainName.Parse(answerDomainName), ttl, answerAddr);
                            recordList.Add(txtRecord);
                            break;
                        }

                        case RecordType.Ptr when answerType == Convert.ToInt32(RecordType.Ptr):
                        {
                            PtrRecord ptrRecord = new PtrRecord(
                                DomainName.Parse(answerDomainName), ttl, DomainName.Parse(answerAddr));
                            recordList.Add(ptrRecord);
                            break;
                        }

                        default:
                            statusCode = Convert.ToInt32(ReturnCode.ServerFailure);
                            break;
                    }
                }
            }

            return (recordList, (ReturnCode) statusCode);
        }

        public static (List<DnsRecordBase> list, ReturnCode statusCode) ResolveOverHttpsByDnsMsg(string clientIpAddress, string domainName, string dohUrl,
            bool proxyEnable = false, IWebProxy wProxy = null, RecordType type = RecordType.A)
        {
            DnsMessage dnsMsg;
            DnsMessage dnsQMsg = new DnsMessage();
            dnsQMsg.Questions.Add(new DnsQuestion(DomainName.Parse(domainName), type, RecordClass.INet));
            dnsQMsg.IsEDnsEnabled = true;
            dnsQMsg.IsQuery = true;
            dnsQMsg.EDnsOptions.Options.Add(new ClientSubnetOption(24, IPAddress.Parse(clientIpAddress)));
            var dnsBase64String = Convert.ToBase64String(DNSEncoder.Encode(dnsQMsg)).TrimEnd('=')
                .Replace('+', '-').Replace('/', '_');

            try
            {
                var dnsDataBytes = MyCurl.GetData(
                    $"{dohUrl}?ct=application/dns-message&dns={dnsBase64String}",
                    DnsSettings.Http2Enable, proxyEnable, wProxy, DnsSettings.AllowAutoRedirect);
                dnsMsg = DnsMessage.Parse(dnsDataBytes);

                if (DnsSettings.Ipv4Disable || DnsSettings.Ipv6Disable)
                    foreach (var item in dnsMsg.AnswerRecords.ToArray())
                    {
                        if (item.RecordType == RecordType.A && DnsSettings.Ipv4Disable)
                            dnsMsg.AnswerRecords.Remove(item);
                        if (item.RecordType == RecordType.Aaaa && DnsSettings.Ipv6Disable)
                            dnsMsg.AnswerRecords.Remove(item);
                    }
            }
            catch (WebException e)
            {
                HttpWebResponse response = (HttpWebResponse) e.Response;
                try
                {
                    BackgroundLog(
                        $@"| - Catch WebException : {Convert.ToInt32(response.StatusCode)} {response.StatusCode} | {e.Status} |  {domainName} | {response.ResponseUri} | {dnsBase64String}");
                    if (response.StatusCode == HttpStatusCode.BadRequest) DnsSettings.DnsMsgEnable = false;
                }
                catch (Exception exception)
                {
                    BackgroundLog(
                        $@"| - Catch WebException : {exception.Message} | {e.Status} | {domainName} | {dohUrl} | {dnsBase64String}");
                }

                if (dohUrl != DnsSettings.HttpsDnsUrl) return (new List<DnsRecordBase>(), ReturnCode.ServerFailure);
                BackgroundLog($@"| -- SecondDoH : {DnsSettings.SecondHttpsDnsUrl}");
                return ResolveOverHttpsByDnsMsg(clientIpAddress, domainName, DnsSettings.SecondHttpsDnsUrl,
                    proxyEnable, wProxy, type);
            }
            return (dnsMsg.AnswerRecords, dnsMsg.ReturnCode);
        }

        public static List<DnsRecordBase> ResolveOverHttpByDPlus(string domainName)
        {
            try
            {
                string dnsStr = MyCurl.GetString($"http://119.29.29.29/d?dn={domainName}&ttl=1"
                    , DnsSettings.Http2Enable, DnsSettings.ProxyEnable, DnsSettings.WProxy,
                    DnsSettings.AllowAutoRedirect);
                if (string.IsNullOrWhiteSpace(dnsStr))
                    return null;

                var ttlTime = Convert.ToInt32(dnsStr.Split(',')[1]);
                var dnsAnswerList = dnsStr.Split(',')[0].Split(';');

                return dnsAnswerList
                    .Select(item => new ARecord(DomainName.Parse(domainName), ttlTime, IPAddress.Parse(item)))
                    .Cast<DnsRecordBase>().ToList();
            }
            catch (Exception e)
            {
                BackgroundLog(
                    $@"| - Catch WebException : {e.Message} | {domainName} | http://119.29.29.29/d?dn={domainName}&ttl=1");
                return new List<DnsRecordBase>();
            }
        }

        private static bool DomainNameInChinaList(DomainName name)
        {
            return DnsSettings.ChinaList.Contains(name.GetParentName()) ||
                   DnsSettings.ChinaList.Contains(name) ||
                   name.IsSubDomainOf(DomainName.Parse("cn.")) ||
                   name.ToString().Contains("xn--");
        }
    }
}
