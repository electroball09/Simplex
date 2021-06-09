using System;
using System.Collections.Generic;
using System.Text;
using Simplex.Serialization;

namespace Simplex.Util
{
    public class StringWithLength : ISmpSerializer
    {
        private int _length = 0;
        private string _string = null;

        public Encoding Encoding { get; set; } = Encoding.UTF8;

        public StringWithLength(string str)
        {
            SetString(str);
        }

        public void SetString(string str)
        {
            _string = str;
            _length = Encoding.GetByteCount(_string);
        }

        public void Serialize(SmpSerializationStructure repo)
        {
            repo.Int32(ref _length);

            Span<byte> bytes = stackalloc byte[_length];
            Encoding.GetBytes(_string, bytes);
            repo.Bytes(ref bytes);
            SetString(Encoding.UTF8.GetString(bytes));
        }

        public override bool Equals(object obj)
        {
            if (!(obj is StringWithLength swl))
                return false;

            return swl._string == _string;
        }

        public override int GetHashCode()
        {
            return _string.GetHashCode();
        }

        public static implicit operator string(StringWithLength swl)
        {
            return swl._string;
        }

        public static implicit operator StringWithLength(string str)
        {
            StringWithLength swl = new StringWithLength(str);
            return swl;
        }

        public static bool operator ==(StringWithLength a, StringWithLength b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(StringWithLength a, StringWithLength b)
        {
            return !(a == b);
        }
    }
}
