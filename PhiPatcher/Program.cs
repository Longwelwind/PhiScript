using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.IO;
using System.Xml;
using System.Reflection;

namespace PhiPatcher
{
    class Program
    {
        private string ModificationsXmlPath = "PhiPatcher.Modifications.xml";

        private string AssemblyPath = "Assembly-CSharp.dll";
        private string MovedAssemblyPath = "Assembly-CSharp.original.dll";
        private string PhiAssemblyPath = "PhiScript.dll";

        private Boolean AlreadyPatched = false;

        private XmlDocument ModificationsXml;

        private AssemblyDefinition CSharpAssembly;
        private ModuleDefinition CSharpModule;

        private AssemblyDefinition PhiAssembly;
        private TypeDefinition PhiType;

        public Program()
        {

        }

        public void Run()
        {
            /**
             * We check if the assembly has already been patched
             */
            this.AlreadyPatched = File.Exists(this.MovedAssemblyPath);

            if (this.AlreadyPatched)
            {
                Console.WriteLine(this.MovedAssemblyPath + " already present");
                Console.WriteLine("Using the backed-up " + this.MovedAssemblyPath);
            }

            /**
             * We launch the patching
             */
            this.ModificationsXml = this.LoadModifications(this.ModificationsXmlPath);

            this.LoadAssemblies();
            this.PatchModifications();

            /**
             * We save or rename everything
             */
            // We rename the original dll
            if (!this.AlreadyPatched)
            {
                System.IO.File.Move(this.AssemblyPath, this.MovedAssemblyPath);
            }

            Console.WriteLine("Writing the new Assembly in " + this.AssemblyPath);

            this.CSharpAssembly.Write(this.AssemblyPath);

            Console.WriteLine("Finished Writing");

            Console.WriteLine("Enter to continue");
            Console.Read();
        }

        public void LoadAssemblies()
        {
            /**
             * We first load the PhiScript assembly containing the static
             * methods that must be called
             */
            try
            {
                this.PhiAssembly = AssemblyDefinition.ReadAssembly(this.PhiAssemblyPath);
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine("Can't find file " + this.PhiAssemblyPath);
                Console.Read();
                return;
            }
            catch
            {
                Console.WriteLine("Couldn't load " + this.PhiAssemblyPath);
                Console.Read();
                return;
            }

            ModuleDefinition phiModule = this.PhiAssembly.MainModule;
            this.PhiType = phiModule.GetType("PhiScript.Phi");

            /**
             * We then load the Assembly-CSharp assembly that contains the
             * vanilla code
             * If it is the first use, we read Assembly-CSharp.dll, but if it is
             * not we read Assembly-CSharp.original.dll
             */
            try
            {
                if (this.AlreadyPatched)
                {
                    this.CSharpAssembly = AssemblyDefinition.ReadAssembly(this.MovedAssemblyPath);
                }
                else
                {
                    this.CSharpAssembly = AssemblyDefinition.ReadAssembly(this.AssemblyPath);
                }
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine("Can't find file " + this.AssemblyPath);
                Console.Read();
                return;
            }
            catch
            {
                Console.WriteLine("Couldn't load " + this.AssemblyPath);
                Console.Read();
                return;
            }

            this.CSharpModule = this.CSharpAssembly.MainModule;
        }

        public void PatchModifications()
        {
            XmlNodeList modificationsList = this.ModificationsXml.ChildNodes;
            /**
             * We now inject the calls to the static methods
             */
            TypeDefinition type = this.CSharpModule.Types.FirstOrDefault(t => t.Name == "GameManager");

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

            Mono.Cecil.Cil.MethodBody body = method.Body;
            ILProcessor processor = method.Body.GetILProcessor();

            InstructionEntry[] instructions = {
               new InstructionEntry(OpCodes.Call, "StaticLaunch")
            };

            // We begin right before the "ret"
            Instruction previousInstruction = body.Instructions.Last().Previous;
            foreach (InstructionEntry instrTuple in instructions)
            {
                Instruction instruction = null;

                if (instrTuple.OpCode == OpCodes.Call)
                {
                    MethodDefinition methodToAdd = this.PhiType.Methods.FirstOrDefault(m => m.Name == instrTuple.Arg);

                    MethodReference methodToAddImported = this.PhiAssembly.MainModule.Import(methodToAdd);

                    instruction = processor.Create(instrTuple.OpCode, methodToAddImported);
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
        }

        public XmlDocument LoadModifications(string path)
        {
            /**
             * We load the file containing the modifications to patch
             * in the assembly
             */
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
            StreamReader reader = new StreamReader(stream);

            XmlDocument modif = new XmlDocument();
            modif.LoadXml(reader.ReadToEnd());

            return modif;
        }

        static void Main(string[] args)
        {
            Program program = new Program();

            program.Run();
        }
    }

    public class InstructionEntry
    {
        public OpCode OpCode
        {
            get;
        }

        public string Arg
        {
            get;
        }

        public InstructionEntry(OpCode opCode, string arg)
        {
            this.OpCode = opCode;
            this.Arg = arg;
        }
    }
}
