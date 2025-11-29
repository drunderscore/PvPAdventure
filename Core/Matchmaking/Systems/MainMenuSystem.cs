using MonoMod.Cil;
using PvPAdventure.Core.Helpers;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.Matchmaking.Systems
{
    public class MainMenuSystem : ModSystem
    {
        public static int ExtraPvpButtons = 1;

        public override void Load()
        {
            Main.QueueMainThreadAction(() => IL_Main.DrawMenu += InjectPvpButton);
        }

        public override void Unload()
        {
            Main.QueueMainThreadAction(() => IL_Main.DrawMenu -= InjectPvpButton);
        }

        private void InjectPvpButton(ILContext il)
        {
            IL.Edit(il, c =>
            {
                int array9Index = -1;
                int array7Index = -1;
                int offYIndex = -1;
                int spacingIndex = -1;
                int num9Index = -1;
                int num11Index = -1;

                if (!c.TryGotoNext(MoveType.After,
                        i => i.MatchLdarg(0),
                        i => i.MatchLdarg(0),
                        i => i.MatchLdfld<Main>("selectedMenu"),
                        i => i.MatchLdloc(out array9Index),
                        i => i.MatchLdloc(out array7Index),
                        i => i.MatchLdloca(out offYIndex),
                        i => i.MatchLdloca(out spacingIndex),
                        i => i.MatchLdloca(out num9Index),
                        i => i.MatchLdloca(out num11Index),
                        i => i.MatchCall(out var m)
                             && m.DeclaringType.FullName == "Terraria.ModLoader.UI.Interface"
                             && m.Name == "AddMenuButtons"))
                {
                    return;
                }

                c.Index = 0;

                int num11InitIndex = -1;
                int num2Index = -1;
                int num5Index = -1;
                int num4Index = -1;

                if (!c.TryGotoNext(MoveType.After,
                        i => i.MatchLdcI4(0),
                        i => i.MatchStloc(out num11InitIndex),
                        i => i.MatchLdcI4(220),
                        i => i.MatchStloc(out num2Index),
                        i => i.MatchLdcI4(7),
                        i => i.MatchStloc(out num5Index),
                        i => i.MatchLdcI4(52),
                        i => i.MatchStloc(out num4Index)))
                {
                    return;
                }

                c.EmitLdloc(array9Index);
                c.EmitLdloca(num11InitIndex);
                c.EmitLdloca(num5Index);

                c.EmitDelegate((string[] labels, ref int num11, ref int num5) =>
                {
                    if (labels == null)
                        return;

                    int count = MainMenuSystem.ExtraPvpButtons;
                    for (int i = 0; i < count; i++)
                    {
                        if (num11 < 0 || num11 >= labels.Length)
                            break;

                        labels[num11] = "PvP Adventure Matchmaking";
                        num11++;
                        num5++;
                    }
                });
            });
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            base.ModifyInterfaceLayers(layers);
        }
    }
}
