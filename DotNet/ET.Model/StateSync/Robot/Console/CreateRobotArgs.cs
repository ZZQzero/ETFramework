using CommandLine;

namespace ET
{
    public class CreateRobotArgs: Object
    {
        [Option("Num", Required = false, Default = 1)]
        public int Num { get; set; }
    }
}