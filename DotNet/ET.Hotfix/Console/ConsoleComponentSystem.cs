using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ET
{
    [EntitySystemOf(typeof(ConsoleComponent))]
    [FriendOf(typeof(ModeContext))]
    public static partial class ConsoleComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ConsoleComponent self)
        {
            self.Start().NoContext();
        }

        
        private static async ETTask Start(this ConsoleComponent self)
        {
            self.CancellationTokenSource = new CancellationTokenSource();

            while (true)
            {
                try
                {
                    ModeContext modeContext = self.GetComponent<ModeContext>();
                    string line = await Task.Factory.StartNew(() =>
                    {
                        Console.Write($"{modeContext?.Mode ?? ""}> ");
                        return Console.In.ReadLine();
                    }, self.CancellationTokenSource.Token);

                    if (line == null)
                    {
                        continue;
                    }
                    line = line.Trim();

                    switch (line)
                    {
                        case "":
                            break;
                        case "exit":
                            self.RemoveComponent<ModeContext>();
                            break;
                        default:
                        {
                            string[] lines = line.Split(" ");
                            string mode = modeContext == null? lines[0] : modeContext.Mode;

                            IConsoleHandler iConsoleHandler = ConsoleDispatcher.Instance.Get(mode);
                            if (modeContext == null)
                            {
                                modeContext = self.AddComponent<ModeContext>();
                                modeContext.Mode = mode;
                            }
                            await iConsoleHandler.Run(self.Fiber(), modeContext, line);
                            break;
                        }
                    }


                }
                catch (Exception e)
                {
                    Log.Console(e.ToString());
                }
            }
        }
    }
}