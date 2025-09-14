using System;
using System.IO;

namespace ET
{
    [Invoke]
    public class RecastFileReader: AInvokeHandler<NavmeshComponent.RecastFileLoader, byte[]>
    {
        public override byte[] Handle(NavmeshComponent.RecastFileLoader args)
        {
            string path = @"Unity\Assets\Config\Recast\" + args.Name;
            return File.ReadAllBytes(path);
        }
    }
}