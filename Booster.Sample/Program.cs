using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Booster.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            var factoryMethod = DynamicActivator.CreateFactory(typeof(Sample), new object[] { "aaa", 9 });

            object obj = null;
            Do(() =>
            {
                for (int i = 0; i < 100000; i++)
                {
                    obj = factoryMethod(new object[] { "aaa", 9 });
                }
            }, "ILUtil");

            Do(() =>
            {
                for (int i = 0; i < 100000; i++)
                {
                    obj = new Sample("aaa", 9);
                }
            }, "Native");

            Do(() =>
            {
                for (int i = 0; i < 100000; i++)
                {
                    obj = Activator.CreateInstance(typeof(Sample), new object[] { "aaa", 9 });
                }
            }, "Activator");

            Console.ReadKey();
        }


        static void Do(Action action, string traceMessage)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            action();
            sw.Stop();
            Console.WriteLine($"${traceMessage} => {sw.Elapsed}");
        }
    }



    class Sample
    {
        public Sample() { }
        public Sample(Hoge hoge)
        {
            this.MyProperty = hoge;
        }
        public Hoge MyProperty { get; set; }

        public Sample(string s, int i)
        {

        }
    }


    class Hoge
    {
        public int MyProperty { get; set; }
    }
}
