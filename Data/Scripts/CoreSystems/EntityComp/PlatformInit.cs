﻿using System;
using System.Collections.Generic;
using CoreSystems.Support;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents.Renders;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRageMath;
using VRageRender;
using static CoreSystems.Support.CoreComponent.Start;
using static CoreSystems.Support.CoreComponent.CompTypeSpecific;
using static CoreSystems.Support.WeaponDefinition.AnimationDef.PartAnimationSetDef;

namespace CoreSystems.Platform
{
    public class CorePlatform
    {
        internal readonly RecursiveSubparts Parts = new RecursiveSubparts();
        private readonly List<int> _orderToCreate = new List<int>();
        internal List<Weapon> Weapons = new List<Weapon>();
        internal List<SupportSys> Support = new List<SupportSys>();
        internal List<Upgrades> Upgrades = new List<Upgrades>();
        internal List<Weapon> Phantoms = new List<Weapon>();
        internal CoreStructure Structure;
        internal CoreComponent Comp;
        internal PlatformState State;

        internal enum PlatformState
        {
            Fresh,
            Invalid,
            Delay,
            Valid,
            Inited,
            Ready,
            Incomplete
        }
        
        internal void Setup(CoreComponent comp)
        {
            if (!comp.Session.PartPlatforms.ContainsKey(comp.Id))
            {
                PlatformCrash(comp, true, true, $"Your block subTypeId ({comp.SubtypeName}) was not found in platform setup, I am crashing now Dave.");
                return;
            }
            Structure = comp.Session.PartPlatforms[comp.Id];
            Comp = comp;
        }

        internal void Clean()
        {
            Weapons.Clear();
            Support.Clear();
            Upgrades.Clear();
            Phantoms.Clear();
            Parts.Clean(null);
            Structure = null;
            State = PlatformState.Fresh;
            Comp = null;
        }

        internal PlatformState Init()
        {

            if (Comp.CoreEntity.MarkedForClose) 
                return PlatformCrash(Comp, true, false, $"Your block subTypeId ({Comp.SubtypeName}) markedForClose, init platform invalid, I am crashing now Dave.");
            
            if (Comp.IsBlock && (!Comp.Cube.IsFunctional || Comp.Cube.MarkedForClose || Comp.Ai != null && Comp.Ai.MarkedForClose)) {
                State = PlatformState.Delay;
                return State;
            }

            //Get or init Ai
            var newAi = false;
            if (!Comp.Session.EntityAIs.TryGetValue(Comp.TopEntity, out Comp.Ai)) {
                newAi = true;
                Comp.Ai = Comp.Session.GridAiPool.Get();
                Comp.Ai.Init(Comp.TopEntity, Comp.Session, Comp.TypeSpecific);
                Comp.Session.EntityAIs.TryAdd(Comp.TopEntity, Comp.Ai);
            }

            var blockDef = Comp.SubTypeId; 
            if (!Comp.Ai.PartCounting.ContainsKey(blockDef)) 
                Comp.Ai.PartCounting[blockDef] = Comp.Session.PartCountPool.Get();

            var wCounter = Comp.Ai.PartCounting[blockDef];
            wCounter.Max = Structure.ConstructPartCap;

            if (newAi) {

                if (Comp.IsBlock) Comp.SubGridInit();
                if (Comp.Ai.MarkedForClose)
                    Log.Line($"PlatFormInit and AI MarkedForClose: CubeMarked:{Comp.CoreEntity.MarkedForClose}");
            }

            if (wCounter.Max == 0 || Comp.Ai.Construct.GetPartCount(blockDef) + 1 <= wCounter.Max) {
                wCounter.Current++;
                Ai.Constructs.UpdatePartCounters(Comp.Ai);
                State = PlatformState.Valid;
            }
            else
                return PlatformCrash(Comp, true, false, $"{blockDef.String} over block limits: {wCounter.Current}.");
            
            Parts.Entity = (MyEntity)Comp.Entity;
            Parts.NameToEntity["None"] = Parts.Entity;
            Parts.EntityToName[Parts.Entity] = "None";

            var initState = GetParts();

            Comp.NeedsWorldMatrix = Comp.TypeSpecific == VanillaTurret || Comp.HasAim || Comp.AnimationsModifyCoreParts || Comp.Entity.NeedsWorldMatrix;
            return initState;
        }

        private PlatformState GetParts()
        {
            for (int i = 0; i < Structure.PartHashes.Length; i++)
                _orderToCreate.Add(i);

            if (Structure.PrimaryPart > 0) {
                var tmpPos = _orderToCreate[Structure.PrimaryPart];
                _orderToCreate[tmpPos] = _orderToCreate[0];
                _orderToCreate[0] = tmpPos;
            }

            Parts.CheckSubparts();

            switch (Structure.StructureType)
            {
                case CoreStructure.StructureTypes.Weapon:
                    if (WeaponParts() == PlatformState.Invalid)
                        return State;
                    break;
                case CoreStructure.StructureTypes.Support:
                    if (SupportParts() == PlatformState.Invalid)
                        return State;
                    break;
                case CoreStructure.StructureTypes.Upgrade:
                    if (UpgradeParts() == PlatformState.Invalid)
                        return State;
                    break;
            }

            _orderToCreate.Clear();
            State = PlatformState.Inited;

            return State;
        }

        private PlatformState WeaponParts()
        {
            for (int i = 0; i < _orderToCreate.Count; i++)
            {
                var index = _orderToCreate[i];
                if (Support.Count > 0 || Upgrades.Count > 0)
                    return PlatformCrash(Comp, true, true, $"Your block subTypeId ({Comp.SubtypeName}) mixed functions, cannot mix weapons/upgrades/armorSupport/phantoms, I am crashing now Dave.");

                var partHashId = Structure.PartHashes[index];
                CoreSystem coreSystem;
                if (!Structure.PartSystems.TryGetValue(partHashId, out coreSystem) || !(coreSystem is WeaponSystem))
                    return PlatformCrash(Comp, true, true, $"Your block subTypeId ({Comp.SubtypeName}) Invalid weapon system - id:{partHashId} - isWeapon:{coreSystem is WeaponSystem}, I am crashing now Dave.");

                var system = (WeaponSystem)coreSystem;
                var muzzlePartName = system.MuzzlePartName.String != "Designator" ? system.MuzzlePartName.String : system.ElevationPartName.String;
                var azimuthPartName = Comp.TypeSpecific == VanillaTurret ? string.IsNullOrEmpty(system.AzimuthPartName.String) ? "MissileTurretBase1" : system.AzimuthPartName.String : system.AzimuthPartName.String;
                var elevationPartName = Comp.TypeSpecific == VanillaTurret ? string.IsNullOrEmpty(system.ElevationPartName.String) ? "MissileTurretBarrels" : system.ElevationPartName.String : system.ElevationPartName.String;
                MyEntity muzzlePartEntity;

                if (!Parts.NameToEntity.TryGetValue(muzzlePartName, out muzzlePartEntity))
                {
                    return PlatformCrash(Comp, true, true, $"Your block subTypeId ({Comp.SubtypeName}) Invalid barrelPart, I am crashing now Dave.");
                }

                foreach (var part in Parts.NameToEntity)
                {
                    part.Value.OnClose += Comp.SubpartClosed;
                    break;
                }

                MyEntity azimuthPart;
                if (!Parts.NameToEntity.TryGetValue(azimuthPartName, out azimuthPart))
                    return PlatformCrash(Comp, true, true, $"Your block subTypeId ({Comp.SubtypeName}) Weapon: {system.PartName} Invalid azimuthPart, I am crashing now Dave.");

                MyEntity elevationPart;
                if (!Parts.NameToEntity.TryGetValue(elevationPartName, out elevationPart))
                    return PlatformCrash(Comp, true, true, $"Your block subTypeId ({Comp.SubtypeName}) Invalid elevationPart, I am crashing now Dave.");
                
                MyEntity spinPart = null;
                if (system.HasBarrelRotation)
                {
                    if (!(system.HasSpinPart && Parts.NameToEntity.TryGetValue(system.SpinPartName.String, out spinPart)))
                        spinPart = muzzlePartEntity;
                }


                var weapon = new Weapon(muzzlePartEntity, system, i, (Weapon.WeaponComponent)Comp, Parts, elevationPart, azimuthPart, spinPart, azimuthPartName, elevationPartName);
                if (Comp.TypeSpecific != Phantom) Weapons.Add(weapon);
                else Phantoms.Add(weapon);
                
                SetupWorldMatrix(azimuthPart);
                SetupWorldMatrix(elevationPart);

                CompileTurret(weapon);
            }
            return State;
        }

        private void SetupWorldMatrix(MyEntity part, bool optimizeOnly = false)
        {
            if (part != null && part != Comp.Entity)
            {
                part.RemoveFromGamePruningStructure();
                part.Flags |= EntityFlags.IsNotGamePrunningStructureObject;
                part.Render.NeedsDrawFromParent = true;
                if (!optimizeOnly) 
                    part.NeedsWorldMatrix = true;
            }
        }

        private PlatformState UpgradeParts()
        {
            foreach (var i in _orderToCreate)
            {
                if (Weapons.Count > 0 || Support.Count > 0 || Phantoms.Count > 0)
                    return PlatformCrash(Comp, true, true, $"Your block subTypeId ({Comp.SubtypeName}) mixed functions, cannot mix weapons/upgrades/armorSupport/phantoms, I am crashing now Dave.");

                CoreSystem coreSystem;
                if (Structure.PartSystems.TryGetValue(Structure.PartHashes[i], out coreSystem))
                {
                    Upgrades.Add(new Upgrades((UpgradeSystem)coreSystem, (Upgrade.UpgradeComponent)Comp, i));
                }
                else return PlatformCrash(Comp, true, true, $"Your block subTypeId ({Comp.SubtypeName}) missing part, cannot mix weapons/upgrades/armorSupport/phantoms, I am crashing now Dave.");
            }
            return State;
        }

        private PlatformState SupportParts()
        {
            foreach (var i in _orderToCreate)
            {
                if (Weapons.Count > 0 || Upgrades.Count > 0 || Phantoms.Count > 0)
                    return PlatformCrash(Comp, true, true, $"Your block subTypeId ({Comp.SubtypeName}) mixed functions, cannot mix weapons/upgrades/armorSupport/phantoms, I am crashing now Dave.");

                CoreSystem coreSystem;
                if (Structure.PartSystems.TryGetValue(Structure.PartHashes[i], out coreSystem))
                {
                    Support.Add(new SupportSys((SupportSystem)coreSystem, (SupportSys.SupportComponent)Comp, i));
                }
                else return PlatformCrash(Comp, true, true, $"Your block subTypeId ({Comp.SubtypeName}) missing part, cannot mix weapons/upgrades/armorSupport/phantoms, I am crashing now Dave.");
            }
            return State;
        }

        private void CompileTurret(Weapon weapon)
        {
            MyEntity muzzlePart = null;
            var weaponSystem = weapon.System;
            var mPartName = weaponSystem.MuzzlePartName.String;
            if (Parts.NameToEntity.TryGetValue(mPartName, out muzzlePart) || weaponSystem.DesignatorWeapon)
            {
                SetupWorldMatrix(muzzlePart, true);
                var azimuthPartName = Comp.TypeSpecific == VanillaTurret ? string.IsNullOrEmpty(weaponSystem.AzimuthPartName.String) ? "MissileTurretBase1" : weaponSystem.AzimuthPartName.String : weaponSystem.AzimuthPartName.String;
                var elevationPartName = Comp.TypeSpecific == VanillaTurret ? string.IsNullOrEmpty(weaponSystem.ElevationPartName.String) ? "MissileTurretBarrels" : weaponSystem.ElevationPartName.String : weaponSystem.ElevationPartName.String;
                if (weaponSystem.DesignatorWeapon)
                {
                    muzzlePart = weapon.ElevationPart.Entity;
                    mPartName = elevationPartName;
                }

                weapon.MuzzlePart.Entity = muzzlePart;
                weapon.HeatingParts = new List<MyEntity> { weapon.MuzzlePart.Entity };

                if (mPartName != "None" && muzzlePart != null)
                {
                    var muzzlePartLocation = Comp.Session.GetPartLocation("subpart_" + mPartName, muzzlePart.Parent.Model);

                    var muzzlePartPosTo = MatrixD.CreateTranslation(-muzzlePartLocation);
                    var muzzlePartPosFrom = MatrixD.CreateTranslation(muzzlePartLocation);

                    weapon.MuzzlePart.ToTransformation = muzzlePartPosTo;
                    weapon.MuzzlePart.FromTransformation = muzzlePartPosFrom;
                    weapon.MuzzlePart.PartLocalLocation = muzzlePartLocation;
                    SetupWorldMatrix(weapon.MuzzlePart.Entity, true);
                }

                if (weapon.System.HasBarrelRotation && weapon.SpinPart.Entity != null)
                {

                    if (weapon.SpinPart.Entity == muzzlePart)
                    {
                        weapon.SpinPart.ToTransformation = weapon.MuzzlePart.ToTransformation;
                        weapon.SpinPart.FromTransformation = weapon.MuzzlePart.FromTransformation;
                        weapon.SpinPart.PartLocalLocation = weapon.MuzzlePart.PartLocalLocation;
                    }
                    else
                    {
                        var spinPartLocation = Comp.Session.GetPartLocation("subpart_" + mPartName, weapon.SpinPart.Entity.Parent.Model);

                        var spinPartPosTo = MatrixD.CreateTranslation(-spinPartLocation);
                        var spinPartPosFrom = MatrixD.CreateTranslation(spinPartLocation);
                        weapon.SpinPart.ToTransformation = spinPartPosTo;
                        weapon.SpinPart.FromTransformation = spinPartPosFrom;
                        weapon.SpinPart.PartLocalLocation = spinPartLocation;
                        SetupWorldMatrix(weapon.SpinPart.Entity, true);
                    }
                }


                if (weapon.AiOnlyWeapon)
                {
                    var azimuthPart = weapon.AzimuthPart.Entity;
                    var elevationPart = weapon.ElevationPart.Entity;
                    if (azimuthPart != null && azimuthPartName != "None" && weapon.System.TurretMovement != WeaponSystem.TurretType.ElevationOnly)
                    {

                        var azimuthPartLocation = Comp.Session.GetPartLocation("subpart_" + azimuthPartName, azimuthPart.Parent.Model);
                        var partDummy = Comp.Session.GetPartDummy("subpart_" + azimuthPartName, azimuthPart.Parent.Model);
                        if (partDummy == null)
                        {
                            PlatformCrash(Comp, true, true, $"partDummy null: name:{azimuthPartName} - azimuthPartParentNull:{azimuthPart.Parent == null}, I am crashing now Dave.");
                            return;
                        }

                        var azPartPosTo = MatrixD.CreateTranslation(-azimuthPartLocation);
                        var azPrtPosFrom = MatrixD.CreateTranslation(azimuthPartLocation);

                        var fullStepAzRotation = azPartPosTo * MatrixD.CreateFromAxisAngle(partDummy.Matrix.Up, -weaponSystem.AzStep) * azPrtPosFrom;

                        var rFullStepAzRotation = MatrixD.Invert(fullStepAzRotation);

                        weapon.AzimuthPart.RotationAxis = partDummy.Matrix.Up;

                        weapon.AzimuthPart.ToTransformation = azPartPosTo;
                        weapon.AzimuthPart.FromTransformation = azPrtPosFrom;
                        weapon.AzimuthPart.FullRotationStep = fullStepAzRotation;
                        weapon.AzimuthPart.RevFullRotationStep = rFullStepAzRotation;
                        weapon.AzimuthPart.PartLocalLocation = azimuthPartLocation;
                        weapon.AzimuthPart.OriginalPosition = azimuthPart.PositionComp.LocalMatrixRef;

                    }
                    else
                    {
                        weapon.AzimuthPart.RotationAxis = Vector3.Zero;
                        weapon.AzimuthPart.ToTransformation = MatrixD.Zero;
                        weapon.AzimuthPart.FromTransformation = MatrixD.Zero;
                        weapon.AzimuthPart.FullRotationStep = MatrixD.Zero;
                        weapon.AzimuthPart.RevFullRotationStep = MatrixD.Zero;
                        weapon.AzimuthPart.PartLocalLocation = Vector3.Zero;
                        weapon.AzimuthPart.OriginalPosition = MatrixD.Zero;
                    }

                    if (elevationPart != null && elevationPartName != "None" && weapon.System.TurretMovement != WeaponSystem.TurretType.AzimuthOnly)
                    {
                        var elevationPartLocation = Comp.Session.GetPartLocation("subpart_" + elevationPartName, elevationPart.Parent.Model);
                        var partDummy = Comp.Session.GetPartDummy("subpart_" + elevationPartName, elevationPart.Parent.Model);
                        if (partDummy == null)
                        {
                            PlatformCrash(Comp, true, true, $"partDummy null: name:{elevationPartName} - azimuthPartParentNull:{elevationPart.Parent == null}, I am crashing now Dave.");
                            return;
                        }
                        var elPartPosTo = MatrixD.CreateTranslation(-elevationPartLocation);
                        var elPartPosFrom = MatrixD.CreateTranslation(elevationPartLocation);

                        var fullStepElRotation = elPartPosTo * MatrixD.CreateFromAxisAngle(partDummy.Matrix.Left, weaponSystem.ElStep) * elPartPosFrom;

                        var rFullStepElRotation = MatrixD.Invert(fullStepElRotation);

                        weapon.ElevationPart.RotationAxis = partDummy.Matrix.Left;
                        weapon.ElevationPart.ToTransformation = elPartPosTo;
                        weapon.ElevationPart.FromTransformation = elPartPosFrom;
                        weapon.ElevationPart.FullRotationStep = fullStepElRotation;
                        weapon.ElevationPart.RevFullRotationStep = rFullStepElRotation;
                        weapon.ElevationPart.PartLocalLocation = elevationPartLocation;
                        weapon.ElevationPart.OriginalPosition = elevationPart.PositionComp.LocalMatrixRef;

                    }
                    else if (elevationPartName == "None")
                    {
                        weapon.ElevationPart.RotationAxis = Vector3.Zero;
                        weapon.ElevationPart.ToTransformation = MatrixD.Zero;
                        weapon.ElevationPart.FromTransformation = MatrixD.Zero;
                        weapon.ElevationPart.FullRotationStep = MatrixD.Zero;
                        weapon.ElevationPart.RevFullRotationStep = MatrixD.Zero;
                        weapon.ElevationPart.PartLocalLocation = Vector3.Zero;
                        weapon.ElevationPart.OriginalPosition = MatrixD.Zero;
                    }
                }


                if (mPartName != "Designator" && muzzlePart != null)
                {
                    weapon.MuzzlePart.Entity.PositionComp.OnPositionChanged += weapon.PositionChanged;
                    weapon.MuzzlePart.Entity.OnMarkForClose += weapon.EntPartClose;

                }
                else
                {
                    if (weapon.ElevationPart.Entity != null)
                    {
                        weapon.ElevationPart.Entity.PositionComp.OnPositionChanged += weapon.PositionChanged;
                        weapon.ElevationPart.Entity.OnMarkForClose += weapon.EntPartClose;

                    }
                    else
                    {
                        weapon.AzimuthPart.Entity.PositionComp.OnPositionChanged += weapon.PositionChanged;
                        weapon.AzimuthPart.Entity.OnMarkForClose += weapon.EntPartClose;
                    }
                }
                for (int i = 0; i < weapon.Muzzles.Length; i++)
                {
                    var muzzleName = weaponSystem.Muzzles[i];
                    if (weapon.Muzzles[i] == null)
                    {
                        weapon.Dummies[i] = new Dummy(weapon.MuzzlePart.Entity, weapon, muzzleName);
                        var muzzle = new Weapon.Muzzle(weapon, i, Comp.Session); ;
                        weapon.Muzzles[i] = muzzle;
                        weapon.MuzzleIdToName.Add(i, muzzleName);
                    }
                    else
                        weapon.Dummies[i].Entity = weapon.MuzzlePart.Entity;
                }

                for (int i = 0; i < weaponSystem.HeatingSubparts.Length; i++)
                {
                    var partName = weaponSystem.HeatingSubparts[i];
                    MyEntity ent;
                    if (Parts.NameToEntity.TryGetValue(partName, out ent))
                    {
                        SetupWorldMatrix(ent, true);
                        weapon.HeatingParts.Add(ent);
                        try
                        {
                            ent.SetEmissiveParts("Heating", Color.Transparent, 0);
                        }
                        catch (Exception ex) { Log.Line($"Exception no emmissive Found: {ex}", null, true); }
                    }
                }

                //was run only on weapon first build, needs to run every reset as well
                try
                {
                    foreach (var emissive in weapon.System.PartEmissiveSet)
                    {
                        if (emissive.Value.EmissiveParts == null) continue;

                        foreach (var part in emissive.Value.EmissiveParts)
                        {
                            Parts.SetEmissiveParts(part, Color.Transparent, 0);
                        }
                    }
                }
                catch (Exception e)
                {
                    //cant check for emissives so may be null ref
                }

                if (Comp.IsBlock && weapon.Comp.FunctionalBlock.Enabled)
                    if (weapon.AnimationsSet.ContainsKey(EventTriggers.TurnOn))
                        weapon.Comp.Session.FutureEvents.Schedule(weapon.TurnOnAV, null, 4);
                    else
                    if (weapon.AnimationsSet.ContainsKey(EventTriggers.TurnOff))
                        weapon.Comp.Session.FutureEvents.Schedule(weapon.TurnOffAv, null, 4);

            }

        }

        internal void ResetTurret()
        {
            var registered = false;
            var collection = Comp.TypeSpecific != Phantom ? Comp.Platform.Weapons : Comp.Platform.Phantoms;
            for (int x = 0; x < collection.Count; x++)
            {
                var weapon = collection[x];
                var weaponSystem = weapon.System;
                MyEntity muzzlePart = null;
                var mPartName = weaponSystem.MuzzlePartName.String;

                if (Parts.NameToEntity.TryGetValue(mPartName, out muzzlePart) || weaponSystem.DesignatorWeapon)
                {

                    if (muzzlePart != null)
                        SetupWorldMatrix(muzzlePart, true);

                    if (!registered)
                    {
                        Parts.NameToEntity.FirstPair().Value.OnClose += Comp.SubpartClosed;
                        registered = true;
                    }

                    var azimuthPartName = Comp.TypeSpecific == VanillaTurret ? string.IsNullOrEmpty(weaponSystem.AzimuthPartName.String) ? "MissileTurretBase1" : weaponSystem.AzimuthPartName.String : weaponSystem.AzimuthPartName.String;
                    var elevationPartName = Comp.TypeSpecific == VanillaTurret ? string.IsNullOrEmpty(weaponSystem.ElevationPartName.String) ? "MissileTurretBarrels" : weaponSystem.ElevationPartName.String : weaponSystem.ElevationPartName.String;
                    MyEntity azimuthPartEntity;
                    if (Parts.NameToEntity.TryGetValue(azimuthPartName, out azimuthPartEntity))
                    {
                        weapon.AzimuthPart.Entity = azimuthPartEntity;
                        weapon.AzimuthPart.Parent = azimuthPartEntity.Parent;
                        SetupWorldMatrix(azimuthPartEntity, true);
                    }

                    MyEntity elevationPartEntity;
                    if (Parts.NameToEntity.TryGetValue(elevationPartName, out elevationPartEntity))
                    {
                        weapon.ElevationPart.Entity = elevationPartEntity;
                        SetupWorldMatrix(elevationPartEntity, true);
                    }

                    if (weapon.System.HasBarrelRotation) {

                        MyEntity spinPart = null;
                        if (!(weapon.System.HasSpinPart && Parts.NameToEntity.TryGetValue(weapon.System.SpinPartName.String, out spinPart)))
                            spinPart = muzzlePart;

                        if (spinPart != null) {
                            weapon.SpinPart.Entity = spinPart;
                            SetupWorldMatrix(weapon.SpinPart.Entity, true);
                        }
                    }

                    string ejectorMatch;
                    MyEntity ejectorPart;
                    if (weapon.System.HasEjector && Comp.Platform.Parts.FindFirstDummyByName(weapon.System.Values.Assignments.Ejector, weapon.System.AltEjectorName, out ejectorPart, out ejectorMatch))
                        weapon.Ejector.Entity = ejectorPart;

                    string scopeMatch;
                    MyEntity scopePart;
                    if ((weapon.System.HasScope) && Comp.Platform.Parts.FindFirstDummyByName(weapon.System.Values.Assignments.Scope, weapon.System.AltScopeName, out scopePart, out scopeMatch))
                        weapon.Scope.Entity = scopePart;

                    if (weaponSystem.DesignatorWeapon)
                        muzzlePart = weapon.ElevationPart.Entity;

                    weapon.MuzzlePart.Entity = muzzlePart;

                    weapon.HeatingParts.Clear();
                    weapon.HeatingParts.Add(weapon.MuzzlePart.Entity);

                    foreach (var animationSet in weapon.AnimationsSet)
                    {
                        for (int i = 0; i < animationSet.Value.Length; i++)
                        {
                            var animation = animationSet.Value[i];
                            MyEntity part;
                            if (Parts.NameToEntity.TryGetValue(animation.SubpartId, out part) && !(string.IsNullOrEmpty(animation.SubpartId) || animation.SubpartId == "None"))
                            {
                                SetupWorldMatrix(part, true);
                                animation.Part = part;
                                //if (animation.Running)
                                //  animation.Paused = true;
                                animation.Reset();
                            }
                        }
                    }

                    foreach (var particleEvents in weapon.ParticleEvents)
                    {
                        for (int i = 0; i < particleEvents.Value.Length; i++)
                        {
                            var particle = particleEvents.Value[i];

                            MyEntity part;
                            if (Parts.NameToEntity.TryGetValue(particle.PartName, out part))
                            {
                                SetupWorldMatrix(part, true);
                                particle.MyDummy.Entity = part;
                            }
                        }
                    }

                    if (mPartName != "Designator")
                    {
                        weapon.MuzzlePart.Entity.PositionComp.OnPositionChanged += weapon.PositionChanged;
                        weapon.MuzzlePart.Entity.OnMarkForClose += weapon.EntPartClose;

                    }
                    else
                    {
                        if (weapon.ElevationPart.Entity != null)
                        {
                            weapon.ElevationPart.Entity.PositionComp.OnPositionChanged += weapon.PositionChanged;
                            weapon.ElevationPart.Entity.OnMarkForClose += weapon.EntPartClose;

                        }
                        else
                        {
                            weapon.AzimuthPart.Entity.PositionComp.OnPositionChanged += weapon.PositionChanged;
                            weapon.AzimuthPart.Entity.OnMarkForClose += weapon.EntPartClose;
                        }
                    }

                    for (int i = 0; i < weapon.Muzzles.Length; i++)
                        weapon.Dummies[i].Entity = weapon.MuzzlePart.Entity;


                    for (int i = 0; i < weaponSystem.HeatingSubparts.Length; i++)
                    {
                        var partName = weaponSystem.HeatingSubparts[i];
                        MyEntity ent;
                        if (Parts.NameToEntity.TryGetValue(partName, out ent))
                        {
                            SetupWorldMatrix(ent, true);
                            weapon.HeatingParts.Add(ent);
                            try
                            {
                                ent.SetEmissiveParts("Heating", Color.Transparent, 0);
                            }
                            catch (Exception ex) { Log.Line($"Exception no emmissive Found: {ex}", null, true); }
                        }
                    }

                    //was run only on weapon first build, needs to run every reset as well
                    try
                    {
                        foreach (var emissive in weapon.System.PartEmissiveSet)
                        {
                            if (emissive.Value.EmissiveParts == null) continue;

                            foreach (var part in emissive.Value.EmissiveParts)
                            {
                                Parts.SetEmissiveParts(part, Color.Transparent, 0);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        //cant check for emissives so may be null ref
                    }

                    if (Comp.IsBlock && weapon.Comp.IsWorking)
                        if (weapon.AnimationsSet.ContainsKey(EventTriggers.TurnOn))
                            weapon.Comp.Session.FutureEvents.Schedule(weapon.TurnOnAV, null, 4);
                        else
                        if (weapon.AnimationsSet.ContainsKey(EventTriggers.TurnOff))
                            weapon.Comp.Session.FutureEvents.Schedule(weapon.TurnOffAv, null, 4);
                }
                weapon.UpdatePivotPos();
            }
        }

        internal void ResetParts()
        {
            if (Structure.StructureType != CoreStructure.StructureTypes.Weapon || Comp.TypeSpecific == Phantom)
                return;

            Parts.Clean(Comp.Entity as MyEntity);
            Parts.CheckSubparts();
            
            ResetTurret();

            Comp.LastOnOffState = Comp.FunctionalBlock.Enabled;
            Comp.Status = Started;
        }

        internal void RemoveParts()
        {
            var collection = Comp.TypeSpecific != Phantom ? Comp.Platform.Weapons : Comp.Platform.Phantoms;
            foreach (var w in collection)
            {
                if (w.MuzzlePart.Entity == null) continue;
                w.MuzzlePart.Entity.PositionComp.OnPositionChanged -= w.PositionChanged;
            }
            Parts.Clean(Comp.Entity as MyEntity);
            Comp.Status = Stopped;
        }

        internal void SetupWeaponUi(Weapon w)
        {
            var ui = w.System.Values.HardPoint.Ui;
            w.Comp.HasGuidanceToggle = w.Comp.HasGuidanceToggle || ui.ToggleGuidance;
            //w.BaseComp.HasStrengthSlider = w.BaseComp.HasStrengthSlider || ui.DamageModifier; // this has exploits
            w.Comp.HasRofSlider = w.Comp.HasRofSlider || ui.RateOfFire;
            w.BaseComp.CanOverload = w.BaseComp.CanOverload || ui.EnableOverload ;
            w.BaseComp.HasTurret = w.BaseComp.HasTurret || w.System.Values.HardPoint.Ai.TurretAttached;
            w.BaseComp.HasArming = w.BaseComp.HasArming || w.System.Values.HardPoint.HardWare.CriticalReaction.Enable && w.System.Values.HardPoint.HardWare.CriticalReaction.TerminalControls;
            w.BaseComp.IsBomb = w.System.Values.HardPoint.HardWare.CriticalReaction.Enable && !w.Comp.Platform.Structure.MultiParts;
            w.BaseComp.OverrideLeads = w.BaseComp.OverrideLeads || w.System.Values.HardPoint.Ai.OverrideLeads;
            w.Comp.HasTracking = w.Comp.HasTracking || w.System.Values.HardPoint.Ai.TrackTargets || w.System.Values.HardPoint.Ai.LockOnFocus;
            w.Comp.HasDelayToFire = w.Comp.HasDelayToFire || w.System.DelayToFire > 0;
            w.Comp.ShootSubmerged = w.Comp.ShootSubmerged || w.System.Values.HardPoint.CanShootSubmerged;
            w.BaseComp.HasServerOverrides = w.BaseComp.HasServerOverrides || w.System.WConst.HasServerOverrides;
            if (ui.EnableOverload || ui.RateOfFire || ui.ToggleGuidance) // removed ui.DamageModifier explit
                w.BaseComp.UiEnabled = true;

            if (w.System.HasAmmoSelection)
                w.BaseComp.ConsumableSelectionPartIds.Add(w.PartId);

            foreach (var m in w.System.Values.Assignments.MountPoints) {
                if (m.SubtypeId == Comp.SubTypeId.String && !string.IsNullOrEmpty(m.IconName)) {
                    Comp.CustomIcon = m.IconName;
                }
            }
        }

        internal PlatformState PlatformCrash(CoreComponent comp, bool markInvalid, bool suppress, string message)
        {
            if (suppress)
                comp.Session.SuppressWc = true;
            
            if (markInvalid)
                State = PlatformState.Invalid;
            
            if (Comp.Session.HandlesInput) {
                if (suppress)
                    MyAPIGateway.Utilities.ShowNotification($"CoreSystems hard crashed during block init, shutting down\n Send log files to server admin or submit a bug report to mod author:\n {comp.Platform?.Structure?.ModPath} - {comp.SubtypeName}", 10000);
            }
            Log.Line($"PlatformCrash: {Comp.SubtypeName} - {message}");

            return State;
        }
    }
}
