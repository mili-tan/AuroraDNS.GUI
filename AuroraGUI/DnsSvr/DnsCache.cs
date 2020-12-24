using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using ARSoft.Tools.Net;
using ARSoft.Tools.Net.Dns;
using AuroraGUI.DnsSvr;

namespace ArashiDNS
{
    public class DnsCache
    {
        public static void Add(DnsMessage dnsMessage)
        {
            if (DnsSettings.TtlRewrite &&
                dnsMessage.AnswerRecords.All(item => item.Name != DomainName.Parse("cache.auroradns.mili.one")))
            {
                var list = dnsMessage.AnswerRecords.Where(item =>
                    item.TimeToLive < DnsSettings.TtlMinTime - DateTime.Now.Second).ToList();
                foreach (var item in list)
                {
                    switch (item)
                    {
                        case ARecord aRecord:
                            dnsMessage.AnswerRecords.Add(
                                new ARecord(aRecord.Name, DnsSettings.TtlMinTime, aRecord.Address));
                            dnsMessage.AnswerRecords.Remove(item);
                            break;
                        case AaaaRecord aaaaRecord:
                            dnsMessage.AnswerRecords.Add(
                                new ARecord(aaaaRecord.Name, DnsSettings.TtlMinTime, aaaaRecord.Address));
                            dnsMessage.AnswerRecords.Remove(item);
                            break;
                        case CNameRecord cNameRecord:
                            dnsMessage.AnswerRecords.Add(new CNameRecord(cNameRecord.Name, DnsSettings.TtlMinTime,
                                cNameRecord.CanonicalName));
                            dnsMessage.AnswerRecords.Remove(item);
                            break;
                    }
                }
            }

            if (dnsMessage.AnswerRecords.Count <= 0) return;
            var dnsRecordBase = dnsMessage.AnswerRecords.FirstOrDefault();
            var ttl = DnsSettings.TtlRewrite && dnsRecordBase.TimeToLive < DnsSettings.TtlMinTime
                ? DnsSettings.TtlMinTime
                : dnsRecordBase.TimeToLive;
            Add(new CacheItem($"DNS:{dnsRecordBase.Name}:{dnsRecordBase.RecordType}",
                new CacheEntity
                {
                    List = dnsMessage.AnswerRecords.ToList(),
                    Time = DateTime.Now, ExpiresTime = DateTime.Now.AddSeconds(ttl)
                }), ttl);
        }

        public static void Add(CacheItem cacheItem, int ttl)
        {
            if (!MemoryCache.Default.Contains(cacheItem.Key))
                MemoryCache.Default.Add(cacheItem,
                    new CacheItemPolicy
                    {
                        AbsoluteExpiration =
                            DateTimeOffset.Now + TimeSpan.FromSeconds(ttl)
                    });
        }

        public static bool Contains(DnsMessage dnsQMsg)
        {
            return MemoryCache.Default.Contains(
                $"DNS:{dnsQMsg.Questions.FirstOrDefault().Name}:{dnsQMsg.Questions.FirstOrDefault().RecordType}");
        }

        public static CacheEntity Get(string key)
        {
            return (CacheEntity) MemoryCache.Default.Get(key) ??
                   throw new InvalidOperationException();
        }

        public static DnsMessage Get(DnsMessage dnsQMessage)
        {
            var dCacheMsg = new DnsMessage
            {
                IsRecursionAllowed = true,
                IsRecursionDesired = true,
                TransactionID = dnsQMessage.TransactionID
            };
            var getName =
                $"DNS:{dnsQMessage.Questions.FirstOrDefault().Name}:{dnsQMessage.Questions.FirstOrDefault().RecordType}";
            var cacheEntity = Get(getName);
            //var ttl = Convert.ToInt32((cacheEntity.ExpiredTime - DateTime.Now).TotalSeconds);
            foreach (var item in cacheEntity.List)
            {
                if (item is ARecord aRecord)
                    dCacheMsg.AnswerRecords.Add(new ARecord(aRecord.Name,
                        Convert.ToInt32((cacheEntity.Time.AddSeconds(item.TimeToLive) - DateTime.Now)
                            .TotalSeconds), aRecord.Address));
                else if (item is AaaaRecord aaaaRecord)
                    dCacheMsg.AnswerRecords.Add(new AaaaRecord(aaaaRecord.Name,
                        Convert.ToInt32((cacheEntity.Time.AddSeconds(item.TimeToLive) - DateTime.Now)
                            .TotalSeconds), aaaaRecord.Address));
                else if (item is CNameRecord cNameRecord)
                    dCacheMsg.AnswerRecords.Add(new CNameRecord(cNameRecord.Name,
                        Convert.ToInt32((cacheEntity.Time.AddSeconds(item.TimeToLive) - DateTime.Now)
                            .TotalSeconds), cNameRecord.CanonicalName));
                else
                    dCacheMsg.AnswerRecords.Add(item);
            }

            dCacheMsg.Questions.AddRange(dnsQMessage.Questions);
            dCacheMsg.AnswerRecords.Add(new TxtRecord(DomainName.Parse("cache.auroradns.mili.one"), 0,
                cacheEntity.ExpiresTime.ToString("r")));
            return dCacheMsg;
        }

        public class CacheEntity
        {
            public List<DnsRecordBase> List;
            public DateTime Time;
            public DateTime ExpiresTime;
        }
    }
}
