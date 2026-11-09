Name: New Loop Plus
Summary: End a run from the pause menu and start a new loop at once, tune the Planning Points runs earn and what loop rewards cost, and add new loop rewards. Thirty-six new rewards and five more levels for seventeen game rewards ship switched off, and a Talents tab in the Mod Manager lets you edit any reward or build your own.

Description (BBCode):
[center][img]https://sldb.core-directive.com/media/notice/beta.png[/img][/center]

Want a fresh loop without waiting for your character to die? New Loop Plus adds an End Run button to the pause menu. Confirm, and the run ends exactly as a death does: the death screen, the settlement and the save are the game's own. It also lets you tune how many Planning Points a run earns and what loop rewards cost.

[size=5]Main features[/size]
[list]
[*][b]End Run button[/b]: the pause menu gets an End Run button near the bottom, below other mods' buttons and above Restart and Save & Exit.[/*]
[*][b]Always asks first[/b]: nothing ends until you click End Run in the confirm dialog. Cancel, or a click outside it, leaves the run alone.[/*]
[*][b]Shows what you will get[/b]: in a normal run the dialog shows the Planning Points the death screen will pay, split into survival points, crises and days survived.[/*]
[*][b]Planning Points multipliers[/b]: one overall setting, plus one each for survival points, crises and events, and days survived. The death screen and the End Run preview show the scaled points.[/*]
[*][b]Starting bonus[/b]: extra Planning Points at the start of each new loop, paid as the loop reward screen opens, so you can spend them right there.[/*]
[*][b]Endless starting points[/b]: a multiplier for the 500 Planning Points a new endless game starts with.[/*]
[*][b]Loop reward costs[/b]: one multiplier for what every loop reward upgrade costs, plus your own multiplier for single talents by ID. Resetting the upgrade screen refunds what you paid.[/*]
[*][b]Talents file[/b]: [font=Courier New]BepInEx\config\NewLoopPlus.Talents.json[/font] lets you change loop rewards by hand: costs, buffs, text, extra levels and new talents. It ships with ten talents the game has in its data but never offers, all switched off. Driver, No-Till Soil, Trailblazer, Liaison and Radio Station work as the game left them. Well-Connected, Last-Minute Preparation, Resourceful, Yoga Master and Shut-In were unfinished and get new effects from the mod.[/*]
[*][b]Thirty-six new loop rewards[/b], also in the talents file and switched off. Eighteen use effects the game already has: Prepper, Insulated Pack, Quick Rest, Every Crumb Counts, Field Kit, Solar Engineer, Trapper, Quick Learner, Old Friend (start with an HK-1 robot), Light Footed, Brewmaster (faster brewing, more bottles), Hardened Defenses (less bite damage to doors, windows and defenses), Long-Range Scavenging (more drone scavenging a day), Trade Routes (more drone trades a day), Packed Fresh (backpack food keeps longer), Quick Hands (faster crafting), Solar Tuning and Generator Tuning (more power). Eighteen have effects made by the mod: Interest (Planning Points on what you kept), Safety Net (a floor for what a run pays), Extra Pick (more rewards to choose from each day), Crisis Veteran, Second Wind (survive running out of Life), Lucky Finds, Muscle Memory (keep part of the last run's skill levels), Scout's Notes (start with drone trading posts found), Tinkerer (crafting can give a material back), Family Recipe (better cooking quality, multi-pot cooking sooner), Drone Pilot (shorter drone trips, more trades a day), Good Neighbor (more affinity per basket), Seasoned Explorer (higher Max Exposure), Dealer Discount (cheaper vehicles) Shortcuts (faster travel before the disaster), Fast Rotors (faster drone trips), and Wind Engineer and Wind Tuning (more power from Wind Power's turbines).[/*]
[*][b]Five more levels for seventeen loop rewards[/b], also in the talents file and switched off. Early Riser, Savings Account, Encumbrance, Clearance, Bulk Bargains, Hoarding Habit, Quick Fortification and Gratitude go to ten levels, and Resourceful Management, Interior Decorator, Energy Saver, Bargaining, Deep Freeze, Long Harvest and Cold-Resistant Constitution to eight. Mental Toughness goes to ten, and its new levels can be used two to four times. Mold Master goes to eight and is spread out to reach 100% at the top, so its levels 2 and 3 are weaker than the game's.[/*]
[*][b]Endless rewards everywhere[/b]: an option, off by default, that offers the endless mode loop rewards in every mode and makes them work there as they do in endless. The camp rewards and Cartographer stay endless only.[/*]
[*][b]Talents tab[/b]: with the Mod Manager 0.7.0 or newer, New Loop Plus's page gets a Talents tab that edits the talents file for you. Change what any loop reward costs, how many levels it has, its numbers and its text, switch the file's rewards on or off, or click NEW REWARD to build your own from the effects the game's rewards use and the mod's own, with any talent or item image. SAVE writes the file; the changes apply at the next game start.[/*]
[/list]
With every talent in the talents file switched off, New Loop Plus adds no data of its own to your save. A switched-on talent can leave its levels in the save; switch it off and play a run before removing the mod. Points and costs go back to the game's own at the next start.

Works in English and Chinese.

[size=5]Known issues[/size]
[list]
[*]If the game is still paused five seconds after the pause menu closes (another window holding it paused, for example), the run is not ended and a short message says so. Close that window and try again.[/*]
[*]Points are whole numbers, so scaled values are rounded. A small reward times a small multiplier can round to 0.[/*]
[*]Crisis and event penalties that take Planning Points away are not scaled.[/*]
[*]The talents file is read once at game start. Changes need a restart, including changes saved from the Talents tab.[/*]
[*]Saving from the Talents tab drops the comments in the talents file. The file as it was is kept next to it as [font=Courier New]NewLoopPlus.Talents.json.bak[/font].[/*]
[/list]

[center][img]https://sldb.core-directive.com/img/mods-banner.jpg[/img]
[size=4][url=https://www.nexusmods.com/survivallog/mods/73]Get Mod Manager[/url] to set up all your mods in game  |  [url=https://sldb.core-directive.com/en/]Open the Survival Log wiki[/url] for items, recipes and maps[/size][/center]

[size=5]Requirements[/size]
[list]
[*]Survival Log 1.1.19611. A game update can break it until the mod is updated.[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]. Install it first.[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url] 0.19.0 or newer. New Loop Plus does not load without it. It shows New Loop Plus's messages as toasts and applies the points multipliers and the starting bonus changed on the Mod Manager's Mods screen without a restart. Cost settings always apply at the next game start.[/*]
[*]Optional: Mod Manager 0.7.0 or newer, for the Talents tab. Without it, edit the talents file by hand.[/*]
[*]Optional: Wind Power 0.9.3 or newer, for the Wind Engineer and Wind Tuning rewards. Without it, they are left out of the game and the Talents tab does not offer the wind effect.[/*]
[/list]

[size=5]Installation[/size]
[list=1]
[*]Install [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i](if no other mods were installed before, start the game once so BepInEx finishes setup, then quit)[/i].[/*]
[*]Install Exiled's Toolkit the same way. Download its zip and extract it into the game folder.[/*]
[*]Download the zip from the Files tab.[/*]
[*]Extract it into the game folder (the folder with [font=Courier New]SurvivalLog.exe[/font]). The zip holds a BepInEx folder, so the DLL lands in the right place. When asked, merge the folders. The result should be:
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\NewLoopPlus.dll[/code][/*]
[/list]

[size=5]Uninstall[/size]
Delete [font=Courier New]NewLoopPlus.dll[/font] from the [font=Courier New]BepInEx\plugins[/font] folder. A starting bonus already paid stays paid.

[size=5]Configuration[/size]
To change settings, edit [font=Courier New]BepInEx\config\com.dev.survivallog.newloopplus.cfg[/font] while the game is closed, or do it in game on the Mod Manager's Mods screen (see [url=https://github.com/AnExiledDev/SurvivalLog-NewLoopPlus/blob/main/CONFIG.md]CONFIG.md[/url] for every setting; the cfg file explains each setting in English and Chinese). The points multipliers and the starting bonus take effect at once; cost settings take effect at the next game start.

[size=5]Feedback[/size]
Found a bug or have an idea? Come say hi on the [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url], or post in this page's Posts tab. For bugs, please include [font=Courier New]BepInEx\LogOutput.log[/font] and [font=Courier New]BepInEx\new-loop-plus.log[/font].
To reach me directly, message [b]exiled.dev[/b] on Discord; you can find me in the [url=https://discord.gg/MZGg4gQAUe]official Survival Log Discord[/url]. I'm also on X as [url=https://x.com/AnExiledDev]@AnExiledDev[/url].

[size=5]Source and license[/size]
Open source under MIT: [url=https://github.com/AnExiledDev/SurvivalLog-NewLoopPlus]github.com/AnExiledDev/SurvivalLog-NewLoopPlus[/url]

[line]
[size=5]简体中文[/size]

不想等角色死亡就开始新的轮回？New Loop Plus 在暂停菜单里加了一个「结束本局」按钮。确认后，本局会像角色死亡一样结束：结算界面、结算和存档都是游戏原本的。你还可以调整每局获得的信念点和天赋升级的消耗。

[size=5]主要功能[/size]
[list]
[*][b]「结束本局」按钮[/b]：暂停菜单靠下的位置多了一个「结束本局」按钮，位于其他模组的按钮下方、重新开始和保存退出的上方。[/*]
[*][b]先确认再结束[/b]：只有在确认窗口中点击「结束本局」后才会结束。点击「取消」或窗口外部，本局照常继续。[/*]
[*][b]预先显示收益[/b]：普通模式下，确认窗口会显示结算界面将给出的信念点，并按生存点、危机和生存天数分开列出。[/*]
[*][b]信念点倍数[/b]：一个总倍数，另有生存点、危机与事件、生存天数各一个倍数。结算界面和「结束本局」的预览都会显示调整后的信念点。[/*]
[*][b]开局奖励[/b]：每次开始新轮回时额外获得信念点，在一局结束后打开轮回奖励界面时发放，可以当场使用。[/*]
[*][b]无尽模式初始信念点[/b]：新开无尽模式时的 500 初始信念点可以乘以一个倍数。[/*]
[*][b]天赋消耗[/b]：一个倍数调整所有天赋升级消耗的信念点，也可以按天赋 ID 单独设置倍数。在升级界面重置时会退还已花费的信念点。[/*]
[*][b]天赋文件[/b]：可以在 [font=Courier New]BepInEx\config\NewLoopPlus.Talents.json[/font] 中手动修改天赋：消耗、增益、文字、额外等级，以及新天赋。文件中自带十个游戏数据里有但从未开放的天赋，默认全部关闭。「载具驾驶」、「免翻土」、「开拓者」、「联络员」和「无线电台」保持游戏原样。「人脉网络」、「临阵磨枪」、「资源回收」、「瑜伽大师」和「宅人之心」在游戏中没有做完，由模组赋予了新效果。[/*]
[*][b]三十六个新的轮回奖励[/b]，同样在天赋文件中，默认关闭。其中十八个使用游戏已有的效果：「未雨绸缪」、「保鲜背包」、「养精蓄锐」、「颗粒归仓」、「应急物资包」、「太阳能工程师」、「捕猎老手」、「学得快」、「老伙计」（开局获得一台 HK-1 机器人）、「轻手轻脚」、「酿酒大师」（酿酒更快，出酒更多）、「固若金汤」（门窗和防御设施受到的啃咬伤害更低）、「远程拾荒」（无人机每天可多拾荒几次）、「交易航线」（无人机每天可多交易几次）、「持久保鲜」（背包中的食物保存更久）、「快手制作」（制造更快）、「太阳能调校」和「发电机调校」（产电更多）。另外十八个的效果由模组实现：「利息」（按剩下的信念点获得利息）、「保底」（一局所得信念点的下限）、「多一个选择」（每天可选的奖励更多）、「危机老兵」、「绝处逢生」（生命耗尽时挺过来）、「意外收获」、「肌肉记忆」（保留上一局的部分技能等级）、「侦察笔记」（开局时已发现无人机交易点）、「巧手匠人」（制作时有几率返还一份材料）、「家传秘方」（烹饪品质更高，更早解锁多锅烹饪）、「无人机飞手」（无人机往返更快，每天可多交易几次）、「远亲近邻」（每次吊篮送物好感度更高）、「老练探险家」（最大暴露上限更高）、「购车优惠」（载具更便宜）、「抄近路」（灾前出行更快）、「高速旋翼」（无人机行程更快），以及「风力工程师」和「风力调校」（Wind Power 的风力发电机产电更多）。[/*]
[*][b]十七个轮回奖励多五个等级[/b]，同样在天赋文件中，默认关闭。「觉醒」、「储蓄」、「负重」、「快速消费」、「大包划算」、「囤积习惯」、「快速加固」和「感恩」提高到十级，「周转有术」、「慧眼装修」、「精打细电」、「讨价还价」、「深度冷藏」、「延长收获窗口」和「耐寒体质」提高到八级。「心理暗示」提高到十级，新等级可以使用二到四次。「化腐为奇」提高到八级，并重新分配数值，最高级达到 100%，因此它的第 2 级和第 3 级比游戏原版弱。[/*]
[*][b]无尽天赋全模式可用[/b]：一个默认关闭的选项，在所有模式中提供无尽模式的天赋，并让它们像在无尽模式中一样生效。营地类天赋和「地图学家」仍只限无尽模式。[/*]
[*][b]天赋页[/b]：装了 Mod Manager 0.7.0 或更新版本后，New Loop Plus 的页面会多出「天赋」页，替你编辑天赋文件。可以修改任意轮回奖励的消耗、等级数、数值和文字，开关文件中的奖励，也可以点击「新建奖励」，用游戏奖励中已有的效果和模组自己的效果做出你自己的奖励，并可选用任意天赋或物品的图标。点击「保存」写入文件，改动在下次启动游戏时生效。[/*]
[/list]
天赋文件中所有天赋都关闭时，New Loop Plus 不会往存档里添加任何自己的数据。开启的天赋可能会把等级留在存档中；移除模组前请先关闭它并玩一局。信念点和消耗会在下次启动游戏时恢复为游戏原本的数值。

支持英文和中文。

[size=5]已知问题[/size]
[list]
[*]如果暂停菜单关闭五秒后游戏仍处于暂停状态（例如有其他窗口让游戏保持暂停），本局不会结束，屏幕上会有简短提示。关闭那个窗口后再试一次。[/*]
[*]信念点是整数，调整后的数值会四舍五入。较小的奖励乘以较小的倍数可能变成 0。[/*]
[*]危机和事件扣除的信念点不会被调整。[/*]
[*]天赋文件只在游戏启动时读取一次，修改后需要重启游戏，在「天赋」页保存的改动也一样。[/*]
[*]在「天赋」页保存会去掉天赋文件中的注释。原来的文件会以 [font=Courier New]NewLoopPlus.Talents.json.bak[/font] 保存在旁边。[/*]
[/list]

[url=https://www.nexusmods.com/survivallog/mods/73]获取 Mod Manager[/url]，在游戏里设置所有模组。[url=https://sldb.core-directive.com/zh/]打开 Survival Log 维基[/url]，物品、配方、地图一查就有。

[size=5]前置要求[/size]
[list]
[*]Survival Log 1.1.19611。游戏更新后模组可能暂时失效，直到模组更新为止。[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/12]BepInEx for Survival Log[/url]，请先安装。[/*]
[*][url=https://www.nexusmods.com/survivallog/mods/61]Exiled's Toolkit[/url] 0.19.0 或更新版本。没有它本模组不会加载。它把 New Loop Plus 的消息显示为弹出通知，并让在 Mod Manager 的「模组」界面改动的信念点倍数和开局奖励无需重启游戏即可生效。消耗相关的设置始终在下次启动游戏时生效。[/*]
[*]可选：Mod Manager 0.7.0 或更新版本，用于「天赋」页。没装的话，可以手动编辑天赋文件。[/*]
[*]可选：Wind Power 0.9.3 或更新版本，用于「风力工程师」和「风力调校」。没装的话，这两个奖励不会出现在游戏中，「天赋」页也不提供风力效果。[/*]
[/list]

[size=5]安装[/size]
[list=1]
[*]安装 [url=https://www.nexusmods.com/survivallog/mods/12]BepInEx[/url] [i]（如果之前没有装过其他模组，先启动一次游戏，让 BepInEx 完成初始化，然后退出）[/i]。[/*]
[*]用同样的方式安装 Exiled's Toolkit，下载它的压缩包并解压到游戏目录。[/*]
[*]在「文件」页下载压缩包。[/*]
[*]把压缩包解压到游戏目录（[font=Courier New]SurvivalLog.exe[/font] 所在的文件夹）。压缩包里带有 BepInEx 文件夹，DLL 会自动放到正确位置。如果提示合并文件夹，选择合并。解压后应为：
[code]C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\NewLoopPlus.dll[/code][/*]
[/list]

[size=5]卸载[/size]
从 [font=Courier New]BepInEx\plugins[/font] 文件夹中删除 [font=Courier New]NewLoopPlus.dll[/font]。已经发放的开局奖励不会收回。

[size=5]配置[/size]
可以在游戏内 Mod Manager 的「模组」界面修改，也可以在游戏关闭时编辑 [font=Courier New]BepInEx\config\com.dev.survivallog.newloopplus.cfg[/font]。配置文件中每项设置都有中文说明，完整说明见 [url=https://github.com/AnExiledDev/SurvivalLog-NewLoopPlus/blob/main/CONFIG.md]CONFIG.md[/url]（英文）。信念点倍数和开局奖励的改动立即生效；消耗相关的设置在下次启动游戏时生效。

[size=5]反馈[/size]
发现 bug 或有好点子？欢迎来 [url=https://discord.gg/trTsyxUFqg]Core Directive Discord[/url] 打个招呼，或在本页的「Posts」标签里发帖。报告 bug 时，请附上 [font=Courier New]BepInEx\LogOutput.log[/font] 和 [font=Courier New]BepInEx\new-loop-plus.log[/font]。
想直接联系我，可以在 Discord 上给 [b]exiled.dev[/b] 发消息，我在 [url=https://discord.gg/MZGg4gQAUe]Survival Log 官方 Discord[/url] 里。也可以在 X 上找我：[url=https://x.com/AnExiledDev]@AnExiledDev[/url]。

[size=5]源代码与许可[/size]
基于 MIT 许可开源：[url=https://github.com/AnExiledDev/SurvivalLog-NewLoopPlus]github.com/AnExiledDev/SurvivalLog-NewLoopPlus[/url]
