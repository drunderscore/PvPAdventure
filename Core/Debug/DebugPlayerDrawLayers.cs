using Terraria.DataStructures;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Debug;

// Class used to check which player draw layers to draw on, so we can apply visual effects and modifications to fit our needs.
// Usually 

#if DEBUG
public class DebugPlayerDrawLayers : ModSystem
{
    public override void Load()
    {
        // 01*
        On_PlayerDrawLayers.DrawPlayer_01_2_JimsCloak += Modify_01_2_JimsCloak;
        On_PlayerDrawLayers.DrawPlayer_01_BackHair += Modify_01_BackHair;
        On_PlayerDrawLayers.DrawPlayer_01_3_BackHead += Modify_01_3_BackHead;

        // 02-11
        On_PlayerDrawLayers.DrawPlayer_02_MountBehindPlayer += Modify_02_MountBehindPlayer;
        On_PlayerDrawLayers.DrawPlayer_03_Carpet += Modify_03_Carpet;
        On_PlayerDrawLayers.DrawPlayer_03_PortableStool += Modify_03_PortableStool;
        On_PlayerDrawLayers.DrawPlayer_04_ElectrifiedDebuffBack += Modify_04_ElectrifiedDebuffBack;
        On_PlayerDrawLayers.DrawPlayer_05_ForbiddenSetRing += Modify_05_ForbiddenSetRing;
        On_PlayerDrawLayers.DrawPlayer_05_2_SafemanSun += Modify_05_2_SafemanSun;
        On_PlayerDrawLayers.DrawPlayer_06_WebbedDebuffBack += Modify_06_WebbedDebuffBack;
        On_PlayerDrawLayers.DrawPlayer_07_LeinforsHairShampoo += Modify_07_LeinforsHairShampoo;
        On_PlayerDrawLayers.DrawPlayer_08_Backpacks += Modify_08_Backpacks;
        On_PlayerDrawLayers.DrawPlayer_08_1_Tails += Modify_08_1_Tails;
        On_PlayerDrawLayers.DrawPlayer_09_Wings += Modify_09_Wings;
        On_PlayerDrawLayers.DrawPlayer_10_BackAcc += Modify_10_BackAcc;
        On_PlayerDrawLayers.DrawPlayer_11_Balloons += Modify_11_Balloons;

        // 12-26
        On_PlayerDrawLayers.DrawPlayer_12_Skin += Modify_12_Skin;
        On_PlayerDrawLayers.DrawPlayer_13_Leggings += Modify_13_Leggings;
        On_PlayerDrawLayers.DrawPlayer_14_Shoes += Modify_14_Shoes;
        On_PlayerDrawLayers.DrawPlayer_15_SkinLongCoat += Modify_15_SkinLongCoat;
        On_PlayerDrawLayers.DrawPlayer_16_ArmorLongCoat += Modify_16_ArmorLongCoat;
        On_PlayerDrawLayers.DrawPlayer_17_Torso += Modify_17_Torso;
        On_PlayerDrawLayers.DrawPlayer_18_OffhandAcc += Modify_18_OffhandAcc;
        On_PlayerDrawLayers.DrawPlayer_19_WaistAcc += Modify_19_WaistAcc;
        On_PlayerDrawLayers.DrawPlayer_20_NeckAcc += Modify_20_NeckAcc;
        On_PlayerDrawLayers.DrawPlayer_21_Head += Modify_21_Head;
        On_PlayerDrawLayers.DrawPlayer_21_2_FinchNest += Modify_21_2_FinchNest;
        On_PlayerDrawLayers.DrawPlayer_22_FaceAcc += Modify_22_FaceAcc;
        On_PlayerDrawLayers.DrawPlayer_23_MountFront += Modify_23_MountFront;
        On_PlayerDrawLayers.DrawPlayer_24_Pulley += Modify_24_Pulley;
        On_PlayerDrawLayers.DrawPlayer_25_Shield += Modify_25_Shield;
        On_PlayerDrawLayers.DrawPlayer_26_SolarShield += Modify_26_SolarShield;

        // 27-38
        On_PlayerDrawLayers.DrawPlayer_27_HeldItem += Modify_27_HeldItem;
        On_PlayerDrawLayers.DrawPlayer_28_ArmOverItem += Modify_28_ArmOverItem;
        On_PlayerDrawLayers.DrawPlayer_29_OnhandAcc += Modify_29_OnhandAcc;
        On_PlayerDrawLayers.DrawPlayer_30_BladedGlove += Modify_30_BladedGlove;
        On_PlayerDrawLayers.DrawPlayer_31_ProjectileOverArm += Modify_31_ProjectileOverArm;
        On_PlayerDrawLayers.DrawPlayer_32_FrontAcc_BackPart += Modify_32_FrontAcc_BackPart;
        On_PlayerDrawLayers.DrawPlayer_32_FrontAcc_FrontPart += Modify_32_FrontAcc_FrontPart;
        On_PlayerDrawLayers.DrawPlayer_33_FrozenOrWebbedDebuff += Modify_33_FrozenOrWebbedDebuff;
        On_PlayerDrawLayers.DrawPlayer_34_ElectrifiedDebuffFront += Modify_34_ElectrifiedDebuffFront;
        On_PlayerDrawLayers.DrawPlayer_35_IceBarrier += Modify_35_IceBarrier;
        On_PlayerDrawLayers.DrawPlayer_36_CTG += Modify_36_CTG;
        On_PlayerDrawLayers.DrawPlayer_37_BeetleBuff += Modify_37_BeetleBuff;
        On_PlayerDrawLayers.DrawPlayer_38_EyebrellaCloud += Modify_38_EyebrellaCloud;

        // Non-numbered layer present in FixedVanillaLayers
        On_PlayerDrawLayers.DrawPlayer_JimsDroneRadio += Modify_JimsDroneRadio;
    }

    private static void Modify_01_2_JimsCloak(On_PlayerDrawLayers.orig_DrawPlayer_01_2_JimsCloak orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_01_BackHair(On_PlayerDrawLayers.orig_DrawPlayer_01_BackHair orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_01_3_BackHead(On_PlayerDrawLayers.orig_DrawPlayer_01_3_BackHead orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_02_MountBehindPlayer(On_PlayerDrawLayers.orig_DrawPlayer_02_MountBehindPlayer orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_03_Carpet(On_PlayerDrawLayers.orig_DrawPlayer_03_Carpet orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_03_PortableStool(On_PlayerDrawLayers.orig_DrawPlayer_03_PortableStool orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_04_ElectrifiedDebuffBack(On_PlayerDrawLayers.orig_DrawPlayer_04_ElectrifiedDebuffBack orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_05_ForbiddenSetRing(On_PlayerDrawLayers.orig_DrawPlayer_05_ForbiddenSetRing orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_05_2_SafemanSun(On_PlayerDrawLayers.orig_DrawPlayer_05_2_SafemanSun orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_06_WebbedDebuffBack(On_PlayerDrawLayers.orig_DrawPlayer_06_WebbedDebuffBack orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_07_LeinforsHairShampoo(On_PlayerDrawLayers.orig_DrawPlayer_07_LeinforsHairShampoo orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_08_Backpacks(On_PlayerDrawLayers.orig_DrawPlayer_08_Backpacks orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_08_1_Tails(On_PlayerDrawLayers.orig_DrawPlayer_08_1_Tails orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_09_Wings(On_PlayerDrawLayers.orig_DrawPlayer_09_Wings orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_10_BackAcc(On_PlayerDrawLayers.orig_DrawPlayer_10_BackAcc orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_11_Balloons(On_PlayerDrawLayers.orig_DrawPlayer_11_Balloons orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_12_Skin(On_PlayerDrawLayers.orig_DrawPlayer_12_Skin orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_13_Leggings(On_PlayerDrawLayers.orig_DrawPlayer_13_Leggings orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_14_Shoes(On_PlayerDrawLayers.orig_DrawPlayer_14_Shoes orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_15_SkinLongCoat(On_PlayerDrawLayers.orig_DrawPlayer_15_SkinLongCoat orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_16_ArmorLongCoat(On_PlayerDrawLayers.orig_DrawPlayer_16_ArmorLongCoat orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_17_Torso(On_PlayerDrawLayers.orig_DrawPlayer_17_Torso orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_18_OffhandAcc(On_PlayerDrawLayers.orig_DrawPlayer_18_OffhandAcc orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_19_WaistAcc(On_PlayerDrawLayers.orig_DrawPlayer_19_WaistAcc orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_20_NeckAcc(On_PlayerDrawLayers.orig_DrawPlayer_20_NeckAcc orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_21_Head(On_PlayerDrawLayers.orig_DrawPlayer_21_Head orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_21_2_FinchNest(On_PlayerDrawLayers.orig_DrawPlayer_21_2_FinchNest orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_22_FaceAcc(On_PlayerDrawLayers.orig_DrawPlayer_22_FaceAcc orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_23_MountFront(On_PlayerDrawLayers.orig_DrawPlayer_23_MountFront orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_24_Pulley(On_PlayerDrawLayers.orig_DrawPlayer_24_Pulley orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_25_Shield(On_PlayerDrawLayers.orig_DrawPlayer_25_Shield orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_26_SolarShield(On_PlayerDrawLayers.orig_DrawPlayer_26_SolarShield orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_27_HeldItem(On_PlayerDrawLayers.orig_DrawPlayer_27_HeldItem orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_28_ArmOverItem(On_PlayerDrawLayers.orig_DrawPlayer_28_ArmOverItem orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_29_OnhandAcc(On_PlayerDrawLayers.orig_DrawPlayer_29_OnhandAcc orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_30_BladedGlove(On_PlayerDrawLayers.orig_DrawPlayer_30_BladedGlove orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_31_ProjectileOverArm(On_PlayerDrawLayers.orig_DrawPlayer_31_ProjectileOverArm orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_32_FrontAcc_BackPart(On_PlayerDrawLayers.orig_DrawPlayer_32_FrontAcc_BackPart orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_32_FrontAcc_FrontPart(On_PlayerDrawLayers.orig_DrawPlayer_32_FrontAcc_FrontPart orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_33_FrozenOrWebbedDebuff(On_PlayerDrawLayers.orig_DrawPlayer_33_FrozenOrWebbedDebuff orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_34_ElectrifiedDebuffFront(On_PlayerDrawLayers.orig_DrawPlayer_34_ElectrifiedDebuffFront orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_35_IceBarrier(On_PlayerDrawLayers.orig_DrawPlayer_35_IceBarrier orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_36_CTG(On_PlayerDrawLayers.orig_DrawPlayer_36_CTG orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_37_BeetleBuff(On_PlayerDrawLayers.orig_DrawPlayer_37_BeetleBuff orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_38_EyebrellaCloud(On_PlayerDrawLayers.orig_DrawPlayer_38_EyebrellaCloud orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

    private static void Modify_JimsDroneRadio(On_PlayerDrawLayers.orig_DrawPlayer_JimsDroneRadio orig, ref PlayerDrawSet drawInfo)
    {
        orig(ref drawInfo);
    }

}
#endif