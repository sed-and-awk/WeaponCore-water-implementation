﻿using System;
using CoreSystems.Platform;
using Sandbox.Game.Entities;
using VRage.Game.Components;
using VRageMath;
using static CoreSystems.Session;
using static CoreSystems.Support.Ai;

namespace CoreSystems.Support
{
    public partial class CoreComponent : MyEntityComponentBase
    {
        public override void OnAddedToContainer()
        {
            try
            {
                base.OnAddedToContainer();
                if (Container.Entity.InScene) {

                    LastAddToScene = Session.Tick;
                    if (Platform.State == CorePlatform.PlatformState.Fresh)
                        PlatformInit();
                }
                else 
                    Log.Line($"Tried to add comp but it was not in scene");
            }
            catch (Exception ex) { Log.Line($"Exception in OnAddedToContainer: {ex}", null, true); }
        }

        public override void OnAddedToScene()
        {
            try
            {
                base.OnAddedToScene();

                if (Platform.State == CorePlatform.PlatformState.Inited || Platform.State == CorePlatform.PlatformState.Ready)
                    ReInit();
                else {

                    if (Platform.State == CorePlatform.PlatformState.Delay)
                        return;
                    
                    if (Platform.State != CorePlatform.PlatformState.Fresh)
                        Log.Line($"OnAddedToScene != Fresh, Inited or Ready: {Platform.State}");

                    PlatformInit();
                }
            }
            catch (Exception ex) { Log.Line($"Exception in OnAddedToScene: {ex}", null, true); }
        }
        
        public override void OnBeforeRemovedFromContainer()
        {
            base.OnBeforeRemovedFromContainer();
        }

        internal void PlatformInit()
        {
            switch (Platform.Init()) {

                case CorePlatform.PlatformState.Invalid:
                    Platform.PlatformCrash(this, false, false, $"Platform PreInit is in an invalid state: {SubtypeName}");
                    break;
                case CorePlatform.PlatformState.Valid:
                    Platform.PlatformCrash(this, false, true, $"Something went wrong with Platform PreInit: {SubtypeName}");
                    break;
                case CorePlatform.PlatformState.Delay:
                    Session.CompsDelayed.Add(this);
                    break;
                case CorePlatform.PlatformState.Inited:
                    Init();
                    break;
            }
        }

        internal void Init()
        {
            using (CoreEntity.Pin()) 
            {
                if (!CoreEntity.MarkedForClose && Entity != null) 
                {
                    Ai.FirstRun = true;

                    StorageSetup();

                    if (TypeSpecific != CompTypeSpecific.Phantom) {
                        InventoryInit();

                        if (IsBlock)
                            PowerInit();
                    }

                    //if (Type == CompType.Weapon && Platform.PartState == CorePlatform.PlatformState.Inited)
                        //Platform.ResetParts(this);

                    Entity.NeedsWorldMatrix = NeedsWorldMatrix;
                    WorldMatrixEnabled = NeedsWorldMatrix;
                    if (!Ai.AiInit) Session.CompReAdds.Add(new CompReAdd { Ai = Ai, AiVersion = Ai.Version, AddTick = Ai.Session.Tick, Comp = this });
                    else OnAddedToSceneTasks(true);

                    Platform.State = CorePlatform.PlatformState.Ready;
                } 
                else Log.Line("BaseComp Init() failed");
            }
        }

        internal void ReInit()
        {
            using (CoreEntity.Pin())  {

                if (!CoreEntity.MarkedForClose && Entity != null)  {

                    if (IsBlock) 
                        TopEntity = ((Weapon.WeaponComponent)this).GetTopEntity();
                    
                    Ai ai;
                    if (!Session.EntityAIs.TryGetValue(TopEntity, out ai)) {

                        var newAi = Session.GridAiPool.Get();
                        newAi.Init(TopEntity, Session, TypeSpecific);
                        Session.EntityAIs[TopEntity] = newAi;
                        Ai = newAi;
                    }
                    else {
                        Ai = ai;
                    }

                    if (Ai != null) {

                        Ai.FirstRun = true;

                        if (Type == CompType.Weapon && Platform.State == CorePlatform.PlatformState.Inited)
                            Platform.ResetParts();

                        Entity.NeedsWorldMatrix = NeedsWorldMatrix; 
                        WorldMatrixEnabled = NeedsWorldMatrix;

                        // ReInit Counters
                        if (!Ai.PartCounting.ContainsKey(SubTypeId)) // Need to account for reinit case
                            Ai.PartCounting[SubTypeId] = Session.PartCountPool.Get();

                        var pCounter = Ai.PartCounting[SubTypeId];
                        pCounter.Max = Platform.Structure.ConstructPartCap;

                        pCounter.Current++;
                        Constructs.UpdatePartCounters(Ai);
                        // end ReInit

                        if (IsBlock && !Ai.AiInit || IsBlock && !Ai.Session.GridToInfoMap.ContainsKey(Ai.TopEntity)) 
                            Session.CompReAdds.Add(new CompReAdd { Ai = Ai, AiVersion = Ai.Version, AddTick = Ai.Session.Tick, Comp = this });
                        else 
                            OnAddedToSceneTasks(false);
                    }
                    else {
                        Log.Line("BaseComp ReInit() failed stage2!");
                    }
                }
                else {
                    Log.Line($"BaseComp ReInit() failed stage1! - marked:{CoreEntity.MarkedForClose} - Entity:{Entity != null} - hasAi:{Session.EntityAIs.ContainsKey(TopEntity)}");
                }
            }
        }

        internal void OnAddedToSceneTasks(bool firstRun)
        {
            try {

                if (Ai.MarkedForClose)
                    Log.Line($"OnAddedToSceneTasks and AI MarkedForClose - Subtype:{SubtypeName} - grid:{TopEntity.DebugName} - CubeMarked:{CoreEntity.MarkedForClose} - GridMarked:{TopEntity.MarkedForClose} - GridMatch:{TopEntity == Ai.TopEntity} - AiContainsMe:{Ai.CompBase.ContainsKey(CoreEntity)} - MyGridInAi:{Ai.Session.EntityToMasterAi.ContainsKey(TopEntity)}[{Ai.Session.EntityAIs.ContainsKey(TopEntity)}]");
                
                Ai.UpdatePowerSources = true;
                RegisterEvents();
                if (!Ai.AiInit) {

                    Ai.AiInit = true;
                    if (IsBlock)
                    {
                        var fatList = Session.GridToInfoMap[TopEntity].MyCubeBocks;

                        for (int i = 0; i < fatList.Count; i++)
                        {

                            var cubeBlock = fatList[i];
                            if (cubeBlock is MyBatteryBlock || cubeBlock.HasInventory)
                                Ai.FatBlockAdded(cubeBlock);
                        }

                        SubGridInit();
                    }
                }

                if (Type == CompType.Weapon)
                    ((Weapon.WeaponComponent)this).OnAddedToSceneWeaponTasks(firstRun);


                if (!Ai.CompBase.TryAdd(CoreEntity, this))
                    Log.Line("failed to add cube to gridAi");

                Ai.CompChange(true, this);

                Ai.IsStatic = Ai.TopEntity.Physics?.IsStatic ?? false;
                Ai.Construct.Refresh(Ai, Constructs.RefreshCaller.Init);

                if (IsBlock)
                {
                    MyOrientedBoundingBoxD obb;
                    SUtils.GetBlockOrientedBoundingBox(Cube, out obb);
                    foreach (var weapon in Platform.Weapons)
                    {
                        var scopeInfo = weapon.GetScope.Info;
                        if (!obb.Contains(ref scopeInfo.Position))
                        {
                            var rayBack = new RayD(scopeInfo.Position, -scopeInfo.Direction);
                            weapon.ScopeDistToCheckPos = obb.Intersects(ref rayBack) ?? 0;
                        }
                        Session.FutureEvents.Schedule(weapon.DelayedStart, FunctionalBlock.Enabled, 1);
                    }
                }
                Status = !IsWorking ? Start.Starting : Start.ReInit;
            }
            catch (Exception ex) { Log.Line($"Exception in OnAddedToSceneTasks: {ex} AiNull:{Ai == null} - SessionNull:{Session == null} EntNull{Entity == null} MyCubeNull:{TopEntity == null}", null, true); }
        }

        public override void OnRemovedFromScene()
        {
            try
            {
                base.OnRemovedFromScene();
                RemoveComp();
            }
            catch (Exception ex) { Log.Line($"Exception in OnRemovedFromScene: {ex}", null, true); }
        }

        public override bool IsSerialized()
        {
            if (Platform.State == CorePlatform.PlatformState.Ready) {

                if (CoreEntity?.Storage != null) {
                    BaseData.Save();
                }
            }
            return false;
        }

        public override string ComponentTypeDebugString => "CoreSystems";
    }
}
