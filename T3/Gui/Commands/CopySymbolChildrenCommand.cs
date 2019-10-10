﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using T3.Core.Operator;

namespace T3.Gui.Commands
{
    public class CopySymbolChildrenCommand : ICommand
    {
        public string Name => "Copy Symbol Children";

        public bool IsUndoable => true;

        public Dictionary<Guid, Guid> OldToNewIdDict { get; } = new Dictionary<Guid, Guid>();

        public CopySymbolChildrenCommand(SymbolUi sourceCompositionUi, IEnumerable<SymbolChildUi> symbolChildrenToCopy, SymbolUi targetCompositionUi,
                                         Vector2 targetPosition)
        {
            _sourceSymbolId = sourceCompositionUi.Symbol.Id;
            _targetSymbolId = targetCompositionUi.Symbol.Id;
            _targetPosition = targetPosition;

            if (symbolChildrenToCopy == null)
            {
                // if now specific child are selected copy all of the source composition op
                symbolChildrenToCopy = sourceCompositionUi.ChildUis;
            }

            Vector2 upperLeftCorner = new Vector2(Single.MaxValue, Single.MaxValue);
            foreach (var childToCopy in symbolChildrenToCopy)
            {
                upperLeftCorner = Vector2.Min(upperLeftCorner, childToCopy.PosOnCanvas);
            }

            foreach (var childToCopy in symbolChildrenToCopy)
            {
                Entry entry = new Entry(childToCopy.Id, Guid.NewGuid(), childToCopy.PosOnCanvas - upperLeftCorner, childToCopy.Size);
                _childrenToCopy.Add(entry);
                OldToNewIdDict.Add(entry.ChildId, entry.AddedId);
            }

            foreach (var entry in _childrenToCopy)
            {
                _connectionsToCopy.AddRange(from con in sourceCompositionUi.Symbol.Connections
                                            where con.TargetParentOrChildId == entry.ChildId
                                            let newTargetId = OldToNewIdDict[entry.ChildId]
                                            from connectionSource in symbolChildrenToCopy
                                            where con.SourceParentOrChildId == connectionSource.Id
                                            let newSourceId = OldToNewIdDict[connectionSource.Id]
                                            select new Symbol.Connection(newSourceId, con.SourceSlotId, newTargetId, con.TargetSlotId));
            }

            _connectionsToCopy.Reverse(); // to keep multi input order
        }

        public void Undo()
        {
            var parentSymbolUi = SymbolUiRegistry.Entries[_targetSymbolId];
            foreach (var child in _childrenToCopy)
            {
                parentSymbolUi.RemoveChild(child.AddedId);
            }
        }

        public void Do()
        {
            var targetCompositionSymbolUi = SymbolUiRegistry.Entries[_targetSymbolId];
            var sourceCompositionSymbolUi = SymbolUiRegistry.Entries[_sourceSymbolId];
            foreach (var childToCopy in _childrenToCopy)
            {
                SymbolChild symbolChildToCopy = sourceCompositionSymbolUi.Symbol.Children.Find(child => child.Id == childToCopy.ChildId);
                var symbolToAdd = SymbolRegistry.Entries[symbolChildToCopy.Symbol.Id];
                targetCompositionSymbolUi.AddChild(symbolToAdd, childToCopy.AddedId, _targetPosition + childToCopy.RelativePosition, childToCopy.Size);
                var targetSymbol = targetCompositionSymbolUi.Symbol;
                SymbolChild newSymbolChild = targetSymbol.Children.Find(child => child.Id == childToCopy.AddedId);
                var newSymbolInputs = newSymbolChild.InputValues;
                foreach (var input in symbolChildToCopy.InputValues)
                {
                    var newInput = newSymbolInputs[input.Key];
                    newInput.Value.Assign(input.Value.Value.Clone());
                    newInput.IsDefault = input.Value.IsDefault;
                }
            }

            // add connections between copied children
            foreach (var connection in _connectionsToCopy)
            {
                targetCompositionSymbolUi.Symbol.AddConnection(connection);
            }
        }

        struct Entry
        {
            public Entry(Guid childId, Guid addedId, Vector2 relativePosition, Vector2 size)
            {
                ChildId = childId;
                AddedId = addedId;
                RelativePosition = relativePosition;
                Size = size;
            }

            public readonly Guid ChildId;
            public readonly Guid AddedId;
            public readonly Vector2 RelativePosition;
            public readonly Vector2 Size;
        }

        private readonly Vector2 _targetPosition;
        private readonly Guid _sourceSymbolId;
        private readonly Guid _targetSymbolId;
        private readonly List<Entry> _childrenToCopy = new List<Entry>();
        private readonly List<Symbol.Connection> _connectionsToCopy = new List<Symbol.Connection>();
    }
}