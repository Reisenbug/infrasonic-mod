using TerraVoice.IO;
using TerraVoice.Systems;
using Terraria.ModLoader.IO;

void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    Console.WriteLine("PASS: " + name);
}

var data = new UserDataStore();
Check(data.MicrophoneEnabled.Value && data.OpenMic.Value && !data.PushToTalk.Value && data.ProximityDistance.Value == 0, "Fresh defaults");
data.LoadGlobal(new TagCompound { ["MicrophoneEnabled"] = false, ["PushToTalk"] = true, ["ProximityDistance"] = 64 });
Check(data.MicrophoneEnabled.Value && data.OpenMic.Value && !data.PushToTalk.Value && data.ProximityDistance.Value == 0, "Old settings migrate once");
data.MicrophoneEnabled.Value = data.OpenMic.Value = false;
data.PushToTalk.Value = true;
data.ProximityDistance.Value = 32;
var saved = new TagCompound();
data.SaveGlobal(saved);
var restored = new UserDataStore();
restored.LoadGlobal(saved);
Check(!restored.MicrophoneEnabled.Value && !restored.OpenMic.Value && restored.PushToTalk.Value && restored.ProximityDistance.Value == 32, "Manual settings survive reload");
PersistentDataStoreSystem.PutDataStore(data);
var player = new TeamVoicePlayer();
for (int team = 0; team <= 5; team++)
{
    player.Player.team = team;
    player.PostUpdate();
    Check(data.Channel.Value == team, "Team channel " + team);
    data.Channel.Value = 9;
    player.PostUpdate();
    Check(data.Channel.Value == 9, "Manual channel persists for team " + team);
}
player.OnEnterWorld();
player.PostUpdate();
Check(data.Channel.Value == 5, "World entry resets channel");
player.Player.whoAmI = 1;
player.Player.team = 2;
player.PostUpdate();
Check(data.Channel.Value == 5, "Remote players cannot change local channel");

namespace Terraria
{
    public sealed class Player { public int whoAmI; public int team; }
    public static class Main { public static int myPlayer; }
}
namespace Terraria.ModLoader
{
    public enum ModSide { Client }
    public sealed class AutoloadAttribute : Attribute { public ModSide Side { get; set; } }
    public class ModPlayer
    {
        public Terraria.Player Player = new();
        public virtual void OnEnterWorld() { }
        public virtual void PostUpdate() { }
    }
}
namespace Terraria.ModLoader.IO
{
    public class TagCompound : Dictionary<string, object> { public T Get<T>(string key) => (T)this[key]; }
}
namespace TerraVoice.IO
{
    public class Ref<T> { public T Value; public Ref(T value = default) => Value = value; }
    public abstract class PersistentDataStore
    {
        public abstract string FileName { get; }
        public abstract void LoadGlobal(TagCompound tag);
        public abstract void SaveGlobal(TagCompound tag);
    }
}
