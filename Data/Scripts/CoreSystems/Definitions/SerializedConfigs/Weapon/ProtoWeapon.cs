﻿using System;
using System.ComponentModel;
using CoreSystems.Platform;
using CoreSystems.Support;
using ProtoBuf;
using VRageMath;
using static CoreSystems.Support.WeaponDefinition.TargetingDef;
using static CoreSystems.Support.CoreComponent;

namespace CoreSystems
{
    [ProtoContract]
    public class ProtoWeaponRepo : ProtoRepo
    {
        [ProtoMember(1)] public ProtoWeaponAmmo[] Ammos;
        [ProtoMember(2)] public ProtoWeaponComp Values;

        public void ResetToFreshLoadState(Weapon.WeaponComponent comp)
        {
            Values.State.TrackingReticle = false;
            Values.Set.DpsModifier = 1;
            Values.Set.Overrides.Control = ProtoWeaponOverrides.ControlModes.Auto;

            if (Values.State.Control == ProtoWeaponState.ControlMode.Ui)
                Values.State.Control = ProtoWeaponState.ControlMode.None;

            if (Values.State.TerminalAction == TriggerActions.TriggerOnce)
                Values.State.TerminalAction = TriggerActions.TriggerOff;

            if (comp.DefaultTrigger != TriggerActions.TriggerOff)
                Values.State.TerminalAction = comp.DefaultTrigger;

            for (int i = 0; i < Ammos.Length; i++)
            {
                var ws = Values.State.Weapons[i];
                var wr = Values.Reloads[i];
                var wa = Ammos[i];
                var we = comp.Collection[i];

                if (comp.DefaultReloads != 0)
                    we.Reload.CurrentMags = comp.DefaultReloads;
                
                ws.Heat = 0;
                ws.Overheated = false;
                if (ws.Action == TriggerActions.TriggerOnce)
                    ws.Action = TriggerActions.TriggerOff;

                if (comp.DefaultTrigger != TriggerActions.TriggerOff)
                {
                    ws.Action = comp.DefaultTrigger;
                }

                wr.StartId = 0;
                wr.WaitForClient = false;
            }
            ResetCompBaseRevisions();
        }

        public void ResetCompBaseRevisions()
        {
            Values.Revision = 0;
            Values.State.Revision = 0;
            for (int i = 0; i < Ammos.Length; i++)
            {
                Values.Targets[i].Revision = 0;
                Values.Reloads[i].Revision = 0;
                Ammos[i].Revision = 0;
            }
        }
    }

    [ProtoContract]
    public class ProtoWeaponAmmo
    {
        [ProtoMember(1)] public uint Revision;
        [ProtoMember(2)] public int CurrentAmmo; //save
        [ProtoMember(3)] public float CurrentCharge; //save
        //[ProtoMember(4)] public long CurrentMags; // save
        //[ProtoMember(5)] public int AmmoTypeId; //remove me
        //[ProtoMember(6)] public int AmmoCycleId; //remove me

        public bool Sync(Weapon w, ProtoWeaponAmmo sync)
        {
            if (sync.Revision > Revision)
            {
                Revision = sync.Revision;

                CurrentAmmo = sync.CurrentAmmo;
                CurrentCharge = sync.CurrentCharge;
                w.ClientMakeUpShots = 0;

                return true;
            }
            return false;
        }
    }

    [ProtoContract]
    public class ProtoWeaponComp
    {
        [ProtoMember(1)] public uint Revision;
        [ProtoMember(2)] public ProtoWeaponSettings Set;
        [ProtoMember(3)] public ProtoWeaponState State;
        [ProtoMember(4)] public ProtoWeaponTransferTarget[] Targets;
        [ProtoMember(5)] public ProtoWeaponReload[] Reloads;

        public bool Sync(Weapon.WeaponComponent comp, ProtoWeaponComp sync)
        {
            if (sync.Revision > Revision)
            {
                Revision = sync.Revision;
                Set.Sync(comp, sync.Set);
                State.Sync(comp, sync.State, ProtoWeaponState.Caller.CompData);

                for (int i = 0; i < Targets.Length; i++)
                {
                    var w = comp.Collection[i];
                    sync.Targets[i].SyncTarget(w, true);
                    Reloads[i].Sync(w, sync.Reloads[i], true);
                }
                return true;
            }
            return false;

        }

        public void UpdateCompPacketInfo(Weapon.WeaponComponent comp, bool clean = false, bool resetRnd = false)
        {
            ++Revision;
            ++State.Revision;
            Session.PacketInfo info;
            if (clean && comp.Session.PrunedPacketsToClient.TryGetValue(comp.Data.Repo.Values.State, out info))
            {
                comp.Session.PrunedPacketsToClient.Remove(comp.Data.Repo.Values.State);
                comp.Session.PacketWeaponStatePool.Return((WeaponStatePacket)info.Packet);
            }

            for (int i = 0; i < Targets.Length; i++)
            {

                var t = Targets[i];
                var wr = Reloads[i];

                if (clean)
                {
                    if (comp.Session.PrunedPacketsToClient.TryGetValue(t, out info))
                    {
                        comp.Session.PrunedPacketsToClient.Remove(t);
                        comp.Session.PacketTargetPool.Return((TargetPacket)info.Packet);
                    }
                    if (comp.Session.PrunedPacketsToClient.TryGetValue(wr, out info))
                    {
                        comp.Session.PrunedPacketsToClient.Remove(wr);
                        comp.Session.PacketReloadPool.Return((WeaponReloadPacket)info.Packet);
                    }
                }
                ++wr.Revision;
                ++t.Revision;
                
                if (resetRnd)
                    t.WeaponRandom.ReInitRandom();
            }
        }
    }

    [ProtoContract]
    public class ProtoWeaponReload
    {
        [ProtoMember(1)] public uint Revision;
        [ProtoMember(2)] public int StartId; //save
        [ProtoMember(3)] public int EndId; //save
        [ProtoMember(4)] public int MagsLoaded = 1;
        [ProtoMember(5)] public bool WaitForClient; //don't save
        [ProtoMember(6)] public int AmmoTypeId; //save
        [ProtoMember(7)] public int CurrentMags; // save

        public void Sync(Weapon w, ProtoWeaponReload sync, bool force)
        {
            if (sync.Revision > Revision || force)
            {
                Revision = sync.Revision;
                var newReload = StartId != sync.StartId && EndId == sync.EndId;
                StartId = sync.StartId;
                EndId = sync.EndId;

                MagsLoaded = sync.MagsLoaded;
                
                WaitForClient = sync.WaitForClient;
                
                var oldAmmoId = AmmoTypeId;
                AmmoTypeId = sync.AmmoTypeId;

                if (oldAmmoId != AmmoTypeId)
                {
                    w.ServerQueuedAmmo = newReload;

                    if (newReload)
                        w.AmmoName = w.System.AmmoTypes[AmmoTypeId].AmmoName;

                    w.ChangeActiveAmmoClient();
                }

                w.ClientReload(true);
            }
        }
    }

    [ProtoContract]
    public class ProtoWeaponSettings
    {
        [ProtoMember(1), DefaultValue(true)] public bool Guidance = true;
        [ProtoMember(2), DefaultValue(1)] public int Overload = 1;
        [ProtoMember(3), DefaultValue(1)] public float DpsModifier = 1;
        [ProtoMember(4), DefaultValue(1)] public float RofModifier = 1;
        [ProtoMember(5), DefaultValue(100)] public float Range = 100;
        [ProtoMember(6)] public ProtoWeaponOverrides Overrides;


        public ProtoWeaponSettings()
        {
            Overrides = new ProtoWeaponOverrides();
        }

        public void Sync(Weapon.WeaponComponent comp, ProtoWeaponSettings sync)
        {
            Guidance = sync.Guidance;
            Range = sync.Range;
            Weapon.WeaponComponent.SetRange(comp);

            Overrides.Sync(sync.Overrides);

            var rofChange = Math.Abs(RofModifier - sync.RofModifier) > 0.0001f;
            var dpsChange = Math.Abs(DpsModifier - sync.DpsModifier) > 0.0001f;

            if (Overload != sync.Overload || rofChange || dpsChange)
            {
                Overload = sync.Overload;
                RofModifier = sync.RofModifier;
                DpsModifier = sync.DpsModifier;
                if (rofChange) Weapon.WeaponComponent.SetRof(comp);
            }
        }

    }

    [ProtoContract]
    public class ProtoWeaponState
    {
        public enum Caller
        {
            Direct,
            CompData,
        }

        public enum ControlMode
        {
            None,
            Ui,
            Toolbar,
            Camera
        }

        [ProtoMember(1)] public uint Revision;
        [ProtoMember(2)] public ProtoWeaponPartState[] Weapons;
        [ProtoMember(3)] public bool TrackingReticle; //don't save
        [ProtoMember(4), DefaultValue(-1)] public long PlayerId = -1;
        [ProtoMember(5), DefaultValue(ControlMode.None)] public ControlMode Control = ControlMode.None;
        [ProtoMember(6)] public TriggerActions TerminalAction;
        [ProtoMember(7)] public bool CountingDown;
        [ProtoMember(8)] public bool CriticalReaction;

        public bool Sync(Weapon.WeaponComponent comp, ProtoWeaponState sync, Caller caller)
        {
            if (sync.Revision > Revision || caller == Caller.CompData)
            {
                Revision = sync.Revision;
                TrackingReticle = sync.TrackingReticle;
                PlayerId = sync.PlayerId;
                Control = sync.Control;
                TerminalAction = sync.TerminalAction;
                CountingDown = sync.CountingDown;
                CriticalReaction = sync.CriticalReaction;
                for (int i = 0; i < sync.Weapons.Length; i++)
                    comp.Platform.Weapons[i].PartState.Sync(sync.Weapons[i]);
                return true;
            }
            return false;
        }

        public void TerminalActionSetter(Weapon.WeaponComponent comp, TriggerActions action, bool syncWeapons = false, bool updateWeapons = true)
        {
            TerminalAction = action;

            if (updateWeapons)
            {
                for (int i = 0; i < Weapons.Length; i++)
                    Weapons[i].Action = action;
            }

            if (syncWeapons)
                comp.Session.SendState(comp);
        }
    }

    [ProtoContract]
    public class ProtoWeaponPartState
    {
        [ProtoMember(1)] public float Heat; // don't save
        [ProtoMember(2)] public bool Overheated; //don't save
        [ProtoMember(3), DefaultValue(TriggerActions.TriggerOff)] public TriggerActions Action = TriggerActions.TriggerOff; // save

        public void Sync(ProtoWeaponPartState sync)
        {
            Heat = sync.Heat;
            Overheated = sync.Overheated;
            Action = sync.Action;
        }

        public void WeaponMode(Weapon.WeaponComponent comp, TriggerActions action, bool resetTerminalAction = true, bool syncCompState = true)
        {
            if (resetTerminalAction)
                comp.Data.Repo.Values.State.TerminalAction = TriggerActions.TriggerOff;

            Action = action;
            if (comp.Session.MpActive && comp.Session.IsServer && syncCompState)
                comp.Session.SendState(comp);
        }

    }

    [ProtoContract]
    public class ProtoWeaponTransferTarget
    {
        [ProtoMember(1)] public uint Revision;
        [ProtoMember(2)] public long EntityId;
        [ProtoMember(3)] public Vector3 TargetPos;
        [ProtoMember(4)] public int PartId;
        [ProtoMember(5)] public WeaponRandomGenerator WeaponRandom; // save

        internal bool SyncTarget(Weapon w, bool force)
        {
            if (Revision > w.TargetData.Revision || force)
            {
                w.TargetData.Revision = Revision;
                w.TargetData.EntityId = EntityId;
                w.TargetData.TargetPos = TargetPos;
                w.PartId = PartId;
                w.TargetData.WeaponRandom.Sync(WeaponRandom);

                var target = w.Target;

                var newProjectile = EntityId == -1;
                var noTarget = EntityId == 0;

                if (!w.ActiveAmmoDef.AmmoDef.Const.Reloadable && !noTarget)
                    w.ProjectileCounter = 0;

                if (newProjectile)
                {
                    target.ProjectileEndTick = 0;
                    target.SoftProjetileReset = false;
                    target.IsProjectile = true;
                }
                else if (noTarget && target.IsProjectile)
                {
                    target.SoftProjetileReset = true;
                    target.ProjectileEndTick = w.System.Session.Tick + 62;
                }
                else
                {
                    target.ProjectileEndTick = 0;
                    target.SoftProjetileReset = false;
                    target.IsProjectile = false;
                }



                target.IsFakeTarget = EntityId == -2;
                target.TargetPos = TargetPos;
                target.ClientDirty = true;
                return true;
            }
            return false;
        }

        internal void ClearTarget()
        {
            ++Revision;
            EntityId = 0;
            TargetPos = Vector3.Zero;
        }
    }

    [ProtoContract]
    public class ProtoWeaponOverrides
    {
        public enum MoveModes
        {
            Any,
            Moving,
            Mobile,
            Moored,
        }

        public enum ControlModes
        {
            Auto,
            Manual,
            Painter,
        }

        [ProtoMember(1)] public bool Neutrals;
        [ProtoMember(2)] public bool Unowned;
        [ProtoMember(3)] public bool Friendly;
        [ProtoMember(4)] public bool FocusTargets;
        [ProtoMember(5)] public bool FocusSubSystem;
        [ProtoMember(6)] public int MinSize;
        [ProtoMember(7), DefaultValue(ControlModes.Auto)] public ControlModes Control = ControlModes.Auto;
        [ProtoMember(8), DefaultValue(BlockTypes.Any)] public BlockTypes SubSystem = BlockTypes.Any;
        [ProtoMember(9), DefaultValue(true)] public bool Meteors = true;
        [ProtoMember(10), DefaultValue(true)] public bool Biologicals = true;
        [ProtoMember(11), DefaultValue(true)] public bool Projectiles = true;
        [ProtoMember(12), DefaultValue(16384)] public int MaxSize = 16384;
        [ProtoMember(13), DefaultValue(MoveModes.Any)] public MoveModes MoveMode = MoveModes.Any;
        [ProtoMember(14), DefaultValue(true)] public bool Grids = true;
        [ProtoMember(15), DefaultValue(true)] public bool ArmorShowArea;
        [ProtoMember(16)] public bool Repel;
        [ProtoMember(17)] public long CameraChannel;
        [ProtoMember(18)] public bool Debug;
        [ProtoMember(19)] public long LeadGroup;
        [ProtoMember(20)] public bool Armed;
        [ProtoMember(21)] public long ArmedTimer;


        public void Sync(ProtoWeaponOverrides syncFrom)
        {
            MoveMode = syncFrom.MoveMode;
            MaxSize = syncFrom.MaxSize;
            MinSize = syncFrom.MinSize;
            Neutrals = syncFrom.Neutrals;
            Unowned = syncFrom.Unowned;
            Friendly = syncFrom.Friendly;
            Control = syncFrom.Control;
            FocusTargets = syncFrom.FocusTargets;
            FocusSubSystem = syncFrom.FocusSubSystem;
            SubSystem = syncFrom.SubSystem;
            Meteors = syncFrom.Meteors;
            Grids = syncFrom.Grids;
            ArmorShowArea = syncFrom.ArmorShowArea;
            Biologicals = syncFrom.Biologicals;
            Projectiles = syncFrom.Projectiles;
            Repel = syncFrom.Repel;
            CameraChannel = syncFrom.CameraChannel;
            Debug = syncFrom.Debug;
            LeadGroup = syncFrom.LeadGroup;
        }
    }
}
