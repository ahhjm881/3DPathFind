using System;

namespace Candy.Pathfind3D
{
    using UnityEngine;
    using NUnit.Framework;
    using Unity.Collections;

    public class MinHeapTests
    {
        private MinHeap<int> _heap;

        [SetUp]
        public void SetUp()
        {
            _heap = new MinHeap<int>(10, Allocator.Temp);
        }

        [TearDown]
        public void TearDown()
        {
            _heap.Dispose();
        }

        [Test]
        public void Insert_SingleElement_PeekReturnsElement()
        {
            _heap.Insert(42);
            Assert.AreEqual(42, _heap.Peek());
        }

        [Test]
        public void Insert_MultipleElements_PeekReturnsMinimum()
        {
            _heap.Insert(10);
            _heap.Insert(5);
            _heap.Insert(20);
            _heap.Insert(1);

            Assert.AreEqual(1, _heap.Peek());
        }

        [Test]
        public void Pop_RemovesAndReturnsMinimum()
        {
            _heap.Insert(10);
            _heap.Insert(5);
            _heap.Insert(1);

            int min = _heap.Pop();

            Assert.AreEqual(1, min);
            Assert.AreEqual(2, _heap.Count);
            Assert.AreEqual(5, _heap.Peek());
        }

        [Test]
        public void Count_ReflectsNumberOfElements()
        {
            Assert.AreEqual(0, _heap.Count);
            _heap.Insert(3);
            _heap.Insert(7);
            Assert.AreEqual(2, _heap.Count);
        }

        [Test]
        public void Pop_AllElementsInSortedOrder()
        {
            int[] values = { 10, 3, 5, 1, 9 };
            foreach (var val in values)
                _heap.Insert(val);

            Array.Sort(values);

            foreach (var expected in values)
            {
                int actual = _heap.Pop();
                Assert.AreEqual(expected, actual);
            }
        }
    }
}