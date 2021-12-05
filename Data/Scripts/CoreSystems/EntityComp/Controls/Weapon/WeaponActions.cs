﻿using System;
using System.Text;
using CoreSystems.Platform;
using CoreSystems.Support;
using Sandbox.ModAPI;
using VRageMath;
using static CoreSystems.Support.CoreComponent.TriggerActions;

namespace CoreSystems.Control
{
    public static partial class CustomActions
    {
        #region Call Actions
        internal static void TerminalActionShootClick(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;

            comp.RequestShootUpdate(TriggerClick, comp.Session.MpServer ? comp.Session.PlayerId : -1);
        }

        internal static void TerminActionToggleShoot(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            comp.RequestShootUpdate(TriggerOn, comp.Session.MpServer ? comp.Session.PlayerId : -1);
        }

        internal static void TerminalActionShootOn(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            comp.RequestShootUpdate(TriggerOn, comp.Session.MpServer ? comp.Session.PlayerId : -1);
        }

        internal static void TerminalActionShootOff(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            comp.RequestShootUpdate(TriggerOff, comp.Session.MpServer ? comp.Session.PlayerId : -1);
        }

        internal static void TerminalActionShootOnce(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;

            comp.RequestShootUpdate(TriggerOnce, comp.Session.MpServer ? comp.Session.PlayerId : -1);
        }

        internal static void TerminalActionControlMode(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;
            
            var numValue = (int)comp.Data.Repo.Values.Set.Overrides.Control;
            var value = numValue + 1 <= 2 ? numValue + 1 : 0;

            Weapon.WeaponComponent.RequestSetValue(comp, "ControlModes", value, comp.Session.PlayerId);
        }

        internal static void TerminalActionMovementMode(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var numValue = (int)comp.Data.Repo.Values.Set.Overrides.MoveMode;
            var value = numValue + 1 <= 3 ? numValue + 1 : 0;

            Weapon.WeaponComponent.RequestSetValue(comp, "MovementModes", value, comp.Session.PlayerId);
        }

        internal static void TerminActionCycleSubSystem(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var numValue = (int)comp.Data.Repo.Values.Set.Overrides.SubSystem;
            var value = numValue + 1 <= 7 ? numValue + 1 : 0;

            Weapon.WeaponComponent.RequestSetValue(comp, "SubSystems", value, comp.Session.PlayerId);
        }

        internal static void TerminalActionToggleNeutrals(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var newBool = !comp.Data.Repo.Values.Set.Overrides.Neutrals;
            var newValue = newBool ? 1 : 0;

            Weapon.WeaponComponent.RequestSetValue(comp, "Neutrals", newValue, comp.Session.PlayerId);
        }

        internal static void TerminalActionToggleProjectiles(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var newBool = !comp.Data.Repo.Values.Set.Overrides.Projectiles;
            var newValue = newBool ? 1 : 0;

            Weapon.WeaponComponent.RequestSetValue(comp, "Projectiles", newValue, comp.Session.PlayerId);
        }

        internal static void TerminalActionToggleBiologicals(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var newBool = !comp.Data.Repo.Values.Set.Overrides.Biologicals;
            var newValue = newBool ? 1 : 0;

            Weapon.WeaponComponent.RequestSetValue(comp, "Biologicals", newValue, comp.Session.PlayerId);
        }

        internal static void TerminalActionToggleMeteors(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var newBool = !comp.Data.Repo.Values.Set.Overrides.Meteors;
            var newValue = newBool ? 1 : 0;

            Weapon.WeaponComponent.RequestSetValue(comp, "Meteors", newValue, comp.Session.PlayerId);
        }

        internal static void TerminalActionToggleGrids(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var newBool = !comp.Data.Repo.Values.Set.Overrides.Grids;
            var newValue = newBool ? 1 : 0;

            Weapon.WeaponComponent.RequestSetValue(comp, "Grids", newValue, comp.Session.PlayerId);
        }

        internal static void TerminalActionToggleFriendly(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var newBool = !comp.Data.Repo.Values.Set.Overrides.Friendly;
            var newValue = newBool ? 1 : 0;

            Weapon.WeaponComponent.RequestSetValue(comp, "Friendly", newValue, comp.Session.PlayerId);
        }

        internal static void TerminalActionToggleUnowned(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var newBool = !comp.Data.Repo.Values.Set.Overrides.Unowned;
            var newValue = newBool ? 1 : 0;

            Weapon.WeaponComponent.RequestSetValue(comp, "Unowned", newValue, comp.Session.PlayerId);
        }

        internal static void TerminalActionToggleFocusTargets(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var newBool = !comp.Data.Repo.Values.Set.Overrides.FocusTargets;
            var newValue = newBool ? 1 : 0;

            Weapon.WeaponComponent.RequestSetValue(comp, "FocusTargets", newValue, comp.Session.PlayerId);
        }

        internal static void TerminalActionToggleFocusSubSystem(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var newBool = !comp.Data.Repo.Values.Set.Overrides.FocusSubSystem;
            var newValue = newBool ? 1 : 0;

            Weapon.WeaponComponent.RequestSetValue(comp, "FocusSubSystem", newValue, comp.Session.PlayerId);
        }

        internal static void TerminalActionMaxSizeIncrease(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var nextValue = comp.Data.Repo.Values.Set.Overrides.MaxSize * 2;
            var newValue = nextValue > 0 && nextValue < 16384 ? nextValue : 16384;

            Weapon.WeaponComponent.RequestSetValue(comp, "MaxSize", newValue, comp.Session.PlayerId);
        }

        internal static void TerminalActionMaxSizeDecrease(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var nextValue = comp.Data.Repo.Values.Set.Overrides.MaxSize / 2;
            var newValue = nextValue > 0 && nextValue < 16384 ? nextValue : 1;

            Weapon.WeaponComponent.RequestSetValue(comp, "MaxSize", newValue, comp.Session.PlayerId);
        }

        internal static void TerminalActionMinSizeIncrease(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var nextValue = comp.Data.Repo.Values.Set.Overrides.MinSize == 0 ? 1 : comp.Data.Repo.Values.Set.Overrides.MinSize * 2;
            var newValue = nextValue > 0 && nextValue < 128 ? nextValue : 128;

            Weapon.WeaponComponent.RequestSetValue(comp, "MinSize", newValue, comp.Session.PlayerId);
        }

        internal static void TerminalActionMinSizeDecrease(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var nextValue = comp.Data.Repo.Values.Set.Overrides.MinSize / 2;
            var newValue = nextValue > 0 && nextValue < 128 ? nextValue : 0;

            Weapon.WeaponComponent.RequestSetValue(comp, "MinSize", newValue, comp.Session.PlayerId);
        }

        internal static void TerminalActionCycleAmmo(IMyTerminalBlock block)
        {
            var comp = block?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp?.Platform.State != CorePlatform.PlatformState.Ready) return;
            for (int i = 0; i < comp.Collection.Count; i++)
            {
                var w = comp.Collection[i];

                if (!w.System.HasAmmoSelection)
                    continue;

                var availAmmo = w.System.AmmoTypes.Length;
                var aId = w.DelayedCycleId >= 0 ? w.DelayedCycleId : w.Reload.AmmoTypeId;
                var currActive = w.System.AmmoTypes[aId];
                var next = (aId + 1) % availAmmo;
                var currDef = w.System.AmmoTypes[next];

                var change = false;

                while (!(currActive.Equals(currDef)))
                {
                    if (currDef.AmmoDef.Const.IsTurretSelectable)
                    {
                        change = true;
                        break;
                    }

                    next = (next + 1) % availAmmo;
                    currDef = w.System.AmmoTypes[next];
                }

                if (change)
                {
                    w.QueueAmmoChange(next);
                }
            }
        }

        internal static void TerminActionCycleDecoy(IMyTerminalBlock blk)
        {
            long valueLong;
            long.TryParse(blk.CustomData, out valueLong);
            var value = valueLong + 1 <= 7 ? valueLong + 1 : 1;
            blk.CustomData = value.ToString();
            blk.RefreshCustomInfo();
        }
        internal static void TerminalActionToggleRepelMode(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent; ;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var newBool = !comp.Data.Repo.Values.Set.Overrides.Repel;
            var newValue = newBool ? 1 : 0;

            Weapon.WeaponComponent.RequestSetValue(comp, "Repel", newValue, comp.Session.PlayerId);
        }

        internal static void TerminalActionCameraChannelIncrease(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent; ;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var value = Convert.ToInt32(comp.Data.Repo.Values.Set.Overrides.CameraChannel);
            var nextValue = MathHelper.Clamp(value + 1, 0, 24);

            Weapon.WeaponComponent.RequestSetValue(comp, "CameraChannel", nextValue, comp.Session.PlayerId);
        }

        internal static void TerminalActionCameraChannelDecrease(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent; ;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var value = Convert.ToInt32(comp.Data.Repo.Values.Set.Overrides.CameraChannel);
            var nextValue = MathHelper.Clamp(value - 1, 0, 24);

            Weapon.WeaponComponent.RequestSetValue(comp, "CameraChannel", nextValue, comp.Session.PlayerId);
        }

        internal static void TerminalActionLeadGroupIncrease(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent; ;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var value = Convert.ToInt32(comp.Data.Repo.Values.Set.Overrides.LeadGroup);
            var nextValue = MathHelper.Clamp(value + 1, 0, 5);

            Weapon.WeaponComponent.RequestSetValue(comp, "LeadGroup", nextValue, comp.Session.PlayerId);
        }

        internal static void TerminalActionLeadGroupDecrease(IMyTerminalBlock blk)
        {
            var comp = blk?.Components?.Get<CoreComponent>() as Weapon.WeaponComponent; ;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready)
                return;

            var value = Convert.ToInt32(comp.Data.Repo.Values.Set.Overrides.LeadGroup);
            var nextValue = MathHelper.Clamp(value - 1, 0, 5);

            Weapon.WeaponComponent.RequestSetValue(comp, "LeadGroup", nextValue, comp.Session.PlayerId);
        }
        internal static void TerminalActionCameraIncrease(IMyTerminalBlock blk)
        {
            long valueLong;
            long.TryParse(blk.CustomData, out valueLong);
            var value = valueLong + 1 <= 7 ? valueLong + 1 : 1;
            blk.CustomData = value.ToString();
            blk.RefreshCustomInfo();
        }

        internal static void TerminalActionCameraDecrease(IMyTerminalBlock blk)
        {
            long valueLong;
            long.TryParse(blk.CustomData, out valueLong);
            var value = valueLong + 1 <= 7 ? valueLong + 1 : 1;
            blk.CustomData = value.ToString();
            blk.RefreshCustomInfo();
        }
        #endregion

        #region Writters
        internal static void ClickShootWriter(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as Weapon.WeaponComponent;

            var on = comp != null && comp.Data.Repo?.Values.State.TerminalAction == TriggerClick;

            if (on)
                sb.Append(Localization.GetText("ActionStateOn"));
            else
                sb.Append(Localization.GetText("ActionStateOff"));
        }

        internal static void ShootStateWriter(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;
            if (comp.Data.Repo.Values.State.TerminalAction == TriggerOn)
                sb.Append(Localization.GetText("ActionStateOn"));
            else
                sb.Append(Localization.GetText("ActionStateOff"));
        }

        internal static void NeutralWriter(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;
            if (comp.Data.Repo.Values.Set.Overrides.Neutrals)
                sb.Append(Localization.GetText("ActionStateOn"));
            else
                sb.Append(Localization.GetText("ActionStateOff"));
        }

        internal static void ProjectilesWriter(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;
            if (comp.Data.Repo.Values.Set.Overrides.Projectiles)
                sb.Append(Localization.GetText("ActionStateOn"));
            else
                sb.Append(Localization.GetText("ActionStateOff"));
        }

        internal static void BiologicalsWriter(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;
            if (comp.Data.Repo.Values.Set.Overrides.Biologicals)
                sb.Append(Localization.GetText("ActionStateOn"));
            else
                sb.Append(Localization.GetText("ActionStateOff"));
        }

        internal static void MeteorsWriter(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;
            if (comp.Data.Repo.Values.Set.Overrides.Meteors)
                sb.Append(Localization.GetText("ActionStateOn"));
            else
                sb.Append(Localization.GetText("ActionStateOff"));
        }

        internal static void GridsWriter(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;
            if (comp.Data.Repo.Values.Set.Overrides.Grids)
                sb.Append(Localization.GetText("ActionStateOn"));
            else
                sb.Append(Localization.GetText("ActionStateOff"));
        }

        internal static void FriendlyWriter(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;
            if (comp.Data.Repo.Values.Set.Overrides.Friendly)
                sb.Append(Localization.GetText("ActionStateOn"));
            else
                sb.Append(Localization.GetText("ActionStateOff"));
        }

        internal static void UnownedWriter(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;
            if (comp.Data.Repo.Values.Set.Overrides.Unowned)
                sb.Append(Localization.GetText("ActionStateOn"));
            else
                sb.Append(Localization.GetText("ActionStateOff"));
        }

        internal static void FocusTargetsWriter(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;
            if (comp.Data.Repo.Values.Set.Overrides.FocusTargets)
                sb.Append(Localization.GetText("ActionStateOn"));
            else
                sb.Append(Localization.GetText("ActionStateOff"));
        }

        internal static void FocusSubSystemWriter(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;
            if (comp.Data.Repo.Values.Set.Overrides.FocusSubSystem)
                sb.Append(Localization.GetText("ActionStateOn"));
            else
                sb.Append(Localization.GetText("ActionStateOff"));
        }

        internal static void MaxSizeWriter(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;
            sb.Append(comp.Data.Repo.Values.Set.Overrides.MaxSize);
        }

        internal static void MinSizeWriter(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;
            sb.Append(comp.Data.Repo.Values.Set.Overrides.MinSize);
        }

        internal static void ControlStateWriter(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;
            sb.Append(comp.Data.Repo.Values.Set.Overrides.Control);
        }

        internal static void MovementModeWriter(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;

            sb.Append(comp.Data.Repo.Values.Set.Overrides.MoveMode);
        }

        internal static void SubSystemWriter(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;

            sb.Append(comp.Data.Repo.Values.Set.Overrides.SubSystem);
        }

        internal static void DecoyWriter(IMyTerminalBlock blk, StringBuilder sb)
        {
            long value;
            if (long.TryParse(blk.CustomData, out value))
            {
                sb.Append(((WeaponDefinition.TargetingDef.BlockTypes)value).ToString());
            }
        }

        internal static void CameraWriter(IMyTerminalBlock blk, StringBuilder sb)
        {
            long value;
            if (long.TryParse(blk.CustomData, out value))
            {
                var group = $"Camera Channel {value}";
                sb.Append(group);
            }
        }

        internal static void WeaponCameraChannelWriter(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;

            sb.Append(comp.Data.Repo.Values.Set.Overrides.CameraChannel);
        }

        internal static void LeadGroupWriter(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;

            sb.Append(comp.Data.Repo.Values.Set.Overrides.LeadGroup);
        }

        internal static void AmmoSelectionWriter(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready || comp.ConsumableSelectionPartIds.Count == 0) return;
            var w = comp.Collection[comp.ConsumableSelectionPartIds[0]];
            sb.Append(w.AmmoName);
        }

        internal static void RepelWriter(IMyTerminalBlock blk, StringBuilder sb)
        {
            var comp = blk.Components.Get<CoreComponent>() as Weapon.WeaponComponent;
            if (comp == null || comp.Platform.State != CorePlatform.PlatformState.Ready) return;
            if (comp.Data.Repo.Values.Set.Overrides.Repel)
                sb.Append(Localization.GetText("ActionStateOn"));
            else
                sb.Append(Localization.GetText("ActionStateOff"));
        }
        #endregion
    }
}
