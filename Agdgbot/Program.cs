using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IA;
using Discord;
using System.IO;
using unirest_net.http;
using IA.Events;
using IA.Node;
using Discord.WebSocket;

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
        static async Task<uint> nodevs()
        {
            IGuild s = bot.Client.Servers.First(x =>
            {
                return x.Id == 121565307515961346;
            });

            uint outp = 0;

            IGuildUser[] u = (await s.GetUsersAsync()).ToArray();

            foreach (IGuildUser x in u)
            {
                if (x.GetPermissions(await s.GetDefaultChannelAsync())FindRoles("nodev").ElementAt(0) || x.Roles.Count() <= 1) outp++;
            }

            return outp;
        }

        static DateTime uptime;

        static void Main(string[] args) => new Program().Start();

        void Start()
        {
            uptime = DateTime.Now;

            bot = new Bot(x =>
            {
                x.Name = "Aggy";
                x.Token = GetToken();
                x.Identifier = ">";
            });
            bot.AddDeveloper(121919449996460033);
            bot.AddDeveloper(121566705192402944);
            bot.AddDeveloper(101392521376055296);
            bot.AddDeveloper(104252835167748096);
            bot.AddDeveloper(191698267313143808);
            bot.AddDeveloper(101603144827424768);
            bot.AddDeveloper(101389503087792128);

            // set color
            bot.Events.AddCommandEvent(x =>
            {
                x.name = "check";
                x.accessibility = IA.Events.EventAccessibility.ADMINONLY;
                x.processCommand = (e, arg) =>
                {
                    e.Channel.SendMessage("check!");
                };
            });

            // set color
            bot.Events.AddCommandEvent(x =>
            {
                x.name = "setcolor";
                x.accessibility = IA.Events.EventAccessibility.ADMINONLY;
                x.processCommand = (e, arg) =>
                {
                    ulong colorid = 0;
                    colorid = ulong.Parse(arg.Trim('<', '>', '@', '&'));
                    colorStart = e.Server.GetRole(colorid);
                };
            });

            // sort colors
            bot.Events.AddCommandEvent(x =>
            {
                x.name = "sortcolors";
                x.accessibility = EventAccessibility.ADMINONLY;
                x.processCommand = (e, arg) =>
                {
                    int lowestposition = 999;
                    int highestposition = 0;

                    List<IRole> colors = new List<IRole>();
                    foreach(IRole r in e.Guild.Roles)
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

                    List<Role> sortedList = colors.OrderBy(c => ColorToHSV(System.Drawing.Color.FromArgb(c.Color.R, c.Color.B, c.Color.G))).ToList();

                    for (int i = 0; i < colors.Count; i++)
                    {
                        sortedList[i].Edit(null, null, null, null, lowestposition + i);
                    }

                    e.Channel.SendMessage("Sorted! " + lowestposition);
                };
            });

            // set prof
            bot.Events.AddCommandEvent(x =>
            {
                x.name = "setprof";
                x.accessibility = IA.Events.EventAccessibility.ADMINONLY;
                x.processCommand = (e, arg) =>
                {
                    ulong colorid = 0;
                    try
                    {
                        colorid = ulong.Parse(arg.Trim('<', '>', '@', '&'));
                        profStart = e.Server.GetRole(colorid);
                    }
                    catch
                    {
                        e.User.SendMessage("Failed to parse link.");
                    }
                };
            });

            // save
            bot.Events.AddCommandEvent(x =>
            {
                x.name = "save";
                x.accessibility = IA.Events.EventAccessibility.ADMINONLY;
                x.processCommand = (e, arg) =>
                {
                    StreamWriter sw = new StreamWriter(idFilePath);
                    sw.WriteLine(colorStart.Id);
                    sw.WriteLine(profStart.Id);
                    sw.Flush();
                    sw.Close();
                };
            });

            // show dividers
            bot.Events.AddCommandEvent(x =>
            {
                x.name = "showdividers";
                x.accessibility = IA.Events.EventAccessibility.ADMINONLY;
                x.processCommand = (e, arg) =>
                {
                    e.Channel.SendMessage(colorStart.Id + " - " + colorStart.Name + "\n" + profStart.Id + " - " + profStart.Name);
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
                    ulong colorid = 0;
                    Role r = null;
                    try
                    {
                        colorid = ulong.Parse(arg.Trim('<', '>', '@', '&'));
                        r = e.Server.GetRole(colorid);
                    }
                    catch
                    {
                        try
                        {
                            r = e.Server.FindRoles(arg).ElementAt(0);
                        }
                        catch
                        {
                            await e.User.SendMessage("Please add the color you want.");
                            return;
                        }
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

                            List<Role> deleteRoles = new List<Role>();
                            foreach (Role role in e.Server.Roles)
                            {
                                if (role.Color.ToString() != "#0")
                                {
                                    if (e.User.HasRole(role))
                                    {
                                        await e.User.RemoveRoles(role);
                                    }
                                }
                            }
                            await Task.Delay(100);
                            await e.User.AddRoles(r);
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
                    string[] allArgs = arg.Split(' ');
                    string name = allArgs[0];
                    int color = Convert.ToInt32(allArgs[1].Trim('#'), 16);
                    Role r = await e.Server.CreateRole(name, ServerPermissions.None, new Color((uint)color), false, true);
                    await r.Edit(null, null, null, null, colorStart.Position - 1);
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
                    Role r = await e.Server.CreateRole(arg, ServerPermissions.None, Color.Default, false, true);
                    await r.Edit(null, null, null, null, profStart.Position - 1);
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
                    ulong colorid = 0;
                    Role r = null;
                    try
                    {
                        colorid = ulong.Parse(arg.Trim('<', '>', '@', '&'));
                        r = e.Server.GetRole(colorid);
                    }
                    catch
                    {
                        try
                        {
                            r = e.Server.FindRoles(arg).ElementAt(0);
                        }
                        catch
                        {
                            return;
                        }
                    }
                    if (r != null)
                    {
                        if (r.Position < profStart.Position)
                        {
                            await e.User.AddRoles(r);
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
                    ulong colorid = 0;
                    Role r = null;
                    try
                    {
                        colorid = ulong.Parse(arg.Trim('<', '>', '@', '&'));
                        r = e.Server.GetRole(colorid);
                    }
                    catch
                    {
                        try
                        {
                            r = e.Server.FindRoles(arg).ElementAt(0);
                        }
                        catch
                        {
                            return;
                        }
                    }
                    if (r != null)
                    {
                        if (r.Position < profStart.Position)
                        {
                            await e.User.RemoveRoles(r);
                        }
                    }

                };
            });

            // check color
            bot.Events.AddCommandEvent(x =>
            {
                x.name = "checkcolor";
                x.processCommand = (e, arg) =>
                {
                    ulong colorid = 0;
                    Role r = null;
                    try
                    {
                        colorid = ulong.Parse(arg.Trim('<', '>', '@', '&'));
                        r = e.Server.GetRole(colorid);
                    }
                    catch
                    {
                        try
                        {
                            r = e.Server.FindRoles(arg).ElementAt(0);
                        }
                        catch
                        {
                            e.User.SendMessage("Please add the color you want.");
                            return;
                        }
                    }
                    if (r != null)
                    {
                        
                        e.Channel.SendMessage(r.Name + " is " + ((r.Color.ToString() != "#0") ? "a color" : "NOT a " + ((r.Name == "Unity")?"engine":"color")));
                        if(r.Color!= Color.Default)
                        {
                            e.Channel.SendMessage("instead, it is " + r.Color.ToString());
                        }
                    }
                };
            });

            // statistics
            bot.Events.AddCommandEvent(x =>
            {
                x.name = "stats";
                x.accessibility = EventAccessibility.ADMINONLY;
                x.processCommand = (e, arg) =>
                {
                    e.Channel.SendMessage($"Messages Recieved: {messagesRecieved}\nAmount were shitposts involving me: {shitpostsDoneWithMentions}\n\nUptime: {(DateTime.Now - uptime).ToString().Split('.')[0]}\n\n Total Users: {e.Server.UserCount}\n Total Nodevs: {nodevs()}");
                };
            });

            // purge
            bot.Events.AddCommandEvent(x =>
            {
                x.name = "purge";
                x.accessibility = EventAccessibility.ADMINONLY;
                x.processCommand = (e, arg) =>
                {
                    User u = e.Channel.Users.First(z => z.Id == ulong.Parse(arg.Trim('<', '>', '@', '!')));
                    e.Channel.DownloadMessages();

                    List<Message> deleteMessages = new List<Message>();
                    deleteMessages.Add(e.Message);


                    foreach(Message m in e.Channel.Messages)
                    {
                        if(m.User == u)
                        {
                            deleteMessages.Add(m);
                            Log.Message(m.Text);
                        }
                    }

                    e.Channel.DeleteMessages(deleteMessages.ToArray());
                    e.Channel.SendMessageAndDelete("Deleted " + deleteMessages.Count + " messages", 4);
                };
            });

            // mute
            bot.Events.AddCommandEvent(x =>
            {
                x.name = "mute";
                x.processCommand = async (e, arg) =>
                {
                    if (e.Message.MentionedUsers.Count() > 0 && e.Message.RawText.Split(' ').Length > 1)
                    {
                        int minutes = int.Parse(e.Message.RawText.Split(' ')[2]);

                        await e.Message.MentionedUsers.ElementAt(0).AddRoles(e.Server.FindRoles("muted").First());
                        await e.Channel.SendMessageAndDelete($"muted `{e.Message.MentionedUsers.First().Name}` for `{minutes}` minutes", 5);
                        await Task.Delay(minutes * 60000);
                        await e.Message.MentionedUsers.ElementAt(0).RemoveRoles(e.Server.FindRoles("muted").First());

                    }
                };
            });

            // help 
            bot.Events.AddCommandEvent(x =>
            {
                x.name = "help";
                x.processCommand = async (e, arg) =>
                {
                    await e.User.SendMessage(await bot.Events.ListCommands(e));
                };
            });

            // Cleverbot
            bot.Events.AddMentionEvent(x =>
            {
                x.name = "cleverbot";
                x.cooldown = 30;
                x.checkCommand = (e, a, c) =>
                {
                    Profile me = bot.Client.CurrentUser;
                    return e.Message.RawText.StartsWith(me.Mention) && (e.Channel.Id == 121566911837241344 || e.Channel.Id == 226393903216066561);
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
