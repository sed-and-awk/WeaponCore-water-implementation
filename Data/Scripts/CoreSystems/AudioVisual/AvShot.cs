﻿using System;
using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace CoreSystems.Support
{
    internal class AvShot
    {
        internal WeaponSystem System;
        internal WeaponDefinition.AmmoDef AmmoDef;
        internal MyEntity PrimeEntity;
        internal MyEntity TriggerEntity;
        internal MySoundPair FireSound;
        internal MySoundPair TravelSound;
        internal MyEntity3DSoundEmitter FireEmitter;
        internal MyEntity3DSoundEmitter TravelEmitter;
        internal MyQueue<AfterGlow> GlowSteps = new MyQueue<AfterGlow>(64);
        internal Queue<Shrinks> TracerShrinks = new Queue<Shrinks>(64);
        internal List<Vector3D> Offsets = new List<Vector3D>(64);
        internal MyParticleEffect AmmoEffect;
        internal MyParticleEffect FieldEffect;
        internal MyEntity CoreEntity;
        internal WeaponSystem.FiringSoundState FiringSoundState;
        internal bool Offset;
        internal bool AmmoSound;
        internal bool HasTravelSound;
        internal bool HitSoundActive;
        internal bool HitSoundInitted;
        internal bool StartSoundActived;
        internal bool Triggered;
        internal bool Cloaked;
        internal bool Active;
        internal bool ShrinkInited;
        internal bool TrailActivated;
        internal bool Hitting;
        internal bool Back;
        internal bool DetonateFakeExp;
        internal bool LastStep;
        internal bool IsShrapnel;
        internal bool AmmoParticleStopped;
        internal bool AmmoParticleInited;
        internal bool FieldParticleStopped;
        internal bool FieldParticleInited;
        internal bool ModelOnly;
        internal bool LastHitShield;
        internal bool ForceHitParticle;
        internal bool HitParticleActive;
        internal bool FakeExplosion;
        internal bool MarkForClose;
        internal bool ProEnded;
        internal bool SmartOn;
        internal double MaxTracerLength;
        internal double MaxGlowLength;
        internal double StepSize;
        internal double ShortStepSize;
        internal double TotalLength;
        internal double TracerWidth;
        internal double SegmentWidth;
        internal double TrailWidth;
        internal double VisualLength;
        internal double MaxSpeed;
        internal double MaxStepSize;
        internal double TracerLengthSqr;
        internal double EstTravel;
        internal double ShortEstTravel;
        internal double MaxTrajectory;
        internal float ShotFade;
        internal float TrailScaler;
        internal float GlowShrinkSize;
        internal float DistanceToLine;
        internal ulong ParentId = ulong.MaxValue;
        internal ulong UniqueMuzzleId;
        internal int LifeTime;
        internal int MuzzleId;
        internal int WeaponId;
        internal int TracerStep;
        internal int TracerSteps;
        internal uint LastTick;
        internal uint LastHit = uint.MaxValue / 2;
        internal int FireCounter;
        internal ParticleState HitParticle;
        internal TracerState Tracer;
        internal TrailState Trail;
        internal ModelState Model;
        internal Screen OnScreen;
        internal MatrixD OffsetMatrix;
        internal Vector3D Origin;
        internal Vector3D OriginUp;
        internal Vector3D OriginDir;
        internal Vector3D Direction;
        internal Vector3D VisualDir;
        internal Vector3D HitVelocity;
        internal Vector3D ShootVelStep;
        internal Vector3D TracerFront;
        internal Vector3D TracerBack;
        internal Vector3D ClosestPointOnLine;
        internal Vector4 Color;
        internal Vector4 SegmentColor;

        internal Hit Hit;
        internal AvClose EndState;
        internal MatrixD PrimeMatrix = MatrixD.Identity;
        internal BoundingSphereD ModelSphereCurrent;
        internal MatrixD TriggerMatrix = MatrixD.Identity;
        internal BoundingSphereD TestSphere = new BoundingSphereD(Vector3D.Zero, 200f);
        internal Shrinks EmptyShrink;

        public bool SegmentGaped;
        public bool TextureReverse;
        public int TextureIdx = -1;
        public uint TextureLastUpdate;
        public double SegmentLenTranserved = 1;
        public double SegMeasureStep;

        internal enum ParticleState
        {
            None,
            Explosion,
            Custom,
            Dirty,
        }

        internal enum TracerState
        {
            Off,
            Full,
            Grow,
            Shrink,
        }

        internal enum ModelState
        {
            None,
            Exists,
        }

        internal enum TrailState
        {
            Off,
            Front,
            Back,
        }

        internal enum Screen // Tracer includes Tail;
        {
            None,
            ModelOnly,
            InProximity,
            Tracer,
            Trail,
        }

        #region Run
        internal void Init(ProInfo info, bool smartsOn, double firstStepSize, double maxSpeed, ref Vector3D originDir)
        {
            System = info.System;
            AmmoDef = info.AmmoDef;
            IsShrapnel = info.IsShrapnel;
            SmartOn = smartsOn;
            if (ParentId != ulong.MaxValue) Log.Line($"invalid avshot, parentId:{ParentId}");
            ParentId = info.Id;
            Model = (info.AmmoDef.Const.PrimeModel || info.AmmoDef.Const.TriggerModel) ? Model = ModelState.Exists : Model = ModelState.None;
            PrimeEntity = info.PrimeEntity;
            TriggerEntity = info.TriggerEntity;
            Origin = info.Origin;
            OriginUp = info.OriginUp;
            Offset = AmmoDef.Const.OffsetEffect;
            MaxTracerLength = info.TracerLength;
            MuzzleId = info.MuzzleId;
            UniqueMuzzleId = info.UniqueMuzzleId;
            WeaponId = info.PartId;
            MaxSpeed = maxSpeed;
            MaxStepSize = MaxSpeed * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;
            ShootVelStep = info.ShooterVel * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;
            CoreEntity = info.Target.CoreEntity;
            MaxTrajectory = info.MaxTrajectory;
            ShotFade = info.ShotFade;
            FireCounter = info.FireCounter;
            ShrinkInited = false;
            ModelOnly = info.ModelOnly;
            OriginDir = originDir;
            if (AmmoDef.Const.DrawLine) Tracer = !AmmoDef.Const.IsBeamWeapon && firstStepSize < MaxTracerLength && !MyUtils.IsZero(firstStepSize - MaxTracerLength, 1E-01F) ? TracerState.Grow : TracerState.Full;
            else Tracer = TracerState.Off;

            if (AmmoDef.Const.Trail)
            {
                MaxGlowLength = MathHelperD.Clamp(AmmoDef.AmmoGraphics.Lines.Trail.DecayTime * MaxStepSize, 0.1f, MaxTrajectory);
                Trail = AmmoDef.AmmoGraphics.Lines.Trail.Back ? TrailState.Back : Trail = TrailState.Front;
                GlowShrinkSize = !AmmoDef.AmmoGraphics.Lines.Trail.UseColorFade ? AmmoDef.Const.TrailWidth / AmmoDef.AmmoGraphics.Lines.Trail.DecayTime : 1f / AmmoDef.AmmoGraphics.Lines.Trail.DecayTime;
                Back = Trail == TrailState.Back;
            }
            else Trail = TrailState.Off;
            TotalLength = MathHelperD.Clamp(MaxTracerLength + MaxGlowLength, 0.1f, MaxTrajectory);

            AvInfoCache infoCache;
            if (AmmoDef.Const.IsBeamWeapon && AmmoDef.Const.TracerMode != AmmoConstants.Texture.Normal && System.Session.AvShotCache.TryGetValue(info.UniqueMuzzleId, out infoCache))
                UpdateCache(infoCache);
        }
        static void ShellSort(List<DeferedAv> list)
        {
            int length = list.Count;

            for (int h = length / 2; h > 0; h /= 2)
            {
                for (int i = h; i < length; i += 1)
                {
                    var tempValue = list[i];
                    var temp = Vector3D.DistanceSquared(tempValue.TracerFront, tempValue.AvShot.System.Session.CameraPos);

                    int j;
                    for (j = i; j >= h && Vector3D.DistanceSquared(list[j - h].TracerFront, tempValue.AvShot.System.Session.CameraPos) > temp; j -= h)
                    {
                        list[j] = list[j - h];
                    }

                    list[j] = tempValue;
                }
            }
        }

        internal static void DeferedAvStateUpdates(Session s)
        {
            var drawCnt = s.Projectiles.DeferedAvDraw.Count;
            var maxDrawCnt = s.Settings.ClientConfig.ClientOptimizations ? s.Settings.ClientConfig.MaxProjectiles : int.MaxValue;
            if (drawCnt > maxDrawCnt)
                ShellSort(s.Projectiles.DeferedAvDraw);

            int onScreenCnt = 0;

            for (int x = 0; x < drawCnt; x++)
            {
                var d = s.Projectiles.DeferedAvDraw[x];
                var a = d.AvShot;
                var lineEffect = a.AmmoDef.Const.Trail || a.AmmoDef.Const.DrawLine;
                var saveHit = d.Hit;
                ++a.LifeTime;
                a.LastTick = s.Tick;
                a.StepSize = d.StepSize;
                a.EstTravel = a.StepSize * a.LifeTime;
                a.ShortStepSize = d.ShortStepSize ?? d.StepSize;
                a.ShortEstTravel = MathHelperD.Clamp((a.EstTravel - a.StepSize) + a.ShortStepSize, 0, double.MaxValue);

                a.VisualLength = d.VisualLength;
                if (a.SmartOn || a.AmmoDef.Const.IsBeamWeapon && a.AmmoDef.Const.ConvergeBeams)
                    a.VisualDir = d.Direction;
                else if (a.LifeTime == 1)
                    a.VisualDir = a.OriginDir;
                else if (!MyUtils.IsEqual(d.Direction, a.Direction) && !saveHit) {
                    var relativeDifference = (d.TracerFront - a.TracerFront) - a.ShootVelStep;
                    Vector3D.Normalize(ref relativeDifference, out a.VisualDir);
                }

                a.Direction = d.Direction;

                a.TracerFront = d.TracerFront;
                a.TracerBack = a.TracerFront + (-a.VisualDir * a.VisualLength);
                a.OnScreen = Screen.None; // clear OnScreen

                if (a.ModelOnly)
                {
                    a.ModelSphereCurrent.Center = a.TracerFront;
                    if (a.Triggered)
                        a.ModelSphereCurrent.Radius = d.TriggerGrowthSteps < a.AmmoDef.Const.AreaEffectSize ? a.TriggerMatrix.Scale.AbsMax() : a.AmmoDef.Const.AreaEffectSize;

                    if (s.Camera.IsInFrustum(ref a.ModelSphereCurrent))
                        a.OnScreen = Screen.ModelOnly;
                }
                else if (lineEffect || a.AmmoDef.Const.AmmoParticle)
                {
                    var rayTracer = new RayD(a.TracerBack, a.VisualDir);
                    var rayTrail = new RayD(a.TracerFront + (-a.VisualDir * a.ShortEstTravel), a.VisualDir);

                    //DsDebugDraw.DrawRay(rayTracer, VRageMath.Color.White, 0.25f, (float) VisualLength);
                    //DsDebugDraw.DrawRay(rayTrail, VRageMath.Color.Orange, 0.25f, (float)ShortEstTravel);

                    double? dist;
                    s.CameraFrustrum.Intersects(ref rayTracer, out dist);

                    if (a.AmmoDef.Const.AlwaysDraw || dist != null && dist <= a.VisualLength)
                        a.OnScreen = Screen.Tracer;
                    else if (a.AmmoDef.Const.Trail)
                    {
                        s.CameraFrustrum.Intersects(ref rayTrail, out dist);
                        if (dist != null && dist <= a.ShortEstTravel + a.ShortStepSize + a.MaxGlowLength)
                            a.OnScreen = Screen.Trail;
                    }
                    if (a.OnScreen != Screen.None && !a.TrailActivated && a.AmmoDef.Const.Trail) a.TrailActivated = true;

                    if (a.OnScreen == Screen.None && a.TrailActivated) a.OnScreen = Screen.Trail;

                    if (a.Model != ModelState.None)
                    {
                        a.ModelSphereCurrent.Center = a.TracerFront;
                        if (a.Triggered)
                            a.ModelSphereCurrent.Radius = d.TriggerGrowthSteps < a.AmmoDef.Const.AreaEffectSize ? a.TriggerMatrix.Scale.AbsMax() : a.AmmoDef.Const.AreaEffectSize;

                        if (a.OnScreen == Screen.None && s.Camera.IsInFrustum(ref a.ModelSphereCurrent))
                            a.OnScreen = Screen.ModelOnly;
                    }
                }

                if (a.OnScreen == Screen.None)
                {
                    a.TestSphere.Center = a.TracerFront;
                    if (s.Camera.IsInFrustum(ref a.TestSphere))
                        a.OnScreen = Screen.InProximity;
                    else if (Vector3D.DistanceSquared(a.TracerFront, s.CameraPos) <= 225)
                        a.OnScreen = Screen.InProximity;
                }

                if (maxDrawCnt > 0) {
                    if (a.OnScreen != Screen.None && ++onScreenCnt > maxDrawCnt)
                        a.OnScreen = Screen.None;
                }

                if (a.MuzzleId == -1)
                    return;

                if (saveHit)
                {
                    a.HitVelocity = a.Hit.HitVelocity;
                    a.Hitting = !a.ShrinkInited && a.ProEnded;
                    a.HitEffects();
                    a.LastHit = a.System.Session.Tick;
                }
                a.LastStep = a.Hitting || MyUtils.IsZero(a.MaxTrajectory - a.ShortEstTravel, 1E-01F);

                if (a.AmmoDef.Const.DrawLine)
                {
                    if (a.AmmoDef.Const.IsBeamWeapon || !saveHit && MyUtils.IsZero(a.MaxTracerLength - a.VisualLength, 1E-01F))
                    {
                        a.Tracer = TracerState.Full;
                    }
                    else if (a.Tracer != TracerState.Off && a.VisualLength <= 0)
                    {
                        a.Tracer = TracerState.Off;
                    }
                    else if (a.Hitting  && !a.ModelOnly && lineEffect && a.VisualLength / a.StepSize > 1 && !MyUtils.IsZero(a.EstTravel - a.ShortEstTravel))
                    {
                        a.Tracer = TracerState.Shrink;
                        a.TotalLength = MathHelperD.Clamp(a.VisualLength + a.MaxGlowLength, 0.1f, Vector3D.Distance(a.Origin, a.TracerFront));
                    }
                    else if (a.Tracer == TracerState.Grow && a.LastStep)
                    {
                        a.Tracer = TracerState.Full;
                    }
                }

                var lineOnScreen = a.OnScreen > (Screen)2;

                if (!a.Active && (a.OnScreen != Screen.None || a.HitSoundInitted || a.AmmoSound)) {
                    a.Active = true;
                    s.Av.AvShots.Add(a);
                }
                
                if (lineEffect && (a.Active || lineOnScreen))
                    a.LineVariableEffects();

                if (a.Tracer != TracerState.Off && lineOnScreen)
                {
                    if (a.Tracer == TracerState.Shrink && !a.ShrinkInited)
                        a.Shrink();
                    else if (a.AmmoDef.Const.IsBeamWeapon && a.AmmoDef.Const.HitParticle && !(a.MuzzleId != 0 && (a.AmmoDef.Const.ConvergeBeams || a.AmmoDef.Const.OneHitParticle)))
                    {
                        MyParticleEffect effect;
                        if (a.Hitting)
                        {
                            ContainmentType containment;
                            s.CameraFrustrum.Contains(ref a.Hit.SurfaceHit, out containment);
                            if (containment != ContainmentType.Disjoint) a.RunBeam();
                        }
                        else if (a.System.Session.Av.BeamEffects.TryGetValue(a.UniqueMuzzleId, out effect))
                        {
                            effect.Stop();
                            a.System.Session.Av.BeamEffects.Remove(a.UniqueMuzzleId);
                        }
                    }

                    if (a.AmmoDef.Const.OffsetEffect)
                        a.PrepOffsetEffect(a.TracerFront, a.VisualDir, a.VisualLength);
                }

                var backAndGrowing = a.Back && a.Tracer == TracerState.Grow;
                if (a.Trail != TrailState.Off && !backAndGrowing && lineOnScreen)
                    a.RunGlow(ref a.EmptyShrink, false, saveHit);

                if (a.AmmoDef.Const.AmmoParticle && a.Active)
                {
                    if (a.OnScreen != Screen.None)
                    {
                        /*
                        if (!a.AmmoDef.Const.IsBeamWeapon && !a.AmmoParticleStopped && a.AmmoEffect != null && a.AmmoDef.Const.AmmoParticleShrinks)
                            a.AmmoEffect.UserScale = MathHelper.Clamp(MathHelper.Lerp(1f, 0, a.DistanceToLine / a.AmmoDef.AmmoGraphics.Particles.Hit.Extras.MaxDistance), 0.05f, 1f);
                        */
                        if ((a.AmmoParticleStopped || !a.AmmoParticleInited))
                            a.PlayAmmoParticle();
                    }
                    else if (!a.AmmoParticleStopped && a.AmmoEffect != null)
                        a.DisposeAmmoEffect(false, true);
                }

                if (a.AmmoDef.Const.FieldParticle && a.Active)
                {
                    if (a.OnScreen != Screen.None)
                    {
                        /*
                        if (!a.AmmoDef.Const.IsBeamWeapon && !a.FieldParticleStopped && a.FieldEffect != null && a.AmmoDef.Const.FieldParticleShrinks)
                            a.FieldEffect.UserScale = MathHelper.Clamp(MathHelper.Lerp(1f, 0, a.DistanceToLine / a.AmmoDef.AreaEffect.Pulse.Particle.Extras.MaxDistance), 0.05f, 1f);
                        */
                        if ((a.FieldParticleStopped || !a.FieldParticleInited))
                            a.PlayFieldParticle();
                    }
                    else if (!a.FieldParticleStopped && a.FieldEffect != null)
                        a.DisposeFieldEffect(false, true);
                }

                a.Hitting = false;
            }
            s.Projectiles.DeferedAvDraw.Clear();
        }

        internal void RunGlow(ref Shrinks shrink, bool shrinking = false, bool hit = false)
        {
            var glowCount = GlowSteps.Count;
            var firstStep = glowCount == 0;
            var onlyStep = firstStep && LastStep;
            var extEnd = !Back && Hitting;
            var extStart = Back && firstStep && VisualLength < ShortStepSize;
            Vector3D frontPos;
            Vector3D backPos;

            var stopVel = shrinking || hit;
            var velStep = !stopVel ? ShootVelStep : Vector3D.Zero;

            if (shrinking)
            {
                frontPos = shrink.NewFront;
                backPos = !shrink.Last ? shrink.NewFront : TracerFront;
            }
            else
            {
                var futureStep = (VisualDir * ShortStepSize);
                var pastStep = (-VisualDir * ShortStepSize);
                if (!Back) futureStep -= velStep;
                frontPos = Back && !onlyStep ? TracerBack + futureStep : TracerFront;
                backPos = Back && !extStart ? TracerBack : TracerFront + pastStep;
            }

            if (glowCount <= AmmoDef.AmmoGraphics.Lines.Trail.DecayTime)
            {
                var glow = System.Session.Av.Glows.Count > 0 ? System.Session.Av.Glows.Pop() : new AfterGlow();

                glow.TailPos = backPos;
                GlowSteps.Enqueue(glow);
                ++glowCount;
            }
            var idxStart = glowCount - 1;
            var idxEnd = 0;
            for (int i = idxStart; i >= idxEnd; i--)
            {
                var g = GlowSteps[i];

                if (i != idxEnd)
                {
                    var extend = extEnd && i == idxStart;
                    g.Parent = GlowSteps[i - 1];
                    g.Line = new LineD(extend ? TracerFront + velStep : g.Parent.TailPos += velStep, extend ? g.Parent.TailPos : g.TailPos);
                }
                else if (i != idxStart)
                    g.Line = new LineD(g.Line.From + velStep, g.TailPos);
                else
                    g.Line = new LineD(frontPos, backPos);
            }
        }

        internal void Shrink()
        {
            ShrinkInit();
            for (int i = 0; i < TracerSteps; i++)
            {
                var last = (i == TracerSteps - 1);
                var shrunk = GetLine();
                if (shrunk.HasValue)
                {
                    if (shrunk.Value.Reduced < 0.1) continue;

                    var color = AmmoDef.AmmoGraphics.Lines.Tracer.Color;
                    if (AmmoDef.Const.LineColorVariance)
                    {
                        var cv = AmmoDef.AmmoGraphics.Lines.ColorVariance;
                        var randomValue = MyUtils.GetRandomFloat(cv.Start, cv.End);
                        color.X *= randomValue;
                        color.Y *= randomValue;
                        color.Z *= randomValue;
                    }

                    if (ShotFade > 0)
                        color *= MathHelper.Clamp(1f - ShotFade, 0.005f, 1f);

                    var width = AmmoDef.AmmoGraphics.Lines.Tracer.Width;
                    if (AmmoDef.Const.LineWidthVariance)
                    {
                        var wv = AmmoDef.AmmoGraphics.Lines.WidthVariance;
                        var randomValue = MyUtils.GetRandomFloat(wv.Start, wv.End);
                        width += randomValue;
                    }

                    width = (float)Math.Max(width, 0.10f * System.Session.ScaleFov * (DistanceToLine / 100));
                    TracerShrinks.Enqueue(new Shrinks { NewFront = shrunk.Value.NewTracerFront, Color = color, Length = shrunk.Value.Reduced, Thickness = width, Last = last });
                }
            }
        }

        private void ShrinkInit()
        {
            ShrinkInited = true;

            var fractualSteps = VisualLength / StepSize;
            TracerSteps = (int)Math.Floor(fractualSteps);
            TracerStep = TracerSteps;
            if (TracerSteps <= 0 || fractualSteps < StepSize && !MyUtils.IsZero(fractualSteps - StepSize, 1E-01F))
                Tracer = TracerState.Off;
        }

        internal Shrunk? GetLine()
        {
            if (TracerStep > 0)
            {
                Hit.LastHit += ShootVelStep;
                var newTracerFront = Hit.LastHit + -(VisualDir * (TracerStep * StepSize));
                var reduced = TracerStep-- * StepSize;
                return new Shrunk(ref newTracerFront, (float)reduced);
            }
            return null;
        }
        #endregion

        internal void LineVariableEffects()
        {
            var color = AmmoDef.AmmoGraphics.Lines.Tracer.Color;
            var segmentColor = AmmoDef.AmmoGraphics.Lines.Tracer.Segmentation.Color;
            if (AmmoDef.Const.TracerMode != AmmoConstants.Texture.Normal && TextureLastUpdate != System.Session.Tick)
            {
                if (System.Session.Tick - TextureLastUpdate > 1)
                    AmmoInfoClean();

                TextureLastUpdate = System.Session.Tick;

                switch (AmmoDef.Const.TracerMode) {
                    case AmmoConstants.Texture.Resize:
                        var wasGapped = SegmentGaped;
                        var segSize = AmmoDef.AmmoGraphics.Lines.Tracer.Segmentation;
                        var thisLen = wasGapped ? segSize.SegmentGap : segSize.SegmentLength;
                        var oldmStep = SegMeasureStep;

                        if (oldmStep > thisLen) {
                            wasGapped = !wasGapped && segSize.SegmentGap > 0;
                            SegmentGaped = wasGapped;
                            SegMeasureStep = 0;
                        }
                        SegMeasureStep += AmmoDef.Const.SegmentStep;
                        SegmentLenTranserved = wasGapped ? MathHelperD.Clamp(segSize.SegmentGap, 0, Math.Min(SegMeasureStep, segSize.SegmentGap)) : MathHelperD.Clamp(segSize.SegmentLength, 0, Math.Min(SegMeasureStep, segSize.SegmentLength));
                        break;
                    case AmmoConstants.Texture.Cycle:
                    case AmmoConstants.Texture.Wave:
                        if (AmmoDef.Const.TracerMode == AmmoConstants.Texture.Cycle) {
                            var current = TextureIdx;
                            if (current + 1 < AmmoDef.Const.TracerTextures.Length)
                                TextureIdx = current + 1;
                            else
                                TextureIdx = 0;
                        }
                        else {
                            var current = TextureIdx;
                            if (!TextureReverse) {
                                if (current + 1 < AmmoDef.Const.TracerTextures.Length)
                                    TextureIdx = current + 1;
                                else {
                                    TextureReverse = true;
                                    TextureIdx = current - 1;
                                }
                            }
                            else {
                                if (current - 1 >= 0)
                                    TextureIdx = current - 1;
                                else {
                                    TextureReverse = false;
                                    TextureIdx = current + 1;
                                }
                            }
                        }
                        break;
                    case AmmoConstants.Texture.Chaos:
                        TextureIdx = MyUtils.GetRandomInt(0, AmmoDef.Const.TracerTextures.Length);
                        break;
                }

                if (AmmoDef.Const.IsBeamWeapon)
                    System.Session.AvShotCache[UniqueMuzzleId] = new AvInfoCache {SegMeasureStep = SegMeasureStep, SegmentGaped = SegmentGaped, SegmentLenTranserved = SegmentLenTranserved, TextureIdx = TextureIdx, TextureLastUpdate = TextureLastUpdate, TextureReverse = TextureReverse};
            }

            if (AmmoDef.Const.LineColorVariance)
            {
                var cv = AmmoDef.AmmoGraphics.Lines.ColorVariance;
                var randomValue = MyUtils.GetRandomFloat(cv.Start, cv.End);
                color.X *= randomValue;
                color.Y *= randomValue;
                color.Z *= randomValue;
                if (AmmoDef.Const.TracerMode == AmmoConstants.Texture.Resize && AmmoDef.AmmoGraphics.Lines.Tracer.Segmentation.UseLineVariance)
                {
                    segmentColor.X *= randomValue;
                    segmentColor.Y *= randomValue;
                    segmentColor.Z *= randomValue;
                }
            }

            if (AmmoDef.Const.SegmentColorVariance)
            {
                var cv = AmmoDef.AmmoGraphics.Lines.Tracer.Segmentation.ColorVariance;
                var randomValue = MyUtils.GetRandomFloat(cv.Start, cv.End);
                segmentColor.X *= randomValue;
                segmentColor.Y *= randomValue;
                segmentColor.Z *= randomValue;
            }

            Color = color;
            SegmentColor = segmentColor;
            var tracerWidth = AmmoDef.AmmoGraphics.Lines.Tracer.Width;
            var trailWidth = AmmoDef.Const.TrailWidth;
            if (AmmoDef.Const.LineWidthVariance)
            {
                var wv = AmmoDef.AmmoGraphics.Lines.WidthVariance;
                var randomValue = MyUtils.GetRandomFloat(wv.Start, wv.End);
                tracerWidth += randomValue;
                if (AmmoDef.AmmoGraphics.Lines.Trail.UseWidthVariance)
                    trailWidth += randomValue;
            }

            var checkPos = TracerFront + (-VisualDir * TotalLength);
            ClosestPointOnLine = MyUtils.GetClosestPointOnLine(ref TracerFront, ref checkPos, ref System.Session.CameraPos);
            DistanceToLine = (float)Vector3D.Distance(ClosestPointOnLine, System.Session.CameraMatrix.Translation);

            if (AmmoDef.Const.IsBeamWeapon && Vector3D.DistanceSquared(TracerFront, TracerBack) > 640000)
            {
                checkPos = TracerFront + (-VisualDir * (TotalLength - MathHelperD.Clamp(DistanceToLine * 6, DistanceToLine, MaxTrajectory * 0.5)));
                ClosestPointOnLine = MyUtils.GetClosestPointOnLine(ref TracerFront, ref checkPos, ref System.Session.CameraPos);
                DistanceToLine = (float)Vector3D.Distance(ClosestPointOnLine, System.Session.CameraMatrix.Translation);
            }

            double scale = 0.1f;
            var widthScaler = !System.Session.GunnerBlackList ? 1f : (System.Session.ScaleFov * 1.3f);

            TracerWidth = MathHelperD.Clamp(scale * System.Session.ScaleFov * (DistanceToLine / 100), tracerWidth * widthScaler, double.MaxValue);
            TrailWidth = MathHelperD.Clamp(scale * System.Session.ScaleFov * (DistanceToLine / 100), trailWidth * widthScaler, double.MaxValue);

            TrailScaler = ((float)TrailWidth / trailWidth);

            var seg = AmmoDef.AmmoGraphics.Lines.Tracer.Segmentation;
            SegmentWidth = seg.WidthMultiplier > 0 ? TracerWidth * seg.WidthMultiplier : TracerWidth;
            if (AmmoDef.Const.SegmentWidthVariance)
            {
                var wv = AmmoDef.AmmoGraphics.Lines.Tracer.Segmentation.WidthVariance;
                var randomValue = MyUtils.GetRandomFloat(wv.Start, wv.End);
                SegmentWidth += randomValue;
            }
        }

        internal void PrepOffsetEffect(Vector3D tracerStart, Vector3D direction, double tracerLength)
        {
            var up = MatrixD.Identity.Up;
            var startPos = tracerStart + -(direction * tracerLength);
            MatrixD.CreateWorld(ref startPos, ref direction, ref up, out OffsetMatrix);
            TracerLengthSqr = tracerLength * tracerLength;
            var maxOffset = AmmoDef.AmmoGraphics.Lines.OffsetEffect.MaxOffset;
            var minLength = AmmoDef.AmmoGraphics.Lines.OffsetEffect.MinLength;
            var maxLength = MathHelperD.Clamp(AmmoDef.AmmoGraphics.Lines.OffsetEffect.MaxLength, 0, tracerLength);

            double currentForwardDistance = 0;
            while (currentForwardDistance <= tracerLength)
            {
                currentForwardDistance += MyUtils.GetRandomDouble(minLength, maxLength);
                var lateralXDistance = MyUtils.GetRandomDouble(maxOffset * -1, maxOffset);
                var lateralYDistance = MyUtils.GetRandomDouble(maxOffset * -1, maxOffset);
                Offsets.Add(new Vector3D(lateralXDistance, lateralYDistance, currentForwardDistance * -1));
            }
        }


        internal void DrawLineOffsetEffect(Vector3D pos, Vector3D direction, double tracerLength, float beamRadius, Vector4 color)
        {
            MatrixD matrix;
            var up = MatrixD.Identity.Up;
            var startPos = pos + -(direction * tracerLength);
            MatrixD.CreateWorld(ref startPos, ref direction, ref up, out matrix);
            var offsetMaterial = AmmoDef.Const.TracerTextures[0];
            var tracerLengthSqr = tracerLength * tracerLength;
            var maxOffset = AmmoDef.AmmoGraphics.Lines.OffsetEffect.MaxOffset;
            var minLength = AmmoDef.AmmoGraphics.Lines.OffsetEffect.MinLength;
            var maxLength = MathHelperD.Clamp(AmmoDef.AmmoGraphics.Lines.OffsetEffect.MaxLength, 0, tracerLength);

            double currentForwardDistance = 0;

            while (currentForwardDistance < tracerLength)
            {
                currentForwardDistance += MyUtils.GetRandomDouble(minLength, maxLength);
                var lateralXDistance = MyUtils.GetRandomDouble(maxOffset * -1, maxOffset);
                var lateralYDistance = MyUtils.GetRandomDouble(maxOffset * -1, maxOffset);
                Offsets.Add(new Vector3D(lateralXDistance, lateralYDistance, currentForwardDistance * -1));
            }

            for (int i = 0; i < Offsets.Count; i++)
            {
                Vector3D fromBeam;
                Vector3D toBeam;

                if (i == 0)
                {
                    fromBeam = matrix.Translation;
                    toBeam = Vector3D.Transform(Offsets[i], matrix);
                }
                else
                {
                    fromBeam = Vector3D.Transform(Offsets[i - 1], matrix);
                    toBeam = Vector3D.Transform(Offsets[i], matrix);
                }

                Vector3 dir = (toBeam - fromBeam);
                var length = dir.Length();
                var normDir = dir / length;
                MyTransparentGeometry.AddLineBillboard(offsetMaterial, color, fromBeam, normDir, length, beamRadius);

                if (Vector3D.DistanceSquared(matrix.Translation, toBeam) > tracerLengthSqr) break;
            }
            Offsets.Clear();
        }

        internal void ShortStepAvUpdate(ProInfo info, bool useCollisionSize, bool hit, bool earlyEnd, Vector3D position)
        {

            var stepSize = (info.DistanceTraveled - info.PrevDistanceTraveled);
            var avSize = useCollisionSize ? AmmoDef.Const.CollisionSize : info.TracerLength;

            var endPos = hit ? Hit.LastHit : !earlyEnd ? position + -info.Direction * (info.DistanceTraveled - info.MaxTrajectory) : position;

            double remainingTracer;
            double stepSizeToHit;
            if (AmmoDef.Const.IsBeamWeapon)
            {
                double beamLength;
                Vector3D.Distance(ref Origin, ref endPos, out beamLength);
                remainingTracer = MathHelperD.Clamp(beamLength, 0, avSize);
                stepSizeToHit = remainingTracer;
            }
            else
            {
                double overShot;
                Vector3D.Distance(ref endPos, ref position, out overShot);
                stepSizeToHit = Math.Abs(stepSize - overShot);
                if (avSize < stepSize && !MyUtils.IsZero(avSize - stepSize, 1E-01F))
                {
                    remainingTracer = MathHelperD.Clamp(avSize - stepSizeToHit, 0, stepSizeToHit);
                }
                else if (avSize >= overShot)
                {
                    remainingTracer = MathHelperD.Clamp(avSize - overShot, 0, Math.Min(avSize, info.PrevDistanceTraveled + stepSizeToHit));
                }
                else remainingTracer = 0;
            }

            if (MyUtils.IsZero(remainingTracer, 1E-01F)) remainingTracer = 0;
            System.Session.Projectiles.DeferedAvDraw.Add(new DeferedAv { AvShot = this, StepSize = stepSize, VisualLength = remainingTracer, TracerFront = endPos, ShortStepSize = stepSizeToHit, Hit = hit, TriggerGrowthSteps = info.TriggerGrowthSteps, Direction = info.Direction });
        }

        internal void HitEffects(bool force = false)
        {
            if (System.Session.Tick - LastHit > 4 || force) {

                double distToCameraSqr;
                Vector3D.DistanceSquared(ref Hit.SurfaceHit, ref System.Session.CameraPos, out distToCameraSqr);

                if (OnScreen == Screen.Tracer || distToCameraSqr < 360000) {
                    if (FakeExplosion)
                        HitParticle = ParticleState.Explosion;
                    else if (HitParticleActive && AmmoDef.Const.HitParticle && !(LastHitShield && !AmmoDef.AmmoGraphics.Particles.Hit.ApplyToShield))
                        HitParticle = ParticleState.Custom;
                }


                var hitSound = AmmoDef.Const.HitSound && (HitSoundActive && (distToCameraSqr < AmmoDef.Const.HitSoundDistSqr || !LastHitShield || AmmoDef.AmmoAudio.HitPlayShield));
                if (hitSound) {

                    MySoundPair pair;
                    Stack<MySoundPair> pool;
                    if (!AmmoDef.Const.AltHitSounds)                    {
                        pool = AmmoDef.Const.HitDefaultSoundPairs;
                        pair = pool.Count > 0 ? pool.Pop() : new MySoundPair(AmmoDef.AmmoAudio.HitSound, false);
                    }
                    else {

                        var shield = Hit.Entity as IMyUpgradeModule;
                        var voxel = Hit.Entity as MyVoxelBase;
                        var player = Hit.Entity as IMyCharacter;
                        var floating = Hit.Entity as MyFloatingObject;

                        if (voxel != null && !string.IsNullOrEmpty(AmmoDef.AmmoAudio.VoxelHitSound)) {
                            pool = AmmoDef.Const.HitVoxelSoundPairs;
                            pair = pool.Count > 0 ? pool.Pop() : new MySoundPair(AmmoDef.AmmoAudio.VoxelHitSound, false);
                        }
                        else if (player != null && !string.IsNullOrEmpty(AmmoDef.AmmoAudio.PlayerHitSound)) {
                            pool = AmmoDef.Const.HitPlayerSoundPairs;
                            pair = pool.Count > 0 ? pool.Pop() : new MySoundPair(AmmoDef.AmmoAudio.PlayerHitSound, false);
                        }
                        else if (floating != null && !string.IsNullOrEmpty(AmmoDef.AmmoAudio.FloatingHitSound)) {
                            pool = AmmoDef.Const.HitFloatingSoundPairs;
                            pair = AmmoDef.Const.HitFloatingSoundPairs.Count > 0 ? AmmoDef.Const.HitFloatingSoundPairs.Pop() : new MySoundPair(AmmoDef.AmmoAudio.FloatingHitSound, false);
                        }
                        else {

                            if (shield != null && !string.IsNullOrEmpty(AmmoDef.AmmoAudio.ShieldHitSound)) {
                                pool = AmmoDef.Const.HitShieldSoundPairs;
                                pair = pool.Count > 0 ? pool.Pop() : new MySoundPair(AmmoDef.AmmoAudio.ShieldHitSound, false);
                            }
                            else {
                                pool = AmmoDef.Const.HitDefaultSoundPairs;
                                pair = pool.Count > 0 ? pool.Pop() : new MySoundPair(AmmoDef.AmmoAudio.HitSound, false);
                            }
                        }
                    }

                    if (pool != null && pair != null) {

                        var hitEmitter = System.Session.Av.HitEmitters.Count > 0 ? System.Session.Av.HitEmitters.Pop() : new MyEntity3DSoundEmitter(null);

                        hitEmitter.Entity = Hit.Entity;
                        //hitEmitter.CanPlayLoopSounds = false;
                        System.Session.Av.HitSounds.Add(new HitSound { Hit = true, Pool = pool, Emitter = hitEmitter, SoundPair = pair, Position = Hit.SurfaceHit });

                        HitSoundInitted = true;
                    }
                }
                LastHitShield = false;
            }
        }


        internal void SetupSounds(double distanceFromCameraSqr)
        {
            FiringSoundState = System.FiringSound;

            if (!AmmoDef.Const.IsBeamWeapon && AmmoDef.Const.AmmoTravelSound) {
                HasTravelSound = true;
                TravelEmitter = System.Session.Av.TravelEmitters.Count > 0 ? System.Session.Av.TravelEmitters.Pop() : new MyEntity3DSoundEmitter(null);

                TravelEmitter.CanPlayLoopSounds = true;
                TravelSound = AmmoDef.Const.TravelSoundPairs.Count > 0 ? AmmoDef.Const.TravelSoundPairs.Pop() : new MySoundPair(AmmoDef.AmmoAudio.TravelSound, false);
            }
            else HasTravelSound = false;

            if (AmmoDef.Const.HitSound) {
                var hitSoundChance = AmmoDef.AmmoAudio.HitPlayChance;
                HitSoundActive = (hitSoundChance >= 1 || hitSoundChance >= MyUtils.GetRandomDouble(0.0f, 1f));
            }

            if (!IsShrapnel && FiringSoundState == WeaponSystem.FiringSoundState.PerShot && distanceFromCameraSqr < System.FiringSoundDistSqr) {
                StartSoundActived = true;

                FireEmitter = System.Session.Av.FireEmitters.Count > 0 ? System.Session.Av.FireEmitters.Pop() : new MyEntity3DSoundEmitter(null);

                FireEmitter.CanPlayLoopSounds = true;
                FireEmitter.Entity = CoreEntity;
                FireSound = System.FirePerShotPairs.Count > 0 ? System.FirePerShotPairs.Pop() : new MySoundPair(System.Values.HardPoint.Audio.FiringSound, false);

                FireEmitter.SetPosition(Origin);
            }
                        
            if (StartSoundActived) {
                StartSoundActived = false;
                FireEmitter.PlaySound(FireSound, true);
            }
        }

        internal void AmmoSoundStart()
        {
            TravelEmitter.SetPosition(TracerFront);
            TravelEmitter.Entity = PrimeEntity;
            TravelEmitter.PlaySound(TravelSound, true);
            AmmoSound = true;
        }

        internal void PlayAmmoParticle()
        {
            MatrixD matrix;
            if (Model != ModelState.None && PrimeEntity != null)
                matrix = PrimeMatrix;
            else {
                matrix = MatrixD.CreateWorld(TracerFront, Direction, OriginUp);
                var offVec = TracerFront + Vector3D.Rotate(AmmoDef.AmmoGraphics.Particles.Ammo.Offset, matrix);
                matrix.Translation = offVec;
            }

            var renderId = AmmoDef.Const.PrimeModel && PrimeEntity != null ? PrimeEntity.Render.GetRenderObjectID() : uint.MaxValue;
            if (MyParticlesManager.TryCreateParticleEffect(AmmoDef.AmmoGraphics.Particles.Ammo.Name, ref matrix, ref TracerFront, renderId, out AmmoEffect))
            {

                //AmmoEffect.UserColorMultiplier = AmmoDef.AmmoGraphics.Particles.Ammo.Color;
                AmmoEffect.UserRadiusMultiplier = AmmoDef.AmmoGraphics.Particles.Ammo.Extras.Scale;
                //AmmoEffect.UserScale = 1;


                AmmoParticleStopped = false;
                AmmoParticleInited = true;
                var loop = AmmoEffect.Loop || AmmoEffect.DurationMax <= 0;
                if (!loop)
                    AmmoEffect = null;
            }
        }

        internal void PlayFieldParticle()
        {
            var pos = TriggerEntity.PositionComp.WorldAABB.Center;
            if (MyParticlesManager.TryCreateParticleEffect(AmmoDef.AreaEffect.Pulse.Particle.Name, ref TriggerMatrix, ref pos, uint.MaxValue, out FieldEffect))
            {
                //FieldEffect.UserColorMultiplier = AmmoDef.AreaEffect.Pulse.Particle.Color;
                FieldEffect.UserRadiusMultiplier = AmmoDef.AreaEffect.Pulse.Particle.Extras.Scale;
                //FieldEffect.UserScale = 1;
                FieldParticleStopped = false;
                FieldParticleInited = true;
            }
        }

        internal void DisposeAmmoEffect(bool instant, bool pause)
        {
            if (AmmoEffect != null)
            {
                AmmoEffect.Stop(instant);
                AmmoEffect = null;
            }

            if (pause)
                AmmoParticleStopped = true;
        }

        internal void DisposeFieldEffect(bool instant, bool pause)
        {
            if (FieldEffect != null)
            {
                FieldEffect.Stop(instant);
                FieldEffect = null;
            }

            if (pause)
                FieldParticleStopped = true;
        }

        internal void ResetHit()
        {
            ShrinkInited = false;
            TotalLength = MathHelperD.Clamp(MaxTracerLength + MaxGlowLength, 0.1f, MaxTrajectory);
        }

        internal void RunBeam()
        {
            MyParticleEffect effect;
            MatrixD matrix;
            var vel = HitVelocity;
            if (!System.Session.Av.BeamEffects.TryGetValue(UniqueMuzzleId, out effect)) {

                MatrixD.CreateTranslation(ref TracerFront, out matrix);
                if (!MyParticlesManager.TryCreateParticleEffect(AmmoDef.AmmoGraphics.Particles.Hit.Name, ref matrix, ref TracerFront, uint.MaxValue, out effect)) {
                    return;
                }

                if (effect.Loop || effect.DurationMax <= 0)
                    System.Session.Av.BeamEffects[UniqueMuzzleId] = effect;

                effect.UserRadiusMultiplier = AmmoDef.AmmoGraphics.Particles.Hit.Extras.Scale;
                Vector3D.ClampToSphere(ref vel, (float)MaxSpeed);
            }
            else if (effect != null && !effect.IsEmittingStopped) {
                MatrixD.CreateTranslation(ref Hit.SurfaceHit, out matrix);
                Vector3D.ClampToSphere(ref vel, (float)MaxSpeed);
                effect.WorldMatrix = matrix;
            }
        }
        internal void AvClose()
        {
            if (MarkForClose)
                return;

            if (Vector3D.IsZero(TracerFront)) TracerFront = EndState.EndPos;

            if (AmmoDef.Const.AmmoParticle)
            {
                var forceClose = !(AmmoEffect != null && AmmoEffect.Loop && !AmmoDef.AmmoGraphics.Particles.Ammo.Extras.Restart);
                DisposeAmmoEffect(forceClose, false);
            }

            if (EndState.DetonateFakeExp)
            {

                HitParticle = ParticleState.Dirty;
                if (OnScreen != Screen.None && (!string.IsNullOrEmpty(AmmoDef.AreaEffect.Explosions.CustomParticle) || System.Session.Av.ExplosionReady))
                {
                    SUtils.CreateFakeExplosion(System.Session, AmmoDef.Const.DetonationRadius, TracerFront, Direction, Hit.Entity, AmmoDef, Hit.HitVelocity);
                }
            }

            MarkForClose = true;
        }
        
        public void AmmoInfoClean()
        {
            SegmentGaped = false;
            TextureReverse = false;
            SegmentLenTranserved = 1;
            TextureIdx = -1;
            SegMeasureStep = 0;
            TextureLastUpdate = 0;
        }

        internal void UpdateCache(AvInfoCache avInfoCache)
        {
            SegmentGaped = avInfoCache.SegmentGaped;
            TextureReverse = avInfoCache.TextureReverse;
            SegmentLenTranserved = avInfoCache.SegmentLenTranserved;
            TextureIdx = avInfoCache.TextureIdx;
            SegMeasureStep = avInfoCache.SegMeasureStep;
            TextureLastUpdate = avInfoCache.TextureLastUpdate;
        }


        internal void Close()
        {
            // Reset only vars that are not always set
            Hit = new Hit();
            EndState = new AvClose();

            if (FireEmitter != null)
                System.Session.SoundsToClean.Add(new Session.CleanSound { Emitter = FireEmitter, EmitterPool = System.Session.Av.FireEmitters, SoundPair = FireSound, SoundPairPool = System.FirePerShotPairs, SpawnTick = System.Session.Tick });
            
            if (TravelEmitter != null) {
                
                AmmoSound = false;
                System.Session.SoundsToClean.Add(new Session.CleanSound { Force = true, Emitter = TravelEmitter, EmitterPool = System.Session.Av.TravelEmitters, SoundPair = TravelSound, SoundPairPool = AmmoDef.Const.TravelSoundPairs, SpawnTick = System.Session.Tick });
            }

            if (AmmoEffect != null)
                DisposeAmmoEffect(true, false);

            if (PrimeEntity != null && PrimeEntity.InScene)
            {
                PrimeEntity.InScene = false;
                PrimeEntity.Render.RemoveRenderObjects();
            }

            if (Triggered && TriggerEntity != null && TriggerEntity.InScene)
            {
                TriggerEntity.InScene = false;
                TriggerEntity.Render.RemoveRenderObjects();
            }

            HitVelocity = Vector3D.Zero;
            TracerBack = Vector3D.Zero;
            TracerFront = Vector3D.Zero;
            ClosestPointOnLine = Vector3D.Zero;
            Color = Vector4.Zero;
            SegmentColor = Vector4.Zero;
            OnScreen = Screen.None;
            Tracer = TracerState.Off;
            Trail = TrailState.Off;
            LifeTime = 0;
            TracerSteps = 0;
            TracerStep = 0;
            DistanceToLine = 0;
            TracerWidth = 0;
            TrailWidth = 0;
            SegmentWidth = 0;
            TrailScaler = 0;
            MaxTrajectory = 0;
            ShotFade = 0;
            FireCounter = 0;
            UniqueMuzzleId = 0;
            LastHit = uint.MaxValue / 2;
            ParentId = ulong.MaxValue;
            LastHitShield = false;
            AmmoSound = false;
            HitSoundActive = false;
            HitSoundInitted = false;
            StartSoundActived = false;
            IsShrapnel = false;
            HasTravelSound = false;
            HitParticle = ParticleState.None;
            Triggered = false;
            Cloaked = false;
            Active = false;
            TrailActivated = false;
            ShrinkInited = false;
            Hitting = false;
            Back = false;
            LastStep = false;
            DetonateFakeExp = false;
            AmmoParticleStopped = false;
            AmmoParticleInited = false;
            FieldParticleStopped = false;
            FieldParticleInited = false;
            ModelOnly = false;
            ForceHitParticle = false;
            HitParticleActive = false;
            FakeExplosion = false;
            MarkForClose = false;
            ProEnded = false;
            TracerShrinks.Clear();
            GlowSteps.Clear();
            Offsets.Clear();
            //
            SegmentGaped = false;
            TextureReverse = false;
            SegmentLenTranserved = 1;
            TextureIdx = -1;
            SegMeasureStep = 0;
            TextureLastUpdate = 0;
            //

            CoreEntity = null;
            PrimeEntity = null;
            TriggerEntity = null;
            AmmoDef = null;
            System = null;
            FireEmitter = null;
            FireSound = null;
            TravelEmitter = null;
            TravelSound = null;
        }
    }

    internal class AfterGlow
    {
        internal AfterGlow Parent;
        internal Vector3D TailPos;
        internal LineD Line;
        internal int Step;
    }

    internal struct Shrinks
    {
        internal Vector3D NewFront;
        internal Vector4 Color;
        internal float Length;
        internal float Thickness;
        internal bool Last;

    }

    internal struct AvInfoCache
    {
        internal bool SegmentGaped;
        internal bool TextureReverse;
        internal double SegmentLenTranserved;
        internal double SegMeasureStep;
        internal int TextureIdx;
        internal uint TextureLastUpdate;
    }

    internal struct AvClose
    {
        internal bool Dirty;
        internal bool DetonateFakeExp;
        internal Vector3D EndPos;
    }

    internal struct DeferedAv
    {
        internal AvShot AvShot;
        internal bool Hit;
        internal double StepSize;
        internal double VisualLength;
        internal double? ShortStepSize;
        internal int TriggerGrowthSteps;
        internal Vector3D TracerFront;
        internal Vector3D Direction;
    }

    internal struct Shrunk
    {
        internal readonly Vector3D NewTracerFront;
        internal readonly float Reduced;

        internal Shrunk(ref Vector3D newTracerFront, float reduced)
        {
            NewTracerFront = newTracerFront;
            Reduced = reduced;
        }
    }
}
