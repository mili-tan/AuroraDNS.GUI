using System.Text;
using ARSoft.Tools.Net.Dns;

namespace OhMyDnsPackage
{
 public class MyDnsQuestion
    {
        //查询结构的3个组成
        byte[] _name;
        byte[] _type;
        byte[] _class;

        public MyDnsQuestion()
        {
            //初始化，默认查询A记录
            _type = new byte[] { 0x00, (byte)RecordType.A };
            _class = new byte[] { 0x00, (byte)RecordClass.INet };
        }

        public byte[] GetBytes()
        {
            byte[] result = new byte[_name.Length + _type.Length + _class.Length];
            _name.CopyTo(result, 0);
            _type.CopyTo(result, _name.Length);
            _class.CopyTo(result, result.Length - _class.Length);
            return result;
        }
        public RecordType Type
        {
            set => _type = new byte[] { 0x00, (byte)value };
        }
        public RecordClass Class
        {
            set => _class = new byte[] { 0x00, (byte)value };
        }
        public string Qname
        {
            set
            {
                string[] arr = value.Split('.');
                _name = new byte[value.Length + 2];
                int seek = 0;
                foreach (string word in arr)
                {
                    byte[] len = { (byte)word.Length };
                    len.CopyTo(_name, seek);
                    Encoding.UTF8.GetBytes(word).CopyTo(_name, seek + 1);
                    seek += word.Length + 1;
                }
            }
        }
    }
}
