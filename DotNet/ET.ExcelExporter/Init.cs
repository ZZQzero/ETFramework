using System;
using System.Diagnostics;
using System.Text;

namespace ET
{
    [EnableClass]
    internal static class Init
    {
        private static int Main(string[] args)
        {
            try
            {
                GenerateEntitySystem.Generate();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            Console.WriteLine("Finish OK!");
            return 1;
        }
    }
}