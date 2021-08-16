using System.IO;
using System.Text;
using Google.Protobuf;
using Google.Protobuf.Reflection;

public class Protobuf2LuaGenerator
{
    
    /// <summary>
    /// 生成 lua 注解文件
    /// </summary>
    /// <param name="input">protoc 生成的 descriptor 二进制文件</param>
    /// <param name="output">生成脚本放的位置</param>
    /// <returns>生成成功返回 0, 否则返回非 0 数字</returns>
    public static int GenerateLuaFiles(string input, string output)
    {
        if (!File.Exists(input) || !Directory.Exists(output))
        {
            return 1;           // 输入文件或输出文件夹不存在
        }

        using (FileStream fs = File.Open(input, FileMode.Open, FileAccess.Read))
        {
            if (fs.Length == 0) return 2;           // 输入文件内容为空

            // 读取输入文件内容
            byte[] bytes = new byte[fs.Length];
            fs.Read(bytes, 0, bytes.Length);

            var stream = new CodedInputStream(bytes, 0, bytes.Length);
            FileDescriptorSet descriptorSet = FileDescriptorSet.Parser.ParseFrom(stream);
            foreach (var file in descriptorSet.File)
            {
                // 定义所有类型的名称, 方便 lua encode 和 decode
                string defineName = "Proto" + file.Package + "Define";
                // 定义名称容器
                StringBuilder defineBuilder = new StringBuilder();
                defineBuilder.AppendLine(defineName + "= {}");
                
                // 内容(可以理解为命名空间)
                string containerName = "Proto" + file.Package;
                // 内容容器
                StringBuilder contentBuilder = new StringBuilder();
                contentBuilder.AppendLine(containerName + " = {}");
                // 枚举, 只加到内容容器中
                foreach (var enu in file.EnumType)
                {
                    // 类型注解
                    contentBuilder.AppendLine("---@class " + containerName + "." + enu.Name);
                    contentBuilder.AppendLine(containerName + "." + enu.Name + " = {");
                    foreach (var item in enu.Value)
                    {
                        contentBuilder.AppendLine("    " + item.Name + " = " + item.Number + ",");
                    }

                    contentBuilder.AppendLine("}");
                    contentBuilder.AppendLine("");
                }
                // 消息(类)
                foreach (var msg in file.MessageType)
                {
                    // 在名称容器中加入类名
                    defineBuilder.AppendLine(defineName + "." + msg.Name + " = \"" + file.Package + "." + msg.Name + "\"");
                    foreach (var nested in msg.NestedType)
                    {
                        // 在名称容器中加入类名
                        //defineBuilder.AppendLine(defineName + "." + msg.Name + "." + nested.Name + "= \"" + file.Package + "." + msg.Name + "." + nested.Name + "\"");
                        // 在内容容器中加入注解
                        contentBuilder.AppendLine("---@class " + containerName + "." + msg.Name + "." + nested.Name);
                        foreach (var nf in nested.Field)
                        {
                            contentBuilder.AppendLine("---@field " + nf.Name + " " + GetLuaFieldType(nf));
                        }

                        contentBuilder.AppendLine("");
                    }
                    
                    // 在内容容器中加入注解
                    contentBuilder.AppendLine("---@class " + containerName + "." + msg.Name);
                    foreach (var field in msg.Field)
                    {
                        contentBuilder.AppendLine("---@field " + field.Name + " " + GetLuaFieldType(field));
                    }
                    contentBuilder.AppendLine("");
                }

                defineBuilder.AppendLine("");
                
                // 一个 proto 文件对应生成一个 .lua 文件
                var outFileName = "Proto" + file.Package + ".lua";
                var outFilePath = output + "/" + outFileName;
                if (File.Exists(outFilePath))
                {
                    File.Delete(outFilePath);
                }

                using (FileStream outStream = File.Open(outFilePath, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    StreamWriter writer = new StreamWriter(outStream);
                    writer.Write(defineBuilder);
                    writer.Write(contentBuilder);
                    writer.Flush();
                    writer.Close();
                }
            }
        }

        return 0;
    }

    private static string GetLuaFieldType(FieldDescriptorProto field)
    {
        var type = field.Type;
        if (type == FieldDescriptorProto.Types.Type.Double || type == FieldDescriptorProto.Types.Type.Float ||
            type == FieldDescriptorProto.Types.Type.Fixed32 || type == FieldDescriptorProto.Types.Type.Fixed64 ||
            type == FieldDescriptorProto.Types.Type.Int32 || type == FieldDescriptorProto.Types.Type.Int64 ||
            type == FieldDescriptorProto.Types.Type.Sfixed32 || type == FieldDescriptorProto.Types.Type.Sfixed64 ||
            type == FieldDescriptorProto.Types.Type.Sint32 || type == FieldDescriptorProto.Types.Type.Sint64 ||
            type == FieldDescriptorProto.Types.Type.Uint32 || type == FieldDescriptorProto.Types.Type.Uint64)
        {
            if (field.HasLabel && field.Label == FieldDescriptorProto.Types.Label.Repeated)
            {
                return "number[]";
            }
            else
            {
                return "number";
            }
        }

        if (type == FieldDescriptorProto.Types.Type.String)
        {
            if (field.HasLabel && field.Label == FieldDescriptorProto.Types.Label.Repeated)
            {
                return "string[]";
            }
            else
            {
                return "string";
            }
        }

        if (type == FieldDescriptorProto.Types.Type.Bool)
        {
            if (field.HasLabel && field.Label == FieldDescriptorProto.Types.Label.Repeated)
            {
                return "boolean[]";
            }
            else
            {
                return "boolean";
            }
        }

        if (type == FieldDescriptorProto.Types.Type.Enum || type == FieldDescriptorProto.Types.Type.Message)
        {
            // 默认的长相是这样 ".packageName.EnumName", 前面有个点, 我们把点去了.
            var desp = field.TypeName.Substring(1);
            var typeName = "Proto" + desp;
            if (field.Label == FieldDescriptorProto.Types.Label.Repeated)
            {
                return typeName + "[]";
            }
            else
            {
                return typeName;
            }
        }

        // todo: 剩下 Bytes 和 Group, lua 没有对应类型, 暂时不管了
        return "unknown";
    }
}
