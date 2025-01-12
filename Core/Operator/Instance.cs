﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using T3.Core.Logging;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Core.Operator
{
    public abstract class Instance : IDisposable
    {
        public abstract Type Type { get; }
        public Guid SymbolChildId { get; set; }
        public Instance Parent { get; internal set; }
        public Symbol Symbol { get; internal set; }

        public List<ISlot> Outputs { get; set; } = new List<ISlot>();
        public List<Instance> Children { get; set; } = new List<Instance>();
        public List<IInputSlot> Inputs { get; set; } = new List<IInputSlot>();

        /// <summary>
        /// get input without GC allocations 
        /// </summary>
        public IInputSlot GetInput(Guid guid)
        {
            //return Inputs.SingleOrDefault(input => input.Id == guid);
            foreach (var i in Inputs)
            {
                if (i.Id == guid)
                    return i;
            }

            return null;
        }
        
        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
        }

        protected void SetupInputAndOutputsFromType()
        {
            // input identified by base interface
            var inputInfos = from field in Type.GetFields()
                             where typeof(IInputSlot).IsAssignableFrom(field.FieldType)
                             select field;
            foreach (var inputInfo in inputInfos)
            {
                var customAttributes = inputInfo.GetCustomAttributes(typeof(InputAttribute), false);
                Debug.Assert(customAttributes.Length == 1);
                var attribute = (InputAttribute)customAttributes[0];
                var inputSlot = (IInputSlot)inputInfo.GetValue(this);
                inputSlot.Parent = this;
                inputSlot.Id = attribute.Id;
                inputSlot.MappedType = attribute.MappedType;
                Inputs.Add(inputSlot);
            }

            // outputs identified by attribute
            var outputs = (from field in Type.GetFields()
                           let attributes = field.GetCustomAttributes(typeof(OutputAttribute), false)
                           from attr in attributes
                           select (field, (OutputAttribute)attributes[0])).ToArray();
            foreach (var (output, outputAttribute) in outputs)
            {
                var slot = (ISlot)output.GetValue(this);
                slot.Parent = this;
                slot.Id = outputAttribute.Id;
                Outputs.Add(slot);
            }
        }

        public bool AddConnection(Symbol.Connection connection, int multiInputIndex)
        {
            var (_, sourceSlot, _, targetSlot) = GetInstancesForConnection(connection);
            if (targetSlot != null)
            {
                targetSlot.AddConnection(sourceSlot, multiInputIndex);
                return true;
            }

            return false;
        }

        public void RemoveConnection(Symbol.Connection connection, int index)
        {
            var (_, _, _, targetSlot) = GetInstancesForConnection(connection);
            targetSlot.RemoveConnection(index);
        }

        private (Instance, ISlot, Instance, ISlot) GetInstancesForConnection(Symbol.Connection connection)
        {
            Instance compositionInstance = this;

            var sourceInstance = compositionInstance.Children.SingleOrDefault(child => child.SymbolChildId == connection.SourceParentOrChildId);
            ISlot sourceSlot;
            if (sourceInstance != null)
            {
                sourceSlot = sourceInstance.Outputs.SingleOrDefault(output => output.Id == connection.SourceSlotId);
            }
            else
            {
                if (connection.SourceParentOrChildId != Guid.Empty)
                {
                    Log.Error($"connection has incorrect Source: { connection.SourceParentOrChildId}");
                    return (null, null, null, null);
                }
                sourceInstance = compositionInstance;
                sourceSlot = sourceInstance.Inputs.SingleOrDefault(input => input.Id == connection.SourceSlotId);
            }

            var targetInstance = compositionInstance.Children.SingleOrDefault(child => child.SymbolChildId == connection.TargetParentOrChildId);
            ISlot targetSlot;
            if (targetInstance != null)
            {
                targetSlot = targetInstance.Inputs.SingleOrDefault(e => e.Id == connection.TargetSlotId);
            }
            else
            {
                Debug.Assert(connection.TargetParentOrChildId == Guid.Empty);
                targetInstance = compositionInstance;
                targetSlot = targetInstance.Outputs.SingleOrDefault(e => e.Id == connection.TargetSlotId);
            }

            return (sourceInstance, sourceSlot, targetInstance, targetSlot);
        }
    }

    public class Instance<T> : Instance where T : Instance
    {
        public override Type Type { get; } = typeof(T);

        protected Instance()
        {
            SetupInputAndOutputsFromType();
        }
    }
}
