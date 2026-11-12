// The Talents tab in the Mod Manager: what each loop reward costs, how many levels it has, what it gives, and the text
// the player reads, saved to NewLoopPlus.Talents.json. Runs in Root.html after page.js, and only once: the plugin
// sends both scripts again whenever it installs or shows a toast. Talks to the plugin through
// vuplex.postMessage("slmod|newloopplus|talents|...") and is answered through window.__slnlpTalents.
(function () {
  if (window.__slnlpTalents) return;

  const PREFIX = 'slmod|newloopplus|';
  const GUID = 'com.dev.survivallog.newloopplus';
  const TAB_ID = 'talents';
  const TITLE = { en: 'Talents', zh: '天赋' };
  const FILE_NAME = 'NewLoopPlus.Talents.json';
  const STYLE_ID = 'slnlp-talents-css';
  const CUT = [1006, 2017, 3076, 4004, 4006];
  const FIRST_NEW = 5001;
  const LAST_ID = 9999;
  const MAX_LEVELS = 10;
  const MAX_GIVES = 10;
  const GAME_PREFIX = 'game:';
  const NLP_PREFIX = 'nlp:';
  const REWARD_ICONS = 'rewards';

  // The icon picker's groups are the folders the game keeps its images in; these are the ones worth a friendlier name.
  const ICON_GROUPS = {
    rewards: ['Rewards', '奖励'],
    Food: ['Food', '食物'],
    Material: ['Materials', '材料'],
    Consumable: ['Consumables', '消耗品'],
    Furniture: ['Furniture', '家具'],
    Electrical: ['Electrical', '电器'],
    Structure: ['Structures', '建筑'],
    Vehicle: ['Vehicles', '载具'],
    RobotModule: ['Robot modules', '机器人模块'],
    Literature: ['Books', '书籍'],
    Renovation: ['Renovation', '装修'],
    Camp: ['Camp', '营地'],
    icon: ['Other', '其他']
  };
  const TOAST_MS = 2600;
  const INT_FIELD = /^(IntValue\d|Buff(Satiety|Morale|Stamina|Health|Vitality)Max|ItemEndBuff0\d|BuffEndBuff0\d|PonitEnd|ExposureValue|BuffAction)$/;
  const STAT_MAX = /^Buff(Stamina|Satiety|Morale|Health|Vitality)Max$/;
  const NO_SCRIPT = ['', 'Buff_Empty'];

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

  const VARS = '--hl:#FFC83A;--hl-glow:rgba(255,200,58,0.35);--tm:#f0e8d8;--td:#a89a82;--tmeta:#7a6e5a;--bl:rgba(220,188,120,0.16);--bd2:rgba(220,188,120,0.10);';

  const CSS = [
    'N &{' + VARS + 'position:relative;min-height:677px;padding:16px 26px 26px;color:var(--tm);background:radial-gradient(ellipse at 20% 0%,rgba(120,80,30,0.28),transparent 60%),linear-gradient(180deg,#1f1a15,#16120e)}',
    'N &::before{content:"";position:absolute;top:0;left:0;right:0;height:2px;background:linear-gradient(90deg,transparent 10%,var(--hl) 50%,transparent 90%);opacity:.5}',
    'B input,textarea,select{user-select:text}',
    'N .nlp-top{display:flex;align-items:center;gap:16px;margin-bottom:12px}',
    'N .nlp-title{font-size:1.5rem;font-weight:700;letter-spacing:4px;color:#fff;text-shadow:0 2px 10px rgba(0,0,0,.6);white-space:nowrap}',
    'N .nlp-sub{color:var(--td);font-size:.9rem}',
    'B .sp{flex:1}',
    'N .nlp-chips{display:flex;border:1px solid var(--bl)}',
    'N .nlp-chips span{padding:6px 14px;font-size:.85rem;color:var(--td);cursor:pointer;letter-spacing:1px;white-space:nowrap}',
    'N .nlp-chips span.is-on{background:rgba(255,200,58,0.14);color:var(--hl)}',
    'N .nlp-top .key-btn{white-space:nowrap}',
    'N .nlp-probs{margin:0 0 12px;padding:8px 14px;border:1px solid rgba(255,82,82,0.4);background:rgba(255,82,82,0.08);color:#e9b;font-size:.85rem;line-height:1.5}',
    'N .nlp-bad{margin:0 0 12px;padding:10px 14px;border:1px solid rgba(255,82,82,0.5);background:rgba(255,82,82,0.12);color:#fcc;font-size:.92rem}',
    'N .nlp-wait{padding:60px;text-align:center;color:var(--td)}',
    'N .nlp-main{display:grid;grid-template-columns:450px minmax(0,1fr);gap:22px;align-items:start}',
    'N .nlp-list{height:610px;overflow-y:auto;border:1px solid var(--bd2);background:rgba(0,0,0,0.22)}',
    'B ::-webkit-scrollbar{width:6px}',
    'B ::-webkit-scrollbar-thumb{background:rgba(255,200,58,0.3)}',
    'N .col-h{position:sticky;top:0;z-index:2;padding:9px 16px;text-align:center;color:var(--hl);font-weight:700;letter-spacing:5px;font-size:.95rem;text-shadow:0 0 12px var(--hl-glow);background:#1d1813;border-bottom:1px solid var(--bl)}',
    'N .col-h small{position:absolute;right:12px;top:11px;font-family:var(--mono);font-size:.7rem;color:var(--tmeta);letter-spacing:0;text-shadow:none}',
    'N .ab{position:relative;display:grid;grid-template-columns:52px 1fr auto;gap:14px;align-items:center;padding:8px 16px;min-height:66px;cursor:pointer;border-bottom:1px solid rgba(255,255,255,0.04)}',
    'N .ab::before{content:"";position:absolute;left:0;top:10px;bottom:10px;width:3px}',
    'N .ab:hover{background:linear-gradient(90deg,rgba(255,200,58,0.06),transparent)}',
    'N .ab.is-sel{background:linear-gradient(90deg,rgba(255,200,58,0.13),rgba(255,200,58,0.02) 70%,transparent)}',
    'N .ab.is-sel::before{background:var(--hl);box-shadow:0 0 10px var(--hl-glow)}',
    'N .ab img{width:52px;height:52px;object-fit:contain;filter:drop-shadow(0 2px 5px rgba(0,0,0,.5))}',
    'N .ab .nm{font-weight:700;font-size:1rem;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}',
    'N .ab .mt{display:flex;gap:10px;align-items:center;margin-top:3px;font-size:.8rem;color:var(--tmeta)}',
    'N .ab .lvn{font-family:var(--mono);color:var(--hl)}',
    'N .ab.is-out{opacity:.5}',
    'N .ab.is-out:hover,.ab.is-out.is-sel{opacity:.85}',
    'N .ab.is-off .nm,.ab.is-off img{opacity:.5}',
    'N .ab .setting-toggle{display:block}',
    'B .tg{white-space:nowrap;font-family:var(--mono);font-size:.66rem;letter-spacing:1px;padding:1px 6px;border:1px solid}',
    'B .tg.cut{color:#d88;border-color:rgba(255,82,82,0.4)}',
    'B .tg.fin{color:var(--hl);border-color:rgba(255,200,58,0.45)}',
    'B .tg.src-game{color:#9ab;border-color:rgba(150,180,200,0.35)}',
    'B .tg.new{color:#7dd;border-color:rgba(100,220,220,0.4)}',
    'B .tg.src-nlp{color:var(--hl);border-color:rgba(255,200,58,0.45)}',
    'B .tg.is-chg{color:#c9a0ff;border-color:rgba(180,140,255,0.45)}',
    'B .tg.ex{color:var(--tmeta);border-color:rgba(255,255,255,0.15);border-style:dashed}',
    'N .new-btn{padding:7px 14px;border:1px dashed var(--hl);color:var(--hl);cursor:pointer;letter-spacing:2px;font-size:.85rem;font-weight:700;white-space:nowrap}',
    'N .new-btn:hover{background:rgba(255,200,58,0.12);color:#fff}',
    'N .ed{min-width:0;height:610px;overflow-y:auto;padding-right:8px}',
    'N .pv{display:grid;grid-template-columns:110px 1fr;gap:20px;padding:16px 20px;border:1px solid var(--bl);background:linear-gradient(180deg,rgba(40,32,24,0.95),rgba(24,20,16,0.95));box-shadow:0 8px 30px rgba(0,0,0,.5);position:relative}',
    'N .pv img{width:110px;height:110px;object-fit:contain;filter:drop-shadow(0 0 12px rgba(255,200,58,0.25))}',
    'N .pv-name{font-size:1.45rem;font-weight:700;color:#fff;display:flex;align-items:baseline;gap:14px;padding-right:200px}',
    'N .pv-name .lvb{font-family:var(--mono);font-size:1.1rem;color:var(--hl);text-shadow:0 0 6px var(--hl-glow);white-space:nowrap}',
    'N .pv-lt{color:var(--hl);font-size:.95rem;margin-top:4px}',
    'N .pv-lt b{color:#fff;text-decoration:underline;text-decoration-color:var(--hl);text-underline-offset:3px}',
    'N .pv-desc{color:var(--tm);font-size:1rem;margin-top:10px;line-height:1.5;min-height:48px}',
    'N .pv-desc.is-empty{color:var(--tmeta);font-style:italic}',
    'N .pv-cost{margin-top:8px;font-size:.9rem;color:var(--td)}',
    'N .pv-cost b{color:var(--hl);font-family:var(--mono);font-size:1.05rem}',
    'N .pv-tag{position:absolute;right:12px;top:10px;display:flex;gap:8px;align-items:center;font-size:.75rem;color:var(--tmeta)}',
    'N .pv-tag .mini{display:flex;border:1px solid var(--bl)}',
    'N .pv-tag .mini span{padding:2px 8px;cursor:pointer}',
    'N .pv-tag .mini span.is-on{color:var(--hl);background:rgba(255,200,58,0.12)}',
    'N .sec-h{display:flex;align-items:center;gap:12px;margin:16px 0 8px;color:var(--hl);letter-spacing:3px;font-size:.8rem;font-weight:700}',
    'N .sec-h small{color:var(--tmeta);letter-spacing:0;font-weight:400;font-size:.8rem}',
    'N .look{display:grid;grid-template-columns:96px 1fr;gap:16px;padding:12px 14px;border:1px solid var(--bd2);background:rgba(0,0,0,0.25)}',
    'N .look-ic{position:relative;width:96px;height:96px;border:1px dashed var(--bl);cursor:pointer;display:flex;align-items:center;justify-content:center}',
    'N .look-ic img{width:80px;height:80px;object-fit:contain}',
    'N .look-ic span{position:absolute;bottom:-1px;left:-1px;right:-1px;text-align:center;font-size:.66rem;letter-spacing:1px;background:rgba(0,0,0,0.75);color:var(--hl);padding:2px 0}',
    'N .look-ic:hover{border-color:var(--hl)}',
    'N .look-f{display:grid;grid-template-columns:110px 1fr 1fr;gap:8px 10px;align-items:center}',
    'N .look-f .k,.tx .k{color:var(--td);font-size:.88rem}',
    'N .look-f input,.tx input{width:100%;box-sizing:border-box;background:rgba(0,0,0,0.4);border:1px solid var(--bd2);color:#fff;padding:6px 9px;font:400 .92rem var(--body);outline:none}',
    'N .look-f input:focus,.tx input:focus{border-color:var(--hl)}',
    'N .tx input::placeholder{color:#6d6253}',
    'N .seg2{display:inline-flex;border:1px solid var(--bl)}',
    'N .seg2 span{padding:5px 14px;font-size:.82rem;letter-spacing:2px;color:var(--td);cursor:pointer}',
    'N .seg2 span.is-on{color:#1c1814;background:var(--hl);font-weight:700}',
    'N .gv{border:1px solid var(--bd2);background:rgba(0,0,0,0.22)}',
    'N .gv-row{display:grid;grid-template-columns:44px minmax(0,1fr) auto auto auto;gap:12px;align-items:center;padding:8px 12px;border-bottom:1px solid rgba(255,255,255,0.04)}',
    'N .gv-row img{width:44px;height:44px;object-fit:contain}',
    'N .gv-n{font-weight:700;color:#fff;display:flex;gap:8px;align-items:center}',
    'N .gv-w{color:var(--td);font-size:.84rem;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}',
    'N .gv-picks{display:flex;gap:6px}',
    'N .gv-row a{color:var(--hl);font-size:.8rem;letter-spacing:2px;cursor:pointer}',
    'N .gv-row a:hover{color:#fff}',
    'N .gv-row .x{color:var(--tmeta);cursor:pointer;padding:0 4px}',
    'N .gv-row .x:hover{color:#FF5252}',
    'N .gv-add{padding:8px 12px;color:var(--hl);font-size:.85rem;letter-spacing:2px;cursor:pointer}',
    'N .gv-add:hover{background:rgba(255,200,58,0.08)}',
    'N .gv-empty{padding:18px;text-align:center;color:var(--td)}',
    'N .gv-empty .gbtn{display:inline-block;margin-top:10px}',
    'N select.nsel{background:#1d1813;border:1px solid var(--bl);color:#fff;padding:5px 8px;font:400 .88rem var(--body);outline:none}',
    'N .lvs{display:flex;gap:6px;margin:14px 0 6px;align-items:center}',
    'N .lvs span{min-width:46px;text-align:center;padding:6px 0;font-family:var(--mono);font-size:.85rem;color:var(--td);border:1px solid var(--bd2);cursor:pointer}',
    'N .lvs span.is-on{color:#1c1814;background:var(--hl);border-color:var(--hl);font-weight:700}',
    'N .lvs span.is-add{color:var(--hl);border-style:dashed;padding:6px 10px}',
    'N .lvs .total{margin-left:auto;color:var(--td);font-size:.88rem;border:0;cursor:default;font-family:var(--body)}',
    'N .lvs .total b{color:var(--hl);font-family:var(--mono)}',
    'N .lt{width:100%;border-collapse:collapse}',
    'N .lt th{text-align:left;font-weight:400;font-size:.74rem;letter-spacing:2px;color:var(--tmeta);padding:6px 8px;border-bottom:1px solid var(--bl);vertical-align:bottom}',
    'N .lt th small{display:block;letter-spacing:0;font-size:.7rem;color:#5e5546}',
    'N .lt td{padding:5px 8px;border-bottom:1px solid rgba(255,255,255,0.035)}',
    'N .lt tr.is-on td{background:rgba(255,200,58,0.06)}',
    'N .lt td.l{font-family:var(--mono);color:var(--hl);width:54px;cursor:pointer}',
    'N .lt td.x{width:30px;color:var(--tmeta);cursor:pointer}',
    'N .lt td.x:hover{color:#FF5252}',
    'N .stp{display:inline-flex;align-items:center;border:1px solid var(--bl);background:rgba(0,0,0,0.35)}',
    'N .stp b{width:30px;text-align:center;color:var(--hl);cursor:pointer;font-size:1.1rem;user-select:none;line-height:32px}',
    'N .stp b:hover{background:rgba(255,200,58,0.14)}',
    'N .stp input{width:62px;background:transparent;border:0;color:#fff;text-align:center;font:700 1rem var(--mono);outline:none}',
    'N .stp i{font-style:normal;color:var(--td);font-size:.82rem;padding-right:10px}',
    'N .stp.is-ro b{visibility:hidden}',
    'N .nnote{color:var(--td);font-size:.88rem;margin:10px 0 0;line-height:1.5}',
    'N .nnote b{color:var(--tm)}',
    'N .fold{margin-top:14px;border-top:1px solid var(--bd2);padding-top:10px}',
    'N .fold-t{cursor:pointer;color:var(--hl);letter-spacing:3px;font-size:.8rem}',
    'N .tx{display:grid;grid-template-columns:130px 1fr 1fr;gap:8px 10px;align-items:center;margin-top:10px}',
    'N .tx .auto{color:var(--hl);font-size:.88rem;line-height:1.4}',
    'N .tx .hd2{color:var(--tmeta);font-size:.74rem;letter-spacing:2px}',
    'N .chk{display:flex;align-items:center;gap:8px;margin-top:10px;color:var(--td);font-size:.88rem;cursor:pointer}',
    'N .chk input{accent-color:#FFC83A}',
    'N .ed-acts{display:flex;gap:10px;margin-top:16px;align-items:center}',
    'B .gbtn{padding:8px 16px;border:1px solid var(--hl);color:var(--hl);background:rgba(255,200,58,0.08);cursor:pointer;letter-spacing:2px;font-size:.85rem;font-weight:700}',
    'B .gbtn:hover{background:rgba(255,200,58,0.22);color:#fff}',
    'B .gbtn.dim{border-color:var(--bl);color:var(--td);background:transparent}',
    'B .gbtn.is-dis{opacity:.45;pointer-events:none}',
    'N .add-cta{padding:18px 20px;margin-top:14px;border:1px dashed var(--bl);color:var(--td)}',
    'N .add-cta .gbtn{display:inline-block;margin:10px 10px 0 0}',
    'P &{' + VARS + 'position:absolute;inset:0;z-index:50;background:rgba(0,0,0,0.72);display:flex;align-items:center;justify-content:center;color:var(--tm);animation:slmm-pop .18s both}',
    'P .fxp-card{width:1520px;height:850px;display:grid;grid-template-rows:auto 1fr auto;border:1px solid rgba(255,200,58,0.35);background:linear-gradient(180deg,#221c16,#15110d);box-shadow:0 30px 90px rgba(0,0,0,0.9)}',
    'P .fxp-hd{display:flex;align-items:center;gap:18px;padding:14px 20px;border-bottom:1px solid var(--bl)}',
    'P .fxp-hd .t{font-size:1.25rem;font-weight:700;letter-spacing:4px;color:#fff;white-space:nowrap}',
    'P .fxp-hd .t small{display:block;font-size:.8rem;letter-spacing:1px;color:var(--td);font-weight:400}',
    'P .fxp-q{flex:1;display:flex;align-items:center;gap:8px;border:1px solid var(--bl);background:rgba(0,0,0,0.45);padding:0 12px}',
    'P .fxp-q:focus-within{border-color:var(--hl)}',
    'P .fxp-q input{flex:1;background:transparent;border:0;outline:none;color:#fff;font:400 1.05rem var(--body);padding:9px 0}',
    'P .fxp-q span{color:var(--tmeta)}',
    'P .fxp-src{display:flex;border:1px solid var(--bl)}',
    'P .fxp-src span{padding:7px 13px;font-size:.82rem;color:var(--td);cursor:pointer;letter-spacing:1px;white-space:nowrap}',
    'P .fxp-src span b{font-family:var(--mono);font-weight:400;color:var(--tmeta);margin-left:4px}',
    'P .fxp-src span.is-on{background:rgba(255,200,58,0.14);color:var(--hl)}',
    'P .fxp-x{font-size:1.4rem;color:var(--td);cursor:pointer;padding:0 4px}',
    'P .fxp-x:hover{color:#fff}',
    'P .fxp-bd{display:grid;grid-template-columns:250px minmax(0,1fr) 480px;min-height:0}',
    'P .fxp-rail{overflow-y:auto;border-right:1px solid var(--bd2);padding:8px 0;background:rgba(0,0,0,0.2)}',
    'P .fxp-rail .rh{padding:12px 18px 6px;font-size:.7rem;letter-spacing:2px;color:var(--tmeta)}',
    'P .fxp-rail .ri{display:flex;justify-content:space-between;padding:7px 18px;cursor:pointer;font-size:.92rem;color:var(--td);border-left:3px solid transparent}',
    'P .fxp-rail .ri:hover{background:rgba(255,200,58,0.05);color:var(--tm)}',
    'P .fxp-rail .ri.is-on{border-left-color:var(--hl);background:rgba(255,200,58,0.1);color:#fff}',
    'P .fxp-rail .ri.is-zero{opacity:.35}',
    'P .fxp-rail .ri b{font-family:var(--mono);font-weight:400;color:var(--tmeta);font-size:.8rem}',
    'P .fxp-list{overflow-y:auto;padding:4px 14px 14px}',
    'P .fxp-list .ch{position:sticky;top:0;z-index:1;padding:10px 4px 6px;font-size:.72rem;letter-spacing:3px;color:var(--hl);background:#1d1813;border-bottom:1px solid var(--bd2)}',
    'P .fxc{display:grid;grid-template-columns:46px minmax(0,1fr);gap:12px;padding:9px 10px;cursor:pointer;border:1px solid transparent;border-bottom-color:rgba(255,255,255,0.04)}',
    'P .fxc:hover{background:rgba(255,200,58,0.05)}',
    'P .fxc.is-sel{border-color:rgba(255,200,58,0.55);background:rgba(255,200,58,0.09)}',
    'P .fxc.is-added{opacity:.55}',
    'P .fxc img{width:46px;height:46px;object-fit:contain}',
    'P .fxc-n{font-weight:700;color:#fff;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}',
    'P .fxc-w{font-size:.84rem;color:var(--td);white-space:nowrap;overflow:hidden;text-overflow:ellipsis}',
    'P .fxc-m{display:flex;gap:8px;align-items:center;margin-top:3px;font-size:.74rem;color:var(--tmeta);white-space:nowrap;overflow:hidden}',
    'P .fxp-none{padding:40px;text-align:center;color:var(--td)}',
    'P .fxd{overflow-y:auto;border-left:1px solid var(--bd2);padding:18px 22px;background:rgba(0,0,0,0.25)}',
    'P .fxd-top{display:grid;grid-template-columns:76px 1fr;gap:14px;align-items:center}',
    'P .fxd-top img{width:76px;height:76px;object-fit:contain;filter:drop-shadow(0 0 10px rgba(255,200,58,0.2))}',
    'P .fxd-n{font-size:1.3rem;font-weight:700;color:#fff}',
    'P .fxd-s{display:flex;gap:8px;align-items:center;margin-top:4px;flex-wrap:wrap}',
    'P .fxd-s code{font:.76rem var(--mono);color:var(--tmeta)}',
    'P .fxd-w{margin-top:12px;line-height:1.5}',
    'P .fxd h4{margin:18px 0 6px;font-size:.72rem;letter-spacing:3px;color:var(--hl);font-weight:700}',
    'P .fxd table{width:100%;border-collapse:collapse;font-size:.88rem;margin:0}',
    'P .fxd td{padding:5px 0;border-bottom:1px solid rgba(255,255,255,0.04);vertical-align:top}',
    'P .fxd td:first-child{color:var(--td);width:46%}',
    'P .fxd td:last-child{font-family:var(--mono);color:var(--tm)}',
    'P .fxd-rd{padding:10px 12px;border:1px solid var(--bl);background:rgba(40,32,24,0.8)}',
    'P .fxd-rd .l{color:var(--hl);font-size:.9rem}',
    'P .fxd-rd .d{margin-top:6px;font-size:.92rem;line-height:1.5}',
    'P .fxd .ub{display:flex;flex-wrap:wrap;gap:6px}',
    'P .fxd .ub span{display:inline-flex;gap:6px;align-items:center;padding:2px 8px 2px 2px;border:1px solid var(--bd2);font-size:.8rem;color:var(--td)}',
    'P .fxd .ub img{width:22px;height:22px;object-fit:contain}',
    'P .fxd .fn{margin-top:6px;font-size:.84rem;color:var(--td);line-height:1.5}',
    'P .fxd .gbtn{display:inline-block;margin-top:18px}',
    'P .fxp-ft{display:flex;align-items:center;gap:14px;padding:12px 20px;border-top:1px solid var(--bl);color:var(--tmeta);font-size:.84rem}',
    'I &{' + VARS + 'position:absolute;z-index:60;width:720px;padding:10px;background:#1a1611;border:1px solid rgba(255,200,58,0.4);box-shadow:0 20px 60px rgba(0,0,0,0.85)}',
    'I .icg{display:flex;flex-wrap:wrap;gap:4px;margin-bottom:8px}',
    'I .icg span{padding:3px 10px;font-size:.8rem;color:var(--td);border:1px solid var(--bl);cursor:pointer;white-space:nowrap}',
    'I .icg span.is-on{background:rgba(255,200,58,0.14);color:var(--hl);border-color:var(--hl)}',
    'I .icl{display:grid;grid-template-columns:repeat(10,1fr);gap:4px;max-height:400px;overflow-y:auto}',
    'I img{width:58px;height:58px;object-fit:contain;padding:4px;border:1px solid transparent;cursor:pointer}',
    'I img:hover{border-color:var(--hl);background:rgba(255,200,58,0.1)}',
    'T &{position:absolute;left:50%;bottom:74px;transform:translateX(-50%);background:#0d1716;border:1px solid var(--teal);color:#cfe;padding:10px 18px;font-size:.95rem;z-index:40;box-shadow:0 10px 30px rgba(0,0,0,0.7);animation:slmm-pin .25s both}',
    'T b{color:#fff}'
  ];

  function scopedCss() {
    return CSS.map(line => {
      const scopes = SCOPES[line.charAt(0)];
      const brace = line.indexOf('{');
      const selectors = line.slice(2, brace).split(',').map(s => s.trim());
      const scoped = [];

      for (const scope of scopes) {
        for (const s of selectors) scoped.push(s.charAt(0) === '&' ? scope + s.slice(1) : scope + ' ' + s);
      }

      return scoped.join(',') + line.slice(brace);
    }).join('\n');
  }

  function injectStyle() {
    if (document.getElementById(STYLE_ID)) return;

    const style = document.createElement('style');

    style.id = STYLE_ID;
    style.textContent = scopedCss();
    document.head.appendChild(style);
  }

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
    problems: []
  };

  // ===== Small helpers =====

  const isObj = v => v !== null && typeof v === 'object' && !Array.isArray(v);
  const isArr = Array.isArray;
  const isNum = v => typeof v === 'number' && isFinite(v);
  const clone = v => v === undefined ? undefined : JSON.parse(JSON.stringify(v));
  const round2 = v => Math.round(v * 100) / 100;
  const fmt = v => String(round2(isNum(v) ? v : 0));
  const range = n => Array.from({ length: Math.max(0, n) }, (_, i) => i);
  const L = (en, zh) => st.lang === 'zh' ? zh : en;
  const ESCAPES = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' };
  const esc = s => String(s == null ? '' : s).replace(/[&<>"']/g, c => ESCAPES[c]);
  const isEmptyVal = v => v == null || v === 0 || v === '' || v === false || (isArr(v) && v.length === 0);
  const zeroOf = v => isArr(v) ? [] : typeof v === 'string' ? '' : typeof v === 'boolean' ? false : 0;
  const allKeys = (a, b) => Array.from(new Set(Object.keys(a).concat(Object.keys(b))));

  // The file and the game leave zeros, empty text and false out, so those all count as "not set".
  function sameVal(a, b) {
    if (isEmptyVal(a) && isEmptyVal(b)) return true;

    if (isNum(a) && isNum(b)) return Math.abs(a - b) < 1e-6;

    return JSON.stringify(a) === JSON.stringify(b);
  }

  function h(html) {
    const t = document.createElement('template');

    t.innerHTML = html.trim();

    return t.content.firstElementChild;
  }

  // Text in the file is "plain" or { en, zh }; inside the tab it is always { en, zh }, or null when there is none.
  function normText(v) {
    if (typeof v === 'string') return v ? { en: v, zh: v } : null;

    if (!isObj(v)) return null;

    const en = typeof v.en === 'string' ? v.en : '';
    const zh = typeof v.zh === 'string' ? v.zh : '';

    return en || zh ? { en, zh } : null;
  }

  const textIn = (t, lang) => t ? (t[lang] || t.en || t.zh || '') : '';
  const shown = (own, fallback, lang) => textIn(own, lang) || textIn(fallback, lang);

  // ===== What a reward can give =====

  const CATS = [
    ['prep', 'Before the disaster', '灾前准备'],
    ['body', 'Body and needs', '身体与需求'],
    ['morale', 'Morale', '心态'],
    ['items', 'Items and supplies', '物资'],
    ['skills', 'Skills', '熟练度'],
    ['speed', 'Speed', '速度'],
    ['explore', 'Exploring', '探索'],
    ['home', 'Home and power', '居所与电力'],
    ['farm', 'Farming', '种植'],
    ['defense', 'Traps and defense', '陷阱与防御'],
    ['money', 'Money and trade', '金钱与交易'],
    ['loop', 'Loop', '轮回'],
    ['raw', 'Other game scripts', '其他游戏脚本']
  ].map(c => ({ id: c[0], en: c[1], zh: c[2] }));

  // Item groups the game's airdrop rewards use, named after those rewards.
  const GROUP_TALENTS = { 201: 3066, 202: 3067, 203: 3068, 204: 3069, 205: 3070, 206: 3071, 207: 3072, 208: 3073, 228: 3074, 222: 3077, 223: 3078, 224: 3079, 227: 3080 };
  const SKILL_TALENTS = [['Production', 4020], ['Plant', 4021], ['Cooking', 4022], ['Trap', 4023], ['Explore', 4024], ['Defense', 4025]];
  const XP_SKILLS = [['Production', 'Production'], ['Cook', 'Cooking'], ['Plant', 'Plant'], ['Trap', 'Trap'], ['Explore', 'Explore'], ['Defense', 'Defense']];
  const WORK = [['Production', 'Crafting', '制造'], ['Cook', 'Cooking', '烹饪'], ['Interact', 'Searching and using things', '搜索与交互'], ['Chore', 'Chores', '杂务']];
  const STATS = [['Stamina', 'Stamina', '精力'], ['Satiety', 'Satiety', '饱腹'], ['Morale', 'Morale', '心态'], ['Health', 'Health', '健康'], ['Vitality', 'Life', '生命']].map(s => ({ v: s[0], en: s[1], zh: s[2] }));
  const ROBOTS = [['HK', 'HK-1 sorting robot', 'HK-1 整理机器人'], ['TB', 'GR-3 farming robot', 'GR-3 种植机器人'], ['WX', 'FX-5 repair robot', 'FX-5 维修机器人']];
  const robotScript = v => 'EndlessRobotGrant_' + v;

  // "an HK-1 sorting robot" but "a GR-3 farming robot": the article follows how the letter is said.
  function robotName(v, zh, withArticle) {
    const r = ROBOTS.find(x => x[0] === v) || [v, v, v];
    const name = zh ? r[2] : r[1];

    if (!withArticle) return name;

    return (/^[AEFHILMNORSX]/.test(name) ? 'an ' : 'a ') + name;
  }

  // Filled from the game's reward names once the data arrives.
  const named = { groups: [], skills: [], xp: [], work: [], robots: [] };

  function nameOptions() {
    named.groups = Object.keys(GROUP_TALENTS).map(Number).filter(g => st.byId[GROUP_TALENTS[g]]).map(g => {
      const t = st.byId[GROUP_TALENTS[g]];

      return { v: g, en: t.name.en.replace(/^Airdrop - /, ''), zh: t.name.zh.split('\u00b7').pop() };
    });

    const skillLabel = (name, tid) => {
      const t = st.byId[tid];

      return t ? { en: t.name.en.replace(/ Memory$/, '') || name, zh: t.name.zh.replace(/记忆$/, '') || name } : { en: name, zh: name };
    };

    named.skills = SKILL_TALENTS.map(s => Object.assign({ v: s[0] }, skillLabel(s[0], s[1])));
    named.xp = XP_SKILLS.map(s => {
      const from = SKILL_TALENTS.find(k => k[0] === s[1]);

      return Object.assign({ v: s[0], script: 'AE_' + s[0] + 'ExpMultiplier' }, skillLabel(s[1], from ? from[1] : 0));
    });
    named.work = WORK.map(w => ({ v: w[0], en: w[1], zh: w[2], script: 'AE_' + w[0] + 'SpeedMultiplier' }));
    named.robots = ROBOTS.map(r => ({ v: r[0], en: r[1], zh: r[2], script: robotScript(r[0]) }));
  }

  // A number that can change per level. scale and add turn the file's raw value into what the player reads (0.1 -> 10%).
  function num(key, en, zh, unit, def, o) {
    return Object.assign({ type: 'num', key, en, zh, unit, def, step: 1, scale: 1, add: 0, min: 0 }, o);
  }

  const pct = (key, en, zh, def, o) => num(key, en, zh, ['%', '%'], def, Object.assign({ step: 5, scale: 100 }, o));

  // One value for the whole reward. A virtual pick is not a field: it chooses the script (a skill) or the field (a stat).
  function pick(key, en, zh, options, def, o) {
    return Object.assign({ type: 'pick', key, en, zh, options, def, virtual: false }, o);
  }

  // A field the effect needs at a fixed value; an effect row with another value is some other effect.
  const hide = (key, def) => ({ type: 'hide', key, def });

  const optionsOf = p => p.options();
  const isOptionOn = o => !o.script || st.scripts.has(o.script);
  const availableOptions = p => optionsOf(p).filter(isOptionOn);
  const groupUnknown = (v, zh) => (zh ? '物品组' : 'Group ') + v;

  function opt(p, v, zh) {
    const o = optionsOf(p).find(x => x.v === v);

    if (o) return zh ? o.zh : o.en;

    return p.unknown ? p.unknown(v, zh) : String(v == null ? '' : v);
  }

  const itemCount = (n, zh) => fmt(n) + (zh ? '件' : n === 1 ? ' item' : ' items');
  const plural = (n, word) => fmt(n) + ' ' + word + (n === 1 ? '' : 's');
  const numericBuffKeys = v => Object.keys(v).filter(k => /^Buff/.test(k) && isNum(v[k]) && v[k] !== 0);

  const FX = [];

  function fx(o) {
    FX.push(Object.assign({ usedBy: [], finishes: [], params: [], modes: 'both', pickable: true, raw: false }, o, { scripts: o.scripts || [o.script] }));
  }

  // ---- The game's effect scripts, with names and units a player can read. ----

  fx({ id: 'game:countdown', src: 'game', cat: 'prep', script: 'PreDisaster_AddCountDownTime',
    en: 'More time before the disaster', zh: '灾难前更多时间', what: ['Adds hours to the countdown before the disaster hits.', '增加灾难来临前的倒计时。'],
    params: [num('IntValue0', 'Extra time', '额外时间', ['h', '小时'], [1, 2, 3, 4, 7], { scale: 1 / 3600 })],
    line: (v, zh) => '+' + fmt(v.IntValue0) + (zh ? '小时' : 'h'),
    desc: (v, zh) => zh ? '提升灾难前醒来时间' + fmt(v.IntValue0) + '小时' : 'Gives you ' + fmt(v.IntValue0) + ' hours of extra time before the disaster hits.' });

  fx({ id: 'game:gold', src: 'game', cat: 'prep', script: 'PreDisaster_AddGold',
    en: 'Starting money', zh: '初始资金', what: ['Extra money before the disaster.', '灾前额外资金。'],
    params: [num('IntValue0', 'Extra money', '额外资金', ['$', '$'], [100, 200, 300, 400, 600], { step: 50 })],
    line: v => '+$' + fmt(v.IntValue0),
    desc: (v, zh) => zh ? '提升灾难前资金额度' + fmt(v.IntValue0) + '$' : 'Start with an extra $' + fmt(v.IntValue0) + ' before the disaster hits.' });

  fx({ id: 'game:weight', src: 'game', cat: 'prep', script: 'PreDisaster_AddWeight',
    en: 'Carry more when shopping', zh: '购物负重', what: ['More carry weight on pre-disaster shopping trips.', '提升灾前购物时的负重。'],
    params: [num('IntValue0', 'Extra weight', '额外负重', ['kg', 'KG'], [0.5, 1, 1.5, 2, 3], { step: 0.5, scale: 1 / 1200 })],
    line: v => '+' + fmt(v.IntValue0) + 'kg',
    desc: (v, zh) => zh ? '提升购物负重' + fmt(v.IntValue0) + 'KG' : 'Carry ' + fmt(v.IntValue0) + 'kg more weight.' });

  fx({ id: 'game:short-life-discount', src: 'game', cat: 'prep', script: 'PreDisaster_AddDiscount',
    en: 'Discount on short-life supplies', zh: '临期物资折扣', what: ['Cheaper supplies that spoil soon.', '购买即将过期的物资时打折。'],
    params: [pct('FloatValue1', 'Discount', '折扣', [10, 15, 20, 25, 30], { scale: -100, add: 100 }), num('FloatValue0', 'Shelf life under', '保质期小于', ['days', '天'], [5, 5, 5, 5, 5])],
    line: v => '-' + fmt(v.FloatValue1) + '%',
    desc: (v, zh) => zh ? '购买保质期小于' + fmt(v.FloatValue0) + '天的物资时，获得' + fmt(v.FloatValue1) + '%折扣' : 'Get a ' + fmt(v.FloatValue1) + '% discount when purchasing supplies with a shelf life of less than ' + fmt(v.FloatValue0) + ' days.' });

  fx({ id: 'game:bulk-discount', src: 'game', cat: 'prep', script: 'BigVolume_Discount',
    en: 'Discount on big items', zh: '大件折扣', what: ['Cheaper supplies that take a lot of space.', '购买大体积物资时打折。'],
    params: [pct('FloatValue1', 'Discount', '折扣', [5, 10, 15, 20, 25], { scale: -100, add: 100 }), num('FloatValue0', 'Size at least', '体积至少', ['', ''], [6, 6, 6, 6, 6])],
    line: v => '-' + fmt(v.FloatValue1) + '%',
    desc: (v, zh) => zh ? '在购买大于等于' + fmt(v.FloatValue0) + '体积物资时，获得' + fmt(v.FloatValue1) + '%的价格折扣' : 'Get a ' + fmt(v.FloatValue1) + '% discount on supplies of size ' + fmt(v.FloatValue0) + ' or more.' });

  fx({ id: 'game:drive', src: 'game', cat: 'prep', script: 'VehicleTimeReduce',
    en: 'Drive and travel faster', zh: '载具驾驶', what: ['Unlocks buying a vehicle and cuts travel time.', '解锁购买车辆并减少行路时间。'],
    params: [pct('FloatValue0', 'Less travel time', '行路时间减少', [10, 20, 30, 30, 30])],
    line: v => '-' + fmt(v.FloatValue0) + '%',
    desc: (v, zh) => zh ? '解锁购买车辆的能力，并减少' + fmt(v.FloatValue0) + '%的行路时间' : 'Unlocks buying a vehicle and cuts travel time by ' + fmt(v.FloatValue0) + '%.' });

  fx({ id: 'game:cabinet-food', src: 'game', cat: 'prep', script: 'PreDisaster_AddRandomItem',
    en: 'Food left in the cupboards', zh: '柜中遗留食物', what: ['Random food waiting in your home\'s cupboards.', '居所柜子里遗留随机食物。'],
    params: [hide('IntValue0', 301), num('IntValue1', 'Food items', '食物数量', ['items', '件'], [2, 3, 4, 5, 8])],
    line: (v, zh) => itemCount(v.IntValue1, zh),
    desc: (v, zh) => zh ? '在居所的柜子里，会遗留一些随机食物' + fmt(v.IntValue1) + '件' : 'Your home\'s cupboards hold ' + fmt(v.IntValue1) + ' random food items.' });

  fx({ id: 'game:loan', src: 'game', cat: 'money', script: 'LoanMaxBonus',
    en: 'Higher loan limit', zh: '借款上限', what: ['Borrow more before the disaster (the base limit is 500).', '提高灾前可借款上限（原500）。'],
    params: [num('IntValue0', 'Loan limit', '借款上限', ['', ''], [800, 1200, 1800], { step: 100, add: 500 })],
    line: v => fmt(v.IntValue0),
    desc: (v, zh) => zh ? '可借款上限提高至' + fmt(v.IntValue0) + '（原500）' : 'Loan limit increased to ' + fmt(v.IntValue0) + ' (from 500).' });

  fx({ id: 'game:furniture', src: 'game', cat: 'home', script: 'AE_FurnitureDiscount',
    en: 'Furniture discount', zh: '家具折扣', what: ['Cheaper furniture.', '购买家具打折。'],
    params: [pct('FloatValue0', 'Discount', '折扣', [5, 10, 15])],
    line: v => '-' + fmt(v.FloatValue0) + '%',
    desc: (v, zh) => zh ? '购买家具时享' + fmt(v.FloatValue0) + '%折扣' : fmt(v.FloatValue0) + '% off furniture.' });

  fx({ id: 'game:sleep-stamina', src: 'game', cat: 'body', script: 'PostDisaster_AddEnergyEfficiency',
    en: 'More Stamina from sleep', zh: '睡眠恢复精力', what: ['Sleep restores Stamina faster.', '睡眠时精力恢复更快。'],
    params: [pct('FloatValue0', 'Stamina from sleep', '睡眠精力', [10, 20, 30, 40, 50])],
    line: v => '+' + fmt(v.FloatValue0) + '%',
    desc: (v, zh) => zh ? '睡眠增加精力效率增加' + fmt(v.FloatValue0) + '%' : 'Increases Stamina recovery while sleeping by ' + fmt(v.FloatValue0) + '%.' });

  fx({ id: 'game:medicine', src: 'game', cat: 'body', script: 'PostDisaster_MedicalMaster',
    en: 'Stronger medicine', zh: '医疗增效', what: ['Medical items work better and treat worse illness.', '医疗物品效果提升，可治疗更严重疾病。'],
    params: [pct('FloatValue0', 'Medicine effect', '医疗效果', [10, 20, 30, 40, 50])],
    line: v => '+' + fmt(v.FloatValue0) + '%',
    desc: (v, zh) => zh ? '医疗物品效果提升' + fmt(v.FloatValue0) + '%' : 'Medical items are ' + fmt(v.FloatValue0) + '% more effective.' });

  fx({ id: 'game:eat-satiety', src: 'game', cat: 'body', script: 'Efficient_Nutrition',
    en: 'More Satiety from food', zh: '进食饱腹', what: ['Every meal fills you more.', '进食获得更多饱腹。'],
    params: [pct('FloatValue0', 'Satiety from food', '进食饱腹', [15, 20, 25])],
    line: v => '+' + fmt(v.FloatValue0) + '%',
    desc: (v, zh) => zh ? '进食获得的饱腹度+' + fmt(v.FloatValue0) + '%' : 'Food gives ' + fmt(v.FloatValue0) + '% more Satiety.' });

  fx({ id: 'game:staple-satiety', src: 'game', cat: 'body', script: 'Buff_304310_ComfortFood',
    en: 'More Satiety from staple food', zh: '主食饱腹', what: ['Staple foods fill you more.', '食用主食获得更多饱腹。'],
    params: [pct('FloatValue0', 'Satiety from staples', '主食饱腹', [12, 18, 24], { step: 2 })],
    line: v => '+' + fmt(v.FloatValue0) + '%',
    desc: (v, zh) => zh ? '食用主食类别食品时，获得的饱腹效果增加' + fmt(v.FloatValue0) + '%' : 'Eating Staple Foods grants ' + fmt(v.FloatValue0) + '% more Satiety.' });

  fx({ id: 'game:sleep-satiety', src: 'game', cat: 'body', script: 'Deep_Sleep',
    en: 'Less hunger while sleeping', zh: '睡眠饱腹下降减少', what: ['Satiety drops slower while you sleep.', '睡眠时饱腹下降更慢。'],
    params: [pct('FloatValue0', 'Slower Satiety drop', '饱腹下降减少', [15, 25, 35], { scale: -100 })],
    line: v => '-' + fmt(v.FloatValue0) + '%',
    desc: (v, zh) => zh ? '睡眠时饱腹度下降速度-' + fmt(v.FloatValue0) + '%' : 'Satiety drops ' + fmt(v.FloatValue0) + '% slower while sleeping.' });

  // The game heals Life through BuffVitalityStart; a reward made on BuffHealthStart keeps that field.
  fx({ id: 'game:heal-now', src: 'game', cat: 'body', script: 'Buff_Default_StartNotify',
    en: 'Restore Life at once', zh: '立即恢复生命', what: ['Restores Life the moment the reward is taken.', '获得奖励时立即恢复生命。'],
    params: [num('heal', 'Life restored', '恢复生命', ['', '点'], [45, 60, 80], { step: 5, dyn: g => g.field })],
    init: (g, tpl) => { g.field = !isEmptyVal(tpl.BuffHealthStart) && isEmptyVal(tpl.BuffVitalityStart) ? 'BuffHealthStart' : 'BuffVitalityStart'; },
    test: v => numericBuffKeys(v).every(k => k === 'BuffVitalityStart' || k === 'BuffHealthStart'),
    zeroOthers: (k, g) => /^Buff/.test(k) && k !== g.field,
    line: v => '+' + fmt(v.heal),
    desc: (v, zh) => zh ? '立即恢复' + fmt(v.heal) + '点生命值' : 'Restore ' + fmt(v.heal) + ' Life at once.' });

  fx({ id: 'game:stat-max', src: 'game', cat: 'body', script: 'Buff_Default',
    en: 'Raise a stat\'s maximum', zh: '提高属性上限', what: ['Raises the cap of Stamina, Satiety, Morale, Health or Life.', '提高精力、饱腹、心态、健康或生命的上限。'],
    params: [
      pick('stat', 'Stat', '属性', () => STATS, 'Stamina', { virtual: true, from: v => { const k = numericBuffKeys(v).find(x => STAT_MAX.test(x)); return k ? k.slice(4, -3) : 'Stamina'; } }),
      num('BuffMax', 'Extra maximum', '额外上限', ['', ''], [5, 10, 15, 20, 30], { step: 5, dyn: g => 'Buff' + g.pick.stat + 'Max' })
    ],
    test: v => { const keys = numericBuffKeys(v); return keys.length === 1 && STAT_MAX.test(keys[0]); },
    zeroOthers: (k, g) => /^Buff/.test(k) && k !== 'Buff' + g.pick.stat + 'Max',
    line: v => '+' + fmt(v.BuffMax),
    desc: (v, zh, p) => zh ? opt(p[0], v.stat, true) + '上限+' + fmt(v.BuffMax) : 'Max ' + opt(p[0], v.stat) + ' +' + fmt(v.BuffMax) + '.' });

  fx({ id: 'game:cold', src: 'game', cat: 'body', script: 'AE_ColdDebuffReduce',
    en: 'Resist the cold', zh: '耐寒', what: ['Cold snaps take less Satiety, Morale and Vitality.', '寒潮受冻时扣减的饱腹、心态、活力减少。'],
    params: [pct('FloatValue0', 'Less cold damage', '受冻减免', [25, 40, 50])],
    line: v => '-' + fmt(v.FloatValue0) + '%',
    desc: (v, zh) => zh ? '寒潮受冻时，扣减的饱腹 / 心态 / 活力减免' + fmt(v.FloatValue0) + '%' : 'Cold snaps take ' + fmt(v.FloatValue0) + '% less Satiety, Morale and Vitality.' });

  fx({ id: 'game:morale-drop', src: 'game', cat: 'morale', script: 'PostDisaster_PsychologicalAdjustment',
    en: 'Morale drops slower', zh: '心态下降减少', what: ['Morale falls slower on its own.', '心态值自然下降更慢。'],
    params: [pct('FloatValue0', 'Slower Morale drop', '心态下降减少', [10, 20, 30, 40, 45], { scale: -100 })],
    line: v => '-' + fmt(v.FloatValue0) + '%',
    desc: (v, zh) => zh ? '心态值自然下降减少' + fmt(v.FloatValue0) + '%' : 'Morale drops ' + fmt(v.FloatValue0) + '% slower on its own.' });

  fx({ id: 'game:airdrop', src: 'game', cat: 'items', script: 'AddItemToBag_307510',
    en: 'Airdrop next day', zh: '次日空投', what: ['A radio call brings random items of one kind the next day.', '无线电呼救，第二天获得一类随机物品。'],
    params: [pick('IntValue0', 'What drops', '空投类别', () => named.groups, 201, { unknown: groupUnknown }), num('IntValue1', 'How many', '数量', ['items', '件'], [1, 2, 3])],
    line: (v, zh) => itemCount(v.IntValue1, zh),
    desc: (v, zh, p) => zh ? '无线电呼救请求紧急补给，第二天获得' + fmt(v.IntValue1) + '件随机' + opt(p[0], v.IntValue0, true) : 'A radio call brings ' + fmt(v.IntValue1) + ' random ' + opt(p[0], v.IntValue0) + ' the next day.' });

  fx({ id: 'game:scavenge', src: 'game', cat: 'items', script: 'Survival_Context',
    en: 'Random items each day', zh: '每日随机物品', what: ['Random items now, then more every day for a while.', '立即获得随机物品，之后每天再获得。'],
    params: [num('IntValue1', 'Items each day', '每天数量', ['items', '件'], [2, 3, 4])],
    line: (v, zh) => fmt(v.IntValue1) + (zh ? '件/天' : '/day'),
    desc: (v, zh) => zh ? '立即获得' + fmt(v.IntValue1) + '件随机物品，之后数天每天再获得' + fmt(v.IntValue1) + '件' : 'Get ' + fmt(v.IntValue1) + ' random items at once, then ' + fmt(v.IntValue1) + ' more each day for a few days.' });

  fx({ id: 'game:skill-start', src: 'game', cat: 'skills', script: 'EndlessProficiencyGrant', modes: 'endless',
    en: 'Start with a skill level', zh: '开局熟练度', what: ['A skill starts higher. Endless mode only.', '开局时某项熟练度提升。仅无尽模式。'],
    params: [pick('StrValue0', 'Skill', '熟练度', () => named.skills, 'Production'), num('IntValue0', 'Levels', '等级', ['Lv', '级'], [1, 2])],
    line: (v, zh) => '+' + fmt(v.IntValue0) + (zh ? ' 级' : ' Lv'),
    desc: (v, zh, p) => zh ? '开局时' + opt(p[0], v.StrValue0, true) + '熟练度提升 ' + fmt(v.IntValue0) + ' 级' : opt(p[0], v.StrValue0) + ' skill starts ' + plural(v.IntValue0, 'level') + ' higher.' });

  fx({ id: 'game:skill-xp', src: 'game', cat: 'skills', script: 'AE_CookExpMultiplier', scripts: XP_SKILLS.map(s => 'AE_' + s[0] + 'ExpMultiplier'),
    en: 'Learn a skill faster', zh: '熟练度经验加成', what: ['More experience for one skill.', '某项熟练度获得更多经验。'],
    params: [
      pick('skill', 'Skill', '熟练度', () => named.xp, 'Cook', { virtual: true, from: (v, script) => { const m = /^AE_(.*)ExpMultiplier$/.exec(script); return m ? m[1] : 'Cook'; } }),
      pct('FloatValue0', 'More experience', '经验加成', [25, 50, 75])
    ],
    scriptOf: g => 'AE_' + g.pick.skill + 'ExpMultiplier',
    line: v => '+' + fmt(v.FloatValue0) + '%',
    desc: (v, zh, p) => zh ? opt(p[0], v.skill, true) + '经验+' + fmt(v.FloatValue0) + '%' : opt(p[0], v.skill) + ' experience +' + fmt(v.FloatValue0) + '%.' });

  fx({ id: 'game:work-speed', src: 'game', cat: 'speed', script: 'AE_ProductionSpeedMultiplier', scripts: WORK.map(w => 'AE_' + w[0] + 'SpeedMultiplier'),
    en: 'Work faster', zh: '工作加速', what: ['One kind of work takes less time.', '某类工作耗时更短。'],
    params: [
      pick('work', 'Work', '工作', () => named.work, 'Production', { virtual: true, from: (v, script) => { const m = /^AE_(.*)SpeedMultiplier$/.exec(script); return m ? m[1] : 'Production'; } }),
      pct('FloatValue0', 'Faster by', '加速', [10, 20, 30])
    ],
    scriptOf: g => 'AE_' + g.pick.work + 'SpeedMultiplier',
    line: v => '+' + fmt(v.FloatValue0) + '%',
    desc: (v, zh, p) => zh ? opt(p[0], v.work, true) + '速度+' + fmt(v.FloatValue0) + '%' : opt(p[0], v.work) + ' ' + fmt(v.FloatValue0) + '% faster.' });

  fx({ id: 'game:night-move', src: 'game', cat: 'speed', script: 'DayNightMovement',
    en: 'Move faster at night', zh: '夜间移动加速', what: ['Faster walking at night.', '夜间移动速度增加。'],
    params: [pct('FloatValue0', 'Faster by', '加速', [30, 40, 50])],
    line: v => '+' + fmt(v.FloatValue0) + '%',
    desc: (v, zh) => zh ? '夜间移动速度增加' + fmt(v.FloatValue0) + '%' : 'Move ' + fmt(v.FloatValue0) + '% faster at night.' });

  fx({ id: 'game:power-use', src: 'game', cat: 'home', script: 'AE_PowerCostReduce',
    en: 'Use less power', zh: '降低耗电', what: ['Every powered device draws less.', '用电设备总耗电降低。'],
    params: [pct('FloatValue0', 'Less power', '耗电降低', [10, 15, 20])],
    line: v => '-' + fmt(v.FloatValue0) + '%',
    desc: (v, zh) => zh ? '用电设备总耗电降低' + fmt(v.FloatValue0) + '%' : 'Powered devices use ' + fmt(v.FloatValue0) + '% less power.' });

  fx({ id: 'game:solar', src: 'game', cat: 'home', script: 'AE_PowerSolarEfficiency',
    en: 'Better solar panels', zh: '太阳能增效', what: ['Solar panels make more power.', '太阳能板产电效率提高。'],
    params: [pct('FloatValue0', 'More power', '产电提高', [20, 30, 40])],
    line: v => '+' + fmt(v.FloatValue0) + '%',
    desc: (v, zh) => zh ? '太阳能板产电效率提高' + fmt(v.FloatValue0) + '%' : 'Solar panel output increased by ' + fmt(v.FloatValue0) + '%.' });

  fx({ id: 'game:fridge', src: 'game', cat: 'home', script: 'AE_FridgeColdEnhance',
    en: 'Fridge keeps food longer', zh: '冰箱保鲜', what: ['Food in the fridge spoils slower.', '冰箱内食物变质更慢。'],
    params: [pct('FloatValue0', 'Slower spoiling', '变质减慢', [15, 30, 45])],
    line: v => '-' + fmt(v.FloatValue0) + '%',
    desc: (v, zh) => zh ? '冰箱内食物变质速度再降低' + fmt(v.FloatValue0) + '%' : 'Food in the fridge spoils ' + fmt(v.FloatValue0) + '% slower.' });

  fx({ id: 'game:repair', src: 'game', cat: 'home', script: 'Buff_305510',
    en: 'Repair doors and windows better', zh: '修理门窗增效', what: ['Each repair restores more.', '每次修理恢复更多。'],
    params: [pct('FloatValue0', 'More per repair', '修理加成', [15, 30, 45, 60, 75])],
    line: v => '+' + fmt(v.FloatValue0) + '%',
    desc: (v, zh) => zh ? '修理门窗效率提升' + fmt(v.FloatValue0) + '%' : 'Repairs to windows and doors restore ' + fmt(v.FloatValue0) + '% more.' });

  fx({ id: 'game:harvest', src: 'game', cat: 'farm', script: 'AE_HarvestWindowExtend',
    en: 'Longer harvest window', zh: '延长收获窗口', what: ['Ripe crops wait longer before they rot.', '作物成熟后更晚腐烂。'],
    params: [pct('FloatValue0', 'Longer by', '延长', [30, 60, 100], { step: 10 })],
    line: v => '+' + fmt(v.FloatValue0) + '%',
    desc: (v, zh) => zh ? '作物成熟后的收获窗口延长' + fmt(v.FloatValue0) + '%' : 'Harvest window extended by ' + fmt(v.FloatValue0) + '%.' });

  fx({ id: 'game:no-till', src: 'game', cat: 'farm', script: 'AE_SkipTillSoil',
    en: 'No tilling', zh: '免耕', what: ['Plant without tilling the soil first. No numbers.', '无需翻土即可种植。没有数值。'],
    params: [],
    line: (v, zh) => zh ? '免耕' : 'No tilling',
    desc: (v, zh) => zh ? '种植前无需翻土' : 'Plant without tilling the soil first.' });

  fx({ id: 'game:bite', src: 'game', cat: 'defense', script: 'AE_TowerBiteDamageReduce',
    en: 'Defenses take less bite damage', zh: '防御设施减伤', what: ['Bites hurt your defense structures less.', '防御设施受到的啃咬伤害降低。'],
    params: [pct('FloatValue0', 'Less damage', '伤害降低', [30, 40, 50])],
    line: v => '-' + fmt(v.FloatValue0) + '%',
    desc: (v, zh) => zh ? '防御设施受到的啃咬伤害降低 ' + fmt(v.FloatValue0) + '%' : 'Bite damage taken by defense structures reduced by ' + fmt(v.FloatValue0) + '%.' });

  fx({ id: 'game:trap-catch', src: 'game', cat: 'defense', script: 'AE_TrapCaptureMultiplier',
    en: 'Traps catch more', zh: '陷阱捕获加成', what: ['Better odds of a catch.', '陷阱捕获几率提高。'],
    params: [pct('FloatValue0', 'Better odds', '捕获加成', [10, 20, 30])],
    line: v => '+' + fmt(v.FloatValue0) + '%',
    desc: (v, zh) => zh ? '陷阱捕获几率+' + fmt(v.FloatValue0) + '%' : 'Traps catch ' + fmt(v.FloatValue0) + '% more often.' });

  fx({ id: 'game:trade', src: 'game', cat: 'money', script: 'AE_StrangerTradeAppraisalBonus',
    en: 'Better trades with strangers', zh: '交易估值提升', what: ['Strangers value what you offer higher.', '与陌生人交易时物品估值提升。'],
    params: [pct('FloatValue0', 'Higher value', '估值提升', [10, 20, 30])],
    line: v => '+' + fmt(v.FloatValue0) + '%',
    desc: (v, zh) => zh ? '与陌生人交易时，给出物品的估值提升' + fmt(v.FloatValue0) + '%，更易成交' : 'Items you offer strangers are valued ' + fmt(v.FloatValue0) + '% higher.' });

  fx({ id: 'game:drone-trips', src: 'game', cat: 'money', script: 'AE_StrangerTradeDroneExtraTrip',
    en: 'More drone trades a day', zh: '无人机交易次数', what: ['The trade drone can trade more times each day.', '无人机每日可交易次数增加。'],
    params: [num('FloatValue0', 'More trades', '额外次数', ['', '次'], [1, 2], { min: 0 })],
    line: v => '+' + fmt(v.FloatValue0),
    desc: (v, zh) => zh ? '无人机每日可交易次数+' + fmt(v.FloatValue0) : 'The trade drone can make ' + plural(v.FloatValue0, 'more trade') + ' a day.' });

  fx({ id: 'game:brew-speed', src: 'game', cat: 'body', script: 'AE_BrewSpeedUp',
    en: 'Faster brewing', zh: '酿酒加速', what: ['Brewing takes less time.', '酿酒所需时间缩短。'],
    params: [pct('FloatValue0', 'Less time', '时间缩短', [10, 20, 30])],
    line: v => '-' + fmt(v.FloatValue0) + '%',
    desc: (v, zh) => zh ? '酿酒所需时间缩短' + fmt(v.FloatValue0) + '%' : 'Brewing takes ' + fmt(v.FloatValue0) + '% less time.' });

  fx({ id: 'game:brew-yield', src: 'game', cat: 'body', script: 'AE_BrewBottleYield',
    en: 'More bottles per brew', zh: '酿酒出瓶更多', what: ['Each bottle needs less Satiety, so a brew gives more bottles.', '酿酒时每瓶所需的饱腹度降低，能酿出更多酒。'],
    params: [pct('FloatValue0', 'Less per bottle', '每瓶降低', [10, 20, 30])],
    line: v => '-' + fmt(v.FloatValue0) + '%',
    desc: (v, zh) => zh ? '酿酒时每瓶所需的饱腹度降低' + fmt(v.FloatValue0) + '%' : 'Each bottle needs ' + fmt(v.FloatValue0) + '% less Satiety when brewing.' });

  fx({ id: 'game:settle-discount', src: 'game', cat: 'money', script: 'SettlementDiscount',
    en: 'Cheaper end-of-day rewards', zh: '结算奖励折扣', what: ['Rewards bought at settlement cost less.', '结算时购买奖励更便宜。'],
    params: [pct('FloatValue0', 'Discount', '折扣', [15, 20, 25])],
    line: v => '-' + fmt(v.FloatValue0) + '%',
    desc: (v, zh) => zh ? '结算时购买奖励选项享受' + fmt(v.FloatValue0) + '%折扣' : fmt(v.FloatValue0) + '% off rewards bought at settlement.' });

  fx({ id: 'game:savings', src: 'game', cat: 'money', script: 'Savings_Plan',
    en: 'More Survival Points each day', zh: '每日生存点加成', what: ['Each day pays more Survival Points.', '每天获得的生存点增加。'],
    params: [pct('FloatValue0', 'More points', '生存点加成', [5, 10, 15])],
    line: v => '+' + fmt(v.FloatValue0) + '%',
    desc: (v, zh) => zh ? '每天获得的生存点增加' + fmt(v.FloatValue0) + '%' : 'Daily Survival Points +' + fmt(v.FloatValue0) + '%.' });

  // ---- Game scripts only the game's in-run talents use. ----

  fx({ id: 'game:backpack-life', src: 'game', cat: 'items', script: 'AE_BackpackShelfLife',
    en: 'Backpack keeps food longer', zh: '背包保鲜', what: ['Items in your backpack spoil slower.', '随身背包里的物品保质期延长。'],
    params: [num('FloatValue0', 'Shelf life', '保质期', ['x', '倍'], [2, 3, 4], { add: 1, min: 1, step: 0.5 })],
    line: (v, zh) => fmt(v.FloatValue0) + (zh ? '倍' : 'x'),
    desc: (v, zh) => zh ? '随身背包里的物品保质期延长至' + fmt(v.FloatValue0) + '倍' : 'Items in your backpack keep ' + fmt(v.FloatValue0) + 'x as long.' });

  fx({ id: 'game:quick-rest', src: 'game', cat: 'body', script: 'QuickRest',
    en: 'Rest restores Stamina faster', zh: '休息恢复加快', what: ['Resting brings Stamina back faster.', '通过休息恢复精力更快。'],
    params: [pct('FloatValue0', 'Faster by', '加快', [10, 20, 30])],
    line: v => '+' + fmt(v.FloatValue0) + '%',
    desc: (v, zh) => zh ? '通过休息恢复精力的速度加快' + fmt(v.FloatValue0) + '%' : 'Resting restores Stamina ' + fmt(v.FloatValue0) + '% faster.' });

  fx({ id: 'game:press-nutrition', src: 'game', cat: 'body', script: 'AE_CompressValueRate',
    en: 'Ration Press keeps more nutrition', zh: '压制保留更多营养', what: ['The Ration Press keeps more of the food\'s nutrition (normally 60%).', '使用口粮压制机时保留更多营养（原本为 60%）。'],
    params: [pct('FloatValue0', 'More kept', '多保留', [5, 10, 20])],
    line: v => '+' + fmt(v.FloatValue0) + '%',
    desc: (v, zh) => zh ? '使用口粮压制机时，营养保留从 60% 提高到 ' + fmt(60 + v.FloatValue0) + '%' : 'The Ration Press keeps ' + fmt(60 + v.FloatValue0) + '% of the food\'s nutrition, up from 60%.' });

  fx({ id: 'game:exposure', src: 'game', cat: 'explore', script: 'AE_ExposureReduceMultiplier',
    en: 'Exposure builds slower', zh: '暴露值增长降低', what: ['Exposure builds up slower while exploring.', '探索时暴露值增长更慢。'],
    params: [pct('FloatValue0', 'Slower by', '降低', [10, 20, 30], { scale: -100 })],
    line: v => '-' + fmt(v.FloatValue0) + '%',
    desc: (v, zh) => zh ? '探索时暴露值增长-' + fmt(v.FloatValue0) + '%' : 'Exposure builds up ' + fmt(v.FloatValue0) + '% slower while exploring.' });

  fx({ id: 'game:robot', src: 'game', cat: 'home', script: robotScript('HK'), scripts: ROBOTS.map(r => robotScript(r[0])),
    en: 'Start with a robot', zh: '开局机器人', what: ['A robot and all its crafting blueprints from the start of the run. No numbers.', '开局获得一台机器人及其全部制造图纸。没有数值。'],
    params: [pick('robot', 'Robot', '机器人', () => named.robots, 'HK', { virtual: true, from: (v, script) => { const m = /^EndlessRobotGrant_(.*)$/.exec(script); return m ? m[1] : 'HK'; } })],
    scriptOf: g => robotScript(g.pick.robot),
    line: (v, zh) => robotName(v.robot, zh).split(' ')[0] + (zh ? ' 机器人' : ' robot'),
    desc: (v, zh) => zh ? '开局获得一台' + robotName(v.robot, true) + '，并掌握其全部制造图纸' : 'Start the run with ' + robotName(v.robot, false, true) + ' and all its crafting blueprints.' });

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

  fx({ id: 'game:drone-scavenge', src: 'game', cat: 'items', script: 'AE_DroneForagingExtraTrip',
    en: 'More drone scavenging a day', zh: '无人机拾荒次数', what: ['The drone can go scavenging more times each day.', '无人机每日可外出拾荒的次数增加。'],
    params: [num('FloatValue0', 'More trips', '额外次数', ['', '次'], [1, 2, 3], { min: 0 })],
    line: v => '+' + fmt(v.FloatValue0),
    desc: (v, zh) => zh ? '无人机每日可外出拾荒次数+' + fmt(v.FloatValue0) : 'The drone can go scavenging ' + plural(v.FloatValue0, 'more time') + ' a day.' });

  fx({ id: 'game:generator', src: 'game', cat: 'home', script: 'AE_PowerGeneratorEfficiency',
    en: 'Better generators', zh: '发电机增效', what: ['Generators make more power.', '发电机产电效率提高。'],
    params: [pct('FloatValue0', 'More power', '产电提高', [10, 20, 30])],
    line: v => '+' + fmt(v.FloatValue0) + '%',
    desc: (v, zh) => zh ? '发电机产电效率提高' + fmt(v.FloatValue0) + '%' : 'Generator output increased by ' + fmt(v.FloatValue0) + '%.' });

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

  const FX_BY_ID = {};

  FX.forEach(f => { FX_BY_ID[f.id] = f; });

  // ---- Params of an effect, and what a number in the file reads as. ----

  const picksOf = f => f.params.filter(p => p.type === 'pick');
  const numsOf = f => f.params.filter(p => p.type === 'num');
  const hidesOf = f => f.params.filter(p => p.type === 'hide');
  const fieldName = (g, p) => p.dyn ? p.dyn(g) : p.key;
  const display = (p, raw) => (isNum(raw) ? raw : 0) * p.scale + p.add;

  function toRaw(p, field, shownValue) {
    const raw = (shownValue - p.add) / p.scale;

    return INT_FIELD.test(field) ? Math.round(raw) : Math.round(raw * 1e6) / 1e6;
  }

  // A level past the effect's own defaults keeps climbing by the last step.
  function defaultAt(p, i) {
    const def = p.def;
    const last = def[def.length - 1];
    const step = def.length > 1 ? last - def[def.length - 2] : 0;
    const v = i < def.length ? def[i] : last + step * (i - def.length + 1);

    return Math.max(p.min, round2(v));
  }

  const isAvailable = f => f.needs ? !!(st.data && st.data[f.needs]) : !!f.nlp || f.scripts.some(s => st.scripts.has(s));

  // ---- Which effect a script and its numbers are. ----

  function match(script, values) {
    const found = FX.find(f => f.pickable && isAvailable(f) && f.scripts.indexOf(script) >= 0 &&
      hidesOf(f).every(p => sameVal(values[p.key], p.def)) && (!f.test || f.test(values)));

    return found || rawFx(script);
  }

  const rawCache = {};
  const FIELD_RANK = k => /^(IntValue|FloatValue)\d/.test(k) ? 0 : /^Buff/.test(k) ? 1 : 2;

  function prettyScript(script) {
    return script.replace(/^(AE_|PostDisaster_|PreDisaster_|Buff_)/, '').replace(/_?\d{4,}_?/g, ' ').replace(/([a-z])([A-Z])/g, '$1 $2').replace(/_/g, ' ').trim() || script;
  }

  function rawLabel(k) {
    const m = /^(IntValue|FloatValue)(\d+)$/.exec(k);

    if (!m) return { en: k, zh: k };

    return m[1] === 'IntValue' ? { en: 'Whole number ' + m[2], zh: '整数 ' + m[2] } : { en: 'Number ' + m[2], zh: '数值 ' + m[2] };
  }

  // A game script with no friendly name: its numbers are the raw fields the game's rewards set.
  function rawFx(script) {
    if (rawCache[script]) return rawCache[script];

    const users = st.game.filter(t => t.levels.some(l => l.script === script));
    const levels = [].concat.apply([], users.map(t => t.levels.filter(l => l.script === script)));
    const seen = new Set();

    levels.forEach(l => Object.keys(l.values).forEach(k => { if (isNum(l.values[k])) seen.add(k); }));

    const keys = Array.from(seen).sort((a, b) => FIELD_RANK(a) - FIELD_RANK(b) || (a < b ? -1 : a > b ? 1 : 0)).slice(0, 4);
    const firstLevels = users.length ? users[0].levels.filter(l => l.script === script) : [];
    const params = keys.map(k => {
      const all = levels.map(l => l.values[k]).filter(isNum);
      const label = rawLabel(k);

      return num(k, label.en, label.zh, ['', ''], firstLevels.length ? firstLevels.map(l => isNum(l.values[k]) ? l.values[k] : 0) : [0], {
        step: all.every(v => Math.floor(v) === v) ? 1 : 0.05,
        min: -Infinity
      });
    });
    const name = prettyScript(script);
    const made = {
      id: 'raw:' + script, src: 'game', cat: 'raw', script, scripts: [script], raw: true, pickable: true, modes: 'both',
      usedBy: [], finishes: [], en: name, zh: name, what: ['The game\'s script, with its raw numbers.', '游戏脚本，使用原始数值。'],
      params,
      line: v => keys.map(k => fmt(v[k])).join(', '),
      desc: (v, zh) => params.map(p => (zh ? p.zh : p.en) + ' ' + fmt(v[p.key])).join(zh ? '，' : ', ')
    };

    rawCache[script] = made;

    return made;
  }

  // ---- The whole catalogue, built once the game's data is in. ----

  const cat = { list: [], icons: [], defaultIcon: { icon: '', unityIcon: '' } };

  // The talents' own images, for a plugin that sends no icon list of its own.
  function talentIcons() {
    const seen = new Set();
    const icons = [];

    st.game.forEach(t => {
      if (!t.icon || seen.has(t.icon)) return;

      seen.add(t.icon);
      icons.push({ icon: t.icon, unityIcon: t.unityIcon, group: REWARD_ICONS });
    });

    return icons;
  }

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

  function fxIcon(f) {
    if (f.icon) return f.icon;

    const tid = f.usedBy[0] || f.finishes[0];
    const t = tid ? st.byId[tid] : null;

    return t && t.icon ? t.icon : cat.defaultIcon.icon;
  }

  const fxName = (f, zh) => zh ? f.zh : f.en;

  // ===== Gives: one effect of a reward, with its picks and a number per level =====

  const fromRoot = path => String(path || '').replace(/^(\.\.\/)+/, '');
  // A New Loop Plus effect runs no game script, so it has no game row to start from: ''.
  const scriptOfGive = give => give.effect.indexOf(GAME_PREFIX) === 0 ? give.effect.slice(GAME_PREFIX.length) : '';

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

  // Fields the effect does not show but this reward sets apart from the row the plugin copies; kept as they are.
  function fixedFrom(g, v0) {
    const f = g.fx;
    const covered = new Set(numsOf(f).map(p => fieldName(g, p)).concat(hidesOf(f).map(p => p.key), picksOf(f).filter(p => !p.virtual).map(p => p.key)));
    const tpls = [tplValues(g.script, g.tid, 1), firstTpl(g.script)];
    const keys = new Set(Object.keys(v0));
    const fixed = {};

    tpls.forEach(t => Object.keys(t).forEach(k => keys.add(k)));
    keys.forEach(k => {
      if (covered.has(k)) return;

      if (f.zeroOthers && f.zeroOthers(k, g) && isEmptyVal(v0[k])) return;

      if (!tpls.some(t => !sameVal(t[k], v0[k]))) return;

      const tpl = tpls.find(t => k in t);

      fixed[k] = k in v0 ? clone(v0[k]) : zeroOf(tpl ? tpl[k] : 0);
    });

    return fixed;
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

  // A give the tab may edit is one it would write back exactly as it found it.
  function roundTrips(g, vals, count) {
    const out = writeGive(g, count);

    return range(count).every(i => {
      const made = effectValues(out, g.tid, i);
      const want = vals[Math.min(i, vals.length - 1)];

      return allKeys(made, want).every(k => sameVal(made[k], want[k]));
    });
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

  const isEditable = e => !e.frozen;
  const canEditGives = e => isEditable(e) && e.kind !== 'finished' && !e.givesLocked;
  const isAutoOn = e => e.autoText && e.gives.length > 0 && e.gives.every(g => !g.fx.raw) && !e.givesLocked;
  const showsAuto = e => isAutoOn(e) && e.touched;

  function valsOf(g, i) {
    const v = Object.assign({}, g.pick);

    numsOf(g.fx).forEach(p => {
      const per = g.per[p.key];

      v[p.key] = per[Math.min(i, per.length - 1)];
    });

    return v;
  }

  const autoLine = (g, i, zh) => g.fx.line(valsOf(g, i), zh);

  function autoLevelText(e, zh) {
    return range(e.levels.length).map(i => e.gives.map(g => autoLine(g, i, zh)).join(zh ? '，' : ', ')).join(' / ');
  }

  function autoDesc(e, i, zh) {
    return e.gives.map(g => g.fx.desc(valsOf(g, i), zh, g.fx.params)).join(zh ? '；' : ' ');
  }

  // ===== The text the player reads =====

  const baseText = (e, key) => e.base ? e.base[key] : null;
  const nameOf = (e, lang) => shown(e.name, baseText(e, 'name'), lang) || ('#' + e.id);

  function levelLineOf(e, lang) {
    if (showsAuto(e)) return autoLevelText(e, lang === 'zh');

    return shown(e.levelText, baseText(e, 'levelText'), lang);
  }

  function descOf(e, i, lang) {
    const l = e.levels[i];

    if (showsAuto(e)) return autoDesc(e, i, lang === 'zh');

    if (!l) return '';

    return textIn(l.desc, lang) || textIn(e.description, lang) || textIn(l.gameDesc, lang);
  }

  const hasLevelDescs = e => e.levels.some(l => !!l.desc);

  // ===== Building and changing the list =====

  function buildEntries(keepId) {
    const talents = st.file && isArr(st.file.talents) ? st.file.talents : [];

    st.entries = [];
    st.ghosts = {};
    talents.forEach((raw, i) => {
      if (isObj(raw) && isNum(raw.id)) st.entries.push(entryFromRaw(raw, i));
    });
    st.dirty = false;

    const keep = keepId && entryOrGhost(keepId) ? keepId : 0;
    const first = st.entries.find(e => !e.removed) || st.game.find(t => t.show === 1 || t.show === 2);

    st.sel = keep || (first ? first.id : 0);
    st.lv = Math.min(st.lv, Math.max(0, levelCount(st.sel) - 1));
  }

  const entryOf = id => st.entries.find(e => e.id === id && !e.removed) || null;

  function ghostOf(id) {
    if (!st.byId[id]) return null;

    if (!st.ghosts[id]) st.ghosts[id] = entryFromRaw({ id, enabled: true }, -1);

    return st.ghosts[id];
  }

  const entryOrGhost = id => entryOf(id) || ghostOf(id);

  function levelCount(id) {
    const e = entryOrGhost(id);

    return e ? e.levels.length : 0;
  }

  function markChanged(e) {
    e.touched = true;
    st.dirty = true;
  }

  function givesChanged(e) {
    e.givesTouched = true;
    markChanged(e);

    if (!e.autoManual && !e.autoText) e.autoText = e.gives.every(g => !g.fx.raw);
  }

  // Turns a game reward that is not in the file into one that is, so it can be edited.
  function adopt(id) {
    const e = ghostOf(id);

    if (!e) return null;

    delete st.ghosts[id];
    st.entries.push(e);
    markChanged(e);

    return e;
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

  // Reset drops this tab's changes to a game reward; a reward of your own is deleted. Either way it leaves the file.
  function resetOrDelete(e) {
    if (e.rawIndex >= 0) e.removed = true;
    else st.entries.splice(st.entries.indexOf(e), 1);

    st.dirty = true;
  }

  // ---- Levels ----

  const nextStep = arr => {
    const n = arr.length;
    const last = arr[n - 1];

    return n > 1 ? last + (last - arr[n - 2]) : last;
  };

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

  function setGive(e, f, gi) {
    const g = makeGive(f, '', e.id, null, e.levels.length);

    if (gi >= 0 && gi < e.gives.length) e.gives[gi] = g;
    else e.gives.push(g);

    givesChanged(e);

    return g;
  }

  // A pick that chooses the script (a skill) or the field (a stat) starts the effect's row afresh.
  function setPick(e, g, key, value) {
    const p = picksOf(g.fx).find(x => x.key === key);

    if (!p) return;

    g.pick[key] = value;

    if (p.virtual) {
      g.script = g.fx.scriptOf ? g.fx.scriptOf(g) : g.script;
      g.fixed = {};

      if (g.fx.init) g.fx.init(g, tplValues(g.script, g.tid, 1));
    }

    givesChanged(e);
  }

  // ===== Saving =====

  function writeText(own, fallback) {
    const fb = fallback || { en: '', zh: '' };
    const en = (own && own.en) || fb.en || (own && own.zh) || '';
    const zh = (own && own.zh) || fb.zh || (own && own.en) || '';

    return en || zh ? { en, zh } : undefined;
  }

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

  function saved(ok, msg) {
    if (ok && st.pending) {
      st.file = st.pending;
      buildEntries(st.sel);
    }

    st.pending = null;
    toast(esc(msg || ''));
    render();
  }

  function discard() {
    buildEntries(st.sel);
    render();
  }

  function send(msg) {
    try { window.vuplex.postMessage(PREFIX + msg); } catch (e) {}
  }

  // ===== Drawing the tab =====

  const numText = v => String(Math.round((isNum(v) ? v : 0) * 10000) / 10000);
  const unitOf = p => p.unit ? (st.lang === 'zh' ? p.unit[1] : p.unit[0]) : '';
  const tag = (cls, en, zh) => '<span class="tg ' + cls + '">' + L(en, zh) + '</span>';
  const isGhost = e => !!e && !entryOf(e.id);
  const isOwn = e => e.fresh || e.kind === 'new';

  function render() {
    const root = st.root;

    if (!root) return;

    const list = root.querySelector('.nlp-list');
    const ed = root.querySelector('.ed');
    const edScroll = ed ? ed.scrollTop : 0;

    if (list) st.listScroll = list.scrollTop;

    root.innerHTML = topHtml() + bannersHtml() + (st.data ? mainHtml() : '<div class="nlp-wait">' + L('Reading the loop rewards...', '正在读取轮回奖励...') + '</div>');

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
      '<div><div class="nlp-title">' + L('LOOP REWARDS', '轮回奖励') + '</div>' +
      '<div class="nlp-sub">' + L('What each reward costs, how many levels it has, and what it gives. Applies at the next game start.', '每个奖励的消耗、等级数和效果。下次启动游戏时生效。') + '</div></div>' +
      '<span class="sp"></span>' +
      '<span class="new-btn" data-a="new">+ ' + L('NEW REWARD', '新建奖励') + '</span>' +
      '<div class="nlp-chips">' + chip('file', 'CHANGED', '已修改', fileCount) + chip('all', 'ALL REWARDS', '全部奖励', allCount) + '</div>' +
      '<span class="key-btn" data-a="discard">' + L('DISCARD', '放弃') + '</span>' +
      '<span class="key-btn" data-a="save">' + L('SAVE', '保存') + '</span>' +
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
    const ids = st.game.filter(t => t.show === 1 || t.show === 2).map(t => t.id);

    st.entries.forEach(e => {
      if (!e.removed && !st.byId[e.id]) ids.push(e.id);
    });

    return ids;
  }

  function listIds() {
    const ids = st.view === 'all' ? allIds() : st.entries.filter(e => !e.removed).map(e => e.id);

    return ids.slice().sort((a, b) => a - b);
  }

  function columnOf(id) {
    const e = entryOf(id);

    if (e) return e.column;

    const t = st.byId[id];

    return t && (t.show === 1 || t.show === 2) ? t.show : id < 2000 ? 1 : 2;
  }

  function mainHtml() {
    const ids = listIds();
    const col = (n, en, zh) => {
      const mine = ids.filter(id => columnOf(id) === n);

      return '<div class="col-h">' + L(en, zh) + '<small>' + mine.length + '</small></div>' + mine.map(rowHtml).join('');
    };
    const sel = entryOrGhost(st.sel);

    return '<div class="nlp-main">' +
      '<div class="nlp-list">' + col(1, 'PREPARATION', '筹备能力') + col(2, 'SURVIVAL', '生存能力') + '</div>' +
      '<div class="ed">' + (sel ? editorHtml(sel) : '') + '</div>' +
      '</div>';
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
    const cls = ['ab'];

    if (id === st.sel) cls.push('is-sel');

    if (!e && t && t.available === 0) cls.push('is-out');

    if (e && !e.enabled) cls.push('is-off');

    const toggle = e ? '<label class="setting-toggle" title="' + esc(e.kind === 'changed' ? L('Switch your changes on or off', '开关你的修改') : L('Switch this reward on or off', '开关此奖励')) + '">' +
      '<input type="checkbox" data-a="tog" data-v="' + id + '"' + (e.enabled ? ' checked' : '') + '><span class="track"></span><span class="knob"></span></label>' : '<span></span>';

    return '<div class="' + cls.join(' ') + '" data-a="sel" data-v="' + id + '">' +
      '<img src="' + esc(icon) + '" alt="">' +
      '<div><div class="nm">' + esc(name) + '</div><div class="mt">' + rowTag(e, id) + '<span class="lvn">' + (st.lang === 'zh' ? count + '级' : count + (count === 1 ? ' level' : ' levels')) + '</span></div></div>' +
      toggle + '</div>';
  }

  // ---- The editor ----

  function editorHtml(e) {
    const ghost = isGhost(e);
    let html = previewHtml(e) + whyHtml(e, ghost);

    if (ghost) return html + ghostHtml(e);

    if (e.frozen) return html + actionsHtml(e);

    if (e.fresh) html += lookHtml(e);

    return html + givesHtml(e) + levelsHtml(e) + textHtml(e) + actionsHtml(e);
  }

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
    const mini = (v, label) => '<span data-a="pvl" data-v="' + v + '"' + (lang === v ? ' class="is-on"' : '') + '>' + label + '</span>';

    return '<div class="pv">' +
      '<img src="' + esc(e.icon) + '" alt="">' +
      '<div><div class="pv-name">' + esc(nameOf(e, lang)) + '<span class="lvb">Lv.' + (i + 1) + ' / ' + n + '</span></div>' +
      '<div class="pv-lt">' + lineHtml + '</div>' +
      (desc ? '<div class="pv-desc">' + esc(desc) + '</div>' : '<div class="pv-desc is-empty">' + Lp('Choose what it gives and the description writes itself here.', '选择效果后，这里会写出描述。') + '</div>') +
      '<div class="pv-cost">' + Lp('Cost', '消耗') + ' <b>' + numText(cost) + '</b> ' + Lp('Planning Points', '信念点') + '</div></div>' +
      '<div class="pv-tag">' + L('As the game shows it', '游戏内显示') + '<span class="mini">' + mini('en', 'EN') + mini('zh', '中文') + '</span></div>' +
      '</div>';
  }

  function hasNoScript(e) {
    const base = e.base;

    return !e.fresh && !e.gives.length && !!base && base.levels.length > 0 && NO_SCRIPT.indexOf(base.levels[0].script) >= 0;
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

  function ghostHtml(e) {
    const names = e.gives.map(g => esc(fxName(g.fx, st.lang === 'zh'))).join(', ');

    return (names ? '<div class="nnote">' + L('It gives', '效果') + ': <b>' + names + '</b></div>' : '') +
      '<div class="ed-acts">' +
      '<span class="gbtn" data-a="edit">' + L('CHANGE THIS REWARD', '修改此奖励') + '</span>' +
      '<span class="gbtn dim" data-a="copy">' + L('COPY INTO A NEW REWARD', '复制为新奖励') + '</span>' +
      '</div>';
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

  function pickHtml(e, g, gi, p) {
    if (!canEditGives(e)) return '<span class="gv-w">' + esc(opt(p, g.pick[p.key], st.lang === 'zh')) + '</span>';

    const options = availableOptions(p);
    const current = g.pick[p.key];

    if (!options.some(o => o.v === current)) options.unshift({ v: current, en: opt(p, current, false), zh: opt(p, current, true) });

    return '<select class="nsel" data-a="pk" data-c="' + gi + '" data-k="' + esc(p.key) + '">' +
      options.map(o => '<option value="' + esc(JSON.stringify(o.v)) + '"' + (o.v === current ? ' selected' : '') + '>' + esc(L(o.en, o.zh)) + '</option>').join('') +
      '</select>';
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

  function stepper(key, value, unit, isReadOnly) {
    return '<span class="stp' + (isReadOnly ? ' is-ro' : '') + '">' +
      '<b data-a="dec" data-k="' + key + '">&minus;</b>' +
      '<input data-a="val" data-k="' + key + '" value="' + numText(value) + '"' + (isReadOnly ? ' disabled' : '') + '>' +
      '<i>' + esc(unit) + '</i>' +
      '<b data-a="inc" data-k="' + key + '">+</b></span>';
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

  // The description is per level when any level has its own; otherwise one text covers them all.
  function descTarget(e) {
    const l = e.levels[Math.min(st.lv, e.levels.length - 1)];

    return hasLevelDescs(e) ? { own: l.desc, game: l.gameDesc, perLevel: true } : { own: e.description, game: l ? l.gameDesc : null, perLevel: false };
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

  // Only the preview and the footer change while the player types, so the input keeps its focus.
  function refreshPreview() {
    const pv = st.root ? st.root.querySelector('.pv') : null;
    const e = entryOrGhost(st.sel);

    if (pv && e) pv.replaceWith(h(previewHtml(e)));

    footer();
  }

  function footer() {
    if (!st.ctx || typeof st.ctx.footer !== 'function') return;

    try {
      if (st.dirty) st.ctx.footer('<em>' + L('Unsaved changes', '有未保存的修改') + '</em> &middot; ' + L('they apply at the next game start', '下次启动游戏时生效'), true);
      else st.ctx.footer('<b>' + FILE_NAME + '</b> &middot; ' + L('saved', '已保存'), false);
    } catch (err) {}
  }

  // ===== The effect picker =====

  const talentName = id => st.byId[id] ? textIn(st.byId[id].name, st.lang) : '#' + id;
  const winEl = () => document.querySelector('#slmm .win') || st.root;

  function haystack(f) {
    const names = f.usedBy.concat(f.finishes).map(id => st.byId[id] ? st.byId[id].name.en + ' ' + st.byId[id].name.zh : '');

    return [f.en, f.zh, f.what[0], f.what[1], f.scripts.join(' ')].concat(names).join(' ').toLowerCase();
  }

  function matchesQuery(f, words) {
    if (!words.length) return true;

    const hay = haystack(f);

    return words.every(w => hay.indexOf(w) >= 0);
  }

  function pickerLists() {
    const p = st.picker;
    const words = p.q.toLowerCase().split(/\s+/).filter(Boolean);
    const byQuery = cat.list.filter(f => matchesQuery(f, words));
    const bySrc = byQuery.filter(f => p.src === 'all' || f.src === p.src);
    const shown = bySrc.filter(f => p.cat === 'all' || f.cat === p.cat);

    return { byQuery, bySrc, shown };
  }

  // An effect with no picks gives the same thing twice, so the reward may only have it once.
  function isOnOtherGive(e, f) {
    return picksOf(f).length === 0 && e.gives.some((g, gi) => g.fx === f && !(st.picker.mode === 'replace' && gi === st.picker.gi));
  }

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

  function closePicker() {
    if (st.pickerEl && st.pickerEl.parentNode) st.pickerEl.parentNode.removeChild(st.pickerEl);

    st.pickerEl = null;
    st.picker = null;
  }

  function renderPicker(full) {
    const el = st.pickerEl;
    const e = entryOf(st.sel);

    if (!el || !e) return;

    if (full) {
      el.innerHTML = '<div class="fxp-card">' + pickerHead(e) + '<div class="fxp-bd"></div><div class="fxp-ft"></div></div>';

      const input = el.querySelector('.fxp-q input');

      if (input) {
        input.focus();
        input.setSelectionRange(input.value.length, input.value.length);
      }
    }

    const lists = pickerLists();
    const listEl = el.querySelector('.fxp-list');
    const scroll = listEl ? listEl.scrollTop : 0;

    if (!lists.shown.some(f => f.id === st.picker.selId)) st.picker.selId = lists.shown.length ? lists.shown[0].id : '';

    el.querySelector('.fxp-bd').innerHTML = railHtml(lists.bySrc) + listHtml(e, lists.shown) + detailHtml(e, lists.shown);
    el.querySelector('.fxp-ft').innerHTML = lists.shown.length + ' ' + L('effects', '个效果') + '<span class="sp"></span>' + L('Double-click or Enter adds &middot; Esc closes', '双击或回车添加 &middot; Esc 关闭');

    const newList = el.querySelector('.fxp-list');

    if (newList) newList.scrollTop = scroll;
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

  function railHtml(bySrc) {
    const p = st.picker;
    const item = (v, label, n) => '<div class="ri' + (p.cat === v ? ' is-on' : '') + (n === 0 ? ' is-zero' : '') + '" data-a="cat" data-v="' + v + '"><span>' + label + '</span><b>' + n + '</b></div>';

    return '<div class="fxp-rail"><div class="rh">' + L('KIND', '类别') + '</div>' +
      item('all', L('Everything', '全部'), bySrc.length) +
      CATS.map(c => item(c.id, L(c.en, c.zh), bySrc.filter(f => f.cat === c.id).length)).join('') +
      '</div>';
  }

  function cardMeta(e, f) {
    if (e.gives.some(g => g.fx === f)) return '<span class="tg ex">' + L('ADDED', '已添加') + '</span>';

    if (f.finishes.length) return L('Finishes', '完成') + ' ' + esc(talentName(f.finishes[0]));

    if (!f.usedBy.length) return L('No reward uses it yet', '暂无奖励使用');

    return L('Used by', '用于') + ' ' + esc(talentName(f.usedBy[0])) + (f.usedBy.length > 1 ? ' +' + (f.usedBy.length - 1) : '');
  }

  function listHtml(e, shown) {
    if (!shown.length) return '<div class="fxp-list"><div class="fxp-none">' + L('Nothing matches. Try fewer words, or another kind on the left.', '没有匹配项。试试更少的词，或左边的其他类别。') + '</div></div>';

    const groups = CATS.map(c => {
      const mine = shown.filter(f => f.cat === c.id);

      if (!mine.length) return '';

      return '<div class="ch">' + L(c.en, c.zh).toUpperCase() + '</div>' + mine.map(f => {
        const cls = ['fxc'];

        if (f.id === st.picker.selId) cls.push('is-sel');

        if (e.gives.some(g => g.fx === f)) cls.push('is-added');

        return '<div class="' + cls.join(' ') + '" data-a="card" data-v="' + esc(f.id) + '">' +
          '<img src="' + esc(fxIcon(f)) + '" alt="">' +
          '<div><div class="fxc-n">' + esc(fxName(f, st.lang === 'zh')) + '</div>' +
          '<div class="fxc-w">' + esc(L(f.what[0], f.what[1])) + '</div>' +
          '<div class="fxc-m">' + srcTag(f) + cardMeta(e, f) + '</div></div></div>';
      }).join('');
    }).join('');

    return '<div class="fxp-list">' + groups + '</div>';
  }

  function paramRow(p, count) {
    if (p.type === 'pick') {
      return '<tr><td>' + esc(L(p.en, p.zh)) + ' (' + L('one of', '可选') + ')</td><td>' + esc(availableOptions(p).map(o => L(o.en, o.zh)).join(', ')) + '</td></tr>';
    }

    return '<tr><td>' + esc(L(p.en, p.zh)) + '</td><td>' + esc(range(count).map(i => fmt(defaultAt(p, i))).join(' / ') + ' ' + unitOf(p)) + '</td></tr>';
  }

  function readsHtml(e, f, count) {
    try {
      const g = makeGive(f, '', e.id, null, count);
      const zh = st.lang === 'zh';
      const line = range(count).map(i => autoLine(g, i, zh)).join(' / ');
      const desc = g.fx.desc(valsOf(g, 0), zh, f.params);

      return '<div class="fxd-rd"><div class="l">' + esc(line) + '</div><div class="d">' + esc(desc) + '</div></div>';
    } catch (err) {
      return '';
    }
  }

  function chooseButton(e, f) {
    if (isOnOtherGive(e, f)) return '<span class="gbtn is-dis">' + L('ALREADY ON THIS REWARD', '已在此奖励上') + '</span>';

    if (st.picker.mode === 'replace') return '<span class="gbtn" data-a="choose">' + L('USE THIS INSTEAD', '改用此效果') + '</span>';

    return '<span class="gbtn" data-a="choose">' + L('ADD TO', '添加到') + ' ' + esc(nameOf(e, st.lang)) + '</span>';
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

    if (f.raw) notes.push(L('A game script with no friendly names yet. Its numbers are the raw fields from the game\'s tables.', '尚未命名的游戏脚本。数值为游戏表中的原始字段。'));

    if (!numsOf(f).length) notes.push(L('This effect has no numbers, so a reward with only this effect is the same at every level.', '此效果没有数值，所以只有此效果的奖励每级都一样。'));

    let html = '<div class="fxd"><div class="fxd-top"><img src="' + esc(fxIcon(f)) + '" alt=""><div>' +
      '<div class="fxd-n">' + esc(fxName(f, st.lang === 'zh')) + '</div>' +
      '<div class="fxd-s">' + srcTag(f) + (f.nlp ? [NLP_PREFIX + f.nlp] : f.scripts).map(s => '<code>' + esc(s) + '</code>').join('') + '</div></div></div>' +
      '<div class="fxd-w">' + esc(L(f.what[0], f.what[1])) + '</div>';

    if (shownParams.length) html += '<h4>' + L('NUMBERS IT TAKES', '数值') + '</h4><table>' + shownParams.map(p => paramRow(p, count)).join('') + '</table>';

    html += '<h4>' + (st.lang === 'zh' ? '显示效果：' + count + '级' : 'HOW IT READS AT ' + count + (count === 1 ? ' LEVEL' : ' LEVELS')) + '</h4>' + readsHtml(e, f, count);

    if (users.length) html += '<h4>' + L('REWARDS THAT USE IT', '使用它的奖励') + '</h4><div class="ub">' + users.map(id => '<span><img src="' + esc(st.byId[id] ? st.byId[id].icon : '') + '" alt="">' + esc(talentName(id)) + '</span>').join('') + '</div>';

    if (notes.length) html += '<h4>' + L('NOTES', '提示') + '</h4>' + notes.map(n => '<div class="fn">' + n + '</div>').join('');

    return html + chooseButton(e, f) + '</div>';
  }

  function choose() {
    const e = entryOf(st.sel);
    const f = st.picker ? cat.list.find(x => x.id === st.picker.selId) : null;

    if (!e || !f || isOnOtherGive(e, f)) return;

    const isReplace = st.picker.mode === 'replace';

    setGive(e, f, isReplace ? st.picker.gi : -1);
    closePicker();
    render();
    toast((isReplace ? L('Now gives', '现在的效果：') : L('Added', '已添加')) + ' <b>' + esc(fxName(f, st.lang === 'zh')) + '</b>');
  }

  function onPickerDouble(ev) {
    if (ev.target.closest('[data-a="card"]')) choose();
  }

  function onPickerKey(ev) {
    if (ev.key === 'Escape') {
      ev.preventDefault();
      ev.stopPropagation();
      closePicker();
    } else if (ev.key === 'Enter') {
      ev.preventDefault();
      ev.stopPropagation();
      choose();
    }
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

  // One group at a time: the game has some 1800 item images, too many to load at once.
  function iconsHtml() {
    const groups = [];

    cat.icons.forEach(ic => { if (groups.indexOf(ic.group) < 0) groups.push(ic.group); });

    if (groups.indexOf(st.iconGroup) < 0) st.iconGroup = groups[0] || REWARD_ICONS;

    const chips = groups.length > 1 ? '<div class="icg">' + groups.map(g => '<span data-a="icg" data-v="' + esc(g) + '"' + (g === st.iconGroup ? ' class="is-on"' : '') + '>' + esc(iconGroupName(g)) + '</span>').join('') + '</div>' : '';
    const imgs = cat.icons.map((ic, i) => ic.group === st.iconGroup ? '<img data-a="ic" data-v="' + i + '" src="' + esc(ic.icon) + '" loading="lazy" alt="" onerror="this.style.display=\'none\'">' : '').join('');

    return chips + '<div class="icl">' + imgs + '</div>';
  }

  function iconGroupName(group) {
    const known = ICON_GROUPS[group];

    return known ? L(known[0], known[1]) : group;
  }

  function showIconGroup(group) {
    if (!st.iconsEl) return;

    st.iconGroup = group;
    st.iconsEl.innerHTML = iconsHtml();
  }

  function closeIcons() {
    if (st.iconsEl && st.iconsEl.parentNode) st.iconsEl.parentNode.removeChild(st.iconsEl);

    st.iconsEl = null;
    document.removeEventListener('mousedown', onDocDown, true);
  }

  // The icon button toggles the picker itself, so a press on it is left to the click.
  function onDocDown(ev) {
    const t = ev.target;

    if (!st.iconsEl || st.iconsEl.contains(t) || (t.closest && t.closest('.look-ic'))) return;

    closeIcons();
  }

  function closeOverlays() {
    const wasOpen = !!(st.pickerEl || st.iconsEl);

    closePicker();
    closeIcons();

    return wasOpen;
  }

  // ===== Toast =====

  function closeToast() {
    clearTimeout(st.toastTimer);

    if (st.toastEl && st.toastEl.parentNode) st.toastEl.parentNode.removeChild(st.toastEl);

    st.toastEl = null;
  }

  function toast(html) {
    const win = winEl();

    closeToast();

    if (!html || !win) return;

    st.toastEl = h('<div class="nlp-toast" data-own="">' + html + '</div>');
    win.appendChild(st.toastEl);
    st.toastTimer = setTimeout(closeToast, TOAST_MS);
  }

  // ===== Edits from the editor =====

  function clampLevel() {
    st.lv = Math.max(0, Math.min(st.lv, levelCount(st.sel) - 1));
  }

  function select(id) {
    st.sel = id;
    st.lv = 0;
  }

  function setText(t, lang, value) {
    const o = t ? { en: t.en, zh: t.zh } : { en: '', zh: '' };

    o[lang] = value;

    return normText(o);
  }

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

  function afterRemove() {
    if (!entryOrGhost(st.sel)) {
      const first = listIds()[0];

      select(first || 0);
    }

    clampLevel();
  }

  // ===== Events =====

  function onClick(ev) {
    const t = ev.target.closest('[data-a]');

    if (!t || (t.getAttribute('data-a') === 'sel' && ev.target.closest('.setting-toggle'))) return;

    const a = t.getAttribute('data-a');
    const v = t.getAttribute('data-v');
    const k = t.getAttribute('data-k');
    const c = Number(t.getAttribute('data-c'));
    const e = entryOf(st.sel);

    switch (a) {
      case 'view': st.view = v === 'all' ? 'all' : 'file'; break;
      case 'new': {
        const made = newReward();

        if (made) select(made.id);
        else toast(esc(L('There is no free reward number left.', '没有可用的奖励编号了。')));

        break;
      }
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

        if (made) select(made.id);
        else toast(esc(L('There is no free reward number left.', '没有可用的奖励编号了。')));

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

    render();
  }

  function onInput(ev) {
    const t = ev.target;
    const a = t.getAttribute('data-a');

    if (a === 'q' && st.picker) {
      st.picker.q = t.value;
      renderPicker(false);

      return;
    }

    const e = entryOf(st.sel);

    if (a !== 'txt' || !e) return;

    const key = t.getAttribute('data-k');

    editText(e, key, t.value);
    refreshPreview();

    if (key.indexOf('name:') === 0) {
      const nm = st.root.querySelector('.ab.is-sel .nm');

      if (nm) nm.textContent = nameOf(e, st.lang);
    }
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
    }

    if (st.root.parentNode !== el) el.appendChild(st.root);

    load();
    render();
  }

  function hideTab() {
    closeOverlays();
  }

  function escapeTab() {
    return closeOverlays();
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
    st.file =isObj(d.file) ? d.file : null;
    st.problems = isArr(d.problems) ? d.problems.map(String) : [];
    st.pending = null;
    closeOverlays();
    buildCatalogue();
    buildEntries(st.sel);
    render();
  }

  window.__slnlpTalents = { data, saved };

  if (!isArr(window.__modmanagerTabs)) window.__modmanagerTabs = [];

  window.__modmanagerTabs.push({ guid: GUID, id: TAB_ID, title: TITLE, show: showTab, hide: hideTab, escape: escapeTab });

  try {
    if (window.__modmanager && typeof window.__modmanager.tabsChanged === 'function') window.__modmanager.tabsChanged();
  } catch (err) {}
})();
