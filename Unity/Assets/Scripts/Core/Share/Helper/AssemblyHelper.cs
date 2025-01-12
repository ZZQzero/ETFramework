using System;
using System.Collections.Generic;
using System.Reflection;

namespace ET
{
    public static class AssemblyHelper
    {
        [StaticField] public static Dictionary<string, string> AssembleNames = new Dictionary<string, string>
        {
            { "ETClient.Model", "ETClient.Model.dll" },
            { "ETClient.ModelView", "ETClient.ModelView.dll" },
            { "ETClient.Hotfix", "ETClient.Hotfix.dll" },
            { "ETClient.HotfixView", "ETClient.HotfixView.dll" },
            { "ETClient.Loader", "ETClient.Loader.dll" },
            { "ETClient.Core", "ETClient.Core.dll" },
            //服务端
            {"ET.Core","ET.Core.dll"},
            {"ET.Hotfix","ET.Hotfix.dll"},
            {"ET.Loader","ET.Loader.dll"},
            {"ET.Model","ET.Model.dll"},
        };
        public static Dictionary<string, Type> GetAssemblyTypes(params Assembly[] args)
        {
            Dictionary<string, Type> types = new Dictionary<string, Type>();

            foreach (Assembly ass in args)
            {
                var ts = ass.GetTypes();
                foreach (Type type in ts)
                {
                    types[type.FullName] = type;
                }
            }

            return types;
        }
    }
}