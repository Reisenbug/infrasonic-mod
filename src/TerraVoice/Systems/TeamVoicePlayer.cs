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
        Main.NewText($"[InfraSonic] 语音频道已按队伍重置为 {Player.team}。频道对应：0 无队伍、1 红队、2 绿队、3 蓝队、4 黄队、5 粉队。可在语音面板中自行修改。");
    }
}
