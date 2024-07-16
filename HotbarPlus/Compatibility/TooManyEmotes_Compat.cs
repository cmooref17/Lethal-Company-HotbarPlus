using TooManyEmotes;
using TooManyEmotes.Patches;

namespace HotbarPlus.Compatibility
{
    internal static class TooManyEmotes_Compat
    {
        public static bool Enabled { get { return Plugin.IsModLoaded("FlipMods.TooManyEmotes"); } }

        public static bool IsLocalPlayerPerformingCustomEmote()
        {
            if (EmoteControllerPlayer.emoteControllerLocal != null && EmoteControllerPlayer.emoteControllerLocal.IsPerformingCustomEmote())
                return true;
            return false;
        }

        public static bool CanMoveWhileEmoting() => ThirdPersonEmoteController.allowMovingWhileEmoting;
    }
}