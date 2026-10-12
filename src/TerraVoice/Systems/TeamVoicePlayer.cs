using Terraria;
using Terraria.ModLoader;
using TerraVoice.IO;

namespace TerraVoice.Systems;

[Autoload(Side = ModSide.Client)]
internal sealed class TeamVoicePlayer : ModPlayer
{
    private int previousTeam = -1;

    public override void OnEnterWorld() => previousTeam = -1;

    public override void PostUpdate()
    {
        if (Player.whoAmI != Main.myPlayer || previousTeam == Player.team)
            return;

        previousTeam = Player.team;
        PersistentDataStoreSystem.GetDataStore<UserDataStore>().Channel.Value = Player.team;
    }
}
