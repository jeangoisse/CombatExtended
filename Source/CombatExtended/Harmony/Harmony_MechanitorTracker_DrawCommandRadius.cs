﻿using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(Pawn_MechanitorTracker), nameof(Pawn_MechanitorTracker.DrawCommandRadius))]
    public static class Harmony_MechanitorTracker_DrawCommandRadius
    {
        public static bool Prefix(Pawn_MechanitorTracker __instance, Pawn ___pawn)
        {
            if (___pawn != null && ___pawn.Spawned && __instance.AnySelectedDraftedMechs)
            {
                GenDraw.DrawRadiusRing(___pawn.Position, 43.9f, Color.white, /*(IntVec3 c) => ___pawn.mechanitor.CanCommandTo(c)*/null); //radii ending in .9 match up with the radial pattern, predicates aren't needed
            }
            return false;
        }
    }
}
