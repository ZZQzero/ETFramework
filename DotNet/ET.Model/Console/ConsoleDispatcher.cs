using System;
using System.Collections.Generic;

namespace ET
{
    public class ConsoleDispatcher: Singleton<ConsoleDispatcher>, ISingletonAwake
    {
        private readonly Dictionary<string, IConsoleHandler> handlers = new();
        
        public void Awake()
        {
            /*HashSet<Type> types = CodeTypes.Instance.GetTypes(typeof (ConsoleHandlerAttribute));

            foreach (Type type in types)
            {
                object[] attrs = type.GetCustomAttributes(typeof(ConsoleHandlerAttribute), false);
                if (attrs.Length == 0)
                {
                    continue;
                }

                ConsoleHandlerAttribute consoleHandlerAttribute = (ConsoleHandlerAttribute)attrs[0];

                object obj = Activator.CreateInstance(type);

                IConsoleHandler iConsoleHandler = obj as IConsoleHandler;
                if (iConsoleHandler == null)
                {
                    throw new Exception($"ConsoleHandler handler not inherit IConsoleHandler class: {obj.GetType().FullName}");
                }
                this.handlers.Add(consoleHandlerAttribute.Mode, iConsoleHandler);
            }*/
        }

        public void RegisterConsole<T>(string model) where T : IConsoleHandler,new()
        {
            IConsoleHandler handler = new T();
            handlers.Add(model, handler);
        }
        
        public IConsoleHandler Get(string key)
        {
            return this.handlers[key];
        }
    }
}