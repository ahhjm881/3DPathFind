using System;
using Unity.Burst;
using Unity.Collections;

namespace Candy.Pathfind3D
{
    [BurstCompile]
    public struct MinHeap<T> : IDisposable where T : unmanaged, IComparable<T>
    {
        private NativeList<T> _elements;
        private Allocator _allocator;

        public MinHeap(int capacity, Allocator allocator)
        {
            _allocator = allocator;
            _elements = new NativeList<T>(capacity, allocator);
        }

        public int Count => _elements.Length;

        public void Insert(T value)
        {
            _elements.Add(value);
            HeapifyUp(_elements.Length - 1);
        }

        public T Peek()
        {
            if (_elements.Length == 0) throw new InvalidOperationException("Heap is empty.");
            return _elements[0];
        }

        public T Pop()
        {
            if (_elements.Length == 0) throw new InvalidOperationException("Heap is empty.");

            T root = _elements[0];
            int lastIndex = _elements.Length - 1;
            _elements[0] = _elements[lastIndex];
            _elements.RemoveAt(lastIndex);
            HeapifyDown(0);
            return root;
        }

        private void HeapifyUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (_elements[index].CompareTo(_elements[parent]) < 0)
                {
                    Swap(index, parent);
                    index = parent;
                }
                else break;
            }
        }

        private void HeapifyDown(int index)
        {
            int lastIndex = _elements.Length - 1;

            while (true)
            {
                int left = index * 2 + 1;
                int right = index * 2 + 2;
                int smallest = index;

                if (left <= lastIndex && _elements[left].CompareTo(_elements[smallest]) < 0)
                    smallest = left;

                if (right <= lastIndex && _elements[right].CompareTo(_elements[smallest]) < 0)
                    smallest = right;

                if (smallest != index)
                {
                    Swap(index, smallest);
                    index = smallest;
                }
                else break;
            }
        }

        private void Swap(int i, int j)
        {
            T temp = _elements[i];
            _elements[i] = _elements[j];
            _elements[j] = temp;
        }

        public void Dispose()
        {
            if (_elements.IsCreated)
                _elements.Dispose();
        }
    }
}