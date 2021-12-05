﻿using CoreSystems.Platform;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace WeaponCore.Data.Scripts.CoreSystems.Ui.Hud
{
    partial class Hud
    {
        internal void DrawTextures()
        {
            var ticksSinceUpdate = TicksSinceUpdated;
            var reset = false;
            _cameraWorldMatrix = _session.Camera.WorldMatrix;

            if (NeedsUpdate)
                UpdateHudSettings();

            if (WeaponsToDisplay.Count > 0 || KeepBackground)
            {

                if (ticksSinceUpdate >= MinUpdateTicks)
                {

                    if (TexturesToAdd > 0)
                        _weapontoDraw = SortDisplayedWeapons(WeaponsToDisplay);

                    _lastHudUpdateTick = _session.Tick;
                }
                else if (ticksSinceUpdate + 1 >= MinUpdateTicks)
                    reset = true;

                BuildHud(reset);
            }

            AddTextAndTextures();
            DrawHudOnce();


            WeaponsToDisplay.Clear();
            _textAddList.Clear();
            _textureAddList.Clear();
            _drawList.Clear();
            TexturesToAdd = 0;
        }

        private void BuildHud(bool reset)
        {
            var currWeaponDisplayPos = _currWeaponDisplayPos;

            if (_lastHudUpdateTick == _session.Tick)
            {

                var largestName = (_currentLargestName * (_textWidth)) + _stackPadding;

                _bgWidth = largestName > _symbolWidth ? largestName : _symbolWidth;
                _bgBorderHeight = _bgWidth * BgBorderRatio;
                _bgCenterHeight = _weapontoDraw.Count * _infoPaneloffset;
            }

            var bgStartPosX = currWeaponDisplayPos.X - _bgWidth - _padding;

            BackgroundAdd(currWeaponDisplayPos, bgStartPosX);

            if (reset)
                _currentLargestName = 0;

            WeaponsToAdd(reset, currWeaponDisplayPos, bgStartPosX);

            if (reset)
            {
                _weapontoDraw.Clear();
                _weaponInfoListPool.Enqueue(_weapontoDraw);
            }
        }

        private void DrawHudOnce()
        {
            foreach (var textureToDraw in _drawList)
            {

                if (textureToDraw.UvDraw)
                {

                    MyQuadD quad;
                    MyUtils.GetBillboardQuadOriented(out quad, ref textureToDraw.Position, textureToDraw.Width, textureToDraw.Height, ref textureToDraw.Left, ref textureToDraw.Up);
                    if (textureToDraw.Color != Vector4.Zero)
                    {
                        MyTransparentGeometry.AddTriangleBillboard(quad.Point0, quad.Point1, quad.Point2, Vector3.Zero, Vector3.Zero, Vector3.Zero, textureToDraw.P0, textureToDraw.P1, textureToDraw.P3, textureToDraw.Material, 0, textureToDraw.Position, textureToDraw.Color, textureToDraw.Blend);
                        MyTransparentGeometry.AddTriangleBillboard(quad.Point0, quad.Point3, quad.Point2, Vector3.Zero, Vector3.Zero, Vector3.Zero, textureToDraw.P0, textureToDraw.P2, textureToDraw.P3, textureToDraw.Material, 0, textureToDraw.Position, textureToDraw.Color, textureToDraw.Blend);
                    }
                    else
                    {
                        MyTransparentGeometry.AddTriangleBillboard(quad.Point0, quad.Point1, quad.Point2, Vector3.Zero, Vector3.Zero, Vector3.Zero, textureToDraw.P0, textureToDraw.P1, textureToDraw.P3, textureToDraw.Material, 0, textureToDraw.Position, textureToDraw.Blend);
                        MyTransparentGeometry.AddTriangleBillboard(quad.Point0, quad.Point3, quad.Point2, Vector3.Zero, Vector3.Zero, Vector3.Zero, textureToDraw.P0, textureToDraw.P2, textureToDraw.P3, textureToDraw.Material, 0, textureToDraw.Position, textureToDraw.Blend);
                    }
                }
                else
                {
                    textureToDraw.Position = Vector3D.Transform(textureToDraw.Position, _cameraWorldMatrix);
                    MyTransparentGeometry.AddBillboardOriented(textureToDraw.Material, textureToDraw.Color, textureToDraw.Position, _cameraWorldMatrix.Left, _cameraWorldMatrix.Up, textureToDraw.Height, textureToDraw.Blend);
                }

                if (!textureToDraw.Persistant)
                {
                    _textureDrawPool.Enqueue(textureToDraw);
                }
            }
        }

        private void AddTextAndTextures()
        {
            for (int i = 0; i < _textAddList.Count; i++)
            {

                var textAdd = _textAddList[i];

                var height = textAdd.FontSize * ShadowHeightScaler;
                var width = textAdd.FontSize * _session.AspectRatioInv;
                textAdd.Position.Z = _viewPortSize.Z;
                var textPos = Vector3D.Transform(textAdd.Position, _cameraWorldMatrix);

                for (int j = 0; j < textAdd.Text.Length; j++)
                {

                    var c = textAdd.Text[j];

                    var cm = CharacterMap[textAdd.Font][c];

                    TextureDrawData tdd;

                    if (!_textureDrawPool.TryDequeue(out tdd))
                        tdd = new TextureDrawData();

                    tdd.Material = cm.Material;
                    tdd.Color = textAdd.Color;
                    tdd.Position = textPos;
                    tdd.Up = _cameraWorldMatrix.Up;
                    tdd.Left = _cameraWorldMatrix.Left;
                    tdd.Width = width;
                    tdd.Height = height;
                    tdd.P0 = cm.P0;
                    tdd.P1 = cm.P1;
                    tdd.P2 = cm.P2;
                    tdd.P3 = cm.P3;
                    tdd.UvDraw = true;

                    _drawList.Add(tdd);

                    textPos -= (_cameraWorldMatrix.Left * (width * 0.6f) * ShadowSizeScaler);
                }

                _textDrawPool.Enqueue(textAdd);
            }

            for (int i = 0; i < _textureAddList.Count; i++)
            {

                var tdd = _textureAddList[i];
                tdd.Position.Z = _viewPortSize.Z;
                tdd.Position = Vector3D.Transform(tdd.Position, _cameraWorldMatrix);
                tdd.Up = _cameraWorldMatrix.Up;
                tdd.Left = _cameraWorldMatrix.Left;
                _drawList.Add(tdd);
            }
        }

        private void BackgroundAdd(Vector2D currWeaponDisplayPos, double bgStartPosX)
        {
            var bgStartPosY = currWeaponDisplayPos.Y - _bgCenterHeight;
            TextureDrawData backgroundTexture;
            if (!_textureDrawPool.TryDequeue(out backgroundTexture))
                backgroundTexture = new TextureDrawData();

            backgroundTexture.Material = InfoBackground[1].Material;
            backgroundTexture.Color = _bgColor;
            backgroundTexture.Position.X = bgStartPosX;
            backgroundTexture.Position.Y = bgStartPosY;
            backgroundTexture.Width = _bgWidth;
            backgroundTexture.Height = _bgCenterHeight;
            backgroundTexture.P0 = InfoBackground[1].P0;
            backgroundTexture.P1 = InfoBackground[1].P1;
            backgroundTexture.P2 = InfoBackground[1].P2;
            backgroundTexture.P3 = InfoBackground[1].P3;
            backgroundTexture.UvDraw = true;

            _textureAddList.Add(backgroundTexture);

            if (!_textureDrawPool.TryDequeue(out backgroundTexture))
                backgroundTexture = new TextureDrawData();

            backgroundTexture.Material = InfoBackground[0].Material;
            backgroundTexture.Color = _bgColor;
            backgroundTexture.Position.X = bgStartPosX;
            backgroundTexture.Position.Y = bgStartPosY + _bgBorderHeight + _bgCenterHeight;
            backgroundTexture.Width = _bgWidth;
            backgroundTexture.Height = _bgBorderHeight;
            backgroundTexture.P0 = InfoBackground[0].P0;
            backgroundTexture.P1 = InfoBackground[0].P1;
            backgroundTexture.P2 = InfoBackground[0].P2;
            backgroundTexture.P3 = InfoBackground[0].P3;
            backgroundTexture.UvDraw = true;

            _textureAddList.Add(backgroundTexture);

            if (!_textureDrawPool.TryDequeue(out backgroundTexture))
                backgroundTexture = new TextureDrawData();

            backgroundTexture.Material = InfoBackground[2].Material;
            backgroundTexture.Color = _bgColor;
            backgroundTexture.Position.X = bgStartPosX;
            backgroundTexture.Position.Y = bgStartPosY - (_bgBorderHeight + _bgCenterHeight);
            backgroundTexture.Width = _bgWidth;
            backgroundTexture.Height = _bgBorderHeight;
            backgroundTexture.P0 = InfoBackground[2].P0;
            backgroundTexture.P1 = InfoBackground[2].P1;
            backgroundTexture.P2 = InfoBackground[2].P2;
            backgroundTexture.P3 = InfoBackground[2].P3;
            backgroundTexture.UvDraw = true;

            _textureAddList.Add(backgroundTexture);
        }

        private void WeaponsToAdd(bool reset, Vector2D currWeaponDisplayPos, double bgStartPosX)
        {
            for (int i = 0; i < _weapontoDraw.Count; i++)
            {

                TextDrawRequest textInfo;
                var stackedInfo = _weapontoDraw[i];
                var weapon = stackedInfo.HighestValueWeapon;
                
                
                var currLock = _session.TrackingAi.Construct.Data.Repo.FocusData.Locked[0].ToString();
                var needsLock = weapon.System.LockOnFocus && currLock == "None" ? ": " + _session.UiInput.ActionKey.ToString() : ": Locked";
                var name = weapon.System.PartName + (weapon.System.LockOnFocus ? needsLock : "");

                var textOffset = bgStartPosX - _bgWidth + _reloadWidth + _padding;
                var hasHeat = weapon.HeatPerc > 0;
                var showReloadIcon = (weapon.Loading || weapon.Reload.WaitForClient || _session.Tick - weapon.LastLoadedTick < 60);

                if (!_textDrawPool.TryDequeue(out textInfo))
                    textInfo = new TextDrawRequest();

                textInfo.Text = name;
                var color = new Vector4(1, 1, 1, 1);
                textInfo.Color = color;
                textInfo.Position.X = textOffset;
                textInfo.Position.Y = currWeaponDisplayPos.Y;
                textInfo.FontSize = _textSize;
                textInfo.Font = _hudFont;
                _textAddList.Add(textInfo);


                if (stackedInfo.WeaponStack > 1)
                {

                    if (!_textDrawPool.TryDequeue(out textInfo))
                        textInfo = new TextDrawRequest();

                    textInfo.Text = $"(x{stackedInfo.WeaponStack})";
                    textInfo.Color = new Vector4(0.5f, 0.5f, 1, 1);
                    textInfo.Position.X = textOffset + (name.Length * ((_textSize * _session.AspectRatioInv) * 0.6f) * ShadowSizeScaler);

                    textInfo.Position.Y = currWeaponDisplayPos.Y;
                    textInfo.FontSize = _sTextSize;
                    textInfo.Font = FontType.Shadow;
                    _textAddList.Add(textInfo);
                }

                if (hasHeat)
                    HasHeat(weapon, stackedInfo, ref currWeaponDisplayPos, reset);

                if (showReloadIcon)
                    ShowReloadIcon(weapon, stackedInfo, ref currWeaponDisplayPos, textOffset, reset);

                currWeaponDisplayPos.Y -= _infoPaneloffset + (_padding * .6f);

                if (reset)
                    _weaponStackedInfoPool.Enqueue(stackedInfo);
            }

        }

        private void HasHeat(Weapon weapon, StackedWeaponInfo stackedInfo, ref Vector2D currWeaponDisplayPos, bool reset)
        {
            int heatBarIndex;
            if (weapon.PartState.Overheated)
            {
                var index = _session.SCount < 30 ? 1 : 2;
                heatBarIndex = HeatBarTexture.Length - 2;
            }
            else
                heatBarIndex = (int)MathHelper.Clamp(weapon.HeatPerc * 10, 0, HeatBarTexture.Length - 1);

            stackedInfo.CachedHeatTexture.Material = HeatBarTexture[heatBarIndex].Material;
            stackedInfo.CachedHeatTexture.Color = Vector4.Zero;
            stackedInfo.CachedHeatTexture.Position.X = currWeaponDisplayPos.X - _heatOffsetX;
            stackedInfo.CachedHeatTexture.Position.Y = currWeaponDisplayPos.Y - _heatOffsetY;
            stackedInfo.CachedHeatTexture.Width = _heatWidth;
            stackedInfo.CachedHeatTexture.Height = _heatHeight;
            stackedInfo.CachedHeatTexture.P0 = HeatBarTexture[heatBarIndex].P0;
            stackedInfo.CachedHeatTexture.P1 = HeatBarTexture[heatBarIndex].P1;
            stackedInfo.CachedHeatTexture.P2 = HeatBarTexture[heatBarIndex].P2;
            stackedInfo.CachedHeatTexture.P3 = HeatBarTexture[heatBarIndex].P3;

            if (reset)
                stackedInfo.CachedHeatTexture.Persistant = false;

            _textureAddList.Add(stackedInfo.CachedHeatTexture);
        }

        private void ShowReloadIcon(Weapon weapon, StackedWeaponInfo stackedInfo, ref Vector2D currWeaponDisplayPos, double textOffset, bool reset)
        {
            var mustCharge = weapon.ActiveAmmoDef.AmmoDef.Const.MustCharge;
            var texture = mustCharge ? ChargingTexture : ReloadingTexture;
            if (texture.Length > 0)
            {

                if (mustCharge)
                    stackedInfo.ReloadIndex = MathHelper.Clamp((int)(MathHelper.Lerp(0, texture.Length - 1, weapon.ProtoWeaponAmmo.CurrentCharge / weapon.MaxCharge)), 0, texture.Length - 1);

                stackedInfo.CachedReloadTexture.Material = texture[stackedInfo.ReloadIndex].Material;
                stackedInfo.CachedReloadTexture.Color = _bgColor;
                stackedInfo.CachedReloadTexture.Position.X = (textOffset - _paddingReload) - _reloadOffset;
                stackedInfo.CachedReloadTexture.Position.Y = currWeaponDisplayPos.Y;
                stackedInfo.CachedReloadTexture.Width = _reloadWidth;
                stackedInfo.CachedReloadTexture.Height = _reloadHeight;
                stackedInfo.CachedReloadTexture.P0 = texture[stackedInfo.ReloadIndex].P0;
                stackedInfo.CachedReloadTexture.P1 = texture[stackedInfo.ReloadIndex].P1;
                stackedInfo.CachedReloadTexture.P2 = texture[stackedInfo.ReloadIndex].P2;
                stackedInfo.CachedReloadTexture.P3 = texture[stackedInfo.ReloadIndex].P3;

                if (!mustCharge && _session.Tick10 && ++stackedInfo.ReloadIndex > texture.Length - 1)
                    stackedInfo.ReloadIndex = 0;

                if (reset)
                    stackedInfo.CachedReloadTexture.Persistant = false;

                _textureAddList.Add(stackedInfo.CachedReloadTexture);
            }
        }
    }
}
