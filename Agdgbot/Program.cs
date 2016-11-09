using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IA;
using Discord;
using System.IO;
using IA.Events;
using IA.Node;
using Discord.WebSocket;
using IA.SDK;

namespace Agdgbot
{
    class Program
    {
        static IRole colorStart;
        static IRole profStart;
        static IRole mutedRole;

        static ulong startColorId;
        static ulong startProfId;
        static ulong mutedRoleId;

        static string idFilePath = Directory.GetCurrentDirectory() + "/ids.txt";

        List<ulong> ColorIds = new List<ulong>();
        List<ulong> ProfeciencyIds = new List<ulong>();

        static Bot bot;

        static uint shitpostsDoneWithMentions = 0;
        static uint messagesRecieved = 0;

        static DateTime uptime;

        static void Main(string[] args) => new Program().Start();

        void Start()
        {
            uptime = DateTime.Now;

            bot = new Bot(x =>
            {
                x.Name = "Aggy";
                x.Token = GetToken();
                x.Prefix = PrefixValue.Mention;
            });

            bot.AddDeveloper(121919449996460033);

            // set color
            bot.Events.AddCommandEvent(x =>
            {
                x.name = "setcolor";
                x.accessibility = EventAccessibility.ADMINONLY;
                x.processCommand = async (e, arg) =>
                {
                    IGuildUser eg = (e.Author as IGuildUser);

                    ulong colorid = 0;
                    colorid = ulong.Parse(arg.Trim('<', '>', '@', '&'));
                    colorStart = eg.Guild.GetRole(colorid);
                    await Task.CompletedTask;
                };
            });

            // sort colors
            bot.Events.AddCommandEvent(x =>
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
            });

            // set prof
            bot.Events.AddCommandEvent(x =>
            {
                x.name = "setprof";
                x.accessibility = EventAccessibility.ADMINONLY;
                x.processCommand = async (e, arg) =>
                {
                    IGuildUser eg = (e.Author as IGuildUser);

                    ulong colorid = 0;
                    try
                    {
                        colorid = ulong.Parse(arg.Trim('<', '>', '@', '&'));
                        profStart = eg.Guild.GetRole(colorid);
                    }
                    catch
                    {
                        await (await e.Author.CreateDMChannelAsync()).SendMessage("Failed to parse link.");
                    }
                };
            });

            // save
            bot.Events.AddCommandEvent(x =>
            {
                x.name = "save";
                x.accessibility = EventAccessibility.ADMINONLY;
                x.processCommand = async (e, arg) =>
                {
                    StreamWriter sw = new StreamWriter(idFilePath);
                    sw.WriteLine(colorStart.Id);
                    sw.WriteLine(profStart.Id);
                    sw.Flush();
                    sw.Close();
                    await Task.CompletedTask;
                };
            });

            // show dividers
            bot.Events.AddCommandEvent(x =>
            {
                x.name = "showdividers";
                x.accessibility = EventAccessibility.ADMINONLY;
                x.processCommand = async (e, arg) =>
                {
                    await e.Channel.SendMessage(colorStart.Id + " - " + colorStart.Name + "\n" + profStart.Id + " - " + profStart.Name);
                };
            });

            // color
            bot.Events.AddCommandEvent(x =>
            {
                x.name = "color";
                x.description = "Set a color based on colors available.";
                x.usage = new string[] { ">color Cyan" };
                x.processCommand = async (e, arg) =>
                {
                    IGuildUser eg = (e.Author as IGuildUser);

                    ulong colorid = 0;
                    IRole r = null;

                    try
                    {
                        colorid = ulong.Parse(arg.Trim('<', '>', '@', '&'));
                        r = eg.Guild.GetRole(colorid);
                    }
                    catch
                    {
                        await e.Channel.SendMessage("Please add the color you want.");
                        return;
                    }

                    if (r != null)
                    {
                        if (r.Position < colorStart.Position && r.Position > profStart.Position)
                        {
                            if(r.Name == "Gold")
                            {
                                await e.Channel.SendMessage("This color requires a 4chan plus account.");
                                return;
                            }

                            List<IRole> deleteRoles = new List<IRole>();
                            foreach (IRole role in eg.Guild.Roles)
                            {
                                if (role.Color.ToString() != "#0")
                                {
                                    if (eg.RoleIds.Contains(role.Id))
                                    {
                                        await eg.RemoveRolesAsync(role);
                                    }
                                }
                            }
                            await Task.Delay(100);
                            await eg.AddRolesAsync(r);
                        }
                        else
                        {
                            await e.Channel.SendMessage("This is not a color, you cuck.");
                        }
                    }

                };
            });

            // create color
            bot.Events.AddCommandEvent(x =>
            {
                x.name = "createcolor";
                x.accessibility = EventAccessibility.ADMINONLY;
                x.processCommand = async (e, arg) =>
                {
                    IGuildUser eg = (e.Author as IGuildUser);

                    string[] allArgs = arg.Split(' ');
                    string name = allArgs[0];
                    int color = Convert.ToInt32(allArgs[1].Trim('#'), 16);
                    

                    IRole r = await eg.Guild.CreateRoleAsync(name, null, new Color((uint)color), false);
                    await r.ModifyAsync(z => z.Position = colorStart.Position - 1);
                    await e.Channel.SendMessage($"Created Color '{r.Name}' with '{r.Color}'");
                };
            });

            // createskill
            bot.Events.AddCommandEvent(x =>
            {
                x.name = "createskill";
                x.accessibility = EventAccessibility.ADMINONLY;
                x.processCommand = async (e, arg) =>
                {
                    IGuildUser eg = (e.Author as IGuildUser);

                    IRole r = await eg.Guild.CreateRoleAsync(arg, null, Color.Default, false);
                    await r.ModifyAsync(skill => skill.Position = profStart.Position - 1);
                    await e.Channel.SendMessage($"Created Skill '{r.Name}'");
                };
            });

            // prof
            bot.Events.AddCommandEvent(x =>
            {
                x.name = "addskill";
                x.description = "Adds a skill based on skills available.";
                x.usage = new string[] { ">addskill Unity" };
                x.processCommand = async (e, arg) =>
                {
                    IGuildUser eg = (e.Author as IGuildUser);

                    ulong colorid = 0;
                    IRole r = null;

                    try
                    {
                        colorid = ulong.Parse(arg.Trim('<', '>', '@', '&'));
                        r = eg.Guild.GetRole(colorid);
                    }
                    catch
                    {
                        return;
                    }

                    if (r != null)
                    {
                        if (r.Position < profStart.Position)
                        {
                            await eg.AddRolesAsync(r);
                        }
                    }

                };
            });

            // remove prof
            bot.Events.AddCommandEvent(x =>
            {
                x.name = "removeskill";
                x.description = "Removes a skill based on skills available.";
                x.usage = new string[] { ">removeskill Unity" };
                x.processCommand = async (e, arg) =>
                {
                    IGuildUser eg = (e.Author as IGuildUser);

                    ulong colorid = 0;
                    IRole r = null;
                    try
                    {
                        colorid = ulong.Parse(arg.Trim('<', '>', '@', '&'));
                        r = eg.Guild.GetRole(colorid);
                    }
                    catch
                    {
                        return;
                    }
                    if (r != null)
                    {
                        if (r.Position < profStart.Position)
                        {
                            await eg.RemoveRolesAsync(r);
                        }
                    }

                };
            });

            // check color
            bot.Events.AddCommandEvent(x =>
            {
                x.name = "checkcolor";
                x.processCommand = async (e, arg) =>
                {
                    IGuildUser eg = (e.Author as IGuildUser);

                    ulong colorid = 0;
                    IRole r = null;

                    try
                    {
                        colorid = ulong.Parse(arg.Trim('<', '>', '@', '&'));
                        r = eg.Guild.GetRole(colorid);
                    }
                    catch
                    {
                        try
                        {
                            r = eg.Guild.Roles.First(role => role.Name == arg);
                        }
                        catch
                        {
                            await e.Channel.SendMessage("Please add the color you want.");
                            return;
                        }
                    }
                    if (r != null)
                    {
                        
                        await e.Channel.SendMessage(r.Name + " is " + ((r.Color.ToString() != "#0") ? "a color" : "NOT a " + ((r.Name == "Unity")?"engine":"color")));
                        if(r.Color.ToString() != "#0")
                        {
                            await e.Channel.SendMessage("instead, it is " + r.Color.ToString());
                        }
                    }
                };
            });

            // statistics
            bot.Events.AddCommandEvent(x =>
            {
                x.name = "stats";
                x.accessibility = EventAccessibility.ADMINONLY;
                x.processCommand = async (e, arg) =>
                {
                    int userCount = (await (e.Author as IGuildUser).Guild.GetUsersAsync()).Count;

                    await e.Channel.SendMessage($@"
                        Messages Recieved: {messagesRecieved}\n
                        Amount were shitposts involving me: {shitpostsDoneWithMentions}\n\n
                        Uptime: {(DateTime.Now - uptime).ToString().Split('.')[0]}\n\n
                        Total Users: {userCount}");
                };
            });

            // purge
            bot.Events.AddCommandEvent(x =>
            {
                x.name = "purge";
                x.accessibility = EventAccessibility.ADMINONLY;
                x.processCommand = async (e, arg) =>
                {
                   await bot.Client.GetGuild((e as IGuildUser).GuildId).PruneUsersAsync(1);
                };
            });

            // mute
            bot.Events.AddCommandEvent(x =>
            {
                x.name = "mute";
                x.processCommand = async (e, arg) =>
                {
                    if (e.MentionedUserIds.Count() > 0 && e.Content.Split(' ').Length > 1)
                    {
                        IGuildUser eg = (e.Author as IGuildUser);
                        int minutes = int.Parse(e.Content.Split(' ')[2]);

                        IGuildUser mentionedUser = await e.Channel.GetUserAsync(e.MentionedUserIds.ElementAt(0)) as IGuildUser;

                        await mentionedUser.AddRolesAsync(eg.Guild.Roles.FirstOrDefault(r => r.Name == "muted"));
                        await e.Channel.SendMessage($"muted `{ (e.Channel.GetUserAsync(e.MentionedUserIds.First())).GetAwaiter().GetResult().Username }` for `{minutes}` minutes");
                        await Task.Delay(minutes * 60000);
                        await mentionedUser.RemoveRolesAsync(eg.Guild.Roles.FirstOrDefault(r => r.Name == "muted"));

                    }
                };
            });

            // help 
            bot.Events.AddCommandEvent(x =>
            {
                x.name = "help";
                x.processCommand = async (e, arg) =>
                {
                    await (await e.Author.CreateDMChannelAsync()).SendMessage(await bot.Events.ListCommands(e));
                };
            });

            // Cleverbot
            bot.Events.AddMentionEvent(x =>
            {
                x.name = "cleverbot";
                x.cooldown = 30;
                x.checkCommand = (e, a, c) =>
                {
                    SocketSelfUser me = bot.Client.CurrentUser;
                    return e.Content.StartsWith(me.Mention) && (e.Channel.Id == 121566911837241344 || e.Channel.Id == 226393903216066561);
                };
                x.processCommand = async (e, arg) =>
                {
                    Log.Message(arg);
                    await e.Channel.SendMessage(await Node.RunAsync("c", arg));
                };
            });

            bot.Client.MessageReceived += Client_MessageReceived;
            bot.Client.Ready += Client_Ready;
            bot.Client.GuildAvailable += Client_GuildAvailable; ;
            bot.Connect();
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

        private async Task Client_MessageReceived(SocketMessage e)
        {
            if(e.MentionedUsers.Contains(bot.Client.CurrentUser))
            {
                shitpostsDoneWithMentions++;
            }
            messagesRecieved++;
        }

        private async Task Client_GuildAvailable(SocketGuild e)
        {
            if (e.Id == 121565307515961346)
            {
                if (File.Exists(idFilePath))
                {
                    StreamReader sr = new StreamReader(idFilePath);
                    startColorId = ulong.Parse(sr.ReadLine());
                    startProfId = ulong.Parse(sr.ReadLine());
                    colorStart = e.GetRole(startColorId);
                    profStart = e.GetRole(startProfId);
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
            await bot.Client.SetGame("your demo");
        }
    }
}
