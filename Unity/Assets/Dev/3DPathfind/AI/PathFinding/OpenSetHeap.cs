using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenSetHeap
{
    private PathGraphNode[] _elements;
    private HashSet<PathGraphNode> dic;
    private int _size;
    private int id;

    public OpenSetHeap(int id, int size)
    {
        _elements = new PathGraphNode[size];
        dic = new HashSet<PathGraphNode>();
        this.id = id;
    }

    private int GetLeftChildIndex(int elementIndex) => 2 * elementIndex + 1;
    private int GetRightChildIndex(int elementIndex) => 2 * elementIndex + 2;
    private int GetParentIndex(int elementIndex) => (elementIndex - 1) / 2;

    private bool HasLeftChild(int elementIndex) => GetLeftChildIndex(elementIndex) < _size;
    private bool HasRightChild(int elementIndex) => GetRightChildIndex(elementIndex) < _size;
    private bool IsRoot(int elementIndex) => elementIndex == 0;

    private PathGraphNode GetLeftChild(int elementIndex) => _elements[GetLeftChildIndex(elementIndex)];
    private PathGraphNode GetRightChild(int elementIndex) => _elements[GetRightChildIndex(elementIndex)];
    private PathGraphNode GetParent(int elementIndex) => _elements[GetParentIndex(elementIndex)];

    private void Swap(int firstIndex, int secondIndex)
    {
        var temp = _elements[firstIndex];
        _elements[firstIndex] = _elements[secondIndex];
        _elements[secondIndex] = temp;
    }

    public bool IsEmpty()
    {
        return _size == 0;
    }

    public PathGraphNode Peek()
    {
        if (_size == 0)
            throw new System.IndexOutOfRangeException();

        return _elements[0];
    }

    public PathGraphNode Pop()
    {
        if (_size == 0)
            throw new System.IndexOutOfRangeException();

        var result = _elements[0];
        _elements[0] = _elements[_size - 1];
        _size--;

        ReCalculateDown();

        dic.Remove(result);

        return result;
    }

    public void Add(PathGraphNode element)
    {
        if (_size == _elements.Length)
            throw new System.IndexOutOfRangeException();

        _elements[_size] = element;
        dic.Add(element);
        _size++;

        ReCalculateUp();
    }

    public bool HasKey(PathGraphNode g)
    {
        return dic.Contains(g);
    }

    public void Clear(bool realClear = false)
    {
        if (realClear)
        {
            int s = _elements.Length;

            _elements = new PathGraphNode[s];
            dic = new HashSet<PathGraphNode>();
        }
        else
        {
            dic.Clear();
        }
        _size = 0;
    }

    private void ReCalculateDown()
    {
        int index = 0;
        while (HasLeftChild(index))
        {
            var smallerIndex = GetLeftChildIndex(index);
            if (HasRightChild(index) && GetRightChild(index).CompareMin(GetLeftChild(index), id))
            {
                smallerIndex = GetRightChildIndex(index);
            }

            if (!(_elements[smallerIndex].CompareMin(_elements[index], id)))
            {
                break;
            }

            Swap(smallerIndex, index);
            index = smallerIndex;
        }
    }

    private void ReCalculateUp()
    {
        var index = _size - 1;
        while (!IsRoot(index) && _elements[index].CompareMin(GetParent(index), id))
        {
            var parentIndex = GetParentIndex(index);
            Swap(parentIndex, index);
            index = parentIndex;
        }
    }
}