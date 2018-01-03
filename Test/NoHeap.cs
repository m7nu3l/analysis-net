using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class NoHeap
    {
        public int add(int x, int y)
        {
            return x + y;
        }

        public int subtract(int x, int y)
        {
            return x - y;
        }

        public int multiplyBy2Adding(int x)
        {
            int i = add(x, x);
            return i;
        }
    }
}
