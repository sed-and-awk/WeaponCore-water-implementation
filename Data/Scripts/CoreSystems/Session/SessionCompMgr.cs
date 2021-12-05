﻿using System;
using System.Collections.Generic;
using CoreSystems.Platform;
using CoreSystems.Support;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Utils;
using VRageMath;

namespace CoreSystems
{
    public partial class Session
    {
        public struct CompReAdd
        {
            public CoreComponent Comp;
            public Ai Ai;
            public int AiVersion;
            public uint AddTick;
        }

        private bool CompRestricted(CoreComponent comp)
        {
            var cube = comp.Cube;
            var grid = cube?.CubeGrid;

            Ai ai;
            if (grid == null || !EntityAIs.TryGetValue(grid, out ai))
                return false;

            MyOrientedBoundingBoxD b;
            BoundingSphereD s;
            MyOrientedBoundingBoxD blockBox;
            SUtils.GetBlockOrientedBoundingBox(cube, out blockBox);

            if (IsPartAreaRestricted(cube.BlockDefinition.Id.SubtypeId, blockBox, grid, comp.CoreEntity.EntityId, ai, out b, out s)) {

                if (!DedicatedServer) {

                    if (cube.OwnerId == PlayerId)
                        MyAPIGateway.Utilities.ShowNotification($"Block {comp.CoreEntity.DisplayNameText} was placed too close to another gun", 10000);
                }

                if (IsServer)
                    cube.CubeGrid.RemoveBlock(cube.SlimBlock);
                return true;
            }

            return false;
        }

        private void StartComps()
        {
            for (int i = 0; i < CompsToStart.Count; i++) {

                var comp = CompsToStart[i];

                if (comp.IsBlock && (comp.Cube.CubeGrid.IsPreview || CompRestricted(comp))) {

                    PlatFormPool.Return(comp.Platform);
                    comp.Platform = null;
                    CompsToStart.Remove(comp);
                    continue;
                }

                if (comp.IsBlock && (comp.Cube.CubeGrid.Physics == null && !comp.Cube.CubeGrid.MarkedForClose && comp.Cube.BlockDefinition.HasPhysics))
                    continue;

                QuickDisableGunsCheck = true;
                if (comp.Platform.State == CorePlatform.PlatformState.Fresh) {

                    if (comp.CoreEntity.MarkedForClose) {
                        CompsToStart.Remove(comp);
                        continue;
                    }

                    if (comp.IsBlock && !GridToInfoMap.ContainsKey(comp.TopEntity))
                        continue;

                    if (ShieldApiLoaded)
                        SApi.AddAttacker(comp.CoreEntity.EntityId);

                    IdToCompMap[comp.CoreEntity.EntityId] = comp;
                    comp.CoreEntity.Components.Add(comp);

                    CompsToStart.Remove(comp);
                }
                else {
                    Log.Line("comp didn't match CompsToStart condition, removing");
                    CompsToStart.Remove(comp);
                }
            }
            CompsToStart.ApplyRemovals();
        }

        private CoreComponent InitComp(MyEntity entity, ref MyDefinitionId? id)
        {
            CoreComponent comp = null;
            using (entity.Pin())
            {
                if (entity.MarkedForClose)
                    return null;

                CoreStructure c;
                if (id.HasValue && PartPlatforms.TryGetValue(id.Value, out c))
                {
                    switch (c.StructureType)
                    {
                        case CoreStructure.StructureTypes.Upgrade:
                            comp = new Upgrade.UpgradeComponent(this, entity, id.Value);
                            CompsToStart.Add(comp);
                            break;
                        case CoreStructure.StructureTypes.Support:
                            comp = new SupportSys.SupportComponent(this, entity, id.Value);
                            CompsToStart.Add(comp);
                            break;
                        case CoreStructure.StructureTypes.Weapon:
                            comp = new Weapon.WeaponComponent(this, entity, id.Value);
                            CompsToStart.Add(comp);
                            break;
                    }

                    CompsToStart.ApplyAdditions();
                }
            }
            return comp;
        }

        private void ChangeReAdds()
        {
            for (int i = CompReAdds.Count - 1; i >= 0; i--)
            {
                var reAdd = CompReAdds[i];
                if (reAdd.Ai.Version != reAdd.AiVersion || Tick - reAdd.AddTick > 1200)
                {
                    CompReAdds.RemoveAtFast(i);
                    Log.Line($"ChangeReAdds reject: Age:{Tick - reAdd.AddTick} - Version:{reAdd.Ai.Version}({reAdd.AiVersion}) - Marked/Closed:{reAdd.Ai.MarkedForClose}({reAdd.Ai.Closed})");
                    continue;
                }

                if (reAdd.Comp.IsBlock && !GridToInfoMap.ContainsKey(reAdd.Comp.TopEntity))
                    continue;

                if (reAdd.Comp.Ai != null && reAdd.Comp.Entity != null) 
                    reAdd.Comp.OnAddedToSceneTasks(true);
                //else Log.Line($"ChangeReAdds nullSkip: Version:{reAdd.Ai.Version}({reAdd.AiVersion}) - Marked/Closed:{reAdd.Ai.MarkedForClose}({reAdd.Ai.Closed})");
                CompReAdds.RemoveAtFast(i);
            }
        }

        private void DelayedComps(bool forceRemove = false)
        {
            for (int i = CompsDelayed.Count - 1; i >= 0; i--)
            {
                var delayed = CompsDelayed[i];
                if (forceRemove || delayed.Entity == null || delayed.Platform == null || delayed.Cube.MarkedForClose || delayed.Platform.State != CorePlatform.PlatformState.Delay)
                {
                    if (delayed.Platform != null && delayed.Platform.State != CorePlatform.PlatformState.Delay)
                        Log.Line($"[DelayedComps skip due to platform != Delay] marked:{delayed.Cube.MarkedForClose} - entityNull:{delayed.Entity == null} - force:{forceRemove}");

                    CompsDelayed.RemoveAtFast(i);
                }
                else if (delayed.Cube.IsFunctional)
                {
                    delayed.PlatformInit();
                    CompsDelayed.RemoveAtFast(i);
                }
            }
        }

        private void DelayedAiCleanup()
        {
            for (int i = 0; i < DelayedAiClean.Count; i++)
            {
                var ai = DelayedAiClean[i];
                ai.AiDelayedClose();
                if (ai.Closed)
                    DelayedAiClean.Remove(ai);
            }
            DelayedAiClean.ApplyRemovals();
        }

        internal void CloseComps(MyEntity entity)
        {
            try
            {
                entity.OnClose -= CloseComps;
                var cube = entity as MyCubeBlock;
                if (cube != null && cube.CubeGrid.IsPreview)
                    return;

                CoreComponent comp;
                if (!entity.Components.TryGet(out comp)) return;

                for (int i = 0; i < comp.Monitors.Length; i++) {
                    comp.Monitors[i].Clear();
                    comp.Monitors[i] = null;
                }

                if (comp.Platform.State == CorePlatform.PlatformState.Ready)
                {
                    if (comp.Type == CoreComponent.CompType.Weapon) {

                        var wComp = (Weapon.WeaponComponent)comp;
                        wComp.GeneralWeaponCleanUp();
                        wComp.StopAllSounds();
                        wComp.CleanCompParticles();
                        wComp.CleanCompSounds();
                    }
                    comp.Platform.RemoveParts();
                }

                if (comp.Ai != null)
                {
                    Log.Line("BaseComp still had AI on close");
                    comp.Ai = null;
                }
                
                if (comp.Registered)
                {
                    Log.Line("comp still registered");
                    comp.RegisterEvents(false);
                }

                PlatFormPool.Return(comp.Platform);
                comp.Platform = null;
                var sinkInfo = new MyResourceSinkInfo
                {
                    ResourceTypeId = comp.GId,
                    MaxRequiredInput = 0f,
                    RequiredInputFunc = null,
                };

                if (comp.IsBlock) 
                    comp.Cube.ResourceSink.Init(MyStringHash.GetOrCompute("Charging"), sinkInfo, cube);
            }
            catch (Exception ex) { Log.Line($"Exception in DelayedCompClose: {ex}", null, true); }
        }
    }
}
