using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HigginsSoft.DotMPack
{

    [DataContract]
    public sealed class DotMPackArray : DotMPack, IList<DotMPack>
    {
        [DataMember(Order = 1)]
        public new List<byte> Payload
        {
            get
            {
                return this.EncodeToBytes().ToList();
            }
            set
            {
                var pack = DotMPack.ParseFromBytes(value.ToArray()).To<DotMPackArray>();
                this._collection = pack._collection;
            }
        }

        public override object Value { get { return _collection.AsReadOnly(); } }
        public override DotMPackType ValueType { get { return DotMPackType.Array; } }

        private List<DotMPack> _collection = new List<DotMPack>();

        public DotMPackArray()
        {
        }
        public DotMPackArray(IEnumerable<DotMPack> seed)
        {
            foreach (var v in seed)
                _collection.Add(v);
        }

        public override DotMPack this[int index]
        {
            get { return _collection[index]; }
            set { _collection[index] = value; }
        }

        public int Count
        {
            get { return _collection.Count; }
        }
        public bool IsReadOnly
        {
            get { return false; }
        }
        public void Add(DotMPack item)
        {
            _collection.Add(item);
        }
        public void Clear()
        {
            _collection.Clear();
        }
        public bool Contains(DotMPack item)
        {
            return _collection.Contains(item);
        }
        public void CopyTo(DotMPack[] array, int arrayIndex)
        {
            _collection.CopyTo(array, arrayIndex);
        }
        public int IndexOf(DotMPack item)
        {
            return _collection.IndexOf(item);
        }
        public void Insert(int index, DotMPack item)
        {
            _collection.Insert(index, item);
        }
        public bool Remove(DotMPack item)
        {
            return _collection.Remove(item);
        }
        public void RemoveAt(int index)
        {
            _collection.RemoveAt(index);
        }
        public IEnumerator<DotMPack> GetEnumerator()
        {
            return _collection.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _collection.GetEnumerator();
        }
        public override string ToString()
        {
            return String.Join(",", this.Select(v => v.ToString()));
        }


        public static implicit operator DotMPackArray(object[] args)
            => new DotMPackArray(args.Select(DotMPack.From).ToArray());
    }

}
