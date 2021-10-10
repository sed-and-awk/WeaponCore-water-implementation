using CoreSystems.Platform;
using CoreSystems.Support;
using VRageMath;

namespace CoreSystems
{
    public partial class Session
    {
        private void UpdateChargeWeapons() //Fully Inlined due to keen's mod profiler
        {
            foreach (var charger in ChargingParts) {

                var ai = charger.Ai;
                var gridSum = (ai.GridAvailablePower + ai.GridAssignedPower);
                var gridAvail = gridSum < ai.GridMaxPower ? gridSum * 0.94f : ai.GridMaxPower * 0.9f;
                var availMinusDesired = gridAvail - charger.TotalDesired;
                var powerFree = availMinusDesired > 0;
                var rebalance = charger.Rebalance;
                charger.Rebalance = false;

                var group0Count = charger.ChargeGroup0.Count;
                var group1Count = charger.ChargeGroup1.Count;
                var group2Count = charger.ChargeGroup2.Count;
                
                var g0Power = gridAvail * charger.G0Power[charger.State];
                var g1Power = gridAvail * charger.G1Power[charger.State];
                var g2Power = gridAvail * charger.G2Power[charger.State];

                var g0Remaining = MathHelper.Clamp(g0Power - charger.GroupRequested0, 0, g0Power);
                var g1MixedPower = g0Remaining + g1Power >= gridAvail ? g0Remaining : g1Power + g0Remaining;

                var allRemaining = MathHelper.Clamp(gridAvail - (charger.GroupRequested0 + charger.GroupRequested1), 0, gridAvail);
                var g2MixedPower = MathHelper.Clamp(g2Power + allRemaining, 0, gridAvail);

                var group0Budget = group0Count > 0 ? g0Power / group0Count : float.MaxValue;
                var group1Budget = group1Count > 0 ? g1MixedPower / group1Count : float.MaxValue;
                var group2Budget = group2Count > 0 ? g2MixedPower / group2Count : float.MaxValue;

                //if (Tick180)
                    //Log.Line($"[charging] [fullPower:{powerFree} - [avail:{gridAvail}({g0Power}) - desired:{charger.TotalDesired}]] - g0:{group0Budget}({group0Count}) - g1:{group1Budget}({group1Count}) - g2:{group2Budget}({group2Count})");

                for (int i = group0Count - 1; i >= 0; i--)
                {
                    var part = charger.ChargeGroup0[i];

                    var assignedPower = powerFree ? part.DesiredPower : group0Budget;

                    switch (part.BaseComp.Type)
                    {
                        case CoreComponent.CompType.Upgrade:
                            break;
                        case CoreComponent.CompType.Support:
                            break;
                        case CoreComponent.CompType.Weapon:
                            if (WeaponCharged(ai, (Weapon)part, assignedPower, rebalance)) 
                                charger.Remove(part, i);
                            break;
                    }
                }


                for (int i = group1Count - 1; i >= 0; i--)
                {
                    var part = charger.ChargeGroup1[i];

                    var assignedPower = powerFree ? part.DesiredPower : group1Budget;

                    switch (part.BaseComp.Type)
                    {
                        case CoreComponent.CompType.Upgrade:
                            break;
                        case CoreComponent.CompType.Support:
                            break;
                        case CoreComponent.CompType.Weapon:
                            if (WeaponCharged(ai, (Weapon)part,assignedPower, rebalance))
                                charger.Remove(part, i);
                            break;
                    }
                }


                for (int i = group2Count - 1; i >= 0; i--)
                {
                    var part = charger.ChargeGroup2[i];

                    var assignedPower = powerFree ? part.DesiredPower : group2Budget;

                    switch (part.BaseComp.Type)
                    {
                        case CoreComponent.CompType.Upgrade:
                            break;
                        case CoreComponent.CompType.Support:
                            break;
                        case CoreComponent.CompType.Weapon:
                            if (WeaponCharged(ai, (Weapon)part, assignedPower, rebalance))
                                charger.Remove(part, i);
                            break;
                    }

                }
            }
            ChargingParts.ApplyRemovals();
        }

        private bool WeaponCharged(Ai ai, Weapon w, float assignedPower, bool rebalance = false)
        {
            var comp = w.Comp;

            if (!w.BaseComp.UnlimitedPower) {

                if (!w.Charging)
                    w.DrawPower(assignedPower, ai);
                else if (w.NewPowerNeeds || rebalance)
                    w.AdjustPower(assignedPower, ai);
            }


            w.ProtoWeaponAmmo.CurrentCharge = MathHelper.Clamp(w.ProtoWeaponAmmo.CurrentCharge + w.AssignedPower, 0, w.MaxCharge);

            if (!w.ActiveAmmoDef.AmmoDef.Const.Reloadable && w.IsShooting)
                return false;
            //if (Tick180)
                //Log.Line($"[{w.System.PartName}] [current:{w.ProtoWeaponAmmo.CurrentCharge} >= target:{w.MaxCharge}]] - CurrentAmmo:{w.ProtoWeaponAmmo.CurrentAmmo} == MaxAmmo:{w.ActiveAmmoDef.AmmoDef.Const.MagazineSize} - ReloadTime:{w.System.ReloadTime} - StayCharged:{w.StayCharged}");

            var complete = IsServer && w.ProtoWeaponAmmo.CurrentCharge >= w.MaxCharge * comp.Data.Repo.Values.Set.DpsModifier || IsClient && w.Reload.EndId > w.ClientEndId || w.ExitCharger;
            var weaponFailure = !ai.HasPower || !comp.IsWorking;
            var invalidStates = ai != comp.Ai || comp.Ai.MarkedForClose || comp.Ai.TopEntity.MarkedForClose || comp.Ai.Concealed || comp.CoreEntity.MarkedForClose || comp.Platform.State != CorePlatform.PlatformState.Ready;
            
            if (complete || weaponFailure || invalidStates) {
                var serverFullyLoaded = IsServer && w.ProtoWeaponAmmo.CurrentAmmo >= w.ActiveAmmoDef.AmmoDef.Const.MagazineSize;
                var clientReadyToLoad = IsClient && w.Reload.EndId > w.ClientEndId;
                var fullyCharged = serverFullyLoaded || clientReadyToLoad;

                if (complete && (!serverFullyLoaded || clientReadyToLoad) && w.Loading)
                    w.Reloaded(IsClient ? 2 : 0);

                if (!complete || fullyCharged) {
                    w.StopPowerDraw(weaponFailure || invalidStates, ai);
                    return true;
                }
                w.Loading = true;
            }

            if (Tick60) {

                if (w.EstimatedCharge + w.AssignedPower < w.MaxCharge)
                    w.EstimatedCharge += w.AssignedPower;
                else
                    w.EstimatedCharge = w.MaxCharge;
            }
            return false;
        }
    }
}
