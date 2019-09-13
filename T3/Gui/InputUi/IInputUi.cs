﻿using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.Operator;
using T3.Gui.Selection;

namespace T3.Gui.InputUi
{
    [Flags]
    public enum InputEditState
    {
        Nothing = 0x0,
        Focused = 0x1,
        Modified = 0x2,
        Finished = 0x4,
        ShowOptions = 0x8,
        ModifiedAndFinished = Modified | Finished
    }

    public enum Relevancy
    {
        Required,
        Relevant,
        Optional
    }

    public interface IInputUi : ISelectable
    {
        Symbol.InputDefinition InputDefinition { get; set; }
        Guid Id { get; }
        Type Type { get; }
        Relevancy Relevancy { get; set; }

        InputEditState DrawInputEdit(IInputSlot input, SymbolUi compositionUi, SymbolChildUi symbolChildUi);

        void DrawParameterEdits();

        void Write(JsonTextWriter writer);
        void Read(JToken inputToken);
    }
}