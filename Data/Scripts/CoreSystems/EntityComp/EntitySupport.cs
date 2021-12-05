﻿using System;
using CoreSystems.Platform;
using VRage.Game.ObjectBuilders.Definitions;
using static CoreSystems.Support.Ai;
namespace CoreSystems.Support
{
    public partial class CoreComponent 
    {
        internal void TerminalRefresh(bool update = true)
        {
            if (Platform.State != CorePlatform.PlatformState.Ready || Status != Start.Started)
                return;

            if (Ai?.LastTerminal == CoreEntity)  {

                TerminalBlock.RefreshCustomInfo();

                if (update && InControlPanel)
                {
                    Cube.UpdateTerminal();
                }
            }
        }

        internal void RemoveComp()
        {
            try {

                if (Registered) 
                    RegisterEvents(false);
                if (Ai != null) {

                    if (Type == CompType.Weapon)
                    {
                        Ai.OptimalDps -= ((Weapon.WeaponComponent)this).PeakDps;
                        Ai.EffectiveDps -= ((Weapon.WeaponComponent)this).EffectiveDps;
                    }

                    PartCounter wCount;
                    if (Ai.PartCounting.TryGetValue(SubTypeId, out wCount)) {
                        wCount.Current--;
                        Constructs.UpdatePartCounters(Ai);
                        if (wCount.Current == 0)
                        {
                            Ai.PartCounting.Remove(SubTypeId);
                            Session.PartCountPool.Return(wCount);
                        }
                    }
                    else if (Session.LocalVersion) Log.Line($"didnt find counter for: {SubTypeId} - MarkedForClose:{Ai.MarkedForClose} - AiAge:{Ai.Session.Tick - Ai.AiSpawnTick} - CubeMarked:{CoreEntity.MarkedForClose} - GridMarked:{TopEntity.MarkedForClose}");

                    if (Ai.Data.Repo.ActiveTerminal == CoreEntity.EntityId)
                        Ai.Data.Repo.ActiveTerminal = 0;
                    
                    Ai testAi;
                    CoreComponent comp;
                    if (Ai.CompBase.TryRemove(CoreEntity, out comp)) {
                        if (Platform.State == CorePlatform.PlatformState.Ready) {

                            var collection = TypeSpecific != CompTypeSpecific.Phantom ? Platform.Weapons : Platform.Phantoms;

                            for (int i = 0; i < collection.Count; i++) {
                                var w = collection[i];
                                w.StopShooting();
                                w.WeaponCache.HitEntity.Clean();
                                if (!Session.IsClient) w.Target.Reset(Session.Tick, Target.States.AiLost);
                                
                                if (w.InCharger)
                                    w.ExitCharger = true;
                                if (w.CriticalReaction)
                                    w.CriticalOnDestruction();
                            }
                        }
                        Ai.CompChange(false, this);
                    }
                    else Log.Line($"RemoveComp Weaponbase didn't have my comp: {Ai.Session.CompsDelayed.Contains(this)} - FoundAi:{Ai.Session.EntityAIs.TryGetValue(TopEntity, out testAi)} - sameAi:{testAi == Ai} - sameTopEntity:{comp.TopEntity == Ai.TopEntity} - inScene:{comp.CoreEntity.InScene} - LastRemoveFromScene:{LastRemoveFromScene} - LastAddToScene:{LastAddToScene} - Tick:{Session.Tick}");

                    if (Ai.CompBase.Count == 0) {
                        Ai ai;
                        Session.EntityAIs.TryRemove(Ai.TopEntity, out ai);
                    }
                    
                    if (Session.TerminalMon.Comp == this)
                        Session.TerminalMon.Clean(true);

                    Ai = null;
                }
                else if (Platform.State != CorePlatform.PlatformState.Delay) Log.Line($"CompRemove: Ai already null - PartState:{Platform.State} - Status:{Status}");

                LastRemoveFromScene = Session.Tick;
            }
            catch (Exception ex) { Log.Line($"Exception in RemoveComp: {ex} - AiNull:{Ai == null} - SessionNull:{Session == null}", null, true); }
        }
    }
}
