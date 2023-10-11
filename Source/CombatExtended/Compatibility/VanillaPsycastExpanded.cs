﻿using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VanillaPsycastsExpanded;
using Verse;

namespace CombatExtended.Compatibility
{
    public class VanillaPsycastExpanded : IPatch
    {
        const string ModName = "Vanilla Psycasts Expanded";
        public bool CanInstall()
        {
            Log.Message("Vanilla Psycasts Expanded loaded: " + ModLister.HasActiveModWithName(ModName));
            return ModLister.HasActiveModWithName(ModName);
        }

        public IEnumerable<string> GetCompatList()
        {
            yield break;
        }
        public void Install()
        {
            BlockerRegistry.RegisterImpactSomethingCallback(ImpactSomething); //temp commented
            BlockerRegistry.RegisterBeforeCollideWithCallback(BeforeCollideWith);
            BlockerRegistry.RegisterCheckForCollisionCallback(Hediff_Overshield_InterceptCheck);
            BlockerRegistry.RegisterCheckForCollisionBetweenCallback(AOE_CheckIntercept);
        }

        private static bool BeforeCollideWith(ProjectileCE projectile, Thing collideWith)
        {
            if (collideWith is Pawn pawn)
            {
                var interceptor = pawn.health.hediffSet.hediffs.FirstOrDefault(x => x.GetType() == typeof(Hediff_Overshield)) as Hediff_Overshield;
                if (interceptor != null)
                {
                    OnIntercepted(interceptor, projectile);
                    return true;
                }
            }
            return false;
        }
        private static bool ImpactSomething(ProjectileCE projectile, Thing launcher)
        {
            return Hediff_Overshield_InterceptCheck(projectile, projectile.ExactPosition.ToIntVec3(), launcher);
        }
        public static bool Hediff_Overshield_InterceptCheck(ProjectileCE projectile, IntVec3 cell, Thing launcher)
        {
            foreach (var interceptor in projectile.Map.thingGrid.ThingsListAt(cell).OfType<Pawn>()
                .SelectMany(x => x.health.hediffSet.hediffs)
                .Where(x => x.GetType() == typeof(Hediff_Overshield)).Cast<Hediff_Overshield>())
            {
                var def = projectile.def;
                var result = interceptor.pawn != launcher && (interceptor.pawn.Position == cell);
                if (result)
                {
                    OnIntercepted(interceptor, projectile);
                    return result;
                }
            }
            return false;
        }
        public static bool AOE_CheckIntercept(ProjectileCE projectile, Vector3 from, Vector3 newExactPos)
        {
            var def = projectile.def;
            foreach (var interceptor in projectile.Map.listerThings.ThingsInGroup(ThingRequestGroup.Pawn).Cast<Pawn>()
                .SelectMany(x => x.health.hediffSet.hediffs)
                .Where(x => x is Hediff_Overshield && x.GetType() != typeof(Hediff_Overshield)).Cast<Hediff_Overshield>())
            {
                Vector3 shieldPosition = interceptor.pawn.Position.ToVector3ShiftedWithAltitude(0.5f);
                float radius = interceptor.OverlaySize;
                float blockRadius = radius + def.projectile.SpeedTilesPerTick + 0.1f;
                if ((from - shieldPosition).sqrMagnitude < radius * radius)
                {
                    return false;
                }
                if ((newExactPos - shieldPosition).sqrMagnitude > Mathf.Pow(blockRadius, 2))
                {
                    return false;
                }

                if (projectile.def.projectile.flyOverhead)
                {
                    return false;
                }

                if ((shieldPosition - from).sqrMagnitude <= Mathf.Pow((float)radius, 2))
                {
                    return false;
                }
                if (!CE_Utility.IntersectLineSphericalOutline(shieldPosition, radius, from, newExactPos))
                {
                    return false;
                }

                OnIntercepted(interceptor, projectile);
                return true;
            }

            return false;
        }

        private static void OnIntercepted(Hediff_Overshield interceptor, ProjectileCE projectile)
        {
            var exactPosition = CE_Utility.IntersectionPoint(projectile.OriginIV3.ToVector3(), projectile.ExactPosition, interceptor.pawn.Position.ToVector3Shifted(), interceptor.OverlaySize).OrderBy(x => (projectile.OriginIV3.ToVector3() - x).sqrMagnitude).First();
            projectile.ExactPosition = exactPosition;
            new Traverse(interceptor).Field("lastInterceptAngle").SetValue(exactPosition.AngleToFlat(interceptor.pawn.TrueCenter()));
            new Traverse(interceptor).Field("lastInterceptTicks").SetValue(Find.TickManager.TicksGame);
            new Traverse(interceptor).Field("drawInterceptCone").SetValue(true);

            FleckMakerCE.ThrowPsycastShieldFleck(exactPosition, projectile.Map, 0.35f);
            projectile.InterceptProjectile(interceptor, projectile.ExactPosition, true);
        }
    }
}
