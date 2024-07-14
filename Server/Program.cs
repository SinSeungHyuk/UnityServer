using System;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    class Lock
    {
        // 한 스레드가 재귀적으로 호출되지 않는다는 가정
        // int : unused (1) + writeId(15) + readCount(16) = 32bit
        const int EMPTY_FLAG = 0x00000000;
        const int WRITE_MASK = 0x7FFF0000; // 맨앞 안쓰는 비트 제외
        const int READ_MASK  = 0x0000FFFF;
        const int MAX_SPIN_COUNT = 5000;

        int flag = EMPTY_FLAG;

        public void WriteLock()
        {
            // 현재 사용중인 스레드의 id (write할 내용)
            int desired = (Thread.CurrentThread.ManagedThreadId << 16)
                & WRITE_MASK;

            while (true) // 스핀락을 위한 while문
            {
                // 최대스핀횟수만큼 자원요청
                for (int i =0;i < MAX_SPIN_COUNT; i++)
                {
                    if (Interlocked.CompareExchange(ref flag, desired, EMPTY_FLAG) == EMPTY_FLAG)
                        return;
                }

                Thread.Yield(); // 최대스핀횟수 초과하면 양보
            }
        }

        public void WriteUnlock()
        {
            // 이미 write한 스레드를 다시 empty로 언락
            Interlocked.Exchange(ref flag, EMPTY_FLAG);
        }

        public void ReadLock()
        {
            while (true) // 스핀락을 위한 while문
            {
                // 최대스핀횟수만큼 자원요청
                for (int i = 0; i < MAX_SPIN_COUNT; i++)
                {
                    int expected = (flag & READ_MASK);
                    if (Interlocked.CompareExchange(ref flag, expected + 1, expected) == expected)
                        return;
                }

                Thread.Yield(); // 최대스핀횟수 초과하면 양보
            }
        }

        public void ReadUnlock()
        {
            Interlocked.Decrement(ref flag);
        }
    }

    class Program
    {
        static int num = 0;
        static ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        static void Thread_1()
        {
            for (int i = 0; i < 100000; i++)
            {
                _lock.EnterWriteLock();
                num++;
                _lock.ExitWriteLock();
            }
        }

        static void Thread_2()
        {
            for (int i = 0; i < 100000; i++)
            {
                _lock.EnterWriteLock();
                num--;
                _lock.ExitWriteLock();
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
