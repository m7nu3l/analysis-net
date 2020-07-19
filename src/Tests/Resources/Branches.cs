using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        public static int Test0()
        {
            return Resize(true, 0, 4);
        }

        public static int Resize(bool increase, int minCapacity, int Capacity)
        {
            if (minCapacity == 0)// auto increase/decrease
            {
                if (increase)
                {
                    if (Capacity == 0)
                    {
                        Capacity = 1;
                    }
                    else
                    {
                        Capacity = Capacity << 1;
                    }
                }
                else Capacity = Capacity >> 1;
            }
            else// if minCapacity is set
            {
                if (increase)
                {
                    while (Capacity < minCapacity)
                    {
                        if (Capacity == 0) Capacity = 1;
                        else Capacity = Capacity << 1;
                    }
                }
                else
                {
                    while (minCapacity < Capacity) Capacity = Capacity >> 1;
                    Capacity = Capacity << 1;
                }
            }

            return Capacity;
        }
    }
}
