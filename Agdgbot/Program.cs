using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IA;
using Discord;
using System.IO;
using Discord.WebSocket;
using IA.SDK;
using IA.Node;
using IA.SDK.Interfaces;
using IA.Events;

namespace Agdgbot
{
    class Program
    {
        static IDiscordRole colorStart = null;
        static IDiscordRole profStart = null;
        static IDiscordRole mutedRole = null;

        static ulong startColorId = 0;
        static ulong startProfId = 0;
        static ulong mutedRoleId = 0;

        static string idFilePath = Directory.GetCurrentDirectory() + "/ids.txt";

        List<ulong> ColorIds = new List<ulong>();
        List<ulong> ProfeciencyIds = new List<ulong>();

        static Bot bot;

        static uint shitpostsDoneWithMentions = 0;
        static uint messagesRecieved = 0;

        static DateTime uptime;

        static void Main(string[] args) => new Program().Start().GetAwaiter().GetResult();

        async Task Start()
        {
            uptime = DateTime.Now;

            bot = new Bot(x =>
            {
                x.Name = "Aggy";
                x.Token = GetToken();
                x.Prefix = PrefixValue.Set(">");
                x.ShardCount = 1;
            });

            bot.AddDeveloper(121919449996460033);

            ModuleInstance module_colors = new ModuleInstance(module =>
            {
                module.name = "colors";
                module.events = new List<CommandEvent>()
                {
                    new CommandEvent(x =>
                    {
                        x.name = "setcolor";
                        x.accessibility = EventAccessibility.ADMINONLY;
                        x.processCommand = async (e, arg) =>
                        {
                            ulong colorid = 0;
                            colorid = ulong.Parse(arg.Trim('<', '>', '@', '&'));
                            colorStart = e.Guild.GetRole(colorid);
                            await Task.CompletedTask;
                        };
                    }),
                    new CommandEvent(x =>
                    {
                        x.name = "sortcolors";
                        x.accessibility = EventAccessibility.ADMINONLY;
                        x.processCommand = async (e, arg) =>
                        {
                            IGuildUser eg = (e.Author as IGuildUser);

                            int lowestposition = 999;
                            int highestposition = 0;

                            List<IRole> colors = new List<IRole>();
                            foreach(IRole r in eg.Guild.Roles)
                            {
                                if (r.Position < colorStart.Position)
                                {
                                    if (r.Color.ToString() != "#0")
                                    {
                                        colors.Add(r);
                                        if (r.Position < lowestposition)
                                        {
                                            lowestposition = r.Position;
                                        }
                                        if (r.Position > highestposition)
                                        {
                                            highestposition = r.Position;
                                        }
                                    }
                                }
                            }

                            List<IRole> sortedList = colors.OrderBy(c => ColorToHSV(System.Drawing.Color.FromArgb(c.Color.R, c.Color.B, c.Color.G))).ToList();

                            for (int i = 0; i < colors.Count; i++)
                            {
                                await sortedList[i].ModifyAsync(role =>
                                {
                                    role.Position = lowestposition + i;
                                });
                            }

                            await e.Channel.SendMessage("Sorted! " + lowestposition);
                        };
                    }),
                    new CommandEvent(x =>
                    {
                        x.name = "color";
                  //     x.metadata.description = "Set a color based on colors available.";
                 //       x.metadata.usage = new string[] { ">color Cyan" };
                        x.processCommand = async (e, arg) =>
                        {
                            ulong colorid = 0;
                            IDiscordRole r = null;

                            if(e.MentionedRoleIds.Count > 0)
                            {
                                colorid = ulong.Parse(arg.Trim('<', '>', '@', '&'));
                                r = e.Guild.GetRole(colorid);
                            }
                            else if(!string.IsNullOrEmpty(arg))
                            {
                                foreach(IDiscordRole role in e.Guild.Roles)
                                {
                                    if(role.Name.ToLower() == arg.ToLower())
                                    {
                                        r = role;
                                        break;
                                    }
                                }
                            }

                            if (r != null)
                            {
                                if(colorStart == null || profStart == null)
                                {
                                    EmbedBuilder embed = new EmbedBuilder();
                                    embed.Title = "Error";
                                    embed.Description = "color start or profession start aren't defined";
                                    embed.Color = new Color(1, 0, 0);

                                    IDiscordEmbed em = new RuntimeEmbedBuilder(embed);

                                    await e.Channel.SendMessage(em);
                                    return;
                                }

                                if (r.Position < colorStart.Position && r.Position > profStart.Position)
                                {
                                    if(r.Name == "Gold")
                                    {
                                        await e.Channel.SendMessage("This color requires a 4chan plus account.");
                                        return;
                                    }

                                    List<IDiscordRole> deleteRoles = new List<IDiscordRole>();

                                    foreach (ulong roleId in e.Author.RoleIds)
                                    {
                                        IDiscordRole role = e.Guild.GetRole(roleId);

                                        if (role.Position < colorStart.Position && role.Position > profStart.Position)
                                        {
                                            deleteRoles.Add(role);
                                        }
                                    }
                                    await e.Author.RemoveRolesAsync(deleteRoles);

                                    await Task.Delay(50);

                                    await e.Author.AddRoleAsync(r);

                                    await e.Author.SendMessage($"You're {r.Name} now!");
                                }
                                else
                                {
                                    await e.Channel.SendMessage("This is not a color, you fuck.");
                                }
                            }
                            else
                            {
                                await e.Channel.SendMessage("Please mention or name a color.");
                            }
                        };
                    })
                };
            });

            ModuleInstance module_prof = new ModuleInstance(module =>
            {
                module.name = "professions";
                module.events = new List<CommandEvent>()
                {
                    new CommandEvent(x =>
                    {
                        x.name = "setprof";
                        x.accessibility = EventAccessibility.ADMINONLY;
                        x.processCommand = async (e, arg) =>
                        {
                            ulong colorid = 0;
                            try
                            {
                                colorid = ulong.Parse(arg.Trim('<', '>', '@', '&'));
                                profStart = e.Guild.GetRole(colorid);
                            }
                            catch
                            {
                                await e.Author.SendMessage("Failed to parse link.");
                            }
                        };
                    }),
                    new CommandEvent(x =>
                    {
                        x.name = "addskill";
                        //x.description = "Adds a skill based on skills available.";
                        //x.usage = new string[] { ">addskill Unity" };
                        x.processCommand = async (e, arg) =>
                        {
                            ulong colorid = 0;
                            IDiscordRole r = null;

                            if(e.MentionedRoleIds.Count > 0)
                            {
                                colorid = e.MentionedRoleIds.First();
                                r = e.Guild.GetRole(colorid);
                            }
                            else
                            {
                                r = e.Guild.Roles.Find(z => { return z.Name.ToLower() == arg.ToLower(); });
                            }

                            if (r != null)
                            {
                                if (r.Position < profStart.Position)
                                {
                                    await e.Author.AddRoleAsync(r);

                                }
                            }

                        };
                    }),
                    new CommandEvent(x =>
                    {
                        x.name = "removeskill";
                        x.processCommand = async (e, arg) =>
                        {
                            ulong colorid = 0;
                            IDiscordRole r = null;

                            if(e.MentionedRoleIds.Count > 0)
                            { 
                                colorid = e.MentionedRoleIds.First();
                                r = e.Guild.GetRole(colorid);
                            }
                            else
                            {
                                r = e.Guild.Roles.Find(z => { return z.Name.ToLower() == arg.ToLower(); });
                            }

                            if (r != null)
                            {
                                if (r.Position < profStart.Position)
                                {
                                    await e.Author.RemoveRoleAsync(r);

                                    IDiscordEmbed embed = e.CreateEmbed();
                                    embed.Title = "SUCCESS!";
                                    embed.Description = $"Added skill `{ r.Name }`";
                                    embed.Color = new IA.SDK.Color(0, 1, 0);

                                    await e.Author.SendMessage(embed);
                                }
                            }

                        };
                    }),
                };
            });

            new Module(module_prof).InstallAsync(bot).GetAwaiter().GetResult();
            new Module(module_colors).InstallAsync(bot).GetAwaiter().GetResult();


            //// set muted
            //bot.Events.AddCommandEvent(x =>
            //{
            //    x.name = "setmute";
            //    x.accessibility = EventAccessibility.ADMINONLY;
            //    x.processCommand = async (e, arg) =>
            //    {
            //        if (e.MentionedRoleIds.Count > 0)
            //        {
            //            mutedRole = await e.Guild.GetRole(e.MentionedRoleIds.First());
            //            await e.Channel.SendMessage("Set muted role.");
            //            return;
            //        }
            //        await e.Channel.SendMessage("Failed to set muted role.");
            //    };
            //});

            //// save
            //bot.Events.AddCommandEvent(x =>
            //{
            //    x.name = "save";
            //    x.accessibility = EventAccessibility.ADMINONLY;
            //    x.processCommand = async (e, arg) =>
            //    {
            //        StreamWriter sw = new StreamWriter(idFilePath);
            //        sw.WriteLine(colorStart.Id);
            //        sw.WriteLine(profStart.Id);
            //        sw.Flush();
            //        sw.Close();
            //        await Task.CompletedTask;
            //    };
            //});

            //// show dividers
            //bot.Events.AddCommandEvent(x =>
            //{
            //    x.name = "showdividers";
            //    x.accessibility = EventAccessibility.ADMINONLY;
            //    x.processCommand = async (e, arg) =>
            //    {
            //        await e.Channel.SendMessage(colorStart.Id + " - " + colorStart.Name + "\n" + profStart.Id + " - " + profStart.Name);
            //    };
            //});

            //// create color
            //bot.Events.AddCommandEvent(x =>
            //{
            //    x.name = "createcolor";
            //    x.accessibility = EventAccessibility.ADMINONLY;
            //    x.processCommand = async (e, arg) =>
            //    {
            //        IGuildUser eg = (e.Author as IGuildUser);

            //        string[] allArgs = arg.Split(' ');
            //        string name = allArgs[0];
            //        int color = Convert.ToInt32(allArgs[1].Trim('#'), 16);
                    

            //        IRole r = await eg.Guild.CreateRoleAsync(name, null, new Color((uint)color), false);
            //        await r.ModifyAsync(z => z.Position = colorStart.Position - 1);
            //        await e.Channel.SendMessage($"Created Color '{r.Name}' with '{r.Color}'");
            //    };
            //});

            //// createskill
            //bot.Events.AddCommandEvent(x =>
            //{
            //    x.name = "createskill";
            //    x.accessibility = EventAccessibility.ADMINONLY;
            //    x.processCommand = async (e, arg) =>
            //    {
            //        IGuildUser eg = (e.Author as IGuildUser);

            //        IRole r = await eg.Guild.CreateRoleAsync(arg, null, Color.Default, false);
            //        await r.ModifyAsync(skill => skill.Position = profStart.Position - 1);
            //        await e.Channel.SendMessage($"Created Skill '{r.Name}'");
            //    };
            //});

            //// check color
            //bot.Events.AddCommandEvent(x =>
            //{
            //    x.name = "checkcolor";
            //    x.processCommand = async (e, arg) =>
            //    {
            //        IGuildUser eg = (e.Author as IGuildUser);

            //        ulong colorid = 0;
            //        IRole r = null;

            //        try
            //        {
            //            colorid = ulong.Parse(arg.Trim('<', '>', '@', '&'));
            //            r = eg.Guild.GetRole(colorid);
            //        }
            //        catch
            //        {
            //            try
            //            {
            //                r = eg.Guild.Roles.First(role => role.Name == arg);
            //            }
            //            catch
            //            {
            //                await e.Channel.SendMessage("Please add the color you want.");
            //                return;
            //            }
            //        }
            //        if (r != null)
            //        {
                        
            //            await e.Channel.SendMessage(r.Name + " is " + ((r.Color.ToString() != "#0") ? "a color" : "NOT a " + ((r.Name == "Unity")?"engine":"color")));
            //            if(r.Color.ToString() != "#0")
            //            {
            //                await e.Channel.SendMessage("instead, it is " + r.Color.ToString());
            //            }
            //        }
            //    };
            //});

            //// statistics
            //bot.Events.AddCommandEvent(x =>
            //{
            //    x.name = "stats";
            //    x.accessibility = EventAccessibility.ADMINONLY;
            //    x.processCommand = async (e, arg) =>
            //    {
            //        int userCount = (await (e.Author as IGuildUser).Guild.GetUsersAsync()).Count;

            //        await e.Channel.SendMessage($@"
            //            Messages Recieved: {messagesRecieved}\n
            //            Amount were shitposts involving me: {shitpostsDoneWithMentions}\n\n
            //            Uptime: {(DateTime.Now - uptime).ToString().Split('.')[0]}\n\n
            //            Total Users: {userCount}");
            //    };
            //});

            //// purge
            //bot.Events.AddCommandEvent(x =>
            //{
            //    x.name = "purge";
            //    x.accessibility = EventAccessibility.ADMINONLY;
            //    x.processCommand = async (e, arg) =>
            //    {
            //       await bot.Client.GetGuild((e as IGuildUser).GuildId).PruneUsersAsync(1);
            //    };
            //});

            //// mute
            //bot.Events.AddCommandEvent(x =>
            //{
            //    x.name = "mute";
            //    x.processCommand = async (e, arg) =>
            //    {
            //        if (e.MentionedUserIds.Count() > 0 && e.Content.Split(' ').Length > 1)
            //        {
            //            int minutes = int.Parse(e.Content.Split(' ')[2]);

            //            IDiscordUser mentionedUser = await e.Guild.GetUserAsync(e.MentionedUserIds.ElementAt(0));

            //            await mentionedUser.AddRoleAsync(mutedRole);
            //            await e.Channel.SendMessage($"muted `{ (e.Guild.GetUserAsync(e.MentionedUserIds.First())).GetAwaiter().GetResult().Username }` for `{minutes}` minutes");
            //            await Task.Delay(minutes * 60000);
            //            await mentionedUser.RemoveRoleAsync(mutedRole);

            //        }
            //    };
            //});

            //// help 
            //bot.Events.AddCommandEvent(x =>
            //{
            //    x.name = "help";
            //    x.processCommand = async (e, arg) =>
            //    {
            //        await e.Author.SendMessage(await bot.Events.ListCommands(e));
            //    };
            //});

            // Cleverbot
            bot.Events.AddMentionEvent(x =>
            {
                x.name = "cleverbot";
                x.checkCommand = (e, a, c) =>
                {
                    SocketSelfUser me = bot.Client.CurrentUser;
                    return e.Content.StartsWith($"<@!{me.Id}>") || e.Content.StartsWith($"<@{me.Id}>") && e.Channel.Id == 121566911837241344 || e.Channel.Name == "test";
                };
                x.processCommand = async (e, arg) =>
                {
                    Log.Message(arg);
                    await e.Channel.SendMessage(await Node.RunAsync("c", arg));
                };
            });

            bot.Client.GuildAvailable += Client_GuildAvailable;

            await bot.ConnectAsync();
        }

        /// <summary>
        /// gets token from settings.cnf
        /// </summary>
        /// <returns>string with for a key to access the bot account</returns>
        public string GetToken()
        {
            //Loads Token
            if (!File.Exists(Directory.GetCurrentDirectory() + "/settings.cnf"))
            {
                File.Create(Directory.GetCurrentDirectory() + "/settings.cnf").Close();
            }
            StreamReader sr = new StreamReader(Directory.GetCurrentDirectory() + "/settings.cnf");
            string token = sr.ReadLine();
            sr.Close();
            return token;
        }

        public static float ColorToHSV(System.Drawing.Color color)
        {
            int max = Math.Max(color.R, Math.Max(color.G, color.B));
            int min = Math.Min(color.R, Math.Min(color.G, color.B));

            if(color.GetSaturation() == 0)
            {
                return 360 + color.GetSaturation();
            }
            return color.GetHue();
        }

        private async Task Client_GuildAvailable(SocketGuild e)
        {
            RuntimeGuild g = new RuntimeGuild(e);

            if (e.Id == 121565307515961346)
            {
                if (File.Exists(idFilePath))
                {
                    StreamReader sr = new StreamReader(idFilePath);
                    startColorId = ulong.Parse(sr.ReadLine());
                    colorStart = g.GetRole(startColorId);

                    startProfId = ulong.Parse(sr.ReadLine());
                    profStart = g.GetRole(startProfId);

                    mutedRoleId = ulong.Parse(sr.ReadLine());
                    mutedRole = g.GetRole(mutedRoleId);
                }
                else
                {
                    Log.Warning("No ID's loaded.");
                }
            }
            await Task.CompletedTask;
        }

        private static async Task Client_Ready()
        {
            await bot.Client.SetGameAsync("your demo");
        }
    }
}
