using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace PhiPatcher
{
    public class InstructionBuilder
    {
        public static Instruction Build(ILProcessor processor, TypeDefinition type, XElement instrXML)
        {
            var opCode = instrXML.Attribute("OpCode").Value;

            switch (opCode)
            {
                case "Call":
                    return Instructions.Call.ParseInstruction(processor, type, instrXML);
                default:
                    return null;
            }
        }
    }
}
