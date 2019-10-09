// A wrapper dictionary class that has event capabilities without the bloat that comes along with a BindingList.
// It also contains a deep copy method.
using System.Collections;
using System.Collections.Generic;
namespace debugger.Util
{
    public class ListeningDict<T1,T2> : IDictionary<T1,T2>
    {
        private readonly Dictionary<T1,T2> InternalDict;
        public delegate void ListEvent();
        public delegate void OnItemAction(T1 key, T2 value);
        public event OnItemAction OnGet = (item, index) => { };        
        public event OnItemAction OnSet = (item, index) => { };
        public event OnItemAction OnAdd = (item, index) => { };
        public event OnItemAction OnRemove = (item, index) => { };
        public event ListEvent OnClear = () => { };

        public T2 this[T1 index]
        {
            get { OnGet.Invoke(index, InternalDict[index]); return InternalDict[index]; }
            set { InternalDict[index] = value; OnSet.Invoke(index, value); }
        }

        public int Count { get => InternalDict.Count; }
        public bool IsReadOnly { get => false; }

        public ICollection<T1> Keys => ((IDictionary<T1, T2>)InternalDict).Keys;

        public ICollection<T2> Values => ((IDictionary<T1, T2>)InternalDict).Values;

        public ListeningDict()
        {
            InternalDict = new Dictionary<T1,T2>();
        }

        public bool ContainsKey(T1 key)
        {
            return InternalDict.ContainsKey(key);
        }

        public void Add(T1 key, T2 value)
        {
            InternalDict.Add(key, value);
            OnAdd.Invoke(key, value);
        }

        public bool Remove(T1 key)
        {
            OnRemove.Invoke(key, InternalDict[key]);
            return InternalDict.Remove(key);
        }

        public bool TryGetValue(T1 key, out T2 value)
        {
            return InternalDict.TryGetValue(key, out value);
        }

        public void Add(KeyValuePair<T1, T2> item)
        {
            ((IDictionary<T1, T2>)InternalDict).Add(item);
            OnAdd.Invoke(item.Key, item.Value);
        }

        public void Clear()
        {
            OnClear.Invoke();
            InternalDict.Clear();
        }

        public bool Contains(KeyValuePair<T1, T2> item)
        {
            return ((IDictionary<T1, T2>)InternalDict).Contains(item);
        }

        public void CopyTo(KeyValuePair<T1, T2>[] array, int arrayIndex)
        {
            ((IDictionary<T1, T2>)InternalDict).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<T1, T2> item)
        {
            return ((IDictionary<T1, T2>)InternalDict).Remove(item);
            OnRemove.Invoke(item.Key, item.Value);
        }

        public IEnumerator<KeyValuePair<T1, T2>> GetEnumerator()
        {
            return ((IDictionary<T1, T2>)InternalDict).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IDictionary<T1, T2>)InternalDict).GetEnumerator();
        }
    }
}
