using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using ARSoft.Tools.Net;
using ARSoft.Tools.Net.Dns;
using MojoUnity;
// ReSharper disable CollectionNeverUpdated.Global
#pragma warning disable 649
#pragma warning disable 1998

namespace AuroraGUI
{
    static class QueryResolve
    {
        public static List<DomainName> BlackList;
        public static Dictionary<DomainName, IPAddress> WhiteList;

        public static async Task ServerOnQueryReceived(object sender, QueryReceivedEventArgs e)
        {
            if (!(e.Query is DnsMessage query))
                return;

            IPAddress clientAddress = e.RemoteEndpoint.Address;
            if (DnsSettings.EDnsCustomize)
                clientAddress = DnsSettings.EDnsIp;
            else if (Equals(clientAddress, IPAddress.Loopback) || IpTools.InSameLaNet(clientAddress, MainWindow.LocIPAddr))
                clientAddress = MainWindow.IntIPAddr;

            DnsMessage response = query.CreateResponseInstance();

            if (query.Questions.Count <= 0)
                response.ReturnCode = ReturnCode.ServerFailure;

            else
            {
                if (query.Questions[0].RecordType == RecordType.A)
                {
                    foreach (DnsQuestion dnsQuestion in query.Questions)
                    {
                        response.ReturnCode = ReturnCode.NoError;

                        if (DnsSettings.DebugLog)
                        {
                            MyTools.BgwLog($@"| {DateTime.Now} {clientAddress} : { dnsQuestion.Name}");
                        }

                        if (DnsSettings.BlackListEnable && BlackList.Contains(dnsQuestion.Name))
                        {
                            //BlackList
                            ARecord blackRecord = new ARecord(dnsQuestion.Name, 10, IPAddress.Any);
                            response.AnswerRecords.Add(blackRecord);
                            if (DnsSettings.DebugLog)
                            {
                                MyTools.BgwLog(@"|- BlackList");
                            }
                        }

                        else if (DnsSettings.WhiteListEnable && WhiteList.ContainsKey(dnsQuestion.Name))
                        {
                            //WhiteList
                            ARecord blackRecord = new ARecord(dnsQuestion.Name, 10, WhiteList[dnsQuestion.Name]);
                            response.AnswerRecords.Add(blackRecord);
                            if (DnsSettings.DebugLog)
                            {
                                MyTools.BgwLog(@"|- WhiteList");
                            }
                        }

                        else
                        {
                            //Resolve
                            try
                            {
                                var (resolvedDnsList, statusCode) = ResolveOverHttps(clientAddress.ToString(), dnsQuestion.Name.ToString(),
                                    DnsSettings.ProxyEnable, DnsSettings.WProxy);
                                if (resolvedDnsList != null)
                                {
                                    foreach (var item in resolvedDnsList)
                                    {
                                        response.AnswerRecords.Add(item);
                                    }
                                }
                                else
                                {
                                    response.ReturnCode = (ReturnCode)statusCode;
                                }
                            }
                            catch (Exception ex)
                            {
                                response.ReturnCode = ReturnCode.ServerFailure;
                                MyTools.BgwLog(@"| " + ex);
                            }
                        }

                    }
                }
            }

            e.Response = response;

        }

        private static (List<dynamic> list, int statusCode) ResolveOverHttps(string clientIpAddress, string domainName,
            bool proxyEnable = false, IWebProxy wProxy = null)
        {
            string dnsStr;
            List<dynamic> recordList = new List<dynamic>();

            using (WebClient webClient = new WebClient())
            {
                if (proxyEnable)
                    webClient.Proxy = wProxy;

                dnsStr = webClient.DownloadString(
                    DnsSettings.HttpsDnsUrl +
                    @"?ct=application/dns-json&" +
                    $"name={domainName}&type=A&edns_client_subnet={clientIpAddress}");
            }

            JsonValue dnsJsonValue = Json.Parse(dnsStr);
            int statusCode = dnsJsonValue.AsObjectGetInt("Status");
            if (statusCode != 0)
            {
                return (null, statusCode);
            }

            List<JsonValue> dnsAnswerJsonList = dnsJsonValue.AsObjectGetArray("Answer");

            foreach (var itemJsonValue in dnsAnswerJsonList)
            {
                string answerAddr = itemJsonValue.AsObjectGetString("data");
                string answerDomainName = itemJsonValue.AsObjectGetString("name");
                int ttl = itemJsonValue.AsObjectGetInt("TTL");

                if (IpTools.IsIp(answerAddr))
                {
                    ARecord aRecord = new ARecord(
                        DomainName.Parse(answerDomainName), ttl, IPAddress.Parse(answerAddr));

                    recordList.Add(aRecord);
                }
                else
                {
                    CNameRecord cRecord = new CNameRecord(
                        DomainName.Parse(answerDomainName), ttl, DomainName.Parse(answerAddr));

                    recordList.Add(cRecord);

                    //recordList.AddRange(ResolveOverHttps(clientIpAddress,answerAddr));
                    //return recordList;
                }
            }

            return (recordList, statusCode);
        }
    }
}
