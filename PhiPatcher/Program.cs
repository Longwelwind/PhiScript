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
        private static string MovedAssemblyPath = "Assembly-CSharp.original.dll";
        private static string PhiAssemblyPath = "PhiScript.dll";

        static void Main(string[] args)
        {
            AssemblyDefinition assembly = null;
            AssemblyDefinition phiAssembly = null;
            Boolean alreadyUsed = File.Exists(Program.MovedAssemblyPath);

            if (alreadyUsed)
            {
                Console.WriteLine(Program.MovedAssemblyPath + " already present");
                Console.WriteLine("Using the backed-up " + Program.MovedAssemblyPath);
            }

            /**
             * We first load the PhiScript assembly containing the static
             * methods that must be called
             */
            try
            {
                phiAssembly = AssemblyDefinition.ReadAssembly(Program.PhiAssemblyPath);
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine("Can't find file " + Program.PhiAssemblyPath);
                Console.Read();
                return;
            }
            catch
            {
                Console.WriteLine("Couldn't load " + Program.PhiAssemblyPath);
                Console.Read();
                return;
            }

            ModuleDefinition phiModule = phiAssembly.MainModule;
            TypeDefinition phiType = phiModule.GetType("PhiScript.Phi");

            /**
             * We then load the Assembly-CSharp assembly that contains the
             * vanilla code
             * If it is the first use, we read Assembly-CSharp.dll, but if it is
             * the second we read MovedAssemblyPath
             */
            try
            {
                if (alreadyUsed)
                {
                    assembly = AssemblyDefinition.ReadAssembly(Program.MovedAssemblyPath);
                }
                else
                {
                    assembly = AssemblyDefinition.ReadAssembly(Program.AssemblyPath);
                }
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

            /**
             * We now inject the calls to the static methods
             */
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

            Tuple<OpCode, string>[] instructions = {
               new Tuple<OpCode, string>(OpCodes.Call, "StaticLaunch")
            };

            // We begin right before the "ret"
            Instruction previousInstruction = body.Instructions.Last().Previous;
            foreach (Tuple<OpCode, string> instrTuple in instructions)
            {
                Instruction instruction = null;

                if (instrTuple.Item1 == OpCodes.Call)
                {
                    MethodDefinition methodToAdd = phiType.Methods.FirstOrDefault(m => m.Name == instrTuple.Item2);
                    
                    MethodReference methodToAddImported = module.Import(methodToAdd);

                    instruction = processor.Create(instrTuple.Item1, methodToAddImported);

                }
                else
                {
                    
                }

                processor.InsertAfter(
                    previousInstruction,
                    instruction
                );

                previousInstruction = instruction;
            }

            /**
             * We save or rename everything
             */
            // We rename the original dll
            if (!alreadyUsed)
            {
                System.IO.File.Move(Program.AssemblyPath, Program.MovedAssemblyPath);
            }

            Console.WriteLine("Writing the new Assembly in " + Program.AssemblyPath);

            assembly.Write(Program.AssemblyPath);

            Console.WriteLine("Finished Writing");

            Console.WriteLine("Enter to continue");
            Console.Read();
        }
    }
}
