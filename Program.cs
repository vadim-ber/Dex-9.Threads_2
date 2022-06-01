using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Dex_9.Threads_2
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Для завершения создания очереди и начала ее обработки нажмите любую клавишу:");

            Thread[] userThreads = new Thread[3];
            for(int i = 0; i < 3; i++)
            {
                userThreads[i] = new Thread(() => AddCycle(new JobExecutorUser()));
                userThreads[i].Name = "User" + i;
                userThreads[i].Start();
            }

            JobExecutorUser j = new JobExecutorUser();
            j.Stop();

            Console.WriteLine("Введите кол-во необходимых потоков для обработки очереди:");
            try
            {
                int inp = Convert.ToInt32(Console.ReadLine());
                j.Start(inp);
            }
            catch(Exception e)
            {
                Console.WriteLine("Некорректный ввод.");
            }

            for (int k = 0; k < JobExecutorUser.dequeueThreads.Length; k++)
            {
                JobExecutorUser.dequeueThreads[k].Join();
            }

            j.Clear();

        }

        static void DoSomething()
        {
            Console.WriteLine("Задача{0} обработана процессом {1} ", new JobExecutorUser().Amount, Thread.CurrentThread.Name);
        }

        static void AddCycle(JobExecutorUser user)
        {            
            while (JobExecutorUser.threadsActive)
            {                
                user.Add(DoSomething);
                Thread.Sleep(1000);
            }
        }
    }

    public interface IJobExecutor
    {
        /// Кол-во задач в очереди на обработку
        int Amount { get; }
        /// <summary>
        /// Запустить обработку очереди и установить максимальное кол-во  параллельных задач
        /// </summary>
        /// <param name="maxConcurrent">максимальное кол-во одновременно  выполняемых задач</param>
        void Start(int maxConcurrent);
        /// <summary>
        /// Остановить обработку очереди и выполнять задачи
        /// </summary>
        void Stop();
        /// <summary>
        /// Добавить задачу в очередь
        /// </summary>
        /// <param name="action"></param>
        void Add(Action action);
        /// <summary>
        /// Очистить очередь задач
        /// </summary>
        void Clear();
    }

    public class JobExecutorUser : IJobExecutor
    {
        public int Amount => queue.Count;

        private static Queue<Action> queue = new Queue<Action>();

        public static bool threadsActive = true;
        public static Thread[] dequeueThreads;


        public void Add(Action action)
        {
            lock (queue)
            {
                queue.Enqueue(action);
                Console.WriteLine("{0} добавил задачу. В очереди {1} задач(а)", Thread.CurrentThread.Name, Amount);
            }
        }

        public void Clear()
        {
            Thread.Sleep(1000);
            queue.Clear();
            Console.WriteLine("Очередь очищена");
        }

        public void Start(int maxConcurrent)
        {
            dequeueThreads = new Thread[maxConcurrent];

            for(int i =0; i < maxConcurrent; i++)
            {
                dequeueThreads[i] = new Thread(()=>CycleDequeue());
                dequeueThreads[i].Name = "Процесс" + i;
                dequeueThreads[i].Start();               
            }            
        }

        public void Stop()
        {
            Console.ReadKey();
            threadsActive = false;
            Console.WriteLine();
            Console.WriteLine("Заполнение очереди завершено. Общее кол-во задач: {0}", Amount);
        }

        private void CycleDequeue()
        {
            while (Amount > 0)
            {                
                lock (queue)
                {
                    queue.TryDequeue(out Action result);
                    if(result != null)
                    {
                        result.Invoke();
                        Thread.Sleep(1000);
                    }                    
                }                
            }            
        }
    }
}
