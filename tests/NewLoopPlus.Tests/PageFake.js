// A fake root page for page.js and Exiled's Toolkit's page API: elements that keep their children, classes and
// listeners, game iframes only once a test adds one with addFrame, the vuplex bridge, and a fake of the Toolkit's UI
// size that records the panels handed to it. Test code only; nothing here ships.
var window = this;

var intervals = [];

function setInterval(callback) {
  intervals.push(callback);

  return intervals.length;
}

// One run of every interval set, which is how Exiled's Toolkit's 250 ms page check runs.
function poll() {
  intervals.slice().forEach(function (callback) { callback(); });
}

function setTimeout() { return 1; }
function clearTimeout() {}

class FakeEl {
  constructor(tag) {
    var element = this;

    this.tagName = String(tag).toUpperCase();
    this.children = [];
    this.parentNode = null;
    this.isRoot = false;
    this.id = '';
    this.className = '';
    this.innerHTML = '';
    this.textContent = '';
    this.listeners = {};
    this.classes = [];
    this.found = {};
    this.all = {};
    this.style = { setProperty: function (name, value) { this[name] = value; } };
    this.classList = {
      add: function (name) { if (element.classes.indexOf(name) < 0) element.classes.push(name); },
      remove: function (name) { element.classes = element.classes.filter(function (c) { return c !== name; }); },
      contains: function (name) { return element.classes.indexOf(name) >= 0; },
      toggle: function (name, isOn) { if (isOn) this.add(name); else this.remove(name); }
    };
  }

  // In the page while some ancestor is a document's head or body.
  get isConnected() {
    for (var element = this; element; element = element.parentNode) {
      if (element.isRoot) return true;
    }

    return false;
  }

  addEventListener(type, listener) {
    var list = this.listeners[type] || (this.listeners[type] = []);

    if (list.indexOf(listener) < 0) list.push(listener);
  }

  fire(type) {
    var list = this.listeners[type] || [];

    for (var i = 0; i < list.length; i++) list[i].call(this, { type: type, target: this });
  }

  appendChild(child) {
    child.parentNode = this;
    this.children.push(child);

    return child;
  }

  remove() {
    var parent = this.parentNode;

    if (!parent) return;

    parent.children.splice(parent.children.indexOf(this), 1);
    this.parentNode = null;
  }

  // The parts the script builds with innerHTML, one element per selector, so the script can fill them and a test read them.
  querySelector(selector) {
    return this.found[selector] || (this.found[selector] = new FakeEl('div'));
  }

  querySelectorAll(selector) {
    return this.all[selector] || [];
  }
}

function findById(node, id) {
  if (node.id === id) return node;

  for (var i = 0; i < node.children.length; i++) {
    var found = findById(node.children[i], id);

    if (found) return found;
  }

  return null;
}

// A document at the address href: the root page or a game page in an iframe.
function fakeDocument(href) {
  var doc = {
    documentElement: new FakeEl('html'),
    head: new FakeEl('head'),
    body: new FakeEl('body'),
    defaultView: { location: { href: href } },
    createElement: function (tag) { return new FakeEl(tag); },
    getElementById: function (id) { return findById(this.head, id) || findById(this.body, id); },
    querySelector: function (selector) { return this.documentElement.querySelector(selector); },
    querySelectorAll: function () { return []; }
  };

  doc.head.isRoot = true;
  doc.body.isRoot = true;

  return doc;
}

var document = fakeDocument('https://game/UI/Root.html');
var frames = [];

document.getElementsByTagName = function (tag) { return tag === 'iframe' ? frames : []; };

// Every read of a game page's size, which makes the browser lay the page out.
var layoutReads = 0;

// Puts an iframe for a game page in the root page, as Root.html does, shown unless isHidden.
function addFrame(src, isHidden) {
  var frame = document.createElement('iframe');
  var size = isHidden ? 0 : 800;

  frame.src = src;
  frame.contentDocument = fakeDocument('https://game/UI/' + src.replace(/^.*\//, ''));
  Object.defineProperty(frame, 'offsetWidth', { get: function () { layoutReads++; return size; } });
  Object.defineProperty(frame, 'offsetHeight', { get: function () { layoutReads++; return size; } });
  frame.hide = function () { size = 0; };
  document.body.appendChild(frame);
  frames.push(frame);

  return frame;
}

// The end-of-day rewards page with this many cards in its grid.
function addRewardsPage(cardCount) {
  var doc = addFrame('../UI/RogueSelect.html').contentDocument;
  var grid = doc.querySelector('.card-grid');

  grid.all['.card-slot'] = [];

  for (var i = 0; i < cardCount; i++) grid.all['.card-slot'].push(new FakeEl('div'));

  return doc;
}

var sent = [];
window.vuplex = { postMessage: function (message) { sent.push(message); } };

var handed = [];
window.ExiledUi = { size: function (el, options) { handed.push({ el: el, options: options }); } };
