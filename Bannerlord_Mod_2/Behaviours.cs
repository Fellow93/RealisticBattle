﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static TaleWorlds.Core.ItemObject;
using static TaleWorlds.MountAndBlade.ArrangementOrder;

namespace RealisticBattle
{
    class Behaviours
    {

        //private static void SetDefensiveArrangementMoveBehaviorValues(Agent unit)
        //{
        //    unit.SetAIBehaviorValues(AISimpleBehaviorKind.GoToPos, 3f, 8f, 5f, 20f, 6f);
        //    unit.SetAIBehaviorValues(AISimpleBehaviorKind.Melee, 4f, 5f, 0f, 20f, 0f);
        //    unit.SetAIBehaviorValues(AISimpleBehaviorKind.Ranged, 0f, 7f, 0f, 20f, 0f);
        //    unit.SetAIBehaviorValues(AISimpleBehaviorKind.ChargeHorseback, 0f, 7f, 0f, 30f, 0f);
        //    unit.SetAIBehaviorValues(AISimpleBehaviorKind.RangedHorseback, 0f, 15f, 0f, 30f, 0f);

        //    unit.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityMelee, 5f, 12f, 7.5f, 30f, 4f);
        //    unit.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityRanged, 0.55f, 12f, 0.8f, 30f, 0.45f);
        //}

        //internal static void SetDefaultMoveBehaviorValues(Agent unit)
        //{
        //    unit.SetAIBehaviorValues(AISimpleBehaviorKind.GoToPos, 3f, 7f, 5f, 20f, 6f);
        //    unit.SetAIBehaviorValues(AISimpleBehaviorKind.Melee, 8f, 7f, 5f, 20f, 0.01f);
        //    unit.SetAIBehaviorValues(AISimpleBehaviorKind.Ranged, 0.02f, 7f, 0.04f, 20f, 0.03f);
        //    unit.SetAIBehaviorValues(AISimpleBehaviorKind.ChargeHorseback, 10f, 7f, 5f, 30f, 0.05f);
        //    unit.SetAIBehaviorValues(AISimpleBehaviorKind.RangedHorseback, 0.02f, 15f, 0.065f, 30f, 0.055f);

        //    unit.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityMelee, 5f, 12f, 7.5f, 30f, 4f);
        //    unit.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityRanged, 0.55f, 12f, 0.8f, 30f, 0.45f);
        //}

        //[HarmonyPatch(typeof(MovementOrder))]
        //class OverrideMovementOrder
        //{
        //    [HarmonyPostfix]
        //    [HarmonyPatch("OnApply")]
        //    static void PostfixOnApply(ArrangementOrder __instance, ref Formation formation)
        //    {
        //        if(__instance != null)
        //        {
        //            if (formation.ArrangementOrder.OrderEnum == ArrangementOrderEnum.ShieldWall || formation.ArrangementOrder.OrderEnum == ArrangementOrderEnum.Line)
        //            {
        //                //for (int i = 0; i < formation.CountOfUnits; i++)
        //                //{
        //                //    Agent agent = formation.GetUnitWithIndex(i);
        //                //    //agent
        //                //}
        //                formation.ApplyActionOnEachUnit(SetDefensiveArrangementMoveBehaviorValues);
        //            }
        //        }
        //    }

        //    [HarmonyPostfix]
        //    [HarmonyPatch("OnUnitJoinOrLeave")]
        //    static void PostfixOnUnitJoinOrLeave(ArrangementOrder __instance, ref Formation formation)
        //    {
        //        if (__instance != null)
        //        {
        //            if (formation.ArrangementOrder.OrderEnum == ArrangementOrderEnum.ShieldWall || formation.ArrangementOrder.OrderEnum == ArrangementOrderEnum.Line)
        //            {
        //                //for (int i = 0; i < formation.CountOfUnits; i++)
        //                //{
        //                //    Agent agent = formation.GetUnitWithIndex(i);
        //                //    //agent
        //                //}
        //                formation.ApplyActionOnEachUnit(SetDefensiveArrangementMoveBehaviorValues);
        //            }
        //        }
        //    }
        //}

       [HarmonyPatch(typeof(BehaviorSkirmishLine))]
        class OverrideBehaviorSkirmishLine
        {
            [HarmonyPostfix]
            [HarmonyPatch("CalculateCurrentOrder")]
            static void PostfixCalculateCurrentOrder(Formation ____mainFormation, ref FacingOrder ___CurrentFacingOrder)
            {
                if (____mainFormation != null)
                {
                    MethodInfo method = typeof(FacingOrder).GetMethod("FacingOrderLookAtDirection", BindingFlags.NonPublic | BindingFlags.Static);
                    method.DeclaringType.GetMethod("FacingOrderLookAtDirection");
                    ___CurrentFacingOrder = (FacingOrder)method.Invoke(___CurrentFacingOrder, new object[] { ____mainFormation.Direction });
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch("OnBehaviorActivatedAux")]
            static void PostfixOnBehaviorActivatedAux(ref Formation ___formation)
            {
                ___formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLoose;
                ___formation.FormOrder = FormOrder.FormOrderWide;
            }
        }

        [HarmonyPatch(typeof(BehaviorDefend))]
        class OverrideBehaviorDefend
        {
            static WorldPosition medianPositionOld;

            [HarmonyPostfix]
            [HarmonyPatch("CalculateCurrentOrder")]
            static void PostfixCalculateCurrentOrder(Formation ___formation, ref MovementOrder ____currentOrder, ref Boolean ___IsCurrentOrderChanged)
            {
                if (___formation != null)
                {
                    FormationQuerySystem mainEnemyformation = ___formation?.QuerySystem.ClosestSignificantlyLargeEnemyFormation;
                    if (mainEnemyformation != null)
                    {
                        WorldPosition medianPositionNew = ___formation.QuerySystem.MedianPosition;
                        medianPositionNew.SetVec2(___formation.QuerySystem.AveragePosition);

                        Formation rangedFormation = null;
                        foreach (Formation formation in ___formation.Team.Formations)
                        {
                            if (formation.QuerySystem.IsRangedFormation)
                            {
                                rangedFormation = formation;
                            }
                        }
                        if (rangedFormation != null)
                        {
                            if (___formation.QuerySystem.MedianPosition.AsVec2.Distance(mainEnemyformation.MedianPosition.AsVec2) < (rangedFormation.QuerySystem.MissileRange + 50f))
                            {
                                ____currentOrder = MovementOrder.MovementOrderMove(medianPositionOld);
                                ___IsCurrentOrderChanged = true;
                            }
                            else
                            {
                                medianPositionOld = medianPositionNew;
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(BehaviorHoldHighGround))]
        class OverrideBehaviorHoldHighGround
        {
            static WorldPosition medianPositionOld;

            [HarmonyPostfix]
            [HarmonyPatch("CalculateCurrentOrder")]
            static void PostfixCalculateCurrentOrder(Formation ___formation, ref MovementOrder ____currentOrder, ref Boolean ___IsCurrentOrderChanged)
            {
                if (___formation != null)
                {
                    FormationQuerySystem mainEnemyformation = ___formation?.QuerySystem.ClosestSignificantlyLargeEnemyFormation;
                    if (mainEnemyformation != null)
                    {
                        WorldPosition medianPositionNew = ___formation.QuerySystem.MedianPosition;
                        medianPositionNew.SetVec2(___formation.QuerySystem.AveragePosition);

                        Formation rangedFormation = null;
                        foreach (Formation formation in ___formation.Team.Formations)
                        {
                            if (formation.QuerySystem.IsRangedFormation)
                            {
                                rangedFormation = formation;
                            }
                        }
                        if (rangedFormation != null)
                        {
                            if (___formation.QuerySystem.MedianPosition.AsVec2.Distance(mainEnemyformation.MedianPosition.AsVec2) < (rangedFormation.QuerySystem.MissileRange + 50f))
                            {
                                ____currentOrder = MovementOrder.MovementOrderMove(medianPositionOld);
                                ___IsCurrentOrderChanged = true;
                            }
                            else
                            {
                                medianPositionOld = medianPositionNew;
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(BehaviorScreenedSkirmish))]
        class OverrideBehaviorScreenedSkirmish
        {

            [HarmonyPostfix]
            [HarmonyPatch("CalculateCurrentOrder")]
            static void PostfixCalculateCurrentOrder(Formation ____mainFormation, Formation ___formation, ref MovementOrder ____currentOrder, ref FacingOrder ___CurrentFacingOrder)
            {
                if (____mainFormation != null && ___formation != null)
                {
                    MethodInfo method = typeof(FacingOrder).GetMethod("FacingOrderLookAtDirection", BindingFlags.NonPublic | BindingFlags.Static);
                    method.DeclaringType.GetMethod("FacingOrderLookAtDirection");
                    ___CurrentFacingOrder = (FacingOrder)method.Invoke(___CurrentFacingOrder, new object[] { ____mainFormation.Direction });

                    WorldPosition medianPosition = ____mainFormation.QuerySystem.MedianPosition;
                    //medianPosition.SetVec2(medianPosition.AsVec2 - ____mainFormation.Direction * ((____mainFormation.Depth + ___formation.Depth) * 1.5f));
                    medianPosition.SetVec2(medianPosition.AsVec2 - ____mainFormation.Direction * ((____mainFormation.Depth + ___formation.Depth) * 0.25f + 0.0f));
                    ____currentOrder = MovementOrder.MovementOrderMove(medianPosition);

                }
            }

            [HarmonyPrefix]
            [HarmonyPatch("TickOccasionally")]
            static bool PrefixTickOccasionally(Formation ____mainFormation, ref Formation ___formation, BehaviorScreenedSkirmish __instance, ref MovementOrder ____currentOrder, ref FacingOrder ___CurrentFacingOrder)
            {
                MethodInfo method = typeof(BehaviorScreenedSkirmish).GetMethod("CalculateCurrentOrder", BindingFlags.NonPublic | BindingFlags.Instance);
                method.DeclaringType.GetMethod("CalculateCurrentOrder");
                method.Invoke(__instance, new object[] { });
                //bool flag = formation.QuerySystem.ClosestEnemyFormation == null || _mainFormation.QuerySystem.MedianPosition.AsVec2.DistanceSquared(formation.QuerySystem.ClosestEnemyFormation.MedianPosition.AsVec2) <= formation.QuerySystem.AveragePosition.DistanceSquared(formation.QuerySystem.ClosestEnemyFormation.MedianPosition.AsVec2) || formation.QuerySystem.AveragePosition.DistanceSquared(position.AsVec2) <= (_mainFormation.Depth + formation.Depth) * (_mainFormation.Depth + formation.Depth) * 0.25f;
                //if (flag != _isFireAtWill)
                //{
                //    _isFireAtWill = flag;
                //    formation.FiringOrder = (_isFireAtWill ? FiringOrder.FiringOrderFireAtWill : FiringOrder.FiringOrderHoldYourFire);
                //}
                ___formation.MovementOrder = ____currentOrder;
                ___formation.FacingOrder = ___CurrentFacingOrder;
                return false;
            }

            [HarmonyPostfix]
            [HarmonyPatch("OnBehaviorActivatedAux")]
            static void PostfixOnBehaviorActivatedAux(ref Formation ___formation)
            {
                ___formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLoose;
            }
        }
    }

    [HarmonyPatch(typeof(BehaviorSkirmish))]
    class OverrideBehaviorSkirmish
    {
        private enum BehaviorState
        {
            Approaching,
            Shooting,
            PullingBack
        }

        private static int waitCount = 0;

        [HarmonyPostfix]
        [HarmonyPatch("CalculateCurrentOrder")]
        static void PostfixCalculateCurrentOrder(ref Formation ___formation, BehaviorSkirmish __instance, ref BehaviorState ____behaviorState, ref MovementOrder ____currentOrder)
        {
            switch (____behaviorState)
            {
                case BehaviorState.Shooting:
                    if(waitCount > 50)
                    {
                        if (___formation!= null && ___formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null && ___formation.QuerySystem.MakingRangedAttackRatio < 0.4f && ___formation.QuerySystem.MedianPosition.AsVec2.Distance(___formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.QuerySystem.MedianPosition.AsVec2) > 30f)
                        {
                            WorldPosition medianPosition = ___formation.QuerySystem.MedianPosition;
                            medianPosition.SetVec2(medianPosition.AsVec2 + ___formation.Direction * 5f);
                            ____currentOrder = MovementOrder.MovementOrderMove(medianPosition);
                        }
                        waitCount = 0;
                    }
                    else
                    {
                        waitCount++;
                    }
                   
                    break;
            }
        }
    }

    [HarmonyPatch(typeof(BehaviorMountedSkirmish))]
    class OverrideBehaviorMountedSkirmish
    {
        private struct Ellipse
        {
            private readonly Vec2 _center;

            private readonly float _radius;

            private readonly float _halfLength;

            private readonly Vec2 _direction;

            public Ellipse(Vec2 center, float radius, float halfLength, Vec2 direction)
            {
                _center = center;
                _radius = radius;
                _halfLength = halfLength;
                _direction = direction;
            }

            public Vec2 GetTargetPos(Vec2 position, float distance)
            {
                Vec2 v = _direction.LeftVec();
                Vec2 vec = _center + v * _halfLength;
                Vec2 vec2 = _center - v * _halfLength;
                Vec2 vec3 = position - _center;
                bool flag = vec3.Normalized().DotProduct(_direction) > 0f;
                Vec2 v2 = vec3.DotProduct(v) * v;
                bool flag2 = v2.Length < _halfLength;
                bool flag3 = true;
                if (flag2)
                {
                    position = _center + v2 + _direction * (_radius * (float)(flag ? 1 : (-1)));
                }
                else
                {
                    flag3 = (v2.DotProduct(v) > 0f);
                    Vec2 v3 = (position - (flag3 ? vec : vec2)).Normalized();
                    position = (flag3 ? vec : vec2) + v3 * _radius;
                }
                Vec2 vec4 = _center + v2;
                double num = Math.PI * 2.0 * (double)_radius;
                while (distance > 0f)
                {
                    if (flag2 && flag)
                    {
                        float num2 = ((vec - vec4).Length < distance) ? (vec - vec4).Length : distance;
                        position = vec4 + (vec - vec4).Normalized() * num2;
                        position += _direction * _radius;
                        distance -= num2;
                        flag2 = false;
                        flag3 = true;
                    }
                    else if (!flag2 && flag3)
                    {
                        Vec2 v4 = (position - vec).Normalized();
                        double num3 = Math.Acos(MBMath.ClampFloat(_direction.DotProduct(v4), -1f, 1f));
                        double num4 = Math.PI * 2.0 * ((double)distance / num);
                        double num5 = (num3 + num4 < Math.PI) ? (num3 + num4) : Math.PI;
                        double num6 = (num5 - num3) / Math.PI * (num / 2.0);
                        Vec2 direction = _direction;
                        direction.RotateCCW((float)num5);
                        position = vec + direction * _radius;
                        distance -= (float)num6;
                        flag2 = true;
                        flag = false;
                    }
                    else if (flag2)
                    {
                        float num7 = ((vec2 - vec4).Length < distance) ? (vec2 - vec4).Length : distance;
                        position = vec4 + (vec2 - vec4).Normalized() * num7;
                        position -= _direction * _radius;
                        distance -= num7;
                        flag2 = false;
                        flag3 = false;
                    }
                    else
                    {
                        Vec2 vec5 = (position - vec2).Normalized();
                        double num8 = Math.Acos(MBMath.ClampFloat(_direction.DotProduct(vec5), -1f, 1f));
                        double num9 = Math.PI * 2.0 * ((double)distance / num);
                        double num10 = (num8 - num9 > 0.0) ? (num8 - num9) : 0.0;
                        double num11 = num8 - num10;
                        double num12 = num11 / Math.PI * (num / 2.0);
                        Vec2 v5 = vec5;
                        v5.RotateCCW((float)num11);
                        position = vec2 + v5 * _radius;
                        distance -= (float)num12;
                        flag2 = true;
                        flag = true;
                    }
                }
                return position;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("CalculateCurrentOrder")]
        static void PostfixCalculateCurrentOrder(ref Formation ___formation, BehaviorMountedSkirmish __instance, ref bool ____engaging, ref MovementOrder ____currentOrder)
        {
            WorldPosition position = ___formation.QuerySystem.MedianPosition;
            if (___formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation == null)
            {
                position.SetVec2(___formation.QuerySystem.AveragePosition);
            }
            else
            {
                bool num = (___formation.QuerySystem.AverageAllyPosition - ___formation.Team.QuerySystem.AverageEnemyPosition).LengthSquared <= 3600f;
                bool engaging = ____engaging;
                engaging = (____engaging = (num || ((!____engaging) ? ((___formation.QuerySystem.AveragePosition - ___formation.QuerySystem.AverageAllyPosition).LengthSquared <= 3600f) : (!(___formation.QuerySystem.UnderRangedAttackRatio > ___formation.QuerySystem.MakingRangedAttackRatio) && ((!___formation.QuerySystem.FastestSignificantlyLargeEnemyFormation.IsCavalryFormation && !___formation.QuerySystem.FastestSignificantlyLargeEnemyFormation.IsRangedCavalryFormation) || (___formation.QuerySystem.AveragePosition - ___formation.QuerySystem.FastestSignificantlyLargeEnemyFormation.MedianPosition.AsVec2).LengthSquared / (___formation.QuerySystem.FastestSignificantlyLargeEnemyFormation.MovementSpeed * ___formation.QuerySystem.FastestSignificantlyLargeEnemyFormation.MovementSpeed) >= 16f)))));
                if (!____engaging)
                {
                    position = new WorldPosition(Mission.Current.Scene, new Vec3(___formation.QuerySystem.AverageAllyPosition, ___formation.Team.QuerySystem.MedianPosition.GetNavMeshZ() + 100f));
                }
                else
                {
                    Vec2 vec = (___formation.QuerySystem.AveragePosition - ___formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.AveragePosition).Normalized().LeftVec();
                    FormationQuerySystem closestSignificantlyLargeEnemyFormation = ___formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation;
                    float num2 = 50f + (___formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.Width + ___formation.Depth) * 0.5f;
                    float num3 = 0f;
                    Formation formation = ___formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation;
                    for (int i = 0; i < Mission.Current.Teams.Count; i++)
                    {
                        Team team = Mission.Current.Teams[i];
                        if (!team.IsEnemyOf(___formation.Team))
                        {
                            continue;
                        }
                        for (int j = 0; j < team.FormationsIncludingSpecialAndEmpty.Count; j++)
                        {
                            Formation formation2 = team.FormationsIncludingSpecialAndEmpty[j];
                            if (formation2.CountOfUnits > 0 && formation2.QuerySystem != closestSignificantlyLargeEnemyFormation)
                            {
                                Vec2 v = formation2.QuerySystem.AveragePosition - closestSignificantlyLargeEnemyFormation.AveragePosition;
                                float num4 = v.Normalize();
                                if (vec.DotProduct(v) > 0.8f && num4 < num2 && num4 > num3)
                                {
                                    num3 = num4;
                                    formation = formation2;
                                }
                            }
                        }
                    }
                    if (___formation.QuerySystem.RangedCavalryUnitRatio > 0.95f && ___formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation == formation)
                    {
                        ____currentOrder = MovementOrder.MovementOrderCharge;
                        return;
                    }
                    bool flag = formation.QuerySystem.IsCavalryFormation || formation.QuerySystem.IsRangedCavalryFormation;
                    float num5 = flag ? 35f : 20f;
                    num5 += (formation.Depth + ___formation.Width) * 0.25f;
                    //num5 = Math.Min(num5, ___formation.QuerySystem.MissileRange - ___formation.Width * 0.5f);
                    Ellipse ellipse = new Ellipse(formation.QuerySystem.MedianPosition.AsVec2, num5, formation.Width * 0.25f * (flag ? 1.5f : 1f), formation.Direction);
                    position.SetVec2(ellipse.GetTargetPos(___formation.QuerySystem.AveragePosition, 20f));
                }
            }
            ____currentOrder = MovementOrder.MovementOrderMove(position);
        }

        [HarmonyPostfix]
        [HarmonyPatch("GetAiWeight")]
        static void PostfixGetAiWeight(ref Formation ___formation,  ref float __result)
        {
            int mountedSkirmishersCount = 0;
            if(___formation != null)
            {
                PropertyInfo property = typeof(Formation).GetProperty("arrangement", BindingFlags.NonPublic | BindingFlags.Instance);
                property.DeclaringType.GetProperty("arrangement");
                IFormationArrangement arrangement = (IFormationArrangement)property.GetValue(___formation);

                FieldInfo field = typeof(LineFormation).GetField("_allUnits", BindingFlags.NonPublic | BindingFlags.Instance);
                field.DeclaringType.GetField("_allUnits");
                List<IFormationUnit> agents = (List<IFormationUnit>)field.GetValue(arrangement);
                foreach(Agent agent in agents.ToList())
                {
                    bool ismountedSkrimisher = false;
                    for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
                    {
                        if (agent.Equipment != null && !agent.Equipment[equipmentIndex].IsEmpty)
                        {
                            if (agent.Equipment[equipmentIndex].Item.Type == ItemTypeEnum.Thrown && agent.MountAgent != null)
                            {
                                ismountedSkrimisher = true;
                            }
                        }
                    }
                    if (ismountedSkrimisher)
                    {
                        mountedSkirmishersCount++;
                    }
                }

                float mountedSkirmishersRatio = (float)mountedSkirmishersCount / (float)___formation.CountOfUnits;
                if (mountedSkirmishersRatio > 0.6f)
                {
                    //___formation.AI.SetBehaviorWeight<BehaviorProtectFlank>(0f);
                    __result = 5f;
                }
                else
                {
                    //___formation.AI.SetBehaviorWeight<BehaviorProtectFlank>(1f);
                    __result = 1f;
                }
            }
        }
    }
}