// New Loop Plus's web half. Runs in Root.html, the document every game page iframe sits in. Puts "End Run" in the
// pause menu (OutSetting) above Save & Exit, shows the plugin's confirm dialog there in the game's own dialog style, and
// shows a short toast when the plugin has something to say after the menu closed, and lays out the end-of-day rewards
// when Extra Pick offers more than three. Talks to the plugin through
// vuplex.postMessage("slmod|newloopplus|<verb>") and is driven by window.__slnlp.
(function () {
  if (window.__slnlp) return;

  var PREFIX = 'slmod|newloopplus|';
  var BUTTON_ID = 'slnlp-end';
  var DIALOG_ID = 'slnlp-confirm';
  var STYLE_ID = 'slnlp-style';
  var TOAST_ID = 'slnlp-toast';
  var POLL_MS = 400;
  var TOAST_MS = 6000;

  // The game's pause menu sinks Save & Exit (and its Rewrite button, when shown) to the bottom with margin-top:auto.
  // End Run takes that over and sits right above them, so the two buttons that end things stay together.
  var PAGE_CSS = [
    '#slnlp-end{margin-top:auto;border-left-color:rgba(235,163,8,.35)}',
    '#slnlp-end + .btn-restart,#slnlp-end + .btn-exit{margin-top:0}',
    '#slnlp-confirm .confirm-text{white-space:pre-line}'
  ].join('\n');

  var TOAST_CSS = '#slnlp-toast{position:fixed;left:50%;bottom:120px;transform:translateX(-50%);z-index:2147483002;pointer-events:none;display:none;max-width:640px;text-align:center;padding:10px 18px;border-radius:8px;border:1px solid rgba(255,193,7,.75);background:linear-gradient(180deg,rgba(30,30,33,.94),rgba(18,18,20,.96));color:#eee;font-family:"Microsoft YaHei","PingFang SC",sans-serif;font-size:14px;box-shadow:0 6px 20px rgba(0,0,0,.6)}';

  var label = 'End Run';
  var isWanted = false;
  var menuDoc = null;
  var toastTimer = 0;
  var hasReportedAccess = false;

  function send(message) {
    try {
      if (window.vuplex) window.vuplex.postMessage(PREFIX + message);
    } catch (e) {}
  }

  function addStyle(doc) {
    if (doc.getElementById(STYLE_ID)) return;

    var style = doc.createElement('style');

    style.id = STYLE_ID;
    style.textContent = PAGE_CSS;
    doc.head.appendChild(style);
  }

  // Vue re-renders the menu list whenever it likes and drops foreign nodes, so this runs on every poll.
  function placeButton(doc) {
    var list = doc.querySelector('.menu-list');
    var anchor = list && (list.querySelector(':scope > .btn-restart') || list.querySelector(':scope > .btn-exit'));
    var button = doc.getElementById(BUTTON_ID);

    if (!anchor || !isWanted) {
      if (button) button.remove();

      return;
    }

    if (!button) {
      button = doc.createElement('div');
      button.id = BUTTON_ID;
      button.className = 'main-btn';
      button.addEventListener('click', function () { send('ask'); });
    }

    if (button.nextElementSibling !== anchor) list.insertBefore(button, anchor);
    if (button.textContent !== label) button.textContent = label;
  }

  function hideDialog(doc) {
    var dialog = doc && doc.getElementById(DIALOG_ID);

    if (dialog) dialog.classList.remove('active');
  }

  // The game's own confirm dialog markup and classes (warn-overlay sits above the menu), built once per document.
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

  function confirm(title, body, cancel, ok) {
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

  function setButton(text, isShown) {
    label = text || label;
    isWanted = !!isShown;

    if (menuDoc) placeButton(menuDoc);
  }

  function toastNode() {
    if (!document.getElementById(TOAST_ID + '-style')) {
      var style = document.createElement('style');

      style.id = TOAST_ID + '-style';
      style.textContent = TOAST_CSS;
      document.head.appendChild(style);
    }

    var node = document.getElementById(TOAST_ID);

    if (node) return node;

    node = document.createElement('div');
    node.id = TOAST_ID;
    document.body.appendChild(node);
    // A one-line message is never shrunk.
    uiSize(node, { fit: false });

    return node;
  }

  function toast(text) {
    var node = toastNode();

    if (!text) {
      node.style.display = 'none';

      return;
    }

    node.textContent = text;
    node.style.display = 'block';
    clearTimeout(toastTimer);
    toastTimer = setTimeout(function () { node.style.display = 'none'; }, TOAST_MS);
  }

  function isShowing(frame) {
    return frame.offsetWidth > 0 && frame.offsetHeight > 0;
  }

  // The pause menu is a same-origin iframe, so its document can be reached from here; polled because the game creates
  // and hides that iframe whenever it likes. A dialog left open when the menu hides is closed, so it never greets the
  // player the next time the menu opens.
  function patchPauseMenu() {
    var frames = document.querySelectorAll('iframe');
    var found = null;

    for (var i = 0; i < frames.length; i++) {
      if (!/OutSetting\.html/.test(frames[i].src || '')) continue;

      var doc = null;

      try {
        doc = frames[i].contentDocument;
      } catch (e) {}

      if (!doc || !doc.body) {
        if (!hasReportedAccess) send('diag|cannot reach the pause menu document (root ' + location.protocol + ', iframe ' + frames[i].src + ')');
        hasReportedAccess = true;
        continue;
      }

      if (!isShowing(frames[i])) {
        hideDialog(doc);
        continue;
      }

      found = doc;
      watchPauseMenu(doc);
      addStyle(doc);
      placeButton(doc);
    }

    if (menuDoc && menuDoc !== found) hideDialog(menuDoc);

    menuDoc = found;
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

    if (!doc.getElementById(GRID_STYLE_ID)) {
      var style = doc.createElement('style');

      style.id = GRID_STYLE_ID;
      style.textContent = GRID_CSS;
      doc.head.appendChild(style);
    }

    root.classList.add(MANY);
    root.classList.toggle(COMPACT, rows > 1);
    root.style.setProperty('--slnlp-columns', String(Math.ceil(cardCount / rows)));
  }

  function patchRewardsPage() {
    var frames = document.querySelectorAll('iframe');

    for (var i = 0; i < frames.length; i++) {
      if (!/RogueSelect\.html/.test(frames[i].src || '')) continue;

      var doc = null;

      try {
        doc = frames[i].contentDocument;
      } catch (e) {}

      if (doc && doc.documentElement && doc.head) arrangeCards(doc, !!window.__slmorerewards);
    }
  }

  function patchPauseMenuSafely() {
    try {
      patchPauseMenu();
    } catch (e) {}
  }

  // Vue redraws the menu list as the menu opens, which drops End Run; putting it back the moment that happens, rather
  // than at the next poll, keeps it from popping in after the game's own buttons. An observer runs before the next paint.
  function watchPauseMenu(doc) {
    var win = doc.defaultView;

    if (doc.slnlpWatched || !win || typeof win.MutationObserver === 'undefined') return;

    doc.slnlpWatched = true;
    new win.MutationObserver(patchPauseMenuSafely).observe(doc, { childList: true, subtree: true });
  }

  // Only the pause menu's own page loading is patched on the spot. Any other game page loading leaves it to the poll,
  // since a patch reads the pause menu's size and so makes the browser lay out the page that is loading.
  function patchOnPauseMenuLoad(event) {
    var frame = (event && event.target) || this;

    if (frame && /OutSetting\.html/.test(frame.src || '')) patchPauseMenuSafely();
  }

  // A pause menu page loaded into a new iframe is watched as soon as it loads, not at the next poll. Root.html adds
  // each page's iframe on its own, so only an added iframe needs a look, never the inside of any other added element.
  function watchForFrames() {
    if (typeof MutationObserver === 'undefined') return;

    new MutationObserver(function (records) {
      for (var i = 0; i < records.length; i++) {
        var added = records[i].addedNodes;

        for (var j = 0; j < added.length; j++) {
          if (added[j].tagName === 'IFRAME') added[j].addEventListener('load', patchOnPauseMenuLoad);
        }
      }
    }).observe(document.documentElement, { childList: true, subtree: true });
  }

  watchForFrames();

  setInterval(function () {
    try {
      patchPauseMenu();
    } catch (e) {}

    try {
      patchRewardsPage();
    } catch (e) {}
  }, POLL_MS);

  // Hands a panel to Exiled's Toolkit to be drawn at the player's UI size. Before the Toolkit's script has loaded it
  // waits in a list the Toolkit reads; without the Toolkit nothing happens.
  function uiSize(node, options) {
    options = Object.assign({ mod: 'NewLoopPlus' }, options);
    var toolkit = window.ExiledUi;

    if (toolkit) {
      toolkit.size(node, options);
      return;
    }

    var waiting = window.__exiledUiWait = window.__exiledUiWait || [];
    waiting.push([node, options]);
    if (waiting.length > 32) waiting.shift();
  }

  window.__slnlp = { setButton: setButton, confirm: confirm, toast: toast };
  send('ready');
})();
