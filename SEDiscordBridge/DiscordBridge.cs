﻿using DSharpPlus;
using DSharpPlus.Entities;
using System.Threading;
using System.Threading.Tasks;
using Torch.API.Managers;
using Torch.Commands;
using VRage.Game;

namespace SEDiscordBridge
{
    public class DiscordBridge
    {
        private static SEDicordBridgePlugin Plugin;
        private static DiscordClient discord;
        private Thread thread;
        private DiscordGame game;

        private bool ready = false;
        public bool Ready { get => ready; set => ready = value; }

        public DiscordBridge(SEDicordBridgePlugin plugin)
        {
            Plugin = plugin;

            thread = new Thread(() =>
            {
                RegisterDiscord().ConfigureAwait(false).GetAwaiter().GetResult();
            });
            thread.Start();            
        }

        public void Stopdiscord()
        {
            DisconnectDiscord().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private async Task DisconnectDiscord()
        {            
            await discord.DisconnectAsync();
        }

        private Task RegisterDiscord()
        {            
            discord = new DiscordClient(new DiscordConfiguration
            {
                Token = Plugin.Config.BotToken,
                TokenType = TokenType.Bot
            });
            discord.ConnectAsync();

            discord.MessageCreated += Discord_MessageCreated;
            game = new DiscordGame();

            discord.Ready += async e =>
            {
                Ready = true;
                //start message
                if (Plugin.Config.Started.Length > 0)
                    await discord.SendMessageAsync(discord.GetChannelAsync(ulong.Parse(Plugin.Config.StatusChannelId)).Result, Plugin.Config.Started);
            };
            return Task.CompletedTask;
        }

        public void SendStatus(string status)
        {
            if (Ready)
            {
                game.Name = status;
                discord.UpdateStatusAsync(game);
            }            
        }

        public void SendChatMessage(string user, string msg)
        {
            if (Plugin.Config.ChatChannelId.Length > 0)
            {                
                if (user != null)
                {
                    msg = Plugin.Config.Format.Replace("{msg}", msg).Replace("{p}", user);
                }
                discord.SendMessageAsync(discord.GetChannelAsync(ulong.Parse(Plugin.Config.ChatChannelId)).Result, msg);
            }            
        }

        public void SendStatusMessage(string user, string msg)
        {
            if (Plugin.Config.StatusChannelId.Length > 0)
            {
                if (user != null)
                {
                    msg = msg.Replace("{p}", user);
                }
                discord.SendMessageAsync(discord.GetChannelAsync(ulong.Parse(Plugin.Config.StatusChannelId)).Result, msg);
            }                
        }

        private Task Discord_MessageCreated(DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            if (!e.Author.IsBot)
            {
                if (e.Channel.Id.Equals(ulong.Parse(Plugin.Config.ChatChannelId)))
                {
                    string sender = Plugin.Config.ServerName;

                    if (!Plugin.Config.AsServer)
                        sender = e.Author.Username;
                    
                    Plugin.Torch.Invoke(() =>
                    {
                        var manager = Plugin.Torch.CurrentSession.Managers.GetManager<IChatManagerServer>();
                        manager.SendMessageAsOther(Plugin.Config.Format2.Replace("{p}", sender), e.Message.Content, MyFontEnum.White);
                    });                        
                }
                if (e.Channel.Id.Equals(ulong.Parse(Plugin.Config.CommandChannelId)) && e.Message.Content.StartsWith(Plugin.Config.CommandPrefix))
                {
                    string cmd = e.Message.Content.Replace(Plugin.Config.CommandPrefix, "");                  
                    Plugin.Torch.Invoke(() =>
                    {
                        var manager = Plugin.Torch.CurrentSession.Managers.GetManager<CommandManager>();
                        manager.HandleCommandFromServer(cmd);
                    });
                }
            }            
            return Task.CompletedTask;
        }

    }
}
