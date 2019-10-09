// A wrapper list class that has event capabilities without the bloat that comes along with a BindingList.
// It also contains a deep copy method.
using System.Collections;
using System.Collections.Generic;
namespace debugger.Util
{
    public class ListeningList<T> : IList<T>
    {
        private readonly List<T> InternalList;
        public delegate void ListEvent();
        public delegate void OnItemAction(T item, int index);
        public event OnItemAction OnGet = (item, index) => { };        
        public event OnItemAction OnSet = (item, index) => { };
        public event OnItemAction OnAdd = (item, index) => { };
        public event OnItemAction OnRemove = (item, index) => { };
        public event ListEvent OnClear = () => { };

        public T this[int index]
        {
            get { OnGet.Invoke(InternalList[index], index); return InternalList[index]; }
            set { OnSet.Invoke(value, index);  InternalList[index] = value; } }

        public int Count { get => InternalList.Count; }
        public bool IsReadOnly { get => false; }
        public ListeningList()
        {
            InternalList = new List<T>();
        }
        public ListeningList(List<T> toWrap)
        {
            InternalList = toWrap;
        }
        public ListeningList<T> DeepCopy() => new ListeningList<T>(this);
        private ListeningList(ListeningList<T> toCopy)
        {
            InternalList = toCopy.InternalList.DeepCopy();
        }
        public void Add(T item)
        {
            InternalList.Add(item);
            OnAdd.Invoke(item, InternalList.Count-1);
        }
        public void AddRange(IList<T> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                InternalList.Add(items[i]);
                OnAdd.Invoke(items[i], InternalList.Count - 1);
            }
        }
        public void Clear()
        {
            InternalList.Clear();
            OnClear.Invoke();
        }

        public bool Contains(T item) => InternalList.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => InternalList.CopyTo(array, arrayIndex);

        public IEnumerator<T> GetEnumerator() => InternalList.GetEnumerator();

        public int IndexOf(T item) => InternalList.IndexOf(item);

        public void Insert(int index, T item)
        {
            InternalList.Insert(index, item);
            OnAdd.Invoke(item, index);
        }

        public bool Remove(T item)
        {
            int index = InternalList.IndexOf(item);
            if(index > -1)
            {
                InternalList.RemoveAt(index);
                return true;
            }
            return false;
        }

        public void RemoveAt(int index)
        {
            T item = InternalList[index];
            InternalList.RemoveAt(index);
            OnRemove.Invoke(item, index);
        }

        IEnumerator IEnumerable.GetEnumerator() => InternalList.GetEnumerator();
    }
}
