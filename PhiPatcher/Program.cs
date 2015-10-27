using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.IO;

namespace PhiPatcher
{
    class Program
    {
        private static string AssemblyPath = "Assembly-CSharp.dll";
        private static string AssemblyOutPath = "Assembly-CSharp.patched.dll";

        static void Main(string[] args)
        {
            AssemblyDefinition assembly = null;

            try
            {
                assembly = AssemblyDefinition.ReadAssembly(Program.AssemblyPath);
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine("Can't find file " + Program.AssemblyPath);
                Console.Read();
                return;
            }
            catch
            {
                Console.WriteLine("Couldn't load " + Program.AssemblyPath);
                Console.Read();
                return;
            }


            ModuleDefinition module = assembly.MainModule;

            TypeDefinition type = assembly.MainModule.Types.FirstOrDefault(t => t.Name == "GameManager" );

            if (type == null)
            {
                Console.WriteLine("Couldn't find class " + "GameManager");
                Console.Read();
                return;
            }

            MethodDefinition method = type.Methods.FirstOrDefault(m => m.Name == ".ctor");

            if (method == null)
            {
                Console.WriteLine("Couldn't find method " + type.Name + "." + ".ctor");
                Console.Read();
                return;
            }

            MethodBody body = method.Body;
            ILProcessor processor = method.Body.GetILProcessor();

            Instruction instruction = processor.Create(OpCodes.Nop);
            processor.InsertAfter(
                body.Instructions.First(),
                instruction
            );

            assembly.Write(Program.AssemblyOutPath);
        }
    }
}
