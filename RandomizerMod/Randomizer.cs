using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RandomizerMod
{
    // Token: 0x0200091E RID: 2334
    public static class Randomizer
    {
        // Token: 0x06003117 RID: 12567 RVA: 0x0000269C File Offset: 0x0000089C
        public static int GetPlayerDataInt(GameManager man, string name)
        {
            return 0;
        }

        // Token: 0x06003118 RID: 12568 RVA: 0x0000269A File Offset: 0x0000089A
        public static void SetPlayerDataInt(GameManager man, string name, int value)
        {
        }

        // Token: 0x06003119 RID: 12569 RVA: 0x00125C70 File Offset: 0x00123E70
        public static bool GetPlayerDataBool(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }
            PlayerData instance = PlayerData.instance;
            string text = new StackTrace().ToString();
            if (text.Contains("at HutongGames.PlayMaker.Fsm.Start()") || (name.Contains("gotCharm_") && text.Contains("at HutongGames.PlayMaker.Fsm.DoTransition(HutongGames.PlayMaker.FsmTransition transition, Boolean isGlobal)")))
            {
                return instance.GetBoolInternal(name);
            }
            string key;
            string key2;
            bool boolRandomizer;
            if (!Randomizer.reverseLookup.TryGetValue(name, out key) || !Randomizer.permutation.TryGetValue(key, out key2))
            {
                boolRandomizer = instance.GetBoolInternal(name);
            }
            else
            {
                int index = Randomizer.entries[key].GetIndex(name);
                RandomizerEntry randomizerEntry = Randomizer.entries[key2];
                if (randomizerEntry.entries.Length > index)
                {
                    boolRandomizer = instance.GetBoolInternal(randomizerEntry.entries[index]);
                }
                else
                {
                    boolRandomizer = instance.GetBoolInternal(randomizerEntry.entries[0]);
                }
            }
            return boolRandomizer;
        }

        // Token: 0x0600311A RID: 12570 RVA: 0x00125D48 File Offset: 0x00123F48
        public static void SetPlayerDataBool(string name, bool val)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }
            PlayerData instance = PlayerData.instance;
            string key;
            string text;
            if (Randomizer.reverseLookup.TryGetValue(name, out key) && Randomizer.permutation.TryGetValue(key, out text))
            {
                if (text == "Shade Cloak" && !instance.hasDash && !instance.canDash)
                {
                    Randomizer.Swap("Shade Cloak", "Mothwing Cloak");
                    text = "Mothwing Cloak";
                    Randomizer.swappedCloak = true;
                }
                for (int i = 0; i < Randomizer.entries[text].entries.Length; i++)
                {
                    instance.SetBoolInternal(Randomizer.entries[text].entries[i], val);
                }
                return;
            }
            instance.SetBoolInternal(name, val);
        }

        // Token: 0x0600311B RID: 12571 RVA: 0x00125DFC File Offset: 0x00123FFC
        public static void AddEntry(string name, string[] entries, string[][] requirements)
        {
            Randomizer.entries.Add(name, new RandomizerEntry(name, entries, requirements));
            for (int i = 0; i < entries.Length; i++)
            {
                Randomizer.reverseLookup.Add(entries[i], name);
            }
        }

        // Token: 0x0600311C RID: 12572 RVA: 0x00125E38 File Offset: 0x00124038
        public static void Randomize(System.Random random)
        {
            bool flag = false;
            while (!flag)
            {
                List<string> list = new List<string>();
                List<string> list2 = new List<string>();
                List<string> list3 = new List<string>();
                list.AddRange(Randomizer.entries.Keys);
                list2.AddRange(Randomizer.entries.Keys);
                Randomizer.permutation.Clear();
                while (list.Count > 0)
                {
                    flag = false;
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (Randomizer.IsReachable(list3, Randomizer.entries[list[i]]))
                        {
                            flag = true;
                            int index = random.Next(list2.Count);
                            Randomizer.permutation.Add(list[i], list2[index]);
                            list3.Add(list2[index]);
                            list.RemoveAt(i);
                            i--;
                            list2.RemoveAt(index);
                        }
                    }
                    if (!flag)
                    {
                        break;
                    }
                }
            }
        }

        // Token: 0x0600311D RID: 12573 RVA: 0x00125F24 File Offset: 0x00124124
        public static bool IsReachable(List<string> reachable, RandomizerEntry entry)
        {
            for (int i = 0; i < entry.requires.Length; i++)
            {
                bool flag = true;
                for (int j = 0; j < entry.requires[i].Length; j++)
                {
                    if (!reachable.Contains(entry.requires[i][j]))
                    {
                        flag = false;
                        break;
                    }
                }
                if (flag)
                {
                    return true;
                }
            }
            return entry.requires.Length == 0;
        }

        // Token: 0x0600311E RID: 12574 RVA: 0x00125F80 File Offset: 0x00124180
        public static void Swap(string entry1, string entry2)
        {
            try
            {
                string key = Randomizer.permutation.FirstOrDefault((KeyValuePair<string, string> x) => x.Value == entry1).Key;
                string key2 = Randomizer.permutation.FirstOrDefault((KeyValuePair<string, string> x) => x.Value == entry2).Key;
                Randomizer.permutation[key] = entry2;
                Randomizer.permutation[key2] = entry1;
            }
            catch (Exception)
            {
            }
        }

        // Token: 0x0600311F RID: 12575 RVA: 0x00126018 File Offset: 0x00124218
        public static void SaveGame(int profileId)
        {
            if (Randomizer.randomizer)
            {
                using (StreamWriter streamWriter = new StreamWriter(string.Concat(new object[]
				{
					Application.persistentDataPath,
					"\\user",
					profileId,
					".rnd"
				})))
                {
                    streamWriter.WriteLine(Randomizer.seed);
                    streamWriter.WriteLine(Randomizer.swappedCloak);
                    streamWriter.WriteLine(Randomizer.hardMode);
                }
            }
        }

        // Token: 0x06003120 RID: 12576 RVA: 0x0012609C File Offset: 0x0012429C
        public static void LoadGame(int profileId)
        {
            Randomizer.randomizer = false;
            Randomizer.hardMode = false;
            if (File.Exists(string.Concat(new object[]
			{
				Application.persistentDataPath,
				"\\user",
				profileId,
				".rnd"
			})))
            {
                using (StreamReader streamReader = new StreamReader(string.Concat(new object[]
				{
					Application.persistentDataPath,
					"\\user",
					profileId,
					".rnd"
				})))
                {
                    Randomizer.seed = Convert.ToInt32(streamReader.ReadLine());
                    Randomizer.swappedCloak = Convert.ToBoolean(streamReader.ReadLine());
                    Randomizer.hardMode = Convert.ToBoolean(streamReader.ReadLine());
                }
                Randomizer.SetHardMode(Randomizer.hardMode);
                Randomizer.Randomize(new System.Random(Randomizer.seed));
                if (Randomizer.swappedCloak)
                {
                    Randomizer.Swap("Mothwing Cloak", "Shade Cloak");
                }
                Randomizer.randomizer = true;
            }
        }

        // Token: 0x06003121 RID: 12577 RVA: 0x0002463E File Offset: 0x0002283E
        public static void NewGame()
        {
            if (Randomizer.randomizer)
            {
                Randomizer.SetHardMode(Randomizer.hardMode);
                if (Randomizer.seed == -1)
                {
                    Randomizer.seed = new System.Random().Next();
                }
                Randomizer.swappedCloak = false;
                Randomizer.Randomize(new System.Random(Randomizer.seed));
            }
        }

        // Token: 0x06003122 RID: 12578 RVA: 0x001261A0 File Offset: 0x001243A0
        public static void DeleteGame(int profileId)
        {
            if (File.Exists(string.Concat(new object[]
			{
				Application.persistentDataPath,
				"\\user",
				profileId,
				".rnd"
			})))
            {
                File.Delete(string.Concat(new object[]
				{
					Application.persistentDataPath,
					"\\user",
					profileId,
					".rnd"
				}));
            }
        }

        // Token: 0x06003123 RID: 12579 RVA: 0x00126214 File Offset: 0x00124414
        public static void SetHardMode(bool hard)
        {
            Randomizer.hardMode = hard;
            Randomizer.entries.Clear();
            Randomizer.reverseLookup.Clear();
            if (!hard)
            {
                Randomizer.AddEntry("Mothwing Cloak", new string[]
				{
					"hasDash",
					"canDash"
				}, new string[0][]);
                Randomizer.AddEntry("Mantis Claw", new string[]
				{
					"hasWalljump",
					"canWallJump"
				}, new string[][]
				{
					new string[]
					{
						"Mothwing Cloak"
					},
					new string[]
					{
						"Shade Cloak"
					},
					new string[]
					{
						"Monarch Wings"
					},
					new string[]
					{
						"Mantis Claw"
					},
					new string[]
					{
						"Crystal Heart"
					}
				});
                Randomizer.AddEntry("Crystal Heart", new string[]
				{
					"hasSuperDash",
					"canSuperDash"
				}, new string[][]
				{
					new string[]
					{
						"Mantis Claw",
						"Mothwing Cloak"
					},
					new string[]
					{
						"Mantis Claw",
						"Shade Cloak"
					}
				});
                Randomizer.AddEntry("Monarch Wings", new string[]
				{
					"hasDoubleJump"
				}, new string[][]
				{
					new string[]
					{
						"Crystal Heart",
						"Mantis Claw"
					},
					new string[]
					{
						"Mantis Claw",
						"Monarch Wings"
					}
				});
                Randomizer.AddEntry("Shade Cloak", new string[]
				{
					"hasShadowDash",
					"canShadowDash"
				}, new string[][]
				{
					new string[]
					{
						"Mantis Claw",
						"Monarch Wings"
					}
				});
                Randomizer.AddEntry("Soul Catcher", new string[]
				{
					"gotCharm_20"
				}, new string[0][]);
                Randomizer.AddEntry("Soul Eater", new string[]
				{
					"gotCharm_21"
				}, new string[][]
				{
					new string[]
					{
						"Mantis Claw",
						"Mothwing Cloak"
					},
					new string[]
					{
						"Mantis Claw",
						"Crystal Heart"
					},
					new string[]
					{
						"Monarch Wings",
						"Crystal Heart"
					},
					new string[]
					{
						"Mantis Claw",
						"Shade Cloak"
					},
					new string[]
					{
						"Mantis Claw",
						"Monarch Wings"
					}
				});
                Randomizer.AddEntry("Dashmaster", new string[]
				{
					"gotCharm_31"
				}, new string[][]
				{
					new string[]
					{
						"Mothwing Cloak"
					},
					new string[]
					{
						"Mantis Claw"
					},
					new string[]
					{
						"Monarch Wings"
					},
					new string[]
					{
						"Crystal Heart"
					},
					new string[]
					{
						"Shade Cloak"
					}
				});
                Randomizer.AddEntry("Thorns of Agony", new string[]
				{
					"gotCharm_12"
				}, new string[][]
				{
					new string[]
					{
						"Mothwing Cloak"
					},
					new string[]
					{
						"Shade Cloak"
					},
					new string[]
					{
						"Mantis Claw",
						"Crystal Heart"
					}
				});
                Randomizer.AddEntry("Fury of the Fallen", new string[]
				{
					"gotCharm_6"
				}, new string[0][]);
                Randomizer.AddEntry("Spell Twister", new string[]
				{
					"gotCharm_33"
				}, new string[][]
				{
					new string[]
					{
						"Mantis Claw",
						"Mothwing Cloak"
					},
					new string[]
					{
						"Mantis Claw",
						"Crystal Heart"
					},
					new string[]
					{
						"Monarch Wings",
						"Crystal Heart"
					},
					new string[]
					{
						"Mantis Claw",
						"Shade Cloak"
					},
					new string[]
					{
						"Mantis Claw",
						"Monarch Wings"
					}
				});
                Randomizer.AddEntry("Quick Slash", new string[]
				{
					"gotCharm_32"
				}, new string[][]
				{
					new string[]
					{
						"Mothwing Cloak",
						"Mantis Claw"
					},
					new string[]
					{
						"Mothwing Cloak",
						"Monarch Wings"
					},
					new string[]
					{
						"Shade Cloak",
						"Mantis Claw"
					},
					new string[]
					{
						"Shade Cloak",
						"Monarch Wings"
					}
				});
                Randomizer.AddEntry("Mark of Pride", new string[]
				{
					"gotCharm_13"
				}, new string[][]
				{
					new string[]
					{
						"Mantis Claw"
					}
				});
                Randomizer.AddEntry("Baldur Shell", new string[]
				{
					"gotCharm_5"
				}, new string[][]
				{
					new string[]
					{
						"Mantis Claw"
					},
					new string[]
					{
						"Mothwing Cloak"
					},
					new string[]
					{
						"Shade Cloak"
					},
					new string[]
					{
						"Monarch Wings"
					},
					new string[]
					{
						"Crystal Heart"
					}
				});
                Randomizer.AddEntry("Flukenest", new string[]
				{
					"gotCharm_11"
				}, new string[][]
				{
					new string[]
					{
						"Crystal Heart"
					},
					new string[]
					{
						"Mantis Claw",
						"Mothwing Cloak"
					},
					new string[]
					{
						"Mantis Claw",
						"Shade Cloak"
					},
					new string[]
					{
						"Mantis Claw",
						"Monarch Wings"
					}
				});
                Randomizer.AddEntry("Glowing Womb", new string[]
				{
					"gotCharm_22"
				}, new string[][]
				{
					new string[]
					{
						"Mantis Claw",
						"Crystal Heart"
					},
					new string[]
					{
						"Monarch Wings",
						"Crystal Heart"
					}
				});
                Randomizer.AddEntry("Deep Focus", new string[]
				{
					"gotCharm_34"
				}, new string[][]
				{
					new string[]
					{
						"Crystal Heart",
						"Mantis Claw"
					}
				});
                Randomizer.AddEntry("Grubsong", new string[]
				{
					"gotCharm_3"
				}, new string[][]
				{
					new string[]
					{
						"Mothwing Cloak",
						"Mantis Claw",
						"Monarch Wings",
						"Crystal Heart",
						"Shade Cloak"
					}
				});
                Randomizer.AddEntry("Hiveblood", new string[]
				{
					"gotCharm_29"
				}, new string[][]
				{
					new string[]
					{
						"Mantis Claw",
						"Monarch Wings"
					}
				});
                Randomizer.AddEntry("Spore Shroom", new string[]
				{
					"gotCharm_17"
				}, new string[][]
				{
					new string[]
					{
						"Mantis Claw",
						"Mothwing Cloak"
					},
					new string[]
					{
						"Mantis Claw",
						"Shade Cloak"
					},
					new string[]
					{
						"Mantis Claw",
						"Monarch Wings"
					},
					new string[]
					{
						"Mantis Claw",
						"Crystal Heart"
					}
				});
                Randomizer.AddEntry("Defender's Crest", new string[]
				{
					"gotCharm_10"
				}, new string[][]
				{
					new string[]
					{
						"Mantis Claw",
						"Crystal Heart"
					},
					new string[]
					{
						"Mantis Claw",
						"Mothwing Cloak"
					},
					new string[]
					{
						"Mantis Claw",
						"Shade Cloak"
					},
					new string[]
					{
						"Mantis Claw",
						"Monarch Wings"
					}
				});
                return;
            }
            Randomizer.AddEntry("Mothwing Cloak", new string[]
			{
				"hasDash",
				"canDash"
			}, new string[0][]);
            Randomizer.AddEntry("Mantis Claw", new string[]
			{
				"hasWalljump",
				"canWallJump"
			}, new string[0][]);
            Randomizer.AddEntry("Crystal Heart", new string[]
			{
				"hasSuperDash",
				"canSuperDash"
			}, new string[][]
			{
				new string[]
				{
					"Mantis Claw"
				}
			});
            Randomizer.AddEntry("Monarch Wings", new string[]
			{
				"hasDoubleJump"
			}, new string[][]
			{
				new string[]
				{
					"Crystal Heart",
					"Mantis Claw"
				},
				new string[]
				{
					"Monarch Wings"
				}
			});
            Randomizer.AddEntry("Shade Cloak", new string[]
			{
				"hasShadowDash",
				"canShadowDash"
			}, new string[][]
			{
				new string[]
				{
					"Mantis Claw",
					"Monarch Wings"
				},
				new string[]
				{
					"Mantis Claw",
					"Mothwing Cloak"
				},
				new string[]
				{
					"Mantis Claw",
					"Shade Cloak"
				},
				new string[]
				{
					"Mantis Claw",
					"Crystal Heart"
				}
			});
            Randomizer.AddEntry("Soul Catcher", new string[]
			{
				"gotCharm_20"
			}, new string[0][]);
            if (PlayerData.instance.permadeathMode > 0)
            {
                Randomizer.AddEntry("Soul Eater", new string[]
				{
					"gotCharm_21"
				}, new string[][]
				{
					new string[]
					{
						"Mantis Claw",
						"Mothwing Cloak"
					},
					new string[]
					{
						"Mantis Claw",
						"Crystal Heart"
					},
					new string[]
					{
						"Monarch Wings",
						"Crystal Heart"
					},
					new string[]
					{
						"Mantis Claw",
						"Shade Cloak"
					},
					new string[]
					{
						"Mantis Claw",
						"Monarch Wings"
					}
				});
            }
            else
            {
                Randomizer.AddEntry("Soul Eater", new string[]
				{
					"gotCharm_21"
				}, new string[][]
				{
					new string[]
					{
						"Mantis Claw"
					},
					new string[]
					{
						"Monarch Wings"
					}
				});
            }
            Randomizer.AddEntry("Dashmaster", new string[]
			{
				"gotCharm_31"
			}, new string[0][]);
            Randomizer.AddEntry("Thorns of Agony", new string[]
			{
				"gotCharm_12"
			}, new string[][]
			{
				new string[]
				{
					"Mothwing Cloak"
				},
				new string[]
				{
					"Shade Cloak"
				},
				new string[]
				{
					"Mantis Claw",
					"Crystal Heart"
				}
			});
            Randomizer.AddEntry("Fury of the Fallen", new string[]
			{
				"gotCharm_6"
			}, new string[0][]);
            if (PlayerData.instance.permadeathMode > 0)
            {
                Randomizer.AddEntry("Spell Twister", new string[]
				{
					"gotCharm_33"
				}, new string[][]
				{
					new string[]
					{
						"Mantis Claw",
						"Mothwing Cloak"
					},
					new string[]
					{
						"Mantis Claw",
						"Crystal Heart"
					},
					new string[]
					{
						"Monarch Wings",
						"Crystal Heart"
					},
					new string[]
					{
						"Mantis Claw",
						"Shade Cloak"
					},
					new string[]
					{
						"Mantis Claw",
						"Monarch Wings"
					}
				});
            }
            else
            {
                Randomizer.AddEntry("Spell Twister", new string[]
				{
					"gotCharm_33"
				}, new string[][]
				{
					new string[]
					{
						"Mantis Claw"
					},
					new string[]
					{
						"Monarch Wings"
					}
				});
            }
            Randomizer.AddEntry("Quick Slash", new string[]
			{
				"gotCharm_32"
			}, new string[][]
			{
				new string[]
				{
					"Mothwing Cloak",
					"Mantis Claw"
				},
				new string[]
				{
					"Mothwing Cloak",
					"Monarch Wings"
				},
				new string[]
				{
					"Shade Cloak",
					"Mantis Claw"
				},
				new string[]
				{
					"Shade Cloak",
					"Monarch Wings"
				}
			});
            Randomizer.AddEntry("Mark of Pride", new string[]
			{
				"gotCharm_13"
			}, new string[][]
			{
				new string[]
				{
					"Mantis Claw"
				}
			});
            Randomizer.AddEntry("Baldur Shell", new string[]
			{
				"gotCharm_5"
			}, new string[][]
			{
				new string[]
				{
					"Mantis Claw"
				},
				new string[]
				{
					"Mothwing Cloak"
				},
				new string[]
				{
					"Shade Cloak"
				},
				new string[]
				{
					"Monarch Wings"
				},
				new string[]
				{
					"Crystal Heart"
				}
			});
            if (PlayerData.instance.permadeathMode > 0)
            {
                Randomizer.AddEntry("Flukenest", new string[]
				{
					"gotCharm_11"
				}, new string[][]
				{
					new string[]
					{
						"Mantis Claw",
						"Crystal Heart"
					},
					new string[]
					{
						"Mantis Claw",
						"Mothwing Cloak"
					},
					new string[]
					{
						"Mantis Claw",
						"Shade Cloak"
					},
					new string[]
					{
						"Mantis Claw",
						"Monarch Wings"
					}
				});
            }
            else
            {
                Randomizer.AddEntry("Flukenest", new string[]
				{
					"gotCharm_11"
				}, new string[0][]);
            }
            Randomizer.AddEntry("Glowing Womb", new string[]
			{
				"gotCharm_22"
			}, new string[][]
			{
				new string[]
				{
					"Mantis Claw",
					"Crystal Heart"
				},
				new string[]
				{
					"Monarch Wings",
					"Crystal Heart"
				}
			});
            Randomizer.AddEntry("Deep Focus", new string[]
			{
				"gotCharm_34"
			}, new string[][]
			{
				new string[]
				{
					"Crystal Heart",
					"Mantis Claw"
				}
			});
            Randomizer.AddEntry("Grubsong", new string[]
			{
				"gotCharm_3"
			}, new string[][]
			{
				new string[]
				{
					"Mothwing Cloak",
					"Mantis Claw",
					"Monarch Wings",
					"Crystal Heart",
					"Shade Cloak"
				}
			});
            Randomizer.AddEntry("Hiveblood", new string[]
			{
				"gotCharm_29"
			}, new string[][]
			{
				new string[]
				{
					"Mantis Claw"
				}
			});
            Randomizer.AddEntry("Spore Shroom", new string[]
			{
				"gotCharm_17"
			}, new string[][]
			{
				new string[]
				{
					"Mantis Claw",
					"Mothwing Cloak"
				},
				new string[]
				{
					"Mantis Claw",
					"Shade Cloak"
				},
				new string[]
				{
					"Mantis Claw",
					"Monarch Wings"
				},
				new string[]
				{
					"Mantis Claw",
					"Crystal Heart"
				}
			});
            if (PlayerData.instance.permadeathMode > 0)
            {
                Randomizer.AddEntry("Defender's Crest", new string[]
				{
					"gotCharm_10"
				}, new string[][]
				{
					new string[]
					{
						"Mantis Claw",
						"Crystal Heart"
					},
					new string[]
					{
						"Mantis Claw",
						"Mothwing Cloak"
					},
					new string[]
					{
						"Mantis Claw",
						"Shade Cloak"
					},
					new string[]
					{
						"Mantis Claw",
						"Monarch Wings"
					}
				});
                return;
            }
            Randomizer.AddEntry("Defender's Crest", new string[]
			{
				"gotCharm_10"
			}, new string[][]
			{
				new string[]
				{
					"Mantis Claw"
				}
			});
        }

        // Token: 0x06003124 RID: 12580 RVA: 0x001271B8 File Offset: 0x001253B8
        public static void DebugLog(string message)
        {
            if (!Randomizer.debug)
            {
                return;
            }
            if (Randomizer.debugWriter == null)
            {
                Randomizer.debugWriter = new StreamWriter(Application.persistentDataPath + "\\randomizer.txt", true);
                Randomizer.debugWriter.AutoFlush = true;
            }
            if (Randomizer.debugWriter != null)
            {
                Randomizer.debugWriter.WriteLine(message);
            }
        }

        // Token: 0x040038DC RID: 14556
        private static Dictionary<string, RandomizerEntry> entries = new Dictionary<string, RandomizerEntry>();

        // Token: 0x040038DD RID: 14557
        private static Dictionary<string, string> reverseLookup = new Dictionary<string, string>();

        // Token: 0x040038DE RID: 14558
        private static Dictionary<string, string> permutation = new Dictionary<string, string>();

        // Token: 0x040038DF RID: 14559
        public static int seed = -1;

        // Token: 0x040038E0 RID: 14560
        public static bool swappedCloak;

        // Token: 0x040038E1 RID: 14561
        public static bool randomizer;

        // Token: 0x040038E2 RID: 14562
        public static bool hardMode;

        // Token: 0x040038E3 RID: 14563
        public static bool debug = true;

        // Token: 0x040038E4 RID: 14564
        public static StreamWriter debugWriter = null;
    }
}
