using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using StardewModdingAPI;

namespace MailServicesModChinese
{
    public class ModEntry : Mod
    {
        private Harmony? _harmony;

        private static readonly Dictionary<string, string> Translations = new()
        {
            // ===== 标题和分类 =====
            ["Enable Services:"] = "启用服务：",
            ["General:"] = "常规：",
            ["Services Fees:"] = "服务费用：",
            ["Tool Upgrade Service:"] = "工具升级服务：",
            ["Gift Service:"] = "礼物服务：",
            ["Recovery Service (Default):"] = "找回服务 (默认)：",
            ["Recovery Config - Farmers List:"] = "找回配置 - 农夫列表：",
            ["Back to the main page"] = "返回主页",

            // ===== 服务开关及描述 =====
            ["Gift Service"] = "礼物服务",
            ["Let you send gifts to the villagers using the mailbox."] = "允许您使用邮箱向村民发送礼物。",
            ["Quest Service"] = "任务服务",
            ["Let you send items to complete quests using the mailbox."] = "允许您使用邮箱发送物品以完成任务。",
            ["Tool Delivery Service"] = "工具交付服务",
            ["You will receive upgraded tools in the mailbox."] = "您将在邮箱中收到升级后的工具。",
            ["Tool Shipment Service"] = "工具运送服务",
            ["Let you send tools to upgrade using the mailbox."] = "允许您使用邮箱发送工具进行升级。",

            // ===== 费用相关 =====
            ["Fee per use of service."] = "每次使用服务的费用。",
            ["Gift Shipment (G)"] = "礼物运送 (金币)",
            ["How much gold you'll be charged for sending gifts."] = "发送礼物将收取多少金币。",
            ["Gift Shipment (%)"] = "礼物运送 (%)",
            ["How much in gift value percentage you'll be charged for sending gifts."] = "发送礼物将收取礼物价值百分比的多少费用。",
            ["Quest Item Shipment (G)"] = "任务物品运送 (金币)",
            ["How much gold you'll be charged for sending quest items."] = "发送任务物品将收取多少金币。",
            ["Tool Shipment (G)"] = "工具运送 (金币)",
            ["How much extra gold you'll be charged for sending tools to upgrade."] = "发送工具进行升级将额外收取多少金币。",
            ["Tool Shipment (%)"] = "工具运送 (%)",
            ["How much extra in tool upgrade cost percentage you'll be charged for sending tools to upgrade."] = "发送工具进行升级将额外收取工具升级费用百分比的多少费用。",

            // ===== 常规和工具升级 =====
            ["Show Dialog On Shipment"] = "运送时显示对话",
            ["Show the npc dialog as if you were delivering something in person. Works for gifts and quest completion."] = "像当面交付一样显示NPC对话。\n适用于礼物和完成任务。",
            ["Properties related to the tool upgrade service."] = "与工具升级服务相关的属性。",
            ["Ask to Upgrade Tool"] = "询问是否升级工具",
            ["When placing the tool in the mailbox you will have to confirm if you want to upgrade it."] = "将工具放入邮箱时，您需要确认是否要进行升级。",

            // ===== 礼物服务详情 =====
            ["Properties related to the gift service."] = "与礼物服务相关的属性。",
            ["Minimum Friendship Points"] = "最低好感度点数",
            ["Friendship points needed to send gifts to a NPC. 250 friendship points equal 1 heart level."] = "向NPC发送礼物所需的好感度点数。\n250点好感度等于1颗心。",
            ["NPC Page Size"] = "NPC页面大小",
            ["Number of villagers shown per page on gift shipment."] = "礼物运送每页显示的村民数量。",
            ["Jealousy"] = "嫉妒",
            ["Make it possible for your spouse to be jealous of gifts sent by mail like of gifts given in person."] = "使您的配偶像对待当面赠送的礼物一样，对通过邮件发送的礼物产生嫉妒。",
            ["Max Friendship"] = "最高好感度",
            ["Make it possible to send gifts to friends with maxed friendship."] = "使您能够向好感度已满的朋友发送礼物。",

            // ===== 找回服务 =====
            ["Properties related to the recovery of lost items. This properties are the ones used if you don't enable per framer configuration. They're also the ones that a farmer starts with."] = "与丢失物品找回相关的属性。\n如果您未启用按农夫配置，将使用这些属性。\n它们也是农夫初始的属性。",
            ["In Game Config Changes"] = "游戏内配置更改",
            ["Let in game events change the farmer's recovery config. If per farmer config is disabled, the default properties will be changed."] = "允许游戏内事件更改农夫的找回配置。\n如果禁用按农夫配置，将更改默认属性。",
            ["Recovery Service"] = "找回服务",
            ["Recover All Items"] = "找回所有物品",
            ["Recover For Free"] = "免费找回",
            ["Clear Lost Items"] = "清除丢失物品",
            ["Per Farmer Config"] = "按农夫配置",
            ["Farmers recovery config will be tracked individually."] = "将单独追踪每个农夫的找回配置。",
            ["Once you open a save file or create a new game, the farmer config should be tracked here."] = "打开存档或创建新游戏后，农夫配置将在此处追踪。",
            
            // ===== 选项值 =====
            ["Yes"] = "是",
            ["No"] = "否",
            ["One"] = "一个",
            ["All"] = "全部",
            ["None"] = "无"
        };

        public override void Entry(IModHelper helper)
        {
            _harmony = new Harmony(ModManifest.UniqueID);

            // 匹配 MailServicesMod 程序集
            Assembly? targetMod = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name?.Contains("MailServicesMod", StringComparison.OrdinalIgnoreCase) == true);

            if (targetMod == null)
            {
                Monitor.Log("未找到 MailServicesMod 程序集，跳过汉化。", LogLevel.Warn);
                return;
            }

            MethodInfo transpiler = typeof(ModEntry).GetMethod(nameof(Transpiler), BindingFlags.Static | BindingFlags.NonPublic)!;
            var harmonyTranspiler = new HarmonyMethod(transpiler);
            int patchedCount = 0;

            foreach (Type type in targetMod.GetTypes())
            {
                foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    if (!ShouldPatch(method)) continue;
                    try
                    {
                        _harmony.Patch(method, transpiler: harmonyTranspiler);
                        patchedCount++;
                    }
                    catch { }
                }
            }
            Monitor.Log($"MailServicesMod 汉化补丁已加载，共修补 {patchedCount} 个方法。", LogLevel.Info);
        }

        private static bool ShouldPatch(MethodBase method)
        {
            string name = method.Name;
            // MailServicesMod 使用 CreateConfigMenu 来构建菜单
            if (!name.Contains("CreateConfigMenu") && 
                !name.Contains("RegisterConfig") && !name.Contains("AddBoolOption") && 
                !name.Contains("AddSectionTitle") && !name.Contains("AddParagraph")) return false;
            try { return method.GetMethodBody() != null; } catch { return false; }
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldstr && instruction.operand is string original && Translations.TryGetValue(original, out string? translated))
                {
                    instruction.operand = translated;
                }
                yield return instruction;
            }
        }
    }
}
