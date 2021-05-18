using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImageQuantization
{
    class pqueue
    {
        class Node
        {
            public double Priority;
            public ColorNode Object { get; set; }
        }
        List<Node> queue = new List<Node>();

        int heapSize = -1;

        public int Count { get { return queue.Count; } }

        private void Swap(int i, int j)
        {
            var temp = queue[i];
            queue[i] = queue[j];
            queue[j] = temp;
        }
        private int ChildL(int i)
        {
            return i * 2 + 1;
        }
        private int ChildR(int i)
        {
            return i * 2 + 2;
        }

        private void MinHeapify(int i)
        {
            int left = ChildL(i);
            int right = ChildR(i);

            int lowest = i;

            if (left <= heapSize && queue[lowest].Priority > queue[left].Priority)
                lowest = left;
            if (right <= heapSize && queue[lowest].Priority > queue[right].Priority)
                lowest = right;

            if (lowest != i)
            {
                Swap(lowest, i);
                MinHeapify(lowest);
            }
        }

        private void BuildHeapMin(int i)
        {
            while (i >= 0 && queue[(i - 1) / 2].Priority > queue[i].Priority)
            {
                Swap(i, (i - 1) / 2);
                i = (i - 1) / 2;
            }
        }

        public void Enqueue(double priority, ColorNode obj)
        {
            Node node = new Node() { Priority = priority, Object = obj };
            queue.Add(node);
            heapSize++;
            BuildHeapMin(heapSize);

        }

        public ColorNode Dequeue()
        {
            if (heapSize > -1)
            {
                var returnVal = queue[0].Object;
                queue[0] = queue[heapSize];
                queue.RemoveAt(heapSize);
                heapSize--;
                MinHeapify(0);

                return returnVal;
            }
            else
                throw new Exception("Queue is empty");
        }

        public bool IsInQueue(ColorNode obj)
        {
            foreach (Node node in queue)
                if (object.ReferenceEquals(node.Object, obj))
                    return true;
            return false;
        }

        public void UpdatePriority(ColorNode obj, double priority)
        {
            int i = queue.FindIndex(x => x.Object.index.Equals(obj.index));
            if (i < 0)
                return;
            queue[i].Object = obj;
            queue[i].Priority = priority;
            BuildHeapMin(i);
            MinHeapify(i);

        }
    }

}