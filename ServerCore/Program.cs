using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServerCore
{
    class SpinLock
    {
        volatile int locked = 0;

        public void Acquire()
        {
            while (true)
            {
                // CAS (Compare-And-Swap)
                int original = Interlocked.CompareExchange(ref locked, 1, 0);
                if (original == 0) 
                    break;
            }
        }

        public void Release()
        {
            locked = 0;
        }
    }

    class Program
    {
        static int num = 0;
        static SpinLock spin = new SpinLock();
        static object lockObj = new object();

        static void Thread_1()
        {
            for (int i = 0; i < 100000; i++)
            {
                spin.Acquire();
                num++;
                spin.Release();
            }
        }

        static void Thread_2()
        {
            for (int i = 0; i < 100000; i++)
            { 
                spin.Acquire();
                num--;
                spin.Release();
            }
        }

        static void Main(string[] args)
        {
            Task task1 = new Task(Thread_1);
            Task task2 = new Task(Thread_2);
            task1.Start();
            task2.Start();

            Task.WaitAll(task1, task2);

            Console.WriteLine(num);
        }
    }
}
