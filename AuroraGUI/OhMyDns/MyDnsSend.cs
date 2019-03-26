using System;
using ARSoft.Tools.Net.Dns;

namespace OhMyDnsPackage
{
    static class MyDnsSend
    {
        public static byte[] GetQuestionData(string host, RecordType type = RecordType.A, byte[] id = null)
        {
            byte[] mId = id ?? Newid();

            var header = new MyDnsHeader();
            header.NewID(mId);
            var question = new MyDnsQuestion { Class = RecordClass.INet, Type = type, Qname = host };
            byte[] dataHead = header.GetBytes();
            byte[] dataQuestion = question.GetBytes();
            byte[] sendData = new byte[dataHead.Length + dataQuestion.Length];
            dataHead.CopyTo(sendData, 0);
            dataQuestion.CopyTo(sendData, dataHead.Length);

            return sendData;
        }

        private static byte[] Newid()
        {
            byte[] result = new byte[2];
            new Random().NextBytes(result);
            return result;
        }
    }
}
