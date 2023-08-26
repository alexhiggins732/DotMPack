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
    public class DotMPackMap : DotMPack, IDictionary<DotMPack, DotMPack>
    {
        [DataMember(Order = 1)]
        public new List<byte> Payload
        {
            get
            {
                var result = this.EncodeToBytes().ToList();

                var pack = DotMPack.ParseFromBytes(result.ToArray());
                var map = pack.ToOrDefault<DotMPackMap>();
                return result;
            }
            set
            {
                var pack = DotMPack.ParseFromBytes(value.ToArray());
                if (pack is DotMPackMap map)
                {
                    this._collection = map._collection;
                }

            }
        }
        public int Count { get { return _collection.Count; } }
        public bool IsReadOnly { get { return _collection.IsReadOnly; } }
        public ICollection<DotMPack> Keys { get { return _collection.Keys; } }
        public ICollection<DotMPack> Values { get { return _collection.Values; } }
        public override object Value { get { return _collection; } }
        public override DotMPackType ValueType { get { return DotMPackType.Map; } }

        private IDictionary<DotMPack, DotMPack> _collection;

        public DotMPackMap()
        {
            _collection = new Dictionary<DotMPack, DotMPack>();
        }
        public DotMPackMap(IDictionary<DotMPack, DotMPack> seed)
        {
            _collection = new Dictionary<DotMPack, DotMPack>(seed);
        }
        public DotMPackMap(IEnumerable<KeyValuePair<DotMPack, DotMPack>> seed)
        {
            _collection = new Dictionary<DotMPack, DotMPack>();
            foreach (var v in seed)
                _collection.Add(v);
        }

        public override DotMPack this[DotMPack key]
        {
            get { return _collection[key]; }
            set { _collection[key] = value; }
        }

        public IEnumerator<KeyValuePair<DotMPack, DotMPack>> GetEnumerator()
        {
            return _collection.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public void Add(KeyValuePair<DotMPack, DotMPack> item)
        {
            _collection.Add(item);
        }
        public void Clear()
        {
            _collection.Clear();
        }
        public bool Contains(KeyValuePair<DotMPack, DotMPack> item)
        {
            return _collection.Contains(item);
        }
        public void CopyTo(KeyValuePair<DotMPack, DotMPack>[] array, int arrayIndex)
        {
            _collection.CopyTo(array, arrayIndex);
        }
        public bool Remove(KeyValuePair<DotMPack, DotMPack> item)
        {
            return _collection.Remove(item);
        }
        public bool ContainsKey(DotMPack key)
        {
            return _collection.ContainsKey(key);
        }
        public bool ContainsKeys(IEnumerable<DotMPack> keys)
        {
            return keys.All(ContainsKey);
        }
        public void Add(DotMPack key, DotMPack value)
        {
            _collection.Add(key, value);
        }
        public bool Remove(DotMPack key)
        {
            return _collection.Remove(key);
        }
        public bool TryGetValue(DotMPack key, out DotMPack value)
        {
            return _collection.TryGetValue(key, out value);
        }

        public override string ToString()
        {
            return String.Join(",", this.Select(kvp => kvp.Key.ToString() + ":" + kvp.Value.ToString()));
        }
    }

}
