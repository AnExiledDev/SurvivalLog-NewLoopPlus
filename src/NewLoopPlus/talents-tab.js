// The Talents tab in the Mod Manager: what each loop reward costs, how many levels it has, what it gives, and the text
// the player reads, saved to NewLoopPlus.Talents.json. Runs in Root.html after page.js, and only once: the plugin
// sends both scripts again whenever it installs or shows a toast. Talks to the plugin through
// vuplex.postMessage("slmod|newloopplus|talents|...") and is answered through window.__slnlpTalents.
(function () {
  if (window.__slnlpTalents) return;
  if (!window.ExiledEditor || window.ExiledEditor.version < 1) return;

  // The shared editor from Exiled's Toolkit. It reads this tab's side from T, filled in below.
  const T = {};
  const {
    FX, FX_BY_ID, GAME_PREFIX, L, MAX_GIVES, MAX_LEVELS, NO_SCRIPT, REWARD_ICONS, TOAST_MS, VARS, adopt, afterRemove,
    autoDesc, autoLevelText, availableOptions, baseText, cat, choose, chooseButton, clampLevel, clone, closeIcons,
    closeOverlays, closePicker, closeToast, defaultAt, descOf, descTarget, discard, display, editorHtml, entryOf,
    entryOrGhost, esc, escapeTab, fieldName, fixedFrom, fmt, fromRoot, fx, fxIcon, fxName, givesChanged, h,
    hasLevelDescs, hasNoScript, hide, hideTab, hidesOf, iconsHtml, injectStyle, isArr, isAutoOn, isEditable, isNum,
    isObj, itemCount, levelCount, listIds, markChanged, match, matchesQuery, nameOf, nameOptions, nextStep, normText,
    num, numText, numsOf, onDocDown, onInput, onPickerDouble, onPickerKey, paramRow, pct, pickHtml, picksOf, plural,
    range, rawCache, rawFx, readsHtml, renderPicker, resetOrDelete, round2, roundTrips, saved, scriptOfGive, select,
    send, setPick, setText, showIconGroup, shown, showsAuto, stepper, tag, talentIcons, talentName, textIn, toRaw,
    unitOf, winEl, writeText, editorCss, searchHtml, noMatchHtml, clearSearch, onSearchKey, showSelectedRow, fitHeight,
    watchHeight, renderAfterChange, watchPresses, keepSelectionInView, gameCardsHtml, RAW_NOTE
  } = window.ExiledEditor.make(T);

  const PREFIX = 'slmod|newloopplus|';
  const GUID = 'com.dev.survivallog.newloopplus';
  const TAB_ID = 'talents';
  const TITLE = { en: 'Talents', zh: '天赋' };
  const FILE_NAME = 'NewLoopPlus.Talents.json';
  const STYLE_ID = 'slnlp-talents-css';
  const CUT = [1006, 2017, 3076, 4004, 4006];
  const FIRST_NEW = 5001;
  const LAST_ID = 9999;
  const NLP_PREFIX = 'nlp:';

  // Rewards the game left without an effect. New Loop Plus writes one effect row per level; the tab edits one number of it.
  const FINISHED = {
    1008: { fx: 'nlp:supplier', field: 'IntValue1', suffix: 0 },
    2004: { fx: 'nlp:junk', field: 'IntValue1', suffix: 0 },
    1010: { fx: 'nlp:reinforce', field: 'IntValue0', suffix: 1 },
    2006: { fx: 'nlp:yoga', field: 'BuffSatietyDel', suffix: 0 },
    2007: { fx: 'nlp:alone', field: 'BuffInterval', suffix: 0 }
  };

  // ===== Style =====

  // Each line starts with who it is for: N the tab, P the effect picker, B both, I the icon picker, T the toast.
  // "&" is the element itself; anything else sits inside it.
  const SCOPES = {
    N: ['#slmm .nlp'],
    P: ['#slmm .nlp-fxp'],
    B: ['#slmm .nlp', '#slmm .nlp-fxp'],
    I: ['#slmm .nlp-icons'],
    T: ['#slmm .nlp-toast']
  };

  const CSS = editorCss("nlp", []);

  // ===== State =====

  const st = {
    data: null,
    file: null,
    game: [],
    byId: {},
    scripts: new Set(),
    entries: [],
    ghosts: {},
    sel: 0,
    lv: 0,
    view: 'file',
    lang: 'en',
    pvLang: 'en',
    dirty: false,
    root: null,
    ctx: null,
    asked: false,
    picker: null,
    pickerEl: null,
    iconsEl: null,
    icons: [],
    iconGroup: REWARD_ICONS,
    toastEl: null,
    toastTimer: 0,
    pending: null,
    textOpen: false,
    listScroll: 0,
    edOpen: null,
    q: '',
    problems: []
  };

  // ===== Small helpers =====

  // ===== What a reward can give =====

  // ---- The game's effect scripts, with names and units a player can read. ----

  // ---- Game scripts only the game's in-run talents use. ----

  // ---- New Loop Plus: the effects it gives the rewards the game left unfinished. ----

  fx({ id: 'nlp:supplier', src: 'nlp', cat: 'items', script: 'PreDisaster_AddRandomItem', finishes: [1008],
    en: 'Special supplier', zh: '特殊供应商', what: ['Rare medicine waiting in your home\'s cabinets before the disaster.', '灾前居所柜子里留有稀有药物。'],
    params: [hide('IntValue0', 227), num('IntValue1', 'Rare medicines', '稀有药物', ['items', '件'], [1, 1, 2, 2, 3])],
    line: (v, zh) => itemCount(v.IntValue1, zh),
    desc: (v, zh) => zh ? '灾难前，特殊供应商会在居所的柜子里留下' + fmt(v.IntValue1) + '件稀有药物' : 'Before the disaster, your Special Supplier contact leaves ' + fmt(v.IntValue1) + ' rare medicine' + (v.IntValue1 === 1 ? '' : 's') + ' in your home\'s cabinets.' });

  fx({ id: 'nlp:junk', src: 'nlp', cat: 'items', script: 'PreDisaster_AddRandomItem', finishes: [2004],
    en: 'Salvaged raw materials', zh: '回收的基础材料', what: ['Raw materials salvaged from junk, waiting in your home\'s cabinets before the disaster.', '灾前居所柜子里留有从废品中回收的基础材料。'],
    params: [hide('IntValue0', 222), num('IntValue1', 'Raw materials', '基础材料', ['items', '件'], [3, 5, 7, 9, 12])],
    line: (v, zh) => itemCount(v.IntValue1, zh),
    desc: (v, zh) => zh ? '灾难前，从废品中回收的' + fmt(v.IntValue1) + '件基础材料会留在居所的柜子里' : 'Before the disaster, ' + fmt(v.IntValue1) + ' raw materials salvaged from junk are left in your home\'s cabinets.' });

  fx({ id: 'nlp:reinforce', src: 'nlp', cat: 'home', scripts: [], pickable: false, finishes: [1010],
    en: 'Reinforce doors and windows', zh: '加固门窗', what: ['Raises the Max Durability of your home\'s doors and windows.', '提高居所门窗的耐久上限。'],
    params: [num('IntValue0', 'Extra Max Durability', '额外耐久上限', ['points', '点'], [100, 200, 300, 400, 500], { step: 50 })],
    line: v => '+' + fmt(v.IntValue0),
    desc: (v, zh) => zh ? '居所的门窗得到加固，耐久上限+' + fmt(v.IntValue0) : 'Your home\'s doors and windows are reinforced: +' + fmt(v.IntValue0) + ' Max Durability.' });

  fx({ id: 'nlp:yoga', src: 'nlp', cat: 'body', scripts: [], pickable: false, finishes: [2006],
    en: 'Yoga slows hunger', zh: '瑜伽减缓饥饿', what: ['After yoga, Satiety drops slower for a while.', '修炼瑜伽后一段时间内饱腹下降更慢。'],
    params: [pct('BuffSatietyDel', 'Satiety drops slower by', '饱腹下降减少', [10, 15, 20, 25, 30], { scale: -100 })],
    line: v => fmt(v.BuffSatietyDel) + '%',
    desc: (v, zh) => zh ? '修炼瑜伽后，「神清气爽」期间饱腹下降减少' + fmt(v.BuffSatietyDel) + '%' : 'After yoga, Satiety drops ' + fmt(v.BuffSatietyDel) + '% slower while Vim and Vigor lasts.' });

  fx({ id: 'nlp:alone', src: 'nlp', cat: 'morale', scripts: [], pickable: false, finishes: [2007],
    en: 'Morale while alone', zh: '独处恢复心态', what: ['Morale comes back on a timer while you are Lonely.', '处于「孤独」状态时定时恢复心态。'],
    params: [num('BuffInterval', 'Hours per Morale point', '每次恢复心态间隔', ['h', '小时'], [5, 4, 3, 2, 1], { scale: 1 / 3600, min: 0.5, step: 0.5 })],
    line: (v, zh) => fmt(v.BuffInterval) + (zh ? '小时' : 'h'),
    desc: (v, zh) => zh ? '处于「孤独」状态时，每' + fmt(v.BuffInterval) + '小时恢复1点心态' : 'While Lonely, recover 1 Morale every ' + fmt(v.BuffInterval) + ' hours.' });

  // ---- New Loop Plus: effects it runs itself, written to the file as "nlp:<name>". ----

  fx({ id: 'nlp:interest', src: 'nlp', nlp: 'interest', cat: 'loop', scripts: [],
    en: 'Interest on kept points', zh: '信念点利息', what: ['A new loop pays a share of the Planning Points you kept.', '新轮回开始时，按保留的信念点支付利息。'],
    params: [pct('FloatValue0', 'Interest', '利息', [5, 10, 15])],
    line: v => '+' + fmt(v.FloatValue0) + '%',
    desc: (v, zh) => zh ? '新轮回开始时，获得保留信念点的' + fmt(v.FloatValue0) + '%' : 'Each new loop pays ' + fmt(v.FloatValue0) + '% interest on the Planning Points you kept.' });

  fx({ id: 'nlp:safety-net', src: 'nlp', nlp: 'safety-net', cat: 'loop', scripts: [],
    en: 'Minimum points a run pays', zh: '信念点保底', what: ['A run that pays less is topped up to this many Planning Points as the next loop starts.', '结算的信念点不足时补足到保底值。'],
    params: [num('IntValue0', 'At least', '至少', ['points', '点'], [100, 200, 300], { step: 50 })],
    line: (v, zh) => fmt(v.IntValue0) + (zh ? '点' : ' points'),
    desc: (v, zh) => zh ? '一局结算所得的信念点不足' + fmt(v.IntValue0) + '点时，在开始新轮回时补足' : 'A run that pays fewer than ' + fmt(v.IntValue0) + ' Planning Points is topped up to ' + fmt(v.IntValue0) + ' as the next loop starts.' });

  fx({ id: 'nlp:extra-pick', src: 'nlp', nlp: 'extra-pick', cat: 'loop', scripts: [],
    en: 'More reward choices', zh: '更多奖励选项', what: ['More rewards to choose from at the end of each day.', '每日结算时可选的奖励更多。'],
    params: [num('IntValue0', 'More choices', '额外选项', ['', '个'], [1, 2], { min: 1 })],
    line: v => '+' + fmt(v.IntValue0),
    desc: (v, zh) => zh ? '每日结算时多' + fmt(v.IntValue0) + '个奖励选项' : plural(v.IntValue0, 'more reward choice') + ' at the end of each day.' });

  fx({ id: 'nlp:crisis-veteran', src: 'nlp', nlp: 'crisis-veteran', cat: 'loop', scripts: [],
    en: 'More points from crises', zh: '危机信念点加成', what: ['Crises and events pay more Planning Points.', '危机与事件获得更多信念点。'],
    params: [pct('FloatValue0', 'More points', '加成', [10, 20, 30])],
    line: v => '+' + fmt(v.FloatValue0) + '%',
    desc: (v, zh) => zh ? '危机与事件获得的信念点+' + fmt(v.FloatValue0) + '%' : 'Crises and events pay ' + fmt(v.FloatValue0) + '% more Planning Points.' });

  fx({ id: 'nlp:second-wind', src: 'nlp', nlp: 'second-wind', cat: 'body', scripts: [],
    en: 'Survive a death', zh: '死里逃生', what: ['When your Life runs out, you live on instead, a set number of times per run.', '生命耗尽时免于死亡，每局有次数限制。'],
    params: [num('IntValue0', 'Times per run', '每局次数', ['', '次'], [1, 2], { min: 1 }), num('FloatValue0', 'Life left', '剩余生命', ['Life', '生命'], [30, 30], { min: 1 })],
    line: (v, zh) => fmt(v.IntValue0) + (zh ? '次' : 'x'),
    desc: (v, zh) => zh ? '生命耗尽时免于死亡，保留' + fmt(v.FloatValue0) + '点生命，每局' + fmt(v.IntValue0) + '次' : 'When your Life runs out, live on with ' + fmt(v.FloatValue0) + ' Life, ' + plural(v.IntValue0, 'time') + ' per run.' });

  fx({ id: 'nlp:lucky-finds', src: 'nlp', nlp: 'lucky-finds', cat: 'explore', scripts: [],
    en: 'Lucky finds when searching', zh: '搜索幸运', what: ['A chance of one more of an item when you search a container.', '搜索容器时有几率多获得一件物品。'],
    params: [pct('FloatValue0', 'Chance', '几率', [10, 20, 30])],
    line: v => fmt(v.FloatValue0) + '%',
    desc: (v, zh) => zh ? '搜索容器时，' + fmt(v.FloatValue0) + '%几率多获得一件物品' : fmt(v.FloatValue0) + '% chance of one more of an item when you search a container.' });

  fx({ id: 'nlp:muscle-memory', src: 'nlp', nlp: 'muscle-memory', cat: 'skills', scripts: [],
    en: 'Keep skill levels between runs', zh: '保留熟练度', what: ['A run starts with a share of the skill levels the last run gained.', '开局保留上一局熟练度提升的一部分。'],
    params: [pct('FloatValue0', 'Share kept', '保留', [25, 50])],
    line: v => fmt(v.FloatValue0) + '%',
    desc: (v, zh) => zh ? '开局保留上一局熟练度提升的' + fmt(v.FloatValue0) + '%' : 'Start each run with ' + fmt(v.FloatValue0) + '% of the skill levels the last run gained.' });

  fx({ id: 'nlp:scouts-notes', src: 'nlp', nlp: 'scouts-notes', cat: 'explore', scripts: [],
    en: 'Trading posts known from the start', zh: '开局已知交易点', what: ['Drone trading posts are on your map from the start.', '开局时地图上已显示无人机交易点。'],
    params: [num('IntValue0', 'Trading posts', '交易点', ['', '个'], [1, 2, 3], { min: 1 })],
    line: v => '+' + fmt(v.IntValue0),
    desc: (v, zh) => zh ? '开局时地图上已显示' + fmt(v.IntValue0) + '个无人机交易点' : 'Start with ' + plural(v.IntValue0, 'drone trading post') + ' on your map.' });

  fx({ id: 'nlp:tinkerer', src: 'nlp', nlp: 'tinkerer', cat: 'items', scripts: [],
    en: 'Materials back from crafting', zh: '制作返还材料', what: ['A chance that crafting at a workbench gives one of the materials back.', '在工作台制作时有几率返还一份材料。'],
    params: [pct('FloatValue0', 'Chance', '几率', [5, 10, 15, 20, 30])],
    line: v => fmt(v.FloatValue0) + '%',
    desc: (v, zh) => zh ? '在工作台制作时，' + fmt(v.FloatValue0) + '%几率返还一份材料' : fmt(v.FloatValue0) + '% chance that crafting at a workbench gives one of the materials back.' });

  fx({ id: 'nlp:family-recipe', src: 'nlp', nlp: 'family-recipe', cat: 'body', scripts: [],
    en: 'Better cooking quality', zh: '烹饪品质加成', what: ['Fresh ingredients and exact recipes add more cooking quality, and multi-pot cooking can unlock at a lower Cooking Level.', '新鲜食材和按菜谱烹饪带来的品质加成更高，多锅烹饪可在更低的烹饪熟练度解锁。'],
    params: [num('IntValue0', 'More quality', '品质加成', ['', ''], [3, 6, 9], { min: 0 }), num('IntValue1', 'Multi-pot levels sooner', '多锅烹饪提前', ['levels', '级'], [0, 0, 2], { min: 0 })],
    line: v => '+' + fmt(v.IntValue0),
    desc: (v, zh) => zh
      ? '新鲜食材和按菜谱烹饪的品质加成各+' + fmt(v.IntValue0) + (v.IntValue1 > 0 ? '，多锅烹饪提前' + fmt(v.IntValue1) + '级解锁' : '')
      : 'Fresh ingredients and exact recipes each add ' + fmt(v.IntValue0) + ' more cooking quality' + (v.IntValue1 > 0 ? ', and multi-pot cooking unlocks ' + plural(v.IntValue1, 'Cooking Level') + ' sooner.' : '.') });

  fx({ id: 'nlp:drone-pilot', src: 'nlp', nlp: 'drone-pilot', cat: 'money', scripts: [],
    en: 'Shorter drone trips', zh: '无人机往返更快', what: ['Trade drone trips take less time, never under a minute.', '无人机交易往返时间缩短，最短1分钟。'],
    params: [num('IntValue0', 'Seconds shorter', '缩短秒数', ['s', '秒'], [600, 900, 1200], { min: 0, step: 60 })],
    line: (v, zh) => '-' + fmt(v.IntValue0 / 60) + (zh ? '分钟' : ' min'),
    desc: (v, zh) => zh ? '无人机交易往返时间缩短' + fmt(v.IntValue0 / 60) + '分钟' : 'Trade drone trips take ' + plural(v.IntValue0 / 60, 'minute') + ' less.' });

  fx({ id: 'nlp:good-neighbor', src: 'nlp', nlp: 'good-neighbor', cat: 'body', scripts: [],
    en: 'More affinity per basket', zh: '吊篮好感度', what: ['Each basket sent to the neighbor or your companion gives more affinity.', '每次用吊篮给邻居或同伴送物，增加的好感度提高。'],
    params: [num('IntValue0', 'More affinity', '额外好感度', ['', ''], [5, 10, 15], { min: 0 })],
    line: v => '+' + fmt(v.IntValue0),
    desc: (v, zh) => zh ? '每次吊篮送物好感度额外+' + fmt(v.IntValue0) : 'Each basket gives ' + fmt(v.IntValue0) + ' more affinity.' });

  fx({ id: 'nlp:max-exposure', src: 'nlp', nlp: 'max-exposure', cat: 'explore', scripts: [],
    en: 'Higher Max Exposure', zh: '最大暴露上限提高', what: ['Max Exposure while exploring is higher, so trips out can last longer.', '探索时最大暴露上限提高，可以在外探索更久。'],
    params: [num('IntValue0', 'More exposure', '上限提高', ['', ''], [50, 100, 150], { min: 0, step: 50 })],
    line: v => '+' + fmt(v.IntValue0),
    desc: (v, zh) => zh ? '最大暴露上限 +' + fmt(v.IntValue0) : 'Max Exposure +' + fmt(v.IntValue0) + '.' });

  fx({ id: 'nlp:vehicle-discount', src: 'nlp', nlp: 'vehicle-discount', cat: 'money', scripts: [],
    en: 'Cheaper vehicles', zh: '载具降价', what: ['Vehicles cost less, at most 90% off.', '购买载具更便宜，最多减90%。'],
    params: [pct('FloatValue0', 'Off the price', '降价', [10, 20, 30])],
    line: v => '-' + fmt(v.FloatValue0) + '%',
    desc: (v, zh) => zh ? '载具价格降低' + fmt(v.FloatValue0) + '%' : 'Vehicles cost ' + fmt(v.FloatValue0) + '% less.' });

  fx({ id: 'nlp:travel-time', src: 'nlp', nlp: 'travel-time', cat: 'explore', scripts: [],
    en: 'Faster travel before the disaster', zh: '灾前出行更快', what: ['Travel takes less time before the disaster, with or without a vehicle, at most 90% less.', '灾前出行耗时减少，有无载具均生效，最多减90%。'],
    params: [pct('FloatValue0', 'Less time', '减少耗时', [10, 20, 30])],
    line: v => '-' + fmt(v.FloatValue0) + '%',
    desc: (v, zh) => zh ? '灾前出行耗时减少' + fmt(v.FloatValue0) + '%' : 'Travel before the disaster takes ' + fmt(v.FloatValue0) + '% less time.' });

  fx({ id: 'nlp:drone-speed', src: 'nlp', nlp: 'drone-speed', cat: 'money', scripts: [],
    en: 'Faster drone trips', zh: '无人机行程更快', what: ['Every drone trip takes less time, at most 90% less and never under a minute.', '所有无人机行程耗时减少，最多减90%，最短1分钟。'],
    params: [pct('FloatValue0', 'Less time', '减少耗时', [15, 30, 45])],
    line: v => '-' + fmt(v.FloatValue0) + '%',
    desc: (v, zh) => zh ? '无人机行程耗时减少' + fmt(v.FloatValue0) + '%' : 'Drone trips take ' + fmt(v.FloatValue0) + '% less time.' });

  // Offered only with Wind Power installed; it multiplies Wind Power's turbines.
  fx({ id: 'nlp:wind-power', src: 'nlp', nlp: 'wind-power', needs: 'windPower', cat: 'home', scripts: [],
    en: 'Better wind turbines', zh: '风力增效', what: ['Wind turbines make more power. Needs Wind Power.', '风力发电机产电更多。需要Wind Power模组。'],
    params: [pct('FloatValue0', 'More power', '产电增加', [15, 30, 45])],
    line: v => '+' + fmt(v.FloatValue0) + '%',
    desc: (v, zh) => zh ? '风力发电机产电提高' + fmt(v.FloatValue0) + '%' : 'Wind turbine output increased by ' + fmt(v.FloatValue0) + '%.' });

  FX.forEach(f => { FX_BY_ID[f.id] = f; });

  // ---- Params of an effect, and what a number in the file reads as. ----

  const isAvailable = f => f.needs ? !!(st.data && st.data[f.needs]) : !!f.nlp || f.scripts.some(s => st.scripts.has(s));

  // ---- Which effect a script and its numbers are. ----

  // ---- The whole catalogue, built once the game's data is in. ----

  function buildCatalogue() {
    Object.keys(rawCache).forEach(k => { delete rawCache[k]; });
    FX.forEach(f => { f.usedBy = []; });
    nameOptions();

    cat.icons = st.icons.length ? st.icons : talentIcons();
    cat.defaultIcon = cat.icons.find(i => i.icon.indexOf('xinzhongyoushu') >= 0) || cat.icons[0] || { icon: '', unityIcon: '' };

    st.game.forEach(t => {
      const first = t.levels[0];

      if (!first || NO_SCRIPT.indexOf(first.script) >= 0 || !st.scripts.has(first.script)) return;

      match(first.script, first.values).usedBy.push(t.id);
    });

    const covered = new Set();

    FX.filter(f => f.pickable && isAvailable(f)).forEach(f => f.scripts.forEach(s => covered.add(s)));
    st.scripts.forEach(s => { if (NO_SCRIPT.indexOf(s) < 0 && !covered.has(s)) rawFx(s); });

    cat.list = FX.filter(f => f.pickable && isAvailable(f)).concat(Object.keys(rawCache).sort().map(k => rawCache[k]).filter(f => !covered.has(f.script)));
  }

  // ===== Gives: one effect of a reward, with its picks and a number per level =====

  // The first game reward level that runs this script, which the plugin copies for a level with no effect of its own.
  function firstTpl(script) {
    if (!script) return {};

    for (const t of st.game) {
      const l = t.levels.find(x => x.script === script);

      if (l) return l.values;
    }

    return {};
  }

  // What the plugin starts a give's effect row from: the reward's own level when it runs the same script.
  function tplValues(script, tid, lv) {
    if (!script) return {};

    const t = st.byId[tid];
    const own = t ? t.levels[lv - 1] : null;

    return own && own.script === script ? own.values : firstTpl(script);
  }

  function makeGive(f, script, tid, levelValues, count) {
    const has = isArr(levelValues) && levelValues.length > 0;
    const v0 = has ? levelValues[0] : null;
    const g = { fx: f, tid, script: '', pick: {}, per: {}, fixed: {}, field: '' };

    picksOf(f).forEach(p => {
      const options = availableOptions(p);

      if (v0 && p.virtual) g.pick[p.key] = p.from(v0, script);
      else if (v0 && p.key in v0) g.pick[p.key] = v0[p.key];
      else g.pick[p.key] = options.some(o => o.v === p.def) || !options.length ? p.def : options[0].v;
    });

    g.script = f.nlp ? '' : f.scriptOf ? f.scriptOf(g) : (script || f.scripts[0]);

    if (f.init) f.init(g, tplValues(g.script, tid, 1));

    numsOf(f).forEach(p => {
      g.per[p.key] = range(count).map(i => has ? display(p, levelValues[Math.min(i, levelValues.length - 1)][fieldName(g, p)]) : defaultAt(p, i));
    });
    g.fixed = v0 ? fixedFrom(g, v0) : {};

    return g;
  }

  function writeGive(g, count) {
    const f = g.fx;
    const choice = clone(g.fixed) || {};
    const nums = numsOf(f);
    const out = { effect: f.nlp ? NLP_PREFIX + f.nlp : GAME_PREFIX + g.script };

    hidesOf(f).forEach(p => { choice[p.key] = p.def; });
    picksOf(f).filter(p => !p.virtual).forEach(p => { choice[p.key] = g.pick[p.key]; });

    if (f.zeroOthers) {
      [tplValues(g.script, g.tid, 1), firstTpl(g.script)].forEach(t => Object.keys(t).forEach(k => {
        if (!(k in choice) && isNum(t[k]) && t[k] !== 0 && f.zeroOthers(k, g)) choice[k] = 0;
      }));
    }

    if (Object.keys(choice).length) out.choice = choice;

    if (nums.length) {
      out.levels = range(count).map(i => {
        const level = {};

        nums.forEach(p => {
          const field = fieldName(g, p);

          level[field] = toRaw(p, field, g.per[p.key][i]);
        });

        return level;
      });
    }

    return out;
  }

  // The effect row a give in the file ends up as at level i, the way the plugin builds it.
  function effectValues(give, tid, i) {
    const levels = isArr(give.levels) ? give.levels : [];
    const level = levels.length ? levels[Math.min(i, levels.length - 1)] : {};

    return Object.assign({}, tplValues(scriptOfGive(give), tid, i + 1), isObj(give.choice) ? give.choice : {}, isObj(level) ? level : {});
  }

  function giveFor(fileGive, tid, count) {
    if (!isObj(fileGive) || typeof fileGive.effect !== 'string') return null;

    if (fileGive.effect.indexOf(NLP_PREFIX) === 0) return nlpGiveFor(fileGive, tid, count);

    if (fileGive.effect.indexOf(GAME_PREFIX) !== 0) return null;

    const script = scriptOfGive(fileGive);

    if (!st.scripts.has(script)) return null;

    const vals = range(count).map(i => effectValues(fileGive, tid, i));

    for (const f of [match(script, vals[0]), rawFx(script)]) {
      const g = makeGive(f, script, tid, vals, count);

      if (roundTrips(g, vals, count)) return g;
    }

    return null;
  }

  function nlpGiveFor(fileGive, tid, count) {
    const name = fileGive.effect.slice(NLP_PREFIX.length);
    const f = FX.find(x => x.nlp === name);

    if (!f) return null;

    const vals = range(count).map(i => effectValues(fileGive, tid, i));
    const g = makeGive(f, '', tid, vals, count);

    return roundTrips(g, vals, count) ? g : null;
  }

  // What a game reward gives as it stands. Shown locked when the tab could not write it back the same way.
  function baseGives(e, base, count) {
    const levels = base.levels;
    const script = levels.length ? levels[0].script : '';

    if (NO_SCRIPT.indexOf(script) >= 0) return [];

    const vals = levels.map(l => l.values);
    const g = makeGive(match(script, vals[0]), script, e.id, vals, count);
    const rawLevels = isArr(e.raw.levels) ? e.raw.levels : [];
    const isMixed = levels.some(l => l.script !== script);
    const hasOwnBuff = rawLevels.some(l => isObj(l) && 'buff' in l);

    if (!st.scripts.has(script) || isMixed || hasOwnBuff || !roundTrips(g, vals, count)) e.givesLocked = true;

    return [g];
  }

  const finishedRowId = (id, i, suffix) => 200000000 + id * 100 + (i + 1) * 10 + suffix;

  function finishedGive(e) {
    const fin = FINISHED[e.id];
    const f = FX_BY_ID[fin.fx];
    const p = numsOf(f)[0];
    const rows = e.raw.effects;
    const g = { fx: f, tid: e.id, script: '', pick: {}, per: {}, fixed: {}, field: '' };

    g.per[p.key] = range(e.levels.length).map(i => {
      const row = rows.find(r => isObj(r) && r.id === finishedRowId(e.id, i, fin.suffix));
      const raw = row && isObj(row.fields) ? row.fields[fin.field] : undefined;

      if (!isNum(raw)) e.frozen = true;

      return display(p, raw);
    });

    return g;
  }

  // ===== Entries: the rewards in the file, as the tab edits them =====

  function entryFromRaw(raw, rawIndex) {
    const id = raw.id;
    const game = st.byId[id] || null;
    const copySrc = isNum(raw.copyFrom) ? st.byId[raw.copyFrom] || null : null;
    const fields = isObj(raw.fields) ? raw.fields : {};
    const fresh = !game && isArr(raw.gives) && typeof fields.WebIcon === 'string';
    const kind = game && CUT.indexOf(id) >= 0 ? 'cut' : FINISHED[id] && isArr(raw.effects) ? 'finished' : game ? 'changed' : copySrc ? 'new' : 'unknown';
    const base = fresh ? null : (game || copySrc);
    const rawLevels = isArr(raw.levels) ? raw.levels : null;
    const baseCount = base ? base.levels.length : 0;
    const count = rawLevels ? rawLevels.length : baseCount;
    const e = {
      id, kind, fresh, game, base, copySrc, raw, rawIndex,
      enabled: raw.enabled !== false,
      copyFrom: raw.copyFrom,
      name: normText(raw.name),
      description: normText(raw.description),
      levelText: normText(raw.levelText),
      column: isNum(fields.ShowType) ? fields.ShowType : base && (base.show === 1 || base.show === 2) ? base.show : id < 2000 ? 1 : 2,
      icon: fresh ? fromRoot(fields.WebIcon) : base ? base.icon : cat.defaultIcon.icon,
      unityIcon: typeof fields.Icon === 'string' ? fields.Icon : base ? base.unityIcon : cat.defaultIcon.unityIcon,
      levels: [],
      gives: [],
      givesLocked: false,
      givesTouched: false,
      frozen: false,
      autoText: false,
      autoManual: false,
      touched: false,
      removed: false
    };

    e.levels = range(count).map(i => {
      const rl = rawLevels && isObj(rawLevels[i]) ? rawLevels[i] : {};
      const bl = baseCount ? base.levels[Math.min(i, baseCount - 1)] : null;

      return {
        cost: isNum(rl.cost) ? rl.cost : bl ? bl.cost : 0,
        gameCost: bl && i < baseCount ? bl.cost : null,
        desc: normText(rl.description),
        gameDesc: bl ? bl.desc : null,
        isNew: !base || i >= baseCount,
        raw: rawLevels ? rl : null
      };
    });

    const hasExtras = ['effects', 'buffs', 'reinforce'].some(k => isArr(raw[k]) && raw[k].length > 0);

    if (kind === 'unknown' || count === 0 || (kind !== 'finished' && hasExtras)) e.frozen = true;

    if (!e.frozen) {
      if (kind === 'finished') e.gives = [finishedGive(e)];
      else if (isArr(raw.gives)) e.gives = raw.gives.map(gv => giveFor(gv, id, count));
      else if (base) e.gives = baseGives(e, base, count);
    }

    if (e.gives.some(g => !g)) {
      e.frozen = true;
      e.gives = [];
    }

    const own = normText(raw.levelText);

    e.autoText = e.gives.length > 0 && !!own && own.en === autoLevelText(e, false) && own.zh === autoLevelText(e, true);

    return e;
  }

  const canEditGives = e => isEditable(e) && e.kind !== 'finished' && !e.givesLocked;

  // ===== The text the player reads =====

  function levelLineOf(e, lang) {
    if (showsAuto(e)) return autoLevelText(e, lang === 'zh');

    return shown(e.levelText, baseText(e, 'levelText'), lang);
  }

  // ===== Building and changing the list =====

  function buildEntries(keepId) {
    const talents = st.file && isArr(st.file.talents) ? st.file.talents : [];

    st.entries = [];
    st.ghosts = {};
    talents.forEach((raw, i) => {
      if (isObj(raw) && isNum(raw.id)) st.entries.push(entryFromRaw(raw, i));
    });
    st.dirty = false;

    st.sel = keepId && entryOrGhost(keepId) ? keepId : 0;
    keepSelectionInView();
    st.lv = Math.min(st.lv, Math.max(0, levelCount(st.sel) - 1));
  }

  function ghostOf(id) {
    if (!st.byId[id]) return null;

    if (!st.ghosts[id]) st.ghosts[id] = entryFromRaw({ id, enabled: true }, -1);

    return st.ghosts[id];
  }

  function nextFreeId() {
    for (let id = FIRST_NEW; id <= LAST_ID; id++) {
      if (!st.byId[id] && !st.entries.some(e => e.id === id && !e.removed)) return id;
    }

    return 0;
  }

  function freshCopyFrom(column) {
    const t = st.game.find(x => x.show === column && !x.endless && x.available === 1) || st.game[0];

    return t ? t.id : 0;
  }

  function copyInto(srcId) {
    const src = st.byId[srcId];
    const id = nextFreeId();

    if (!src || !id) return null;

    const e = entryFromRaw({ id, enabled: true, copyFrom: srcId, name: { en: src.name.en + ' II', zh: src.name.zh + ' II' } }, -1);

    st.entries.push(e);
    markChanged(e);

    return e;
  }

  function newReward() {
    const id = nextFreeId();

    if (!id) return null;

    const icon = cat.defaultIcon;
    const e = entryFromRaw({
      id, enabled: true, copyFrom: freshCopyFrom(2), name: { en: 'New reward', zh: '新奖励' },
      fields: { ShowType: 2, WebIcon: '../../' + icon.icon, Icon: icon.unityIcon },
      levels: [{ cost: 5 }, { cost: 10 }, { cost: 15 }], gives: []
    }, -1);

    e.autoText = true;
    st.entries.push(e);
    markChanged(e);

    return e;
  }

  // ---- Levels ----

  function addLevel(e) {
    const costs = e.levels.map(l => l.cost);
    const last = e.levels[e.levels.length - 1];

    e.levels.push({ cost: Math.max(0, Math.round(nextStep(costs))), gameCost: null, desc: last && last.desc ? clone(last.desc) : null, gameDesc: last ? last.gameDesc : null, isNew: true, raw: null });
    e.gives.forEach(g => numsOf(g.fx).forEach(p => {
      const per = g.per[p.key];

      per.push(Math.max(p.min, round2(nextStep(per))));
    }));

    if (canEditGives(e)) e.givesTouched = true;

    markChanged(e);
  }

  function removeLevel(e) {
    if (e.levels.length < 2) return;

    e.levels.pop();
    e.gives.forEach(g => numsOf(g.fx).forEach(p => { g.per[p.key].pop(); }));

    if (canEditGives(e)) e.givesTouched = true;

    markChanged(e);
  }

  // ---- Gives ----

  // ===== Saving =====

  const writesGives = e => e.kind !== 'finished' && canEditGives(e) && (e.givesTouched || e.fresh) && e.gives.length > 0;

  function writesLevels(e) {
    const base = e.base;

    if (e.fresh || isAutoOn(e) || writesGives(e) || isArr(e.raw.levels)) return true;

    if (!base || base.levels.length !== e.levels.length) return true;

    return e.levels.some(l => l.desc || l.cost !== l.gameCost);
  }

  function outLevels(e, auto) {
    const writeGivesNow = writesGives(e);

    return e.levels.map((l, i) => {
      const out = l.raw ? clone(l.raw) : {};
      const hadCost = l.raw && 'cost' in l.raw;

      if (l.isNew || hadCost || l.cost !== l.gameCost) out.cost = l.cost;
      else delete out.cost;

      if (writeGivesNow) delete out.buff;

      const desc = auto ? { en: autoDesc(e, i, false), zh: autoDesc(e, i, true) } : l.desc ? writeText(l.desc, l.gameDesc) : undefined;

      if (desc) out.description = desc;
      else delete out.description;

      return out;
    });
  }

  function outEntry(e) {
    if (e.frozen) return Object.assign(clone(e.raw), { enabled: e.enabled });

    const auto = isAutoOn(e);
    const out = { id: e.id, enabled: e.enabled };
    const managed = ['id', 'enabled', 'copyFrom', 'name', 'levelText', 'description', 'levels', 'gives'];
    const name = writeText(e.name, baseText(e, 'name'));
    const levelText = auto ? { en: autoLevelText(e, false), zh: autoLevelText(e, true) } : writeText(e.levelText, baseText(e, 'levelText'));
    const description = auto ? undefined : writeText(e.description, null);

    if (isNum(e.copyFrom)) out.copyFrom = e.copyFrom;

    if (e.name || e.fresh || e.kind === 'new') out.name = name;

    if (levelText && (auto || e.levelText)) out.levelText = levelText;

    if (description) out.description = description;

    if (e.fresh) {
      out.fields = Object.assign(clone(e.raw.fields) || {}, { Icon: e.unityIcon, WebIcon: '../../' + e.icon, ShowType: e.column });
      managed.push('fields');
    }

    if (writesLevels(e)) out.levels = outLevels(e, auto);

    if (writesGives(e)) out.gives = e.gives.map(g => writeGive(g, e.levels.length));
    else if (isArr(e.raw.gives) && !e.givesTouched) out.gives = clone(e.raw.gives);

    if (e.kind === 'finished') {
      const fin = FINISHED[e.id];
      const g = e.gives[0];
      const p = numsOf(g.fx)[0];
      const effects = clone(e.raw.effects);

      e.levels.forEach((l, i) => {
        const row = effects.find(r => isObj(r) && r.id === finishedRowId(e.id, i, fin.suffix));

        if (row) row.fields[fin.field] = toRaw(p, fin.field, g.per[p.key][i]);
      });
      out.effects = effects;
      managed.push('effects');
    }

    Object.keys(e.raw).forEach(k => {
      if (managed.indexOf(k) < 0) out[k] = clone(e.raw[k]);
    });

    return out;
  }

  function toFile() {
    const file = clone(st.file);
    const talents = isArr(st.file.talents) ? st.file.talents : [];
    const out = [];

    talents.forEach((raw, i) => {
      const e = st.entries.find(x => x.rawIndex === i);

      if (!e) out.push(clone(raw));
      else if (e.removed) return;
      else if (e.touched) out.push(outEntry(e));
      else out.push(Object.assign(clone(raw), { enabled: e.enabled }));
    });
    st.entries.filter(e => e.rawIndex < 0 && !e.removed).forEach(e => out.push(outEntry(e)));
    file.talents = out;

    return file;
  }

  // Why the tab will not save right now, or '' when it can.
  function saveBlocker() {
    if (!st.file) return L('The talents file could not be read, so this tab will not save over it.', '无法读取天赋文件，本页不会覆盖它。');

    const empty = st.entries.find(e => !e.removed && e.fresh && !e.gives.length);

    if (empty) return L('Choose what it gives first: ', '请先选择效果：') + nameOf(empty, st.lang);

    return '';
  }

  function save() {
    const blocker = saveBlocker();

    if (blocker) {
      toast(esc(blocker));

      return;
    }

    st.pending = toFile();
    send('talents|save|' + JSON.stringify(st.pending));
  }

  // ===== Drawing the tab =====

  const isOwn = e => e.fresh || e.kind === 'new';

  function render() {
    const root = st.root;

    if (!root) return;

    const list = root.querySelector('.nlp-list');
    const ed = root.querySelector('.ed');
    const edScroll = ed && st.sel === st.edOpen ? ed.scrollTop : 0;

    if (list) st.listScroll = list.scrollTop;

    st.edOpen = st.sel;
    root.innerHTML = topHtml() + bannersHtml() + (st.data ? mainHtml() : '<div class="nlp-wait">' + L('Reading the loop rewards...', '正在读取轮回奖励...') + '</div>');
    fitHeight();

    const newList = root.querySelector('.nlp-list');
    const newEd = root.querySelector('.ed');

    if (newList) newList.scrollTop = st.listScroll;

    if (newEd) newEd.scrollTop = edScroll;

    footer();
  }

  function topHtml() {
    const fileCount = st.entries.filter(e => !e.removed).length;
    const allCount = allIds().length;
    const chip = (v, en, zh, n) => '<span data-a="view" data-v="' + v + '"' + (st.view === v ? ' class="is-on"' : '') + '>' + L(en, zh) + ' ' + n + '</span>';

    return '<div class="nlp-top">' +
      '<div class="nlp-head"><div class="nlp-title">' + L('LOOP REWARDS', '轮回奖励') + '</div>' +
      '<div class="nlp-sub">' + L('What each reward costs, how many levels it has, and what it gives. Applies at the next game start.', '每个奖励的消耗、等级数和效果。下次启动游戏时生效。') + '</div></div>' +
      '<span class="key-btn" data-a="discard">' + L('DISCARD', '放弃') + '</span>' +
      '<span class="key-btn' + (st.dirty ? ' is-due' : '') + '" data-a="save">' + L('SAVE', '保存') + '</span>' +
      '</div>' +
      '<div class="nlp-bar">' +
      '<div class="nlp-chips">' + chip('file', 'CHANGED', '已修改', fileCount) + chip('all', 'ALL REWARDS', '全部奖励', allCount) + '</div>' +
      searchHtml() +
      '<span class="new-btn" data-a="new">+ ' + L('NEW REWARD', '新建奖励') + '</span>' +
      '</div>';
  }

  function bannersHtml() {
    let html = '';

    if (st.problems.length) html += '<div class="nlp-probs">' + st.problems.map(esc).join('<br>') + '</div>';

    if (st.data && !st.file) {
      html += '<div class="nlp-bad">' + L('The talents file could not be read, so this tab will not save over it. Fix or delete NewLoopPlus.Talents.json, then restart the game.', '无法读取天赋文件，本页不会覆盖它。请修复或删除 NewLoopPlus.Talents.json，然后重启游戏。') + '</div>';
    }

    return html;
  }

  // ---- The list ----

  function allIds() {
    // Type 2 rows are survival plans. They share the talent table but are not loop rewards, and their row IDs leave no room for more levels.
    const ids = st.game.filter(t => t.type !== 2 && (t.show === 1 || t.show === 2)).map(t => t.id);

    st.entries.forEach(e => {
      if (!e.removed && !st.byId[e.id]) ids.push(e.id);
    });

    return ids;
  }

  function columnOf(id) {
    const e = entryOf(id);

    if (e) return e.column;

    const t = st.byId[id];

    return t && (t.show === 1 || t.show === 2) ? t.show : id < 2000 ? 1 : 2;
  }

  // The list draws its first column above its second, so the search box's Up, Down and Enter go in that order too.
  const compareIds = (a, b) => columnOf(a) - columnOf(b) || a - b;

  function mainHtml() {
    const sel = entryOrGhost(st.sel);

    return '<div class="nlp-main">' +
      '<div class="nlp-list" data-list="">' + rowsHtml() + '</div>' +
      '<div class="ed">' + (sel ? editorHtml(sel) : '') + '</div>' +
      '</div>';
  }

  // The rows of the list. A search redraws only these, and leaves out a column it finds nothing in.
  function rowsHtml() {
    const ids = listIds();
    const isSearching = !!st.q.trim();
    const col = (n, en, zh) => {
      const mine = ids.filter(id => columnOf(id) === n);

      if (!mine.length && isSearching) return '';

      return '<div class="col-h">' + L(en, zh) + '<small>' + mine.length + '</small></div>' + mine.map(rowHtml).join('');
    };

    if (!ids.length && isSearching) return noMatchHtml();

    if (!ids.length && st.view === 'file') {
      return '<div class="nomatch">' + L('You have not changed any rewards yet.', '还没有改过任何奖励。') +
        '<br><span class="gbtn dim" data-a="view" data-v="all">' + L('SHOW ALL REWARDS', '查看全部奖励') + '</span></div>';
    }

    return col(1, 'PREPARATION', '筹备能力') + col(2, 'SURVIVAL', '生存能力');
  }

  function rowTag(e, id) {
    if (!e) return CUT.indexOf(id) >= 0 ? tag('cut', 'CUT', '未上线') : '';

    if (e.kind === 'cut') return tag('cut', 'CUT', '未上线');

    if (e.kind === 'finished') return tag('fin', 'FINISHED BY MOD', '模组完成');

    if (isOwn(e)) return tag('new', 'NEW', '新');

    return tag('is-chg', 'CHANGED', '已修改');
  }

  function rowHtml(id) {
    const e = entryOf(id);
    const t = st.byId[id];
    const name = e ? nameOf(e, st.lang) : textIn(t.name, st.lang);
    const icon = e ? e.icon : t.icon;
    const count = e ? e.levels.length : t.levels.length;
    const look = e || ghostOf(id);
    const desc = look && count ? descOf(look, 0, st.lang) : '';
    const cls = ['ab'];

    if (id === st.sel) cls.push('is-sel');

    if (!e && t && t.available === 0) cls.push('is-out');

    if (e && !e.enabled) cls.push('is-off');

    const toggle = e ? '<label class="setting-toggle" title="' + esc(e.kind === 'changed' ? L('Switch your changes on or off', '开关你的修改') : L('Switch this reward on or off', '开关此奖励')) + '">' +
      '<input type="checkbox" data-a="tog" data-v="' + id + '"' + (e.enabled ? ' checked' : '') + '><span class="track"></span><span class="knob"></span></label>' : '<span></span>';

    return '<div class="' + cls.join(' ') + '" data-a="sel" data-v="' + id + '">' +
      '<img src="' + esc(icon) + '" alt="">' +
      '<div><div class="nm">' + esc(name) + '</div>' + (desc ? '<div class="ds">' + esc(desc) + '</div>' : '') + '<div class="mt">' + rowTag(e, id) + '<span class="lvn">' + (st.lang === 'zh' ? count + '级' : count + (count === 1 ? ' level' : ' levels')) + '</span></div></div>' +
      toggle + '</div>';
  }

  // ---- The editor ----

  function previewHtml(e) {
    const lang = st.pvLang;
    const Lp = (en, zh) => lang === 'zh' ? zh : en;
    const n = e.levels.length;
    const i = Math.max(0, Math.min(st.lv, n - 1));
    const line = levelLineOf(e, lang);
    const parts = line.split(/\s*\/\s*/);
    const lineHtml = n > 1 && parts.length === n ? parts.map((p, k) => k === i ? '<b>' + esc(p) + '</b>' : esc(p)).join(' / ') : esc(line);
    const desc = descOf(e, i, lang);
    const cost = n ? e.levels[i].cost : 0;

    return '<div class="pv">' +
      '<img src="' + esc(e.icon) + '" alt="">' +
      '<div><div class="pv-name">' + esc(nameOf(e, lang)) + '<span class="lvb">Lv.' + (i + 1) + ' / ' + n + '</span></div>' +
      '<div class="pv-lt">' + lineHtml + '</div>' +
      (desc ? '<div class="pv-desc">' + esc(desc) + '</div>' : '<div class="pv-desc is-empty">' + Lp('Pick what it gives and its description shows up here.', '选好效果后，描述会显示在这里。') + '</div>') +
      '<div class="pv-cost">' + Lp('Cost', '消耗') + ' <b>' + numText(cost) + '</b> ' + Lp('Planning Points', '信念点') + '</div></div>' +
      '</div>';
  }

  function whyHtml(e, ghost) {
    const note = text => '<div class="nnote">' + text + '</div>';
    let html;

    if (e.frozen && !ghost) html = note(L('This reward uses parts of the file the tab cannot edit. Change it in NewLoopPlus.Talents.json.', '此奖励使用了本页无法编辑的文件内容。请在 NewLoopPlus.Talents.json 中修改。'));
    else if (ghost) html = note(L('This is one of the game\'s rewards, and it hasn\'t been changed.', '这是游戏中的奖励，没有改动过。'));
    else if (e.fresh) html = note(L('A reward of your own. Pick an icon, a name and what it gives. The text writes itself from the numbers.', '你自己的奖励。选图标、名称和效果。文字会根据数字自动生成。'));
    else if (e.kind === 'cut') html = note(L('The game left this reward\'s effect in place but never offers it. Switching it on adds it to the loop screen as it is.', '游戏保留了此奖励的效果但从未提供。打开后它会原样出现在轮回界面。'));
    else if (e.kind === 'finished') html = note(L('The game never finished this reward. New Loop Plus adds the missing effect. The numbers above are what it gives.', '游戏未完成此奖励。新轮回+补上了缺失的效果。上面的数字就是它给的。'));
    else if (e.kind === 'new') html = note(L('A new reward, copied from', '一个新奖励，复制自') + ' <b>' + esc(e.copySrc ? textIn(e.copySrc.name, st.lang) : '#' + e.copyFrom) + '</b>');
    else html = note(L('You can change what it gives, its cost, levels and text here.', '这里可以改效果、消耗、等级和文字。'));

    if (!ghost && !e.frozen && e.givesLocked) html += note(L('You can change this reward\'s cost, levels and text here, but not what it gives.', '这里可以修改此奖励的消耗、等级和文字，但不能修改它的效果。'));

    if (hasNoScript(e)) html += note(L('The game\'s code runs this reward, so there is no effect to show. Any effect you add works on top of it.', '此奖励由游戏的代码运行，没有可显示的效果。添加的效果会额外生效。'));

    return html;
  }

  function lookHtml(e) {
    const seg = (n, en, zh) => '<span data-a="col" data-v="' + n + '"' + (e.column === n ? ' class="is-on"' : '') + '>' + L(en, zh) + '</span>';
    const name = e.name || { en: '', zh: '' };

    return '<div class="sec-h">' + L('LOOK', '外观') + '<small>' + L('Icon, name, and which column it sits in', '图标、名称和所在栏') + '</small></div>' +
      '<div class="look">' +
      '<div class="look-ic" data-a="icon"><img src="' + esc(e.icon) + '" alt=""><span>' + L('CHANGE', '更换') + '</span></div>' +
      '<div class="look-f">' +
      '<span></span><span class="k">English</span><span class="k">中文</span>' +
      '<span class="k">' + L('Name', '名称') + '</span>' +
      '<input data-a="txt" data-k="name:en" value="' + esc(name.en) + '">' +
      '<input data-a="txt" data-k="name:zh" value="' + esc(name.zh) + '">' +
      '<span class="k">' + L('Column', '栏') + '</span>' +
      '<span><span class="seg2">' + seg(1, 'PREPARATION', '筹备能力') + seg(2, 'SURVIVAL', '生存能力') + '</span></span><span></span>' +
      '</div></div>';
  }

  function srcTag(f) {
    return f.src === 'nlp' ? tag('src-nlp', 'NEW LOOP PLUS', '新轮回+') : tag('src-game', 'GAME', '游戏');
  }

  function givesHtml(e) {
    const editable = canEditGives(e);
    const finished = e.kind === 'finished';
    const canRemove = editable && (e.fresh || e.gives.length > 1);
    const head = '<div class="sec-h">' + L('WHAT IT GIVES', '效果') + '<small>' + L('Effects from the game and New Loop Plus', '来自游戏和新轮回+的效果') + '</small></div>';

    if (!e.gives.length) {
      if (!editable) return '';

      return head + '<div class="gv"><div class="gv-empty">' + L('Nothing yet. A reward needs at least one effect.', '还没有效果。奖励至少需要一个效果。') +
        '<br><span class="gbtn" data-a="add">' + L('CHOOSE WHAT IT GIVES', '选择效果') + '</span></div></div>';
    }

    const rows = e.gives.map((g, gi) => '<div class="gv-row">' +
      '<img src="' + esc(fxIcon(g.fx)) + '" alt="">' +
      '<div><div class="gv-n">' + esc(fxName(g.fx, st.lang === 'zh')) + srcTag(g.fx) + '</div><div class="gv-w">' + esc(L(g.fx.what[0], g.fx.what[1])) + '</div></div>' +
      '<div class="gv-picks">' + picksOf(g.fx).map(p => pickHtml(e, g, gi, p)).join('') + '</div>' +
      (editable && !finished ? '<a data-a="chg" data-c="' + gi + '">' + L('CHANGE', '更换') + '</a>' : '<span></span>') +
      (canRemove && !finished ? '<span class="x" data-a="rm" data-c="' + gi + '" title="' + L('Remove', '移除') + '">&#x2715;</span>' : '<span></span>') +
      '</div>').join('');
    const add = editable && !finished && e.gives.length < MAX_GIVES ? '<div class="gv-add" data-a="add">+ ' + L('ADD ANOTHER EFFECT', '再添加一个效果') + '</div>' : '';

    return head + '<div class="gv">' + rows + add + '</div>';
  }

  function levelsHtml(e) {
    const n = e.levels.length;
    const finished = e.kind === 'finished';
    const valuesLocked = e.givesLocked;
    const total = e.levels.reduce((sum, l) => sum + l.cost, 0);
    const chips = e.levels.map((l, i) => '<span data-a="lv" data-v="' + i + '"' + (i === st.lv ? ' class="is-on"' : '') + '>Lv.' + (i + 1) + '</span>').join('');
    const canAdd = !finished && n < MAX_LEVELS;
    const cols = [];

    e.gives.forEach((g, gi) => numsOf(g.fx).forEach(p => cols.push({ g, gi, p })));

    const many = e.gives.length > 1;
    const head = '<tr><th>' + L('LEVEL', '等级') + '</th><th>' + L('COST', '消耗') + '</th>' +
      cols.map(c => '<th>' + esc(L(c.p.en, c.p.zh).toUpperCase()) + (many ? '<small>' + esc(fxName(c.g.fx, st.lang === 'zh')) + '</small>' : '') + '</th>').join('') + '<th></th></tr>';
    const rows = e.levels.map((l, i) => '<tr' + (i === st.lv ? ' class="is-on"' : '') + '>' +
      '<td class="l" data-a="lv" data-v="' + i + '">Lv.' + (i + 1) + '</td>' +
      '<td>' + stepper('c:' + i, l.cost, '', false) + '</td>' +
      cols.map(c => '<td>' + stepper('g:' + c.gi + ':' + c.p.key + ':' + i, c.g.per[c.p.key][i], unitOf(c.p), valuesLocked) + '</td>').join('') +
      (i > 0 && i === n - 1 && !finished ? '<td class="x" data-a="rmlv" title="' + L('Remove', '移除') + '">&#x2715;</td>' : '<td></td>') +
      '</tr>').join('');

    return '<div class="lvs">' + chips +
      (canAdd ? '<span class="is-add" data-a="addlv">+ ' + L('LEVEL', '等级') + '</span>' : '') +
      '<span class="total">' + L('To max level', '升满共需') + ' <b>' + numText(total) + '</b> ' + L('points', '点') + '</span></div>' +
      '<table class="lt">' + head + rows + '</table>';
  }

  function textHtml(e) {
    const open = st.textOpen;
    const head = '<div class="fold"><div class="fold-t" data-a="fold">' + (open ? '&#9660; ' : '&#9654; ') + L('TEXT THE PLAYER SEES', '玩家看到的文字') + '</div>';

    if (!open) return head + '</div>';

    const canAuto = e.gives.length > 0 && e.gives.every(g => !g.fx.raw) && !e.givesLocked;
    const auto = isAutoOn(e);
    const i = Math.min(st.lv, e.levels.length - 1);
    const d = descTarget(e);
    const inputs = (key, own, game) => ['en', 'zh'].map(lang => '<input data-a="txt" data-k="' + key + ':' + lang + '" value="' + esc(own ? own[lang] : '') + '" placeholder="' + esc(textIn(game, lang)) + '">').join('');
    const autoCells = (en, zh) => '<span class="auto">' + esc(en) + '</span><span class="auto">' + esc(zh) + '</span>';
    const descLabel = L('Description', '描述') + (auto || d.perLevel ? ' (Lv.' + (i + 1) + ')' : '');
    let html = head;

    if (canAuto) html += '<label class="chk"><input type="checkbox" data-a="auto"' + (auto ? ' checked' : '') + '>' + L('Write the description and level line from the numbers above', '根据上面的数字自动生成描述和等级文字') + '</label>';

    html += '<div class="tx"><span></span><span class="hd2">ENGLISH</span><span class="hd2">中文</span>';
    html += '<span class="k">' + descLabel + '</span>' + (auto ? autoCells(autoDesc(e, i, false), autoDesc(e, i, true)) : inputs('desc', d.own, d.game));
    html += '<span class="k">' + L('Level line', '等级文字') + '</span>' + (auto ? autoCells(autoLevelText(e, false), autoLevelText(e, true)) : inputs('lt', e.levelText, baseText(e, 'levelText')));

    if (!e.fresh) html += '<span class="k">' + L('Name', '名称') + '</span>' + inputs('name', e.name, baseText(e, 'name'));

    html += '</div>';
    html += '<div class="nnote">' + (auto ? L('Untick the box to write your own text.', '取消勾选即可自己写文字。') : L('Empty boxes use the game\'s text, shown in grey.', '留空则使用游戏原文（灰色显示）。')) + '</div>';

    return html + '</div>';
  }

  function actionsHtml(e) {
    if (e.kind === 'finished') return '';

    const label = isOwn(e) ? L('DELETE THIS REWARD', '删除此奖励') : L('RESET THIS REWARD', '重置此奖励');

    return '<div class="ed-acts"><span class="gbtn dim" data-a="reset">' + label + '</span></div>';
  }

  function footer() {
    if (!st.ctx || typeof st.ctx.footer !== 'function') return;

    try {
      if (st.dirty) st.ctx.footer('<em>' + L('Unsaved changes', '有未保存的修改') + '</em> &middot; ' + L('they apply at the next game start', '下次启动游戏时生效'), true);
      else st.ctx.footer('<b>' + FILE_NAME + '</b> &middot; ' + L('saved', '已保存'), false);
    } catch (err) {}
  }

  // ===== The effect picker =====

  function openPicker(mode, gi) {
    const e = entryOf(st.sel);
    const win = winEl();

    if (!e || !win) return;

    closeOverlays();

    const current = mode === 'replace' && e.gives[gi] ? e.gives[gi].fx.id : '';

    st.picker = { mode, gi, q: '', src: 'all', cat: 'all', selId: current };
    st.pickerEl = h('<div class="nlp-fxp" data-own=""></div>');
    st.pickerEl.addEventListener('click', onClick);
    st.pickerEl.addEventListener('dblclick', onPickerDouble);
    st.pickerEl.addEventListener('input', onInput);
    st.pickerEl.addEventListener('keydown', onPickerKey);
    win.appendChild(st.pickerEl);
    renderPicker(true);
  }

  function pickerHead(e) {
    const p = st.picker;
    const old = p.mode === 'replace' && e.gives[p.gi] ? e.gives[p.gi].fx : null;
    const sub = (old ? L('Replacing', '替换') + ' ' + esc(fxName(old, st.lang === 'zh')) + ' &middot; ' : '') + esc(nameOf(e, st.lang));
    const words = p.q.toLowerCase().split(/\s+/).filter(Boolean);
    const byQuery = cat.list.filter(f => matchesQuery(f, words) && (p.cat === 'all' || f.cat === p.cat));
    const src = (v, en, zh) => '<span data-a="src" data-v="' + v + '"' + (p.src === v ? ' class="is-on"' : '') + '>' + L(en, zh) + '<b>' + byQuery.filter(f => v === 'all' || f.src === v).length + '</b></span>';

    return '<div class="fxp-hd">' +
      '<div class="t">' + L('WHAT IT GIVES', '选择效果') + '<small>' + sub + '</small></div>' +
      '<label class="fxp-q"><span>&#x2315;</span><input data-a="q" value="' + esc(p.q) + '" placeholder="' + esc(L('Search by name, what it does, or a reward that uses it...', '按名称、效果或使用它的奖励搜索...')) + '"></label>' +
      '<div class="fxp-src">' + src('all', 'ALL', '全部') + src('game', 'GAME', '游戏') + src('nlp', 'NEW LOOP PLUS', '新轮回+') + '</div>' +
      '<span class="fxp-x" data-a="close" title="' + L('Close', '关闭') + '">&#x2715;</span>' +
      '</div>';
  }

  function detailHtml(e, shown) {
    const f = shown.find(x => x.id === st.picker.selId);

    if (!f) return '<div class="fxd"></div>';

    const count = e.levels.length;
    const shownParams = f.params.filter(p => p.type !== 'hide');
    const users = f.finishes.concat(f.usedBy);
    const notes = [];

    if (f.modes === 'endless') notes.push(L('The game only applies this in Endless mode.', '游戏只在无尽模式中生效。'));

    if (f.nlp) notes.push(L('New Loop Plus runs this effect itself.', '由新轮回+自身实现。'));
    else if (f.src === 'nlp') notes.push(L('Written by New Loop Plus to finish a reward the game left empty.', '由新轮回+编写，用于补完游戏留空的奖励。'));

    if (f.raw) notes.push(L(RAW_NOTE[0], RAW_NOTE[1]));

    if (!numsOf(f).length) notes.push(L('This effect has no numbers, so a reward with only this effect is the same at every level.', '此效果没有数值，所以只有此效果的奖励每级都一样。'));

    let html = '<div class="fxd"><div class="fxd-top"><img src="' + esc(fxIcon(f)) + '" alt=""><div>' +
      '<div class="fxd-n">' + esc(fxName(f, st.lang === 'zh')) + '</div>' +
      '<div class="fxd-s">' + srcTag(f) + (f.nlp ? [NLP_PREFIX + f.nlp] : f.scripts).map(s => '<code>' + esc(s) + '</code>').join('') + '</div></div></div>' +
      '<div class="fxd-w">' + esc(L(f.what[0], f.what[1])) + '</div>';

    if (shownParams.length) html += '<h4>' + L('NUMBERS IT TAKES', '数值') + '</h4><table>' + shownParams.map(p => paramRow(p, count)).join('') + '</table>';

    html += '<h4>' + (st.lang === 'zh' ? '显示效果：' + count + '级' : 'HOW IT READS AT ' + count + (count === 1 ? ' LEVEL' : ' LEVELS')) + '</h4>' + readsHtml(e, f, count);
    html += gameCardsHtml(f);

    if (users.length) html += '<h4>' + L('REWARDS THAT USE IT', '使用它的奖励') + '</h4><div class="ub">' + users.map(id => '<span><img src="' + esc(st.byId[id] ? st.byId[id].icon : '') + '" alt="">' + esc(talentName(id)) + '</span>').join('') + '</div>';

    if (notes.length) html += '<h4>' + L('NOTES', '提示') + '</h4>' + notes.map(n => '<div class="fn">' + n + '</div>').join('');

    return html + chooseButton(e, f) + '</div>';
  }

  // ===== The icon picker =====

  function openIcons(anchor) {
    const win = winEl();

    if (!win || !cat.icons.length) return;

    closeIcons();
    st.iconsEl = h('<div class="nlp-icons" data-own="">' + iconsHtml() + '</div>');
    st.iconsEl.addEventListener('click', onClick);
    win.appendChild(st.iconsEl);

    const w = win.getBoundingClientRect();
    const a = anchor.getBoundingClientRect();
    const scale = win.offsetWidth ? w.width / win.offsetWidth : 1;
    const top = Math.max(10, Math.min((a.top - w.top) / scale, win.offsetHeight - st.iconsEl.offsetHeight - 10));
    const left = Math.max(10, Math.min((a.right - w.left) / scale + 10, win.offsetWidth - st.iconsEl.offsetWidth - 10));

    st.iconsEl.style.left = left + 'px';
    st.iconsEl.style.top = top + 'px';
    document.addEventListener('mousedown', onDocDown, true);
  }

  // ===== Toast =====

  function toast(html) {
    const win = winEl();

    closeToast();

    if (!html || !win) return;

    st.toastEl = h('<div class="nlp-toast" data-own="">' + html + '</div>');
    win.appendChild(st.toastEl);
    st.toastTimer = setTimeout(closeToast, TOAST_MS);
  }

  // ===== Edits from the editor =====

  function editText(e, key, value) {
    const parts = key.split(':');
    const field = parts[0];
    const lang = parts[1] === 'zh' ? 'zh' : 'en';

    if (field === 'name') e.name = setText(e.name, lang, value);
    else if (field === 'lt') e.levelText = setText(e.levelText, lang, value);
    else if (field === 'desc' && hasLevelDescs(e)) {
      const l = e.levels[Math.min(st.lv, e.levels.length - 1)];

      l.desc = setText(l.desc, lang, value);
    } else if (field === 'desc') e.description = setText(e.description, lang, value);

    markChanged(e);
  }

  // Unticking keeps what the numbers wrote, so the player starts from that text instead of the game's.
  function setAuto(e, isOn) {
    if (!isOn && isAutoOn(e)) {
      e.levelText = { en: autoLevelText(e, false), zh: autoLevelText(e, true) };
      e.levels.forEach((l, i) => { l.desc = { en: autoDesc(e, i, false), zh: autoDesc(e, i, true) }; });
    }

    e.autoText = isOn;
    e.autoManual = !isOn;
    markChanged(e);
  }

  // A stepper key is "c:<level>" for a cost or "g:<give>:<param>:<level>" for an effect's number.
  function stepTarget(e, key) {
    const parts = key.split(':');

    if (parts[0] === 'c') return { isCost: true, i: Number(parts[1]), step: 1 };

    const g = e.gives[Number(parts[1])];
    const p = g ? numsOf(g.fx).find(x => x.key === parts[2]) : null;

    return p ? { isCost: false, g, p, i: Number(parts[3]), step: p.step } : null;
  }

  function readValue(e, t) {
    return t.isCost ? e.levels[t.i].cost : t.g.per[t.p.key][t.i];
  }

  function writeValue(e, t, value) {
    if (t.isCost) {
      e.levels[t.i].cost = Math.max(0, Math.round(value));
      markChanged(e);

      return;
    }

    if (e.givesLocked) return;

    t.g.per[t.p.key][t.i] = Math.max(t.p.min, Math.round(value * 10000) / 10000);

    if (canEditGives(e)) e.givesTouched = true;

    markChanged(e);
  }

  function stepValue(e, key, dir) {
    const t = e ? stepTarget(e, key) : null;

    if (!t || !(t.isCost ? e.levels[t.i] : t.g.per[t.p.key].length > t.i)) return;

    writeValue(e, t, readValue(e, t) + dir * t.step);
    render();
  }

  // ===== Events =====

  // After a reward is made, the search is cleared so the list shows it, and the list scrolls to it.
  function showMade(made) {
    st.q = '';

    select(made.id);
    render();
    showSelectedRow();
  }

  function onClick(ev) {
    const t = ev.target.closest('[data-a]');

    if (!t || (t.getAttribute('data-a') === 'sel' && ev.target.closest('.setting-toggle'))) return;

    const a = t.getAttribute('data-a');
    const v = t.getAttribute('data-v');
    const k = t.getAttribute('data-k');
    const c = Number(t.getAttribute('data-c'));
    const e = entryOf(st.sel);

    switch (a) {
      case 'view': st.view = v === 'all' ? 'all' : 'file'; keepSelectionInView(); break;
      case 'new': {
        const made = newReward();

        if (made) { showMade(made); return; }

        toast(esc(L('There is no free reward number left.', '没有可用的奖励编号了。')));

        break;
      }
      case 'rqx': clearSearch(); return;
      case 'save': save(); return;
      case 'discard': closeOverlays(); discard(); return;
      case 'sel': select(Number(v)); clampLevel(); break;
      case 'lv': st.lv = Number(v); clampLevel(); break;
      case 'addlv': if (e && e.kind !== 'finished' && e.levels.length < MAX_LEVELS) { addLevel(e); st.lv = e.levels.length - 1; } break;
      case 'rmlv': if (e && e.kind !== 'finished') { removeLevel(e); clampLevel(); } break;
      case 'pvl': st.pvLang = v === 'zh' ? 'zh' : 'en'; st.pvChosen = true; break;
      case 'fold': st.textOpen = !st.textOpen; break;
      case 'icon': if (st.iconsEl) closeIcons(); else openIcons(t); return;
      case 'icg': showIconGroup(v || REWARD_ICONS); return;
      case 'ic': {
        const ic = cat.icons[Number(v)];

        if (e && ic) {
          e.icon = ic.icon;
          e.unityIcon = ic.unityIcon;
          markChanged(e);
        }

        closeIcons();
        break;
      }
      case 'col': if (e && e.fresh) { e.column = Number(v) === 1 ? 1 : 2; e.copyFrom = freshCopyFrom(e.column); markChanged(e); } break;
      case 'add': if (e && canEditGives(e) && e.gives.length < MAX_GIVES) openPicker('add', -1); return;
      case 'chg': if (e && canEditGives(e)) openPicker('replace', c); return;
      case 'rm': if (e && canEditGives(e) && (e.fresh || e.gives.length > 1)) { e.gives.splice(c, 1); givesChanged(e); } break;
      case 'reset': case 'delete': if (e) { resetOrDelete(e); afterRemove(); } break;
      case 'edit': adopt(st.sel); break;
      case 'copy': {
        const made = copyInto(st.sel);

        if (made) { showMade(made); return; }

        toast(esc(L('There is no free reward number left.', '没有可用的奖励编号了。')));

        break;
      }
      case 'dec': case 'inc': stepValue(e, k, a === 'inc' ? 1 : -1); return;
      case 'card': st.picker.selId = v; renderPicker(false); return;
      case 'src': st.picker.src = v; renderPicker(true); return;
      case 'cat': st.picker.cat = v; renderPicker(false); return;
      case 'close': closePicker(); return;
      case 'choose': choose(); return;
      default: return;
    }

    render();
  }

  function onChange(ev) {
    const t = ev.target;
    const a = t.getAttribute('data-a');
    const e = entryOf(st.sel);

    if (a === 'tog') {
      const target = entryOf(Number(t.getAttribute('data-v')));

      if (target) {
        target.enabled = t.checked;
        st.dirty = true;
      }
    } else if (a === 'pk' && e) {
      const g = e.gives[Number(t.getAttribute('data-c'))];
      let value;

      try { value = JSON.parse(t.value); } catch (err) { value = t.value; }

      if (g) setPick(e, g, t.getAttribute('data-k'), value);
    } else if (a === 'val' && e) {
      const target = stepTarget(e, t.getAttribute('data-k'));
      const value = parseFloat(t.value);

      if (target && isFinite(value)) writeValue(e, target, value);
    } else if (a === 'auto' && e) setAuto(e, t.checked);
    else return;

    renderAfterChange();
  }

  // ===== Wiring =====

  function load() {
    if (st.asked) return;

    st.asked = true;

    if (window.vuplex) send('talents|load');
    else window.addEventListener('vuplexready', () => send('talents|load'), { once: true });
  }

  function showTab(el, ctx) {
    injectStyle();
    st.ctx = ctx || null;
    st.lang = ctx && ctx.lang === 'zh' ? 'zh' : 'en';

    if (!st.pvChosen) st.pvLang = st.lang;

    if (!st.root) {
      st.root = document.createElement('div');
      st.root.className = 'nlp';
      st.root.addEventListener('click', onClick);
      st.root.addEventListener('change', onChange);
      st.root.addEventListener('input', onInput);
      st.root.addEventListener('keydown', onSearchKey);
    }

    if (st.root.parentNode !== el) el.appendChild(st.root);

    load();
    render();
    watchHeight();
    watchPresses();
  }

  function data(d) {
    if (!isObj(d)) return;

    st.data = d;
    st.game = isArr(d.game) ? d.game.filter(t => isObj(t) && isNum(t.id) && isArr(t.levels)) : [];
    st.byId = {};
    st.game.forEach(t => { st.byId[t.id] = t; });
    st.scripts = new Set(isArr(d.scripts) ? d.scripts : []);
    st.icons = isArr(d.icons) ? d.icons.filter(i => isObj(i) && typeof i.icon === 'string' && i.icon).map(i => ({
      icon: i.icon,
      unityIcon: typeof i.unityIcon === 'string' ? i.unityIcon : '',
      group: typeof i.group === 'string' && i.group ? i.group : REWARD_ICONS
    })) : [];
    st.file = isObj(d.file) ? d.file : null;
    st.problems = isArr(d.problems) ? d.problems.map(String) : [];
    st.pending = null;
    closeOverlays();
    buildCatalogue();
    buildEntries(st.sel);
    render();
  }

  Object.assign(T, {
    CSS, PREFIX, SCOPES, STYLE_ID, actionsHtml, allIds, buildEntries, canEditGives, compareIds, detailHtml, editText,
    effectValues, firstTpl, footer, ghostOf, givesHtml, isAvailable, levelsHtml, lookHtml, makeGive, pickerHead,
    previewHtml, render, rowsHtml, srcTag, st, textHtml, toast, tplValues, whyHtml, writeGive
  });

  window.__slnlpTalents = { data, saved };

  if (!isArr(window.__modmanagerTabs)) window.__modmanagerTabs = [];

  window.__modmanagerTabs.push({ guid: GUID, id: TAB_ID, title: TITLE, show: showTab, hide: hideTab, escape: escapeTab });

  try {
    if (window.__modmanager && typeof window.__modmanager.tabsChanged === 'function') window.__modmanager.tabsChanged();
  } catch (err) {}
})();
