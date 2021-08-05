using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace protobuf2lua
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("参数错误");
                return;
            }

            var ret = Protobuf2LuaGenerator.GenerateLuaFiles(args[0], args[1]);
            if (ret != 0)
            {
                Console.WriteLine("生成失败, 错误码: " + ret);
            }
            else {
                Console.WriteLine("生成lua注解文件成功");
            }
        }
    }
}
