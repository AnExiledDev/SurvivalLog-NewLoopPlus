// New Loop Plus's web half. Runs in Root.html, the document every game page iframe sits in. Shows End Run's confirm
// dialog in the pause menu (OutSetting) in the game's dialog style (the End Run button itself is placed by Exiled's
// Toolkit, from the plugin), and lays out the end-of-day rewards when Extra Pick offers more than three. Talks to the
// plugin through Exiled's Toolkit's page API ("slmod|newloopplus|<verb>") and is driven by window.__slnlp.
(function () {
  if (window.__slnlp) return;

  var PREFIX = 'slmod|newloopplus|';
  var DIALOG_ID = 'slnlp-confirm';
  var STYLE_ID = 'slnlp-style';
  var PAUSE_MENU_PAGE = /OutSetting\.html/;
  var REWARDS_PAGE = /RogueSelect\.html/;

  var PAGE_CSS = '#slnlp-confirm .confirm-text{white-space:pre-line}';

  var etk = null;
  var pauseMenuWatch = null;
  var menuDoc = null;

  // Through the Toolkit's bridge. Nothing goes before its page API is there; the page says it is ready once it is.
  function send(message) {
    if (etk) etk.send(PREFIX, message);
  }

  function hideDialog(doc) {
    var dialog = doc && doc.getElementById(DIALOG_ID);

    if (dialog) dialog.classList.remove('active');
  }

  // The game's confirm dialog markup and classes (warn-overlay sits above the menu), built once per document.
  function dialogIn(doc) {
    var dialog = doc.getElementById(DIALOG_ID);

    if (dialog) return dialog;

    dialog = doc.createElement('div');
    dialog.id = DIALOG_ID;
    dialog.className = 'modal-overlay warn-overlay';
    dialog.innerHTML = '<div class="confirm-box"><div class="confirm-title"></div><div class="confirm-text"></div>' +
      '<div class="confirm-buttons"><div class="c-btn c-btn-cancel"></div><div class="c-btn c-btn-confirm"></div></div></div>';
    dialog.addEventListener('click', function (event) {
      if (event.target === dialog) hideDialog(doc);
    });
    dialog.querySelector('.c-btn-cancel').addEventListener('click', function () { hideDialog(doc); });
    dialog.querySelector('.c-btn-confirm').addEventListener('click', function () {
      if (!dialog.classList.contains('active')) return;

      hideDialog(doc);
      send('end');
    });
    doc.body.appendChild(dialog);

    return dialog;
  }

  // The button's click reaches here through the plugin, so the menu may have opened since the last check: look again.
  function confirm(title, body, cancel, ok) {
    findPauseMenu();

    if (!menuDoc || !menuDoc.body) {
      send('diag|no pause menu to show the confirm dialog in');

      return;
    }

    var dialog = dialogIn(menuDoc);

    dialog.querySelector('.confirm-title').textContent = title;
    dialog.querySelector('.confirm-text').textContent = body;
    dialog.querySelector('.c-btn-cancel').textContent = cancel;
    dialog.querySelector('.c-btn-confirm').textContent = ok;
    dialog.classList.add('active');
  }

  function isShowing(frame) {
    return frame.offsetWidth > 0 && frame.offsetHeight > 0;
  }

  // The pause menu is a same-origin iframe the game creates and hides whenever it likes, so it is handed over at every
  // check. A dialog left open when the menu hides is closed, so it never greets the player the next time the menu opens.
  function watchPauseMenu(doc, frame) {
    if (!doc.body) return false;

    if (!isShowing(frame)) {
      hideDialog(doc);

      if (menuDoc === doc) menuDoc = null;

      return;
    }

    if (menuDoc && menuDoc !== doc) hideDialog(menuDoc);

    menuDoc = doc;
    etk.addStyle(doc, STYLE_ID, PAGE_CSS);
  }

  // A menu that is gone since the last check is forgotten, and its dialog closed, so the dialog only ever shows in the
  // menu showing now.
  function findPauseMenu() {
    if (!pauseMenuWatch) return;

    var before = menuDoc;

    menuDoc = null;
    pauseMenuWatch.refresh();

    if (before && before !== menuDoc) hideDialog(before);
  }

  // Extra Pick can put more than the game's three cards on the end-of-day rewards page (RogueSelect.html). They go in
  // even rows of at most five, smaller once there is more than one row, and scroll when they still do not fit, as More
  // Rewards lays them out. With More Rewards installed its own layout does this, so this one stands aside.
  var GAME_CARDS = 3;
  var MOST_PER_ROW = 5;
  var GRID_STYLE_ID = 'slnlp-grid-style';
  var MANY = 'slnlp-many';
  var COMPACT = 'slnlp-compact';
  var GRID_CSS = [
    'html.slnlp-many .content{min-height:0}',
    'html.slnlp-many .card-grid{flex:0 1 auto;min-height:0;overflow-x:hidden;overflow-y:auto;' +
      'grid-template-columns:repeat(var(--slnlp-columns),minmax(0,320px));justify-content:center;gap:16px;padding:42px 36px 36px}',
    'html.slnlp-many .card-grid::-webkit-scrollbar{width:8px}',
    'html.slnlp-many .card-grid::-webkit-scrollbar-track{background:rgba(0,0,0,.25);border-radius:4px}',
    'html.slnlp-many .card-grid::-webkit-scrollbar-thumb{background:rgba(255,200,80,.35);border-radius:4px}',
    'html.slnlp-compact .card-slot{display:flex}',
    'html.slnlp-compact .card{flex:1;min-height:0;padding:16px 16px 16px}',
    'html.slnlp-compact .icon-frame{width:64px;height:64px;margin-bottom:10px}',
    'html.slnlp-compact .card-name{font-size:16px;letter-spacing:0;overflow-wrap:anywhere;margin-bottom:8px;padding-bottom:8px}',
    'html.slnlp-compact .card-desc{font-size:13px;line-height:1.5;overflow-wrap:anywhere;margin-bottom:10px}',
    'html.slnlp-compact .card-cost{overflow-wrap:anywhere;margin-bottom:8px}',
    'html.slnlp-compact .card-btn{font-size:14px;letter-spacing:2px;white-space:normal;overflow-wrap:anywhere;padding:9px 6px 10px}'
  ].join('\n');

  function arrangeCards(doc, isMoreRewardsHere) {
    var root = doc.documentElement;
    var grid = doc.querySelector('.card-grid');
    var cardCount = grid ? grid.querySelectorAll('.card-slot').length : 0;

    if (isMoreRewardsHere || cardCount <= GAME_CARDS) {
      root.classList.remove(MANY);
      root.classList.remove(COMPACT);

      return;
    }

    var rows = Math.ceil(cardCount / MOST_PER_ROW);

    etk.addStyle(doc, GRID_STYLE_ID, GRID_CSS);
    root.classList.add(MANY);
    root.classList.toggle(COMPACT, rows > 1);
    root.style.setProperty('--slnlp-columns', String(Math.ceil(cardCount / rows)));
  }

  // The game adds cards after the page loads, and a refresh redraws them, so the page is handed over at every check.
  function watchRewardsPage(doc) {
    if (!doc.documentElement || !doc.head) return false;

    arrangeCards(doc, !!window.__slmorerewards);
  }

  // Runs once the Toolkit's page API is there.
  function startWithToolkit(api) {
    etk = api;
    pauseMenuWatch = etk.watchPages(PAUSE_MENU_PAGE, watchPauseMenu, { everyCheck: true });
    etk.watchPages(REWARDS_PAGE, watchRewardsPage, { everyCheck: true });
    send('ready');
  }

  window.__slnlp = { confirm: confirm };
  (window.ExiledToolkitReady = window.ExiledToolkitReady || []).push(startWithToolkit);
})();
