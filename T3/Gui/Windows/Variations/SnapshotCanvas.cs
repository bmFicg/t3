﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Operator;
using T3.Gui.Graph.Interaction;
using T3.Gui.Interaction.Variations;
using T3.Gui.Interaction.Variations.Model;
using T3.Gui.Selection;
using T3.Gui.Styling;

namespace T3.Gui.Windows.Variations
{
    public class SnapshotCanvas : VariationBaseCanvas
    {
        protected override Instance InstanceForBlendOperations => VariationHandling.ActiveInstanceForSnapshots;
        protected override SymbolVariationPool PoolForBlendOperations => VariationHandling.ActivePoolForSnapshots;

        public override void DrawToolbarFunctions()
        {
            var s = ImGui.GetFrameHeight();

            if (CustomComponents.IconButton(Icon.Plus, "##addbutton", new Vector2(s, s)))
            {
                CreateVariation();
            }
            
            var filteredOpCount = 0;

            var compositionId = VariationHandling.ActiveInstanceForSnapshots.Symbol.Id;
            if( VariationHandling.SubsetsForCompositions.TryGetValue(compositionId, out var filterSet))
            {
                filteredOpCount = filterSet.Count;
            }

            ImGui.SameLine();
            ImGui.Dummy(new Vector2(20,20));
            
            ImGui.SameLine();
            if (filteredOpCount == 0)
            {
                if (ImGui.Button("Set focus"))
                {
                    var set = new HashSet<Guid>();
                    foreach (var selectedOp in NodeSelection.GetSelectedInstances())
                    {
                        set.Add(selectedOp.SymbolChildId);
                    }
                    VariationHandling.SubsetsForCompositions[compositionId] = set;
                }
                CustomComponents.TooltipForLastItem("This will limit the parameters stored in new snapshots to the Operators selected when setting the focus.");
            }
            else
            {
                if (ImGui.Button($"Clear focus ({filteredOpCount})"))
                {
                    VariationHandling.SubsetsForCompositions.Remove(compositionId);
                }

                if (ImGui.IsItemHovered())
                {
                    if (filterSet != null)
                    {
                        foreach (var id in filterSet)
                        {
                            T3Ui.AddHoveredId(id);
                        }
                    }
                }
            }
        }
        
        protected override void DrawAdditionalContextMenuContent()
        {
            var oneSelected = Selection.SelectedElements.Count == 1;
            
            if (ImGui.MenuItem("Select affected Operators",
                               "",
                               false,
                               oneSelected))
            {
                if (Selection.SelectedElements[0] is not Variation selectedVariation)
                    return;
                
                NodeSelection.Clear();

                var parentSymbolUi = SymbolUiRegistry.Entries[InstanceForBlendOperations.Symbol.Id];
                    
                foreach (var symbolChildUi in parentSymbolUi.ChildUis)
                {
                    if (selectedVariation.ParameterSetsForChildIds.ContainsKey(symbolChildUi.Id))
                    {
                        var instance = InstanceForBlendOperations.Children.FirstOrDefault(c => c.SymbolChildId == symbolChildUi.Id);
                        if(instance != null)
                            NodeSelection.AddSymbolChildToSelection(symbolChildUi, instance);
                    }
                }
                FitViewToSelectionHandling.FitViewToSelection();
            }
        }

        public override Variation CreateVariation()
        {
            var newVariation = VariationHandling.SaveVariationForSelectedOperators();
            if (newVariation == null)
                return new Variation();
            
            Selection.SetSelection(newVariation);
            ResetView();
            TriggerThumbnailUpdate();
            VariationThumbnail.VariationForRenaming = newVariation;
            return new Variation();
        }
    }
}