using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PhoneDataLookup
{
    public struct PhoneData
    {
        private static readonly string[] Carriers = new string[] { "未知", "移动", "联通", "电信", "电信虚拟运营商", "联通虚拟运营商", "移动虚拟运营商" };
        public readonly string Province;
        public readonly string City;
        public readonly string ZipCode;
        public readonly string AreaCode;
        public readonly byte Carrier;
        public PhoneData(string province, string city, string zipCode, string areaCode, byte carrier)
        {
            this.Province = province;
            this.City = city;
            this.ZipCode = zipCode;
            this.AreaCode = areaCode;
            this.Carrier = carrier;
        }


        public string GetCarrierDesc()
        {
            if (Carrier < 0 || Carrier >= Carriers.Length)
            {
                return Carriers[0];
            }
            return Carriers[Carrier];
        }

        override public string ToString()
        {
            return string.Format("Province: {0}\nCity: {1}\nZipCode: {2}\nAreaCode: {3}\nCarrier: {4}\n", this.Province, this.City, this.ZipCode, this.AreaCode, this.GetCarrierDesc());
        }
    }

    public class PhoneLookup
    {
        private static readonly PhoneData Empty = new PhoneData { };
        private byte[] _buf;
        private Dictionary<int, PhoneData> _cache = new Dictionary<int, PhoneData>();
        public PhoneLookup(String path)
        {
            if (!File.Exists(path))
            {
                return;
            }
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                _buf = new byte[fs.Length];
                fs.Read(_buf, 0, _buf.Length);
            }
        }

        public PhoneLookup(byte[] buf)
        {
            this._buf = buf;
        }

        public PhoneData Lookup(string phoneNumber)
        {
            char[] chars = phoneNumber.ToCharArray();
            foreach (char c in chars)
            {
                if (!char.IsDigit(c))
                {
                    return Empty;
                }
            }
            int key;
            try
            {
                key = int.Parse(phoneNumber.Substring(0, 7));
            }
            catch (Exception e)
            {
                throw e;
            }
            PhoneData record;
            if (_cache.TryGetValue(key, out record))
            {
                return record;
            }
            Buffer buffer = Buffer.Wrap(_buf);
            buffer.Position = 4;
            int startOffset = buffer.ReadInt();
            int endOffset = buffer.Capacity;
            int left = startOffset;
            int right = endOffset;
            int mid = (left + right) / 2;
            mid = AlignPosition(mid, startOffset);
            while (mid >= left && mid <= right)
            {
                if (mid == right)
                {
                    _cache.Add(key, Empty);
                    return Empty;
                }
                int compare = Compare(mid, key, buffer);
                if (compare == 0)
                {
                    var found = Extract(mid, buffer);
                    _cache.Add(key, found);
                    return found;
                }
                else if (mid == left)
                {
                    _cache.Add(key, Empty);
                    return Empty;
                }
                else if (compare > 0)
                {
                    int tempMid = (mid + left) / 2;
                    right = mid;
                    mid = AlignPosition(tempMid, startOffset);
                }
                else
                {
                    int tempMid = (mid + right) / 2;
                    left = mid;
                    mid = AlignPosition(tempMid, startOffset);
                }
            }
            _cache.Add(key, Empty);
            return Empty;
        }

        private PhoneData Extract(int indexStart, Buffer byteBuffer)
        {
            byteBuffer.Position = indexStart + 4;
            int infoStartIndex = byteBuffer.ReadInt();
            byte carrier = byteBuffer.ReadByte();
            byte[] bytes = new byte[ComputeLength(infoStartIndex, byteBuffer)];
            byteBuffer.Read(bytes);
            String propertyCare = Encoding.UTF8.GetString(bytes);
            String[] array = propertyCare.Split('|');
            return new PhoneData(array[0], array[1], array[2], array[3], carrier);
        }

        private long ComputeLength(int infoStartIndex, Buffer byteBuffer)
        {
            byteBuffer.Position = infoStartIndex;
            while (byteBuffer.ReadByte() != 0)
            {
                ;
            }
            long infoEnd = byteBuffer.Position - 1;
            byteBuffer.Position = infoStartIndex;
            return infoEnd - infoStartIndex;
        }

        private int Compare(int position, int key, Buffer byteBuffer)
        {
            byteBuffer.Position = position;
            int phonePrefix = byteBuffer.ReadInt();
            return phonePrefix.CompareTo(key);
        }

        private int AlignPosition(int pos, int startOffset)
        {
            int remain = (pos - startOffset) % 9;
            if (pos - startOffset < 9)
            {
                return pos - remain;
            }
            else if (remain != 0)
            {
                return pos + 9 - remain;
            }
            else
            {
                return pos;
            }
        }

        public void ClearCache()
        {
            _cache.Clear();
            _cache = null;
            _buf = null;
        }

        private class Buffer
        {
            private MemoryStream _stream;
            public Buffer(MemoryStream stream)
            {
                _stream = stream;
            }

            public static Buffer Wrap(byte[] array)
            {
                MemoryStream ms = new MemoryStream(array, 0, array.Length, true, true)
                {
                    Capacity = array.Length,
                    Position = 0
                };
                ms.SetLength(array.Length);
                return new Buffer(ms);
            }

            public int ReadInt()
            {
                byte[] bytes = new byte[4];
                for (int i = 0; i < 4; i++)
                {
                    bytes[i] = (byte)_stream.ReadByte();
                }
                return (bytes[0]) | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24);
            }

            public byte ReadByte()
            {
                return (byte)_stream.ReadByte();
            }

            public int Read(byte[] buffer)
            {
                return _stream.Read(buffer, 0, buffer.Length);
            }

            public long Position
            {
                get
                {
                    return _stream.Position;
                }
                set
                {
                    _stream.Position = value;
                }
            }

            public int Capacity
            {
                get { return _stream.Capacity; }
            }
        }
    }
}
