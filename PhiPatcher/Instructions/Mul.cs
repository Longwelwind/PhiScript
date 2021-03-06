﻿using System.Xml.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace PhiPatcher.Instructions
{
    public class Mul
    {
        public static Instruction ParseInstruction(ILProcessor processor, TypeDefinition type, XElement instrXML)
        {
            return processor.Create(OpCodes.Mul);
        }
    }
}