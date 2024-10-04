using LinePutScript;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using VPet.Plugin.SearchBoxForMod;
using VPet_Simulator.Core;

namespace VPet_Simulator.Windows
{
    internal class CoreMOD
    {
        public string Name { get; set; }
        public string Author { get; set; }
        public ulong ItemID { get; set; }
        public string Intro { get; set; }
        public int GameVer { get; set; }
        public bool IsOn { get; set; }
        public bool IsStar { get; set; }
        public HashSet<string> Tag = new HashSet<string>();

        public CoreMOD(DirectoryInfo directory, SearchBoxForMod main)
        {
            LpsDocument modlps = new LpsDocument(File.ReadAllText(directory.FullName + @"\info.lps"));
            this.Name = modlps.FindLine("vupmod").Info;
            this.Intro = modlps.FindLine("intro").Info;
            this.GameVer = modlps.FindSub("gamever").InfoToInt;
            this.Author = modlps.FindSub("author").Info.Split('[').First();
            this.IsOn = true;
            this.IsStar = main.favorities.Contains(this.Name) ? true : false;

            if (modlps.FindLine("itemid") != null)
                this.ItemID = Convert.ToUInt64(modlps.FindLine("itemid").info);
            else
                this.ItemID = 0;

            if (main.disabledMods.Contains(this.Name))
            {
                this.Tag.Add("该模组已停用");
                this.Tag.Add("disabled");
                foreach (DirectoryInfo di in directory.EnumerateDirectories())
                    Tag.Add(di.Name.ToLower());
                this.IsOn = false;
                return;
            }
            else
                this.Tag.Add("enabled");

            foreach (DirectoryInfo di in directory.EnumerateDirectories())
            {
                switch (di.Name.ToLower())
                {
                    case "theme":
                        this.Tag.Add("theme");
                        break;
                    case "pet":
                        foreach (FileInfo fi in di.EnumerateFiles("*.lps"))
                        {
                            LpsDocument lps = new LpsDocument(File.ReadAllText(fi.FullName));
                            if (lps.First().Name.ToLower() == "pet")
                            {
                                var name = lps.First().Info;
                                if (name == "默认虚拟桌宠")
                                    name = "vup";

                                var p = main.MW.Pets.FirstOrDefault(x => x.Name == name);
                                if (p == null)
                                {
                                    this.Tag.Add("pet");
                                    p = new PetLoader(lps, di);
                                    if (p.Config.Works.Count > 0)
                                        this.Tag.Add("work");
                                }
                                else
                                {
                                    if (lps.FindAllLine("work").Length >= 0)
                                        this.Tag.Add("work");
                                    var dis = new DirectoryInfo(di.FullName + "\\" + lps.First()["path"].Info);
                                    if (dis.Exists && dis.GetDirectories().Length > 0)
                                        this.Tag.Add("pet");
                                }
                            }
                        }
                        break;
                    case "food":
                        this.Tag.Add("food");
                        break;
                    case "image":
                        this.Tag.Add("image");
                        break;
                    case "file":
                        this.Tag.Add("file");
                        break;
                    case "photo":
                        this.Tag.Add("photo");
                        break;
                    case "text":
                        this.Tag.Add("text");
                        foreach (FileInfo fi in di.EnumerateFiles("*.lps"))
                        {
                            var tmp = new LpsDocument(File.ReadAllText(fi.FullName));
                            foreach (ILine li in tmp)
                            {
                                switch (li.Name.ToLower())
                                {
                                    case "lowfoodtext":
                                        this.Tag.Add("lowtext");
                                        break;
                                    case "lowdrinktext":
                                        this.Tag.Add("lowtext");
                                        break;
                                    case "clicktext":
                                        this.Tag.Add("clicktext");
                                        break;
                                    case "selecttext":
                                        this.Tag.Add("selecttext");
                                        break;
                                }
                            }
                        }
                        break;
                    case "lang":
                        this.Tag.Add("lang");
                        break;
                    case "plugin":
                        this.Tag.Add("plugin");
                        break;
                }
            }
        }
    }
}
