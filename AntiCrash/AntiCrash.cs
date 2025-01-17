using System;
using System.Linq;
using System.Text.RegularExpressions;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using CommonGround.Configuration;

namespace AntiCrash;

[ApiVersion(2, 1)]
public class AntiCrash : TerrariaPlugin
{
    public override string Name => "AntiCrash";
    /// The name of the plugin
    
    public override Version Version => new Version(1, 1, 5);
    /// The version of the plugin
    
    public override string Author => "Melton";
    /// The author of the plugin
    
    public override string Description => "A TShock plugin that attempts to prevent various crash exploits.";
    /// The Description of the plugin

    private AntiCrashConfig Config;

    public AntiCrash(Main game) : base(game)
    { }

    public override void Initialize()
    {
        //Create a new config if one does not exist yet and read them
        Config = PluginConfiguration.Load<AntiCrashConfig>();

        ServerApi.Hooks.ServerChat.Register(this, OnChat);
        ServerApi.Hooks.ServerJoin.Register(this, OnJoin);
        TShockAPI.Hooks.GeneralHooks.ReloadEvent += OnReload;

        if (!Config.Enabled)
            return;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ServerApi.Hooks.ServerChat.Deregister(this, OnChat);
            ServerApi.Hooks.ServerJoin.Deregister(this, OnJoin);
            TShockAPI.Hooks.GeneralHooks.ReloadEvent -= OnReload;
        }
        
        base.Dispose(disposing);
    }

    /// called everytime server receives a chat message before being sent to clients
    public void OnChat(ServerChatEventArgs args)
    {
        if (args.Handled)
            return;

        string message = args.Text;
        bool triggered = false;

        if (TShock.Players[args.Who] == null) return;
        /// If a player doesn't exist or is null then return
        /// This is to prevent error when a player doesn't exist.
        
        if (message.Split(" ").Any(substring => substring.Length >= Config.MaxMessageLengthWithoutSpaces))
        {
            TShock.Players[args.Who].Kick("Sent a message with excessive substring length", true);
            /// If sent a message contains a substring with length greater than 50
            /// This is the first blocker for blocking long and cted messages
            /// Kick the player with a kick message in chat (optional: true is on, if you want to not kick an Admin set the true to false)
            
            triggered = true;
        }
        else if (ContainsBadCT(message))
        {
            if (!Config.AllowAntiCT) 
                return;
            TShock.Players[args.Who].Kick("Badly formatted controls touch tag pattern", true);
            /// Else if the message contains a bad character: @"\[ct:(1|7),(\d*)\]
            /// This is the second blocker for blocking long and cted messages
            /// Kick the player with a kick message in chat (optional: true is on, if you want to not kick an Admin set the true to false)
            
            triggered = true;
        }
        else if (ShortBadCT(message))
        {
            if (!Config.AllowAntiCT) 
                return;
            TShock.Players[args.Who].Kick("Badly short formatted ct tag pattern", true);
            /// Else if the message has a short CT: [ct:7,5456]
            /// This is for blocking short cted messages
            /// Kick the player with a kick message in chat (optional: if you dont want to kick everyone even Admin set the true to false)

            triggered = true;
        }
        
        if (triggered)
        {
            args.Handled = true;
        }
    }
    private static bool ContainsBadCT(string message)
    {
        string ctPattern = @"\[ct:(1|7),(\d*)\]";
        /// The ct pattern

        if (Regex.IsMatch(message, @"\[ct:(1|7),$"))
        {
            return true;
        }

        MatchCollection matches = new Regex(ctPattern).Matches(message);

        foreach (Match match in matches)
        {
            /// if the message they sent matches in the ct pattern
            string itemIDstring = match.Groups[2].Value;
            int itemID;
            if (int.TryParse(itemIDstring, out itemID))
            {
                if (itemID < 0 || itemID >= 5456)
                {
                    return true;
                }
            }
        }
        return false;
    }
    private static bool ShortBadCT(string message)
    {
        return message.Contains("5456");
        /// Check for exact phrase
        /// return true if it contains the phrase
    }

    public void OnJoin(JoinEventArgs args)
    {
        var player = TShock.Players[args.Who];
        if (player.Name.Contains("5456"))
        {
            player.Kick("Your name contains a bad character! Please change it to something else.", true);
            /// If the player contains 5456 then kick
        }
    }

    public void OnReload(ReloadEventArgs args)
    {
        Config = PluginConfiguration.Load<AntiCrashConfig>();

        if (!Config.Enabled)
        {
            ServerApi.Hooks.ServerChat.Deregister(this, OnChat);
            ServerApi.Hooks.ServerJoin.Deregister(this, OnJoin);
        }
        else
        {
            ServerApi.Hooks.ServerChat.Register(this, OnChat);
            ServerApi.Hooks.ServerJoin.Register(this, OnJoin);
        }
    }
}

/// If you want to contact me, here's my information!
/// Discord: itzmelton (Melton)
/// Github: https://github.com/ItzMelton
/// Twitter: https://twitter.com/MeltonTan
/// TCF (Terraria Community Forum): https://forums.terraria.org/index.php?members/itzmelton.317788/
